import { UpdateStatusProjectComponent } from './../../projects-team/update-status-project/update-status-project.component';
import { ProjectsTeamService } from './../../projects-team/Services/department-and-project.service';
import { DepartmentProjectDataSource } from './../../projects-team/Model/data-sources/department-and-project.datasource';
import { MenuAsideService } from './../../../../_metronic/jeework_old/core/_base/layout/services/menu-aside.service';
import { ListDepartmentService } from './../../List-department/Services/List-department.service';
import { SortState } from "./../../../../_metronic/shared/crud-table/models/sort.model";
import { PaginatorState } from "./../../../../_metronic/shared/crud-table/models/paginator.model";
import { CommonService } from "./../../../../_metronic/jeework_old/core/services/common.service";
import { TokenStorage } from "./../../../../_metronic/jeework_old/core/auth/_services/token-storage.service";
import { QueryParamsModelNew } from "./../../../../_metronic/jeework_old/core/models/query-models/query-params.model";
import { DanhMucChungService } from "./../../../../_metronic/jeework_old/core/services/danhmuc.service";
import {
  MessageType,
  LayoutUtilsService,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
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
} from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
// Material
import { MatDialog } from "@angular/material/dialog";
import { SelectionModel } from "@angular/cdk/collections";
// RXJS
import { BehaviorSubject, fromEvent, merge } from "rxjs";
import { TranslateService } from "@ngx-translate/core";
// Models
import { DepartmentModel } from "../../List-department/Model/List-department.model";
import { WeWorkService } from "../../services/wework.services";


@Component({
  selector: 'app-page-work-department',
  templateUrl: './page-work-department.component.html',
  styleUrls: ['./page-work-department.component.scss']
})
export class PageWorkDepartmentComponent implements OnInit, OnChanges {
  // Table fields
  dataSource: DepartmentProjectDataSource;
  displayedColumns = [
    "pie",
    "title",
    "department",
    "hoten",
    "Status",
    "Locked",
    "TrangThai",
  ];
  // @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
  //@ViewChild(MatSort, { static: true }) sort: MatSort;
  sorting: SortState = new SortState();
  // Filter fields
  listchucdanh: any[] = [];
  // Selection
  selection = new SelectionModel<DepartmentModel>(true, []);
  productsResult: DepartmentModel[] = [];
  //=================PageSize Table=====================
  paginatorNew: PaginatorState = new PaginatorState();
  pageSize: number;
  Id_Department: number = 0;
  @Input() Values: any = {};
  flag: boolean = true;
  constructor(
    public deptService: ProjectsTeamService,
    private danhMucService: DanhMucChungService,
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
  ngOnInit() {
    var path = this.router.url;
    if (path) {
      var arr = path.split("/");
      if (arr.length > 2) this.Id_Department = +arr[2];
    }
 
    this.dataSource = new DepartmentProjectDataSource(this.deptService);

    this.dataSource.entitySubject.subscribe(
      (res) => (this.productsResult = res)
    );

    this.dataSource.paginatorTotal$.subscribe(
      (res) => (this.paginatorNew.total = res)
    );
    this.route.params.subscribe((params) => {
      this.loadDataList();
    });

    setTimeout(() => {
      this.dataSource.loading$ = new BehaviorSubject<boolean>(false);
    }, 3000);
  }

  ngOnChanges() {
    // if (this.dataSource) this.loadDataList();
  }
  
  
  loadDataList() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      this.sorting.direction,
      this.sorting.column,
      this.paginatorNew.page - 1,
      this.paginatorNew.pageSize
    );
    this.dataSource.loadListProjectByDepartment(queryParams);
    // setTimeout((x) => {
    //   this.loadPage();
    // }, 500);
  }
  
  loadPage() {
    var arrayData = [];
    this.dataSource.entitySubject.subscribe((res) => (arrayData = res));
    if (arrayData !== undefined && arrayData.length == 0) {
      var totalRecord = 0;
      this.dataSource.paginatorTotal$.subscribe((tt) => (totalRecord = tt));
      const queryParams1 = new QueryParamsModelNew(
        this.filterConfiguration(),
        this.sorting.direction,
        this.sorting.column,
        this.paginatorNew.page = 0,
        this.paginatorNew.pageSize,
        true
      );
      this.dataSource.loadListProjectByDepartment(queryParams1);
    }
  }
  

  
  filterConfiguration(): any {
    let filter: any = {};
    if (this.Values) filter = this.Values;
    if (this.Id_Department > 0) filter.id_department = this.Id_Department;
    return filter;
  }

  
  getHeight(): any {
    let obj = window.location.href.split("/").find((x) => x == "wework");
    let tmp_height = 0;
    if (obj) {
      tmp_height = window.innerHeight - 190;
    } else {
      tmp_height = window.innerHeight - 175;
    }
    return tmp_height - this.tokenStorage.getHeightHeader() + "px";
  }
  
    
  
}
