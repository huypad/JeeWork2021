import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Inject, Input, Output, EventEmitter, ViewEncapsulation, ViewChild } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
// Material
import { MatDialog } from '@angular/material/dialog';
// RxJS
import { Observable, BehaviorSubject, Subscription, interval, ReplaySubject } from 'rxjs';
// NGRX
// Service
//Models

import * as moment  from 'moment';
import { UpdateWorkModel } from '../work.model';
import { WorkService } from '../work.service';
import { Router } from '@angular/router';
import { PopoverContentComponent } from 'ngx-smart-popover';
import { ListDepartmentService } from '../../List-department/Services/List-department.service';
import { WeWorkService } from '../../services/wework.services';
import { TranslateService } from '@ngx-translate/core';

@Component({
	selector: 'kt-work-item',
	styleUrls: ['./work-item.component.scss'],
	templateUrl: './work-item.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})

export class WorkItemComponent implements OnInit {
	// Public properties
	@Input() item: any;
	@Input() selectedItem: any = undefined;
	@Input() listUser: any[] = [];
	@Output() ItemSelected = new EventEmitter<any>();
	@Output() Reload = new EventEmitter<any>();
	IsShow_Title: boolean = false;
	Title: string = '';
	deadline: string = '';
	_model: UpdateWorkModel;
	options: any = {};
	optionsUser: any = {};
	id_project_team: number = 0;
	project_team: string = '';
	List_milestone: any[] = [];
	isComplete = false;
	loaiItem = ''//id_milestone - Tags
	@ViewChild('PopoverTime', { static: true }) myPopoverA: PopoverContentComponent;
	constructor(
		private FormControlFB: FormBuilder,
		public dialog: MatDialog,
		private _service: WorkService,
		public weworkService: WeWorkService,
		private layoutUtilsService: LayoutUtilsService,
		private router: Router,
		private translate: TranslateService,
		private deptmentServices: ListDepartmentService,
		private changeDetectorRefs: ChangeDetectorRef) { }

	/**
	 * On init
	 */
	ngOnInit() {
		if (this.item.status == 2) {
			this.isComplete = true;
		}
		const filter: any = {};
		filter.key = 'id_project_team';
		filter.value = this.item.id_project_team;
		this.optionsUser.data=this.listUser;
	}

	setTrangthai() {
		if (this.item.status == 2) {
			this.Update_Status(3)
		} else {
			this.Update_Status(2)
		}
	}

	Update_Status(val: any) {

		var model = new UpdateWorkModel();
		model.id_row = this.item.id_row;
		model.key = 'status';
		model.value = '' + val;
		this.layoutUtilsService.showWaitingDiv();
		this._service.UpdateByKey(model).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			this.changeDetectorRefs.detectChanges();
			if (res && res.status == 1) {
				this.item.status = val;
				this.layoutUtilsService.showActionNotification(this.translate.instant('GeneralKey.capnhatthanhcong'), MessageType.Read, 4000, true, false, 3000, 'top', 1);
			}
			else {
				this.isComplete = !this.isComplete;
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
			}

		});
	}

	update(model: UpdateWorkModel) {
		this.layoutUtilsService.showWaitingDiv();
		this._service.UpdateByKey(model).subscribe(res => {
			if (res && res.status == 1) {

				this.item[res.data.key] = res.data.value == 'true' ? true : false;
				if (model.key == 'title')
					this.IsShow_Title = !this.IsShow_Title;
				this.changeDetectorRefs.detectChanges();
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Create, 9999999, true, false);
			}
			this.layoutUtilsService.OffWaitingDiv();
		});
		// this.router.navigateByUrl('/tasks', { skipLocationChange: true }).then(() => {
		// 	this.router.navigate(['/tasks']);
		// });
		// this.changeDetectorRefs.detectChanges();
	}
	update_Title(e: any) {
		var objSave = new UpdateWorkModel();
		objSave.id_row = this.item.id_row;
		objSave.key = 'title';
		objSave.value = e;
		this.layoutUtilsService.showWaitingDiv();
		this._service.UpdateByKey(objSave).subscribe(res => {
			if (res.status == 1 && res) {
				this.IsShow_Title = !this.IsShow_Title;
				this.item.title = res.data.value;
				this.changeDetectorRefs.detectChanges();
				this.layoutUtilsService.showActionNotification("Update success", MessageType.Create, 1000, true, false);
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);

			}
			this.layoutUtilsService.OffWaitingDiv();
		});
	}
	selected() {
		this.ItemSelected.emit(this.item);
		this.Reload.emit(this.item);
	}
	favourite() {
		this._service.favouritework(this.item.id_row).subscribe(res => {
			if (res && res.status == 1) {
				if(!res.data){
					this.item.favourite=1;
				}
				else{
					this.item.favourite=0;
				}
				this.changeDetectorRefs.detectChanges();
				this.layoutUtilsService.showActionNotification(this.translate.instant('GeneralKey.capnhatthanhcong'), MessageType.Read, 4000, true, false, 3000, 'top', 1);
			}
			else
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);

		});
		// this.layoutUtilsService.showActionNotification("favourite:" + this.item.id_row)
	}
	update_key(key: string) {
		this._model = new UpdateWorkModel();
		this._model.id_row = this.item.id_row;
		this._model.key = key;
		this.update(this._model);
	}
	update_start() {
		this.layoutUtilsService.showActionNotification("update-start:" + this.item.id_row)
	}

	UpdateTagandMilestone(item){
		if(this.loaiItem=='id_milestone'){
			this.item.id_milestone = item.id_row;
			this.item.milestone = item.title;
		}else if(this.loaiItem=='Tags')
		{
			var index =this.item.Tags.findIndex(x => x.id_row == item.id_row);
			if(index != -1){
				this.item.Tags.splice(index, 1);
			}else{
				this.item.Tags.push(item);
			}
			 
		}
	}

	SelectedItem($event){
		this.UpdateTagandMilestone($event)
	}
	mark_urgent() {

		this._model = new UpdateWorkModel();
		this._model.id_row = this.item.id_row;
		this._model.key = 'urgent';
		this._model.value = '' + !this.item.urgent;
		this.update(this._model);
	}
	mark_important() {
		this._model = new UpdateWorkModel();
		this._model.id_row = this.item.id_row;
		this._model.key = 'important';
		this._model.value = '' + !this.item.important;
		this.update(this._model);
		// this.layoutUtilsService.showActionNotification("mark_important:" + this.item.id_row)
	}
	Assign(val: any) {
		var model = new UpdateWorkModel();
		model.id_row = this.item.id_row;
		model.key = 'assign';
		model.value = val.id_nv;
		this.layoutUtilsService.showWaitingDiv();
		this._service.UpdateByKey(model).subscribe(res => {

			if (res && res.status == 1) {
				(this.item.assign) = val;
				this.changeDetectorRefs.detectChanges();
				this.layoutUtilsService.showActionNotification(this.translate.instant('GeneralKey.capnhatthanhcong'), MessageType.Read, 4000, true, false, 3000, 'top', 1);
			}
			else
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
			this.layoutUtilsService.OffWaitingDiv();
			this.changeDetectorRefs.detectChanges();
		});
	}
	add_milestone() {
		this.loaiItem = 'id_milestone';
		this.id_project_team = this.item.id_project_team;
		this.layoutUtilsService.showWaitingDiv();
		this.weworkService.lite_milestone(this.id_project_team).subscribe(res => {
			if (res && res.status === 1) {
				this.options = res.data;
				this.changeDetectorRefs.detectChanges();
			};
			this.layoutUtilsService.OffWaitingDiv();
		});
	}
	mark_tag() {
		this.loaiItem = 'Tags';
		this.id_project_team = this.item.id_project_team;
		this.project_team = this.item.project_team;
		this.layoutUtilsService.showWaitingDiv();
		this.weworkService.lite_tag(this.id_project_team).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.options = res.data;
				this.changeDetectorRefs.detectChanges();
			};
			this.layoutUtilsService.OffWaitingDiv();
		});
	}
	delete() {
		const _title = this.translate.instant('GeneralKey.xoa');
		const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
		const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
		const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			this.layoutUtilsService.showWaitingDiv();
			this._service.WorkDelete(this.item.id_row).subscribe(res => {
				if (res && res.status === 1) {
					this.ngOnInit();
					this.Reload.emit(true);
					this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
				}
				else {
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				}
				this.layoutUtilsService.OffWaitingDiv();
			});
		});
	}
	ShowUpdate() {
		this.IsShow_Title = !this.IsShow_Title;
		this.Title = this.item.title;
	}
	// #region css
	getItemCssClassByStatus(status: number = 0) {
		switch (status) {
			case 1: return 'metal';
			case 2: return 'success';
			case 3: return 'danger';
			default: return 'success';
		}
	}
	getItemStatusString(status: number = 0) {
		switch (status) {
			case 1: return this.translate.instant("filter.choreview");
			case 2: return this.translate.instant("filter.hoanthanh");
			case 3: return this.translate.instant("filter.danglam");
			default: return this.translate.instant("filter.choreview");
		}
	}

	getItemCssClassByimportant(status: boolean): string {
		switch (status) {
			case true:
				return 'success';
		}
	}
	getItemImportantString(condition: boolean): string {
		switch (condition) {
			case true:
				return this.translate.instant("filter.quantrong");
		}
	}

	getItemOverdue(condition: number): string {
		switch (condition) {
			case 1:
				return this.translate.instant("filter.htsau");
		}
	}

	getItemurgent(condition: boolean): string {
		switch (condition) {
			case true:
				return this.translate.instant("filter.khancap");
		}
	}
	//#endregion
	stopPropagation(event) {
		event.stopPropagation();
	}
	Status: any[] = [
		{
			name: this.translate.instant("filter.choduyet"),
			value: 1,
			color: "warn"//accent
		},
		{
			name: this.translate.instant("filter.daduyet"),
			value: 2,
			color: "accent"
		},
		{
			name: this.translate.instant("filter.huy"),
			value: 4,
			color: "danger"
		},
		{
			name: "Yêu cầu đổi tài xế",
			value: 3,
			color: "warn"
		},
		{
			name: "Yêu cầu đổi xe",
			value: true,
			color: "warn"
		},
		{
			name: "Không duyệt",
			value: 6,
			color: "danger"
		},
		{
			name: "Đã về",
			value: 99,
			color: "complete"
		},
		{
			name: "Hết hạn",
			value: false,
			color: "purple"
		},
		{
			name: "Đã điều xe",
			value: 8,
			color: "primary"
		}
	];

	getColor(status) {
		let _status = this.Status.filter(x => x.value == status)[0];
		return _status ? _status.color : "primary";
	}

	Datechange(val) {
		let a = val === "" ? new Date() : new Date(val.value);
		let date = ("0" + (a.getDate())).slice(-2) + "/" + ("0" + (a.getMonth() + 1)).slice(-2) + "/" + a.getFullYear() + " "
			+ a.getHours() + ":" + a.getMinutes();
		if (val.key == 'deadline') {
			this.item.deadline = date;
		}
		else {
			this.item.start_date = date;
		}
	}
}
