import { AuthorizeEditComponent } from './../authorize-edit/authorize-edit.component';
import { MessageType } from './../../../../_metronic/jeework_old/core/_base/crud/utils/layout-utils.service';
import { AuthorizeModel } from './../Model/user.model';
import { QueryParamsModelNew } from "./../../../../_metronic/jeework_old/core/models/query-models/query-params.model";
import { BehaviorSubject } from "rxjs";
import { TokenStorage } from "src/app/_metronic/jeework_old/core/auth/_services";
import { WeWorkService } from "./../../services/wework.services";
import { TranslateService } from "@ngx-translate/core";
import { LayoutUtilsService } from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { DanhMucChungService } from "./../../../../_metronic/jeework_old/core/services/danhmuc.service";
import { UserService } from "./../Services/user.service";
import { PaginatorState } from "./../../../../_metronic/shared/crud-table/models/paginator.model";
import { DepartmentModel } from "./../../List-department/Model/List-department.model";
import { SelectionModel } from "@angular/cdk/collections";
import { SortState } from "./../../../../_metronic/shared/crud-table/models/sort.model";
import { Router, ActivatedRoute } from "@angular/router";
import { Component, OnInit } from "@angular/core";
import { UyQuyenDataSource } from "../data-sources/uyquyen.datasource";
import { MatDialog } from "@angular/material/dialog";

@Component({
  selector: "app-uyquyen",
  templateUrl: "./uyquyen.component.html",
  styleUrls: ["./uyquyen.component.scss"],
})
export class UyquyenComponent implements OnInit {
  UserID = 0;
  // Table fields
  dataSource: UyQuyenDataSource;
  displayedColumns = ["hoten", "listproject", "thoigian", "createddate", "actions"];
  sorting: SortState = new SortState();

  // Selection
  selection = new SelectionModel<DepartmentModel>(true, []);
  productsResult: DepartmentModel[] = [];
  id_menu: number = 60702;
  //=================PageSize Table=====================
  pageSize: number;
  flag: boolean = true;
  keyword: string = "";
  customStyle: any = {};
  paginatorNew: PaginatorState = new PaginatorState();
  constructor(
    private router: Router,
    public service: UserService,
    private danhMucService: DanhMucChungService,
    public dialog: MatDialog,
    private route: ActivatedRoute,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    private tokenStorage: TokenStorage,
    public WeWorkService: WeWorkService
  ) { }

  ngOnInit(): void {
    this.UserID = +this.router.url.split("/")[2];

    this.tokenStorage.getPageSize().subscribe((res) => {
      this.pageSize = +res;
    });

    this.dataSource = new UyQuyenDataSource(this.service);
    this.dataSource.entitySubject.subscribe(
      (res) => (this.productsResult = res)
    );
    // this.layoutUtilsService.setUpPaginationLabels(this.paginator);
    this.dataSource.paginatorTotal$.subscribe(
      (res) => (this.paginatorNew.total = res)
    );
    this.loadDataList();
    setTimeout(() => {
      this.dataSource.loading$ = new BehaviorSubject<boolean>(false);
    }, 10000);
  }
  loadDataList() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      this.sorting.direction,
      this.sorting.column,
      this.paginatorNew.page - 1,
      this.paginatorNew.pageSize
    );
    this.dataSource.loadList(queryParams);
    setTimeout((x) => {
      this.loadPage();
    }, 500);
  }
  filterConfiguration(): any {
    let filter: any = {};
    // if (this.keyword)
    // 	filter.keyword = this.keyword;
    return filter;
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
        this.dataSource.loadList(queryParams1);
      } else {
        const queryParams1 = new QueryParamsModelNew(
          this.filterConfiguration(),
          this.sorting.direction,
          this.sorting.column,
          this.paginatorNew.page = 0,
          this.paginatorNew.pageSize
        );
        this.dataSource.loadList(queryParams1);
      }
    }
  }

  paginate(paginator: PaginatorState) {
    this.loadDataList();
  }

  getHeight(): any {
    let tmp_height = 0;
    tmp_height = window.innerHeight - 220 - this.tokenStorage.getHeightHeader();
    return tmp_height + "px";
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

  uyquyen(item) {
    // this.service.DetailUQ(item.id_row).subscribe(res => {
    // 	console.log(res);
    // });
    let saveMessageTranslateParam = '';
    var _item = new AuthorizeModel();
    _item.clear();
    if (item)
      _item = item;
    saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(AuthorizeEditComponent, { data: { _item } });
    dialogRef.afterClosed().subscribe(res => {
      if (!res) {
        this.ngOnInit();
      }
      else {
        this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
        this.ngOnInit();
      }
    });
  }
  xoauyquyen(item) {
    this.layoutUtilsService.showWaitingDiv();
    this.service.delete(item.id_row).subscribe(res => {
      this.layoutUtilsService.OffWaitingDiv();
      if (res && res.status === 1) {
        this.loadDataList();
      }
      else {
        this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
      }
    });
  }

}
