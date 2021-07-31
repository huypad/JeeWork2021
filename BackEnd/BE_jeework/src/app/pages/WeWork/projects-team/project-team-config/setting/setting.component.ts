import { tinyMCE } from 'src/app/_metronic/jeework_old/components/tinyMCE';
import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { LayoutUtilsService } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { ProjectTeamEditStatusComponent } from './../../project-team-edit-status/project-team-edit-status.component';
import { ProjectViewsModel } from "./../../Model/department-and-project.model";
import {
	Component,
	OnInit,
	ElementRef,
	ViewChild,
	ChangeDetectionStrategy,
	ChangeDetectorRef,
	Inject,
	HostListener,
	Input,
	SimpleChange,
	Output,
	EventEmitter,
} from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
// Material
import {  MatDialog  } from "@angular/material/dialog";
import { SelectionModel } from "@angular/cdk/collections";
// RXJS
import { fromEvent, merge, ReplaySubject, BehaviorSubject } from "rxjs";
import { TranslateService } from "@ngx-translate/core";

// Models
import {
	AbstractControl,
	FormBuilder,
	FormControl,
	FormGroup,
	Validators,
} from "@angular/forms";
import { WeWorkService } from "../../../services/wework.services";
import {
	ProjectTeamModel,
	TagsModel,
} from "../../Model/department-and-project.model";
import { ProjectsTeamService } from "../../Services/department-and-project.service";

@Component({
	selector: "kt-setting",
	templateUrl: "./setting.component.html",
	changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SettingComponent {
	item: ProjectTeamModel;
	item_lable: TagsModel;
	itemForm: FormGroup;
	itemForm2: FormGroup;
	itemForm3: FormGroup;
	itemForm4: FormGroup;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	disabledBtn: boolean = false;
	IsProject: boolean;
	title: string = "";
	selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
	ID_Struct: string = "";
	@Output() ItemSelected = new EventEmitter<any>();
	filter: any = {};
	tendapb: string = "";
	mota: string = "";
	tinyMCE: any = {};
	Tags: any[];
	_lable: string = "";
	checkcolor: string = "";
	public datatree: BehaviorSubject<any[]> = new BehaviorSubject([]);
	id_project_team: number = 0;
	//filter list department
	public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public projectFilterCtrl: FormControl = new FormControl();
	listdepartment: any[] = [];
	listDefaultView: any[] = [];
	
	//icon
	icon: any = {};
	constructor(
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: ProjectsTeamService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,		
		public dialog: MatDialog,
		private danhMucChungService: DanhMucChungService,
		public weworkService: WeWorkService,
		private router: Router
	) {}
	/** LOAD DATA */
	ngOnInit() {
		var arr = this.router.url.split("/");
		this.id_project_team = +arr[2];
		this.tinyMCE = Object.assign({}, tinyMCE);
		this.tinyMCE.height = 100;
		this.title = this.translate.instant("GeneralKey.choncocautochuc") + "";
		this.item = new ProjectTeamModel();
		this.item.clear();
		this.createForm();
		this.getTreeValue();
		this.weworkService.lite_department().subscribe((res) => {
			this.disabledBtn = false;
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listdepartment = res.data;
				this.setUpDropSearchProject();
				this.changeDetectorRefs.detectChanges();
			}
		});
		this.weworkService.lite_tag(this.id_project_team).subscribe((res) => {
			if (res && res.status === 1) {
				this.Tags = res.data;
			}
		});
		this.layoutUtilsService.showWaitingDiv();
		this._service.DeptDetail(this.id_project_team).subscribe((res) => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1) {
				this.item = res.data;
				this.TempSelected = res.data.id_template;
				this.IsProject = res.data.is_project;
				this.ID_Struct = "" + this.item.id_department;
				this.createForm();
			} else this.layoutUtilsService.showError(res.error.message);
			if (this.IsProject) {
				this.tendapb = this.translate.instant("projects.tenduan") + "";
				this.mota =
					this.translate.instant("projects.motangangonveduan") + "";
			} else {
				this.tendapb = this.translate.instant("projects.phongban") + "";
				this.mota =
					this.translate.instant("projects.motangangonvephongban") +
					"";
			}
			this.changeDetectorRefs.detectChanges();
		});

		this.LoadData();
		this.LoadDataTemp();
	}

	LoadData() {
		this.weworkService
			.ListViewByProject(this.id_project_team)
			.subscribe((res) => {
				if (res && res.status === 1) {
					this.listDefaultView = res.data;
					this.listDefaultView.forEach((element) => {
						if (element.id_project_team == this.id_project_team)
							element.isChecked = true;
						else element.isChecked = false;
					});
				}
				this.changeDetectorRefs.detectChanges();
			});
	}

	litsTemplateDemo:any=[];
	listSTT:any=[];
	TempSelected = 0;
	LoadDataTemp() {
		//load lại
		this.weworkService.ListTemplateByCustomer().subscribe((res) => {
			if (res && res.status === 1) {
				this.litsTemplateDemo = res.data;
				setTimeout(() => {
					if (this.TempSelected == 0)
						this.TempSelected = this.litsTemplateDemo[0].id_row;
					this.LoadListSTT();
				}, 10);
			}
			this.changeDetectorRefs.detectChanges();
		});
	}

	TemplateUsed(){
		var x =this.litsTemplateDemo.find(x=>x.id_row==this.TempSelected);
		if(x){
			return x.title;
		}
		return 'Chưa sử dụng giao diện template';
	}
	LoadListSTT() {
		var x = this.litsTemplateDemo.find(
			(x) => x.id_row == this.TempSelected
		);
		if (x) {
			this.listSTT = x.status;
		}
		this.changeDetectorRefs.detectChanges();
	}

	UpdateTemplate(){
		const dialogRef = this.dialog.open(ProjectTeamEditStatusComponent, {
			data: this.item,
			minWidth: '800px',
		});
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				location.reload();
			}
		});
	}

	dataChanged(view) {
		setTimeout(() => {
			if(view.isChecked){ // thêm view 
				this.addView(view);
			}else{ // xóa view
				this.deleteView(view);
			}
		}, 10);
	}

	createForm() {
		this.icon = {};
		if (this.item.icon != "") this.icon.src = this.item.icon;
		else {
			this.icon.src =
				"https://img.icons8.com/fluent/48/000000/add-image.png";
		}
		this.itemForm = this.fb.group({
			title: [this.item.title, Validators.required],
			detail: [this.item.detail],
			description: [this.item.description],
			loai: [this.item.loai],
			status: [this.item.status],
			color: [this.item.color],
			allow_percent_done: [this.item.allow_percent_done],
			// allow_estimate_time: [this.item.allow_estimate_time],
			require_evaluate: [this.item.require_evaluate],
			// evaluate_by_assignner: [this.item.evaluate_by_assignner],
			locked: [this.item.locked],
			default_view: [this.item.default_view],
			period_type: [this.item.period_type],
		});
		this.itemForm2 = this.fb.group({
			Department: ["" + this.item.id_department, Validators.required],
		});
		this.itemForm3 = this.fb.group({
			start_date: [this.item.start_date],
			end_date: [this.item.end_date],
		});
		this.itemForm4 = this.fb.group({
			_label: ["", Validators.required],
		});
	}
	prepare(group): ProjectTeamModel {
		let form;
		let controls;
		let invalid: boolean = false;
		const _item = Object.assign({}, this.item);
		if (group == 1) {
			form = this.itemForm;
			controls = this.itemForm.controls;
			invalid = this.itemForm.invalid;
			if (!invalid) {
				_item.title = controls["title"].value;
				_item.detail = controls["detail"].value;
				_item.description = controls["description"].value;
				_item.loai = controls["loai"].value;
				_item.allow_percent_done = controls["allow_percent_done"].value;
				// _item.allow_estimate_time =
				// 	controls["allow_estimate_time"].value;
				_item.locked = controls["locked"].value;
				_item.status = controls["status"].value;
				_item.require_evaluate = controls["require_evaluate"].value;
				// if (_item.require_evaluate)
				// 	_item.evaluate_by_assignner =
				// 		controls["evaluate_by_assignner"].value;
				_item.start_date = this.f_convertDate(this.item.start_date);
				_item.end_date = this.f_convertDate(this.item.end_date);
				if (this.icon.strBase64) {
					_item.icon = this.icon;
				}
			}
		}
		if (group == 3) {
			form = this.itemForm3;
			invalid = this.itemForm3.invalid;
			controls = this.itemForm3.controls;
			if (!invalid) {
				_item.start_date = this.f_convertDate(
					controls["start_date"].value
				);
				_item.end_date = this.f_convertDate(controls["end_date"].value);
			}
		}
		/* check form */
		if (invalid) {
			Object.keys(controls).forEach((controlName) =>
				controls[controlName].markAsTouched()
			);
			this.hasFormErrors = true;
			return;
		}
		return _item;
	}
	f_convertDate(p_Val: any) {
		if (p_Val == null) return "1/1/0001";
		let a = p_Val === "" ? new Date() : new Date(p_Val);
		return (
			a.getFullYear() +
			"/" +
			("0" + (a.getMonth() + 1)).slice(-2) +
			"/" +
			("0" + a.getDate()).slice(-2)
		);
	}
	prepareTags(): TagsModel {
		const controls = this.itemForm4.controls;
		const _item = Object.assign({}, this.item_lable);

		if (this.item_lable == undefined) {
			this.item_lable = new TagsModel();
			this.item_lable.clear();
		}
		_item.id_row = this.item_lable.id_row;
		_item.color = this.checkcolor;
		_item.title = controls["_label"].value;
		_item.id_project_team = "" + this.id_project_team;
		return _item;
	}
	//group:1-chỉnh sửa, 2: department, 3:thời gian, 4: tag
	onSubmit(group, withBack: boolean = false) {
		this.hasFormErrors = false;
		this.loadingAfterSubmit = false;
		if (group == 4) {
			const updatedegree = this.prepareTags();
			if (updatedegree.id_row > 0) {
				this.update_tags(updatedegree, withBack);
			} else this.insert_tags(updatedegree, withBack);
			return;
		}
		if (group == 2) {
			this.update_bykey(this.ID_Struct, "id_department");
		} else {
			const updatedegree = this.prepare(group);
			if (updatedegree == undefined) return;
			this.Update(updatedegree, withBack);
		}
	}
	getTreeValue() {
		this.danhMucChungService.Get_MaCoCauToChuc_HR().subscribe((res) => {
			if (res.data && res.data.length > 0) {
				this.datatree.next(res.data);
				this.selectedNode.next({
					RowID: "" + this.item.id_department,
				});
			}
		});
	}

	GetValueNode(val: any) {
		this.ID_Struct = val.RowID;
		this.filter = this.ID_Struct;
	}
	huyTag() {
		this.itemForm.controls["_label"].setValue(null);
		this.checkcolor = "";
		this.item_lable.clear();
	}
	checkColor(event): any {
		let val = event.value;
		this.checkcolor = event;
	}
	BindColor(tag): any {
		this.item_lable = tag;
		this.checkcolor = this.item_lable.color;
		this.itemForm4.controls["_label"].setValue(this.item_lable.title);
	}
	update_bykey(event, key): any {
		let val = event.value;
		if (key == "color" || key == "id_department") val = event;
		this.layoutUtilsService.showWaitingDiv();
		this._service
			.UpdateByKey(this.item.id_row, key, val)
			.subscribe((res) => {
				this.layoutUtilsService.OffWaitingDiv();
				if (res && res.status == 1) this.ngOnInit();
				else this.layoutUtilsService.showError(res.error.message);
			});
	}
	insert_tags(_item: TagsModel, withBack: boolean): any {
		// let val = event.value;
		// if (key == 'color' || key == 'id_department')
		// 	val = event;
		this.layoutUtilsService.showWaitingDiv();
		this._service.InsertTags(_item).subscribe((res) => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1) this.ngOnInit();
			else this.layoutUtilsService.showError(res.error.message);
		});
	}
	update_tags(_item: TagsModel, withBack: boolean): any {
		// let val = event.value;
		// if (key == 'color' || key == 'id_department')
		// 	val = event;
		this.layoutUtilsService.showWaitingDiv();
		this._service.UpdateTags(_item).subscribe((res) => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1) this.ngOnInit();
			else this.layoutUtilsService.showError(res.error.message);
		});
	}
	Update(_item: ProjectTeamModel, withBack: boolean) {
		this.layoutUtilsService.showWaitingDiv();
		this._service.UpdateProjectTeam(_item).subscribe((res) => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status === 1) this.ngOnInit();
			else {
				this.layoutUtilsService.showError(res.error.message);
			}
		});
	}

	deleteTag(tag) {
		//deleteElement
		// this.layoutUtilsService.deleteElement('Bạn có muốn xóa','Do you want delete tag from List');
		const _title = this.translate.instant("GeneralKey.xoa");
		const _description = this.translate.instant(
			"GeneralKey.bancochacchanmuonxoakhong"
		);
		const _waitDesciption = this.translate.instant(
			"GeneralKey.dulieudangduocxoa"
		);
		const _deleteMessage = this.translate.instant(
			"GeneralKey.xoathanhcong"
		);
		const dialogRef = this.layoutUtilsService.deleteElement(
			_title,
			_description,
			_waitDesciption
		);
		dialogRef.afterClosed().subscribe((res) => {
			if (!res) {
				return;
			}

			this._service.DeleteTag(tag.id_row).subscribe((res) => {
				this.layoutUtilsService.OffWaitingDiv();
				if (res && res.status == 1) {
					this.ngOnInit();
					this.layoutUtilsService.showActionNotification(_deleteMessage);
				} else this.layoutUtilsService.showError(res.error.message);
			});

			// this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false);
		});
	}

	onAlertClose($event) {
		this.hasFormErrors = false;
	}

	setUpDropSearchProject() {
		this.projectFilterCtrl.setValue("");
		this.filterProject();
		this.projectFilterCtrl.valueChanges.pipe().subscribe(() => {
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
			this.listdepartment.filter(
				(bank) => bank.title.toLowerCase().indexOf(search) > -1
			)
		);
	}
	chooseFile() {
		let f = document.getElementById("inputIcon");
		f.click();
	}
	onSelectFile(event) {
		if (event.target.files && event.target.files[0]) {
			var filesAmount = event.target.files[0];
			var Strfilename = filesAmount.name.split(".");

			event.target.type = "text";
			event.target.type = "file";
			var reader = new FileReader();
			let base64Str: any;
			reader.onload = (event) => {
				base64Str = event.target["result"];
				var metaIdx = base64Str.indexOf(";base64,");
				let strBase64 = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64
				this.icon = {
					filename: filesAmount.name,
					strBase64: strBase64,
					base64Str: base64Str,
				};
				this.changeDetectorRefs.detectChanges();
			};
			reader.readAsDataURL(filesAmount);
		}
	}

	addView(view) {
		var _item = new ProjectViewsModel();
		_item.clear();
		_item.id_project_team = this.id_project_team;
		_item.view_name_new = view.view_name_new;
		_item.viewid = view.viewid;

		this._service.Add_View(_item).subscribe((res) => {
			if (res && res.status == 1) {
				this.layoutUtilsService.showActionNotification(
					"Thêm mới thành công"
				);
			} else {
				this.layoutUtilsService.showError(res.error.message);
			}
			this.LoadData();
		});
	}

	deleteView(view) {
		let saveMessageTranslateParam = "";
		saveMessageTranslateParam += "GeneralKey.capnhatthanhcong";
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		this._service.Delete_View(view.id_row).subscribe((res) => {
			if (res && res.status == 1) {
				this.LoadData();
				this.layoutUtilsService.showActionNotification(_saveMessage);
			} else {
				this.layoutUtilsService.showError(res.error.message);
			}
			this.LoadData();
		});
	}
}
