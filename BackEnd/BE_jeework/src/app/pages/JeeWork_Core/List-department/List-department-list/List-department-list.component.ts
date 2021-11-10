import { DepartmentEditNewComponent } from './../department-edit-new/department-edit-new.component';
import { MenuAsideService } from './../../../../_metronic/jeework_old/core/_base/layout/services/menu-aside.service';
import { CommonService } from './../../../../_metronic/jeework_old/core/services/common.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { fromEvent, merge, BehaviorSubject } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { FormGroup, FormBuilder } from '@angular/forms';
import { ListDepartmentService } from '../Services/List-department.service';
import { DatePipe } from '@angular/common';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';

import { DepartmentEditComponent } from '../List-department-edit/List-department-edit.component';
import { DepartmentModel } from '../Model/List-department.model';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { DepartmentDataSource } from '../Model/data-sources/List-department.datasource';
// Services
// Models

@Component({
	selector: 'kt-List-department-list',
	templateUrl: './List-department-list.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})

export class ListDepartmentListComponent implements OnInit {
	@ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
	dataSource: DepartmentDataSource;
	@ViewChild(MatSort, { static: true }) sort: MatSort;
	itemForm: FormGroup;
	loadingSubject = new BehaviorSubject<boolean>(false);
	loadingControl = new BehaviorSubject<boolean>(false);
	loading1$ = this.loadingSubject.asObservable();
	hasFormErrors: boolean = false;
	selectedTab: number = 0;
	hoten: string = '';
	// Selection
	ID_NV: string = '';

	//===============Khai báo value chi tiêt==================
	item: DepartmentModel;
	oldItem: DepartmentModel;
	//================Danh sách trình độ ngoại ngữ========================================
	itemTrinhDo: any;
	showBtNV: boolean = false;
	listQuytrinh: any[] = [];
	listThongBao: any[] = [];
	listDept: any[] = [];
	listquydinh: any[] = [];
	showTruyCapNhanh: boolean = true;
	@ViewChild("tenphongban", { static: true }) tenphongban: ElementRef;

	//[=======================================================]
	constructor(
		private thongTinCaNhanService: ListDepartmentService,
		private activatedRoute: ActivatedRoute,
		public dialog: MatDialog,
		private route: ActivatedRoute,
		private itemFB: FormBuilder,
		public subheaderService: SubheaderService,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private translate: TranslateService,
		public datepipe: DatePipe,
		public tokenStorage: TokenStorage,
		public commonService: CommonService,
		public menuAsideService: MenuAsideService,
	) { }

	/** LOAD DATA */
	ngOnInit() {
		this.loadingSubject.next(true);
		this.reset();
		this.dataSource = new DepartmentDataSource(this.thongTinCaNhanService);
		this.loadDataList();
		this.changeDetectorRefs.detectChanges();
	}
	reset() {
		this.item = Object.assign({}, this.oldItem);
		this.createForm();
		this.hasFormErrors = false;
		this.itemForm.markAsPristine();
		this.itemForm.markAsUntouched();
		this.itemForm.updateValueAndValidity();
	}

	initProduct() {
		this.createForm();
		this.loadingSubject.next(false);
		this.loadingControl.next(true);
	}
	createForm() {
		this.itemForm = this.itemFB.group({

		});
	}
	getRandomColor() {
		var color = Math.floor(0x1000000 * Math.random()).toString(16);
		return '#' + ('000000' + color).slice(-6);
	}
	goBack() {
		window.history.back();
	}
	//---------------------------------------------------------
	/** FILTRATION */
	filterConfiguration(): any {
		const filter: any = {};
		filter.keyword = this.tenphongban.nativeElement.value;
		return filter;
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
	checkDate(e: any, vi: any) {
		if (!((e.keyCode > 95 && e.keyCode < 106)
			|| (e.keyCode > 46 && e.keyCode < 58)
			|| e.keyCode == 8)) {
			e.preventDefault();
		}
	}
	checkValue(e: any, vi: any) {
		if (!((e.keyCode > 95 && e.keyCode < 106)
			|| (e.keyCode > 47 && e.keyCode < 58)
			|| e.keyCode == 8)) {
			e.preventDefault();
		}
	}
	f_convertDate(v: any) {
		if (v != "") {
			let a = new Date(v);
			return a.getFullYear() + "-" + ("0" + (a.getMonth() + 1)).slice(-2) + "-" + ("0" + (a.getDate())).slice(-2) + "T00:00:00.0000000";
		}
	}

	f_date(value: any, args?: any): any {
		let latest_date = this.datepipe.transform(value, 'dd/MM/yyyy');
		return latest_date;
	}
	buildStyle(_weight) {
		return (_weight && _weight.Is_Read == true) ? '' : 'bold';
	}
	buildColor(_color) {
		return (_color && _color.Is_Read == true) ? '#500050' : '#15c';
	}
	getHeight(): any {
		let obj = window.location.href.split("/").find(x => x == "tabs-references");
		let tmp_height = 0;
		if (obj) {
			tmp_height = window.innerHeight - 214;
		} else {
			tmp_height = window.innerHeight - 60;
		}

		return tmp_height - this.tokenStorage.getHeightHeader() + 'px';
	}
	Add(ID_Department = 0) {
		const ObjectModels = new DepartmentModel();
		ObjectModels.clear(); // Set all defaults fields
		if (ID_Department > 0) {
			ObjectModels.RowID = ID_Department;
			this.Update(ObjectModels);
		} else {
			this.Create(ObjectModels);
		}
	}
	applyFilter(text: string) {

		this.loadDataList();
		// this.dataSource4.filter = filterValue.trim().toLowerCase();
	}
	loadDataList() {
		const queryParams = new QueryParamsModelNew(
			this.filterConfiguration(),
			'',
			'',
			0,
			50,
			true
		);

		this.dataSource.loadList(queryParams);
		this.dataSource.entitySubject.subscribe(res => {
			if (res) {
				this.listDept = res;
				this.changeDetectorRefs.detectChanges();
			}
			else {
			}
		})
	}
	Update(_item: DepartmentModel) {
		let saveMessageTranslateParam = '';
		saveMessageTranslateParam += _item.RowID > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.RowID > 0 ? MessageType.Update : MessageType.Create;
		const IsUpdate = _item.RowID > 0 ? true : false;
		// const dialogRef = this.dialog.open(DepartmentEditNewComponent, { data: { _item, _IsEdit: _item.IsEdit } });
		const dialogRef = this.dialog.open(DepartmentEditNewComponent, {
			// minHeight: '50vh',
			data: { _item, _IsEdit: _item.IsEdit, IsUpdate },
			minWidth: '650px',
		});
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.loadDataList();
				this.menuAsideService.loadMenu();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.changeDetectorRefs.detectChanges();
			}
		});
	}
	Create(_item: DepartmentModel) {
		let saveMessageTranslateParam = '';
		saveMessageTranslateParam += _item.RowID > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.RowID > 0 ? MessageType.Update : MessageType.Create;

		const dialogRef = this.dialog.open(DepartmentEditNewComponent, { data: { _item, _IsEdit: _item.IsEdit } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.loadDataList();
				this.menuAsideService.loadMenu();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.changeDetectorRefs.detectChanges();
			}
		});
	}
	Delete(ID_Department) {
		var ObjectModels = new DepartmentModel();
		ObjectModels.clear();
		const _title = this.translate.instant('GeneralKey.xoa');
		const _description = this.translate.instant('department.confirmxoa');
		const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
		const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			this.thongTinCaNhanService.Delete_Dept(ID_Department).subscribe(res => {
				if (res && res.status === 1) {
					this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
					this.menuAsideService.loadMenu();
					this.ngOnInit();
				}
				else {
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
					this.ngOnInit();
				}
			});
		});
	}
}
