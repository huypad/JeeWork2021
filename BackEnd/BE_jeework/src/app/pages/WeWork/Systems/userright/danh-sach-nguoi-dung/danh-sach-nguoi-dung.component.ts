import { SortState } from './../../../../../_metronic/shared/crud-table/models/sort.model';
import { PaginatorState } from './../../../../../_metronic/shared/crud-table/models/paginator.model';
import { SubheaderService } from './../../../../../_metronic/partials/layout/subheader/_services/subheader.service';
import { TokenStorage } from './../../../../../_metronic/jeework_old/core/services/token-storage.service';
import { LayoutUtilsService, MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { QueryParamsModel } from './../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, Inject } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
// Material
import { MatDialog, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSort } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
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
import { DanhSachNguoiDungThemMoiComponent } from '../danh-sach-nguoi-dung-them-moi/danh-sach-nguoi-dung-them-moi.component';
@Component({
	selector: 'kt-danh-sach-nguoi-dung',
	templateUrl: './danh-sach-nguoi-dung.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class DanhSachNguoiDungComponent implements OnInit {
	// Table fields
	dataSource: UserRightDataSource;
	dataSource1: UserRightDataSource;
	displayedColumns = ['Username', 'HoTen', 'ChucDanh', 'actions'];
	paginatorNew: PaginatorState = new PaginatorState();
	sorting: SortState = new SortState();

	// Filter fields
	filterDonVi: string = '';
	filterPhongBan: string = '';
	filterChucDanh: string = '';
	disabledBtn: boolean = false;

	listDonVi: any[] = [];
	listPhongBan: any[] = [];
	listChucDanh: any[] = [];
	//Form

	loadingSubject = new BehaviorSubject<boolean>(false);
	loadingControl = new BehaviorSubject<boolean>(false);
	loading1$ = this.loadingSubject.asObservable();
	hasFormErrors: boolean = false;
	selectedTab: number = 0;
	luu: boolean = true;
	capnhat: boolean = false;
	ID_NV: string = '';
	// Selection
	productsResult: UserModel[] = [];
	item: GroupNameModel;
	constructor(
		@Inject(MAT_DIALOG_DATA) public data: any,
		public dialogRef: MatDialogRef<DanhSachNguoiDungComponent>,
		private userRightService: PermissionService,
		private danhMucChungService: DanhMucChungService,
		public dialog: MatDialog,
		public datepipe: DatePipe,
		private route: ActivatedRoute,
		private itemFB: FormBuilder,
		public subheaderService: SubheaderService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private tokenStorage: TokenStorage) { }

	/** LOAD DATA */
	ngOnInit() {

		this.item = this.data._item; 
		// this.dataSource = new UserRightDataSource(this.userRightService);
		// this.dataSource.entitySubject.subscribe(res => this.productsResult = res);
		this.dataSource1 = new UserRightDataSource(this.userRightService);
		this.dataSource1.entitySubject.subscribe(res => this.productsResult = res);
		this.dataSource1.paginatorTotal$.subscribe(res => this.paginatorNew.total = res);
		this.loadDataList();
	}
	//---------------------------------------------------------
	loadDataList() {
		const queryParams1 = new QueryParamsModel(
			this.filterConfiguration1(),
			this.sorting.direction,
			this.sorting.column,
			this.paginatorNew.page - 1,
			this.paginatorNew.pageSize
		);
		this.dataSource1.loadList_NguoiDungNhom(queryParams1);
		setTimeout(x => {
			this.loadPage();
		}, 500)
	}
	loadPage() {
		var arrayData = [];
		this.dataSource1.entitySubject.subscribe(res => arrayData = res);
		if (arrayData && arrayData.length == 0) {
			var totalRecord = 0;
			this.dataSource1.paginatorTotal$.subscribe(tt => totalRecord = tt)
			if (totalRecord > 0) {
				const queryParams1 = new QueryParamsModel(
					this.filterConfiguration1(),
					this.sorting.direction,
					this.sorting.column,
					this.paginatorNew.page - 1,
					this.paginatorNew.pageSize
				);
				this.dataSource1.loadList_NguoiDungNhom(queryParams1);
			}
			else {
				const queryParams1 = new QueryParamsModel(
					this.filterConfiguration1(),
					this.sorting.direction,
					this.sorting.column,
					this.paginatorNew.page = 0,
					this.paginatorNew.pageSize
				);
				this.dataSource1.loadList_NguoiDungNhom(queryParams1);
			}
		}
	}
	/** FILTRATION */
	filterConfiguration1(): any {
		const filter: any = {};
		filter.ID_Nhom = this.item.ID_Nhom;
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

	goBack() {
		this.dialogRef.close();
	}

	AddUsers() {
		let _item = this.item;
		let saveMessageTranslateParam = '';
		saveMessageTranslateParam += this.translate.instant('GeneralKey.themthanhcong');
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = MessageType.Create;
		const dialogRef = this.dialog.open(DanhSachNguoiDungThemMoiComponent, { data: { _item }, height: '70%' });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.loadDataList();
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.loadDataList();
			}
		});
	}
	//=========================Chuyển Popup===========

	Delete(item: any) {

		if (this.userRightService.VisibleNDN == true) {
			const q = new UserAddData();
			q.ID_Nhom = this.item.ID_Nhom;
			q.UserName = item.Username;
			this.deleteItem(q);
		}
	}
	deleteItem(_item: UserAddData) {
		this.userRightService.deleteDanhSachNhom(_item).subscribe(res => {
			if (res && res.status === 1) {
				this.loadDataList();
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 10000, true, false, 3000, 'top', 0);
			}
		});
	}

	//==================================
	getComponentTitle() {
		let result = this.translate.instant('phanquyen.danhsachnguoidung') + " " + this.item.TenNhom;
		if (!this.item || !this.item.ID_Nhom) {
			return result;
		}

		return result;
	}
}
