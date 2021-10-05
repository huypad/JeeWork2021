import { SortState } from './../../../../../_metronic/shared/crud-table/models/sort.model';
import { PaginatorState } from './../../../../../_metronic/shared/crud-table/models/paginator.model';
import { LayoutUtilsService, MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { QueryParamsModel } from './../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
//Cores
import { Component, OnInit, ViewChild, ElementRef, Inject, ChangeDetectorRef } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
// Materials
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
// Services
// Models
//Datasources
// RXJS
import { merge, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { PermissionService } from '../Services/userright.service';
import { UserRightDataSource } from '../Model/data-sources/userright.datasource';
import { SelectionModel } from '@angular/cdk/collections';
import { GroupNameModel, QuyenAddData } from '../Model/userright.model';


@Component({
	selector: 'kt-functions-group-list',
	templateUrl: './functions-group-list.component.html',
})
export class FunctionsGroupListComponent implements OnInit {
	item: GroupNameModel;
	dataSource: UserRightDataSource;
	dataSource_setting: UserRightDataSource;

	displayedColumns = [];
	availableColumns = [
		{
			stt: 1,
			name: 'Id_Quyen',
			alwaysChecked: false,
		},
		{
			stt: 1,
			name: 'Tenquyen',
			alwaysChecked: false,
		},
		{
			stt: 2,
			name: 'ChinhSua',
			alwaysChecked: false,
		},
		{
			stt: 3,
			name: 'ChiXem',
			alwaysChecked: false,
		},
	];
	selectedColumns = new SelectionModel<any>(true, this.availableColumns);
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	listChucNang: any[] = [];
	listsetting: any[] = [];

	filterChucNang: string = '';
	filtersetting: string = '';

	paginatorNew: PaginatorState = new PaginatorState();
	// Filter fields
	// @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
	sorting: SortState = new SortState();
	selectedValue: any;
	selectedTab: number = 0;
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
	module: string = 'webadmin';
	IsGroup: boolean;
	Title: string = '';
	ColumnKey: string;
	ValueKey: string;
	constructor(public dialogRef: MatDialogRef<FunctionsGroupListComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		public userRightService: PermissionService,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private translate: TranslateService,) { }

	/** LOAD DATA */
	ngOnInit() {
		this.applySelectedColumns();
		this.item = this.data._item;
		debugger
		this.IsGroup = this.data.IsGroup;
		if (this.IsGroup) {
			this.Title = this.data._item.TenNhom;
			this.ColumnKey = 'id_group';
			this.ValueKey = this.data._item.ID_Nhom;
		}
		else {
			this.Title = this.data._item.HoTen;
			this.ColumnKey = 'username';
			this.ValueKey = this.data._item.Username;
		}

		// this.dataSource.paginatorTotal$.subscribe(res => this.paginatorNew.total = res)
		this.dataSource = new UserRightDataSource(this.userRightService);
		this.loadDataList();
	}
	onLinkClick() {

	}

	/** FILTRATION */
	filterConfiguration(): any {
		const filter: any = {};
		// if (this.filterChucNang && this.filterChucNang.length > 0) {
		// 	filter.ID_NhomChucNang = this.filterChucNang;
		// }
		filter.ID_NhomChucNang = '1';
		filter[this.ColumnKey] = this.ValueKey;
		filter.Type = this.IsGroup;
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
		this.dataSource.entitySubject.subscribe(res => {
			if (res.length > 0) {
				this.productsResult = res;
				this.disthEdit = this.disthRead = false;
				res.map((item, index) => {
					if (item.IsEdit_Enable) {
						this.disthEdit = true;
					}
					if (item.IsRead_Enable && item.IsReadPermit) {
						debugger
						this.disthRead = true;
					}
				})
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
	//chọn nhân viên
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
			this.productsResult.forEach(row => {
				if (row.IsRead_Enable == true && row.IsReadPermit == true) {
					row.IsRead = true;
				} else {
					row.IsRead = false;
				}
			});
		}
		else {
			this.productsResult.forEach(row => {
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
		debugger
		// if (val.checked) {
		// 	this.productsResult.forEach(row => {
		// 		if (row.IsEdit_Enable == true) {
		// 			row.IsEdit = true;
		// 		}
		// 	});
		// }
		// else {
		// 	this.productsResult.forEach(row => {
		// 		if (row.IsEdit_Enable == true) {
		// 			row.IsEdit = false;
		// 		}
		// 	});
		// }
		if (val.checked) {
			this.productsResult.forEach(row => {
				if (row.IsRead_Enable == true && row.IsReadPermit == true) {
					row.IsRead = true;
				} else {
					row.IsRead = false;
				}
			});
		}
		else {
			this.productsResult.forEach(row => {
				if (row.IsRead_Enable == true) {
					row.IsRead = false;
				}
			});
		}
	}
	applySelectedColumns() {
		const _selectedColumns: string[] = [];
		this.selectedColumns.selected.sort((a, b) => { return a.stt > b.stt ? 1 : 0; }).forEach(col => { _selectedColumns.push(col.name) });
		this.displayedColumns = _selectedColumns;
	}

	goBack() {
		this.dialogRef.close();
	}

	getComponentTitle() {
		let result = this.translate.instant('phanquyen.phanquyennhomnguoidung') + " " + this.item.TenNhom;
		if (!this.item || !this.item.ID_Nhom) {
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
	luuQuyen(withBack: boolean = true) {

		this.listQuyen = [];
		this.productsResult.forEach(row => {
			const q = new QuyenAddData();
			q.ID = this.ValueKey;
			q.ID_NhomChucNang = +this.filterChucNang;
			q.ID_Quyen = row.Id_Quyen;
			q.IsEdit = row.IsEdit;
			q.IsRead = row.IsRead;
			q.IsGroup = this.IsGroup;
			q.TenQuyen = row.TenQuyen;
			q.Ten = this.item.TenNhom;
			this.listQuyen.push(q);
		});
		if (this.listQuyen.length > 0) {
			this.updateNhomNguoiDung(this.listQuyen, withBack);
		}
	}
	updateNhomNguoiDung(_product: any[], withBack: boolean = true) {
		this.loadingSubject.next(true);
		this.disabledBtn = true;

		this.userRightService.UpdatePermision(_product).subscribe(res => {
			this.loadingSubject.next(false);

			this.disabledBtn = false;
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				// if (withBack) {
				this.dialogRef.close({
					_product
				});
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 99999999999, true, false, 3000, 'top', 0);
			}
		});
	}
}
