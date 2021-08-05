import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service'
import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { WeWorkService } from '../../services/wework.services';
import { AuthorizeModel } from '../Model/user.model';
import { UserService } from '../Services/user.service';
// import { startWith, map } from 'rxjs/operators';
@Component({
	selector: 'kt-authorize-edit',
	templateUrl: './authorize-edit.component.html',
})
export class AuthorizeEditComponent implements OnInit {
	item: AuthorizeModel;
	itemForm: FormGroup;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	is_all_project: boolean = false;
	// @ViewChild("focusInput", { static: true }) focusInput: ElementRef;
	disabledBtn: boolean = false;
	IsEdit: boolean;
	listUser: any[] = [];
	ds_project : any = [];
	public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public bankFilterCtrl: FormControl = new FormControl();
	constructor(public dialogRef: MatDialogRef<AuthorizeEditComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: UserService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: WeWorkService,
		private danhMucChungService: DanhMucChungService,
		private router: Router,) {
	}
	/** LOAD DATA */
	ngOnInit() {
		this.item = this.data._item;
		this.is_all_project = this.item.is_all_project;
		if (this.item.id_row > 0) {
			this.viewLoading = true;
		}
		else {
			this.viewLoading = false;
		}
		const filter: any = {};
		// filter.key = 'id_project_team';
		// filter.value = this.data._item.id_project_team;
		filter.id_project_team = this.data._item.id_project_team;
		this.weworkService.list_account({}).subscribe(res => {
			if (res && res.status === 1) {
				this.listUser = res.data;
				this.setUpDropSearchNhanVien();
				this.changeDetectorRefs.detectChanges();
			}else {
				this.layoutUtilsService.showError(res.error.message);
			};
		});
		this.createForm();

		this.weworkService.lite_project_team_byuser().subscribe(res => {
			if (res && res.status === 1) {
				this.ds_project = res.data
				// this.changeDetectorRefs.detectChanges();
			}else {
				this.layoutUtilsService.showError(res.error.message);
			};
		});
	}
	LoadDetail(id){
		this._service.DetailUQ(id).subscribe(res => {
		});
	}
	checkProject(value){
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
		var x = [];
		if(this.item.list_project){
			this.item.list_project.split(',').forEach(y=>{
				x.push(+y);
			})
			
		}
		this.itemForm = this.fb.group({
			id_user: [''+this.item.id_user, Validators.required],
			start: [this.item.start_date , Validators.required],
			end: [ this.item.end_date , Validators.required],
			list_project: [x],
		});
		this.changeDetectorRefs.detectChanges();
		// this.itemForm.controls["id_user"].markAsTouched();
	}

	/** UI */
	getTitle(): string {
		let result = this.translate.instant('wuser.uyquyengiaoviec');
		if (!this.item || !this.item.id_row) {
			return result;
		}

		result = this.translate.instant('wuser.uyquyengiaoviec');
		return result;
	}
	/** ACTIONS */
	prepare(): AuthorizeModel {
		const controls = this.itemForm.controls;
		const _item = new AuthorizeModel();
		_item.clear();
		_item.id_row = this.item.id_row;
		_item.id_user = controls['id_user'].value;
		_item.start_date = this.f_convertDate(controls['start'].value);
		_item.end_date = this.f_convertDate(controls['end'].value);
		_item.is_all_project = this.is_all_project;
		if(!this.is_all_project){
			if(controls['list_project'].value)
				_item.list_project = controls['list_project'].value.join();
			else {
				this.layoutUtilsService.showError('Lỗi lấy danh sách dự án');
				return null;
			}
			}
		return _item;
	}
	f_convertDate(v: any = "") {
		let a = v === "" ? new Date() : new Date(v);
		return (
		  a.getFullYear() +
		  "-" +
		  ("0" + (a.getMonth() + 1)).slice(-2) +
		  "-" +
		  ("0" + a.getDate()).slice(-2) +
		  "T00:00:00.0000000"
		);
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
		if(!updatedegree) return;
		if (updatedegree.id_row > 0) {
			this.Update(updatedegree, withBack);
		} else {
			this.Create(updatedegree, withBack);
		}
	}

	Update(item: AuthorizeModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this._service.UpdateAuthorize(item).subscribe(res => {
			/* Server loading imitation. Remove this on real code */
			this.disabledBtn = false;
			if (res && res.status === 1) {
				if (withBack == true) {
					this.dialogRef.close({
						item
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
	Create(item: AuthorizeModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this._service.InsertAuthorize(item).subscribe(res => {
			this.disabledBtn = false;
			if (res && res.status === 1) {
				if (withBack == true) {
					this.dialogRef.close({
						item
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
}
