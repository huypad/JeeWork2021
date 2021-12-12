import { LayoutUtilsService, MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { MatDialog } from '@angular/material/dialog';
import { WorkGroupEditComponent } from './../../../work/work-group-edit/work-group-edit.component';
import { WorkGroupModel } from './../../../work/work.model';
import { SortState } from './../../../../../_metronic/shared/crud-table/models/sort.model';
import { BehaviorSubject } from 'rxjs';
import { Route, Router } from '@angular/router';
import { PaginatorState } from './../../../../../_metronic/shared/crud-table/models/paginator.model';
import { take } from 'rxjs/operators';
import { WorkService } from './../../../work/work.service';
import { Component, OnInit } from '@angular/core';
import { QueryParamsModelNew } from 'src/app/_metronic/jeework_old/core/models/query-models/query-params.model';
import { WorkGroupDataSource } from './work-group.datasource';
import { TranslateService } from '@ngx-translate/core';

@Component({
  selector: 'app-work-group',
  templateUrl: './work-group.component.html',
  styleUrls: ['./work-group.component.scss']
})
export class WorkGroupComponent implements OnInit {

  ID_Project = 0;
  dataSource: WorkGroupDataSource;
  productsResult: any = [];
  sorting: SortState = new SortState();
  paginatorNew: PaginatorState = new PaginatorState();
  constructor(
    private workService: WorkService,
    public dialog: MatDialog,
    public LayoutUtilsService: LayoutUtilsService,
    // private route:Route,
    private router: Router,
    private translate: TranslateService,
  ) { }

  displayedColumns = [
    "title",
    "ngaytao",
    "nguoitao",
    "ngaycapnhat",
    "nguoicapnhat",
    "nguoitheodoi",
    "tongsocongviec",
    "locked",
    "action",
  ];

  ngOnInit(): void {

    var path = this.router.url;
    if (path) {
      var arr = path.split("/");
      if (arr.length > 2) this.ID_Project = +arr[2];
    }

    // const queryparam = new QueryParamsModelNew(this.filterConfiguration());
    // this.workService.ListWorkGroup(queryparam).subscribe(
    //   res => {
    //   }
    // )

    this.dataSource = new WorkGroupDataSource(this.workService);

    this.dataSource.entitySubject.subscribe(
      (res) => (this.productsResult = res)
    );

    this.dataSource.paginatorTotal$.subscribe(
      (res) => (this.paginatorNew.total = res)
    );
    // this.route.params.subscribe((params) => {
    //   // this.loadDataList();
    // });

    setTimeout(() => {
      this.dataSource.loading$ = new BehaviorSubject<boolean>(false);
    }, 3000);
  }

  loadDataList() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      this.sorting.direction,
      this.sorting.column,
      this.paginatorNew.page - 1,
      this.paginatorNew.pageSize
    );
    this.dataSource.loadListWorkGroup(queryParams);
    setTimeout((x) => {
      this.loadPage();
    }, 500);
  }
  sortField(column: string) {
    const sorting = this.sorting;
    const isActiveColumn = sorting.column === column;
    if (!isActiveColumn) {
      sorting.column = column;
      sorting.direction = "asc";
    } else {
      sorting.direction = sorting.direction === "asc" ? "desc" : "asc";
    }
    // this.paginatorNew.page = 1;
    this.loadDataList();
  }
  loadPage() {
    var arrayData = [];
    this.dataSource.entitySubject.subscribe((res) => (arrayData = res));
    if (arrayData && arrayData.length == 0) {
      var totalRecord = 0;
      this.dataSource.paginatorTotal$.subscribe((tt) => (totalRecord = tt));
      if (totalRecord > 0) {
        const queryParams = new QueryParamsModelNew(
          this.filterConfiguration(),
          this.sorting.direction,
          this.sorting.column,
          this.paginatorNew.page - 1,
          this.paginatorNew.pageSize
        );
        this.dataSource.loadListWorkGroup(queryParams);
      } else {
        const queryParams = new QueryParamsModelNew(
          this.filterConfiguration(),
          this.sorting.direction,
          this.sorting.column,
          this.paginatorNew.page = 0,
          this.paginatorNew.pageSize
        );
        this.dataSource.loadListWorkGroup(queryParams);
      }
    }
  }

  filterConfiguration(): any {
    const filter: any = {};
    filter.id_project_team = this.ID_Project;
    return filter;
  }

  paginate(paginator: PaginatorState) {
    this.loadDataList();
  }

  UpdateItem(item) {
    // return;
    // this.workService.UpdateWorkGroup
    this.chinhsuaNhomCV(item);
  }
  chinhsuaNhomCV(item) {
    let saveMessageTranslateParam = "";
    var _item = new WorkGroupModel();
    _item.clear();
    _item.id_project_team = "" + this.ID_Project;
    if (item && item.id_row) {
      _item.id_row = item.id_row;
      _item.title = item.title;
      _item.description = item.description;
    }
    saveMessageTranslateParam +=
      _item.id_row > 0
        ? "GeneralKey.capnhatthanhcong"
        : "GeneralKey.themthanhcong";
        debugger
    const dialogRef = this.dialog.open(WorkGroupEditComponent, {
      data: { _item },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      } else {
        this.loadDataList();
      }
    });
  }
  Themmoi() {
    this.chinhsuaNhomCV(null);
  }
  DeleteItem(item) {
    const _title = this.translate.instant("GeneralKey.xoa");
    const _description = this.translate.instant(
      "GeneralKey.bancochacchanmuonxoakhong"
    );
    const _waitDesciption = this.translate.instant(
      "GeneralKey.dulieudangduocxoa"
    );
    const _deleteMessage = this.translate.instant("GeneralKey.xoathanhcong");

    const dialogRef = this.LayoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      }
      if (item.Count.tong > 0 || item.Count.tong > 0) {
        this.LayoutUtilsService.showError('Nhóm công có công việc đang thực hiện không thể xóa.');
      }
      else {
        this.workService.DeleteWorkGroup(item.id_row).subscribe(res => {
          if (res && res.status === 1) {
            this.loadDataList();
          }
          else {
            // this.LayoutUtilsService.showError(res.error.message);
        this.LayoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 9999999, 'top', 0);

          }
        });
      }
    })
  }
  lock1(id: number) {
    // this.quanLyTaiKhoanService.getLockUnLock(id).subscribe((res) => {
    //   if (res && res.status == 1) {
    //     this.loadDataList();
    //   } else {
    //   }
    // });
  }
  lock(val: any, row: any) {
    this.workService.CloseWorkGroup(row, val.checked).subscribe(res => {
      if (res && res.status == 1) {
        this.loadDataList();
      }
      else {
        this.LayoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 9999999, 'top', 0);
      }
    });
  }
}
