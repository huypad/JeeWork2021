import { filter, takeUntil } from 'rxjs/operators';
import { MenuAsideService } from './../../../_metronic/jeework_old/core/_base/layout/services/menu-aside.service';
import { MenuPhanQuyenServices } from './../../../_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service';
import { QueryParamsModelNew } from './../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { CommonService } from './../../../_metronic/jeework_old/core/services/common.service';
import {
    LayoutUtilsService,
    MessageType,
} from './../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { DocumentsService } from './../documents/documents.service';
import { BehaviorSubject, Subject } from 'rxjs';
import { WeWorkService } from './../services/wework.services';
import {
    Component,
    OnInit,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    OnChanges,
    ViewChild,
    ElementRef,
} from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { ClosedProjectComponent } from './closed-project/closed-project.component';
import { DuplicateProjectComponent } from './duplicate-project/duplicate-project.component';
import { MatDialog } from '@angular/material/dialog';
import { ProjectTeamEditComponent } from './project-team-edit/project-team-edit.component';
import { ProjectsTeamService } from './Services/department-and-project.service';
import {
    MilestoneModel,
    ProjectTeamDuplicateModel,
    ProjectTeamModel,
    ProjectViewsModel,
} from './Model/department-and-project.model';
import { DepartmentModel } from '../List-department/Model/List-department.model';
import { UpdateStatusProjectComponent } from './update-status-project/update-status-project.component';
import { milestoneDetailEditComponent } from '../List-department/milestone-detail-edit/milestone-detail-edit.component';

@Component({
    selector: 'kt-projects-team',
    templateUrl: './projects-team.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ProjectsTeamComponent implements OnInit {

    constructor(
        public _Services: ProjectsTeamService,
        private changeDetectorRefs: ChangeDetectorRef,
        private translate: TranslateService,
        private router: Router,
        private layoutUtilsService: LayoutUtilsService,
        public dialog: MatDialog,
        private activatedRoute: ActivatedRoute,
        public menuAsideService: MenuAsideService,
        public WeWorkService: WeWorkService,
        private menuServices: MenuPhanQuyenServices,
        private DocumentsService: DocumentsService,
        public commonService: CommonService
    ) {
        this.language = localStorage.getItem('language');
        this.UserID = +localStorage.getItem('idUser');
    }
    ID_Project: number;
    activeLink = 'home';
    view = '';
    loadingSubject = new BehaviorSubject<boolean>(false);
    destroy$: Subject<boolean> = new Subject<boolean>();
    // @ViewChild('container') container : ElementRef;
    TabName: string =
        this.translate.instant('projects.congviecdangdanhsach') + '';
    ShowFull = true;
    item: any;
    // item: ProjectTeamModel;
    mywork: any;
    overview: any;
    // mywork: ProjectTeamModel;
    // overview: ProjectTeamModel;
    isFavourite = false;
    language = 'vi';
    colorName = 'white';
    ShowImage = false;
    loaded = false;
    labelChart = [
        this.translate.instant('filter.htdunghan'),
        this.translate.instant('filter.htquahan'),
        this.translate.instant('filter.quahan'),
        this.translate.instant('filter.danglam'),
        this.translate.instant('filter.dangcho'),
    ];
    UserID = 0;
    list_role: any = [];
    RolesByProject: any = [];
    listDefaultView: any = [];
    listDocument: any = [];
    customStyle = {
        // backgroundColor: "#ffffaa",
        // border: "1px solid #7e7e7e",
        // borderRadius: "50%",
        // color: "#7e7e7e",
        // fontsize: "#7e7e7e",
        cursor: 'pointer',
    };
    isShowaddview = false;
    IsAdminGroup = false;
    roles: any = [];

    nameNewView: any = '';
    idNewView: any = '';
    imageNewView: any = '';

    GetColorName(val) {
        // name
        this.WeWorkService.getColorName(val).subscribe((res) => {
            this.colorName = res.data.Color;
            this.changeDetectorRefs.detectChanges();
        });
    }

    getActionActivities(value) {
        let text = '';
        if (this.language === 'vi') {
            text = value.action;
        } else {
            text = value.action_en;
        }
        return text;
    }

    ngOnInit() {

        this._Services.currentMessage.subscribe(e => {
            if (e) {
                this.LoadData();
            }
        });
        this.layoutUtilsService.showWaitingDiv();
        let path = this.router.url;
        if (path) {
            const arr = path.split('/');
            if (arr.length > 3) {
                this.activeLink = arr[3];
            }
            if (
                this.activeLink === 'clickup' ||
                this.activeLink === 'board' ||
                this.activeLink === 'gantt' ||
                this.activeLink === 'calendar'
            ) {
                this.activeLink = 'home';
            }
        }


        this.activatedRoute.params.subscribe((params) => {
            this.ID_Project = +params.id;
            this.LoadData();
            this.activeLink = 'home/clickup';
            path = this.router.url;
            if (path) {
                const arr = path.split('/');
                if (arr.length > 3) {
                    this.activeLink = arr[3] + (arr[4] ? '/' + arr[4] : '');
                }
                if (
                    this.activeLink === 'clickup' ||
                    this.activeLink === 'board' ||
                    this.activeLink === 'gantt' ||
                    this.activeLink === 'calendar'
                ) {
                    this.activeLink = 'home';
                }
            }
        });
        this._Services.ListRole(this.ID_Project).subscribe((res) => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status === 1) {
                this.roles = res.data;
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
            this.changeDetectorRefs.detectChanges();
        });
    }

    ngAfterViewInit(): void {
        // Called after ngAfterContentInit when the component's view has been initialized. Applies to components only.
        // Add 'implements AfterViewInit' to the class.
        this.layoutUtilsService.OffWaitingDiv();

    }

    ngOnDestroy(): void {
        // Called once, before the instance is destroyed.
        // Add 'implements OnDestroy' to the class.
        this.destroy$.next(true);
        this.destroy$.unsubscribe();
    }


    SelectedView(view) {
        // TabName=view.view_name_new;click('home');linkTo(view.link)
        this.TabName = view.view_name_new;
        this.click('home');
        this.linkTo(view.link);
    }

    IsAdmin() {
        if (this.IsAdminGroup) {
            return true;
        }
        if (this.list_role) {
            const x = this.list_role.find((x) => x.id_row === this.ID_Project);
            if (x) {
                if (x.admin === true || x.admin === 1 || +x.owner === 1 || +x.parentowner === 1) {
                    return true;
                }
            }
        }
        return false;
    }

    infoUser() {
        if (this.IsAdmin()) {
            return 'Bạn là quản trị viên của dự án';
        } else {
            let txt = 'Bạn là thành viên tham gia dự án' + '\n';
            this.roles.forEach((element) => {
                // if(element.nhom != "group3"){
                element.roles
                    .filter((x) => x.member)
                    .map((r) => (txt += r.description + '\n'));
                // }else{

                // }
            });
            return txt;
        }
    }

    CheckRoles(roleID: number) {
        if (this.IsAdminGroup) {
            return true;
        }
        if (this.list_role) {
            const x = this.list_role.find((x) => x.id_row === this.ID_Project);
            if (x) {
                if (x.admin === true || x.admin === 1 || +x.owner === 1 || +x.parentowner === 1) {
                    return true;
                } else {
                    if (x.Roles.find((r) => r.id_role === 15)) {
                        return false;
                    }
                    const r = x.Roles.find((r) => r.id_role === roleID);
                    if (r) {
                        return true;
                    } else {
                        return false;
                    }
                }
            } else {
                return false;
            }
        }
        return false;
    }

    modelChangeFn(view) {
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam += 'GeneralKey.capnhatthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        setTimeout(() => {
            let _item = new ProjectViewsModel();
            _item = view;
            this._Services.update_view(_item).subscribe((res) => {
                if (res && res.status === 1) {
                    this.layoutUtilsService.showActionNotification(_saveMessage);
                } else {
                    this.layoutUtilsService.showError(res.error.message);
                }
                this.LoadData();
            });
        }, 10);
    }

    deleteView(view) {
        this._Services.Delete_View(view.id_row).subscribe((res) => {
            if (res && res.status === 1) {
                this.router.navigateByUrl(`/project/` + this.ID_Project).then(() => {
                    this.ngOnInit();
                });
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    linkTo(link) {
        const _backUrl = `/project/` + this.ID_Project + `/` + link;
        this.router.navigateByUrl(_backUrl);
    }

    LoadData() {

        this.menuServices.GetRoleWeWork('' + this.UserID).subscribe((res) => {
            if (res && res.status === 1) {
                this.list_role = res.data.dataRole;
                this.IsAdminGroup = res.data.IsAdminGroup;
            }
        });

        this.WeWorkService.ListViewByProject(this.ID_Project).subscribe((res) => {
            if (res && res.status === 1) {
                this.listDefaultView = res.data;
                const x = this.listDefaultView.find((x) => x.id_project_team === null);
                if (x) {
                    this.isShowaddview = true;
                    this.selectedNewView(x.view_name_new, x.image, x.viewid);
                } else {
                    this.isShowaddview = false;
                }
                this.changeDetectorRefs.detectChanges();
            }
        });
        this.WeWorkService.getRolesByProjects(this.ID_Project).subscribe((res) => {
            if (res && res.status === 1) {
                if (res.data.data && res.data.data.data_roles) {
                    this.RolesByProject = res.data.data.data_roles.filter(x => x.member);
                }
            }
        });

        this._Services.OverView(this.ID_Project).subscribe((res) => {
            if (res && res.status === 1) {
                this.overview = res.data;
            }
        });
        this._Services.MyWork().subscribe((res) => {
            if (res && res.status === 1) {
                this.mywork = res.data;
            }
        });
        this._Services.DeptDetail(this.ID_Project).subscribe((res) => {
            if (res && res.status === 1) {
                this.item = res.data;
                this.loaded = true;
                if (this.item.icon != '') {
                    this.ShowImage = true;
                }
                this.GetColorName(this.item.title.slice(0, 1));
                this.item.Users.forEach((element) => {
                    if (element.id_nv === +localStorage.getItem('idUser')) {
                        this.isFavourite = element.favourite;
                    }
                });
                this.changeDetectorRefs.detectChanges();
                this.ShowFull = true;
                this.item.default_view = 10;
                switch (this.item.default_view) {
                    case 3: {
                        this.view = 'board';
                        break;
                    }
                    case 5: {
                        this.view = 'gantt';
                        break;
                    }
                    case 1: {
                        this.view = 'stream';
                        break;
                    }
                    case 6: {
                        this.view = 'calendar';
                        break;
                    }
                    case 2: {
                        this.view = 'period';
                        break;
                    }
                    case 10: {
                        this.view = 'clickup';
                        break;
                    }
                    default: {
                        this.view = 'home';
                        break;
                    }
                }
                if (this.activeLink === '' || this.activeLink === 'home') {
                    let view_type = '1';
                    switch (this.view) {
                        case 'board': {
                            view_type = '3';
                            break;
                        }
                        case 'gantt': {
                            view_type = '5';
                            break;
                        }
                        case 'stream': {
                            view_type = '1';
                            break;
                        }
                        case 'calendar': {
                            view_type = '6';
                            break;
                        }
                        case 'period': {
                            view_type = '2';
                            break;
                        }
                        case 'clickup': {
                            view_type = '10';
                            break;
                        }
                        default: {
                            view_type = '4';
                            break;
                        }
                    }
                    this.ClickShow('' + this.item.default_view);
                } else {
                    this.ShowFull = true;
                }
                // 		}
            } else {
                this.router.navigate(['']);
            }
        });

        const queryParams = new QueryParamsModelNew(
            this.filterConfiguration(),
            '',
            '',
            1,
            50,
            true
        );
        this.DocumentsService.ListDocuments(queryParams).subscribe((res) => {
            if (res && res.status === 1) {
                this.listDocument = res.data;
            }
            this.changeDetectorRefs.detectChanges();
        });
    }

    filterConfiguration(): any {
        const filter: any = {};
        filter.id_project_team = this.ID_Project;
        return filter;
    }

    selectedNewView(name, image, viewid) {
        this.nameNewView = name;
        this.imageNewView = image;
        this.idNewView = viewid;
    }

    getItemCssClassByLocked(status: number = 0): string {
        switch (status) {
            case 1:
                return 'success';
            case 2:
                return 'brand';
            case 3:
                return 'metal';
        }
    }

    getItemLockedString(condition: number = 0): string {
        switch (condition) {
            case 1:
                return this.translate.instant('projects.dungtiendo');
            case 2:
                return this.translate.instant('projects.chamtiendo');
            case 3:
                return this.translate.instant('projects.ruirocao');
        }
    }

    stringIsNull(val) {
        if (val.trim() === '') {
            return true;
        }
        return false;
    }

    getColorStatus(condition: number = 0) {
        switch (condition) {
            case 1:
                return '#149329'; // đúng tiến độ
            case 2:
                return '#c7a50c'; // chậm tiến độ
            case 3:
                return '#a61d1d'; // rủi ro cao
        }
    }

    getStatusLuuY(status: number = 0): string {
        switch (status) {
            case 1:
                return 'success';
            case 2:
                return 'info';
            case 3:
                return 'danger';
        }
    }

    getItemLuuY(condition: number = 0): string {
        switch (condition) {
            case 1:
                return this.translate.instant('filter.choreview');
            case 2:
                return this.translate.instant('filter.hoanthanh');
            case 3:
                return this.translate.instant('filter.danglam');
        }
    }

    ClickShow(view_type: string) {
        this.activeLink = 'home';
        this.ShowFull = true;
        switch (view_type) {
            case '1': {
                this.ShowFull = false;
                this.TabName =
                    this.translate.instant('projects.congviecdangstream') + '';
                break;
            }
            case '2': {
                this.ShowFull = false;
                this.TabName =
                    this.translate.instant('projects.congviecdangperiod') + '';
                break;
            }
            case '3': {
                this.ShowFull = true;
                this.TabName =
                    this.translate.instant('projects.congviecdangdanhbang') + '';
                break;
            }
            case '4': {
                this.ShowFull = false;
                this.TabName =
                    this.translate.instant('projects.congviecdangdanhsach') + '';
                break;
            }
            case '5': {
                this.ShowFull = true;
                this.TabName = this.translate.instant('projects.gantt') + '';
                break;
            }
            case '6': {
                this.ShowFull = true;
                this.TabName = this.translate.instant('projects.lichbieu') + '';
                break;
            }
            case '10': {
                this.ShowFull = true;
                this.TabName = this.translate.instant('projects.clickup');
                break;
            }
        }
        this.changeDetectorRefs.detectChanges();
    }

    click(link) {
        // this.TabName = '';
        this.activeLink = link;
        this.ShowFull = true;
        // this.changeDetectorRefs.detectChanges();
    }

    ClosedProject() {
        const model = new ProjectTeamModel();
        model.clear(); // Set all defaults fields
        this.Update_ClosedProject(model);
    }

    Update_ClosedProject(_item: ProjectTeamModel) {
        let saveMessageTranslateParam = '';
        _item = this.item;
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? 'GeneralKey.capnhatthanhcong'
                : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
            _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(ClosedProjectComponent, {
            data: { _item },
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                // this.router.navigateByUrl("/project/"+this.ID_Project).then(()=>{
                this.LoadData();
                // });
            } else {
                this.layoutUtilsService.showActionNotification(
                    _saveMessage,
                    _messageType,
                    4000,
                    true,
                    false
                );
                this.router.navigateByUrl('/project/' + this.ID_Project).then(() => {
                    this.LoadData();
                    this.menuAsideService.loadMenu();
                });
            }
        });
    }

    DuplicateProject() {
        const model = new ProjectTeamDuplicateModel();
        model.clear(); // Set all defaults fields
        this.Update_DuplicateProject(model);
    }

    Update_DuplicateProject(_item: ProjectTeamDuplicateModel) {
        let saveMessageTranslateParam = '';
        // _item = this.item;
        _item.id = this.ID_Project;
        _item.title = this.item.title;
        saveMessageTranslateParam +=
            _item.id > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = _item.id > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(DuplicateProjectComponent, {
            data: { _item },
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                this.ngOnInit();
            } else {
                this.layoutUtilsService.showActionNotification(
                    _saveMessage,
                    _messageType,
                    4000,
                    true,
                    false
                );
                this.gotoPageDuplicate(res.res.data.ItemArray);
                // this.layoutUtilsService.deleteElement
            }
        });
    }

    gotoPageDuplicate(item) {
        const newurl = '/project/' + item[0];
        const _title = this.translate.instant('notify.thongbaochuyentrang');
        const _description =
            this.translate.instant('notify.bancomuonchuyendentrang') + item[2];
        const _waitDesciption =
            this.translate.instant('notify.dangchuyentrang') + item[2];
        const dialogRef = this.layoutUtilsService.deleteElement(
            _title,
            _description,
            _waitDesciption
        );
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                this.ngOnInit();
                return;
            } else {
                this.router.navigate(['/project', item[0]]).then(() => {
                    this.ngOnInit();
                });
            }
        });
    }

    AddProject(is_project: boolean) {
        const _project = new ProjectTeamModel();
        _project.clear(); // Set all defaults fields
        _project.is_project = is_project;
        this.UpdateProject(_project, is_project);
    }

    UpdateProject(_item: ProjectTeamModel, is_project: boolean) {
        _item = this.item;
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? 'GeneralKey.capnhatthanhcong'
                : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
            _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(ProjectTeamEditComponent, {
            data: { _item, _IsEdit: _item.IsEdit, is_project: _item.is_project },
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                return;
            } else {
                this.ngOnInit();
                this.menuAsideService.loadMenu();
                this.layoutUtilsService.showActionNotification(
                    _saveMessage,
                    _messageType,
                    4000,
                    true,
                    false
                );
                // this.changeDetectorRefs.detectChanges();
            }
        });
    }

    updateStage(_item: ProjectTeamModel) {
        // this.layoutUtilsService.showActionNotification("Updating");
        let saveMessageTranslateParam = '';
        _item = this.item;
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? 'GeneralKey.capnhatthanhcong'
                : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
            _item.id_row > 0 ? MessageType.Update : MessageType.Create;

        const dialogRef = this.dialog.open(UpdateStatusProjectComponent, {
            data: { _item, _IsEdit: _item.IsEdit },
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                return;
            } else {
                this.ngOnInit();
                this.layoutUtilsService.showActionNotification(
                    _saveMessage,
                    _messageType,
                    4000,
                    true,
                    false
                );
                // this.changeDetectorRefs.detectChanges();
            }
        });
    }

    Deleted() {
        const ObjectModels = new DepartmentModel();
        ObjectModels.clear();
        this.Delete(ObjectModels);
    }

    Delete(_item: DepartmentModel) {
        _item.RowID = this.ID_Project;
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant('projects.confirmxoa');
        const _waitDesciption = this.translate.instant(
            'GeneralKey.dulieudangduocxoa'
        );
        const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
        const dialogRef = this.layoutUtilsService.deleteElement(
            _title,
            _description,
            _waitDesciption
        );
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                return;
            }
            this._Services.DeleteProject(_item.RowID).subscribe((res) => {
                if (res && res.status === 1) {
                    this.layoutUtilsService.showActionNotification(
                        _deleteMessage,
                        MessageType.Delete,
                        4000,
                        true,
                        false,
                        3000,
                        'top',
                        1
                    );
                    const _backUrl = `wework/projects`;
                    this.menuAsideService.loadMenu();
                    this.router.navigateByUrl(_backUrl);
                } else {
                    this.layoutUtilsService.showActionNotification(
                        res.error.message,
                        MessageType.Read,
                        9999999999,
                        true,
                        false,
                        3000,
                        'top',
                        0
                    );
                    this.ngOnInit();
                }
            });
        });
    }

    Change() {
        const ObjectModels = new DepartmentModel();
        ObjectModels.clear();
        this.ChangeType(ObjectModels);
    }

    ChangeType(_item: DepartmentModel) {
        _item.RowID = this.ID_Project;
        const _title = this.translate.instant('projects.chuyenloaiteam');
        const _description = this.translate.instant('projects.confirmchange');
        const _waitDesciption = this.translate.instant(
            'projects.dulieudangduocthaydoi'
        );
        const _deleteMessage = this.translate.instant('projects.thaydoithanhcong');
        const dialogRef = this.layoutUtilsService.deleteElement(
            _title,
            _description,
            _waitDesciption
        );
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                return;
            }
            this._Services.ChangeType(_item.RowID).subscribe((res) => {
                if (res && res.status === 1) {
                    this.layoutUtilsService.showActionNotification(
                        _deleteMessage,
                        MessageType.Delete,
                        4000,
                        true,
                        false,
                        3000,
                        'top',
                        1
                    );
                    const _backUrl = `/wework/projects`;
                    this.router.navigateByUrl(_backUrl);
                } else {
                    this.layoutUtilsService.showActionNotification(
                        res.error.message,
                        MessageType.Read,
                        9999999999,
                        true,
                        false,
                        3000,
                        'top',
                        0
                    );
                    this.ngOnInit();
                }
            });
        });
    }

    favourite() {
        this._Services.favourireproject(this.ID_Project).subscribe((res) => {
            if (res && res.status === 1) {
                this.isFavourite = res.data;
                this.changeDetectorRefs.detectChanges();
                this.layoutUtilsService.showActionNotification(
                    this.translate.instant('GeneralKey.capnhatthanhcong'),
                    MessageType.Read,
                    4000,
                    true,
                    false,
                    3000,
                    'top',
                    1
                );
            } else {
                this.layoutUtilsService.showActionNotification(
                    res.error.message,
                    MessageType.Read,
                    999999999,
                    true,
                    false,
                    3000,
                    'top',
                    0
                );
            }
        });
    }

    AddMileston() {
        let saveMessageTranslateParam = '';
        const _item = new MilestoneModel();
        _item.clear();
        _item.id_project_team = this.ID_Project;
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? 'GeneralKey.capnhatthanhcong'
                : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
            _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(milestoneDetailEditComponent, {
            data: { _item },
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                this.ngOnInit();
                return;
            } else {
                this.ngOnInit();
                this.layoutUtilsService.showActionNotification(
                    _saveMessage,
                    _messageType,
                    4000,
                    true,
                    false
                );
                // this.changeDetectorRefs.detectChanges();
            }
        });
    }

    addView() {
        const _item = new ProjectViewsModel();
        _item.clear();
        _item.id_project_team = this.ID_Project;
        _item.view_name_new = this.nameNewView;
        _item.viewid = this.idNewView;
        this._Services.Add_View(_item).subscribe((res) => {
            if (res && res.status === 1) {
                this.layoutUtilsService.showActionNotification('Thêm mới thành công');

                // load view
                this.WeWorkService.ListViewByProject(this.ID_Project).subscribe(
                    (res) => {
                        if (res && res.status === 1) {
                            this.listDefaultView = res.data;
                            const x = this.listDefaultView.find(
                                (x) => x.id_project_team === null
                            );
                            if (x) {
                                this.isShowaddview = true;
                                this.selectedNewView(x.view_name_new, x.image, x.viewid);
                            } else {
                                this.isShowaddview = false;
                            }
                        }
                        this.changeDetectorRefs.detectChanges();
                    }
                );
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    ViewReport() {
        const url = 'project/' + this.ID_Project + '/report/' + this.ID_Project;
        this.router.navigateByUrl(url);
        // this._Services.FindDepartmentFromProjectteam(this.ID_Project).subscribe(res => {
        // 	if (res && res.status === 1) {
        // 		const url = 'project/' + this.ID_Project + '/report/' + res.data; ///project/1/activities
        // 		this.router.navigateByUrl(url);
        // 	}
        // })
    }
}
