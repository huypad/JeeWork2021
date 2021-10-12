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
import { UpdateStatusProjectComponent } from "../update-status-project/update-status-project.component";
import { ProjectTeamEditComponent } from "../project-team-edit/project-team-edit.component";
import { DepartmentProjectDataSource } from "../Model/data-sources/department-and-project.datasource";
import { ProjectsTeamService } from "../Services/department-and-project.service";
import { ProjectTeamModel } from "../Model/department-and-project.model";
import { WeWorkService } from "../../services/wework.services";

@Component({
  selector: "kt-department-project-tab",
  templateUrl: "./department-project-tab.component.html",
  // 	styles: [`
  // 	ngb-progressbar {
  // 		margin-top: 5rem;
  // 	}
  // `],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DepartmentProjectTabComponent implements OnInit, OnChanges {
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

    if (this.Id_Department > 0) {
      this.LoadDataFolder();
    }
    this.tokenStorage.getPageSize().subscribe((res) => {
      this.pageSize = +res;
    });
    this.dataSource = new DepartmentProjectDataSource(this.deptService);

    this.dataSource.entitySubject.subscribe(
      (res) => (this.productsResult = res)
    );

    this.dataSource.paginatorTotal$.subscribe(
      (res) => (this.paginatorNew.total = res)
    );


    this.route.queryParams.subscribe((params) => {
      if (params?.id){
          this.Id_Department = params.id;
          this.loadDataList();
          this.LoadDataFolder();
        }
    });
    this.loadDataList();
    setTimeout(() => {
      this.dataSource.loading$ = new BehaviorSubject<boolean>(false);
    }, 10000);
  }

  ngOnChanges() {
    if (this.dataSource) this.loadDataList();
  }
  DrawPie(point: number) {
    if (point == 0) return "conic-gradient(#d1cbcb 25%, #d1cbcb 0 75%)";
    else if (point <= 50) {
      var numbers = 100;
      numbers = numbers - point;
      return (
        "conic-gradient(#f1e208 " + point + "%, #d1cbcb 0 " + numbers + "%)"
      );
    } else if (point < 100) {
      var number100 = 100;
      number100 = number100 - point;
      return (
        "conic-gradient(#2068ce " + point + "%, #d1cbcb 0 " + number100 + "%)"
      );
    } else return "conic-gradient(#16f002 25%, #16f002 0 75%)";
  }
  loadDataList() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      this.sorting.direction,
      this.sorting.column,
      this.paginatorNew.page - 1,
      this.paginatorNew.pageSize
    );
    if (this.Id_Department > 0)
      this.dataSource.loadListProjectByDepartment(queryParams);
    else this.dataSource.loadListProject(queryParams);
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
    if (arrayData !== undefined && arrayData.length == 0) {
      var totalRecord = 0;
      this.dataSource.paginatorTotal$.subscribe((tt) => (totalRecord = tt));
      if (totalRecord > 0) {
        const queryParams1 = new QueryParamsModelNew(
          this.filterConfiguration(),
          this.sorting.direction,
          this.sorting.column,
          this.paginatorNew.page - 1,
          this.paginatorNew.pageSize
        );
        if (this.Id_Department > 0)
          this.dataSource.loadListProjectByDepartment(queryParams1);
        else this.dataSource.loadListProject(queryParams1);
      } else {
        const queryParams1 = new QueryParamsModelNew(
          this.filterConfiguration(),
          this.sorting.direction,
          this.sorting.column,
          (this.paginatorNew.page = 0),
          this.paginatorNew.pageSize
        );
        if (this.Id_Department > 0)
          this.dataSource.loadListProjectByDepartment(queryParams1);
        else this.dataSource.loadListProject(queryParams1);
      }
    }
  }
  getItemStatusString(status: number = 0): string {
    switch (status) {
      case 0:
        return "Selling";
      case 1:
        return "Sold";
    }
    return "";
  }

  getColorProgressbar(status: number = 0): string {
    if (status < 50) return "metal";
    else if (status < 100) return "brand";
    else return "success";
  }
  /**
   * Returns item condition
   *
   * @param condition: number
   */
  getItemConditionString(condition: number = 0): string {
    switch (condition) {
      case 1:
        return this.translate.instant("projects.dungtiendo");
      case 3:
        return this.translate.instant("projects.ruirocao");
    }
    return this.translate.instant("projects.chamtiendo");
  }

  /**
   * Returns CSS Class name by condition
   *
   * @param condition: number
   */

  filterConfiguration(): any {
    let filter: any = {};
    if (this.Values) filter = this.Values;
    if (this.Id_Department > 0) filter.id_department = this.Id_Department;
    return filter;
  }

  XuatFile(item: any) {
    var linkdownload = item.Link;
    window.open(linkdownload);
  }

  Add() {
    const ProcessWorkModels = new DepartmentModel();
    ProcessWorkModels.clear(); // Set all defaults fields
    this.Update(ProcessWorkModels);
  }

  Update(_item: DepartmentModel) { }
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
  updateStage(_item: DepartmentModel) {
    // this.layoutUtilsService.showActionNotification("Updating");
    let saveMessageTranslateParam = "";

    saveMessageTranslateParam +=
      _item.id_row > 0
        ? "GeneralKey.capnhatthanhcong"
        : "GeneralKey.themthanhcong";
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.id_row > 0 ? MessageType.Update : MessageType.Create;

    const dialogRef = this.dialog.open(UpdateStatusProjectComponent, {
      data: { _item, _IsEdit: _item.IsEdit },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      } else {
        this.loadDataList();
        this.layoutUtilsService.showActionNotification(
          _saveMessage,
          _messageType,
          4000,
          true,
          false,
          3000,
          "top",
          1
        );
        this.changeDetectorRefs.detectChanges();
      }
    });
  }
  quickEdit(_item: ProjectTeamModel) {
    // this.layoutUtilsService.showActionNotification("Updating");
    //_item = this.item;
    let saveMessageTranslateParam = "";

    saveMessageTranslateParam +=
      _item.id_row > 0
        ? "GeneralKey.capnhatthanhcong"
        : "GeneralKey.themthanhcong";
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(ProjectTeamEditComponent, {
      data: { _item, _IsEdit: _item.IsEdit, is_project: false },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      } else {
        this.loadDataList();
        this.menuAsideService.loadMenu();
        // this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false, 3000, 'top', 1);
        this.changeDetectorRefs.detectChanges();
      }
    });
  }
  Status: any[] = [
    {
      name: this.translate.instant("projects.chamtiendo"),
      value: 2,
      color: "accent",
    },
    {
      name: this.translate.instant("projects.ruirocao"),
      value: 3,
      color: "danger",
    },
    {
      name: this.translate.instant("projects.dungtiendo"),
      value: 1,
      color: "info",
    },
  ];

  getColor(status) {
    let _status = this.Status.filter((x) => x.value == status)[0];
    return _status ? _status.color : "purple";
  }
  getItemLockedString(condition: boolean): string {
    return condition
      ? this.translate.instant("filter.locked")
      : this.translate.instant("filter.active");
  }

  getColorLocked(status: boolean) {
    return status ? "danger" : "complete";
  }

  Viewdetail(item) {
    // [routerLink]="['/project',item.id]"
    this.router.navigate(["/project", item.id_row]);
  }

  paginate(paginator: PaginatorState) {
    this.loadDataList();
  }

  dataFolder: any = [];
  loadListfolder = false;
  LoadDataFolder() {
    this._deptServices.DeptDetail(this.Id_Department).subscribe(res => {
      if (res && res.status == 1) {
        if (!res.data.ParentID) {
          this.dataFolder = res.data.data_folder;
          var itemhientai = {
            CreatedBy: res.data.CreatedBy,
            CreatedDate: res.data.CreatedDate,
            id_row: res.data.id_row,
            parentid: res.data.ParentID,
            templateid: res.data.Template?.[0]?.TemplateID,
            title: 'Dự án trực tiếp của phòng ban',
          }
          this.dataFolder.unshift(itemhientai)
          this.loadListfolder = true;
          this.changeDetectorRefs.detectChanges();
        }

      }
    })
  }

  ReloadList(event) {
    this.Id_Department = event;
    this.loadDataList();
  }
}
