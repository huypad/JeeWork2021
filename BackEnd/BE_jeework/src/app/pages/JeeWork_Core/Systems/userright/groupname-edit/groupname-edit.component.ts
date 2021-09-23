import { LayoutUtilsService, MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { ListStateModel } from './../../../../../_metronic/jeework_old/core/utils/list-state.model';
import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ChangeDetectorRef, ViewChild, ElementRef } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router } from '@angular/router';
import { FormBuilder, FormGroup, Validators } from '@angular/forms';
import { Observable, forkJoin, from, of, BehaviorSubject } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog'; 
import { GroupNameModel } from '../Model/userright.model';
import { PermissionService } from '../Services/userright.service';

@Component({
	selector: 'kt-groupname-edit',
	templateUrl: './groupname-edit.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class GroupNameEditComponent implements OnInit {
	item: GroupNameModel;
	oldItem: GroupNameModel;
	selectedTab: number = 0;
	loadingSubject = new BehaviorSubject<boolean>(false);
	loading$ = this.loadingSubject.asObservable();
	itemForm: FormGroup;
	viewLoading: boolean = false;
	hasFormErrors: boolean = false;
	remarksListState: ListStateModel;
	loadingAfterSubmit: boolean = false;
	loadingControl = new BehaviorSubject<boolean>(false);
	specsListState: ListStateModel;
	listHinhThuc: any[] = [];
	HinhThuc: string = '';
	ID_NV: string = '';
	ShowButton: boolean = false;
	listLoaiPhep: any[] = [];
	listNam: any[] = [];
	// Filter fields
	filterLoaiPhep: string = '';
	filtertNam: string = '';
	@ViewChild("focusInput", { static: true }) focusInput: ElementRef;
	disabledBtn: boolean = false;
	constructor(public dialogRef: MatDialogRef<GroupNameEditComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private userRightService: PermissionService,
		private itemFB: FormBuilder,
		public dialog: MatDialog,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private translate: TranslateService) { }


	ngOnInit() {
		this.reset();
		this.item = this.data._item;
		this.ShowButton = true;
		this.createForm();
		setTimeout(function () { document.getElementById('tennhom').focus(); }, 100);
	}

	reset() {
		this.item = Object.assign({}, this.oldItem);
		this.createForm();
		this.hasFormErrors = false;
		this.itemForm.markAsPristine();
		this.itemForm.markAsUntouched();
		this.itemForm.updateValueAndValidity();
	}


	createForm() {
		this.itemForm = this.itemFB.group({
			tenNhom: [this.item.TenNhom, [Validators.required]],
		});
		this.itemForm.controls["tenNhom"].markAsTouched();
	}


	onSumbit(withBack: boolean = false) {
		this.hasFormErrors = false;
		const controls = this.itemForm.controls;
		/** check form */
		if (this.itemForm.invalid) {
			Object.keys(controls).forEach(controlName =>
				controls[controlName].markAsTouched()
			);
			this.hasFormErrors = true;
			this.selectedTab = 0;
			return;
		}

		let editedProduct = this.prepareProduct();
		this.createNhomNguoiDung(editedProduct, withBack);
		return;
	}

	prepareProduct(): GroupNameModel {
		const controls = this.itemForm.controls;
		const _product = new GroupNameModel();
		_product.TenNhom = controls["tenNhom"].value;
		_product.Module = "0";
		return _product;
	}

	createNhomNguoiDung(_product: GroupNameModel, withBack: boolean = false) {
		this.disabledBtn = true;
		this.userRightService.CreateNhomNguoiDung(_product).subscribe(res => {
			this.disabledBtn = false;
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				if (withBack == true) {
					this.dialogRef.close({
						_product
					});
				}
				else {
					this.reset();
					document.getElementById("tennhom").focus();
					this.changeDetectorRefs.detectChanges();
					const _messageType = this.translate.instant('GeneralKey.themthanhcong');
					this.layoutUtilsService.showActionNotification(_messageType, MessageType.Create, 4000, true, false);
				}
			}else{
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		});
	}

	goBack() {
		this.dialogRef.close();
	}
	getComponentTitle() {
		let result = this.translate.instant('phanquyen.themnhom');
		if (!this.item || !this.item.ID_Nhom) {
			return result;
		}

		return result;
	}

	//=========================================================
	@HostListener('document:keydown', ['$event'])
	onKeydownHandler(event: KeyboardEvent) {
		if (event.ctrlKey && event.keyCode == 13)//phím Enter
		{
			if (this.ShowButton == true) {
				this.onSumbit(false);
			}
			else {
				this.onSumbit(true);
			}

		}
	}
}
