import { SortState } from './../../../../_metronic/shared/crud-table/models/sort.model';
import { PaginatorState } from './../../../../_metronic/shared/crud-table/models/paginator.model';
import { MessageType, LayoutUtilsService } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { milestoneDetailEditComponent } from './../milestone-detail-edit/milestone-detail-edit.component';
import { MilestoneModel } from './../../projects-team/Model/department-and-project.model';
import { JeeWorkLiteService } from './../../services/wework.services';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, Input, SimpleChange, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { merge, BehaviorSubject } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
// Services
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
// Models
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { DepartmentModel } from '../Model/List-department.model';
import { DepartmentDataSource } from '../Model/data-sources/List-department.datasource';
import { ListDepartmentService } from '../Services/List-department.service';

@Component({
	selector: 'kt-tab-muc-tieu',
	templateUrl: './tab-muc-tieu.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})

export class TabMucTieuComponent implements OnInit {
	// Table fields
	dataSource: DepartmentDataSource;
	// displayedColumns = ['TaskName', 'ProcessName', 'Description', 'CreatedBy', 'CreatedDate', 'DownFile', 'actions'];
	displayedColumns = ['CreatedDate', 'title', 'project_team', 'Status', 'hoten'];

	// Filter fields
	listchucdanh: any[] = [];
	// Selection
	selection = new SelectionModel<DepartmentModel>(true, []);
	productsResult: DepartmentModel[] = [];
	showTruyCapNhanh: boolean = true;
	sorting: SortState = new SortState();
	//=================PageSize Table=====================
	paginatorNew: PaginatorState = new PaginatorState();
	pageSize: number;
	customStyle: any = {};
	// ID_QuyTrinh: number = 10003;
	// TenQuyTrinh: string = 'Pad Trần Văn';
	Id_Department: any = 0;
	@Input() ID_QuyTrinh: any = 0;
	@Input() TenQuyTrinh: any;
	@Input() WorkID: any;
	@Input() Values: any;
	flag: boolean = true;
	constructor(public deptService: ListDepartmentService,
		public WeWorkService: JeeWorkLiteService,
		public dialog: MatDialog,
		private router: Router,
		private route: ActivatedRoute,
		private changeDetectorRefs: ChangeDetectorRef,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private tokenStorage: TokenStorage,) { }


	ngOnInit() {
		var path = this.router.url;
		if (path) {
			var arr = path.split('/');
			if (arr.length > 2) {
				this.ID_QuyTrinh = +arr[2];
				this.Id_Department = +arr[2];

			}
		}
		if (this.Id_Department > 0) {
			this.LoadDataFolder();
		}
		this.Load();
		setTimeout(() => {
			this.dataSource.loading$ = new BehaviorSubject<boolean>(false);
		}, 10000);
	}
	/** LOAD DATA */
	Load() {
		this.tokenStorage.getPageSize().subscribe(res => {
			this.pageSize = +res;
		});
		// If the user changes the sort order, reset back to the first page.

		this.dataSource = new DepartmentDataSource(this.deptService);
		let queryParams = new QueryParamsModelNew({});

		this.dataSource.paginatorTotal$.subscribe(res => this.paginatorNew.total = res)
		this.dataSource.entitySubject.subscribe(res => this.productsResult = res);
		// this.layoutUtilsService.setUpPaginationLabels(this.paginator);
		this.loadDataList();
	}
	DrawPie(point: number) {
		if (point <= 50)
			return 'pie per-25';
		else if (point <= 75)
			return 'pie per-50';
		else if (point < 100)
			return 'pie per-75';
		else return 'pie per-100';
	}
	loadDataList() {

		const queryParams = new QueryParamsModelNew(
			this.filterConfiguration(),
			this.sorting.direction,
			this.sorting.column,
			this.paginatorNew.page - 1,
			this.paginatorNew.pageSize
		);
		this.dataSource.loadListMilestone(queryParams);
		this.dataSource.entitySubject.subscribe(res => {
			if (res.length > 0) {

			}
			else {
			}
		})
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
				const queryParams1 = new QueryParamsModelNew(
					this.filterConfiguration(),
					this.sorting.direction,
					this.sorting.column,
					this.paginatorNew.page - 1,
					this.paginatorNew.pageSize
				);
				this.dataSource.loadListMilestone(queryParams1);
			}
			else {
				const queryParams1 = new QueryParamsModelNew(
					this.filterConfiguration(),
					this.sorting.direction,
					this.sorting.column,
					this.paginatorNew.page = 0,
					this.paginatorNew.pageSize
				);
				this.dataSource.loadListMilestone(queryParams1);
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
	getItemStatusString(status: number = 0): string {
		switch (status) {
			case 0:
				return 'Selling';
			case 1:
				return 'Sold';
		}
		return '';
	}
	getItemCssClassByLocked(status: number = 0): string {
		switch (status) {
			case 0:
				return 'success';
			case 1:
				return 'info';
		}
		return '';
	}
	getItemLockedString(condition: number = 0): string {
		switch (condition) {
			case 1:
				return 'ACTIVE';
			case 0:
				return 'LOCKED';
		}
		return '';
	}

	/**
	 * Returens item CSS Class Name by status
	 *
	 * @param status: number
	 */
	getItemCssClassByStatus(status: number = 0): string {
		switch (status) {
			case 0:
				return 'success';
			case 1:
				return 'info';
		}
		return '';
	}
	getColorProgressbar(status: number = 0): string {

		// switch (status) {
		// 	case  > 50:
		// 		return 'success';
		// 	case 1:
		// 		return 'info';
		// }
		// return 'warn';
		if (status < 50)
			return 'warn';
		else
			if (status < 100)
				return 'info';
			else
				return 'success';
	}
	/**
	 * Returns item condition
	 *
	 * @param condition: number
	 */
	getItemConditionString(condition: number = 0): string {
		switch (condition) {
			case 1:
				return this.translate.instant('projects.dungtiendo');
			case 3:
				return this.translate.instant('projects.ruirocao');
		}
		return this.translate.instant('projects.chamtiendo');
	}

	/**
	 * Returns CSS Class name by condition
	 *
	 * @param condition: number
	 */
	getItemCssClassByCondition(condition: number = 0): string {
		switch (condition) {
			case 1:
				return 'success';
			case 2:
				return 'info';
		}
		return 'warn';
	}
	filterConfiguration(): any {

		const filter: any = {};
		filter.id_department = this.ID_QuyTrinh;
		return filter;
	}

	XuatFile(item: any) {
		var linkdownload = item.Link;
		window.open(linkdownload);

	}

	/** Delete */
	Delete(_item: DepartmentModel) {
		// const _title = this.translate.instant('GeneralKey.xoa');
		// const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
		// const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		// const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');

		// const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		// dialogRef.afterClosed().subscribe(res => {
		// 	if (!res) {
		// 		return;
		// 	}

		// 	this.deptService.Delete_WorkProcess(_item.RowID).subscribe(res => {
		// 		if (res && res.status === 1) {
		// 			this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
		// 		}
		// 		else {
		// 			this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
		// 		}
		// 		this.loadDataList();
		// 	});
		// });
	}
	Add() {
		const ProcessWorkModels = new DepartmentModel();
		ProcessWorkModels.clear(); // Set all defaults fields
		this.Update(ProcessWorkModels);
	}

	Update(_item: DepartmentModel) {
		// let saveMessageTranslateParam = '';
		// saveMessageTranslateParam += _item.RowID > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		// const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		// const _messageType = _item.RowID > 0 ? MessageType.Update : MessageType.Create;
		// const dialogRef = this.dialog.open(ProcessWorkEditComponent, { data: { _item, _type: 0 }, height: '70%', width: '50%' });
		// dialogRef.afterClosed().subscribe(res => {
		// 	if (!res) {
		// 		this.loadDataList();
		// 	}
		// 	else {
		// 		this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
		// 		this.loadDataList();
		// 	}
		// });
	}

	getHeight(): any {
		let tmp_height = 0;
		tmp_height = window.innerHeight - 200 - this.tokenStorage.getHeightHeader();//286
		return tmp_height + 'px';
	}


	AddMileston() {


		let saveMessageTranslateParam = '';
		var _item = new MilestoneModel;
		_item.clear();
		_item.id_department = this.ID_QuyTrinh;
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(milestoneDetailEditComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				// this.ngOnInit();
				return;
			}
			else {
				this.loadDataList();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				// this.changeDetectorRefs.detectChanges();
			}
		});
	}

	dataFolder: any = [];
	loadListfolder = false;
	LoadDataFolder() {
		this.deptService.DeptDetail(this.Id_Department).subscribe(res => {
			if (res && res.status == 1) {
				if (!res.data.ParentID) {
					this.dataFolder = res.data.data_folder;
					var itemhientai = {
						CreatedBy: res.data.CreatedBy,
						CreatedDate: res.data.CreatedDate,
						id_row: res.data.id_row,
						parentid: res.data.ParentID,
						templateid: res.data.templateid,
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
		this.ID_QuyTrinh = event;
		this.loadDataList();
	}
}
