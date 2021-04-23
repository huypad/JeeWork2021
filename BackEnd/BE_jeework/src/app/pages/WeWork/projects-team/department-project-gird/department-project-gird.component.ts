import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, SimpleChange, Output, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { fromEvent, merge, BehaviorSubject } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { FormGroup, FormBuilder } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';

import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';

import { DepartmentModel } from '../../List-department/Model/List-department.model';
import { ProjectTeamEditComponent } from '../project-team-edit/project-team-edit.component';
import { ProjectsTeamService } from '../Services/department-and-project.service';
import { DepartmentProjectDataSource } from '../Model/data-sources/department-and-project.datasource';
// Services
// Models

@Component({
	selector: 'kt-department-project-gird',
	templateUrl: './department-project-gird.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})

export class DepartmentProjecGirdComponent implements OnInit {
	@ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
	dataSource: DepartmentProjectDataSource;
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
	listproject: any[] = [];
	listquydinh: any[] = [];
	showTruyCapNhanh: boolean = true;
	TenQuyTrinh: string = 'Pad Trần Văn';
	filter: any = {};
	thongbao: any = {};
	// ShowHead: boolean = false;
	// ShowHead: boolean;
	@Input() ShowHead: any = {};
	//[=======================================================]
	constructor(
		private _service: ProjectsTeamService,
		private activatedRoute: ActivatedRoute,
		public dialog: MatDialog,
		private route: ActivatedRoute,
		private itemFB: FormBuilder,
		public subheaderService: SubheaderService,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private translate: TranslateService,
		public datepipe: DatePipe,
		private tokenStorage: TokenStorage,
		private danhMucChungService: DanhMucChungService,
	) { }
	// ngOnChanges(changes: SimpleChange) {
	// 	if (changes['filter']) {
	// 		
	// 		if (changes['filter']) {
	// 			this.filter = changes['filter'].currentValue;
	// 		}
	// 		this.loadDataList();
	// 	}
	// }
	/** LOAD DATA */
	ngOnInit() {
		
		var a = this.ShowHead;
		this.loadingSubject.next(true);
		this.reset();
		this.dataSource = new DepartmentProjectDataSource(this._service);
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
		if (obj) {
			let tmp_height = 0;
			tmp_height = window.innerHeight - 354-this.tokenStorage.getHeightHeader();
			return tmp_height + 'px';
		} else {
			let tmp_height = 0;
			tmp_height = window.innerHeight - 120-this.tokenStorage.getHeightHeader();
			return tmp_height + 'px';
		}
	}
	Add() {
		const ObjectModels = new DepartmentModel();
		ObjectModels.clear(); // Set all defaults fields
		this.Update(ObjectModels);
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

		this.dataSource.loadListProject(queryParams);
		this.dataSource.entitySubject.subscribe(res => {

			if (res.length > 0) {

				this.listproject = res;
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
		const dialogRef = this.dialog.open(ProjectTeamEditComponent, { data: { _item, _IsEdit: _item.IsEdit } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.loadDataList();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.changeDetectorRefs.detectChanges();
			}
		});
	}
}
