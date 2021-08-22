import { SortState } from './../../../../../_metronic/shared/crud-table/models/sort.model';
import { PaginatorState } from './../../../../../_metronic/shared/crud-table/models/paginator.model';
import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { QueryParamsModel } from './../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { LayoutUtilsService, MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { SubheaderService } from './../../../../../_metronic/partials/layout/subheader/_services/subheader.service';
import { TokenStorage } from './../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { MatDialog, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog'; 
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, Inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
// Material
import {  MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { tap, distinctUntilChanged, debounceTime } from 'rxjs/operators';
import { fromEvent, merge, BehaviorSubject } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
// Services
// Models
import { UserModel, GroupNameModel, UserAddData } from '../Model/userright.model';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { isFulfilled } from 'q';
// import { LeaveRegistrationEditComponent } from '../leave-registration-edit/leave-registration-edit.component';
import { DatePipe } from '@angular/common';
import { PermissionService } from '../Services/userright.service';
import { UserRightDataSource } from '../Model/data-sources/userright.datasource';

@Component({
	selector: 'kt-danh-sach-nguoi-dung-them-moi',
	templateUrl: './danh-sach-nguoi-dung-them-moi.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class DanhSachNguoiDungThemMoiComponent implements OnInit {
	// Table fields
	dataSource: UserRightDataSource;
	displayedColumns = ['actions', 'Username', 'HoTen', 'ChucVu'];
	paginatorNew: PaginatorState = new PaginatorState();
	// @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
	sorting: SortState = new SortState();
	// Filter fields
	filterDonVi: string = '';
	filterPhongBan: string = '';
	filterChucDanh: string = '';
	@ViewChild('searchInputHoTen',{ static: true }) searchInputHoTen: ElementRef;
	listDonVi: any[] = [];
	listPhongBan: any[] = [];
	listChucDanh: any[] = [];
	//Form
	disabledBtn: boolean = false;
	loadingSubject = new BehaviorSubject<boolean>(false);
	loadingControl = new BehaviorSubject<boolean>(false);
	loading1$ = this.loadingSubject.asObservable();
	hasFormErrors: boolean = false;
	selectedTab: number = 0;
	luu: boolean = true;
	capnhat: boolean = false;
	ID_NV: string = '';
	// Selection
	productsResult: any[] = [];
	item: GroupNameModel;

	public datatree: BehaviorSubject<any[]> = new BehaviorSubject([]);
	title: string = '';
	selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
	ID_Struct: string = '';
	constructor(
		@Inject(MAT_DIALOG_DATA) public data: any,
		public dialogRef: MatDialogRef<DanhSachNguoiDungThemMoiComponent>,
		private userRightService: PermissionService,
		private danhMucChungService: DanhMucChungService,
		public dialog: MatDialog,
		public datepipe: DatePipe,
		public subheaderService: SubheaderService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private tokenStorage: TokenStorage) { }

	/** LOAD DATA */
	ngOnInit() {
		this.title = this.translate.instant("GeneralKey.choncocautochuc");
		this.item = this.data._item;
		fromEvent(this.searchInputHoTen.nativeElement, 'keyup')
		.pipe(
			debounceTime(150),
			distinctUntilChanged(),
			tap(() => {
				this.paginatorNew.page = 0;
				this.loadDataList();
			})
		)
		.subscribe();
		this.dataSource = new UserRightDataSource(this.userRightService);
		this.dataSource.paginatorTotal$.subscribe(res => this.paginatorNew.total = res);
		this.dataSource.entitySubject.subscribe(res => this.productsResult = res);
		this.getTreeValue();
		this.loadDataList();

		setTimeout(() => {
			this.dataSource.loading$ = new BehaviorSubject<boolean>(false);
		}, 10000);
	}

	getTreeValue() {
		this.danhMucChungService.Get_CoCauToChuc().subscribe(res => {
			if (res.data && res.data.length > 0) {
				this.datatree.next(res.data);
				this.loadListChucVu();
			}
		});
	}

	GetValueNode(val: any) {
		this.ID_Struct = val.RowID;
		this.danhMucChungService.GetListPositionbyStructure_All(this.ID_Struct).subscribe(res => {
			this.listChucDanh = res.data;
			this.filterChucDanh = "-1";
			this.loadDataList();
		});
	}

	loadListChucVu() {
		this.danhMucChungService.GetListPositionbyStructure_All(this.ID_Struct).subscribe(res => {
			this.listChucDanh = res.data;
		});
	}


	//---------------------------------------------------------
	loadDataList(page: boolean = false) {
		const queryParams = new QueryParamsModel(
			this.filterConfiguration(),
			this.sorting.direction,
			this.sorting.column,
			this.paginatorNew.page - 1,
			this.paginatorNew.pageSize
		);
		this.dataSource.loadList_NguoiDungHeThong(queryParams);
		setTimeout(x => {
			this.loadPage();
		}, 500)
	}
	loadPage() {
		var arrayData = [];
		this.dataSource.entitySubject.subscribe(res => arrayData = res);
		if (arrayData !== undefined && arrayData.length == 0) {
			var totalRecord = 0;
			this.dataSource.paginatorTotal$.subscribe(tt => totalRecord = tt)
			if (totalRecord > 0) {
				const queryParams1 = new QueryParamsModel(
					this.filterConfiguration(),
					this.sorting.direction,
					this.sorting.column,
					this.paginatorNew.page - 1,
					this.paginatorNew.pageSize
				);
				this.dataSource.loadList_NguoiDungHeThong(queryParams1);
			}
			else {
				const queryParams1 = new QueryParamsModel(
					this.filterConfiguration(),
					this.sorting.direction,
					this.sorting.column,
					this.paginatorNew.page = 0,
					this.paginatorNew.pageSize
				);
				this.dataSource.loadList_NguoiDungHeThong(queryParams1);
			}
		}
	}
	/** FILTRATION */
	filterConfiguration(): any {
		const filter: any = {};
		const searchText1: string = this.searchInputHoTen.nativeElement.value;
		filter.StructureID = '' + this.ID_Struct;
		if (this.filterChucDanh && this.filterChucDanh != "-1") {
			filter.IDChucDanh = this.filterChucDanh;
		}
		filter.ID_Nhom = this.item.ID_Nhom;
		filter.HoTen = searchText1;
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


	loadPhongBan(id: any) {
		this.danhMucChungService.GetListDepartmentbyBranch(id).subscribe(res => {
			this.listPhongBan = res.data;
			if (this.listPhongBan.length > 0) {
				this.loadDataList();
			}
			else {
				this.filterPhongBan = "";
				this.loadDataList();
			}

		});
	}

	loadChucDanh(id: any) {
		this.danhMucChungService.GetListPositionbyDepartment(id).subscribe(res => {
			this.listChucDanh = res.data;
			if (this.listChucDanh.length > 0) {
				this.loadDataList();
			}
			else {
				this.filterChucDanh = "";
				this.loadDataList();
			}
		});
	}

	goBack() {
		this.dialogRef.close();
	}
	//=========================Chuyển Popup===========

	checkedChange(checked: boolean, items: any) {
		const q = new UserAddData();
		q.ID_Nhom = this.item.ID_Nhom;
		q.UserName = items.Username;
		this.updateDanhSachNhom(q, false);
	}
	LuuDanhSachNhom() {
		this.dialogRef.close();
	}


	updateDanhSachNhom(_product: UserAddData, withBack: boolean = false) {
		this.loadingSubject.next(true);
		this.userRightService.UpdateDanhSachNhom(_product).subscribe(res => {
			this.loadingSubject.next(false);
			if (res && res.status === 1) {
				if (withBack) {
				} else {
					this.loadDataList();
				}
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
			}
		});
	}

	//==================================
	getComponentTitle() {
		let result = this.translate.instant('phanquyen.themnguoidungvaonhom') + " " + this.item.TenNhom;
		if (!this.item || !this.item.ID_Nhom) {
			return result;
		}

		return result;
	}

}
