import { SortState } from "./../../../../../_metronic/shared/crud-table/models/sort.model";
import { PaginatorState } from "./../../../../../_metronic/shared/crud-table/models/paginator.model";
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { QueryParamsModel } from "./../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model";
import { DanhMucChungService } from "./../../../../../_metronic/jeework_old/core/services/danhmuc.service";
//Cores
import {
  Component,
  OnInit,
  ViewChild,
  ElementRef,
  Inject,
  ChangeDetectorRef,
} from "@angular/core";
import { TranslateService } from "@ngx-translate/core";
// Materials
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
import { MatPaginator } from "@angular/material/paginator";
import { MatSort } from "@angular/material/sort";
// Services
// Models
//Datasources
// RXJS
import { merge, BehaviorSubject } from "rxjs";
import { tap } from "rxjs/operators";
import { PermissionService } from "../Services/userright.service";
import { UserRightDataSource } from "../Model/data-sources/userright.datasource";
import { SelectionModel } from "@angular/cdk/collections";
import { QuyenAddData, UserModel } from "../Model/userright.model";

@Component({
  selector: "kt-chucnanguser-list",
  templateUrl: "./chucnanguser-list.component.html",
})
export class ChucNangUserListComponent implements OnInit {
  item: UserModel;
  dataSource: UserRightDataSource;
  displayedColumns = [];
  availableColumns = [
    {
      stt: 1,
      name: "Id_Quyen",
      alwaysChecked: false,
    },
    {
      stt: 2,
      name: "Tenquyen",
      alwaysChecked: false,
    },
    {
      stt: 3,
      name: "ChinhSua",
      alwaysChecked: false,
    },
    {
      stt: 4,
      name: "ChiXem",
      alwaysChecked: false,
    },
  ];
  selectedColumns = new SelectionModel<any>(true, this.availableColumns);
  hasFormErrors: boolean = false;
  viewLoading: boolean = false;
  loadingAfterSubmit: boolean = false;
  listChucNang: any[] = [];
  filterChucNang: string = "";
  // Filter fields
  sorting: SortState = new SortState();

  selectedValue: any;

  paginatorNew: PaginatorState = new PaginatorState();
  selection = new SelectionModel<QuyenAddData>(true, []);
  selection1 = new SelectionModel<QuyenAddData>(true, []);
  productsResult: any[] = [];
  listQuyen: any[] = [];
  disabledBtn: boolean = false;
  loadingSubject = new BehaviorSubject<boolean>(false);
  //=======================================================
  disthEdit: boolean = false;
  disthRead: boolean = false;
  Edit: boolean = false;
  Read: boolean = false;
  selectedTab: number = 0;
  module: string = "wework";
  constructor(
    public dialogRef: MatDialogRef<ChucNangUserListComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    public userRightService: PermissionService,
    private danhMucService: DanhMucChungService,
    private layoutUtilsService: LayoutUtilsService,
    private changeDetectorRefs: ChangeDetectorRef,
    private translate: TranslateService
  ) {}

  /** LOAD DATA */
  ngOnInit() {
    this.applySelectedColumns();
    this.item = this.data._item;
    this.dataSource = new UserRightDataSource(this.userRightService);
    if (this.selectedTab > 0) this.module = "'wework'";
    else this.module = "'wework','wework'";
    //Load unit list
    // this.danhMucService.GetListModuleWeWork(this.module).subscribe(res => {
    // 	this.listChucNang = res.data;
    // 	this.filterChucNang = '' + res.data[0].ID_Row;
    // 	this.dataSource.entitySubject.subscribe(val => this.productsResult = val);
    // 	this.loadDataList();
    // });
    this.dataSource.paginatorTotal$.subscribe(
      (res) => (this.paginatorNew.total = res)
    );
    this.loadDataList();
  }
  onLinkClick() {}
  /** FILTRATION */
  filterConfiguration(): any {
    const filter: any = {};
    // if (this.filterChucNang && this.filterChucNang.length > 0) {
    // 	filter.ID_NhomChucNang = +this.filterChucNang;
    // }
    filter.ID_NhomChucNang = "1";
    if (this.item.TenDangNhap && this.item.TenDangNhap.length > 0) {
      filter.ID_User = this.item.TenDangNhap;
    }
    return filter;
  }

  /** ACTIONS */
  loadDataList() {
    this.Edit = this.Read = false;
    const queryParams = new QueryParamsModel(
      this.filterConfiguration(),
      this.sorting.direction,
      this.sorting.column,
      this.paginatorNew.page - 1,
      this.paginatorNew.pageSize
    );
    this.dataSource.LoadListFunctions(queryParams);
    this.dataSource.entitySubject.subscribe((res) => {
      if (res.length > 0) {
        this.disthEdit = this.disthRead = false;
        res.map((item, index) => {
          if (item.IsEdit_Enable) {
            this.disthEdit = true;
          }
          if (item.IsRead_Enable && item.IsReadPermit) {
            this.disthRead = true;
          }
        });
      }
    });
  }

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
  //chọn cán bộ

  onAlertClose($event) {
    this.hasFormErrors = false;
  }

  close() {
    this.dialogRef.close();
  }

  /** SELECTION */
  isAllSelected() {
    const numSelected = this.selection.selected.length;
    const numRows = this.productsResult.length;
    return numSelected === numRows;
  }

  /** Selects all rows if they are not all selected; otherwise clear selection. */
  masterToggle(val: any) {
    if (val.checked) {
      this.productsResult.forEach((row) => {
        if (row.IsRead_Enable == true && row.IsReadPermit == true) {
          row.IsRead = true;
        } else {
          row.IsRead = false;
        }
      });
    } else {
      this.productsResult.forEach((row) => {
        if (row.IsRead_Enable == true) {
          row.IsRead = false;
        }
      });
    }
  }

  /** SELECTION */
  isAllSelected1() {
    const numSelected = this.selection1.selected.length;
    const numRows = this.productsResult.length;
    return numSelected === numRows;
  }

  /** Selects all rows if they are not all selected; otherwise clear selection. */
  masterToggle1(val: any) {
    if (val.checked) {
      this.productsResult.forEach((row) => {
        if (row.IsEdit_Enable == true) {
          row.IsEdit = true;
        }
      });
    } else {
      this.productsResult.forEach((row) => {
        if (row.IsEdit_Enable == true) {
          row.IsEdit = false;
        }
      });
    }
  }

  // IsAllColumnsChecked() {
  //
  // 	const numSelected = this.selectedColumns.selected.length;
  // 	const numRows = this.availableColumns.length;
  // 	return numSelected === numRows;
  // }
  // CheckAllColumns() {
  //
  // 	if (this.IsAllColumnsChecked()) {
  // 		this.availableColumns.forEach(row => { if (!row.alwaysChecked) this.selectedColumns.deselect(row); });
  // 	} else {
  // 		this.availableColumns.forEach(row => this.selectedColumns.select(row));
  // 	}
  // }

  applySelectedColumns() {
    const _selectedColumns: string[] = [];
    this.selectedColumns.selected
      .sort((a, b) => {
        return a.stt > b.stt ? 1 : 0;
      })
      .forEach((col) => {
        _selectedColumns.push(col.name);
      });
    this.displayedColumns = _selectedColumns;
  }

  goBack() {
    this.dialogRef.close();
  }

  getComponentTitle() {
    let result =
      this.translate.instant("phanquyen.hanquyennguoidung") +
      " " +
      this.item.HoTen;
    if (!this.item || !this.item.ID_NV) {
      return result;
    }

    return result;
  }
  //=================================================================================================
  changeChinhSua(val: any, row: any) {
    this.productsResult.map((item, index) => {
      if (item.Id_Quyen == row.Id_Quyen) {
        item.IsEdit = val.checked;
      }
    });
  }
  changeChiXem(val: any, row: any) {
    this.productsResult.map((item, index) => {
      if (item.Id_Quyen == row.Id_Quyen) {
        item.IsRead = val.checked;
      }
    });
  }
  luuQuyen(withBack: boolean = false) {
    this.listQuyen = [];
    this.productsResult.forEach((row) => {
      const q = new QuyenAddData();
      q.ID = this.item.TenDangNhap;
      q.ID_NhomChucNang = +this.filterChucNang;
      q.ID_Quyen = row.Id_Quyen;
      q.IsEdit = row.IsEdit;
      q.IsRead = row.IsRead;
      q.TenQuyen = row.TenQuyen;
      q.Ten = this.item.HoTen;
      this.listQuyen.push(q);
    });
    if (this.listQuyen.length > 0) {
      this.updateNhomUser(this.listQuyen, withBack);
    }
  }
  updateNhomUser(_product: any[], withBack: boolean = false) {
    this.loadingSubject.next(true);
    this.disabledBtn = true;
    this.userRightService.UpdateNhomUser(_product).subscribe((res) => {
      this.loadingSubject.next(false);
      this.disabledBtn = false;
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        if (withBack) {
        } else {
          const _messageType = this.translate.instant(
            "GeneralKey.themthanhcong"
          );
          this.layoutUtilsService.showActionNotification(
            _messageType,
            MessageType.Update,
            4000,
            true,
            false
          );
          this.loadDataList();
        }
      } else {
        this.layoutUtilsService.showActionNotification(
          res.error.message,
          MessageType.Read,
          99999999999,
          true,
          false,
          3000,
          "top",
          0
        );
      }
    });
  }
}
