import { ReviewerModel } from './../work.model';
// import { tinyMCE } from 'src/app/views/components/tinyMCE';
// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Inject, OnChanges, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
// Material
// RxJS
import { Observable, BehaviorSubject, Subscription, ReplaySubject } from 'rxjs';
// NGRX
// Service
//Models
import { WorkGroupModel } from '../work.model';
import { WeWorkService } from '../../services/wework.services';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { WorkService } from '../work.service';
import { TranslateService } from '@ngx-translate/core';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { tinyMCE } from 'src/app/_metronic/jeework_old/components/tinyMCE';

@Component({
	selector: 'kt-work-group-edit',
	templateUrl: './work-group-edit.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})

export class WorkGroupEditComponent implements OnInit {
	ItemData: any;
	hasFormErrors: boolean = false;
	disBtnSubmit: boolean = false;
	loading$: Observable<boolean>;
	viewLoading: boolean = false;
	item: WorkGroupModel;
	oldItem: WorkGroupModel;
	itemForm: FormGroup;
	loadingAfterSubmit: boolean = false;
	disabledBtn: boolean = false;
	tinyMCE = {};
	listUser: any[] = [];
	listProject: any[] = [];
	filter: any = {};
	NoiDung: string;
	public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public projectFilterCtrl: FormControl = new FormControl();
	private componentSubscriptions: Subscription;
	data: any;
	TenFile: string = '';
	indexItem: number;
	ObjImage: any = { h1: "", h2: "" };
	@ViewChild('focusInput', { static: true }) focusInput: ElementRef;
	@ViewChild('myInput', { static: true }) myInputVariable: ElementRef;
	list_User: any[] = [];
	is_edit: boolean = false;
	id_project_team: number;
	constructor(
		public dialogRef: MatDialogRef<WorkGroupEditComponent>,
		private FormControlFB: FormBuilder,
		public weworkService: WeWorkService,
		private workServices: WorkService,
		private translate: TranslateService,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		@Inject(MAT_DIALOG_DATA) public DATA: any,
	) { }
	ngOnInit() {

		this.item = this.DATA._item;
		if (this.item.id_row > 0)
			this.is_edit = true;
		this.id_project_team = parseInt(this.item.id_project_team);
		this.tinyMCE = tinyMCE;
		const filter: any = {};
		filter.id_project_team = this.id_project_team;
		this.weworkService.list_account(filter).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listUser = res.data;
			}
			this.changeDetectorRefs.detectChanges();
		});
		this.createForm();
		//Detail
		this.workServices.DetailWorkGroup(this.item.id_row).subscribe(res => {
			if (res && res.status == 1) {
				console.log(res.data);
				this.item = res.data;
				this.createForm();
			}
		}
		)
		this.changeDetectorRefs.detectChanges();
	}
	createForm() {
		this.itemForm = this.FormControlFB.group({
			title: ['' + this.item.title, Validators.required],
			reviewer: ['' + this.item.reviewer ? this.item.reviewer : ''],
			NoiDung: ['' + this.item.description],
		});
		// this.itemForm.controls["title"].markAsTouched();
	}

	getTitle(): string {
		let result = this.translate.instant('GeneralKey.themmoi');
		if (!this.item || !this.item.id_row) {
			return result;
		}
		result = this.translate.instant('GeneralKey.capnhat') + ` - ${this.item.title}`;
		return result;
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
		const update = this.prepare();
		if (update.id_row > 0) {
			this.Update(update, withBack);
		} else {
			this.Create(update, withBack);
		}
	}
	prepare(): WorkGroupModel {
		const controls = this.itemForm.controls;
		const _item = new WorkGroupModel();
		_item.id_row = this.item.id_row;
		_item.title = controls['title'].value;
		_item.description = controls['NoiDung'].value;
		_item.id_project_team = '' + this.id_project_team;
		// const reviewer = new ReviewerModel();
		// reviewer.id_nv = controls['reviewer'].value;
		// reviewer.id_user = controls['reviewer'].value;
		_item.reviewer = controls['reviewer'].value ? controls['reviewer'].value : '0';
		console.log(_item);
		return _item;
	}
	close() {
		this.dialogRef.close();
	}

	Update(_item: WorkGroupModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this.workServices.UpdateWorkGroup(_item).subscribe(res => {
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
					this.changeDetectorRefs.detectChanges();
					const _messageType = this.translate.instant('GeneralKey.capnhatthanhcong');
					this.layoutUtilsService.showActionNotification(_messageType, MessageType.Update, 4000, true, false).afterDismissed().subscribe(tt => {
					});
				}
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		});
	}
	Create(_item: WorkGroupModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this.workServices.InsertWorkGroup(_item).subscribe(res => {
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
	/**
	 * Close alert
	 *
	 * @param $event
	 */
	onAlertClose($event) {
		this.hasFormErrors = false;
	}
	reset() {
		this.item = Object.assign({}, this.item);
		this.createForm();
		this.hasFormErrors = false;
		this.itemForm.markAsPristine();
		this.itemForm.markAsUntouched();
		this.itemForm.updateValueAndValidity();
	}

}
