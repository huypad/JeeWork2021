import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { locale } from './../../../../modules/i18n/vocabs/vi';
import { filter } from 'rxjs/operators';
import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';

import { JeeWorkLiteService } from '../../services/wework.services';
import { ProjectTeamCloseModel } from '../Model/department-and-project.model';
import { ProjectsTeamService } from '../Services/department-and-project.service';

@Component({
	selector: 'kt-closed-project',
	templateUrl: './closed-project.component.html',
})
export class ClosedProjectComponent implements OnInit {
	item1: ProjectTeamCloseModel;
	oldItem: ProjectTeamCloseModel;
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
	filter: any = {};
	optionsModel: number[];
	checked: any[] = [];
	checkedAdmin: any[] = [];

	colorCtr: AbstractControl = new FormControl(null);
	tendapb: string = '';
	mota: string = '';
	trangthai: string = '';
	status: any;
	constructor(public dialogRef: MatDialogRef<ClosedProjectComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: ProjectsTeamService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: JeeWorkLiteService,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {

		this.title = this.translate.instant("GeneralKey.choncocautochuc") + '';
		this.item1 = this.data._item;
		this.LoadStatus(this.item1['locked'])
		this.IsEdit = this.data._IsEdit;
		this.IsProject = this.data.is_project;
		if (this.IsProject) {
			this.tendapb = this.translate.instant("projects.tenduan") + '';
			this.mota = this.translate.instant("projects.motangangonveduan") + '';

		}
		else {
			this.tendapb = this.translate.instant("projects.phongban") + '';
			this.mota = this.translate.instant("projects.motangangonvephongban") + '';
		}
		if (this.item1.id_row > 0) {
			this.viewLoading = true;
		}
		else {
			this.viewLoading = false;
		}
		this.createForm();
	}
	LoadStatus(value) {
		// true = đóng -- false mở
		const translate = locale.data;
		if (!value) {
			this.trangthai = translate.projects.close_status,
				this.status = [
					{
						title: translate.projects.duanthanhcong,
						value: '4',
					},
					{
						title: translate.projects.duanthatbai,
						value: '5',
					},
					{
						title: translate.projects.duanhuy,
						value: '6',
					},
				]
		} else {
			this.trangthai = translate.projects.open_status,
				this.status = [
					{
						title: translate.filter.dungtiendo,
						value: '1',
					},
					{
						title: translate.filter.chamtiendo,
						value: '2',
					},
					{
						title: translate.filter.ruirocao,
						value: '3',
					},
				]
		}
	}

	onSearchChange(searchValue: string): void {
	}
	createForm() {

		this.itemForm = this.fb.group({
			title: [this.item1.title, Validators.required],
			close_status: [this.item1.close_status, Validators.required],
			stop_reminder: [this.item1.stop_reminder, Validators.required],
			note: [this.item1.note],
		});
		// this.itemForm.controls["close_status"].markAsTouched();
		// this.itemForm.controls["stop_reminder"].markAsTouched();
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
	prepare(): ProjectTeamCloseModel {
		const controls = this.itemForm.controls;
		const _item = new ProjectTeamCloseModel();
		_item.id_row = this.item1.id_row;
		_item.close_status = +controls['close_status'].value;
		_item.stop_reminder = controls['stop_reminder'].value;
		_item.note = controls['note'].value;
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
		// if item.locked == true ( đang đóng thao tác mở) else (ngược lại)
		if (!this.item1['locked']) {
			// đóng team 
			this.Update(updatedegree, withBack);
		} else {
			// Mở team
			this.Create(updatedegree, withBack);
		}
	}
	filterConfiguration(): any {
		const filter: any = {};
		return filter;
	}
	Update(_item: ProjectTeamCloseModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this._service.ClosedProject(_item).subscribe(res => {
			/* Server loading imitation. Remove this on real code */
			this.disabledBtn = false;
			this.viewLoading = false;
			if (res && res.status === 1) {
				if (withBack == true) {
					if (this.data.isReset) {
						window.location.reload();
					}
					this.dialogRef.close({
						_item
					});
				}
				// else {
				// 	this.ngOnInit();
				// 	const _messageType = this.translate.instant('GeneralKey.capnhatthanhcong');
				// 	this.layoutUtilsService.showActionNotification(_messageType, MessageType.Update, 4000, true, false).afterDismissed().subscribe(tt => {
				// 	});
				// 	// this.focusInput.nativeElement.focus();
				// }
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
			this.changeDetectorRefs.detectChanges();
		});
	}
	Create(_item: ProjectTeamCloseModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this._service.OpenProject(_item).subscribe(res => {
			this.loadingAfterSubmit = false;
			this.disabledBtn = false;
			this.viewLoading = false;
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
