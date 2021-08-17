import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, OnChanges, Input, EventEmitter, Output } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { tap } from 'rxjs/operators';
import { fromEvent, merge, BehaviorSubject, ReplaySubject } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
// Services
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';import { FormGroup, FormBuilder, Validators, FormControl } from '@angular/forms';
import { isFulfilled } from 'q';
import { DatePipe } from '@angular/common';
import { WeWorkService } from '../../services/wework.services';
import { WorkService } from '../work.service';
import { WorkModel, UserInfoModel, LuuYModel, GiaoQuaHanModel, MyWorkModel, CountModel, MyMilestoneModel, FilterModel, MoiDuocGiaoModel } from '../work.model';
import { PopoverContentComponent } from 'ngx-smart-popover';
import { ProjectsTeamService } from '../../projects-team/Services/department-and-project.service';

@Component({
	selector: 'kt-work-detail-tab-right',
	templateUrl: './work-detail-tab-right.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class WorkDetailTabRightComponent implements OnInit, OnChanges {
	listNam: any[] = [];
	loaiItem = '';
	@Input() data: any[];
	@Input() selectedItem: any = undefined;
	@Output() SelectedMilestone: any = new EventEmitter<any>();
	// @Output() closeDetail = new EventEmitter<any>();
	itemForm: FormGroup;
	loadingSubject = new BehaviorSubject<boolean>(false);
	loadingControl = new BehaviorSubject<boolean>(false);
	loading1$ = this.loadingSubject.asObservable();
	hasFormErrors: boolean = false;
	// work_model: WorkModel;
	work_model: any = [];
	id_project_team: number;
	admins: any[] = [];
	members: any[] = [];
	options: any = {};
	IsAdmin: boolean = false;
	@ViewChild('myPopoverA', { static: true }) myPopoverA: PopoverContentComponent;

	selectedTab: number = 0;
	//===============Khai báo value chi tiêt==================
	listNoiCapCMND: any[] = [];
	//========================================================
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	show: boolean = true;
	_data: any[];
	constructor(private _service: WorkService,
		public dialog: MatDialog,
		public myworkSer: ProjectsTeamService,
		public subheaderService: SubheaderService,
		private changeDetectorRefs: ChangeDetectorRef,
		public datepipe: DatePipe,
		public weworkService: WeWorkService,
		private translate: TranslateService,
		private work_service: WorkService,
		private layoutUtilsService: LayoutUtilsService,
	) { }

	/** LOAD DATA */
	ngOnInit() {
		this.data = this.selectedItem;
		
		this._service.WorkDetail(this.selectedItem.id_row).subscribe(res => {
			if (res && res.status == 1) {
				this.work_model = res.data;
				// this.options = this.getOptions();
			}
			this.changeDetectorRefs.detectChanges();
		});
	}

	ngOnChanges() {
		this._service.WorkDetail(this.selectedItem.id_row).subscribe(res => {
			if (res && res.status == 1) {
				this.work_model = res.data;
				// this.changeDetectorRefs.detectChanges();
			}
		});

	}
	refreshData() {
		this._service.WorkDetail(this.selectedItem.id_row).subscribe(res => {
			if (res && res.status == 1) {
				this.ngOnInit();
				this.changeDetectorRefs.detectChanges();
			}
		});
	}
	filterConfiguration(): any {
		const filter: any = {};
		// filter.id_work = this.item.id_row;
		return filter;
	}

	getItemCssClass(item: any): string {

		if (item.is_danglam == '1')
			return 'success';
		if (item.is_quahan == '1')
			return 'metal';
		if (item.is_htquahan == '1')
			return 'brand';
		if (item.urgent == '1')
			return 'metal';
		else
			return 'metal';
	}
	getString(item: any): string {
		if (item.isdanglam == 1)
			return this.translate.instant('wuser.danglam');
		if (item.is_quahan == 1)
			return this.translate.instant('wuser.quahan');
		if (item.is_htquahan == 1)
			return this.translate.instant('wuser.htquahan');
		if (item.urgent == 1)
			return this.translate.instant('wuser.khancap');
		else
			return this.translate.instant('wuser.quantrong');
	}
	getOptions() {
		var options: any = {};
		var filter: any = {};
		filter.key = 'id_project_team';
		filter.value = this.id_project_team;
		options.filter = filter;
		if (this.IsAdmin)
			options.excludes = this.admins.map(x => x.id_nv);
		else
			options.excludes = this.members.map(x => x.id_nv);
		return options;
	}

	initAddUser($event, admin = false) {
		this.options = this.getOptions();
		this.IsAdmin = admin;
		let el = $event.currentTarget.offsetParent;
		this.myPopoverA.show();
		this.myPopoverA.top = el.offsetTop + 50;
		this.myPopoverA.left = el.offsetLeft;
		this.changeDetectorRefs.detectChanges();
	}
	ItemSelected(user) {
		this.myPopoverA.hide();
		this.addMember(user.id_nv);
	}

	ItemSelectedMiles(miles) {
		this.SelectedMilestone.emit(miles);
		this.ngOnChanges();
	}

	initAddMembers(admin = false) {
		// var title = admin ? 'Thêm nhiều quản lý dự án' : 'Thêm nhiều thành viên'
		// const dialogRef = this.dialog.open(AddUsersDialogComponent, { data: { title: title, filter: {}, excludes: [] }, width: '500px' });
		// dialogRef.afterClosed().subscribe(res => {
		// 	if (!res) {
		// 		return;
		// 	}
		// 	else {
		// 		this.addMembers(res, admin);
		// 	}
		// });
	}
	addMembers(users, admin) {
		// this.layoutUtilsService.showWaitingDiv();
		// var data = {
		// 	id_row: this.id_project_team,
		// 	Users: users.map(x => {
		// 		return {
		// 			id_user: x,
		// 			admin: admin
		// 		}
		// 	})
		// }
		// this._services.Add_user(data).subscribe(res => {
		// 	this.layoutUtilsService.OffWaitingDiv();
		// 	if (res && res.status == 1)
		// 		this.ngOnInit();
		// 	else
		// 		this.layoutUtilsService.showError(res.error.message);
		// })
	}
	addMember(id_nv) {
		this.layoutUtilsService.showWaitingDiv();
		var data = {
			id_row: this.selectedItem.id_row,
			Users: [{
				id_user: id_nv,
				admin: this.IsAdmin
			}]
		}
		this.work_service.Add_followers(data).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1)
				this.ngOnInit();
			else
				this.layoutUtilsService.showError(res.error.message);
		})

	}
	ViewDetail(id_activities: number) {

	}
	add_milestone() {
		this.loaiItem = 'id_milestone';
		this.id_project_team = this.work_model.id_project_team;
		this.weworkService.lite_milestone(this.id_project_team).subscribe(res => {
			if (res && res.status === 1) {
				this.options = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
	}
}
