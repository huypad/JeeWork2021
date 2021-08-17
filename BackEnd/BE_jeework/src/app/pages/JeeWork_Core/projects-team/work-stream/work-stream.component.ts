import { locale } from './../../../../modules/i18n/vocabs/vi';
import { WeWorkService } from './../../services/wework.services';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Injectable, Input } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
// Material
import { MatSort } from '@angular/material/sort';
import { MatPaginator,MatPaginatorIntl } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatTreeFlattener, MatTreeFlatDataSource } from '@angular/material/tree';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { BehaviorSubject, Observable, of as observableOf } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { FormGroup, FormBuilder } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';

import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';

import { DepartmentModel } from '../../List-department/Model/List-department.model';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';import { ProjectTeamEditComponent } from '../project-team-edit/project-team-edit.component';
import { ProjectsTeamService } from '../Services/department-and-project.service';
import { WorkEditPageComponent } from '../../work/work-edit-page/work-edit-page.component';
import { WorkGroupModel, WorkModel } from '../../work/work.model';
import { WorkGroupEditComponent } from '../../work/work-group-edit/work-group-edit.component';
import { WorkEditDialogComponent } from '../../work/work-edit-dialog/work-edit-dialog.component';
@Component({
	selector: 'kt-work-stream',
	templateUrl: './work-stream.component.html',
	styleUrls: ['./work-stream.component.scss'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkStreamComponent implements OnInit {
	ChildComponentInstance: any;
	data: any[] = [];
	selectedItem: any = undefined;
	childComponentType: any = WorkEditPageComponent;
	childComponentData: any = {};

	@ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
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
	id_menu: number = 40640;
	ID_QuyTrinh: number = 10003;
	TenQuyTrinh: string = 'Pad Trần Văn';
	@Input() ID_Project: number = 0;
	keyword: string = "";
	filterStage: any;
	filtermoicapnhat: any;
	filtersubtask: any;
	filtermilestone: string = "";
	filterassign: string = "";
	listUser: any[] = [];
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
		private weworkService: WeWorkService, 
	) {
	}

	loadData() {
		const queryParams = new QueryParamsModelNew(
			this.filterConfiguration(),
			'',
			'',
			0,
			50,
			true
		);
		this.data = [];
		queryParams.sortField = this.filtermoicapnhat.value;
		this._service.findStreamView(queryParams).subscribe(res => {
			this.layoutUtilsService.showWaitingDiv();
			if (res && res.status === 1) {
				this.data = res.data;
				// this.layoutUtilsService.OffWaitingDiv();
			}
			setTimeout(() => {
				this.layoutUtilsService.OffWaitingDiv();
				this.changeDetectorRefs.detectChanges();
			}, 3000);
		});
	}
	/** LOAD DATA */
	ngOnInit() {

		this.weworkService.list_account({}).subscribe(res => {
			if (res && res.status == 1)
				this.listUser = res.data;
		});

		this.filterStage = this._ListTilterTrangthai[0];
		this.filtermoicapnhat = this._listFilterMoicapnhap[0];
		this.filtersubtask = this._listFilterSubtask[0];
		this.loadData();
		this.reset();
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
		filter.keyword = this.keyword;
		filter.id_project_team = this.ID_Project;

		let stage = '';
		if (this.filterStage.value) {
			stage = this.filterStage.value;
		}
		if (stage == 'status') {
			filter[stage] = this.filterStage.id_row;
		} else {
			if (this.filterStage.type == 1) {
				filter[stage] = 1;
			}
			else {
				filter[stage] = 'True';
			}
		}

		filter.displayChild = this.filtersubtask.value;
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
	Add() {
		const ObjectModels = new WorkModel();
		ObjectModels.clear(); // Set all defaults fields
		this.Update(ObjectModels);
	}
	open() {
		const dialogRef = this.dialog.open(WorkEditDialogComponent, { data: { DATA: { Id: 1 } } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
		});
	}
	Update(_item: WorkModel) {
		let saveMessageTranslateParam = '';
		_item.id_project_team = this.ID_Project;
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(WorkEditDialogComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.changeDetectorRefs.detectChanges();
			}
		});
	}
	AddWorkGroup() {
		const ObjectModels = new WorkGroupModel();
		ObjectModels.clear(); // Set all defaults fields
		this.UpdateWorkGroup(ObjectModels);
	}
	UpdateWorkGroup(_item: WorkGroupModel) {
		let saveMessageTranslateParam = '';
		_item.id_project_team = '' + this.ID_Project;

		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(WorkGroupEditComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.ngOnInit();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.changeDetectorRefs.detectChanges();
			}
		});
	}
	selected($event) {
		this.selectedItem = $event;
		let temp: any = {};
		temp.Id = this.selectedItem.id_row;
		this.childComponentData.DATA = this.selectedItem;
		if (this.ChildComponentInstance != undefined)
			this.ChildComponentInstance.ngOnChanges();

			// this.selectedItem = $event;
			// var url = this.router.url + '?id='+this.selectedItem.id_row;
			// this.router.navigateByUrl(url).then( () => {
			// 	let temp: any = {};
			// 	temp.Id = this.selectedItem.id_row;
			// 	this.childComponentData.DATA = this.selectedItem;
			// 	if (this.ChildComponentInstance != undefined)
			// 		this.ChildComponentInstance.ngOnChanges();
			// } )
	}
	close_detail() {
		this.selectedItem = undefined;
	}
	getInstance($event) {
		this.ChildComponentInstance = $event;
	}

	closeDetail(event) {

		this.selectedItem = undefined;
	}

	_filter = locale.data.filter;
	_ListTilterTrangthai = [
		{
			title: this.translate.instant('filter.tatcatrangthai'),
			value: 'all',
			loai: 'trangthai'
		},
		{
			title: this.translate.instant('filter.dangthuchien'),
			value: 'status',
			id_row: 1,
			loai: 'trangthai'
		},
		{
			title: this.translate.instant('filter.phailam'),
			value: 'require',
			loai: 'trangthai',
			type: 1
		},
		{
			title: this.translate.instant('filter.danglam'),
			value: 'is_danglam',
			loai: 'trangthai',
			type: 3
		},
		{
			title: this.translate.instant('filter.hoanthanh'),
			value: 'status',
			id_row: 2,
			loai: 'trangthai'
		},
		{
			title: this.translate.instant('filter.choreview'),
			value: 'status',
			id_row: 1,
			loai: 'trangthai',
			type: 1
		},
		{
			title: this.translate.instant('filter.quahan'),
			value: 'is_quahan',
			type: 1,
			loai: 'trangthai'
		},
		{
			title: this.translate.instant('filter.htmuon'),
			value: 'is_htquahan',
			loai: 'trangthai',
		},
		{
			title: this.translate.instant('filter.quantrong'),
			value: 'important',
			loai: 'trangthai',
			type: 2
		},
		{
			title: this.translate.instant('filter.khancap'),
			value: 'urgent',
			loai: 'trangthai',
			type: 2
		},
	]
	_listFilterMoicapnhap = [
		{
			title: this.translate.instant('filter.moicapnhat'),
			value: 'UpdatedDate',
			loai: 'timeupdate'
		},
		{
			title: this.translate.instant('filter.thoigiantao'),
			value: 'CreatedDate',
			loai: 'timeupdate'
		},
		{
			title: this.translate.instant('filter.deadline'),
			value: 'deadline',
			loai: 'timeupdate'
		},
	]

	_listFilterSubtask = [
		{
			title: this.translate.instant('filter.hienthisubtask'),
			value: 1,
			loai: 'subtask'
		},
		{
			title: this.translate.instant('filter.ansubtask'),
			value: 0,
			loai: 'subtask'
		},
	]



	LoadFilter(item) {
		if (item.loai == 'trangthai') {
			this.filterStage = item;
		}
		if (item.loai == 'timeupdate') {
			this.filtermoicapnhat = item

		}
		if (item.loai == 'subtask') {
			this.filtersubtask = item;
		}
		this.loadData();
	}

}
