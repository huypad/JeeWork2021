// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Inject, OnChanges, ViewChild, ElementRef } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
// Material
// RxJS
import { Observable, BehaviorSubject, Subscription, ReplaySubject } from 'rxjs';
// NGRX
// Service
//Models

import * as moment  from 'moment';
import { WorkModel, UserInfoModel, WorkTagModel } from '../work.model';
import { tinyMCE } from 'src/app/_metronic/jeework_old/components/tinyMCE';
import { WeWorkService } from '../../services/wework.services';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { PopoverContentComponent } from 'ngx-smart-popover';
import { WorkService } from '../work.service';
import { TranslateService } from '@ngx-translate/core';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
@Component({
	selector: 'kt-work-edit',
	templateUrl: './work-edit.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})

export class WorkEditComponent implements OnInit, OnChanges, OnDestroy {
	ItemData: any;
	hasFormErrors: boolean = false;
	disBtnSubmit: boolean = false;
	loading$: Observable<boolean>;
	viewLoading: boolean = false;
	item: WorkModel;
	oldItem: WorkModel;
	itemForm: FormGroup;
	loadingAfterSubmit: boolean = false;
	disabledBtn: boolean = false;
	tinyMCE = {};
	ListFollower: any[] = [];
	listUser: any[] = [];
	listProject: any[] = [];
	filter: any = {};
	optionsModel: number[];
	check_tags: any;
	listTag: any[] = [];
	listType: any[] = [];
	editor_description: string = '';
	NoiDung: string;
	minDate:any = new Date();
	UserInfo: any = {};
	listTags: any[] = [];
	public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public projectFilterCtrl: FormControl = new FormControl();
	private componentSubscriptions: Subscription;
	public bankFilterCtrl: FormControl = new FormControl();
	data: any;
	indexItem: number;
	Image: WorkModel;
	@ViewChild('focusInput', { static: true }) focusInput: ElementRef;
	@ViewChild('myInput', { static: true }) myInputVariable: ElementRef;
	Attachments: any[] = [];
	List_id_milestone: any[] = [];
	Id_project_team: number = 0;
	options: any = {};
	options_assign: any = {};
	ListAttachFile: any[] = [];
	AttachFileComment: any[] = [];
	@ViewChild('follower', { static: true }) myPopover: PopoverContentComponent;
	@ViewChild('myPopover_Assign', { static: true }) myPopover_Assign: PopoverContentComponent;
	@ViewChild('myPopoverChild', { static: true }) myPopoverChild: PopoverContentComponent;
	selected: any[] = [];
	selected_Assign: any[] = [];
	@ViewChild('hiddenText', { static: true }) textEl: ElementRef;
	@ViewChild('hiddenText_Assign', { static: true }) text_Assign: ElementRef;
	_Follower: string = '';
	_Assign: string = '';
	list_follower: any[] = [];
	list_Assign: any[] = [];
	list_User: any[] = [];
	is_edit: boolean = false;
	id_project_team: number = 0;
	@ViewChild('matInput', { static: true }) matInput: ElementRef;
	@ViewChild('matInput_Assign', { static: true }) matInput_Assign: ElementRef;
	ShowDrd_Project: boolean = false;
	constructor(
		public dialogRef: MatDialogRef<WorkEditComponent>,
		private FormControlFB: FormBuilder,
		public weworkService: WeWorkService,
		private workServices: WorkService,
		private translate: TranslateService,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		@Inject(MAT_DIALOG_DATA) public DATA: any,
	) { }
	ngOnInit() {
		this.ItemData = this.data.DATA;
		this.item = this.data._item;
		this.options = this.getOptions();
		this.options_assign = this.getOptions_Assign();
		this.workServices.WorkDetail(this.item.id_row).subscribe(res => {
			if (res && res.status == 1) {
				this.item = res.data;
				this.changeDetectorRefs.detectChanges();
			}
		});
		this.selected = this.item.Followers ? this.item.Followers : [];
		this.selected_Assign = this.item.Followers ? this.item.Followers : [];
		if (this.item.id_row > 0) {
			this.is_edit = true;
		}
		this.weworkService.list_account({}).subscribe(res => {
			if (res && res.status === 1) {
				this.listUser = res.data;
				this.options = this.getOptions();
				this.options_assign = this.getOptions_Assign();
			}
		});
		this.id_project_team = this.item.id_project_team == null ? 0 : this.item.id_project_team;
		if (this.id_project_team > 0)
			this.ShowDrd_Project = true;
		else
			this.ShowDrd_Project = false;
		this.BindList(this.id_project_team);
		this.UserInfo = JSON.parse(localStorage.getItem('UserInfo'));
		this.tinyMCE = tinyMCE;
		const filter: any = {};
		this.weworkService.lite_project_team_byuser("").subscribe(res => {
			this.disabledBtn = false;
			if (res && res.status === 1) {
				this.listProject = res.data;
				this.setUpDropSearchProject();
				this.changeDetectorRefs.detectChanges();
			};
		});
		this.createForm();
	}
	createForm() {
		if (this.item.id_row > 0) {
			this.itemForm = this.FormControlFB.group({
				title: ['' + this.item.title, Validators.required],
				id_project_team: ['' + this.id_project_team, Validators.required],
				id_group: [this.item.id_group, Validators.required],
				urgent: ['' + this.item.urgent],
				NoiDung: ['' + this.item.description],

			});
			// this.itemForm.controls["title"].markAsTouched();
			// this.itemForm.controls["id_project_team"].markAsTouched();
			// this.itemForm.controls["id_group"].markAsTouched();
			// this.itemForm.controls["urgent"].markAsTouched();

		}
		else {
			this.itemForm = this.FormControlFB.group({
				title: ['' + this.item.title, Validators.required],
				id_project_team: ['' + this.id_project_team, Validators.required],
				Tags: [this.listTag],
				id_group: ['' + this.item.id_group, Validators.required],
				NoiDung: ['' + this.item.description],
				FileName: [this.item.Attachments],
				deadline: ['' + this.item.deadline],
				id_milestone: ['' + this.item.id_milestone],
				urgent: ['' + (this.item.urgent == 0 ? 'false' : this.item.urgent)],
			});
			// this.itemForm.controls["title"].markAsTouched();
			// this.itemForm.controls["id_project_team"].markAsTouched();
			// this.itemForm.controls["deadline"].markAsTouched();
			// this.itemForm.controls["id_milestone"].markAsTouched();
			// this.itemForm.controls["Tags"].markAsTouched();
			// this.itemForm.controls["id_group"].markAsTouched();
			// this.itemForm.controls["urgent"].markAsTouched();
		}
		// this.itemForm.controls["id_project_team"].disable();
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
			let el = $event.currentTarget.offsetParent;
			var w = this.textEl.nativeElement.offsetWidth + 30;
			var h = this.textEl.nativeElement.offsetHeight + 30;
			this.myPopover.show();
			this.myPopover.top = el.offsetTop + h;
			this.myPopover.left = el.offsetLeft + w;
			this.changeDetectorRefs.detectChanges();
		}
		// else{
		// 	this.myPopover.hide();
		// }
	}
	click($event, vi = -1) {
		this.myPopover.hide();
		// this.myPopoverChild.hide();
	}
	ItemSelected(data) {
		this.selected.push(data);
		let i = this._Follower.lastIndexOf('@');
		this._Follower = this._Follower.substr(0, i) + '@' + data.username + ' ';
		this.myPopover.hide();
		// (<HTMLInputElement>this.matInput.nativeElement).focus();
		// (<HTMLTextAreaElement>document.getElementById("InputUser")).focus();
		this.changeDetectorRefs.detectChanges();
	}
	getKeyword_Assign() {
		let i = this._Assign.lastIndexOf('@');
		if (i >= 0) {
			let temp = this._Assign.slice(i);
			if (temp.includes(' '))
				return '';
			return this._Assign.slice(i);
		}
		return '';
	}
	getOptions_Assign() {
		var options_assign: any = {
			showSearch: false,
			keyword: this.getKeyword_Assign(),
			data: this.listUser.filter(x => this.selected_Assign.findIndex(y => x.id_nv == y.id_nv) < 0),
		};
		return options_assign;
	}

	click_Assign($event, vi = -1) {
		this.myPopoverChild.hide();
		// this.myPopover.hide();
	}
	onSearchChange_Assign($event) {
		if (this.selected_Assign.length > 0) {
			var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm
			var match = this._Assign.match(reg);
			if (match != null && match.length > 0) {
				let arr = match.map(x => x);
				this.selected_Assign = this.selected_Assign.filter(x => arr.includes('@' + x.username));
			} else {
				this.selected_Assign = [];
			}
		}
		this.options_assign = this.getOptions_Assign();
		if (this.options_assign.keyword) {
			let el = $event.currentTarget.offsetParent;
			var w = this.text_Assign.nativeElement.offsetWidth + 30;
			var h = this.text_Assign.nativeElement.offsetHeight + 30;
			this.myPopoverChild.show();
			this.myPopoverChild.top = el.offsetTop + h;
			this.myPopoverChild.left = el.offsetLeft + w;
			this.changeDetectorRefs.detectChanges();
		}
		// else{
		// 	this.myPopoverChild.hide();
		// }
	}
	ItemSelected_Assign(data) {
		this.selected_Assign = [];
		this.selected_Assign.push(data);
		// let i = this._Assign.lastIndexOf('@');
		this._Assign = '@' + data.username + ' ';
		this.myPopoverChild.hide();
		// (<HTMLInputElement>this.matInput_Assign.nativeElement).focus();
		this.changeDetectorRefs.detectChanges();
	}

	checkTags(value: any) {
		this.check_tags = value;
	}
	BindList(id_project: any) {

		this.weworkService.lite_tag(id_project).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listTag = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
		this.weworkService.lite_workgroup(id_project).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listType = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
		this.weworkService.lite_milestone(id_project).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.List_id_milestone = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
	}
	ngOnChanges() {
		this.ItemData = this.data.DATA;
		this.changeDetectorRefs.detectChanges();
	}
	setUpDropSearchProject() {
		this.projectFilterCtrl.setValue('');
		this.filterProject();
		this.projectFilterCtrl.valueChanges
			.pipe()
			.subscribe(() => {
				this.filterProject();
			});
	}
	protected filterProject() {
		if (!this.listProject) {
			return;
		}
		let search = this.projectFilterCtrl.value;
		if (!search) {
			this.filtereproject.next(this.listProject.slice());
			return;
		} else {
			search = search.toLowerCase();
		}
		// filter the banks
		this.filtereproject.next(
			this.listUser.filter(bank => bank.hoten.toLowerCase().indexOf(search) > -1)
		);
	}
	ngOnDestroy() {
		if (this.componentSubscriptions) {
			this.componentSubscriptions.unsubscribe();
		}
	}
	selectFile(index: number) {
		//this.indexDetail = this.Listbiuldview[index].indexOf(obj);
		this.indexItem = index;
		let f = document.getElementById("imgInpdd" + index);
		f.click();
	}
	onSubmit(withBack: boolean = false) {
		this.hasFormErrors = false;
		this.loadingAfterSubmit = false;
		const controls = this.itemForm.controls;
		/* check form */
		if (this.itemForm.controls.id_group.invalid) {
			this.itemForm.controls.id_group.setValue('NULL')
		}
		if (this.itemForm.invalid) {
			Object.keys(controls).forEach(controlName =>
				controls[controlName].markAsTouched()
			);
			this.hasFormErrors = true;
			this.layoutUtilsService.showActionNotification('Chưa nhập đủ trường thông tin bắt buộc!')
			return;
		}
		const update = this.prepare();
		if (update.id_row > 0) {
			this.Update(update, withBack);
		} else {
			this.Create(update, withBack);
		}
	}
	prepare(): WorkModel {

		const controls = this.itemForm.controls;
		const _item = new WorkModel();
		_item.id_row = this.item.id_row;
		_item.title = controls['title'].value;
		_item.description = controls['NoiDung'].value;
		_item.id_project_team = controls['id_project_team'].value;
		_item.id_group = controls['id_group'].value;
		if (_item.id_row > 0) {
			// _item.urgent = controls['urgent'].value;
			var khancap = controls['urgent'].value;
			if (khancap == "true" || khancap == true) {
				khancap = 1;
			}
			else if (khancap == "false" || khancap == false) {
				khancap = 0;
			}
			_item.urgent = khancap;
			_item.id_milestone = this.item.id_milestone;
			_item.Attachments = this.item.Attachments;
			// _item.Users = this.data._item.Followers;
			this.list_User.push(this.data._item.Followers);
			const _model = new UserInfoModel();
			_model.id_user = this.item.assign.id_nv;
			_model.loai = 1;
			this.list_User.push(_model);
			_item.Users = this.list_User;
			_item.Tags = this.item.Tags;
			_item.prioritize = this.item.prioritize;
		}
		else {
			_item.id_milestone = controls['id_milestone'].value;
			if (this.selected_Assign != undefined) {
				if (this.selected_Assign.length > 0) {
					this.listUser.map((item, index) => {
						let _true = this.selected_Assign.find(x => x.id_nv === item.id_nv);
						if (_true) {
							const _model = new UserInfoModel();
							_model.id_user = item.id_nv;
							_model.loai = 1;
							this.list_User.push(_model);
						}
					});
				}
			}
			if (this.selected != undefined) {
				if (this.selected.length > 0) {
					this.listUser.map((item, index) => {
						let _true = this.selected.find(x => x.id_nv === item.id_nv);
						if (_true) {
							const _model = new UserInfoModel();
							_model.id_user = item.id_nv;
							_model.loai = 2;
							this.list_User.push(_model);
						}
					});
				}
			}
			_item.Users = this.list_User;
			_item.deadline = controls['deadline'].value;
			this.check_tags.map((item, index) => {
				let ktc = this.listTag.find(x => x.id_row == item);
				if (ktc) {
					let tag = new WorkTagModel;
					tag.id_tag = ktc.id_row;
					this.listTags.push(tag);
				}
			});
			_item.Tags = this.listTags;
			_item.Attachments = this.AttachFileComment;
		}
		return _item;
	}

	close() {
		this.dialogRef.close();
	}

	Update(_item: WorkModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this.workServices.UpdateWork(_item).subscribe(res => {
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
	Create(_item: WorkModel, withBack: boolean) {
		// var user = new UserInfoModel();
		// user.id_user = + this._Assign;
		// user.loai = 1;
		// _item.Users.push(user);
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this.workServices.InsertWork(_item).subscribe(res => {
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


	selectFile_PDF(ind) {
		if (ind == -1) {
			let f = document.getElementById("PDFInpdd");
			f.click();
		}
		else {
			let f = document.getElementById("PDFInpdd" + ind);
			f.click();
		}

	}

	onSelectFile_PDF(event, ind) {
		// event.target.type='text';
		// event.target.type='file';
		if (event.target.files && event.target.files[0]) {
			var filesAmount = event.target.files[0];
			var Strfilename = filesAmount.name.split('.');
			// if (Strfilename[Strfilename.length - 1] != 'docx' && Strfilename[Strfilename.length - 1] != 'doc') {
			// 	this.layoutUtilsService.showInfo("File không đúng định dạng");
			// 	return;
			// }
			if (ind == -1) {
				for (var i = 0; i < this.AttachFileComment.length; i++) {
					if (filesAmount.name == this.AttachFileComment[i].filename) {
						this.layoutUtilsService.showInfo(this.translate.instant('notify.filedatontai'));
						return;
					}
				}
			}
			else {
				for (var i = 0; i < this.ListAttachFile[ind].length; i++) {
					if (filesAmount.name == this.ListAttachFile[ind][i].filename) {
						this.layoutUtilsService.showInfo(this.translate.instant('notify.filedatontai'));
						return;
					}
				}
			}

			event.target.type = 'text';
			event.target.type = 'file';
			var reader = new FileReader();
			//this.FileAttachName = filesAmount.name;
			let base64Str: any;
			reader.onload = (event) => {
				base64Str = event.target["result"]
				var metaIdx = base64Str.indexOf(';base64,');
				base64Str = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64

				//this.FileAttachStrBase64 = base64Str;
				if (ind == -1) {
					this.AttachFileComment.push({ filename: filesAmount.name, strBase64: base64Str });
					this.changeDetectorRefs.detectChanges();
				}
				else {
					this.ListAttachFile[ind].push({ filename: filesAmount.name, strBase64: base64Str });
					this.changeDetectorRefs.detectChanges();
				}
			}

			reader.readAsDataURL(filesAmount);

		}
	}
	DeleteFile_PDF(ind, ind1) {
		//this.ListAttachFile[ind].push({filename:filesAmount.name,StrBase64:base64Str});
		if (ind == -1) {
			this.AttachFileComment.splice(ind1, 1);
		}
		else {
			this.ListAttachFile[ind].splice(ind1, 1);
		}
	}

}
