import { SortState } from './../../../../../_metronic/shared/crud-table/models/sort.model';
import { PaginatorState } from './../../../../../_metronic/shared/crud-table/models/paginator.model';
import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { QueryParamsModel } from './../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { LayoutUtilsService, MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { SubheaderService } from './../../../../../_metronic/partials/layout/subheader/_services/subheader.service';
import { TokenStorage } from './../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { tap, distinctUntilChanged, debounceTime } from 'rxjs/operators';
import { fromEvent, merge, BehaviorSubject } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
// Services
// Models
import { UserModel } from '../Model/userright.model';
import { FormGroup, FormBuilder, Validators } from '@angular/forms';
import { isFulfilled } from 'q';
// import { LeaveRegistrationEditComponent } from '../leave-registration-edit/leave-registration-edit.component';
import { DatePipe } from '@angular/common';
import { PermissionService } from '../Services/userright.service';
import { UserRightDataSource } from '../Model/data-sources/userright.datasource';
import { FunctionsGroupListComponent } from '../functions-group/functions-group-list.component';
//import { RewardDisciplineEditComponent } from '../reward-discipline-edit/reward-discipline-edit.component';


@Component({
	selector: 'kt-users-list',
	templateUrl: './users-list.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class UsersListComponent implements OnInit {
	// Table fields
	dataSource: UserRightDataSource;
	displayedColumns = ['ID_NV', 'HoTen', 'Username', 'CCTC', 'ChucDanh', 'actions'];
	@ViewChild('searchInputHoTen',{ static: true }) searchInputHoTen: ElementRef;
	sorting: SortState = new SortState() ;
	// Filter fields
	filterDonVi: string = '';
	filterPhongBan: string = '';
	filterChucDanh: string = '';

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
	paginatorNew: PaginatorState = new PaginatorState();

	productsResult: UserModel[] = [];

	public datatree: BehaviorSubject<any[]> = new BehaviorSubject([]);
	title: string = '';
	selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
	ID_Struct: string = '';
	filter: any = {};
	constructor(
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

		this.title = this.translate.instant("GeneralKey.choncocautochuc");

		this.getTreeValue();

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
		// Init DataSource
		this.dataSource = new UserRightDataSource(this.userRightService);
		this.dataSource.paginatorTotal$.subscribe(res => this.paginatorNew.total = res)
		this.dataSource.entitySubject.subscribe(res => this.productsResult = res);
		this.loadDataList();

		setTimeout(() => {
			this.dataSource.loading$ = new BehaviorSubject<boolean>(false);
		}, 10000);
	}
	getTreeValue() {
		this.danhMucChungService.Get_CoCauToChuc().subscribe(res => {
			if (res.data && res.data.length > 0) {
				this.datatree.next(res.data);
			}
		});
	}
	GetValueNode(val: any) {
		this.ID_Struct = val.RowID;
		this.danhMucChungService.GetListPositionbyStructure_All(this.ID_Struct).subscribe(res => {
			this.listChucDanh = res.data;
			if (this.listChucDanh.length > 0) {
				this.loadDataList();
			}
			else {
				this.filterChucDanh = "";
				this.loadDataList();
			}
		});
		this.loadDataList();
	}
	loadDataList(page: boolean = false) {
		const queryParams = new QueryParamsModel(
			this.filterConfiguration(),
			this.sorting.direction,
			this.sorting.column,
			this.paginatorNew.page - 1,
			this.paginatorNew.pageSize
		);
		this.dataSource.loadListUsers(queryParams);
		// setTimeout(x => {
		// 	this.loadPage();
		// }, 500)
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
				this.dataSource.loadListUsers(queryParams1);
			}
			else {
				const queryParams1 = new QueryParamsModel(
					this.filterConfiguration(),
					this.sorting.direction,
					this.sorting.column,
					this.paginatorNew.page = 0,
					this.paginatorNew.pageSize
				);
				this.dataSource.loadListGroupName(queryParams1);
			}
		}
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
	/** FILTRATION */
	filterConfiguration(): any {
		const filter: any = {};
		const searchText1: string = this.searchInputHoTen.nativeElement.value;
		filter.StructureID = '' + this.ID_Struct;

		if (this.filterChucDanh && this.filterChucDanh != "-1") {
			filter.IDChucDanh = this.filterChucDanh;
		}
		filter.HoTen = searchText1;
		return filter;
	}
	//=========================Chuyển Popup===========
	
	PhanQuyen(_item: UserModel) {
		let saveMessageTranslateParam = '';
		saveMessageTranslateParam += _item.ID_NV > 0 ? this.translate.instant('GeneralKey.capnhatthanhcong') : this.translate.instant('GeneralKey.themthanhcong');
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.ID_NV > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(FunctionsGroupListComponent, { data: { _item, IsGroup: false }, height: '70%'});
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.loadDataList();
			}
			else
			{
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.loadDataList();
			}
		});
		
	}
	/*Download file */
	//----------Hàm kiểm tra input------------------
	checkDate(v: any, row: any, index: any, col: string) {
		if (v.data == null) {
			this.dataSource.entitySubject.value[index]["cssClass"][col] = "";
			this.dataSource.entitySubject.value[index][col] = v.target.value;
		}
		else {
			if (v.data == "-") {
				this.dataSource.entitySubject.value[index]["cssClass"][col] = "inp-error";
				return
			}
			else {
				this.dataSource.entitySubject.value[index]["cssClass"][col] = "";
				this.dataSource.entitySubject.value[index][col] = v.target.value;
			}
		}

	}
	f_number(value: any) {
		return Number((value + '').replace(/,/g, ""));
	}

	f_currency(value: any, args?: any): any {
		let nbr = Number((value + '').replace(/,|-/g, ""));
		return (nbr + '').replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,");
	}
	textPres(e: any, vi: any) {
		if (isNaN(e.key)
			//&& e.keyCode != 8 // backspace
			//&& e.keyCode != 46 // delete
			&& e.keyCode != 32 // space
			&& e.keyCode != 189
			&& e.keyCode != 45
		) {// -
			e.preventDefault();
		}
	}
	text(e: any, vi: any) {
		if (!((e.keyCode > 95 && e.keyCode < 106)
			|| (e.keyCode > 45 && e.keyCode < 58)
			|| e.keyCode == 8)) {
			e.preventDefault();
		}
	}
	textNam(e: any, vi: any) {
		if (!((e.keyCode > 95 && e.keyCode < 106)
			|| (e.keyCode > 47 && e.keyCode < 58)
			|| e.keyCode == 8)) {
			e.preventDefault();
		}
	}
	f_date(value: any, args?: any): any {
		let latest_date = this.datepipe.transform(value, 'dd/MM/yyyy');
		return latest_date;
	}
	getHeight(): any {
		let tmp_height = 0;
		tmp_height = window.innerHeight - 263 -this.tokenStorage.getHeightHeader();
		return tmp_height + 'px';
	}

}
