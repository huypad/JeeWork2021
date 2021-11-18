import { tap, catchError, finalize } from 'rxjs/operators';
import { WorkService } from './../../work/work.service';
import { UpdateStatusProjectComponent } from './../../projects-team/update-status-project/update-status-project.component';
import { ProjectsTeamService } from './../../projects-team/Services/department-and-project.service';
import { DepartmentProjectDataSource } from './../../projects-team/Model/data-sources/department-and-project.datasource';
import { MenuAsideService } from './../../../../_metronic/jeework_old/core/_base/layout/services/menu-aside.service';
import { ListDepartmentService } from './../../department/Services/List-department.service';
import { SortState } from './../../../../_metronic/shared/crud-table/models/sort.model';
import { PaginatorState } from './../../../../_metronic/shared/crud-table/models/paginator.model';
import { CommonService } from './../../../../_metronic/jeework_old/core/services/common.service';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import {
  MessageType,
  LayoutUtilsService,
} from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import {
  Component,
  OnInit,
  ElementRef,
  ViewChild,
  ChangeDetectionStrategy,
  Input,
  SimpleChange,
  OnChanges,
  ChangeDetectorRef,
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { BehaviorSubject, fromEvent, merge, throwError } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
// Models
import { DepartmentModel } from '../../department/Model/List-department.model';
import { WeWorkService } from '../../services/wework.services';


@Component({
  selector: 'app-page-work-department',
  templateUrl: './page-work-department.component.html',
  styleUrls: ['./page-work-department.component.scss']
})
export class PageWorkDepartmentComponent implements OnInit, OnChanges {
  constructor(
    public deptService: ProjectsTeamService,
    private danhMucService: DanhMucChungService,
    private workService: WorkService,
    public dialog: MatDialog,
    public _deptServices: ListDepartmentService,
    private route: ActivatedRoute,
    private router: Router,
    private changeDetectorRefs: ChangeDetectorRef,
    private layoutUtilsService: LayoutUtilsService,
    public commonService: CommonService,
    public WeWorkService: WeWorkService,
    private translate: TranslateService,
    private tokenStorage: TokenStorage,
    public menuAsideService: MenuAsideService,
  ) { }
  // Table fields
  dataSource: DepartmentProjectDataSource;

  isLoading = true;
  Id_Department = 0;
  @Input() Values: any = {};
  flag = true;
  isFolder = false;
  ngOnInit() {
    const path = this.router.url;
    if (path) {
      const arr = path.split('/');
      if (arr.length > 2) { this.Id_Department = +arr[2]; }
    }

    this.dataSource = new DepartmentProjectDataSource(this.deptService);

    this.route.params.subscribe((params) => {
      if (params?.id){
        this.Id_Department = params.id;
        // this.loadDataList();
        this.LoadDataFolder();
      }
    });

    this.LoadDataFolder();
  }

  ngOnChanges() {
    // if (this.dataSource) this.loadDataList();
  }
  LoadDataFolder() {
    this.isLoading = true;
    this._deptServices.DeptDetail(this.Id_Department).subscribe(res => {
      if (res && res.status == 1) {
        if (res.data.ParentID) {
          this.isFolder = true;
        }else{
          this.isFolder = false;
        }
        this.isLoading = false;
        this.changeDetectorRefs.detectChanges();
      }
    });
  }

  loadDataList() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration()
    );
    this.dataSource.loadListProjectByDepartment(queryParams);
  }

  // loadPage() {
  //   var arrayData = [];
  //   this.dataSource.entitySubject.subscribe((res) => (arrayData = res));
  //   if (arrayData !== undefined && arrayData.length == 0) {
  //     var totalRecord = 0;
  //     this.dataSource.paginatorTotal$.subscribe((tt) => (totalRecord = tt));
  //     const queryParams1 = new QueryParamsModelNew(
  //       this.filterConfiguration()
  //     );
  //     this.dataSource.loadListProjectByDepartment(queryParams1);
  //   }
  // }



  filterConfiguration(): any {
    let filter: any = {};
    if (this.Values) { filter = this.Values; }
    if (this.Id_Department > 0) {
      filter.id_department = this.Id_Department;
    }
    return filter;
  }


  getHeight(): any {
    const obj = window.location.href.split('/').find((x) => x == 'wework');
    let tmp_height = 0;
    if (obj) {
      tmp_height = window.innerHeight - 190;
    } else {
      tmp_height = window.innerHeight - 175;
    }
    return tmp_height - this.tokenStorage.getHeightHeader() + 'px';
  }


}
