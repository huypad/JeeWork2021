import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { MessageType, LayoutUtilsService } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { ProjectsTeamService } from './../../Services/department-and-project.service';
import { WorkDuplicateModel } from './../../../work/work.model';
import { Router } from '@angular/router';
import { WeWorkService } from './../../../services/wework.services';
import { TranslateService } from '@ngx-translate/core';
import { WorkService } from './../../../work/work.service';
import { FormBuilder } from '@angular/forms';
import { DuplicateWorkComponent } from './../../../work/work-duplicate/work-duplicate.component';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Component, OnInit, ChangeDetectorRef, Inject } from '@angular/core';
import * as moment  from 'moment';

@Component({
	selector: 'kt-duplicate-task-new',
	templateUrl: './duplicate-task-new.component.html',
	styleUrls: ['./duplicate-task-new.component.scss']
})
export class DuplicateTaskNewComponent implements OnInit {

	constructor(public dialogRef: MatDialogRef<DuplicateWorkComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private ProjectsTeamService: ProjectsTeamService,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: WorkService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private danhMucChungService: DanhMucChungService,
		public weworkService: WeWorkService,
		private router: Router,) { }
	ItemDuplicate: any = [];
	listProject: any = [];
	DataID = 0;
	type = 1;
	getOnlyIDproject = false;
	ngOnInit() {
		if (this.data._item)
			this.DataID = this.data._item.id_row;
		if (this.data.type)
			this.type = this.data.type;
		if (this.data.getOnlyIDproject)
			this.getOnlyIDproject = this.data.getOnlyIDproject;
		this.LoadData();
	}

	LoadData() {
		this.ProjectsTeamService.WorkDetail(this.DataID).subscribe(res => {
			if (res && res.status == 1) {
				this.ItemDuplicate = res.data;
				this.changeDetectorRefs.detectChanges();
			}
		});

		this.weworkService.lite_project_team_byuser("").subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listProject = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
	}

	listKeyDuplicate = {
		name: 'Everything',
		value: 'Everything',
		type: '',
		checked: false,
		subtasks: [
			{
				name: 'deadline',
				value: 'deadline',
				type: 'date',
				checked: false,
			},
			{
				name: 'start_date',
				value: 'start_date',
				type: 'date',
				checked: false,
			},
			{
				name: 'duplicate_child',
				value: 'duplicate_child',
				type: 'bool',
				checked: false,
			},
			{
				name: 'assign',
				value: 'assign',
				type: 'text',
				checked: false,
			},
			{
				name: 'urgent',
				value: 'urgent',
				type: 'bool',
				checked: false,
			},
			{
				name: 'required_result',
				value: 'required_result',
				type: 'bool',
				checked: false,
			},
			{
				name: 'require',
				value: 'require',
				type: 'bool',
				checked: false,
			},
		]
	};

	allComplete: boolean = false;

	updateAllComplete() {
		this.allComplete = this.listKeyDuplicate.subtasks != null && this.listKeyDuplicate.subtasks.every(t => t.checked);
	}

	someComplete(): boolean {
		if (this.listKeyDuplicate.subtasks == null) {
			return false;
		}
		return this.listKeyDuplicate.subtasks.filter(t => t.checked).length > 0 && !this.allComplete;
	}

	setAll(completed: boolean) {
		this.allComplete = completed;
		if (this.listKeyDuplicate.subtasks == null) {
			return;
		}
		this.listKeyDuplicate.subtasks.forEach(t => t.checked = completed);
	}

	DuplicateTask() {
		const duplicate = new WorkDuplicateModel();
		duplicate.clear();
		duplicate.title = this.ItemDuplicate.title;
		duplicate.type = this.type;
		duplicate.description = this.ItemDuplicate.description ? this.ItemDuplicate.description : '';
		duplicate.id_parent = this.ItemDuplicate.id_parent ? this.ItemDuplicate.id_parent : 0;
		duplicate.id_project_team = this.ItemDuplicate.id_project_team;
		duplicate.id = this.ItemDuplicate.id_row;
		this.listKeyDuplicate.subtasks.forEach(element => {
			if (element.type == 'bool') {
				duplicate[element.value] = element.checked;
			} else {
				if (element.checked) {
					if (element.type == 'date') {
						if (moment(this.ItemDuplicate[element.value]).format('MM/DD/YYYY HH:mm:ss') != "Invalid date") {
							duplicate[element.value] = moment(this.ItemDuplicate[element.value]).format('MM/DD/YYYY  HH:mm:ss')
						}

					} else
						duplicate[element.value] = element.value == 'assign' ? this.getIDNV(this.ItemDuplicate.User) : this.ItemDuplicate[element.value];
				}
			}
		})

		this.Create(duplicate);
	}

	getIDNV(assign){
		if(assign && assign[0].id_nv )
			return assign[0].id_nv;
		return 0;
	}

	DuplicateMoreTask() {
		this.dialogRef.close(
			this.ItemDuplicate.id_project_team
		);
	}

	Create(_item: WorkDuplicateModel) {
		this.ProjectsTeamService.DuplicateCU(_item).subscribe(res => {;
			if (res && res.status === 1) {
				this.layoutUtilsService.showActionNotification('Nhân bản thành công', MessageType.Read, 4000, true, false, 3000, 'top', 1);
				this.dialogRef.close(
					res.data
				);
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
			this.changeDetectorRefs.detectChanges()
		});
	}

}
