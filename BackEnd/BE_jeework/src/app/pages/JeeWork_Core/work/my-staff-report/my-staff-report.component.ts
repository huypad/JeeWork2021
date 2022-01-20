import { LayoutUtilsService } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Inject, Input, Output, EventEmitter, ViewEncapsulation, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
// Material
import { MatDialog } from '@angular/material/dialog';
import { MatSort, } from '@angular/material/sort';
import { MatPaginator } from '@angular/material/paginator';
// RxJS
import { Observable, BehaviorSubject, Subscription, ReplaySubject, merge } from 'rxjs';
// NGRX
// Service
//Models
import { Moment } from 'moment';
import * as moment from 'moment';
import { JeeWorkLiteService } from '../../services/wework.services';
import { TranslateService } from '@ngx-translate/core';
import { Router } from '@angular/router';
import { UserDataSource } from '../../user/data-sources/user.datasource';
import { SelectionModel } from '@angular/cdk/collections';
import { DepartmentModel } from '../../department/Model/List-department.model';
import { tap } from 'rxjs/operators';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { WorkService } from '../work.service';
import { WorkDataSource } from '../work.datasource';
import { DatePipe } from '@angular/common';

@Component({
	selector: 'kt-my-staff-report',
	templateUrl: './my-staff-report.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})

export class MyStaffReportComponent implements OnInit {
	// Public properties
	@Input() item: any;
	@Output() ItemSelected = new EventEmitter<any>();
	UserID: number;
	// item: any = {};
	TuNgay: Moment;
	DenNgay: Moment;
	id_project_team: number = 0;
	collect_by: number = 0;
	hoanthanh: number = 0;
	data: any[] = [];
	selectedItem: any;
	listProject: any[] = [];
	public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public projectFilterCtrl: FormControl = new FormControl();
	dataSource: WorkDataSource;
	displayedColumns = ['hoten', 'num_project', 'num_work', 'num1', 'num2', 'num3', 'ht_quahan', 'quahan', 'percentage'];
	@ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
	@ViewChild(MatSort, { static: true }) sort: MatSort;
	pageSize: number;
	flag: boolean = true;
	keyword: string = '';
	// Selection
	selection = new SelectionModel<DepartmentModel>(true, []);
	productsResult: DepartmentModel[] = [];
	constructor(
		private FormControlFB: FormBuilder,
		public dialog: MatDialog,
		private changeDetect: ChangeDetectorRef,
		private translate: TranslateService,
		private router: Router,
		public weworkService: JeeWorkLiteService,
		private _service: WorkService,
		private datePipe: DatePipe,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private tokenStorage: TokenStorage) { }

	/**
	 * On init
	 */
	ngOnInit() {
		var now = moment();
		this.DenNgay = now;
		this.TuNgay = moment(now).add(-1, 'months');
		this.weworkService.lite_project_team_byuser("").subscribe(res => {
			if (res && res.status === 1) {
				this.listProject = res.data;
				this.setUpDropSearchProject();
				this.changeDetectorRefs.detectChanges();
			};
		});
		// this.tokenStorage.getPageSize().subscribe(res => {
		// 	this.pageSize = +res;
		// });
		// this.sort.sortChange.subscribe(() => (this.paginator.pageIndex = 0));
		// merge(this.sort.sortChange, this.paginator.page)
		// 	.pipe(
		// 		tap(() => {
		// 			this.loadDataList();
		// 		})
		// 	)
		// 	.subscribe();
		this.dataSource = new WorkDataSource(this._service, this.layoutUtilsService);
		this.dataSource.entitySubject.subscribe(res => this.productsResult = res);
		this.loadDataList();
	}
	ngOnChanges() {
		if (this.dataSource)
			this.loadDataList();
	}

	loadDataList() {
		const queryParams = new QueryParamsModelNew(
			this.filterConfiguration(),
			// this.sort.direction,
			// this.sort.active,
			// this.paginator.pageIndex,
			// this.paginator.pageSize
		);
		this.dataSource.loadList(queryParams);
		this.layoutUtilsService.showWaitingDiv();
		setTimeout(x => {
			this.layoutUtilsService.OffWaitingDiv();
			// this.loadPage();
		}, 500)
	}
	loadPage() {
		var arrayData = [];
		this.dataSource.entitySubject.subscribe(res => arrayData = res);
		// if (arrayData !== undefined && arrayData.length == 0) {
		var totalRecord = 0;
		this.dataSource.paginatorTotal$.subscribe(tt => totalRecord = tt)
		if (totalRecord > 0) {
			const queryParams1 = new QueryParamsModelNew(
				this.filterConfiguration(),
				// this.sort.direction,
				// this.sort.active,
				// this.paginator.pageIndex = this.paginator.pageIndex - 1,
				// this.paginator.pageSize
			);
			this.dataSource.loadList(queryParams1);
		}
		else {
			const queryParams1 = new QueryParamsModelNew(
				this.filterConfiguration(),
				// this.sort.direction,
				// this.sort.active,
				// this.paginator.pageIndex = 0,
				// this.paginator.pageSize
			);
			this.dataSource.loadList(queryParams1);
		}
		// }
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
	filterConfiguration(): any {
		let filter: any = {};
		filter.TuNgay = this.TuNgay.format("DD/MM/YYYY");
		filter.DenNgay = this.DenNgay.format("DD/MM/YYYY");
		if (this.id_project_team > 0) {
			filter.id_project_team = this.id_project_team;
		}
		if (this.collect_by == 1)
			filter.collect_by = 'deadline';
		else
			filter.collect_by = 'CreatedDate';
		if(this.hoanthanh == 1){
			filter.sort_by = 'complete';
		}
		else{
			filter.sort_by = 'work';
		}
		return filter;
	}
	setUpDropSearchProject() {
		this.projectFilterCtrl.setValue('');
		this.filterProject();
		this.projectFilterCtrl.valueChanges
			.pipe()
			.subscribe(() => {
				this.filterProject();
			});
	}
	protected filterProject() {
		if (!this.listProject) {
			return;
		}
		let search = this.projectFilterCtrl.value;
		if (!search) {
			this.filtereproject.next(this.listProject.slice());
			return;
		} else {
			search = search.toLowerCase();
		}
		// filter the banks
		this.filtereproject.next(
			this.listProject.filter(bank => bank.title.toLowerCase().indexOf(search) > -1)
		);
	}
	selected($event) {
		this.selectedItem = $event;
		let temp: any = {};
		temp.Id = this.selectedItem.id_row;
		var url = '/users/' + this.UserID + '/detail/' + temp.Id;
		this.router.navigateByUrl(url);
	}
	close_detail() {
		this.selectedItem = undefined;
		if (!this.changeDetect['destroyed'])
			this.changeDetect.detectChanges();
	}
	getHeight(): any {
		let obj = window.location.href.split("/").find(x => x == "wework");

		if (obj) {
			let tmp_height = 0;
			tmp_height = window.innerHeight - 197 -this.tokenStorage.getHeightHeader();
			return tmp_height + 'px';
		} else {
			let tmp_height = 0;
			tmp_height = window.innerHeight - 180 -this.tokenStorage.getHeightHeader();
			return tmp_height + 'px';
		}
	}
}
