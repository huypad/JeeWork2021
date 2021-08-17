import { UserProfileService } from './../../../../_metronic/jeework_old/core/auth/_services/user-profile.service';
import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';
import { MenuHorizontalService } from './../../../../_metronic/jeework_old/core/_base/layout/services/menu-horizontal.service';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { locale } from './../../../../modules/i18n/vocabs/vi';
import { MilestoneModel } from './../../projects-team/Model/department-and-project.model';
import { milestoneDetailEditComponent } from './../../List-department/milestone-detail-edit/milestone-detail-edit.component';
import { ChooseMilestoneAndTagComponent } from './../../choose-milestone-and-tags/choose-milestone-and-tags.component';
import { Component, OnInit, OnChanges, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, HostListener, OnDestroy, Input } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { DatePipe, PlatformLocation } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
// Material
import { MatDialog } from '@angular/material/dialog';
import { SelectionModel } from '@angular/cdk/collections';
import { CdkDragStart, CdkDropList, moveItemInArray } from '@angular/cdk/drag-drop';
// RXJS
import { debounceTime, distinctUntilChanged, tap, filter } from 'rxjs/operators';
import { ReplaySubject, fromEvent, merge, BehaviorSubject } from 'rxjs';
//Datasource
import { WorkEditDialogComponent } from '../work-edit-dialog/work-edit-dialog.component';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { WorkEditComponent } from '../work-edit/work-edit.component';
import { WorkService } from '../work.service';
import { MyWorkModel, CountModel, MoiDuocGiaoModel, GiaoQuaHanModel, LuuYModel, UserInfoModel, MyMilestoneModel, FilterModel, WorkModel } from '../work.model';
import { filterEditComponent } from '../../filter/filter-edit/filter-edit.component';
import { filterService } from '../../filter/filter.service';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { ProjectsTeamService } from '../../projects-team/Services/department-and-project.service';
import { WeWorkService } from '../../services/wework.services';
//Model

@Component({
	selector: 'kt-work-list',
	templateUrl: './work-list.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
	providers: [DatePipe]
})

export class WorkListComponent implements OnInit {
	ChildComponentInstance: any;
	data: any[] = [];
	listUser: any[] = [];
	selectedItem: any = undefined;
	childComponentType: any = WorkEditComponent;
	childComponentData: any = {};
	listProject: any;
	selectedTab: number = 0;
	idFilter: number = 0;
	milestone: MyMilestoneModel;
	listfilter: any;
	mystaff: UserInfoModel[] = [];
	item: MyWorkModel;
	count: CountModel;
	moigiao: MoiDuocGiaoModel;
	giaoquahan: GiaoQuaHanModel;
	note: LuuYModel;
	show: boolean = true;
	filterStage: any;
	filterCV: any;
	Project: any;
	timeUpdate: any;
	filtersubtask: string = "";

	constructor(
		public dialog: MatDialog,
		private activatedRoute: ActivatedRoute,
		private router: Router,
		public myworkSer: ProjectsTeamService,
		private datePipe: DatePipe,
		private translate: TranslateService,
		private subheaderService: SubheaderService,
		private changeDetect: ChangeDetectorRef,
		private layoutUtilsService: LayoutUtilsService,
		public menuHorService: MenuHorizontalService,
		private _service: WorkService,
		private tokenStore: TokenStorage,
		private userProfileService: UserProfileService,
		private _filterService: filterService,
		private weworkService: WeWorkService,
		location: PlatformLocation) {
		activatedRoute.params.subscribe(val => {
			this.ngOnInit();
		});
		location.onPopState(() => {
			this.close_detail();
		});
	}
	ngOnInit() {
		this.Project = {
			title: this.translate.instant('filter.tatcaduan'),
			id_row: ''
		}
		this.timeUpdate = this.filterTimeUpdate[0];
		this.layoutUtilsService.showWaitingDiv();
		this.filterStage = this.filterTrangthai[0];
		this.filterCV = this.filterGiaoviec[0]
		this.activatedRoute.data.subscribe(res => {
			if (res && res.selectedTab)
				this.selectedTab = res.selectedTab;
		});
		this.activatedRoute.params.subscribe(res => {
			if (res && res.id)
				this.idFilter = res.id
		});
		this.weworkService.list_account({}).subscribe(res => {
			if (res && res.status == 1)
				this.listUser = res.data;
		});
		this.loadThongTinUser();
		this._service.mymilestone().subscribe(res => {
			if (res && res.status === 1) {
				this.milestone = res.data;
				this.changeDetect.detectChanges();
			}
		});
		this.LoadFilter();
		this.myworkSer.MyWork().subscribe(res => {
			if (res && res.status == 1) {
				this.item = res.data;
				this.count = res.data.Count;
				this.giaoquahan = res.data.GiaoQuaHan;
				this.moigiao = res.data.MoiDuocGiao;
				this.note = res.data.LuuY;
			}
		});

		this._service.myStaff().subscribe(res => {
			if (res && res.status === 1) {
				this.mystaff = res.data;
			}
			this.changeDetect.detectChanges();
		});
		//load ds dự án và chèn tất cả dự án vào đầu
		this.weworkService.lite_project_team_byuser("").subscribe(res => {
			if (res && res.status === 1) {
				this.listProject = res.data;
				this.listProject.unshift(
					{
						title: this.translate.instant('filter.tatcaduan'),
						id_row: ''
					}
				)
			};
		});
		this.layoutUtilsService.OffWaitingDiv();

		this.loadData();
	}
	profile: any;
	@ViewChild('avatar2', { static: true }) avatar2: ElementRef;
	Ten: string = '';
	ChucVu: string = '';
	Image: string = '';
	@Input() avatarr: string = './assets/app/media/img/users/user4.jpg';
	loadThongTinUser() {
		let id: any;
		this.tokenStore.getIDUser().subscribe(res => {
			id = +res;
			// this.userProfileService.getHinhAnhByID(+id).subscribe(res => {
			// 	this.profile = res;
			// 	let UserData = {
			// 		HoTen: res.HoTen,
			// 		Image: res.Image,
			// 		ChucVu: res.ChucVu,
			// 		Username: localStorage.getItem('Username')
			// 	};
			// 	this.tokenStore.setUserData(UserData);
			// 	if (this.profile == undefined) {

			// 	}
			// 	else {
			// 		this.Image = this.profile.Image;
			// 		this.Ten = this.profile.HoTen;
			// 		this.ChucVu = this.profile.ChucVu;
			// 	}
			// 	this.changeDetect.detectChanges();
			// });
		});
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
		queryParams.sortField = this.timeUpdate.value;
		this.data = [];
		if (this.selectedTab == 0) {
			this._service.myList(queryParams).subscribe(res => {
				this.layoutUtilsService.showWaitingDiv();
				if (res && res.status === 1) {
					this.data = res.data;
				}
				setTimeout(() => {
					this.layoutUtilsService.OffWaitingDiv();
					this.changeDetect.detectChanges();
				}, 500);
			});
		}
		if (this.selectedTab == 1) {
			this._service.myStaffList(queryParams).subscribe(res => {
				this.layoutUtilsService.showWaitingDiv();
				if (res && res.status === 1) {
					this.data = res.data;
				}
				setTimeout(() => {
					this.layoutUtilsService.OffWaitingDiv();
					this.changeDetect.detectChanges();
				}, 500);
			});
		}
		if (this.selectedTab == 2) {
			this._service.listByFilter(queryParams).subscribe(res => {
				if (res && res.status === 1) {
					this.data = res.data;
				}
				this.changeDetect.detectChanges();
			});
		}
		if (this.selectedTab == 3) {
			this._service.listFollowing(queryParams).subscribe(res => {
				if (res && res.status === 1) {
					this.data = res.data;
				}
				this.changeDetect.detectChanges();
			});
		}
	}

	LoadFilter() {
		this._service.Filter().subscribe(res => {
			if (res && res.status === 1) {
				this.listfilter = res.data;
				this.changeDetect.detectChanges();
			}
		});
	}
	filterConfiguration(): any {
		let filter: any = {};
		if (this.filterStage.value == 'status') {
			filter[this.filterStage.value] = this.filterStage.id_row;
		} else {
			if (this.filterStage.type == 1) {
				filter[this.filterStage.value] = 1;
			}
			else {
				filter[this.filterStage.value] = 'True';
			}
		}
		filter.id_project_team = this.Project.id_row;
		filter.filter = this.filterCV.value;
		if (this.selectedTab == 2) {
			filter.id_filter = this.idFilter;
		}
		filter.displayChild = this.filtersubtask;

		return filter;

	}

	showTitleFilter(id) {
		if (this.listfilter != undefined) {
			var x = this.listfilter.find(x => x.id_row == id)
			if (x) {
				return x.title;
			}
			else {
				return 'NULL';
			}
		}
	}

	open() {
		const dialogRef = this.dialog.open(WorkEditDialogComponent, { data: { DATA: { Id: 1 } } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
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
		var isFollow = this.router.url.split('/').find(x => x == 'following');
		var url = "";
		if (isFollow) {
			url = '/tasks/following/detail/' + temp.Id
		}
		else {
			url = '/tasks/detail/' + temp.Id;
		}
		this.router.navigateByUrl(url);
	}
	Reload(value) {
		if (value) {
			this.ngOnInit();
		}
	}


	SelectedMilestone($event){
		var x = this.selectedItem;
		this.selectedItem = undefined;
		setTimeout(() => {
			this.selectedItem = x;
		}, 50);
		
	}

	close_detail() {
		this.selectedItem = undefined;
		if (!this.changeDetect['destroyed'])
			this.changeDetect.detectChanges();
	}
	getInstance($event) {
		this.ChildComponentInstance = $event;
	}
	AddWork() {
		const models = new WorkModel();
		models.clear();
		this.UpdateWork(models);
	}
	restoreState(queryParams: QueryParamsModelNew, id: number) {
		if (id > 0) {
		}

		if (!queryParams.filter) {
			return;
		}
	}
	UpdateWork(_item: WorkModel) {
		let saveMessageTranslateParam = '';
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(WorkEditDialogComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
			}
			else {
				this.ngOnInit();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
			}
		});
	}

	ChangeFilter(item) {
		const url = 'tasks/filter/' + item;
		this.router.navigateByUrl(url);
	}

	selectedMileston(_item) {
		this.myworkSer.FindDepartmentFromProjectteam(_item.id_project_team).subscribe(res => {
			if (res && res.status == 1) {
				const url = 'depts/' + res.data + '/milestones/' + _item.id_row;
				this.router.navigateByUrl(url);
			}
		})

	}

	addFilter() {
		const model = new FilterModel();
		model.clear();
		this.Update(model);
	}
	addMileston() {
		let saveMessageTranslateParam = '';
		var _item = new MilestoneModel;
		_item.clear();
		_item.id_project_team = 0;
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(milestoneDetailEditComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.ngOnInit();
				return;
			}
			else {
				this.ngOnInit();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
			}
		});
	}

	UpdateMileston() {
		let saveMessageTranslateParam = '';
		var _item = new MilestoneModel;
		_item.clear();
		_item.id_project_team = 0;
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(milestoneDetailEditComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.ngOnInit();
				return;
			}
			else {
				this.ngOnInit();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
			}
		});
	}

	Update(_item: FilterModel) {
		let saveMessageTranslateParam = '';
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(filterEditComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.LoadFilter()
				// this.changeDetect.detectChanges();
			}
		});
	}
	Add() { }
	getItemCssClassByurgent(status: boolean): string {

		switch (status) {
			case true:
				return 'metal';
		}
	}
	getItemurgent(condition: boolean): string {
		switch (condition) {
			case true:
				return 'Urgent';
		}
	}
	DeleteFilter(_item: FilterModel) {
		const _title = this.translate.instant('GeneralKey.xoa');
		const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
		const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
		const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			this._filterService.Delete_filter(_item.id_row).subscribe(res => {
				if (res && res.status === 1) {
					this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
					let _backUrl = `tasks`;
					this.router.navigateByUrl(_backUrl);
					this.LoadFilter();
					// this.changeDetect.detectChanges();
				}
				else {
					this.LoadFilter();
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				}

			});
		});
	}

	UpdateFilter() {

	}


	FilterGV(item) {
		this.filterCV = item;
		this.loadData();
	}
	FilterTT(item) {
		this.filterStage = {};
		this.filterStage = item;
		this.loadData();
	}
	FilterDA(item) {
		this.Project = item;
		this.loadData();
	}
	FilterTimeUpdate(item) {
		this.timeUpdate = item;
		this.loadData();
	}



	_filter = locale.data.filter;
	filterTrangthai = [
		{
			title: this.translate.instant('filter.tatcatrangthai'),
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
	filterTimeUpdate = [
		{
			title: this.translate.instant('filter.moicapnhat'),
			value: 'UpdatedDate'
		},
		{
			title: this.translate.instant('filter.thoigiantao'),
			value: 'CreatedDate'
		},
		{
			title: this.translate.instant('filter.htmuon'),
			value: 'end_date'
		},
		{
			title: this.translate.instant('filter.deadline'),
			value: 'deadline'
		},
	]

	filterGiaoviec = [
		{
			title: this.translate.instant('filter.giaovaduocgiao'),
			value: ''
		},
		{
			title: this.translate.instant('filter.congviecduocgiao'),
			value: '1'
		},
		{
			title: this.translate.instant('filter.congviecgiaodi'),
			value: '2'
		},
	]


}
