import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service'

// import { AngularEditorConfig } from '@kolkov/angular-editor';
import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject } from 'rxjs';
import { Router } from '@angular/router';
import { ListDepartmentService } from '../Services/List-department.service';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';import { DepartmentModel, DepartmentOwnerModel } from '../Model/List-department.model';
import { PopoverContentComponent } from 'ngx-smart-popover';
import { WeWorkService } from '../../services/wework.services';
@Component({
	selector: 'kt-List-department-edit',
	templateUrl: './List-department-edit.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class DepartmentEditComponent implements OnInit {
	item: DepartmentModel;
	oldItem: DepartmentModel
	itemForm: FormGroup;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	// @ViewChild("focusInput", { static: true }) focusInput: ElementRef;
	disabledBtn: boolean = false;
	IsEdit: boolean;
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
	options: any = {};
	id_project_team: number;
	admins: any[] = [];
	members: any[] = [];
	IsAdmin: boolean = false;
	@ViewChild('myPopoverC', { static: true }) myPopover: PopoverContentComponent;
	selected: any[] = [];
	listUser: any[] = [];
	@ViewChild('hiddenText', { static: true }) textEl: ElementRef;
	CommentTemp: string = '';
	listChiTiet: any[] = [];
	list_Owners: any[] = [];
	IsDataStaff_HR = false;
	constructor(public dialogRef: MatDialogRef<DepartmentEditComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _Services: ListDepartmentService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private danhMucChungService: DanhMucChungService,
		public weworkService: WeWorkService,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {
		this.title = this.translate.instant("GeneralKey.choncocautochuc") + '';
		this.item = this.data._item;
		this.options = this.getOptions();
		this.weworkService.list_account({}).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listUser = res.data;
			}
			this.options = this.getOptions();
			this.changeDetectorRefs.detectChanges();
		});
		this.IsEdit = this.data._IsEdit;
		if (this.item.RowID > 0) {
			this._Services.DeptDetail(this.item.RowID).subscribe(res => {
				if (res && res.status == 1) {
					this.item = res.data;
					this.list_Owners = res.data.Owners;
					for (let i = 0; i < this.list_Owners.length; i++) {
						this.CommentTemp += ' @' + this.item.Owners[i]['Username'];;
					};
					
					this.selected = this.item.Owners;
					this.CommentTemp = this.CommentTemp.substring(1);
					this.changeDetectorRefs.detectChanges();
				}
			});

			this.viewLoading = true;
		}
		else {
			this.viewLoading = false;
		}
		this.createForm();
		this.getTreeValue();
		// this.focusInput.nativeElement.focus();
	}
	clickOnUser = (event: Event) => {
		// Prevent opening anchors the default way
		event.preventDefault();
		const anchor = event.target as HTMLAnchorElement;

		this.layoutUtilsService.showInfo("user clicked");
	}

	getTreeValue() {
		this.danhMucChungService.Get_MaCoCauToChuc_HR().subscribe(res => {

			if (res.data && res.data.length > 0) {
				this.datatree.next(res.data);
				this.changeDetectorRefs.detectChanges();
				// this.selectedNode.next({
				// 	RowID: "" + this.item.id_cocau,
				// });
				// if ("" + this.item.id_cocau != undefined)
				// 	this.ID_Struct = '' + this.item.id_cocau;
				// else
				// 	this.ID_Struct = '';
				// this.loadListChucVu();
			}
		});
	}
	GetValueNode(val: any) {
		this.ID_Struct = val.RowID;
		this.danhMucChungService.GetListPositionbyStructure(this.ID_Struct).subscribe(res => {
			// this.listChucDanh = res.data;
			if (res.data.length > 0) {
			} else {
				// this.itemForm.controls['chucDanh'].setValue('');
				// this.itemForm.controls['chucVu'].setValue('');
			}
		});
	}
	createForm() {

		this.itemForm = this.fb.group({
			title: [this.item.title, Validators.required],
			dept_name: [this.item.id_cocau],
			id_user: [''],
		});
		// this.itemForm.controls["title"].markAsTouched();
		// this.itemForm.controls["dept_name"].markAsTouched();

	}

	/** UI */
	getTitle(): string {
		let result = this.translate.instant('department.taomoi');
		if (!this.item || !this.item.id_row) {
			return result;
		}

		result = this.translate.instant('department.chinhsua');
		return result;
	}
	/** ACTIONS */
	prepare(): DepartmentModel {
		const controls = this.itemForm.controls;
		const _item = new DepartmentModel();
		_item.id_row = this.item.id_row;
		_item.id_cocau = controls['dept_name'].value?controls['dept_name'].value:0;
		_item.title = controls['title'].value;
		
		this.selected;
		if (this.selected.length > 0) {
			this.list_Owners.map((item, index) => {
				let _true = this.selected.find(x => x.id_user === item.id_nv);
				if (_true) {
					const ct = new DepartmentOwnerModel();
					if (item.id_row == undefined)
						item.id_row = 0;
					ct.id_row = item.id_row;
					ct.id_department = this.item.id_row;
					ct.id_user = item.id_nv;
					this.listChiTiet.push(ct);
				}
				else {
					const ct = new DepartmentOwnerModel();
					if (item.id_row == undefined)
						item.id_row = 0;
					ct.id_row = item.id_row;
					ct.id_department = this.item.id_row;
					ct.id_user = item.id_nv;
					this.listChiTiet.push(ct);
				}
			});
		}
		_item.Owners = this.listChiTiet;

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
		if(this.IsDataStaff_HR && this.itemForm.controls["dept_name"].value ==""){
			this.itemForm.controls["dept_name"].markAsTouched();
			return;
		}
		const updatedegree = this.prepare();
		if(updatedegree.Owners.length == 0){
			this.layoutUtilsService.showError("Người sở hữu là bắt buộc");
			return;
		}
		if (updatedegree.id_row > 0) {
			this.Update(updatedegree, withBack);
		} else {
			this.Create(updatedegree, withBack);
		}
	}

	Update(_item: DepartmentModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this._Services.UpdateDept(_item).subscribe(res => {
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

	Create(_item: DepartmentModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this._Services.InsertDept(_item).subscribe(res => {
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

	getKeyword() {
		let i = this.CommentTemp.lastIndexOf('@');
		if (i >= 0) {
			let temp = this.CommentTemp.slice(i);
			if (temp.includes(' '))
				return '';
			return this.CommentTemp.slice(i);
		}
		return '';
	}
	getOptions() {
		var options: any = {
			showSearch: false,
			keyword: this.getKeyword(),
			data: this.listUser.filter(x => this.selected.findIndex(y => x.id_nv == y.id_nv) < 0),
		};
		return options;
	}
	onSearchChange($event) {
		this.CommentTemp = (<HTMLInputElement>document.getElementById("InputUser")).value;
		
		if (this.selected.length > 0) {
			var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm
			var match = this.CommentTemp.match(reg);
			if (match != null && match.length > 0) {
				let arr = match.map(x => x);
				this.selected = this.selected.filter(x => arr.includes('@' + x.username));
			} else {
				this.selected = [];
			}
		}
		this.options = this.getOptions();
		if (this.options.keyword) {
			let el = $event.currentTarget;
			let rect = el.getBoundingClientRect();
			this.myPopover.show();
			this.changeDetectorRefs.detectChanges();
		}
	}
	click($event, vi = -1) {
		this.myPopover.hide();
	}
	ItemSelected(data) {
		this.selected = this.list_Owners;
		this.selected.push(data);
		let i = this.CommentTemp.lastIndexOf('@');
		this.CommentTemp = this.CommentTemp.substr(0, i) + '@' + data.username + ' ';
		this.myPopover.hide();
		let ele = (<HTMLInputElement>document.getElementById("InputUser"));
		ele.value = this.CommentTemp;
		ele.focus();
		this.changeDetectorRefs.detectChanges();
	}
}
