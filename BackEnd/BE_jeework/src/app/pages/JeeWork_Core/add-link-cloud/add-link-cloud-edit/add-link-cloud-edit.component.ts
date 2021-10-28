import { LayoutUtilsService, MessageType } from '../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { WeWorkService } from '../../services/wework.services';
import { useAnimation } from '@angular/animations';
import { FilterModel, FilterDetailModel } from '../add-link-cloud.model';
import { filterService } from '../add-link-cloud.service';
import { DatePipe } from '@angular/common';
import { AttachmentModel, FileUploadModel } from '../../projects-team/Model/department-and-project.model';
import { AttachmentService } from '../../services/attachment.service';

@Component({
	selector: 'kt-add-link-cloud-edit',
	templateUrl: './add-link-cloud-edit.component.html',
})
export class addlinkcloudEditComponent implements OnInit {
	oldItem: FileUploadModel;
	item: FileUploadModel;
	itemForm: FormGroup;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	disabledBtn: boolean = false;
	constructor(public dialogRef: MatDialogRef<addlinkcloudEditComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: AttachmentService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: WeWorkService,
		public datepipe: DatePipe,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {
		this.item = new FileUploadModel();
		this.item.clear();
		this.item = this.data._item;
		if (this.item.IdRow > 0) {

		}
		else {
			// this.themcot();
			this.viewLoading = false;
		}
		this.createForm();
	}

	getNamevalue(item, id) {
		if (item) {
			var x = item.find(x => x.id == id);
			if (x) {
				return x.title;
			}
		}
		return id;
	}
	createForm() {
		this.itemForm = this.fb.group({
			link_cloud: ['', Validators.required],
		});
		this.itemForm.controls["link_cloud"].markAsTouched();

	}
	getTitle(): string {
		let result = this.translate.instant('attachment.title');
		if (!this.item || !this.item.IdRow) {
			return result;
		}
		result = this.translate.instant('GeneralKey.capnhat');
		return result;
	}
	/** ACTIONS */
	prepare(): AttachmentModel {
		const controls = this.itemForm.controls;
		var _model = new AttachmentModel();
		_model.object_type = 4;
		_model.object_id = this.data._item.object_id; // object_id = id_project_team
		const ct = new FileUploadModel();
		ct.strBase64 = "";
		ct.link_cloud = controls['link_cloud'].value;
		ct.filename = controls['link_cloud'].value;
		ct.IsAdd = true;
		_model.item = ct;
		return _model;
	}
	onSubmit(withBack: boolean = false) {
		this.hasFormErrors = false;
		this.loadingAfterSubmit = false;
		const controls = this.itemForm.controls;
		const updatedegree = this.prepare();
		// if (updatedegree.IdRow > 0) {
		// 	this.Update(updatedegree, withBack);
		// } else
		{
			this.Create(updatedegree, withBack);
		}
	}
	filterConfiguration(): any {
		const filter: any = {};
		return filter;
	}
	Update(_item: AttachmentModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this._service.Upload_attachment(_item).subscribe(res => {
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
	Create(_item: AttachmentModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this._service.Upload_attachment(_item).subscribe(res => {
			this.disabledBtn = false;
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
			this.changeDetectorRefs.detectChanges();
		});
	}
	onAlertClose($event) {
		this.hasFormErrors = false;
	}
	close() {
		this.dialogRef.close();
	}
	reset() {
		this.item = Object.assign({}, this.item);
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
			this.item = this.data._item;
			if (this.viewLoading == true) {
				this.onSubmit(true);
			}
			else {
				this.onSubmit(false);
			}
		}
	}
	text(e: any, vi: any) {
		if (!((e.keyCode > 95 && e.keyCode < 106)
			|| (e.keyCode > 45 && e.keyCode < 58)
			|| e.keyCode == 8)) {
			e.preventDefault();
		}
	}
	codeReplace(code) {
		if (code) {
			var txt = code.replace("like", ":");
			return txt;
		}
		return code;
	}

	onChange: (_: any) => void = (_: any) => { };
	remove(item) {
		this.changeDetectorRefs.detectChanges();
	}
}
