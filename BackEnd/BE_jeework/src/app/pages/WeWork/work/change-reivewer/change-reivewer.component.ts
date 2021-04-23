import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service'
import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { WeWorkService } from '../../services/wework.services';
import { WorkGroupModel } from '../work.model';
import { WorkService } from '../work.service';
// import { startWith, map } from 'rxjs/operators';
@Component({
	selector: 'kt-change-reivewer',
	templateUrl: './change-reivewer.component.html',
})
export class ChangeReivewerComponent implements OnInit {
	item: WorkGroupModel;
	itemForm: FormGroup;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	// @ViewChild("focusInput", { static: true }) focusInput: ElementRef;
	disabledBtn: boolean = false;
	IsEdit: boolean;
	listUser: any[] = [];
	public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public bankFilterCtrl: FormControl = new FormControl();
	constructor(public dialogRef: MatDialogRef<ChangeReivewerComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: WorkService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: WeWorkService,
		private danhMucChungService: DanhMucChungService,
		private router: Router,) {
	}
	/** LOAD DATA */
	ngOnInit() {
		this.item = this.data._item;
		
		if (this.item.id > 0) {
		// this.itemForm.controls["reviewer"].setValue(this.);

			this.viewLoading = true;
		}
		else {
		this.itemForm.controls["reviewer"].setValue('');
			this.viewLoading = false;
		}
		const filter: any = {};
		filter.key = 'id_project_team';
		filter.value = this.data._item.id_project_team;
		this.weworkService.list_account({}).subscribe(res => {
			this.changeDetectorRefs.detectChanges();

			if (res && res.status === 1) {
				this.listUser = res.data;
				this.setUpDropSearchNhanVien();
				this.changeDetectorRefs.detectChanges();
			};
		});
		this.createForm();

	}
	setUpDropSearchNhanVien() {
		this.bankFilterCtrl.setValue('');
		this.filterBanks();
		this.bankFilterCtrl.valueChanges
			.pipe()
			.subscribe(() => {
				this.filterBanks();
			});
	}
	Update_Status(val: any) {

	}

	protected filterBanks() {
		if (!this.listUser) {
			return;
		}
		let search = this.bankFilterCtrl.value;
		if (!search) {
			this.filteredBanks.next(this.listUser.slice());
			return;
		} else {
			search = search.toLowerCase();
		}
		// filter the banks
		this.filteredBanks.next(
			this.listUser.filter(bank => bank.hoten.toLowerCase().indexOf(search) > -1)
		);
	}
	filterStates(name: string) {
		// return this.states.filter(state =>
		// 	state.name.toLowerCase().indexOf(name.toLowerCase()) === 0);
	}
	createForm() {

		this.itemForm = this.fb.group({
			reviewer: ['' + this.item.reviewer.id_nv, Validators.required],
		});
		// this.itemForm.controls["reviewer"].markAsTouched();
	}

	/** UI */
	getTitle(): string {
		let result = this.translate.instant('wuser.chonnguoireviewer');
		if (!this.item || !this.item.id_row) {
			return result;
		}

		result = this.translate.instant('wuser.chonnguoireviewer');
		return result;
	}
	/** ACTIONS */
	prepare(): WorkGroupModel {
		const controls = this.itemForm.controls;
		const _item = new WorkGroupModel();
		_item.id_row = this.item.id_row;
		_item.title = this.item.title;
		_item.reviewer = controls['reviewer'].value;
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

	Update(_item: WorkGroupModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this._service.UpdateWorkGroup(_item).subscribe(res => {
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

		this._service.InsertWorkGroup(_item).subscribe(res => {
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
	Assign(val) {
		this.layoutUtilsService.showActionNotification("assign");
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
}
