import { SortState } from "./../../../../../_metronic/shared/crud-table/models/sort.model";
import { PaginatorState } from "./../../../../../_metronic/shared/crud-table/models/paginator.model";
import { TokenStorage } from "./../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service";
import { DanhMucChungService } from "./../../../../../_metronic/jeework_old/core/services/danhmuc.service";
import { GroupNameModel } from "./../Model/userright.model";
import { QueryParamsModel } from "./../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model";
import {
  MessageType,
  LayoutUtilsService,
} from "./../../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { SubheaderService } from "./../../../../../_metronic/partials/layout/subheader/_services/subheader.service";
import {
  Component,
  OnInit,
  ElementRef,
  ViewChild,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
} from "@angular/core";
import { ActivatedRoute } from "@angular/router";
// Material
import { MatDialog } from "@angular/material/dialog";
import { MatPaginator } from "@angular/material/paginator";
import { MatSort } from "@angular/material/sort";
import { SelectionModel } from "@angular/cdk/collections";
// RXJS
import { tap } from "rxjs/operators";
import { fromEvent, merge, BehaviorSubject } from "rxjs";
import { TranslateService } from "@ngx-translate/core";
// Services
// Models
import { FormGroup, FormBuilder, Validators } from "@angular/forms";
import { isFulfilled } from "q";
// import { LeaveRegistrationEditComponent } from '../leave-registration-edit/leave-registration-edit.component';
import { DatePipe } from "@angular/common";
import { PermissionService } from "../Services/userright.service";
import { UserRightDataSource } from "../Model/data-sources/userright.datasource";
import { GroupNameEditComponent } from "../groupname-edit/groupname-edit.component";
import { FunctionsGroupListComponent } from "../functions-group/functions-group-list.component";
import { UserGroupPermitComponent } from "../user-group-permit/user-group-permit.component";
@Component({
  selector: "kt-groupname-list",
  templateUrl: "./groupname-list.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupNameListComponent implements OnInit {
  // Table fields
  dataSource: UserRightDataSource;
  displayedColumns = ["ID_Nhom", "TenNhom", "DateCreated", "actions"];
  sorting: SortState = new SortState();

  //Form
  itemForm: FormGroup;
  loadingSubject = new BehaviorSubject<boolean>(false);
  loadingControl = new BehaviorSubject<boolean>(false);
  loading1$ = this.loadingSubject.asObservable();
  hasFormErrors: boolean = false;
  selectedTab: number = 0;
  luu: boolean = true;
  Visable: boolean = false;
  capnhat: boolean = false;
  ID_NV: string = "";
  paginatorNew: PaginatorState = new PaginatorState();
  // Selection
  productsResult: GroupNameModel[] = [];
  showTruyCapNhanh: boolean = true;
  constructor(
    public permitService: PermissionService,
    public dialog: MatDialog,
    public datepipe: DatePipe,
    private route: ActivatedRoute,
    private itemFB: FormBuilder,
    public subheaderService: SubheaderService,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    private tokenStorage: TokenStorage,
    private changeDetectorRefs: ChangeDetectorRef,
    private danhMucChungService: DanhMucChungService
  ) { }

  /** LOAD DATA */
  ngOnInit() {
    this.dataSource = new UserRightDataSource(this.permitService);
    this.dataSource.entitySubject.subscribe(
      (res) => (this.productsResult = res)
    );
    this.loadDataList();

    this.dataSource.paginatorTotal$.subscribe(
      (res) => (this.paginatorNew.total = res)
    );
    this.Visable = this.permitService.Visible_Group;
    this.changeDetectorRefs.detectChanges();

    setTimeout(() => {
      this.dataSource.loading$ = new BehaviorSubject<boolean>(false);
    }, 10000);
  }

  getTitle(): string {
    return this.translate.instant("phanquyen.tieude1");
  }
  //---------------------------------------------------------
  loadDataList() {
    const queryParams = new QueryParamsModel(
      this.filterConfiguration(),
      this.sorting.direction,
      this.sorting.column,
      this.paginatorNew.page - 1,
      this.paginatorNew.pageSize
    );
    this.dataSource.LoadGroup(queryParams);
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
      this.paginatorNew;
      if (totalRecord > 0) {
        const queryParams1 = new QueryParamsModel(
          this.filterConfiguration(),
          this.sorting.direction,
          this.sorting.column,
          this.paginatorNew.page - 1,
          this.paginatorNew.pageSize
        );
        this.dataSource.LoadGroup(queryParams1);
      } else {
        const queryParams1 = new QueryParamsModel(
          this.filterConfiguration(),
          this.sorting.direction,
          this.sorting.column,
          this.paginatorNew.page = 0,
          this.paginatorNew.pageSize
        );
        this.dataSource.LoadGroup(queryParams1);
      }
    }
  }
  /** FILTRATION */
  filterConfiguration(): any {
    const filter: any = {};
    filter.ID_NV = this.ID_NV;
    filter.Module = "0";
    return filter;
  }

  /** ACTIONS */
  paginate(paginator: PaginatorState) {
    this.loadDataList();
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

  //=========================Chuyển Popup===========

  PhanQuyen(_item: GroupNameModel) {
    let saveMessageTranslateParam = "";
    saveMessageTranslateParam +=
      _item.ID_Nhom > 0
        ? this.translate.instant("GeneralKey.capnhatthanhcong")
        : this.translate.instant("GeneralKey.themthanhcong");
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.ID_Nhom > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(FunctionsGroupListComponent, {
      data: { _item, IsGroup: true },
      height: "70%",
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        this.loadDataList();
      } else {
        this.layoutUtilsService.showActionNotification(
          _saveMessage,
          _messageType,
          4000,
          true,
          false
        );
        this.loadDataList();
      }
    });
  }

  DanhSachNguoiDung(_item: GroupNameModel) {
    let saveMessageTranslateParam = "";
    saveMessageTranslateParam +=
      _item.ID_Nhom > 0
        ? this.translate.instant("GeneralKey.capnhatthanhcong")
        : this.translate.instant("GeneralKey.themthanhcong");
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.ID_Nhom > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(UserGroupPermitComponent, {
      data: { _item },
      width: '1000px',
      height: "70%",
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        this.loadDataList();
      } else {
        this.layoutUtilsService.showActionNotification(
          _saveMessage,
          _messageType,
          4000,
          true,
          false
        );
        this.loadDataList();
      }
    });
  }
  /** Delete */
  deleteItem(_item: GroupNameModel) {
    const _title = this.translate.instant("GeneralKey.xoa");
    const _description = this.translate.instant(
      "GeneralKey.bancochacchanmuonxoakhong"
    );
    const _waitDesciption = this.translate.instant(
      "GeneralKey.dulieudangduocxoa"
    );
    const _deleteMessage = this.translate.instant("GeneralKey.xoathanhcong");

    const dialogRef = this.layoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      }

      this.permitService
        .deleteItemNhomNguoiDung(_item.ID_Nhom, _item.TenNhom)
        .subscribe((res) => {
          if (res && res.status === 1) {
            this.layoutUtilsService.showActionNotification(
              _deleteMessage,
              MessageType.Delete,
              4000,
              true,
              false
            );
          } else {
            this.layoutUtilsService.showActionNotification(
              res.error.message,
              MessageType.Read,
              10000,
              true,
              false,
              3000,
              "top",
              0
            );
          }
          this.loadDataList();
        });
    });
  }
  //========================Thêm mới nhóm người dùng=======================

  ThemNhom() {
    const _model = new GroupNameModel();
    _model.clear(); // Set all defaults fields
    this.AddNewGroup(_model);
  }
  AddNewGroup(_item: GroupNameModel) {
    let saveMessageTranslateParam = "";
    saveMessageTranslateParam +=
      _item.ID_Nhom > 0
        ? this.translate.instant("GeneralKey.capnhatthanhcong")
        : this.translate.instant("GeneralKey.themthanhcong");
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.ID_Nhom > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(GroupNameEditComponent, {
      data: { _item },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        this.loadDataList();
      } else {
        this.layoutUtilsService.showActionNotification(
          _saveMessage,
          _messageType,
          4000,
          true,
          false
        );
        this.loadDataList();
      }
    });
  }
  //----------Hàm kiểm tra input------------------
  checkDate(v: any, row: any, index: any, col: string) {
    if (v.data == null) {
      this.dataSource.entitySubject.value[index]["cssClass"][col] = "";
      this.dataSource.entitySubject.value[index][col] = v.target.value;
    } else {
      if (v.data == "-") {
        this.dataSource.entitySubject.value[index]["cssClass"][col] =
          "inp-error";
        return;
      } else {
        this.dataSource.entitySubject.value[index]["cssClass"][col] = "";
        this.dataSource.entitySubject.value[index][col] = v.target.value;
      }
    }
  }
  f_number(value: any) {
    return Number((value + "").replace(/,/g, ""));
  }

  f_currency(value: any, args?: any): any {
    let nbr = Number((value + "").replace(/,|-/g, ""));
    return (nbr + "").replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,");
  }
  textPres(e: any, vi: any) {
    if (
      isNaN(e.key) &&
      //&& e.keyCode != 8 // backspace
      //&& e.keyCode != 46 // delete
      e.keyCode != 32 && // space
      e.keyCode != 189 &&
      e.keyCode != 45
    ) {
      // -
      e.preventDefault();
    }
  }
  text(e: any, vi: any) {
    if (
      !(
        (e.keyCode > 95 && e.keyCode < 106) ||
        (e.keyCode > 45 && e.keyCode < 58) ||
        e.keyCode == 8
      )
    ) {
      e.preventDefault();
    }
  }
  textNam(e: any, vi: any) {
    if (
      !(
        (e.keyCode > 95 && e.keyCode < 106) ||
        (e.keyCode > 47 && e.keyCode < 58) ||
        e.keyCode == 8
      )
    ) {
      e.preventDefault();
    }
  }
  f_date(value: any, args?: any): any {
    let latest_date = this.datepipe.transform(value, "dd/MM/yyyy");
    return latest_date;
  }
  //==========================================================
  getHeight(): any {
    let tmp_height = 0;
    tmp_height = window.innerHeight - 300 - this.tokenStorage.getHeightHeader();
    return tmp_height + "px";
  }
}
