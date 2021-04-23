import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { QueryParamsModelNew } from './../../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { MenuPhanQuyenServices } from './../../../../../_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service';
import { SubheaderService } from './../../../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import { LayoutUtilsService,MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { StatusDynamicModel } from './../../Model/status-dynamic.model';
import { StatusDynamicDialogComponent } from './../../../status-dynamic/status-dynamic-dialog/status-dynamic-dialog.component';
import { ProjectsTeamService } from './../../Services/department-and-project.service';
import { AttachmentModel, FileUploadModel } from './../../Model/department-and-project.model';
import { CheckListEditComponent } from './../../../work/check-list-edit/check-list-edit.component';
import { UpdateByKeyModel, ChecklistModel, ChecklistItemModel } from './../../../update-by-keys/update-by-keys.model';
import { MatDialog } from '@angular/material/dialog';
import { WeWorkService } from './../../../services/wework.services';
import { AttachmentService } from './../../../services/attachment.service';
import { UpdateByKeyService } from './../../../update-by-keys/update-by-keys.service';
import { DatePipe } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { WorkService } from './../../../work/work.service';
import { PopoverContentComponent } from 'ngx-smart-popover';
import { WorkModel, UserInfoModel, UpdateWorkModel } from './../../../work/work.model';
import { BehaviorSubject, ReplaySubject } from 'rxjs';
import { FormGroup, FormControl, FormBuilder } from '@angular/forms';
import { DialogData } from './../../../report/report-tab-dashboard/report-tab-dashboard.component';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Component, OnInit, Inject, ViewChild, ElementRef, ChangeDetectorRef, OnChanges } from '@angular/core';
import * as moment  from 'moment';
import { UpdateByKeysComponent } from './../../../update-by-keys/update-by-keys-edit/update-by-keys-edit.component';
@Component({
	selector: 'kt-work-list-new-detail',
	templateUrl: './work-list-new-detail.component.html',
	styleUrls: ['./work-list-new-detail.component.scss']
})
export class WorkListNewDetailComponent implements OnInit {

	selectedItem: any = undefined;
	itemForm: FormGroup;
	loadingSubject = new BehaviorSubject<boolean>(false);
	loadingControl = new BehaviorSubject<boolean>(false);
	loading1$ = this.loadingSubject.asObservable();
	hasFormErrors: boolean = false;
	item: any = {};
	oldItem: WorkModel;
	item_User: UserInfoModel;
	item_file: UserInfoModel;
	// itemHD: HopDongModel;
	// oldItemHD: HopDongModel;
	selectedTab: number = 0;
	//========================================================
	Visible: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	listUser: any[] = [];
	FormControls: FormGroup;
	disBtnSubmit: boolean = false;
	isChange: boolean = false;
	UserInfo: any = {};
	data: any;
	SubTask: string = '';
	deadline: any;
	listColumn: any[] = [];
	IsShow_MoTaCV: boolean = false;
	IsShow_CheckList: boolean = false;
	IsShow_Result: boolean = false;
	MoTaCongViec: string = '';
	checklist: string = '';
	Result: string = '';
	CheckList: any[] = [];
	Value: string = '';
	Key: string = '';
	ItemFinal = 0;
	description: string = '';
	checklist_item: string = '';
	Id_project_team: number = 0;
	public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public bankFilterCtrl: FormControl = new FormControl();
	listType: any[] = [];
	DataID: number = 0;
	AssignTask: any = [];
	AssignChecklist: any = [];
	list_priority: any = [];
	options_assign: any = {};
	@ViewChild('Assign', { static: true }) myPopover_Assign: PopoverContentComponent;
	selected_Assign: any[] = [];
	@ViewChild('hiddenText_Assign', { static: true }) text_Assign: ElementRef;
	_Assign: string = '';
	list_Assign: any[] = [];
	status_dynamic: any[] = [];
	LogDetail: any[] = [];
	list_role: any[] = [];
	text_item = "";
	valueFocus = "";
	customStyle = [];
	UserID = 0;
	showChucnang = false;
	constructor(private _service: WorkService,
		private el: ElementRef,
		private ProjectsTeamService: ProjectsTeamService,
		private danhMucService: DanhMucChungService,
		public dialog: MatDialog,
		private itemFB: FormBuilder,
		public subheaderService: SubheaderService,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private translate: TranslateService,
		public datepipe: DatePipe,
		private activatedRoute: ActivatedRoute,
		public weworkService: WeWorkService,
		private updatebykeyService: UpdateByKeyService,
		private _attservice: AttachmentService,
		public dialogRef: MatDialogRef<WorkListNewDetailComponent>,
		@Inject(MAT_DIALOG_DATA) public datalog: DialogData,
		private menuServices: MenuPhanQuyenServices
	) {
		this.list_priority = this.weworkService.list_priority;
		this.UserID = +localStorage.getItem("idUser");
	}
	/** LOAD DATA */
	ngOnInit() {
		this.data = this.datalog;
		this.DataID = this.data.id_row;
		this.Id_project_team = this.data.id_project_team;
		this.UserInfo = JSON.parse(localStorage.getItem('UserInfo'));
		this.LoadData();
		this.LoadChecklist();
	}

	OnChanges() {
		// this.ngOnInit();
	}
	LoadChecklist() {
		const queryParams = new QueryParamsModelNew(
			this.filterConfiguration(),
			'',
			'',
			0,
			50,
			true
		);
		this._service.CheckList(queryParams).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				// res.data.forEach(element => {
				// 	element.isExpanded = false;
				// })
				this.CheckList = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
	};

	Refesh(val) {
		this.LoadData();
	}

	LoadChild(item) {
		this.DataID = item.id_row;
		this.LoadData();
		this.LoadChecklist();
	}
	LoadData() {
		this.mark_tag();
		// }
		this.weworkService.ListStatusDynamic(this.Id_project_team).subscribe(res => {
			if (res && res.status === 1) {
				this.status_dynamic = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		})
		
		setTimeout(x => {
			const filter: any = {};
			filter.id_project_team = this.Id_project_team;
			this.weworkService.list_account(filter).subscribe(res => {
				this.changeDetectorRefs.detectChanges();
				if (res && res.status === 1) {
					this.listUser = res.data;
					this.setUpDropSearchNhanVien();
					this.changeDetectorRefs.detectChanges();
				};
				this.options_assign = this.getOptions_Assign();
			});
		}, 500);
		this.ProjectsTeamService.WorkDetail(this.DataID).subscribe(res => {
			if (res && res.status == 1) {
				console.log(res.data);
				this.item = res.data;
				this.changeDetectorRefs.detectChanges();
			}
		});
		this.weworkService.lite_milestone(this.Id_project_team).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listType = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});

		//get LogDetailCU
		this.ProjectsTeamService.LogDetailCU(this.DataID).subscribe(res => {
			if (res && res.status === 1) {
				this.LogDetail = res.data;
				this.changeDetectorRefs.detectChanges();
			} else {
			}
		})

		// quyền
		this.menuServices.GetRoleWeWork('' + this.UserID).subscribe(res => {
			if (res)
				this.list_role = res.data;

		});
	}

	CheckRoles(roleID: number) {
		var x = this.list_role.find(x => x.id_row == this.Id_project_team);
		if (x) {
			if (x.admin == true) {
				return true;
			}
			else {
				if (x.Roles.find(r => r.id_role == 15))
					return false;
				var r = x.Roles.find(r => r.id_role == roleID);
				if (r) {
					return true;
				}
				else {
					return false;
				}
			}
		}
		else {
			return false;
		}
	}
	CheckRoleskeypermit(key) {
		var x = this.list_role.find(x => x.id_row == this.Id_project_team);
		if (x) {
			if (x.admin == true) {
				return true;
			}
			else {
				var r = x.Roles.find(r => r.keypermit == key);
				if (r) {
					return true;
				}
				else {
					return false;
				}
			}
		}
		else {
			return false;
		}
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
			showSearch: true,
			keyword: this.getKeyword_Assign(),
			data: this.listUser.filter(x => this.selected_Assign.findIndex(y => x.id_nv == y.id_nv) < 0),
		};
		return options_assign;
	}

	getColorStatus(val) {
		var index = this.status_dynamic.find(x => x.id_row == val);
		if (index) {
			return index.color;
		}
		else
			return 'gray';
	}

	getPriority(id) {
		if (id > 0) {
			var item = this.list_priority.find(x => x.value == id)
			if (item)
				return item.icon;
			return 'far fa-flag';
		} else {
			return 'far fa-flag'
		}

	}

	NextStatus(type) {
		const item = new UpdateWorkModel();
		item.id_row = this.item.id_row;
		item.key = 'status';
		item.value = this.item.status;
		item.status_type = type;
		
		if (this.item.assign != null) {
			if (this.item.assign.id_nv > 0) {
				item.IsStaff = true;
			}
		}
		this.ProjectsTeamService._UpdateByKey(item).subscribe(res => {
			if (res && res.status == 1) {
				this.LoadData();
			}
		})
	}

	bindStatus(val) {
		var stt = this.status_dynamic.find(x => +x.id_row == +val);
		if (stt) {
			return stt.statusname;
		}
		return this.translate.instant('GeneralKey.chuagantinhtrang');
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
			this.myPopover_Assign.show();
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

	filterConfiguration(): any {
		const filter: any = {};
		filter.id_work = this.DataID;
		return filter;
	}
	//type=1: comment, type=2: reply
	CommentInsert(e: any, Parent: number, ind: number, type: number) {

		// var objSave: any = {};
		// objSave.comment = e;
		// objSave.id_parent = Parent;
		// objSave.object_type = this.Loai;
		// objSave.object_id = this.Id;
		// if (type == 1) { objSave.Attachment = this.AttachFileComment; }
		// else { objSave.Attachments = this.ListAttachFile[ind]; }

		// this.service.getDSYKienInsert(objSave).subscribe(res => {
		// 	if (type == 1) { this.Comment = ''; this.AttachFileComment = []; }
		// 	else {
		// 		// var el = document.getElementById("CommentRep" + ind) as HTMLElement; 
		// 		// el.setAttribute('value', '');
		// 		(<HTMLInputElement>document.getElementById("CommentRep" + ind)).value = "";
		// 		this.ListAttachFile[ind] = [];
		// 	}
		// 	this.changeDetectorRefs.detectChanges();
		// 	//this.getDSYKien();
		// });
	}
	formatLabel(value: number) {
		if (value >= 1000) {
			return Math.round(value / 1000) + '%';
		}
		return value;
	}
	Update_MotaCongViec() {
		this.IsShow_MoTaCV = !this.IsShow_MoTaCV;
		this.MoTaCongViec = this.description;
	}
	Update_CheckList() {
		this.IsShow_CheckList = !this.IsShow_CheckList;
		this.checklist = '';
	}
	Update_Result() {
		this.IsShow_Result = !this.IsShow_Result;
		this.Result = this.Result;
	}
	download(path: any) {
		window.open(path);
	}
	Assign(val: any) {

		var model = new UpdateWorkModel();
		model.id_row = this.item.id_row;
		model.key = 'assign';
		model.value = val.id_nv;
		this._service.UpdateByKey(model).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status == 1) {
				this.layoutUtilsService.showActionNotification(this.translate.instant('GeneralKey.capnhatthanhcong'), MessageType.Read, 4000, true, false, 3000, 'top', 1);
			}
			else
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);

		});
	}
	Delete_Followers(val: any) {
		const _title = this.translate.instant('GeneralKey.xoa');
		const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
		const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
		const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			this._service.Delete_Followers(this.item.Id, val).subscribe(res => {
				if (res && res.status === 1) {
					this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false);
				}
				else {
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
				}
			});
		});
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
		var model = new UpdateWorkModel();
		model.id_row = this.item.id_row;
		model.key = 'status';
		model.value = val;
		this._service.UpdateByKey(model).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status == 1) {
				this.item.status = val;
				this.layoutUtilsService.showActionNotification(this.translate.instant('GeneralKey.capnhatthanhcong'), MessageType.Read, 4000, true, false, 3000, 'top', 1);
			}
			else
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);

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

	CreateTask(val) {
		this.ProjectsTeamService.InsertTask(val).subscribe(res => {
			if (res && res.status == 1) {
				// this.CloseAddnewTask(true);
				this.LoadData();
			}
		})
	}
	onAlertClose($event) {
		this.hasFormErrors = false;
	}
	//---------------------------------------------------------

	f_number(value: any) {
		return Number((value + '').replace(/,/g, ""));
	}

	f_currency(value: any, args?: any): any {
		let nbr = Number((value + '').replace(/,|-/g, ""));
		return (nbr + '').replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,");
	}
	textPres(e: any, vi: any) {
		if (isNaN(e.key)
			//&& e.keyCode != 8 // backspace
			//&& e.keyCode != 46 // delete
			&& e.keyCode != 32 // space
			&& e.keyCode != 189
			&& e.keyCode != 45
		) {// -
			e.preventDefault();
		}
	}
	checkDate(e: any, vi: any) {
		if (!((e.keyCode > 95 && e.keyCode < 106)
			|| (e.keyCode > 46 && e.keyCode < 58)
			|| e.keyCode == 8)) {
			e.preventDefault();
		}
	}
	checkValue(e: any) {
		if (!((e.keyCode > 95 && e.keyCode < 106)
			|| (e.keyCode > 47 && e.keyCode < 58)
			|| e.keyCode == 8)) {
			e.preventDefault();
		}
	}
	f_convertDate(v: any) {
		if (v != "" && v != null) {
			let a = new Date(v);
			return a.getFullYear() + "-" + ("0" + (a.getMonth() + 1)).slice(-2) + "-" + ("0" + (a.getDate())).slice(-2) + "T00:00:00.0000000";
		}
	}

	f_date(value: any): any {
		if (value != "" && value != null && value != undefined) {
			let latest_date = this.datepipe.transform(value, 'dd/MM/yyyy');
			return latest_date;
		}
		return "";
	}
	refreshData() {
		this._service.WorkDetail(this.item.id_row).subscribe(res => {
			if (res && res.status == 1) {
				this.ngOnInit();
				this.changeDetectorRefs.detectChanges();
			}
		});
	}
	ThemCot() {
		let item = {
			RowID: 0,
			FieldName: "",
			Title: "",
			KeyValue: "",
			TypeID: "",
			Formula: "",
			ShowFomula: false,
			Priority: -1,
			IsEdit: true,
			Width: "",
			FormatID: "1",
			IsColumnHide: false,
		};
		this.listColumn.splice(0, 0, item);
		this.changeDetectorRefs.detectChanges();
	}
	Get_ValueByKey(key: string): string {
		switch (key) {
			case '16':
				return this.Value = this.MoTaCongViec;
			case '0':
				return this.Value = this.checklist;
		}
		return '';
	}

	updatePriority(value) {
		this.UpdateByKeyNew(this.item, 'clickup_prioritize', value);
	}

	UpdateByKey(id_log_action: string, key: string, IsQuick: boolean) {
		this.Get_ValueByKey(id_log_action);
		let model = new UpdateByKeyModel();
		if (IsQuick) {
			model.id_row = this.item.id_row;
			model.id_log_action = id_log_action;
			model.value = this.Value;
			model.key = key;
			if (id_log_action == '0') {
				let checklist_model = new ChecklistModel();
				checklist_model.id_work = this.item.id_row;
				checklist_model.title = this.Value;

				this.updatebykeyService.Insert_CheckList(checklist_model).subscribe(res => {
					this.changeDetectorRefs.detectChanges();
					if (res && res.status === 1) {
						this.IsShow_CheckList = !this.IsShow_CheckList;
						this.refreshData();
						this.changeDetectorRefs.detectChanges();
					}
					else {
						this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
					}
				});
			}
			else {
				this.updatebykeyService.UpdateByKey(model).subscribe(res => {
					this.changeDetectorRefs.detectChanges();
					if (res && res.status === 1) {
						this.IsShow_MoTaCV = !this.IsShow_MoTaCV;
						this._service.WorkDetail(model.id_row).subscribe(res => {
							if (res && res.status == 1) {
								this.description = res.data.description;
								this.refreshData();
								// this.checklist = res.data.description;
								this.ngOnInit();
								this.changeDetectorRefs.detectChanges();
							}
						});
						this.changeDetectorRefs.detectChanges();
					}
					else {
						this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
					}
				});
			}

		}
		else {
			model.clear(); // Set all defaults fields
			this.UpdateKey(model);
		}
	}

	//update by key click up

	UpdateStatus(task, status) {

		if (task.status == +status.id_row) {
			// 
		} else {
			// var taskupdate = new WorkModel();
			// taskupdate = task;
			// task.status = status.id_row
			// this.UpdateTask(task);
			this.UpdateByKeyNew(task, 'status', status.id_row);
		}
	}

	UpdateTitle() {
		if (this.valueFocus.trim() != this.item.title.trim()) {
			this.UpdateByKeyNew(this.item, 'title', this.item.title);
			this.setTextinput(this.item.title);
		}
	}
	UpdateDescription() {
		if (this.valueFocus.trim() != this.item.description.trim()) {
			this.UpdateByKeyNew(this.item, 'description', this.item.description);
			this.setTextinput(this.item.description);
		}
	}

	UpdateByKeyNew(task, key, value) {
		const item = new UpdateWorkModel();
		item.id_row = task.id_row;
		item.key = key;
		item.value = value;
		if (task.assign && task.assign.id_nv > 0) {
			item.IsStaff = true;
		}
		this.ProjectsTeamService._UpdateByKey(item).subscribe(res => {
			if (res && res.status == 1) {
				this.LoadData();
			}
		})

	}

	UpdateKey(_item: UpdateByKeyModel) {
		let saveMessageTranslateParam = '';
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(UpdateByKeysComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {

				this.refreshData();
				this.ngOnInit();
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.ngOnInit();
			}
		});
		this.refreshData();
	}
	UpdateCheckList() {
		let model = new UpdateByKeyModel();
		model.clear(); // Set all defaults fields
		this.UpdateKey(model);
	}
	_UpdateCheckList(_item: ChecklistModel) {
		let saveMessageTranslateParam = '';
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(CheckListEditComponent, { data: { _item, IsCheckList: true } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.refreshData();
				this.ngOnInit();
				this.changeDetectorRefs.detectChanges();
				return;
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
			}
		});
		this.refreshData();
	}


	updateNewChecklist(_item) {
		const item = new ChecklistModel();
		item.id_row = _item.id_row;
		item.title = _item.title;
		this._service.Update_CheckList(item).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				// this.layoutUtilsService.showActionNotification('Update thành công', MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				this.LoadChecklist();
			}
		});
	}
	Update_CheckList_Item(_item, id_checklist) {
		const item = new ChecklistItemModel();
		item.id_row = _item.id_row;
		item.title = _item.title;
		item.id_checklist = id_checklist;
		item.checker = _item.checker ? 1 : 0;
		this._service.Update_CheckList_Item(item).subscribe(res => {
			if (res && res.status === 1) {
				// this.layoutUtilsService.showActionNotification('Update thành công', MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				this.LoadChecklist();
			}
			this.changeDetectorRefs.detectChanges();
		});
	}


	UpdateCheckList_Item(_item: ChecklistModel) {
		let saveMessageTranslateParam = '';
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(CheckListEditComponent, { data: { _item, IsCheckList: false } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.refreshData();
				return;
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
			}
		});
	}
	InsertCheckListItem(id_checkList: number) {
		var text = (<HTMLInputElement>document.getElementById("checklist" + id_checkList)).value;
		let model = new ChecklistItemModel();
		model.id_checklist = id_checkList;
		model.title = text; //item.id_row
		this.updatebykeyService.Insert_CheckList_Item(model).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.LoadChecklist();
				this.changeDetectorRefs.detectChanges();
				(<HTMLInputElement>document.getElementById("checklist" + id_checkList)).value = '';
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		});
	}
	deletext(id_checkList) {
		var text = (<HTMLInputElement>document.getElementById("checklist" + id_checkList));
		if (text && text.value != '')
			return true;
		return false;
	}
	hidebuttondelete(id_checkList) {
		(<HTMLInputElement>document.getElementById("checklist" + id_checkList)).value = '';
	}
	Delete_CheckList(_item: ChecklistModel) {
		const _title = this.translate.instant('GeneralKey.xoa');
		const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
		const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
		const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.refreshData();
				return;
			}

			this._service.Delete_CheckList(_item.id_row).subscribe(res => {
				if (res && res.status === 1) {
					this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
					this.LoadChecklist();
				}
				else {
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				}
			});
		});
		this.refreshData();
	}
	DeleteItem(_item: ChecklistItemModel) {
		const _title = this.translate.instant('GeneralKey.xoa');
		const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
		const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
		const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.refreshData();
				return;
			}
			this._service.DeleteItem(_item.id_row).subscribe(res => {
				if (res && res.status === 1) {
					this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
					this.LoadChecklist();
				}
				else {
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				}
			});
		});
		this.refreshData();
	}


	TenFile: string = '';
	File: string = '';
	filemodel: any;
	@ViewChild('csvInput', { static: true }) myInputVariable: ElementRef;
	@ViewChild('resultInput', { static: true }) result: ElementRef;

	save_file_Direct(evt: any, type: string) {

		if (evt.target.files && evt.target.files.length) {//Nếu có file	
			var size = evt.target.files[0].size;
			if (size / 1024 / 1024 > 3) {
				this.layoutUtilsService.showActionNotification("File upload không được vượt quá 3 MB", MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				return;
			}
			let file = evt.target.files[0]; // Ví dụ chỉ lấy file đầu tiên
			this.TenFile = file.name;
			let reader = new FileReader();
			reader.readAsDataURL(evt.target.files[0]);
			let base64Str;
			setTimeout(() => {
				base64Str = reader.result as String;
				var metaIdx = base64Str.indexOf(';base64,');
				base64Str = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64
				this.File = base64Str;
				var _model = new AttachmentModel;
				_model.object_type = parseInt(type);
				_model.object_id = this.item.id_row;
				const ct = new FileUploadModel();
				ct.strBase64 = this.File;
				ct.filename = this.TenFile;
				ct.IsAdd = true;
				_model.item = ct;
				this.loadingAfterSubmit = true;
				this.viewLoading = true;
				this._attservice.Upload_attachment(_model).subscribe(res => {
					this.changeDetectorRefs.detectChanges();
					if (res && res.status === 1) {
						const _messageType = this.translate.instant('GeneralKey.capnhatthanhcong');
						this.layoutUtilsService.showActionNotification(_messageType, MessageType.Update, 4000, true, false).afterDismissed().subscribe(tt => {
						});
						this.LoadData();
					}
					else {
						this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
					}
				});

			}, 50);
		} else {
			this.File = "";
		}
	}
	Delete_File(val: any) {
		const _title = this.translate.instant('GeneralKey.xoa');
		const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
		const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
		const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			this._attservice.delete_attachment(val).subscribe(res => {
				if (res && res.status === 1) {
					this.ngOnInit();
					this.changeDetectorRefs.detectChanges();
					this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false);
				}
				else {
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
				}
			});
		});
	}

	list_User: any[] = [];
	Insert_SubTask() {
		let model = new WorkModel;
		model.title = this.SubTask;
		model.deadline = this.deadline;
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
		model.Users = this.list_User;
		model.id_parent = this.item.id_row;
		model.id_project_team = this.item.id_project_team;
		this.changeDetectorRefs.detectChanges();
		this._service.InsertWork(model).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				model;
				// this.changeDetectorRefs.detectChanges();
			}
			else {
				this.viewLoading = false;
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		});
	}

	stopPropagation(event) {
		event.stopPropagation();
	}

	ItemSelected(val: any) {
		// this.data.DATA.assign = val;
		// var model = new UpdateWorkModel();
		// model.id_row = this.item.id_row;
		// model.key = 'assign';
		// model.value = val.id_nv;
		// this._service.UpdateByKey(model).subscribe(res => {
		// 	this.changeDetectorRefs.detectChanges();
		// 	if (res && res.status == 1) {
		// 		this.layoutUtilsService.showActionNotification(this.translate.instant('GeneralKey.capnhatthanhcong'), MessageType.Read, 4000, true, false, 3000, 'top', 1);
		// 	}
		// 	else
		// 		this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);

		// });

		// ItemSelected(val: any, task) { // chọn item
		// 	this.UpdateByKey(task, 'assign', val.id_nv);
		//   }
		this.UpdateByKeyNew(this.item, 'assign', val.id_nv);
	}

	ItemSelectedSubtask(val,node){
		this.UpdateByKeyNew(node, 'assign', val.id_nv);
	}

	selectedDate: any = {
		startDate: '',
		endDate: '',
	};
	viewdate() {
		return 'Giao việc thòi gian'
	}

	ReloadData(event) {

		this.item.id_milestone = event.id_row;
		this.item.milestone = event.title;
	}

	updateStartDate(event) {
		// alert(event.value)
		var date = event.value
		this.UpdateByKeyNew(this.item, 'start_date', moment(date).format('MM/DD/YYYY HH:mm'));
	}
	updateDueDate(event) {
		// alert(event.value)
		var date = event.value
		this.UpdateByKeyNew(this.item, 'deadline', moment(date).format('MM/DD/YYYY HH:mm'));
	}

	InputData() {
		var result = document.getElementById("input-text-data");
		result.focus();
	}

	focuscomment() {
		this.showChucnang = true
	}
	focusoutcomment() {
		this.showChucnang = true
	}

	//load task
	list_Tag: any = [];
	project_team: any = '';
	mark_tag() {
		this.weworkService.lite_tag(this.Id_project_team).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.list_Tag = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
	}

	ReloadDatas(event) {
		this.LoadData();
	}

	RemoveTag(tag) {
		const model = new UpdateWorkModel();
		model.id_row = this.item.id_row;
		model.key = 'Tags';
		model.value = tag.id_row;
		this._service.UpdateByKey(model).subscribe(res => {
			if (res && res.status == 1) {
				this.LoadData();
				this.changeDetectorRefs.detectChanges();
				// this.layoutUtilsService.showActionNotification(this.translate.instant('work.dachon'), MessageType.Read, 1000, false, false, 3000, 'top', 1);
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		});
		this.changeDetectorRefs.detectChanges();
	}

	chinhsuastt(item) {
		item.id_project_team = this.Id_project_team;
		const dialogRef = this.dialog.open(StatusDynamicDialogComponent, { width: '40vw', minHeight: '200px', data: item });
		dialogRef.afterClosed().subscribe(res => {
		  if (res) {
			this.LoadData();
		  }
		});
	  }

	setTextinput(val) {
		this.valueFocus = val;
	}

	trackByFn(index,item){
		return item.id_row;
	}

	preview(val){
		
	}
	DownloadFile(val){

	}

	UpdateStatus_dynamic(_item,user) {
		const item = new StatusDynamicModel();
		item.clear();
		item.Id_row = _item.id_row;
		item.StatusName = _item.statusname;
		item.Color = _item.color;
		item.Description = _item.description;
		item.Id_project_team = _item.id_project_team;
		item.Follower = user.id_nv;
		item.Type = '1';
		this.ProjectsTeamService.UpdateStatus(item).subscribe(res => {
			if (res && res.status == 1) {
				this.layoutUtilsService.showActionNotification(this.translate.instant('GeneralKey.capnhatthanhcong'), MessageType.Read, 3000, true, false, 3000, 'top', 1);
				this.LoadData();
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		})
	}

	getDate(date){
		if(!date){
			return new Date();
		}
		var dateParts = date.split("/");
		return new Date((dateParts[1]+"/"+dateParts[0]+"/"+ dateParts[2]).toString()); 
	}
}
