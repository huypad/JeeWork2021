import { locale } from './../../../../modules/i18n/vocabs/vi';
import { WeWorkService } from './../../services/wework.services';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Injectable, Input } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
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
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';

import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';

import { DepartmentModel } from '../../List-department/Model/List-department.model';
import { WorkEditComponent } from '../../work/work-edit/work-edit.component';
import { WorkGroupModel, WorkModel } from '../../work/work.model';
import { WorkEditDialogComponent } from '../../work/work-edit-dialog/work-edit-dialog.component';
import { WorkGroupEditComponent } from '../../work/work-group-edit/work-group-edit.component';
import { ProjectsTeamService } from '../Services/department-and-project.service';
import { WorkEditPageComponent } from '../../work/work-edit-page/work-edit-page.component';
@Component({
	selector: 'kt-department-work-list',
	templateUrl: './department-work-list.component.html',
	styleUrls: ['./department-work-list.component.scss'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class Department_WorkListComponent implements OnInit {
	ChildComponentInstance: any;
	data: any[] = [];
	selectedItem: any = undefined;
	childComponentType: any = WorkEditPageComponent; 
	childComponentData: any = {};
	@Input() ID_Project: number = 0;
	// ID_Project: number = 0;
	@ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
	// dataSource: DepartmentProjectDataSource;
	@ViewChild(MatSort, { static: true }) sort: MatSort;
	itemForm: FormGroup;
	loadingSubject = new BehaviorSubject<boolean>(false);
	loadingControl = new BehaviorSubject<boolean>(false);
	loading1$ = this.loadingSubject.asObservable();
	hasFormErrors: boolean = false;
	selectedTab: number = 0;
	// Selection
	ID_NV: string = '';
	filterStage:any;
	item: DepartmentModel;
	oldItem: DepartmentModel;
	itemTrinhDo: any;
	showBtNV: boolean = false;
	listQuytrinh: any[] = [];
	listThongBao: any[] = [];
	listproject: any[] = [];
	listquydinh: any[] = [];
	showTruyCapNhanh: boolean = true;
	filterTinhTrang: string = "";
	keyword : string = "";
	filtermoicapnhat: string = "";
	filtersubtask: string = "";
	filterCVC: string = "";
	filterSX: string = "";
	filternhom: string = "";
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
		private router: Router,
		private weworkService: WeWorkService,
		private tokenStorage: TokenStorage,
		private danhMucChungService: DanhMucChungService
	) { }
	/** LOAD DATA */
	ngOnInit() {
		// this.activatedRoute.params.subscribe(params => {
		// 	this.ID_Project = +params.id;
		// });

		
		this.weworkService.list_account({}).subscribe(res => {
			if (res && res.status == 1)
				this.listUser = res.data;
		});

		this.filterStage = this.filterTrangthai[0];
		this.loadData();
		this.reset();
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
		
		if(this.filterSX == 'deadlinedesc'){
			queryParams.sortField = 'deadline';
			queryParams.sortOrder = 'desc'
		}
		else
			queryParams.sortField = this.filterSX;
		this._service.listView(queryParams).subscribe(res => {
			
			if (res && res.status === 1) {
				this.data = res.data;
			}
			this.changeDetectorRefs.detectChanges();
		});
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
		if (this.filterStage.value == 'status') {
			filter[this.filterStage.value] = this.filterStage.id_row;
		} else {
			if(this.filterStage.type == 1){
				filter[this.filterStage.value] = 1;
			}
			else{
				filter[this.filterStage.value] = 'True';
			}
		}
		filter.groupby = this.filternhom;
		filter.displayChild = this.filterCVC;
		filter.id_project_team = this.ID_Project;
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
		// var url = './detail/' + temp.Id;
		// var url = this.router.url +'/detail/'+ temp.Id;
		// this.router.navigateByUrl(url);
		// this.selectedItem = $event;
		// let temp: any = {};
		
		// temp.Id = this.selectedItem.id_row;
		// this.childComponentData.DATA = this.selectedItem;
		// if (this.ChildComponentInstance != undefined)
		// 	this.ChildComponentInstance.ngOnChanges();
		// var url = '/tasks/detail/' + temp.Id;
		// this.router.navigateByUrl(url);

		// ?id = 
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
	// close_detail() {
	// 	this.selectedItem = undefined;
	// }
	// getInstance($event) {
	// 	this.ChildComponentInstance = $event;
	// }
	// close_detail() {
	// 	this.selectedItem = undefined;
	// 	if (!this.changeDetectorRefs['destroyed'])
	// 		this.changeDetectorRefs.detectChanges();
	// }

	ReloadData(item){
		if(item){
			this.loadData();
		}
	}

	closeDetail(item)
	{
		this.selectedItem=undefined;
	}
	getInstance($event) {
		this.ChildComponentInstance = $event;
	}
	FilterTT(item) {
		this.filterStage = {};
		this.filterStage = item;
		this.loadData();
	}

	_filter = locale.data.filter;
	filterTrangthai = [
		{
			title: this._filter.tatca,
			value: 'all'
		},
		{
			title: this._filter.dangthuchien,
			value: 'status',
			id_row: 3
		},
		{
			title: this._filter.daxong,
			value: 'status',
			id_row: 2
		},
		{
			title: this._filter.dangdanhgia,
			value: 'status',
			id_row: 1
		},
		{
			title: this._filter.quahan,
			value: 'is_quahan',
			type : 1
		},
		{
			title: this._filter.htmuon,
			value: 'is_htquahan',
			type : 1
		},
		{
			title: this._filter.phailam,
			value: 'require',
			type : 1
		},
		{
			title: this._filter.danglam,
			value: 'is_danglam',
			type : 1
		},
		{
			title: this._filter.gansao,
			value: 'favourite',
			type : 1
		},
		{
			title: this._filter.giaochotoi,
			value: 'assign'
		},
		{
			title: this._filter.quantrong,
			value: 'important',
			type : 2
		},
		{
			title: this._filter.khancap,
			value: 'urgent',
			type : 2
		},
		{
			title: this._filter.prioritize,
			value: 'prioritize',
			type : 2
		},
	]
}
