import { DepartmentEditNewComponent } from './../../List-department/department-edit-new/department-edit-new.component';
import { SubheaderService } from './../../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import { CommonService } from './../../../../_metronic/jeework_old/core/services/common.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { MatTabChangeEvent } from '@angular/material/tabs';
// RXJS
import { TranslateService } from '@ngx-translate/core';
// Services
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';// import { ProcessWorkService } from '../Services/process-work.service';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Models

// import { DynamicSearchFormService } from '../../../../dynamic-search-form/dynamic-search-form.service';
import { DepartmentEditComponent } from '../../List-department/List-department-edit/List-department-edit.component';
import { DepartmentModel } from '../../List-department/Model/List-department.model';
import { ProjectTeamEditComponent } from '../project-team-edit/project-team-edit.component';
import { ProjectsTeamService } from '../Services/department-and-project.service';
import { ProjectTeamModel } from '../Model/department-and-project.model';

@Component({
	selector: 'kt-projects-team-list',
	templateUrl: './projects-team-list.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})

export class ProjectsTeamListComponent implements OnInit {
	ID_QuyTrinh: number = 0;
	TenQuyTrinh: string = '';
	is_gird: boolean = false;
	is_list: boolean = true;
	isshow: boolean = false;
	selectedTab: number = 0;
	//==========Dropdown Search==============
	filter: any = {};
	keyword: string = '';
	filterstage: string = '';
	filterTinhTrang: string = '';
	ShowHead = false;
	constructor(
		public _services: ProjectsTeamService,
		private danhMucService: DanhMucChungService,
		public dialog: MatDialog,
		private router: Router,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public subheaderService: SubheaderService,
		private activatedRoute: ActivatedRoute,
		private changeDetectorRefs: ChangeDetectorRef,
		public commonService: CommonService,
		// private dynamicSearchFormService: DynamicSearchFormService,
		) { }

	/** LOAD DATA */
	ngOnInit() {
		this.selectedTab = this.subheaderService.selectTab;
	}

	goBack() {
		window.history.back();
	}
	onLinkClick(eventTab: MatTabChangeEvent) {
		if (eventTab.index == 0) {
			this.selectedTab = 0;
			this.subheaderService.selectTab = 0;
		}
		else if (eventTab.index == 1) {
			this.selectedTab = 1;
			this.subheaderService.selectTab = 1;
		} else {
			this.selectedTab = 2;
		}
	}
	view(_str: string) {

		if (_str == 'gird') {
			this.is_gird = true;
			this.is_list = false;
		}
		else {
			this.is_gird = false;
			this.is_list = true;
		}
	}
	loadDataList(): any {
		this.filter = {};
		if (this.keyword)
			this.filter.keyword = this.keyword;
		if (this.filterstage)
			this.filter.status = this.filterstage;
		if (this.filterTinhTrang)
			this.filter.locked = this.filterTinhTrang;
		this.changeDetectorRefs.detectChanges();
	}
	AddProject(is_project: boolean) {
		const _project = new ProjectTeamModel();
		_project.clear(); // Set all defaults fields
		_project.is_project = is_project;
		this.UpdateProject(_project);
	}
	UpdateProject(_item: ProjectTeamModel) {
		const dialogRef = this.dialog.open(ProjectTeamEditComponent, { data: { _item, _IsEdit: _item.IsEdit, is_project: _item.is_project } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.loadDataList();
			}
		});
	}
	Add() {
		const ObjectModels = new DepartmentModel();
		ObjectModels.clear(); // Set all defaults fields
		this.Update(ObjectModels);
	}
	Update(_item: DepartmentModel) {
		let saveMessageTranslateParam = '';
		saveMessageTranslateParam += _item.RowID > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.RowID > 0 ? MessageType.Update : MessageType.Create;

		const dialogRef = this.dialog.open(DepartmentEditNewComponent, { data: { _item, _IsEdit: _item.IsEdit } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.ngOnInit();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.changeDetectorRefs.detectChanges();
			}
		});
	}
}
