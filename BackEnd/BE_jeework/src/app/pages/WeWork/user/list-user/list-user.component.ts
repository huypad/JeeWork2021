import { SortState } from './../../../../_metronic/shared/crud-table/models/sort.model';
import { PaginatorState } from './../../../../_metronic/shared/crud-table/models/paginator.model';
import { LayoutUtilsService } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { WeWorkService } from './../../services/wework.services';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, Input, SimpleChange, OnChanges } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { BehaviorSubject, fromEvent, merge } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
// Services
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
// Models
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { DepartmentModel } from '../../List-department/Model/List-department.model';
import { UserService } from '../Services/user.service';
import { UserDataSource } from '../data-sources/user.datasource';

@Component({
	selector: 'kt-list-user',
	templateUrl: './list-user.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})

export class ListUserComponent implements OnInit, OnChanges {
	// Table fields
	dataSource: UserDataSource;
	displayedColumns = ['hoten', 'email', 'manangers', 'actions'];
	sorting: SortState = new SortState() ;
	
	// Selection
	selection = new SelectionModel<DepartmentModel>(true, []);
	productsResult: DepartmentModel[] = [];
	id_menu: number = 60702;
	//=================PageSize Table=====================
	pageSize: number;
	flag: boolean = true;
	keyword: string = '';
	customStyle:any = {};
	paginatorNew: PaginatorState = new PaginatorState();
	constructor(public service: UserService,
		private danhMucService: DanhMucChungService,
		public dialog: MatDialog,
		private route: ActivatedRoute,
		private router: Router,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private tokenStorage: TokenStorage,
		public WeWorkService: WeWorkService,
		) {
	}
	ngOnInit() {
		this.tokenStorage.getPageSize().subscribe(res => {
			this.pageSize = +res;
		});
		
		this.dataSource = new UserDataSource(this.service);
		this.dataSource.entitySubject.subscribe(res => this.productsResult = res);
		// this.layoutUtilsService.setUpPaginationLabels(this.paginator);
		this.dataSource.paginatorTotal$.subscribe(res => this.paginatorNew.total = res)
		this.loadDataList();
		setTimeout(() => {
			this.dataSource.loading$ = new BehaviorSubject<boolean>(false);
		}, 10000);
	}

	ngOnChanges() {
		if (this.dataSource)
			this.loadDataList();
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
		setTimeout(x => {
			this.loadPage();
		}, 500)
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
		this.loadDataList();
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
				this.dataSource.loadList(queryParams1);
			}
			else {
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
		if (status < 50)
			return 'metal';
		else
			if (status < 100)
				return 'brand';
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
	getItemCssClassByCondition(condition: number = 0): string {
		switch (condition) {
			case 1:
				return 'success';
			case 2:
				return 'brand';
		}
		return 'metal';
	}
	filterConfiguration(): any {
		let filter: any = {};
		if (this.keyword)
			filter.keyword = this.keyword;
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

	// getHeight(): any {
	// 	let tmp_height = 0;
	// 	tmp_height = window.innerHeight - 175; // 286
	// 	return tmp_height + 'px';
	// }
	getHeight(): any {
		let obj = window.location.href.split("/").find(x => x == "wework");
		if (obj) {
			let tmp_height = 0;
			tmp_height = window.innerHeight - 197 -this.tokenStorage.getHeightHeader();
			return tmp_height + 'px';
		} else {
			let tmp_height = 0;
			tmp_height = window.innerHeight - 140 -this.tokenStorage.getHeightHeader();
			return tmp_height + 'px';
		}
	}
	quickEdit(item) {
		this.layoutUtilsService.showActionNotification("Updating");
	}
	updateStage(item) {
		this.layoutUtilsService.showActionNotification("Updating");
	}
}
