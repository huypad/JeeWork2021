import {MenuAsideService} from './../../../../_metronic/jeework_old/core/_base/layout/services/menu-aside.service';
import {ProjectTeamEditComponent} from './../../projects-team/project-team-edit/project-team-edit.component';
import {CommonService} from './../../../../_metronic/jeework_old/core/services/common.service';
import {milestoneDetailEditComponent} from './../milestone-detail-edit/milestone-detail-edit.component';
import {AddStatusComponent} from './../../projects-team/work-list-new/add-status/add-status.component';
import {ProjectTeamModel, MilestoneModel} from './../../projects-team/Model/department-and-project.model';
import {WeWorkService} from './../../services/wework.services';
import {Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
// Material
import {MatDialog} from '@angular/material/dialog';
import {SelectionModel} from '@angular/cdk/collections';
// RXJS
import {debounceTime, distinctUntilChanged, tap} from 'rxjs/operators';
import {fromEvent, merge} from 'rxjs';
import {TranslateService} from '@ngx-translate/core';
// Services
import {DanhMucChungService} from './../../../../_metronic/jeework_old/core/services/danhmuc.service';// import { ProcessWorkService } from '../Services/process-work.service';
import {LayoutUtilsService, MessageType} from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Models
import {QueryParamsModelNew} from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {SubheaderService} from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';

import {ListDepartmentService} from '../Services/List-department.service';
import {DepartmentModel} from '../Model/List-department.model';
import {DepartmentEditComponent} from '../department-edit/department-edit.component';
import {DepartmentDataSource} from '../Model/data-sources/List-department.datasource';

@Component({
    selector: 'kt-List-department-tab',
    templateUrl: './List-department-tab.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})

export class DepartmentTabComponent implements OnInit {
    ID_Department: number = 0;
    Department: string = '';
    //==========Dropdown Search==============
    filter: any = {};
    dataSource: DepartmentDataSource;
    item: DepartmentModel;
    activeLink = 'projects';

    constructor(
        public _deptServices: ListDepartmentService,
        private danhMucService: DanhMucChungService,
        public dialog: MatDialog,
        private router: Router,
        private layoutUtilsService: LayoutUtilsService,
        private translate: TranslateService,
        public subheaderService: SubheaderService,
        private activatedRoute: ActivatedRoute,
        public menuAsideService: MenuAsideService,
        private changeDetectorRefs: ChangeDetectorRef,
        public WeWorkService: WeWorkService,
        // private dynamicSearchFormService: DynamicSearchFormService,
        public commonService: CommonService,
    ) {
    }

    /** LOAD DATA */
    ngOnInit() {
        var arr = this.router.url.split('/');
        if (arr.length > 3) {
            this.activeLink = arr[3];
        }
        this.activatedRoute.params.subscribe(params => {
            this.ID_Department = +params.id;
            this._deptServices.DeptDetail(this.ID_Department).subscribe(res => {
                if (res && res.status === 1) {
                    this.item = res.data;
                    this.Department = res.data.title;
                    this.changeDetectorRefs.detectChanges();
                }else{
                    this.router.navigate(['depts']);
                }
            });
        });
    }

    goBack() {
        this.router.navigate(['/depts']);
    }

    filterConfiguration(): any {
        const filter: any = {};
        return filter;
    }

    Delete() {
        var ObjectModels = new DepartmentModel();
        ObjectModels.clear();
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant('department.confirmxoa');
        const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
        const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
        const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                return;
            }
            this._deptServices.Delete_Dept(this.ID_Department).subscribe(res => {
                if (res && res.status === 1) {
                    this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
                    let _backUrl = `depts`;
                    this.menuAsideService.loadMenu();
                    this.router.navigateByUrl(_backUrl);
                } else {
                    this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
                    this.ngOnInit();
                }

            });
        });
    }

    Add() {
        const ObjectModels = new DepartmentModel();
        ObjectModels.clear(); // Set all defaults fields
        this.Update(ObjectModels);
    }


    Update(_item: DepartmentModel) {

        _item.RowID = this.item.id_row;
        _item.title = this.item.title;
        _item.id_cocau = this.item.id_cocau;
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam += _item.RowID > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = _item.RowID > 0 ? MessageType.Update : MessageType.Create;

        const dialogRef = this.dialog.open(DepartmentEditComponent, {data: {_item, _IsEdit: _item.IsEdit}});
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                return;
            } else {
                this.ngOnInit();
                this.menuAsideService.loadMenu();
                this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    click(activeLink) {
        this.activeLink = activeLink;
        var a = this.ID_Department;
    }

    // thêm mới project team
    AddNewProject(is_project = true) {
        const _project = new ProjectTeamModel();
        _project.clear(); // Set all defaults fields
        _project.is_project = is_project;
        _project.id_department = this.ID_Department.toString();

        this.UpdateNewProject(_project);
    }

    UpdateNewProject(_item: ProjectTeamModel) {
        const dialogRef = this.dialog.open(ProjectTeamEditComponent, {
                data: {_item, _IsEdit: _item.IsEdit, is_project: _item.is_project, id_project_team: _item.id_project_team}
            }
        );
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                return;
            } else {
            }
        });
    }

    AddMileston() {

        let saveMessageTranslateParam = '';
        var _item = new MilestoneModel;
        _item.clear();
        _item.id_department = this.ID_Department;
        _item.id_project_team = 0;
        saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(milestoneDetailEditComponent, {data: {_item}});
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                // this.ngOnInit();
                return;
            } else {
                this.ngOnInit();
                this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
                // this.changeDetectorRefs.detectChanges();
            }
        });
    }
}
