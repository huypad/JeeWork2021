import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service'

 import { TranslateService } from '@ngx-translate/core';
 import { Router } from '@angular/router';
 
import { JeeWorkLiteService } from '../../services/wework.services';
  import { UserInfoModel, WorkDuplicateModel, WorkModel } from '../work.model';
import { WorkService } from '../work.service';
 import { PopoverContentComponent } from 'ngx-smart-popover';
@Component({
	selector: 'kt-work-add-followers',
	templateUrl: './work-add-followers.component.html',
})
export class workAddFollowersComponent implements OnInit {
	oldItem: WorkDuplicateModel;
	item: WorkDuplicateModel;
	itemForm: FormGroup;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	listUser: any[] = [];
	@ViewChild('Follower', { static: true }) myPopover: PopoverContentComponent;
	selected: any[] = [];
	@ViewChild('hiddenText', { static: true }) textEl: ElementRef;
	_Follower: string = '';
	list_follower: any[] = [];
	list_User: any[] = [];
	options: any = {};
	disabledBtn:boolean=false;
	constructor(public dialogRef: MatDialogRef<workAddFollowersComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: WorkService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: JeeWorkLiteService,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {
		this.item = this.data._item;
		
		if (this.item.id_row > 0) {
			this.viewLoading = true;
		}
		else {
			this.viewLoading = false;
		}
		this.weworkService.list_account({}).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listUser = res.data;
			};
			this.options = this.getOptions();
			this.changeDetectorRefs.detectChanges();
		});
		this.createForm();
	}
	createForm() {
		this.itemForm = this.fb.group({
		});
	}

	getKeyword() {
		let i = this._Follower.lastIndexOf('@');
		if (i >= 0) {
			let temp = this._Follower.slice(i);
			if (temp.includes(' '))
				return '';
			return this._Follower.slice(i);
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
		
		this._Follower = (<HTMLInputElement>document.getElementById("InputUser")).value;
		if (this.selected.length > 0) {
			var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm
			var match = this._Follower.match(reg);
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
		this.selected = this.list_follower;
		this.selected.push(data);
		let i = this._Follower.lastIndexOf('@');
		this._Follower = this._Follower.substr(0, i) + '@' + data.username + ' ';
		this.myPopover.hide();
		let ele = (<HTMLInputElement>document.getElementById("InputUser"));
		ele.value = this._Follower;
		ele.focus();
		this.changeDetectorRefs.detectChanges();
	}
	/** UI */
	getTitle(): string {
		let result = this.translate.instant('work.themnhieunguoitheodoi');
		return result;
	}
	/** ACTIONS */
	prepare(): WorkModel {
		const controls = this.itemForm.controls;
		const item = new WorkModel();
		item.id_row = this.data._item.id_row;
		if (this.selected.length > 0) {
			
			this.listUser.map((user, index) => {
				let _true = this.selected.find(x => x.id_nv === user.id_nv);
				if (_true) {
					const ct = new UserInfoModel();
					ct.id_user = user.id_nv;
					this.list_User.push(ct);
				}
			});
		}
		item.Users = this.list_User;
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
			this.Create(updatedegree, withBack);
	}
	filterConfiguration(): any {

		const filter: any = {};
		return filter;
	}
	Create(_item: WorkModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this._service.Add_followers(_item).subscribe(res => {
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
