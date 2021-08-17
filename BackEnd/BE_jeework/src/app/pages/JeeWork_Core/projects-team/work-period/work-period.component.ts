import { locale } from './../../../../modules/i18n/vocabs/vi';
import { WeWorkService } from './../../services/wework.services';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Injectable, Input, Output, EventEmitter } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatSort } from '@angular/material/sort';
import { MatPaginator,MatPaginatorIntl } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
// RXJS
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { BehaviorSubject, Observable, of as observableOf } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { FormGroup, FormBuilder, FormControl } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';

import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';

import { DepartmentModel } from '../../List-department/Model/List-department.model';
import { WorkGroupModel, WorkModel } from '../../work/work.model';
import { WorkEditDialogComponent } from '../../work/work-edit-dialog/work-edit-dialog.component';
import { DialogSelectdayComponent } from '../../report/dialog-selectday/dialog-selectday.component';
import { ProjectsTeamService } from '../Services/department-and-project.service';
import { WorkEditPageComponent } from '../../work/work-edit-page/work-edit-page.component';
import { WorkGroupEditComponent } from '../../work/work-group-edit/work-group-edit.component';
import * as moment  from 'moment';
import * as range from 'lodash.range';
export interface CalendarDate {
	mDate: moment.Moment;
	selected?: boolean;
	today?: boolean;
}

@Component({
	selector: 'kt-work-period',
	templateUrl: './work-period.component.html',
	styleUrls: ['./work-period.component.scss'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkPeriodComponent implements OnInit {
	ChildComponentInstance: any;
	data: any[] = [];
	selectedItem: any = undefined;
	childComponentType: any = WorkEditPageComponent;
	childComponentData: any = {};
	@Input() inputDate: any;
	@Output('change')
	listDate: EventEmitter<any> = new EventEmitter<any>();

	public currentDate: moment.Moment;
	public flagCurrentDate: moment.Moment;
	public namesOfDays = ['CN', 'T2', 'T3', 'T4', 'T5', 'T6', 'T7'];
	public weeks: Array<CalendarDate[]> = [];

	public selectedDate;
	public selectedStartWeek;
	public selectedEndWeek;
	public show: boolean;

	@ViewChild('calendar', { static: true }) calendar;

	@ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
	// dataSource: DepartmentProjectDataSource;
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
	filterTinhTrang: string = "";
	filtermoicapnhat: string = "";
	filtersubtask: string = "";
	filterCVC: string = "";
	filterSX: string = "";
	filternhom: string = "";
	keyword: string = "";
	filterStage: any;
	TuNgay = "";
	DenNgay = "";
	listUser: any[] = [];
	constructor(
		private _service: ProjectsTeamService,
		private router: Router,
		public dialog: MatDialog,
		private route: ActivatedRoute,
		private itemFB: FormBuilder,
		public subheaderService: SubheaderService,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private translate: TranslateService,
		public datepipe: DatePipe,
		private weworkService: WeWorkService,
		private tokenStorage: TokenStorage,
		private danhMucChungService: DanhMucChungService
	) { }

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
		queryParams.sortField = this.filterCVC;
		this._service.PeriodView(queryParams).subscribe(res => {
			this.layoutUtilsService.showWaitingDiv()
			if (res && res.status === 1) {
				this.data = res.data;
				this.layoutUtilsService.OffWaitingDiv();
				this.changeDetectorRefs.detectChanges();
			}
			// this.changeDetectorRefs.detectChanges();
		});
	}

	/** LOAD DATA */
	ngOnInit() {

		this.weworkService.list_account({}).subscribe(res => {
			if (res && res.status == 1)
				this.listUser = res.data;
		});
		if (this.inputDate != null) {
			this.currentDate = moment(new Date(this.inputDate));
			this.flagCurrentDate = moment(new Date(this.inputDate));
			this.selectedStartWeek = moment(new Date(this.inputDate)).weekday(0);
			this.selectedEndWeek = moment(this.inputDate).weekday(6);
		}
		else {
			this.currentDate = moment();
			this.flagCurrentDate = moment();
			this.selectedStartWeek = moment().weekday(0);// chủ nhật tuần trước
			this.selectedEndWeek = moment().weekday(6);// thứ 7 hiện tại
		}

		this.selectedDate = `${this.selectedStartWeek.format('DD/MM/YYYY')} - ${this.selectedEndWeek.format('DD/MM/YYYY')}`;
		this.generateCalendar();
		this.filterStage = this._listFilterTrangthai[0];
		this.loadData();
		this.reset();
	}
	private generateCalendar(): void {
		const dates = this.fillDates(this.currentDate);
		const weeks = [];
		while (dates.length > 0) {
			weeks.push(dates.splice(0, 7));
		}
		this.weeks = weeks;
		this.weeks.forEach(element => {
			let filterList = element.filter(x => x.selected)
			if (filterList != null && filterList.length > 0) {
				this.listDate.emit(element);
			}
		})
	}
	private fillDates(currentMoment: moment.Moment) {
		// index first day of month in week
		const firstOfMonth = moment(currentMoment).startOf('month').day();
		// index last day of month  in week
		const lastOfMonth = moment(currentMoment).endOf('month').day();

		const firstDayOfGrid = moment(currentMoment).startOf('month').subtract(firstOfMonth, 'days');
		// get last start of week + week
		const lastDayOfGrid = moment(currentMoment).endOf('month').subtract(lastOfMonth, 'days').add(7, 'days');

		const startCalendar = firstDayOfGrid.date();
		return range(startCalendar, startCalendar + lastDayOfGrid.diff(firstDayOfGrid, 'days')).map((date) => {
			const newDate = moment(firstDayOfGrid).date(date);
			return {
				today: this.isToday(newDate),
				selected: this.isSelected(newDate),
				mDate: newDate,
			};
		});
	}

	public prevMonth(): void {
		this.currentDate = moment(this.currentDate).subtract(1, 'months');
		this.generateCalendar();
	}

	public nextMonth(): void {
		this.currentDate = moment(this.currentDate).add(1, 'months');
		this.generateCalendar();
	}

	public isDisabledMonth(currentDate): boolean {
		const today = moment();
		return moment(currentDate).isBefore(today, 'months');
	}

	private isToday(date: moment.Moment): boolean {
		return moment().format('YYYY-MM-DD') === moment(date).format('YYYY-MM-DD');
	}

	private isSelected(date: moment.Moment): boolean {
		return moment(date).isBefore(this.selectedEndWeek) && moment(date).isAfter(this.selectedStartWeek)
			|| moment(date.format('YYYY-MM-DD')).isSame(this.selectedStartWeek.format('YYYY-MM-DD'))
			|| moment(date.format('YYYY-MM-DD')).isSame(this.selectedEndWeek.format('YYYY-MM-DD'));
	}

	public isDayBeforeLastSat(date: moment.Moment): boolean {
		const lastSat = moment().weekday(-1);
		return moment(date).isAfter(lastSat);
	}

	public isSelectedMonth(date: moment.Moment): boolean {
		return moment(date).isSame(this.currentDate, 'month');
	}

	public selectDate(date: CalendarDate[]) {
		this.selectedStartWeek = moment(date[0].mDate);
		this.selectedEndWeek = moment(date[6].mDate);
		this.selectedDate = `${this.selectedStartWeek.format('DD/MM/YYYY')} - ${this.selectedEndWeek.format('DD/MM/YYYY')}`;
		// this.loadData();
		this.generateCalendar();
		this.show = !this.show;
		this.loadData();
		// this.listDate.emit(date);
	}

	public canSelected(date: CalendarDate[]) {
		var isCurrentWeek = false;
		date.forEach(x => {
			if (moment(x.mDate).format('YYYY-MM-DD') == moment(this.flagCurrentDate).format('YYYY-MM-DD')) {
				isCurrentWeek = true;
			}
		})
		if (isCurrentWeek) return true;
		var flagSelected = true;
		date.forEach(x => {
			if (moment(x.mDate).isBefore(this.flagCurrentDate)) {
				flagSelected = false;
			}
		})
		return flagSelected;
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
		filter.TuNgay = this.selectedStartWeek.format('DD/MM/YYYY'); //this.selectedStartWeek.format('DD/MM/YYYY')} - ${this.selectedEndWeek.format('DD/MM/YYYY')
		filter.DenNgay = this.selectedEndWeek.format('DD/MM/YYYY');
		filter.keyword = this.keyword;
		filter.id_project_team = this.ID_Project;
		filter.groupby = this.filternhom;
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

		filter.displayChild = this.filtersubtask;
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
			return ("0" + (a.getDate())).slice(-2) + "/" + ("0" + (a.getMonth() + 1)).slice(-2) + "/" + a.getFullYear();
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
	loadDataList() {
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
				this.loadDataList();
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

	range = new FormGroup({
		start: new FormControl(),
		end: new FormControl()
	});

	//chọn ngày
	// filter header
	// selectedDate = {
	// 	startDate: new Date(),
	// 	endDate: new Date(),
	// }

	// openDialog(): void {
	// 	const dialogRef = this.dialog.open(DialogSelectdayComponent, {
	// 		width: '500px',
	// 		data: this.selectedDate
	// 	});
	// 	dialogRef.afterClosed().subscribe(result => {
	// 		if (result) { 
	// 			this.TuNgay = this.f_convertDate(result.startDate);
	// 			this.DenNgay = this.f_convertDate(result.endDate);
	// 			this.changeDetectorRefs.detectChanges();
	// 			this.loadData();
	// 		}
	// 	});
	// }
	openDialog(): void {
		const dialogRef = this.dialog.open(DialogSelectdayComponent, {
			width: '500px',
			data: this.selectedDate
		});

		dialogRef.afterClosed().subscribe(result => {
			if (result != undefined) {
				this.TuNgay = this.f_convertDate(result.startDate);
				this.DenNgay = this.f_convertDate(result.endDate);
				this.changeDetectorRefs.detectChanges();
				this.loadData();
			}
		});
	}

	LoadFilter(item) {
		this.filterStage = item;
		this.loadData();
	}

	ReloadData(item) {
		if (item) {
			this.loadData();
		}
	}

	isitemTT(item) {
		if (this.filterStage == item)
			return 'bold';
		return '';
	}

	_filter = locale.data.filter;
	_listFilterTrangthai = [
		{
			title: this.translate.instant('filter.tatca'),
			value: 'all'
		},
		{
			title: this.translate.instant('filter.dangthuchien'),
			value: 'status',
			id_row: 3
		},
		{
			title: this.translate.instant('filter.daxong'),
			value: 'status',
			id_row: 2
		},
		{
			title: this.translate.instant('filter.dangdanhgia'),
			value: 'status',
			id_row: 1
		},
		{
			title: this.translate.instant('filter.quahan'),
			value: 'is_quahan',
			type: 1
		},
		{
			title: this.translate.instant('filter.htmuon'),
			value: 'is_htquahan',
			type: 1
		},
		{
			title: this.translate.instant('filter.phailam'),
			value: 'require',
			type: 1
		},
		{
			title: this.translate.instant('filter.danglam'),
			value: 'is_danglam',
			type: 1
		},
		{
			title: this.translate.instant('filter.gansao'),
			value: 'favourite',
			type: 1
		},
		{
			title: this.translate.instant('filter.giaochotoi'),
			value: 'assign'
		},
		{
			title: this.translate.instant('filter.quantrong'),
			value: 'important',
			type: 2
		},
		{
			title: this.translate.instant('filter.khancap'),
			value: 'urgent',
			type: 2
		},
		{
			title: this.translate.instant('filter.prioritize'),
			value: 'prioritize',
			type: 2
		},
	]
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
}
