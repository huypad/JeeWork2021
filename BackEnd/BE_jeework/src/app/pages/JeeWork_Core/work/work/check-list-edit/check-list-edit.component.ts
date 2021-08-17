import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service'
import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';


import { WeWorkService } from '../../services/wework.services';
import { useAnimation } from '@angular/animations';
import { WorkDuplicateModel } from '../work.model';
import { WorkService } from '../work.service';
import { ChecklistModel } from '../../update-by-keys/update-by-keys.model';

@Component({
	selector: 'kt-check-list-edit',
	templateUrl: './check-list-edit.component.html',
})
export class CheckListEditComponent implements OnInit {
	item: ChecklistModel;
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
	colorCtr: AbstractControl = new FormControl(null);
	IsCheckList: boolean = false;
	constructor(public dialogRef: MatDialogRef<CheckListEditComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: WorkService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private danhMucChungService: DanhMucChungService,
		public weworkService: WeWorkService,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {
		this.item = this.data._item;
		
		this.IsCheckList = this.data.IsCheckList;
		if (this.item.id_row > 0) {
			this.viewLoading = true;
		}
		else {
			this.viewLoading = false;
		}
		const filter: any = {};
		this.reset();
	}
	onSearchChange(searchValue: string): void {
	}
	createForm() {
		this.itemForm = this.fb.group({
			title: [this.item.title, Validators.required],
		});
		// this.itemForm.controls["title"].markAsTouched();
	}
	/** UI */
	getTitle(): string {
		let result = this.translate.instant('GeneralKey.themmoi');
		if (!this.item || !this.item.id_row) {
			return result;
		}
		result = this.translate.instant('GeneralKey.chinhsua');
		return result;
	}
	/** ACTIONS */
	prepare(): ChecklistModel {
		const controls = this.itemForm.controls;
		const item = new ChecklistModel();
		item.id_row = this.data._item.id_row;
		item.title = controls['title'].value;
		return item;
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
	Update(_item: ChecklistModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		if(this.IsCheckList)
		{
			this._service.Update_CheckList(_item).subscribe(res => {
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
					}
				}
				else {
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				}
			});
		}
		else
		{
			this._service.Update_CheckList_Item(_item).subscribe(res => {
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
					}
				}
				else {
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				}
			});
		}
		
	}
	Create(_item: ChecklistModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		if(this.IsCheckList)
		{
			this._service.Insert_CheckList(_item).subscribe(res => {
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
		else
		{
			this._service.Insert_CheckList_Item(_item).subscribe(res => {
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
