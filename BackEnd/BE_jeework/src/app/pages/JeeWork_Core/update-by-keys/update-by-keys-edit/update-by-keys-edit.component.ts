import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service'
 import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';import { JeeWorkLiteService } from '../../services/wework.services';
import { useAnimation } from '@angular/animations';
 import { UpdateByKeyService } from '../update-by-keys.service';
import { UpdateByKeyModel } from '../update-by-keys.model';

@Component({
	selector: 'kt-update-by-keys-edit',
	templateUrl: './update-by-keys-edit.component.html',
})
export class UpdateByKeysComponent implements OnInit {
	oldItem: UpdateByKeyModel;
	item: UpdateByKeyModel;
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
	Id_parent: string = '';
	minDate = new Date();
	listUser: any[] = [];
	listChecked: any[] = [];
	filter: any = {};
	optionsModel: number[];
	checked: any[] = [];
	checkedAdmin: any[] = [];
	ShowDrop: boolean = false;
	colorCtr: AbstractControl = new FormControl(null);
	check_nguoitheodoi: any;
	check_nguoithuchien: any;
	listType: any[] = [];
	listID: any[] = [];
	listID_Admin: any[] = [];
	isParent: boolean = false;
	constructor(public dialogRef: MatDialogRef<UpdateByKeysComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: UpdateByKeyService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private danhMucChungService: DanhMucChungService,
		public weworkService: JeeWorkLiteService,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {
		this.title = this.translate.instant("work.bansaocua") + ' ' + this.data._item.title;
		this.item = this.data._item;
		
		if (this.item.id_row > 0) {
			this.viewLoading = true;
		}
		else {
			this.viewLoading = false;
		}
		const filter: any = {};
		this.weworkService.list_account(filter).subscribe(res => {
			this.disabledBtn = false;
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listUser = res.data;
				
				this.setUpDropSearchNhanVienAD();
				this.setUpDropSearchNhanVienTD();
				this.changeDetectorRefs.detectChanges();
			};
		});
		this.weworkService.lite_workgroup(this.data._item.id_project_team).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listType = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});

		this.reset();
		if (this.data._item.id_parent == null) {
			this.isParent = false;
			this.itemForm.controls["id_parent"].setValue('0');
		}
		else {
			this.isParent = true;
			this.itemForm.controls["id_parent"].setValue('');
		}
		this.itemForm.controls["title"].setValue(this.title);
	}
	onSearchChange(searchValue: string): void {
	}
	Check_Assign(value: any) {
		this.check_nguoithuchien = value;
	}
	checkChangeFollower(value: any) {
		this.check_nguoitheodoi = value;
	}
	setUpDropSearchNhanVienAD() {
		
		this.bankFilterCtrlAD.setValue('');
		this.filterBanksAD();
		this.bankFilterCtrlAD.valueChanges
			.pipe()
			.subscribe(() => {
				this.filterBanksAD();
			});
	}
	protected filterBanksAD() {
		
		if (!this.listUser) {
			return;
		}
		let search = this.bankFilterCtrlAD.value;
		if (!search) {
			this.filteredBanksAD.next(this.listUser.slice());
			return;
		} else {
			search = search.toLowerCase();
		}
		// filter the banks
		this.filteredBanksAD.next(
			this.listUser.filter(bank => bank.hoten.toLowerCase().indexOf(search) > -1)
		);
	}
	setUpDropSearchNhanVienTD() {
		this.bankFilterCtrlTD.setValue('');
		this.filterBanksTD();
		this.bankFilterCtrlTD.valueChanges
			.pipe()
			.subscribe(() => {
				this.filterBanksTD();
			});
	}
	protected filterBanksTD() {
		if (!this.listUser) {
			return;
		}
		let search = this.bankFilterCtrlTD.value;
		if (!search) {
			this.filteredBanksTD.next(this.listUser.slice());
			return;
		} else {
			search = search.toLowerCase();
		}
		// filter the banks
		this.filteredBanksTD.next(
			this.listUser.filter(bank => bank.hoten.toLowerCase().indexOf(search) > -1)
		);
	}
	createForm() {
		this.itemForm = this.fb.group({
			// title: [this.item.title, Validators.required],
			// deadline: [this.item.deadline],
			// start_date: [this.item.start_date, Validators.required],
			// description: [this.item.description],
			// id_group: ['' + this.item.id_group],
			// followers: [this.listID, Validators.required],
		});
		// this.itemForm.controls["title"].markAsTouched();
		// this.itemForm.controls["start_date"].markAsTouched();
		// this.itemForm.controls["id_parent"].markAsTouched();
		// this.itemForm.controls["duplicate_child"].markAsTouched();
		// this.itemForm.controls["assign"].markAsTouched();
		// this.itemForm.controls["followers"].markAsTouched();
		// this.itemForm.controls["urgent"].markAsTouched();
		// this.itemForm.controls["required_result"].markAsTouched();
	}
	DeadlineChange(val: any) {
		if (val > 0) {
			this.ShowDrop = true;
			this.itemForm.controls["deadline"].setValue('');
		}
		else {
			this.ShowDrop = false;
			this.itemForm.controls["deadline"].setValue(' ');
		}
	}
	/** UI */
	getTitle(): string {
		let result = this.translate.instant('work.nhanbancongviec') + ' ' + this.data._item.title;
		if (!this.item || !this.item.id_row) {
			return result;
		}
		result = this.translate.instant('work.nhanbancongviec') + ' ' + this.data._item.title;
		return result;
	}
	/** ACTIONS */
	prepare(): UpdateByKeyModel {
		const controls = this.itemForm.controls;
		const item = new UpdateByKeyModel();
		item.id_row = this.data._item.id_row;
	
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
	Update(_item: UpdateByKeyModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this._service.UpdateByKey(_item).subscribe(res => {
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
	Create(_item: UpdateByKeyModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this._service.UpdateByKey(_item).subscribe(res => {
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
