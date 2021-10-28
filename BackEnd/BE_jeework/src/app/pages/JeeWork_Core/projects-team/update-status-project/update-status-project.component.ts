import { LayoutUtilsService } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { MessageType } from './../../../../_metronic/jeework_old/core/_base/crud/utils/layout-utils.service'
import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';

import { WeWorkService } from '../../services/wework.services';
import { useAnimation } from '@angular/animations';
import { ProjectTeamModel } from '../Model/department-and-project.model';
import { ProjectsTeamService } from '../Services/department-and-project.service';

@Component({
	selector: 'kt-update-status-project',
	templateUrl: './update-status-project.component.html',
})
export class UpdateStatusProjectComponent implements OnInit {
	item1: ProjectTeamModel;
	oldItem: ProjectTeamModel
	itemForm: FormGroup;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	// @ViewChild("focusInput", { static: true }) focusInput: ElementRef;
	disabledBtn: boolean = false;
	IsEdit: boolean;
	IsProject: boolean;
	//====================Người Áp dụng====================
	public bankFilterCtrlAD: FormControl = new FormControl();
	public filteredBanksAD: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	//====================Người theo dõi===================
	public bankFilterCtrlTD: FormControl = new FormControl();
	public filteredBanksTD: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public datatree: BehaviorSubject<any[]> = new BehaviorSubject([]);
	title: string = '';
	selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
	ID_Struct: string = '';
	Id_parent: string = '';
	listUser: any[] = [];
	listChecked: any[] = [];
	public touchUi = false;
	Model: any[] = ['@item'];
	itemsAsObjects = [{ id: 0, name: 'Angular', readonly: true }, { id: 1, name: 'React' }];
	checked: any[] = [];
	checkedAdmin: any[] = [];

	colorCtr: AbstractControl = new FormControl(null);
	tendapb: string = '';
	mota: string = '';
	constructor(public dialogRef: MatDialogRef<UpdateStatusProjectComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: ProjectsTeamService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private danhMucChungService: DanhMucChungService,
		public weworkService: WeWorkService,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {
		this.title = this.translate.instant("GeneralKey.choncocautochuc") + '';
		this.item1 = this.data._item;

		this.IsEdit = this.data._IsEdit;
		this.IsProject = this.data.is_project;
		// if(this.IsProject)
		// {
		// 	this.tendapb = this.translate.instant("projects.tenduan") + '';
		// 	this.mota = this.translate.instant("projects.motangangonveduan") + '';

		// }
		// else
		// {
		// 	this.tendapb = this.translate.instant("projects.phongban") + '';
		// 	this.mota = this.translate.instant("projects.motangangonvephongban") + '';
		// }
		this._service.Detail(this.item1.id_row).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1) {
				this.item1 = res.data;
				this.IsProject = res.data.is_project;
				this.ID_Struct = '' + this.item1.id_department;
				this.createForm();
			}
			else
				this.layoutUtilsService.showError(res.error.message);
			if (this.IsProject) {
				this.tendapb = this.translate.instant("projects.tenduan") + '';
				this.mota = this.translate.instant("projects.motangangonveduan") + '';

			}
			else {
				this.tendapb = this.translate.instant("projects.phongban") + '';
				this.mota = this.translate.instant("projects.motangangonvephongban") + '';
			}
			this.changeDetectorRefs.detectChanges();
		});
		if (this.item1.id_row > 0) {
			this.viewLoading = true;
		}
		else {
			this.viewLoading = false;
		}
		this.createForm();
	}
	onSearchChange(searchValue: string): void {
	}
	createForm() {

		this.itemForm = this.fb.group({
			title: [this.item1.title, Validators.required],
			status: ['' + this.item1.status, Validators.required],
			description: [this.item1.stage_description],
		});
		// this.itemForm.controls["title"].markAsTouched();
		// this.itemForm.controls["status"].markAsTouched();
	}

	/** UI */
	getTitle(): string {
		let result = this.translate.instant('GeneralKey.themmoi');
		if (!this.item1 || !this.item1.id_row) {
			return result;
		}
		result = this.translate.instant('GeneralKey.capnhattrangthai');
		return result;
	}
	/** ACTIONS */
	prepare(): ProjectTeamModel {
		const controls = this.itemForm.controls;
		const _item = new ProjectTeamModel();
		_item.id_row = this.item1.id_row;
		_item.status = controls['status'].value;
		_item.stage_description = controls['description'].value;
		_item.is_project = this.IsProject;
		return _item;
	}
	onSubmit(withBack: boolean = false) {
		this.hasFormErrors = false;
		this.loadingAfterSubmit = false;
		const controls = this.itemForm.controls;
		/* check form */
		if (this.itemForm.invalid) {
			Object.keys(controls).forEach(controlName =>
				controls[controlName].markAsTouched()
			);
			this.hasFormErrors = true;
			return;
		}
		const updatedegree = this.prepare();
		if (updatedegree.id_row > 0) {
			this.Update(updatedegree, withBack);
		} else {
			this.Create(updatedegree, withBack);
		}
	}
	filterConfiguration(): any {

		const filter: any = {};
		return filter;
	}
	Update(_item: ProjectTeamModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this._service.UpdateStage(_item).subscribe(res => {
			/* Server loading imitation. Remove this on real code */
			this.disabledBtn = false;
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				if (withBack == true) {
					this.dialogRef.close({
						_item
					});
				}
				else {
					this.ngOnInit();
					const _messageType = this.translate.instant('GeneralKey.capnhatthanhcong');
					this.layoutUtilsService.showActionNotification(_messageType, MessageType.Update, 4000, true, false).afterDismissed().subscribe(tt => {
					});
					// this.focusInput.nativeElement.focus();
				}
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		});
	}

	Create(_item: ProjectTeamModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this._service.InsertProjectTeam(_item).subscribe(res => {
			this.disabledBtn = false;
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				if (withBack == true) {
					this.dialogRef.close({
						_item
					});
				}
				else {
					this.dialogRef.close();
				}
			}
			else {
				this.viewLoading = false;
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		});
	}
	onAlertClose($event) {
		this.hasFormErrors = false;
	}
	close() {
		this.dialogRef.close();
	}
	reset() {
		this.item1 = Object.assign({}, this.item1);
		this.createForm();
		this.hasFormErrors = false;
		this.itemForm.markAsPristine();
		this.itemForm.markAsUntouched();
		this.itemForm.updateValueAndValidity();
	}

	@HostListener('document:keydown', ['$event'])
	onKeydownHandler(event: KeyboardEvent) {
		if (event.ctrlKey && event.keyCode == 13)//phím Enter
		{
			this.item1 = this.data._item;
			if (this.viewLoading == true) {
				this.onSubmit(true);
			}
			else {
				this.onSubmit(false);
			}
		}
	}
}
