import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { TranslateService } from '@ngx-translate/core';
import { AttachmentService } from './../../services/attachment.service';
import { AttachmentModel, FileUploadModel } from './../../projects-team/Model/department-and-project.model';
import { ActivatedRoute, Router } from '@angular/router';
import { DocumentsService } from './../documents.service';
import { BehaviorSubject } from 'rxjs';
import { Component, OnInit, ChangeDetectorRef, ViewChild } from '@angular/core';
import { ProjectsTeamService } from '../../projects-team/Services/department-and-project.service';

@Component({
  selector: 'kt-list-documents',
  templateUrl: './list-documents.component.html',
  styleUrls: ['./list-documents.component.scss'],
})
export class ListDocumentsComponent implements OnInit {
  // dataSource: DocumentDataSource;
  dataSource: any = [];
  loadingSubject = new BehaviorSubject<boolean>(false);
  loading1$ = this.loadingSubject.asObservable();
  id_project_team = 0;
  keyword = '';
  pageSize: number;
  @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  documentResult: any = [];

  constructor(
    private DocumentsService: DocumentsService,
    private projectsTeamService: ProjectsTeamService,
    private activatedRoute: ActivatedRoute,
    private router: Router,
    private translate: TranslateService,
    private LayoutUtilsService: LayoutUtilsService,
    private changeDetectorRefs: ChangeDetectorRef,
    private _attservice: AttachmentService,
    private tokenStorage: TokenStorage
  ) {}

  ngOnInit() {
    var arr = this.router.url.split('/');
    this.id_project_team = +arr[2];

    this.tokenStorage.getPageSize().subscribe((res) => {
      this.pageSize = +res;
    });
    this.LoadData();
  }

  displayedColumns = ['filename', 'lichsu', 'size', 'action'];

  LoadData() {
    const queryParams = new QueryParamsModelNew(this.filterConfiguration(), '', '', 1, 50, true);
    this.LayoutUtilsService.showWaitingDiv();
    this.DocumentsService.ListDocuments(queryParams).subscribe((res) => {
      this.LayoutUtilsService.OffWaitingDiv();
      if (res && res.status == 1) {
        this.dataSource = res.data;
      }
      this.changeDetectorRefs.detectChanges();
    });
  }

  getHeight() {
    return window.innerHeight - 120 - this.tokenStorage.getHeightHeader() + 'px';
  }

  applyFilter(text: string) {
    this.keyword = text;
    this.LoadData();
  }

  filterConfiguration(): any {
    const filter: any = {};
    filter.keyword = this.keyword;
    filter.id_project_team = this.id_project_team;
    return filter;
  }

  TenFile = '';
  File = '';

  onSelectFile_PDF(evt) {
    if (evt.target.files && evt.target.files.length) {
      //Nếu có file
      var size = evt.target.files[0].size;
      if (size / 1024 / 1024 > 15) {
        this.LayoutUtilsService.showError('File upload không được vượt quá 15 MB');
        return;
      }
      let file = evt.target.files[0]; // Ví dụ chỉ lấy file đầu tiên
      this.TenFile = file.name;
      let reader = new FileReader();
      reader.readAsDataURL(evt.target.files[0]);
      let base64Str;
      setTimeout(() => {
        base64Str = reader.result as String;
        var metaIdx = base64Str.indexOf(';base64,');
        base64Str = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64
        this.File = base64Str;
        var _model = new AttachmentModel();
        _model.object_type = 4;
        _model.object_id = this.id_project_team; // object_id = id_project_team
        const ct = new FileUploadModel();
        ct.strBase64 = this.File;
        ct.filename = this.TenFile;
        ct.IsAdd = true;
        _model.item = ct;
        this._attservice.Upload_attachment(_model).subscribe((res) => {
          if (res && res.status === 1) {
            this.LoadData();
            this.LoadParent(true);
            const _messageType = this.translate.instant('GeneralKey.capnhatthanhcong');
            this.LayoutUtilsService.showInfo(_messageType)
              .afterDismissed()
              .subscribe((tt) => {});
          } else {
            this.LayoutUtilsService.showError(res.error.message);
          }
          this.changeDetectorRefs.detectChanges();
        });
      }, 2000);
    } else {
      this.File = '';
    }
  }

  Delete_File(val: any) {
    // truyền vào id_row
    const _title = this.translate.instant('GeneralKey.xoa');
    const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
    const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
    const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
    const dialogRef = this.LayoutUtilsService.deleteElement(_title, _description, _waitDesciption);
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      }
      this.LayoutUtilsService.showWaitingDiv();
      this._attservice.delete_attachment(val).subscribe((res) => {
        this.LayoutUtilsService.OffWaitingDiv();
        if (res && res.status === 1) {
          this.LoadData();
          this.LoadParent(true);
          this.changeDetectorRefs.detectChanges();
          this.LayoutUtilsService.showInfo(_deleteMessage);
        } else {
          this.LayoutUtilsService.showError(res.error.message);
        }
      });
    });
  }

  DownloadFile(link) {
    window.open(link);
  }

  preview(link) {
    this.LayoutUtilsService.ViewDoc(link);
  }

  LoadParent(value): void {
    this.projectsTeamService.changeMessage(value);
  }
}
