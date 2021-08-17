import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';
import { EventEmitter } from '@angular/core';
// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Type, Input, Output } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
// RxJS
import { Observable, BehaviorSubject, Subscription } from 'rxjs';
// Service
import { WorkEditComponent } from '../work-edit/work-edit.component';
import { WorkService } from '../work.service';
import { DuplicateWorkComponent } from '../work-duplicate/work-duplicate.component';
import { UpdateWorkModel, WorkDuplicateModel, WorkModel } from '../work.model';
import { TranslateService } from '@ngx-translate/core';
import { WorkDetailComponent } from '../work-detail/work-detail.component';
import { WorkEditDialogComponent } from '../work-edit-dialog/work-edit-dialog.component';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { workAddFollowersComponent } from '../work-add-followers/work-add-followers.component';
import { WeWorkService } from '../../services/wework.services';
import { WorkAssignedComponent } from '../work-assigned/work-assigned.component';
import { Location } from '@angular/common';
@Component({
	selector: 'kt-work-edit-page',
	templateUrl: './work-edit-page.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkEditPageComponent implements OnInit, OnDestroy {
	ChildComponentInstance: any;
	childComponentData: any = {};
	// Public properties
	loadingSubject = new BehaviorSubject<boolean>(true);
	loading$: Observable<boolean>;
	// Private password
	private componentSubscriptions: Subscription;
	DataID: number = 0;
	ComponentTitle: string = 'Work detail';
	loading = true;
	// childComponentType: Type<any>
	detail: any;
	List_tag: any[];
	childComponentType: any = WorkDetailComponent;
	@Input() data: any;
	@Output() closeDetail = new EventEmitter<any>();
	/**
	 * Component constructor
	 *
	 * @param activatedRoute: ActivatedRoute
	 * @param router: Router
	 * @param UserFB: FormBuilder
	 * @param dialog: MatDialog
	 * @param subheaderService: SubheaderService
	 * @param layoutUtilsService: LayoutUtilsService,
	 * @param UserService: UserService, * 
	 * @param changeDetectorRefs: ChangeDetectorRef
	 */

	IsKhanCap: boolean = false;
	IsQuaHan: boolean = false;
	IsQuanTrong: boolean = false;
	IsFavourite: boolean = false;
	Tags: any[];
	id_row: number = 0;
	options: any = {};
	id_project_team: number = 0;
	options_assign: any;
	project_team: string = '';
	fromTask = true; // dữ liệu được truy xuất từ task công việc 
	constructor(
		private activatedRoute: ActivatedRoute,
		private router: Router,
		public dialog: MatDialog,
		private location: Location,
		private changeDetectorRefs: ChangeDetectorRef,
		private subheaderService: SubheaderService,
		private layoutUtilsService: LayoutUtilsService,
		public _service: WorkService,
		public weworkService: WeWorkService,
		private translate: TranslateService,
	) {
	}
	ngOnInit() {
		this.childComponentType = WorkDetailComponent;
		this.loading$ = this.loadingSubject.asObservable();
		this.loadingSubject.next(true);
		this.activatedRoute.params.subscribe(params => {
			this.loadingSubject.next(false);
			this.DataID = params['id'];
			if (this.data) {
				this.DataID = this.data.DATA.id_row;
				this.fromTask = false;
			}
			this._service.WorkDetail(this.DataID).subscribe(res => {
				if (res && res.status == 1) {
					this.childComponentData.DATA = res.data;
					this.detail = res.data;
					this.id_project_team = this.detail.id_project_team;
					this.IsKhanCap = this.detail.urgent;
					this.IsQuaHan = this.detail.is_quahan;
					this.IsQuanTrong = this.detail.important;
					this.IsFavourite = this.detail.favourite;
					this.Tags = this.detail.Tags;
					this.mark_tag();
					this.ChildComponentInstance.ngOnChanges();
					this.changeDetectorRefs.detectChanges();
				}
			});
			this.changeDetectorRefs.detectChanges();
		});
		this.getListUser();
	}
	ngOnChanges() {
		this.ngOnInit();
	}
	/**
	 * On destroy
	 */
	ngOnDestroy() {
		if (this.componentSubscriptions) {
			this.componentSubscriptions.unsubscribe();
		}
	}

	onSubmit() {
		let data = this.ChildComponentInstance.onSubmit();
		if (data) {
			this.ChildComponentInstance.reset();
		}
	}
	mark_tag() {
		this.weworkService.lite_tag(this.id_project_team).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.options = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
	}
	getInstance($event) {
		this.ChildComponentInstance = $event;
	}
	goBack() {
		window.history.back();
		// if(this.fromTask)
		// {
		// 	window.history.back();
		// }
		// else{
		// 	this.router.navigateByUrl('/project/1/home/stream').then(()=>{
		// 		this.ngOnInit();
		// 	});
		// }
	}
	duplicate(type: number) {
		var model = new WorkDuplicateModel();
		model.clear(); // Set all defaults fields

		model.type = type;
		model = this.detail;
		model.type = type;
		this.Update_duplicate(model);
	}
	Update_duplicate(_item: WorkDuplicateModel) {
		let saveMessageTranslateParam = '';
		// _item = this.detail;
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(DuplicateWorkComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.ngOnInit();
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.ngOnInit();
			}
		});
	}
	work() {
		const model = new WorkModel();
		model.clear();
		this.Update_work(model);
	}
	assign() {
		var item = this.getOptions_Assign();
		const dialogRef = this.dialog.open(WorkAssignedComponent, {
			width: '500px',
			height: '500px',
			data: { item }
		});
		let _item = this.detail;
		dialogRef.afterClosed().subscribe(res => {
			//
			var model = new UpdateWorkModel();
			model.id_row = _item.id_row;
			model.key = 'assign';
			model.value = res.id_nv;
			this.layoutUtilsService.showWaitingDiv();
			this._service.UpdateByKey(model).subscribe(res => {
				this.layoutUtilsService.OffWaitingDiv();
				this.changeDetectorRefs.detectChanges();
				if (res && res.status == 1) {
					this.ngOnInit();
					this.layoutUtilsService.showActionNotification(this.translate.instant('GeneralKey.capnhatthanhcong'), MessageType.Read, 4000, true, false, 3000, 'top', 1);
				}
				else
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);

			});
		});
	}
	Update_work(_item: WorkModel) {
		let saveMessageTranslateParam = '';
		_item = this.detail;
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(WorkEditDialogComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.ngOnInit();
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.ngOnInit();
			}
		});
	}
	Add_followers() {
		let saveMessageTranslateParam = '';
		var _item = new WorkModel();
		_item = this.detail;
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(workAddFollowersComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.ngOnInit();
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.ngOnInit();
			}
		});
	}
	Delete() {
		const _title = this.translate.instant('GeneralKey.xoa');
		const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
		const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
		const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			this._service.DeleteWork(this.detail.id_row).subscribe(res => {
				if (res && res.status === 1) {
					this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
					let _backUrl = `tasks`;
					this.router.navigateByUrl(_backUrl);
				}
				else {
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
					this.ngOnInit();
				}

			});
		});
	}

	update(model: UpdateWorkModel) {
		this.layoutUtilsService.showWaitingDiv();
		this._service.UpdateByKey(model).subscribe(res => {
			if (res && res.status == 1) {
				this.ngOnInit();
				this.changeDetectorRefs.detectChanges();
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Create, 9999999, true, false);
			}
			this.layoutUtilsService.OffWaitingDiv();
		});
		// const _refreshUrl = 'tasks/detail/' + this.detail.id_row;
		// this.router.navigateByUrl(_refreshUrl);
		// this.changeDetectorRefs.detectChanges();
	}

	favourite() {
		this.layoutUtilsService.showWaitingDiv();
		this._service.favouritework(this.detail.id_row).subscribe(res => {
			if (res && res.status == 1) {
				this.ngOnInit();
				this.changeDetectorRefs.detectChanges();
			}
			else
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
		});
		this.layoutUtilsService.OffWaitingDiv();
		// const _refreshUrl = 'tasks/detail/' + this.detail.id_row;
		// this.router.navigateByUrl(_refreshUrl);
		// this.changeDetectorRefs.detectChanges();
	}
	_model: UpdateWorkModel;
	update_key(key: string) {
		this._model = new UpdateWorkModel();
		this._model.id_row = this.detail.id_row;
		this._model.key = key;
		this.update(this._model);
	}
	mark_urgent() {
		this._model = new UpdateWorkModel();
		this._model.id_row = this.detail.id_row;
		this._model.key = 'urgent';
		this._model.value = '' + !this.detail.urgent;
		this.update(this._model);
	}
	mark_important() {
		this._model = new UpdateWorkModel();
		this._model.id_row = this.detail.id_row;
		this._model.key = 'important';
		this._model.value = '' + !this.detail.important;
		this.update(this._model);
	}

	listUser: any;
	async getListUser() {
		const filter: any = {};
		this.weworkService.list_account(filter).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listUser = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
	}

	getOptions_Assign() {
		var options_assign: any = {
			showSearch: true,
			keyword: '',
			data: this.listUser,
		};
		return options_assign;
	}

	ReloadData(event) {
		this.ngOnInit();
	}

}
