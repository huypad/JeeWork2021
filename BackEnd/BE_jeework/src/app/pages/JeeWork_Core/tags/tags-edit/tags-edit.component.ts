import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service'

import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';import { WeWorkService } from '../../services/wework.services';
import { useAnimation } from '@angular/animations';

import { DatePipe } from '@angular/common';
import { TagsService } from '../tags.service';
import { TagsModel } from '../../projects-team/Model/department-and-project.model';

@Component({
	selector: 'kt-tags-edit',
	templateUrl: './tags-edit.component.html',
})
export class TagsEditComponent implements OnInit {
	oldItem: TagsModel;
	item: TagsModel;
	itemForm: FormGroup;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	// @ViewChild("focusInput", { static: true }) focusInput: ElementRef;
	disabledBtn: boolean = false;
	filter: any = {};
	title: string = '';
	_color: string = '';
	constructor(public dialogRef: MatDialogRef<TagsEditComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: TagsService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private danhMucChungService: DanhMucChungService,
		public weworkService: WeWorkService,
		public datepipe: DatePipe,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {
		this.title = this.data._item.project_team;
		this.item = this.data._item;
		
		if (this.item.id_row > 0) {
			this.viewLoading = true;
		}
		else {
			this.viewLoading = false;
		}
		this.createForm();
	}
	createForm() {
		this.itemForm = this.fb.group({
			title: ['' + this.item.title, Validators.required],
			color: [this.item.color],
		});
		this.itemForm.controls["title"].markAsTouched();
	}
	getTitle(): string {
		let result = this.translate.instant('tags.themtag') + ' '+this.translate.instant('tags.vao')+' ' + this.title;
		if (!this.item || !this.item.id_row) {
			return result;
		}
		result = this.translate.instant('GeneralKey.chinhsua');
		return result;
	}
	/** ACTIONS */
	prepare(): TagsModel {
		const controls = this.itemForm.controls;
		const item = new TagsModel();
		item.id_row = this.data._item.id_row;
		item.id_project_team = this.data._item.id_project_team;
		item.title = controls['title'].value;
		item.color = this._color;
		return item;
	}
	ItemSelected(event): any {
		this._color = event.value;
		this.item.color = event;
		this._color = event;
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
	Update(_item: TagsModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this._service.Update(_item).subscribe(res => {
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
	Create(_item: TagsModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this._service.Insert(_item).subscribe(res => {
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
}
