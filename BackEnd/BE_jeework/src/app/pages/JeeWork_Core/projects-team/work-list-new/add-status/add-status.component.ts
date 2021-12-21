import { LayoutUtilsService, MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { ProjectsTeamService } from './../../Services/department-and-project.service';
import { StatusDynamicModel } from './../../Model/status-dynamic.model';
import { ProjectTeamUserModel } from './../../Model/department-and-project.model';
import { PopoverContentComponent } from 'ngx-smart-popover';
import { ReplaySubject } from 'rxjs';
import { ProjectTeamModel } from './../../Model/department-and-project.model';
import { Router } from '@angular/router';
import { WeWorkService } from './../../../services/wework.services';
import { TranslateService } from '@ngx-translate/core';
import { FormBuilder, FormGroup, Validators, AbstractControl, FormControl } from '@angular/forms';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatAccordion } from '@angular/material/expansion';
import { Component, OnInit, Inject, ChangeDetectorRef, ViewChild, ElementRef, HostListener } from '@angular/core';
import { MatStepper } from '@angular/material/stepper';

@Component({
	selector: 'kt-add-status',
	templateUrl: './add-status.component.html',
	styleUrls: ['./add-status.component.scss']
})
export class AddStatusComponent implements OnInit {
	isLinear = false;
	iconaddnew = "https://img.icons8.com/fluent/96/000000/add-image.png";
	firstFormGroup: FormGroup;
	secondFormGroup: FormGroup;
	item: ProjectTeamModel;
	oldItem: ProjectTeamModel
	itemForm: FormGroup;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	// @ViewChild("focusInput", { static: true }) focusInput: ElementRef;
	disabledBtn: boolean = false;
	IsEdit: boolean;
	IsProject: boolean;
	title: string = '';
	Id_parent: string = '';
	listUser: any[] = [];
	listChecked: any[] = [];
	filter: any = {};
	itemsAsObjects = [{ id: 0, name: 'Angular', readonly: true }, { id: 1, name: 'React' }];
	colorCtr: AbstractControl = new FormControl(null);
	tendapb: string = '';
	mota: string = '';
	public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public projectFilterCtrl: FormControl = new FormControl();
	listdepartment: any[] = [];
	@ViewChild('follower', { static: true }) myPopover: PopoverContentComponent;
	@ViewChild('Assign', { static: true }) myPopover_Assign: PopoverContentComponent;
	selected: any[] = [];
	selected_Assign: any[] = [];
	@ViewChild('hiddenText', { static: true }) textEl: ElementRef;
	@ViewChild('hiddenText_Assign', { static: true }) text_Assign: ElementRef;
	ListFollower: string = '';
	list_follower: any[] = [];
	list_Assign: any[] = [];
	options: any = {};
	options_assign: any = {};
	_color: string = '';
	_Follower: string = '';
	_Assign: string = '';
	id_project_team = 0;
	nextStep1 = false;
	ProjectID = 0;
	linear = true;
	accordionclose: boolean = true;
	@ViewChild(MatAccordion, { static: true }) accordion: MatAccordion;
	//icon
	icon: any = {};
	constructor(public dialogRef: MatDialogRef<AddStatusComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: ProjectsTeamService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: WeWorkService,
		private _formBuilder: FormBuilder,
		private router: Router,) { }
	color_status = '';
	statusname = '';
	statusdesp = '';
	listStatus: any = [];
	ngOnInit() {
		this.firstFormGroup = this._formBuilder.group({
			firstCtrl: ['', Validators.required]
		});
		this.secondFormGroup = this._formBuilder.group({
			secondCtrl: ['', Validators.required]
		});

		this.title = this.translate.instant("GeneralKey.choncocautochuc") + '';
		this.item = this.data._item;
		this.IsEdit = this.data._IsEdit;
		this.IsProject = this.data.is_project;
		this.id_project_team = this.data.id_project_team;
		if (this.IsProject) {
			this.tendapb = this.translate.instant("projects.tenduan") + '';
			this.mota = this.translate.instant("projects.motangangonveduan") + '';
		}
		else {
			this.tendapb = this.translate.instant("projects.phongban") + '';
			this.mota = this.translate.instant("projects.motangangonvephongban") + '';
		}

		if (this.item.Users == undefined)
			this.item.Users = this.item.users;
		this.list_follower = this.item.Users.filter(x => x.admin);
		for (let i = 0; i < this.list_follower.length; i++) {
			this._Follower += ' @' + this.list_follower[i]['username'];
		};
		// this.selected.push(this.item.Users.filter(x => x.admin));
		this.selected = this.item.Users.filter(x => x.admin);
		this._Follower = this._Follower.substring(1);
		this.list_Assign = this.item.Users.filter(x => !x.admin);
		for (let i = 0; i < this.list_Assign.length; i++) {
			this._Assign += ' @' + this.list_Assign[i]['username'];
		};

		this.selected_Assign = this.item.Users.filter(x => !x.admin);
		this._Assign = this._Assign.substring(1);
		this.weworkService.lite_department().subscribe(res => {
			this.disabledBtn = false;
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listdepartment = res.data;
				this.setUpDropSearchProject();
				this.changeDetectorRefs.detectChanges();
			};
		});
		this.createForm();
		const filter: any = {};
		if (this.item.id_row > 0) {
			this.viewLoading = true;
			// filter.key = 'cocauid';
			// filter.value = this.item.id_department;
			this._service.Detail(this.item.id_row).subscribe(res => {
				if (res && res.status == 1) {
					this.item = res.data;
					this.createForm();
				}
				else
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
			});
		}
		filter.cocauid = this.item.id_department;
		this.weworkService.list_account(filter).subscribe(res => {
			this.disabledBtn = false;
			if (res && res.status === 1) {
				this.listUser = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
	}

	getIcon() {
		if (this.icon.src)
			return this.icon.src;
		else if (this.icon.base64Str) {
			return this.icon.base64Str;
		}
		return this.iconaddnew;
	}

	createForm() {
		this.icon = {};
		if (this.item.icon)
			this.icon.src = this.item.icon;
		else {
			this.icon.src = "https://img.icons8.com/fluent/48/000000/add-image.png";
		}
		this.itemForm = this.fb.group({
			id_project_team: ['' + this.id_project_team, Validators.required],
			title: ['' + this.item.title, Validators.required],
			description: [this.item.description],
			loai: ['' + this.item.loai, Validators.required],
			status: ['' + this.item.status],
			start_date: [this.item.start_date],
			end_date: [this.item.end_date],
			color: [this.item.color],
		});
	}
	collapse() {
		if (this.accordionclose)
			this.accordion.openAll();
		else
			this.accordion.closeAll();
		this.accordionclose = !this.accordionclose;
	}
	Selected_Color(event): any {
		this._color = event.value;
		this.item.color = event;
		this._color = event;
	}
	f_convertDateTime(date: string) {
		var componentsDateTime = date.split("/");
		var date = componentsDateTime[0];
		var month = componentsDateTime[1];
		var year = componentsDateTime[2];
		var formatConvert = year + "-" + month + "-" + date + "T00:00:00.0000000";
		return new Date(formatConvert);
	}
	f_convertDate(p_Val: any) {
		if (p_Val == null)
			return '1/1/0001';
		let a = p_Val === "" ? new Date() : new Date(p_Val);
		return a.getFullYear() + "/" + ("0" + (a.getMonth() + 1)).slice(-2) + "/" + ("0" + (a.getDate())).slice(-2);
	}
	getKeyword() {
		let i = this.ListFollower.lastIndexOf('@');
		if (i >= 0) {
			let temp = this.ListFollower.slice(i);
			if (temp.includes(' '))
				return '';
			return this.ListFollower.slice(i);
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
		this.ListFollower = (<HTMLInputElement>document.getElementById("InputUser")).value;
		if (this.selected.length > 0) {
			var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm
			var match = this.ListFollower.match(reg);
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
			var ele = (<HTMLInputElement>document.getElementById("inputfollower"));
			var h = ele.offsetTop + 280;
			this.myPopover.show();
			this.myPopover.top = el.offsetTop + h;
			this.myPopover.left = el.offsetLeft + 350;
			this.changeDetectorRefs.detectChanges();
		}
	}
	click($event, vi = -1) {
		this.myPopover.hide();
	}
	ItemSelected(data) {
		this.selected = this.list_follower;
		this.selected.push(data);
		let i = this.ListFollower.lastIndexOf('@');
		this.ListFollower = this.ListFollower.substr(0, i) + '@' + data.username + ' ';
		this.myPopover.hide();
		let ele = (<HTMLInputElement>document.getElementById("InputUser"));
		ele.value = this.ListFollower;
		ele.focus();
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
		this.myPopover_Assign.hide();
	}
	onSearchChange_Assign($event) {
		this._Assign = (<HTMLInputElement>document.getElementById("InputAssign")).value;

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
			let el = $event.currentTarget;
			let rect = el.getBoundingClientRect();
			var ele = (<HTMLInputElement>document.getElementById("inputfollower"));
			var h = ele.offsetTop + 280;
			this.myPopover_Assign.show();
			this.myPopover_Assign.top = el.offsetTop + h;
			this.myPopover_Assign.left = el.offsetLeft + 1000;
			this.changeDetectorRefs.detectChanges();
		}
	}
	ItemSelected_Assign(data) {
		this.selected_Assign = this.list_Assign;
		this.selected_Assign.push(data);
		let i = this._Assign.lastIndexOf('@');
		this._Assign = this._Assign.substr(0, i) + '@' + data.username + ' ';
		this.myPopover_Assign.hide();
		let ele = (<HTMLInputElement>document.getElementById("InputAssign"));
		ele.value = this._Assign;
		ele.focus();
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
		if (!this.listdepartment) {
			return;
		}
		let search = this.projectFilterCtrl.value;
		if (!search) {
			this.filtereproject.next(this.listdepartment.slice());
			return;
		} else {
			search = search.toLowerCase();
		}
		this.filtereproject.next(
			this.listdepartment.filter(bank => bank.title.toLowerCase().indexOf(search) > -1)
		);
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
	/** ACTIONS */
	prepare(): ProjectTeamModel {
		const controls = this.itemForm.controls;
		const _item = new ProjectTeamModel();
		_item.id_row = this.item.id_row;
		_item.id_department = controls['id_project_team'].value;
		_item.title = controls['title'].value;
		_item.description = controls['description'].value;
		_item.loai = controls['loai'].value;
		_item.color = controls['color'].value;
		_item.status = controls['status'].value;
		_item.start_date = this.f_convertDate(controls['start_date'].value);
		_item.end_date = this.f_convertDate(controls['end_date'].value);
		_item.is_project = this.IsProject;
		if (this.icon.strBase64) {
			_item.icon = this.icon;
		}
		this.selected.map((item, index) => {
			let ktc = this.listUser.find(x => x.id_nv == item.id_nv && item.admin);
			if (ktc) {
				const model = new ProjectTeamUserModel;
				model.id_user = ktc.id_nv;
				model.admin = true;
				model.id_row = item.id_row;
				this.listChecked.push(model);
			}
			else {
				const model = new ProjectTeamUserModel();
				if (model.id_row == undefined)
					model.id_row = 0;
				model.id_user = item.id_nv;
				model.admin = true;
				this.listChecked.push(model);
			}
		});
		this.selected_Assign.map((item, index) => {
			let ktc = this.listUser.find(x => x.id_nv == item.id_nv && !item.admin);
			if (ktc) {
				const model = new ProjectTeamUserModel;
				model.id_user = ktc.id_nv;
				model.admin = false;
				model.id_row = item.id_row;
				this.listChecked.push(model);
			}
			else {
				const model = new ProjectTeamUserModel();
				if (model.id_row == undefined)
					model.id_row = 0;
				model.id_user = item.id_nv;
				model.admin = false;
				this.listChecked.push(model);
			}
		});
		_item.Users = this.listChecked;
		return _item;
	}
	filterConfiguration(): any {
		const filter: any = {};
		return filter;
	}
	Update(_item: ProjectTeamModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this._service.UpdateProjectTeam(_item).subscribe(res => {
			this.disabledBtn = false;
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				location.reload();
				this.dialogRef.close({
					_item,
				});
				const _messageType = this.translate.instant('GeneralKey.capnhatthanhcong');
				this.layoutUtilsService.showInfo(_messageType);
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		});
	}

	Create(_item: ProjectTeamModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this._service.InsertProjectTeam(_item).subscribe(res => {
			this.disabledBtn = false;
			if (res && res.status == 1) {
				this.ProjectID = res.data.id_row;
				this.nextStep1 = true;
				this.linear = false;
				this.itemForm.disable();
				this.ListStatusDynamic();
				const _messageType = this.translate.instant('GeneralKey.themthanhcong');
				this.layoutUtilsService.showInfo(_messageType);
				// var el = getElement
				setTimeout(() => {
					let ele = (<HTMLButtonElement>document.getElementById("btnnextStep"));
					ele.click();
				}, 10);
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
	close(type) {
		if (type == 2) {
			var text = "/project/" + this.ProjectID + "/settings/acl";
			this.router.navigateByUrl(text).then(() => {
				this.dialogRef.close();
			})
		} else {
			var text = "/project/" + this.ProjectID;
			this.router.navigateByUrl(text).then(() => {
				this.dialogRef.close();
			})
		}
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
	chooseFile() {
		let f = document.getElementById("inputIcon");
		f.click();
	}
	onSelectFile(event) {
		if (event.target.files && event.target.files[0]) {
			var filesAmount = event.target.files[0];
			var Strfilename = filesAmount.name.split('.');

			event.target.type = 'text';
			event.target.type = 'file';
			var reader = new FileReader();
			let base64Str: any;
			reader.onload = (event) => {
				base64Str = event.target["result"]
				var metaIdx = base64Str.indexOf(';base64,');
				let strBase64 = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64
				this.icon = { filename: filesAmount.name, strBase64: strBase64, base64Str: base64Str };
				this.changeDetectorRefs.detectChanges();
			}
			reader.readAsDataURL(filesAmount);
		}
	}

	ColorPickerStatus(val) {
		setTimeout(() => {
			this.color_status = val;
		}, 10);
	}

	AddStatus() {
		const item = new StatusDynamicModel();
		item.clear();
		item.StatusName = this.statusname;
		item.Color = this.color_status;
		item.Description = this.statusdesp;
		item.Id_project_team = this.ProjectID;
		item.Type = '1';
		this._service.InsertStatus(item).subscribe(res => {
			if (res && res.status == 1) {
				this.resetdata();
				this.ListStatusDynamic();
			}
		})
	}

	resetdata() {
		// this.color_status = 'rgb(187, 181, 181)';
		this.color_status = '#848E9E';
		this.statusname = '';
		this.statusdesp = '';
	}

	ListStatusDynamic() {
		this.weworkService.ListStatusDynamic(this.ProjectID).subscribe(res => {
			if (res && res.status == 1) {
				this.listStatus = res.data;
				this.changeDetectorRefs.detectChanges();
			}
		})
	}

}

