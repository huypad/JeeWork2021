import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service'

// import { AngularEditorConfig } from '@kolkov/angular-editor';
import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { ListDepartmentService } from '../Services/List-department.service';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service'; import { DepartmentModel, MilestoneModel } from '../Model/List-department.model';
import { JeeWorkLiteService } from '../../services/wework.services';
// import { startWith, map } from 'rxjs/operators';
@Component({
	selector: 'kt-milestone-detail-edit',
	templateUrl: './milestone-detail-edit.component.html',
})
export class milestoneDetailEditComponent implements OnInit {
	item: any = {};
	oldItem: MilestoneModel;
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
	title: string = '';
	selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
	ID_Struct: string = '';
	Id_parent: string = '';
	filteredStates: Observable<any[]>;
	stateCtrl: FormControl;
	id_project_team: string = '';
	listProject: any;
	isChangeTeam = true;
	reloadPage = true;
	constructor(public dialogRef: MatDialogRef<milestoneDetailEditComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private ObjectService: ListDepartmentService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: JeeWorkLiteService,
		private router: Router,) {
	}
	/** LOAD DATA */
	ngOnInit() {
		this.item = this.data._item;
		if (this.data.reloadPage) {
			this.reloadPage = this.data.reloadPage;
		}
		if (this.data._item) {
			if (this.item.id_row > 0) {
				this.viewLoading = true;
			}
			else {
				this.viewLoading = false;
			}
			if (this.data._item.id_project_team > 0) {
				this.ChangeProject(this.data._item.id_project_team);
				this.isChangeTeam = false;
			}
			else {
				this.isChangeTeam = true;
			}
			if (this.item.id_department > 0) {
				this.weworkService.lite_project_team_bydepartment(this.item.id_department).subscribe(res => {
					if (res && res.status === 1) {
						this.listProject = res.data;
					};
				});

			} else {
				this.weworkService.lite_project_team_byuser("").subscribe(res => {
					if (res && res.status === 1) {
						this.listProject = res.data;
					};
				});
			}
			this.createForm();
		}
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
			title: ['' + this.item.title, Validators.required],
			milestonedate: ['' + this.item.deadline, Validators.required],
			id_project_team: ['' + this.item.id_project_team],
			description: [this.item.description],
			pic: [this.item.person_in_charge.id_nv, Validators.required],
		});
		// this.itemForm.controls["title"].markAsTouched();
		// this.itemForm.controls["milestonedate"].markAsTouched();
		// this.itemForm.controls["pic"].markAsTouched();
		// this.itemForm.controls["id_project_team"].markAsTouched();
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
	prepare(): MilestoneModel {
		const controls = this.itemForm.controls;
		const _item = new MilestoneModel();
		_item.id_row = this.item.id_row;
		_item.title = controls['title'].value;
		_item.deadline = controls['milestonedate'].value;
		_item.person_in_charge = controls['pic'].value;
		_item.description = controls['description'].value;
		_item.id_project_team = this.item.id_project_team;
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
			this.layoutUtilsService.showError('Thông tin các trường dữ liệu là bắt buộc');
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

	Update(_item: MilestoneModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this.ObjectService.UpdatMilestone(_item).subscribe(res => {
			/* Server loading imitation. Remove this on real code */
			this.disabledBtn = false;
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
	Create(_item: MilestoneModel, withBack: boolean) {
		_item.id_project_team = +this.itemForm.controls.id_project_team.value;
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		const _messageType = this.translate.instant('GeneralKey.themthanhcong');
		this.ObjectService.InsertMilestone(_item).subscribe(res => {
			this.disabledBtn = false;
			if (res && res.status === 1) {
				this.dialogRef.close(_item);
			}
			else {
				this.viewLoading = false;
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
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

	ChangeProject(id_team) {
		const filter: any = {};
		filter.id_project_team = id_team;
		this.weworkService.list_account(filter).subscribe(res => {
			if (res && res.status === 1) {
				this.listUser = res.data;
				this.setUpDropSearchNhanVien();
			};
			this.changeDetectorRefs.detectChanges();
		});
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
