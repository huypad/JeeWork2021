import { WorkProcessEditComponent } from './../work-process-edit/work-process-edit.component';
import { QueryFilterComment } from './../../../jee-comment/jee-comment.model';
import { QueryParamsModelNew } from './../../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { MenuPhanQuyenServices } from './../../../../../_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service';
import { SubheaderService } from './../../../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import {
    LayoutUtilsService,
    MessageType,
} from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { StatusDynamicDialogComponent } from './../../../status-dynamic/status-dynamic-dialog/status-dynamic-dialog.component';
import { ProjectsTeamService } from './../../Services/department-and-project.service';
import {
    AttachmentModel,
    FileUploadModel,
} from './../../Model/department-and-project.model';
import { CheckListEditComponent } from './../../../work/check-list-edit/check-list-edit.component';
import {
    UpdateByKeyModel,
    ChecklistModel,
    ChecklistItemModel,
} from './../../../update-by-keys/update-by-keys.model';
import { MatDialog } from '@angular/material/dialog';
import { WeWorkService } from './../../../services/wework.services';
import { AttachmentService } from './../../../services/attachment.service';
import { UpdateByKeyService } from './../../../update-by-keys/update-by-keys.service';
import { DatePipe } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
import { WorkService } from './../../../work/work.service';
import { PopoverContentComponent } from 'ngx-smart-popover';
import {
    WorkModel,
    UserInfoModel,
    UpdateWorkModel,
} from './../../../work/work.model';
import { BehaviorSubject, ReplaySubject, of, throwError, Subscription } from 'rxjs';
import { FormGroup, FormControl, FormBuilder } from '@angular/forms';
import { DialogData } from './../../../report/report-tab-dashboard/report-tab-dashboard.component';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import {
    Component,
    OnInit,
    Inject,
    ViewChild,
    ElementRef,
    ChangeDetectorRef,
    OnChanges,
    HostListener,
    ViewContainerRef,
    ComponentFactoryResolver,
} from '@angular/core';
import * as moment from 'moment';
import {
    catchError,
    finalize,
    takeUntil,
    tap,
    share,
    switchMap,
    map,
} from 'rxjs/operators';
import { UpdateByKeysComponent } from './../../../update-by-keys/update-by-keys-edit/update-by-keys-edit.component';
import { JeeCommentService } from '../../../jee-comment/jee-comment.service';
import { CommunicateService } from '../work-list-new-service/communicate.service';
import { LogWorkDescriptionComponent } from '../../../log-work-description/log-work-description.component';
import { tinyMCE } from 'src/app/_metronic/jeework_old/components/tinyMCE';
import { DomSanitizer } from '@angular/platform-browser';
@Component({
    selector: 'kt-work-list-new-detail',
    templateUrl: './work-list-new-detail.component.html',
    styleUrls: ['./work-list-new-detail.component.scss'],
})
export class WorkListNewDetailComponent implements OnInit {
    nameList: string[];
    name: string;
    constructor(
        private workService: WorkService,
        private communicateService: CommunicateService,
        private el: ElementRef,
        private projectsTeamService: ProjectsTeamService,
        public dialog: MatDialog,
        public subheaderService: SubheaderService,
        private layoutUtilsService: LayoutUtilsService,
        private changeDetectorRefs: ChangeDetectorRef,
        private translate: TranslateService,
        public datepipe: DatePipe,
        public weworkService: WeWorkService,
        public jeeCommentService: JeeCommentService,
        private updatebykeyService: UpdateByKeyService,
        private _attservice: AttachmentService,
        public dialogRef: MatDialogRef<WorkListNewDetailComponent>,
        @Inject(MAT_DIALOG_DATA) public datalog: DialogData,
        private menuServices: MenuPhanQuyenServices,
        private sanitizer: DomSanitizer,
        private viewContainerRef: ViewContainerRef, private componentFactoryResolver: ComponentFactoryResolver
        // public activeModal: NgbActiveModal,
        // private confirmationDialogService: ConfirmationDialogService

    ) {
        this.list_priority = this.weworkService.list_priority;
        this.UserID = +localStorage.getItem('idUser');
    }

    selectedItem: any = undefined;
    itemForm: FormGroup;
    loadingSubject = new BehaviorSubject<boolean>(false);
    loadingControl = new BehaviorSubject<boolean>(false);
    loading1$ = this.loadingSubject.asObservable();
    hasFormErrors = false;
    item: any = {};
    oldItem: WorkModel;
    selectedTab = 0;
    // ========================================================
    Visible = false;
    viewLoading = false;
    loadingAfterSubmit = false;
    listUser: any[] = [];
    FormControls: FormGroup;
    disBtnSubmit = false;
    isChange = false;
    UserInfo: any = {};
    data: any;
    SubTask = '';
    deadline: any;
    listColumn: any[] = [];
    IsShow_MoTaCV = false;
    IsShow_CheckList = false;
    IsShow_Result = false;
    MoTaCongViec = '';
    checklist = '';
    Result = '';
    CheckList: any[] = [];
    Value = '';
    Key = '';
    ItemFinal = 0;
    description = '';
    checklist_item = '';
    Id_project_team = 0;
    public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    public bankFilterCtrl: FormControl = new FormControl();
    listType: any[] = [];
    ListGroup: any[] = [];
    DataID = 0;
    AssignTask: any = [];
    AssignChecklist: any = [];
    list_priority: any = [];
    options_assign: any = {};
    @ViewChild('Assign', { static: true })
    myPopover_Assign: PopoverContentComponent;
    selected_Assign: any[] = [];
    @ViewChild('hiddenText_Assign', { static: true }) text_Assign: ElementRef;
    _Assign = '';
    list_Assign: any[] = [];
    status_dynamic: any[] = [];
    LogDetail: any[] = [];
    list_role: any[] = [];
    text_item = '';
    valueFocus = '';
    customStyle = [];
    UserID = 0;
    list_milestone: any = [];
    showChucnang = false;
    disabledBtn = false;
    showsuccess = false;
    is_inputting = true;
    private readonly componentName: string = 'kt-task_';
    isDeteachChange$?: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(
        false
    );
    isback = true;
    objectID: string;
    Comment = '';
    showCommentDefault?: boolean;
    number: number;
    public lstObjectID: string[] = [];
    isReset: any;
    IsAdminGroup = false;
    loadTags = false;
    require_evaluate = false;
    newtask = true;
    loading = true;
    isclosed = true;
    TenFile = '';
    File = '';
    filemodel: any;
    tinyMCE = {};
    @ViewChild('csvInput', { static: true }) myInputVariable: ElementRef;
    @ViewChild('resultInput', { static: true }) result: ElementRef;
    list_User: any[] = [];
    selectedDate: any = {
        startDate: '',
        endDate: '',
    };
    // load task
    list_Tag: any = [];
    project_team: any = '';

    ListChild: any = {};
    IsLoading: any = [];
    description_tiny: string;

    /** LOAD DATA */
    ngOnInit() {

        this.workService.currentMessage.subscribe(message => {
        });
        this.data = this.datalog;
        if (this.data && this.data.notback) {
            this.isback = false;
        }
        if (this.data && this.data.notloading) {
            this.loading = false;
        }
        this.DataID = this.data.id_row;
        this.LoadDataWorkDetail(this.DataID);
        this.Id_project_team = this.data.id_project_team;
        this.UserInfo = JSON.parse(localStorage.getItem('UserInfo'));
        this.projectsTeamService.Detail(this.Id_project_team).subscribe(
            (res) => {
                if (res && res.status == 1) {
                    this.require_evaluate = res.data.require_evaluate;
                }
            }
        );
        this.tinyMCE = tinyMCE;
        this.LoadData();
        this.LoadChecklist();
        this.LoadObjectID();
    }
    getHeight(): any {
        let obj = window.location.href.split('/').find(x => x == 'tabs-references');
        if (obj) {
            let tmp_height = 0;
            tmp_height = window.innerHeight - 354;
            return tmp_height + 'px';
        } else {
            let tmp_height = 0;
            tmp_height = window.innerHeight - 236;
            return tmp_height + 'px';
        }
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
        this.workService.CheckList(queryParams).subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                // res.data.forEach(element => {
                // 	element.isExpanded = false;
                // })
                this.CheckList = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
    }


    KiemTraThayDoiCongViec(item, key, closeTask = false) {
        if (!this.CheckClosedTask() && !closeTask) {
            this.layoutUtilsService.showError('Công việc đã đóng');
            return false;
        }
        if (this.IsAdmin()) {
            return true;
        } else if (item.CreatedBy == this.UserID) {
            return true;
        } else {
            if (item.Users) {
                const index = item.Users.findIndex(x => x.id_nv == this.UserID);
                if (index >= 0) {
                    return true;
                }
            }
        };
        var txtError = '';
        switch (key) {
            case 'assign':
                txtError = 'Bạn không có quyền thay đổi người làm của công việc này';
                break;
            case 'status':
                txtError = 'Bạn không có quyền thay đổi trạng thái của công việc này';
                break;
            case 'estimates':
                txtError = 'Bạn không có quyền thay đổi thời gian làm của công việc này';
                break;
            case 'checklist':
                txtError = 'Bạn không có quyền chỉnh sửa checklist của công việc này';
                break;
            case 'title':
                txtError = 'Bạn không có quyền đổi tên của công việc này';
                break;
            case 'description':
                txtError = 'Bạn không có quyền đổi mô tả của công việc này';
                break;
            default:
                txtError = 'Bạn không có quyền chỉnh sửa công việc này';
                break;
        }
        this.layoutUtilsService.showError(txtError);
        this.disabledBtn = false;
        return false;

    }

    IsCheck() {
        var item = this.item;
        if (this.IsAdmin()) {
            return true;
        } else if (item.CreatedBy == this.UserID) {
            return true;
        } else if (item.CreatedBy == this.UserID) {
            return true;
        } else {
            if (item.Users) {
                const index = item.Users.findIndex(x => x.id_nv == this.UserID);
                if (index >= 0) {
                    return true;
                }
            }
        }
        ;
        return false;

    }

    LoadChild(item) {
        // this.DataID = item.id_row;
        // this.LoadData();
        // this.LoadChecklist();
        debugger
        const dialogRef = this.dialog.open(WorkListNewDetailComponent, {
            width: '90vw',
            height: '85vh',
            data: item,
            disableClose: true
        });

        dialogRef.afterClosed().subscribe((result) => {
            this.LoadData();
            if (result != undefined) {
                // this.selectedDate.startDate = new Date(result.startDate)
                // this.selectedDate.endDate = new Date(result.endDate)
            }
        });
    }

    LoadLog() {
        // get LogDetailCU
        this.projectsTeamService.LogDetailCU(this.DataID).subscribe((res) => {
            if (res && res.status === 1) {
                this.LogDetail = res.data;
                this.LogDetail = this.LogDetail.filter(x => x.id_action != 16).sort();
                this.resetComment();
                this.changeDetectorRefs.detectChanges();
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    View_Log_Description() {
        var ID_log = this.item.id_row;
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam += ID_log > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = this.item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(LogWorkDescriptionComponent, { data: { ID_log }, width: '1000px' });
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                // this.ngOnInit();
            } else {
                this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
                // this.ngOnInit();
            }
        });
    }
    LoadDataWorkDetail(WorkID) {
        this.layoutUtilsService.showWaitingDiv();
        this.projectsTeamService.WorkDetail(WorkID).subscribe((res) => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 1) {
                this.item = res.data;
                this.isclosed = this.item.closed;
                if (!this.description_tiny) {
                    this.description_tiny = this.item.description;
                }
                this.changeDetectorRefs.detectChanges();
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }
    LoadData() {
        this.mark_tag();
        this.LoadLog();
        if (this.loading) {
            this.layoutUtilsService.showWaitingDiv();
        }
        this.weworkService.ListStatusDynamic(this.Id_project_team)
            .subscribe((res) => {
                if (res && res.status === 1) {
                    this.status_dynamic = res.data;
                    console.log("Dữ liệu trạng thái", res.data);
                    this.changeDetectorRefs.detectChanges();
                }
            });

        setTimeout((x) => {
            const filter: any = {};
            filter.id_project_team = this.Id_project_team;
            this.weworkService.list_account(filter).subscribe((res) => {
                this.changeDetectorRefs.detectChanges();
                if (res && res.status === 1) {
                    this.listUser = res.data;
                    this.setUpDropSearchNhanVien();
                    this.changeDetectorRefs.detectChanges();
                }
                this.options_assign = this.getOptions_Assign();
            });
        }, 500);
        this.projectsTeamService.WorkDetail(this.DataID).subscribe((res) => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 1) {
                this.item = res.data;
                this.isclosed = this.item.closed;
                if (!this.description_tiny) {
                    this.description_tiny = this.item.description;
                }
                this.changeDetectorRefs.detectChanges();
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
        // this.weworkService.lite_milestone(this.Id_project_team).subscribe((res) => {
        //     this.changeDetectorRefs.detectChanges();
        //     if (res && res.status === 1) {
        //         this.listType = res.data;
        //         this.changeDetectorRefs.detectChanges();
        //     }
        // });

        // Load data work group
        this.weworkService.lite_workgroup(this.Id_project_team)
            .pipe(
                tap((res) => {
                    if (res && res.status === 1) {
                        this.ListGroup = res.data;
                        this.changeDetectorRefs.detectChanges();
                    }
                })
            )
            .subscribe();

        // quyền
        this.menuServices.GetRoleWeWork('' + this.UserID).subscribe((res) => {
            if (res && res.status == 1) {
                this.list_role = res.data.dataRole;
                this.IsAdminGroup = res.data.IsAdminGroup;
                this.CheckUserInProject(res.data);
            }
        });

        this.weworkService.lite_milestone(this.Id_project_team).subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                this.list_milestone = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    CheckUserInProject(data) {
        if (data.IsAdminGroup) {
            return true;
        }
        if (data.dataRole) {
            const x = data.dataRole.find((x) => x.id_row === this.Id_project_team);
            if (x) {
                return true;
            } else {
                this.dialogRef.close();
            }
        }
        return false;
    }

    IsAdmin() {
        if (this.IsAdminGroup) {
            return true;
        }
        if (this.list_role) {
            const x = this.list_role.find((x) => x.id_row == this.Id_project_team);
            if (x) {
                if (x.admin == true || x.admin == 1 || +x.owner == 1 || +x.parentowner == 1) {
                    return true;
                }
            }
        }
        return false;
    }

    HasupdateResult() {
        if (!this.CheckClosedTask()) {
            return false;
        }
        if (this.IsAdmin()) {
            return true;
        } else if (this.item.CreatedBy == this.UserID) {
            return true;
        } else {
            if (this.item.Users) {
                const index = this.item.Users.findIndex(x => x.id_nv == this.UserID);
                if (index >= 0) {
                    return true;
                }
            }
        };
        return false;
    }

    CheckClosedTask() {
        // if (this.IsAdminGroup) {
        //     return true;
        // }
        if (this.item.closed) {
            return false;
        } else {
            return true;
        }
    }

    ClosedTask(value) {
        if (!this.KiemTraThayDoiCongViec(this.item, 'closetask', true)) {
            this.item.closed = !value;
            return;
        }
        this.projectsTeamService.ClosedTask(this.item.id_row, value).subscribe((res) => {
            this.LoadData();
            this.SendMessage(true);
            if (res && res.status == 1) {
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    CheckClosedProject() {
        if (this.list_role) {
            const x = this.list_role.find((x) => x.id_row == this.Id_project_team);
            if (x) {
                if (x.locked) {
                    return false;
                }
            }
        }
        return true;
    }

    CheckRoles(roleID: number) {
        const x = this.list_role.find((res) => res.id_row == this.Id_project_team);
        if (x) {
            if (x.locked) {
                return false;
            }
        }
        if (this.IsAdminGroup) {
            return true;
        }
        if (this.list_role) {
            const x = this.list_role.find((res) => res.id_row == this.Id_project_team);
            if (x) {
                if (x.admin == true || x.admin == 1 || +x.owner == 1 || +x.parentowner == 1) {
                    return true;
                } else {
                    if (
                        roleID == 7 ||
                        roleID == 9 ||
                        roleID == 11 ||
                        roleID == 12 ||
                        roleID == 13
                    ) {
                        if (x.Roles.find((r) => r.id_role == 15)) {
                            return false;
                        }
                    }
                    if (roleID == 10) {
                        if (x.Roles.find((r) => r.id_role == 16)) {
                            return false;
                        }
                    }
                    if (roleID == 4 || roleID == 14) {
                        if (x.Roles.find((r) => r.id_role == 17)) {
                            return false;
                        }
                    }
                    const r = x.Roles.find((r) => r.id_role == roleID);
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

    CheckRoleskeypermit(key) {
        const x = this.list_role.find((res) => res.id_row == this.Id_project_team);
        if (x) {
            if (x.locked) {
                return false;
            }
        }
        if (this.IsAdminGroup) {
            return true;
        }
        if (this.list_role) {
            if (x) {
                if (x.admin == true || x.admin == 1 || +x.owner == 1 || +x.parentowner == 1) {
                    return true;
                } else {
                    if (
                        key == 'title' ||
                        key == 'description' ||
                        key == 'status' ||
                        key == 'checklist' ||
                        key == 'delete'
                    ) {
                        if (x.Roles.find((r) => r.id_role == 15)) {
                            return false;
                        }
                    }
                    if (key == 'deadline') {
                        if (x.Roles.find((r) => r.id_role == 16)) {
                            return false;
                        }
                    }
                    if (key == 'id_nv' || key == 'assign') {
                        if (x.Roles.find((r) => r.id_role == 17)) {
                            return false;
                        }
                    }

                    const r = x.Roles.find((r) => r.keypermit == key);
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

    getKeyword_Assign() {
        const i = this._Assign.lastIndexOf('@');
        if (i >= 0) {
            const temp = this._Assign.slice(i);
            if (temp.includes(' ')) {
                return '';
            }
            return this._Assign.slice(i);
        }
        return '';
    }

    getOptions_Assign() {
        const options_assign: any = {
            showSearch: true,
            keyword: this.getKeyword_Assign(),
            data: this.listUser.filter(
                (x) => this.selected_Assign.findIndex((y) => x.id_nv == y.id_nv) < 0
            ),
        };
        return options_assign;
    }

    getColorStatus(val) {
        const index = this.status_dynamic.find((x) => x.id_row == val);
        if (index) {
            return index.color;
        } else {
            return 'gray';
        }
    }

    getPriority(id) {
        if (id > 0) {
            const item = this.list_priority.find((x) => x.value === id);
            if (item) {
                return item.icon;
            }
            return 'far fa-flag';
        } else {
            return 'far fa-flag';
        }
    }

    getPriorityLog(id) {
        if (+id > 0 && this.list_priority) {
            const prio = this.list_priority.find(x => x.value === +id);
            if (prio) {
                return prio;
            }
        }
        return {
            name: 'Noset',
            value: 0,
            icon: 'far fa-flag',
        };
    }

    NextStatus(type) {
        if (!this.KiemTraThayDoiCongViec(this.item, 'status')) {
            return;
        }
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
        this.projectsTeamService._UpdateByKey(item).subscribe((res) => {
            if (res && res.status == 1) {
                this.LoadData();
                this.SendMessage(true);
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    bindStatus(val) {
        const stt = this.status_dynamic.find((x) => +x.id_row == +val);
        if (stt) {
            return stt.statusname;
        }
        return this.translate.instant('GeneralKey.chuagantinhtrang');
    }

    click_Assign($event, vi = -1) {
        this.myPopover_Assign.hide();
    }

    onSearchChange_Assign($event) {
        this._Assign = ((
            document.getElementById('InputAssign')
        ) as HTMLInputElement).value;

        if (this.selected_Assign.length > 0) {
            const reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm;
            const match = this._Assign.match(reg);
            if (match != null && match.length > 0) {
                const arr = match.map((x) => x);
                this.selected_Assign = this.selected_Assign.filter((x) =>
                    arr.includes('@' + x.username)
                );
            } else {
                this.selected_Assign = [];
            }
        }
        this.options_assign = this.getOptions_Assign();
        if (this.options_assign.keyword) {
            const el = $event.currentTarget;
            const rect = el.getBoundingClientRect();
            this.myPopover_Assign.show();
            this.changeDetectorRefs.detectChanges();
        }
    }

    ItemSelected_Assign(data) {
        this.selected_Assign = this.list_Assign;
        this.selected_Assign.push(data);
        const i = this._Assign.lastIndexOf('@');
        this._Assign = this._Assign.substr(0, i) + '@' + data.username + ' ';
        this.myPopover_Assign.hide();
        const ele = document.getElementById('InputAssign') as HTMLInputElement;
        ele.value = this._Assign;
        ele.focus();
        this.changeDetectorRefs.detectChanges();
    }

    filterConfiguration(): any {
        const filter: any = {};
        filter.id_work = this.DataID;
        return filter;
    }

    // type=1: comment, type=2: reply
    UpdateResult(value) {
        this.UpdateByKeyNew(this.item, 'result', value);
        this.Update_Result();
    }

    UpdateGroup(id_row) {
        if (id_row == 0) {
            this.UpdateByKeyNew(this.item, 'id_group', null);
        } else {
            this.UpdateByKeyNew(this.item, 'id_group', id_row);
        }
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
        const model = new UpdateWorkModel();
        model.id_row = this.item.id_row;
        model.key = 'assign';
        model.value = val.id_nv;
        this.workService.UpdateByKey(model).subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status == 1) {
                this.layoutUtilsService.showActionNotification(
                    this.translate.instant('GeneralKey.capnhatthanhcong'), MessageType.Read, 4000, true, false, 3000, 'top', 1);
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    Delete_Followers(val: any) {
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant(
            'GeneralKey.bancochacchanmuonxoakhong'
        );
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
            this.workService.Delete_Followers(this.item.Id, val).subscribe((res) => {
                if (res && res.status === 1) {
                    this.layoutUtilsService.showActionNotification(
                        _deleteMessage,
                        MessageType.Delete,
                        4000,
                        true,
                        false
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
        });
    }
    setUpDropSearchNhanVien() {
        this.bankFilterCtrl.setValue('');
        this.filterBanks();
        this.bankFilterCtrl.valueChanges.pipe().subscribe(() => {
            this.filterBanks();
        });
    }

    Update_Status(val: any) {
        const model = new UpdateWorkModel();
        model.id_row = this.item.id_row;
        model.key = 'status';
        model.value = val;
        this.workService.UpdateByKey(model).subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status == 1) {
                this.item.status = val;
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
            this.listUser.filter(
                (bank) => bank.hoten.toLowerCase().indexOf(search) > -1
            )
        );
    }

    CreateTask(val) {
        this.newtask = false;
        setTimeout(() => {
            this.newtask = true;
        }, 1000);
        this.projectsTeamService.InsertTask(val).subscribe((res) => {
            if (res && res.status == 1) {
                // this.CloseAddnewTask(true);
                this.LoadData();
                this.SendMessage(true);
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    onAlertClose($event) {
        this.hasFormErrors = false;
    }
    // ---------------------------------------------------------

    f_number(value: any) {
        return Number((value + '').replace(/,/g, ''));
    }

    f_currency(value: any, args?: any): any {
        const nbr = Number((value + '').replace(/,|-/g, ''));
        return (nbr + '').replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1,');
    }

    textPres(e: any, vi: any) {
        if (
            isNaN(e.key) &&
            // && e.keyCode != 8 // backspace
            // && e.keyCode != 46 // delete
            e.keyCode != 32 && // space
            e.keyCode != 189 &&
            e.keyCode != 45
        ) {
            // -
            e.preventDefault();
        }
    }

    checkDate(e: any, vi: any) {
        if (
            !(
                (e.keyCode > 95 && e.keyCode < 106) ||
                (e.keyCode > 46 && e.keyCode < 58) ||
                e.keyCode == 8
            )
        ) {
            e.preventDefault();
        }
    }

    checkValue(e: any) {
        if (
            !(
                (e.keyCode > 95 && e.keyCode < 106) ||
                (e.keyCode > 47 && e.keyCode < 58) ||
                e.keyCode == 8
            )
        ) {
            e.preventDefault();
        }
    }

    f_convertDate(v: any) {
        if (v != '' && v != null) {
            const a = new Date(v);
            return (
                a.getFullYear() +
                '-' +
                ('0' + (a.getMonth() + 1)).slice(-2) +
                '-' +
                ('0' + a.getDate()).slice(-2) +
                'T00:00:00.0000000'
            );
        }
    }

    f_date(value: any): any {
        if (value != '' && value != null && value != undefined) {
            const latest_date = this.datepipe.transform(value, 'dd/MM/yyyy');
            return latest_date;
        }
        return '';
    }

    refreshData() {
        this.projectsTeamService.WorkDetail(this.item.id_row).subscribe((res) => {
            if (res && res.status == 1) {
                this.ngOnInit();
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    ThemCot() {
        const item = {
            RowID: 0,
            FieldName: '',
            Title: '',
            KeyValue: '',
            TypeID: '',
            Formula: '',
            ShowFomula: false,
            Priority: -1,
            IsEdit: true,
            Width: '',
            FormatID: '1',
            IsColumnHide: false,
        };
        this.listColumn.splice(0, 0, item);
        this.changeDetectorRefs.detectChanges();
    }

    Get_ValueByKey(key: string): string {
        switch (key) {
            case '16':
                return (this.Value = this.MoTaCongViec);
            case '0':
                return (this.Value = this.checklist);
        }
        return '';
    }

    updatePriority(value) {
        this.UpdateByKeyNew(this.item, 'clickup_prioritize', value);
    }

    UpdateByKey(id_log_action: string, key: string, IsQuick: boolean) {
        if (!this.KiemTraThayDoiCongViec(this.item, id_log_action == '0' ? 'checklist' : key)) {
            return;
        }
        this.Get_ValueByKey(id_log_action);
        const model = new UpdateByKeyModel();
        if (IsQuick) {
            model.id_row = this.item.id_row;
            model.id_log_action = id_log_action;
            model.value = this.Value;
            model.key = key;
            if (id_log_action == '0') {
                const checklist_model = new ChecklistModel();
                checklist_model.id_work = this.item.id_row;
                checklist_model.title = this.Value;

                this.updatebykeyService
                    .Insert_CheckList(checklist_model)
                    .subscribe((res) => {
                        if (res && res.status === 1) {
                            this.IsShow_CheckList = !this.IsShow_CheckList;
                            this.LoadChecklist();
                            this.refreshData();
                            this.changeDetectorRefs.detectChanges();
                        } else {
                            this.layoutUtilsService.showError(
                                res.error.message
                            );
                        }
                    });
            } else {
                this.updatebykeyService.UpdateByKey(model).subscribe((res) => {
                    this.changeDetectorRefs.detectChanges();
                    if (res && res.status === 1) {
                        this.IsShow_MoTaCV = !this.IsShow_MoTaCV;

                        this.projectsTeamService.WorkDetail(model.id_row).subscribe((res) => {
                            if (res && res.status == 1) {
                                this.description = res.data.description;
                                this.description_tiny = this.description;
                                this.is_inputting = true;
                                this.refreshData();
                                // this.checklist = res.data.description;
                                this.ngOnInit();
                                this.changeDetectorRefs.detectChanges();
                            }
                        });
                        this.changeDetectorRefs.detectChanges();
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
                    }
                });
            }
        } else {
            model.clear(); // Set all defaults fields
            this.UpdateKey(model);
        }
    }

    // update by key click up

    UpdateStatus(task, status) {
        if (+task.status == +status.id_row) {
            return;
        }
        this.UpdateByKeyNew(task, 'status', status.id_row);
    }

    UpdateTitle() {
        const ele = document.getElementById('txttitle' + this.DataID) as HTMLInputElement;
        if (ele.value.toString().trim() == '') {
            // this.layoutUtilsService.showError('Tên công việc không được trống');
            ele.value = this.item.title;
            return;
        }
        if (ele.value.toString().trim() != this.item.title.toString().trim()) {

            if (!this.KiemTraThayDoiCongViec(this.item, 'title')) {
                ele.value = this.item.title;
                return;
            }

            this.item.title = ele.value;
            this.UpdateByKeyNew(this.item, 'title', this.item.title);
        }
    }
    chinhsuamota = false;
    UpdateDescription() {
        // if (!this.chinhsuamota) {
        //   this.chinhsuamota = !this.chinhsuamota;
        //   return;
        // }else 
        {
            if (this.item.description.trim() != this.description_tiny.trim()) {
                this.disabledBtn = true;
                if (!this.KiemTraThayDoiCongViec(this.item, "description")) {
                    this.description_tiny = this.item.description;
                    this.is_inputting = false;
                    return;
                }
                this.item.description = this.description_tiny;
                this.is_inputting = false;
                this.UpdateByKeyNew(this.item, "description", this.description_tiny);
            }
            this.chinhsuamota = !this.chinhsuamota;
        }
    }

    UpdateByKeyNew(task, key, value) {
        if (!this.KiemTraThayDoiCongViec(task, key)) {
            return;
        }
        const item = new UpdateWorkModel();
        item.id_row = task.id_row;
        item.key = key;
        item.value = value;
        if (task.assign && task.assign.id_nv > 0) {
            item.IsStaff = true;
        }
        if (this.loading) {
        }
        this.layoutUtilsService.showWaitingDiv();
        this.projectsTeamService._UpdateByKey(item).pipe(
            tap(() => {
            }),
            map(res => {
                this.layoutUtilsService.OffWaitingDiv();
                if (res && res.status == 1) {
                    this.SendMessage(true);
                    this.LoadData();
                    if (key == "description") {
                        this.is_inputting = true;
                    }
                } else {
                    this.layoutUtilsService.showError(res.error.message);
                }
            }),
            catchError((err) => throwError(err)),
            finalize(() => this.layoutUtilsService.OffWaitingDiv()),
        ).subscribe((res) => {
            if (key == 'description') {
                this.showsuccess = true;
                setTimeout(() => {
                    this.is_inputting = false;
                    this.showsuccess = false;
                    this.disabledBtn = false;
                }, 2000);
            }
        }, (error) => {
            this.disabledBtn = false;
        });
    }

    UpdateKey(_item: UpdateByKeyModel) {
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? 'GeneralKey.capnhatthanhcong'
                : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
            _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(UpdateByKeysComponent, {
            data: { _item },
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                this.refreshData();
                this.ngOnInit();
            } else {
                this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
                this.ngOnInit();
            }
        });
        this.refreshData();
    }
    public decline() {
        // this.activeModal.close(false);
    }

    public accept() {
        // this.activeModal.close(true);
    }

    public dismiss() {
        // this.activeModal.dismiss();
    }
    UpdateCheckList() {
        const model = new UpdateByKeyModel();
        model.clear(); // Set all defaults fields
        this.UpdateKey(model);
    }

    _UpdateCheckList(_item: ChecklistModel) {
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam +=
            _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(CheckListEditComponent, {
            data: { _item, IsCheckList: true },
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                this.refreshData();
                this.ngOnInit();
                this.changeDetectorRefs.detectChanges();
                return;
            } else {
                this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
            }
        });
        this.refreshData();
    }

    updateNewChecklist(_item) {
        const item = new ChecklistModel();
        item.id_row = _item.id_row;
        item.title = _item.title;
        this.workService.Update_CheckList(item).subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                // this.layoutUtilsService.showActionNotification('Update thành công', MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            } else {
                this.layoutUtilsService.showActionNotification(
                    res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
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
        this.workService.Update_CheckList_Item(item).subscribe((res) => {
            if (res && res.status === 1) {
                // this.layoutUtilsService.showActionNotification('Update thành công', MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            } else {
                this.layoutUtilsService.showActionNotification(
                    res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
                this.LoadChecklist();
            }
            this.changeDetectorRefs.detectChanges();
        });
    }

    UpdateCheckList_Item(_item: ChecklistModel) {
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? 'GeneralKey.capnhatthanhcong'
                : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
            _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(CheckListEditComponent, {
            data: { _item, IsCheckList: false },
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                this.refreshData();
                return;
            } else {
                this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
            }
        });
    }

    InsertCheckListItem(id_checkList: number) {
        const text = ((
            document.getElementById('checklist' + id_checkList)
        ) as HTMLInputElement).value;
        const model = new ChecklistItemModel();
        model.id_checklist = id_checkList;
        model.title = text; // item.id_row
        this.updatebykeyService.Insert_CheckList_Item(model).subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                this.LoadChecklist();
                this.changeDetectorRefs.detectChanges();
                ((
                    document.getElementById('checklist' + id_checkList)
                ) as HTMLInputElement).value = '';
            } else {
                this.layoutUtilsService.showActionNotification(
                    res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    deletext(id_checkList) {
        const text = (
            document.getElementById('checklist' + id_checkList)
        ) as HTMLInputElement;
        if (text && text.value != '') {
            return true;
        }
        return false;
    }

    hidebuttondelete(id_checkList) {
        ((
            document.getElementById('checklist' + id_checkList)
        ) as HTMLInputElement).value = '';
    }

    Delete_CheckList(_item: ChecklistModel) {
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant(
            'GeneralKey.bancochacchanmuonxoakhong'
        );
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
                this.refreshData();
                return;
            }

            this.workService.Delete_CheckList(_item.id_row).subscribe((res) => {
                if (res && res.status === 1) {
                    this.layoutUtilsService.showActionNotification(
                        _deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
                    this.LoadChecklist();
                } else {
                    this.layoutUtilsService.showActionNotification(
                        res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
                }
            });
        });
        this.refreshData();
    }

    DeleteItem(_item: ChecklistItemModel) {
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant(
            'GeneralKey.bancochacchanmuonxoakhong'
        );
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
                this.refreshData();
                return;
            }
            this.workService.DeleteItem(_item.id_row).subscribe((res) => {
                if (res && res.status === 1) {
                    this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
                    this.LoadChecklist();
                } else {
                    this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
                }
            });
        });
        this.refreshData();
    }

    save_file_Direct(evt: any, type: string) {
        if (evt.target.files && evt.target.files.length) {
            // Nếu có file 
            const file = evt.target.files[0]; // Ví dụ chỉ lấy file đầu tiên
            this.TenFile = file.name;
            const reader = new FileReader();
            reader.readAsDataURL(evt.target.files[0]);
            let base64Str;
            setTimeout(() => {
                base64Str = reader.result as String;
                const metaIdx = base64Str?.indexOf(';base64,');
                base64Str = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64
                this.File = base64Str;
                const _model = new AttachmentModel();
                _model.object_type = parseInt(type);
                _model.object_id = this.item.id_row;
                const ct = new FileUploadModel();
                ct.strBase64 = this.File;
                ct.filename = this.TenFile;
                ct.IsAdd = true;
                const item = new UpdateWorkModel();
                item.id_row = this.item.id_row;
                item.key = type == '1' ? 'Attachments' : 'Attachments_result';
                item.values = new Array<FileUploadModel>(ct);
                this.projectsTeamService._UpdateByKey(item).subscribe((res) => {
                    if (res && res.status == 1) {
                        this.LoadData();
                        this.SendMessage(true);
                    }
                    else {
                        // alert(res.error.message);
                        this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
                        this.LoadData();
                    }
                });
            }, 100);
        } else {
            this.File = '';
        }
    }

    getActionActivities(value) {
        let text = '';
        text = value.action;
        if (text) {
            return text.replace('{0}', '');
        }
        return '';
    }

    Delete_File(val: any) {
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant(
            'GeneralKey.bancochacchanmuonxoakhong'
        );
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
            this._attservice.delete_attachment(val).subscribe((res) => {
                if (res && res.status === 1) {
                    this.ngOnInit();
                    this.changeDetectorRefs.detectChanges();
                    this.layoutUtilsService.showActionNotification(
                        _deleteMessage,
                        MessageType.Delete,
                        4000,
                        true,
                        false
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
        });
    }

    Insert_SubTask() {
        const model = new WorkModel();
        model.title = this.SubTask;
        model.deadline = this.deadline;
        if (this.selected_Assign.length > 0) {
            this.listUser.map((item, index) => {
                const _true = this.selected_Assign.find((x) => x.id_nv === item.id_nv);
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
        this.workService.InsertWork(model).subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                // model;
                // this.changeDetectorRefs.detectChanges();
            } else {
                this.viewLoading = false;
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
            }
        });
    }

    Updateestimates(event) {
        this.item.estimates = event;
        this.UpdateByKeyNew(this.item, 'estimates', event);
    }

    stopPropagation(event) {
        event.stopPropagation();
    }

    ItemSelected(val: any) {
        this.UpdateByKeyNew(this.item, 'assign', val.id_nv);
    }

    ItemFollower(val: any) {
        this.UpdateByKeyNew(this.item, 'follower', val.id_nv);
    }

    ItemSelectedSubtask(val, node) {
        this.UpdateByKeyNew(node, 'assign', val.id_nv);
    }

    viewdate() {
        return 'Giao việc thòi gian';
    }

    ReloadData(event) {
        this.item.id_milestone = event.id_row;
        this.item.milestone = event.title;
    }

    updateDate(event, field) {
        if (event) {
            this.UpdateByKeyNew(
                this.item,
                field,
                event
            );
        } else {
            this.UpdateByKeyNew(
                this.item,
                field,
                null,
            );
        }
    }

    RemoveBykey(key) {
        this.UpdateByKeyNew(this.item, key, null);
    }

    InputData() {
        const result = document.getElementById('input-text-data');
        result.focus();
    }

    focuscomment() {
        this.showChucnang = true;
    }

    focusoutcomment() {
        this.showChucnang = true;
    }

    mark_tag() {
        this.weworkService.lite_tag(this.Id_project_team).subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                this.list_Tag = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    ReloadDatas(event) {
        this.LoadData();
        this.SendMessage(true);
    }

    RemoveTag(tag) {
        if (!this.KiemTraThayDoiCongViec(this.item, 'tags')) {
            return;
        }
        const model = new UpdateWorkModel();
        model.id_row = this.item.id_row;
        model.key = 'Tags';
        model.value = tag.id_row;
        this.workService.UpdateByKey(model).subscribe((res) => {
            if (res && res.status == 1) {
                this.LoadData();
                this.SendMessage(true);
                this.changeDetectorRefs.detectChanges();
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
            }
        });
        this.changeDetectorRefs.detectChanges();
    }

    UpdateWorkProcess() {
        // item.id_project_team = this.Id_project_team;
        const dialogRef = this.dialog.open(WorkProcessEditComponent, {
            // width: "40vw",
            // minHeight: "200px",
            data: { Process: this.item.Process, id_project_team: this.Id_project_team },
        });
        dialogRef.afterClosed().subscribe((res) => {
            this.LoadData();
            this.SendMessage(true);
        });
    }

    chinhsuastt(item) {
        item.id_project_team = this.Id_project_team;
        const dialogRef = this.dialog.open(StatusDynamicDialogComponent, {
            width: '40vw',
            minHeight: '200px',
            data: item,
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (res) {
                this.LoadData();
                this.SendMessage(true);
            }
        });
    }

    trackByFn(index, item) {
        return item.id_row;
    }

    DownloadFile(link) {
        window.open(link);
    }

    preview(file) {
        if (file.isImage) {
            this.DownloadFile(file.path);
        } else {
            this.layoutUtilsService.ViewDoc(file.path);
            // return this.dialog.open(previewlistComponent, {
            //     data: file.path,
            //     width: '95vw'
            // });
        }
    }

    getDate(date) {
        if (!date) {
            return new Date();
        }
        const dateParts = date.split('/');
        return new Date(
            (dateParts[1] + '/' + dateParts[0] + '/' + dateParts[2]).toString()
        );
    }

    getComponentName() {
        if (this.DataID) {
            return this.componentName + this.DataID;
        } else {
            return '';
        }
    }

    LoadObjectID() {
        if (this.getComponentName()) {
            this.jeeCommentService.getTopicObjectIDByComponentName(
                this.getComponentName()
            )
                .pipe(
                    tap((res) => {
                        this.objectID = res;
                        this.lstObjectID.push(this.objectID);
                        // this.LoadFullcomment();
                    }),
                    catchError((err) => {
                        return of();
                    }),
                    finalize(() => {
                    }),
                    share()
                )
                .subscribe();
        }
    }

    LoadFullcomment() {
        this.jeeCommentService.showTopicCommentByObjectID(
            this.objectID,
            this.filterTopic()
        )
            .pipe(
                tap((res) => {
                }),
                catchError((err) => {
                    return of();
                }),
                finalize(() => {
                }),
                share()
            )
            .subscribe();
    }

    filterTopic(): QueryFilterComment {
        const filter = new QueryFilterComment();
        filter.ViewLengthComment = 999999;
        return filter;
    }

    getTopicChild(id_topic) {
        if (this.objectID && id_topic) {
            if (!this.ListChild[id_topic] && !this.IsLoading[id_topic]) {
                this.IsLoading[id_topic] = true;
                this.jeeCommentService.showFullComment(
                    this.objectID,
                    id_topic,
                    this.filterTopic()
                )
                    .pipe(
                        tap((res) => {
                            if (!this.ListChild[id_topic]) {
                                this.ListChild[id_topic] = res;
                                this.IsLoading[id_topic] = false;
                            }
                        }),
                        catchError((err) => {
                            return of();
                        }),
                        finalize(() => {
                        }),
                        share()
                    )
                    .subscribe();
            }
        }
    }

    LoadComment(id_topic) {
        if (this.ListChild[id_topic]) {
            return this.ListChild[id_topic];
        } else {
            this.getTopicChild(id_topic);
            return false;
        }
    }

    ChangeValue(value: any) {
        value = false;
        setTimeout((x) => {
            value = true;
        }, 500);
    }

    CheckedChecklist(items) {
        this.workService.CheckedItem(items.id_row).subscribe((res) => {
            if (res && res.status === 1) {
                items.checked = !items.checked;
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
            }
            this.changeDetectorRefs.detectChanges();
        });
    }

    resetComment() {
        const listSub = Object.keys(this.ListChild);
        if (listSub) {
            listSub.forEach((element) => {
                if (this.objectID) {
                    this.jeeCommentService.showFullComment(
                        this.objectID,
                        element,
                        this.filterTopic()
                    )
                        .pipe(
                            tap((res) => {
                                this.ListChild[element] = res;
                            }),
                            catchError((err) => {
                                return of();
                            }),
                            finalize(() => {
                            }),
                            share()
                        )
                        .subscribe();
                }
            });
        }
    }
    ngOnDestroy() {
    }
    @HostListener('window:keyup.esc') onKeyUp() {
        // you have modified this work item. you can save your changes, discard your changes, or cancel to continue editing
        // discard changes, save changes
        this.layoutUtilsService.confirm('Xác nhận', 'Bạn đã sửa đổi công việc này. Bạn có thể lưu các thay đổi, hủy các thay đổi hoặc hủy để tiếp tục chỉnh sửa')
            .then((close) => {
                if (close) {
                    this.dialogRef.close(true);
                }
            })
            // .catch(() => console.log('User dismissed the dialog (e.g., by using ESC, clicking the cross icon, or clicking outside the dialog)'));
            .catch((err) => {
                if (err == 'pad') {
                    this.UpdateDescription();
                    this.dialogRef.close(true);
                }
                else {
                    // console.log('User dismissed the dialog (e.g., by using ESC, clicking the cross icon, or clicking outside the dialog)'));
                }
                debugger
            });
    }
    @HostListener('window:beforeunload', ['$event']) unloadHandler(event: Event) {
        console.log('event:', event);
        event.returnValue = false;
    }
    goBack() {
        this.onKeyUp();
        this.changeDetectorRefs.detectChanges();
    }
    DeleteTask() {
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant(
            'GeneralKey.bancochacchanmuonxoakhong'
        );
        const _waitDesciption = this.translate.instant(
            'GeneralKey.dulieudangduocxoa'
        );
        const _deleteMessage = this.translate.instant(
            'GeneralKey.xoathanhcong'
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
            this.projectsTeamService.DeleteTask(this.item.id_row).pipe(
                tap(() => {
                    if (this.loading) {
                        this.layoutUtilsService.showWaitingDiv();
                    }
                }),
                map((res) => {
                    if (res && res.status == 1) {
                        this.dialogRef.close();
                    } else {
                        this.layoutUtilsService.showError(res.error.message);
                    }
                }),
                catchError((err) => throwError(err)),
                finalize(() => this.layoutUtilsService.OffWaitingDiv())
            ).subscribe(() => {
            });
            // this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false);
        });
    }

    // gửi giao tiếp tới commponent ngoài
    SendMessage(value) {
        this.communicateService.changeMessage(value);
    }

    imagesUploadHandler = (blobInfo, success, failure) => {
    };


}
