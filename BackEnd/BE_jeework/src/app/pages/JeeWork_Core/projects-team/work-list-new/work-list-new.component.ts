import {WorkGroupEditComponent} from './../../work/work-group-edit/work-group-edit.component';
import {
    LayoutUtilsService,
    MessageType,
} from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import {SubheaderService} from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';
import {TokenStorage} from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import {QueryParamsModelNew} from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {MenuPhanQuyenServices} from './../../../../_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service';
import {AttachmentService} from './../../services/attachment.service';
import {
    AttachmentModel,
    FileUploadModel,
} from './../Model/department-and-project.model';
import {AddNewFieldsComponent} from './add-new-fields/add-new-fields.component';
import {StatusDynamicDialogComponent} from './../../status-dynamic/status-dynamic-dialog/status-dynamic-dialog.component';
import {WorkService} from './../../work/work.service';
import {DuplicateTaskNewComponent} from './duplicate-task-new/duplicate-task-new.component';
import {DialogSelectdayComponent} from './../../report/dialog-selectday/dialog-selectday.component';
import {
    WorkModel,
    UpdateWorkModel,
    UserInfoModel,
    WorkDuplicateModel,
    WorkGroupModel,
    GoogleCalendarModel,
} from './../../work/work.model';
import {ColumnWorkModel, DrapDropItem} from './drap-drop-item.model';
import {
    filter,
    tap,
    catchError,
    finalize,
    share,
    takeUntil,
    debounceTime,
    startWith,
    switchMap,
    map,
} from 'rxjs/operators';
import {element} from 'protractor';
import {WeWorkService} from './../../services/wework.services';
import {DatePipe, DOCUMENT} from '@angular/common';
import {TranslateService} from '@ngx-translate/core';
import {FormBuilder, FormControl} from '@angular/forms';
import {Router, ActivatedRoute} from '@angular/router';
import {ProjectsTeamService} from './../Services/department-and-project.service';
import {
    CdkDropList,
    CdkDragDrop,
    moveItemInArray,
    transferArrayItem,
    CdkDragStart,
} from '@angular/cdk/drag-drop';
import {
    Component,
    OnInit,
    Input,
    ViewChild,
    ChangeDetectorRef,
    Inject,
    OnChanges,
    OnDestroy,
} from '@angular/core';
import {MatTable} from '@angular/material/table';
import {MatDialog} from '@angular/material/dialog';
import {MatSort} from '@angular/material/sort';
import * as moment from 'moment';
import {SelectionModel} from '@angular/cdk/collections';
import {workAddFollowersComponent} from '../../work/work-add-followers/work-add-followers.component';
import {WorkAssignedComponent} from '../../work/work-assigned/work-assigned.component';
import {DuplicateWorkComponent} from '../../work/work-duplicate/work-duplicate.component';
import {OverlayContainer} from '@angular/cdk/overlay';
import {BehaviorSubject, of, SubscriptionLike, throwError} from 'rxjs';
import {CommunicateService} from './work-list-new-service/communicate.service';
import {AuthService} from '../../../../modules/auth';

@Component({
    selector: 'kt-work-list-new',
    templateUrl: './work-list-new.component.html',
    styleUrls: ['./work-list-new.component.scss'],
})
export class WorkListNewComponent implements OnInit, OnChanges {

    constructor(
        private CommunicateService: CommunicateService,
        @Inject(DOCUMENT) private document: Document, // multi level
        private _service: ProjectsTeamService,
        private WorkService: WorkService,
        private router: Router,
        public dialog: MatDialog,
        private route: ActivatedRoute,
        private itemFB: FormBuilder,
        public subheaderService: SubheaderService,
        private layoutUtilsService: LayoutUtilsService,
        private changeDetectorRefs: ChangeDetectorRef,
        private translate: TranslateService,
        public datepipe: DatePipe,
        private tokenStorage: TokenStorage,
        private weworkService: WeWorkService,
        private menuServices: MenuPhanQuyenServices,
        private overlayContainer: OverlayContainer,
        private auth: AuthService,
        private _attservice: AttachmentService
    ) {
        this.taskinsert.clear();
        this.filter_groupby = this.listFilter_Groupby[0];
        this.filter_subtask = this.listFilter_Subtask[0];
        this.list_priority = this.weworkService.list_priority;
        this.UserID = this.auth.getUserId();
    }

    @Input() ID_Project = 0;

    @ViewChild('table1', {static: true}) table1: MatTable<any>;
    @ViewChild('table2', {static: true}) table2: MatTable<any>;
    @ViewChild('table3', {static: true}) table3: MatTable<any>;
    @ViewChild('list1', {static: true}) list1: CdkDropList;

    ListtopicObjectID$: BehaviorSubject<any> = new BehaviorSubject<any>([]);
    subscription: SubscriptionLike;
    data: any = [];
    ListColumns: any = [];
    listFilter: any = [];
    ListTasks: any = [];
    ListTags: any = [];
    ListUsers: any = [];
    editmail = 0;
    statusDefault = 0;
    isAssignforme = false;
    // col
    displayedColumnsCol: string[] = [];
    @ViewChild(MatSort, {static: true}) sort: MatSort;
    previousIndex: number;
    ListAction: any = [];
    addNodeitem = 0;
    newtask = -1;
    options_assign: any = {};
    filter_groupby: any = [];
    filter_subtask: any = [];
    list_milestone: any = [];
    Assign_me = -1;
    keyword = '';
    // view setting
    tasklocation = false;
    showsubtask = true;
    showclosedtask = true;
    showclosedsubtask = true;
    showtaskmutiple = true;
    showemptystatus = false;
    status_dynamic: any = [];
    list_priority: any[];
    UserID = 0;
    isEdittitle = -1;
    startDatelist: Date = new Date();
    selection = new SelectionModel<WorkModel>(true, []);
    list_role: any = [];
    ItemFinal = 0;
    ProjectTeam: any = {};
    listNewField: any = [];
    DataNewField: any = [];
    listType: any = [];
    textArea = '';
    searchCtrl: FormControl = new FormControl();
    private readonly componentName: string = 'kt-task_';
    Emtytask = true;
    filterDay = {
        startDate: new Date('09/01/2020'),
        endDate: new Date('09/30/2020'),
    };
    IsAdminGroup = false;
    public column_sort: any = [];

    listField: any = [];

    listStatus: any = [];

    // list da nhiệm
    // nodes: any[] = demoData;

    // ids for connected drop lists
    dropTargetIds = [];
    nodeLookup = {};
    dropActionTodo: DropInfo = null;

    listNewfield: any = [];

    taskinsert = new WorkModel();

    cot = 1;

    Assign: any = [];

    listUser: any[];

    selectedDate: any = {
        startDate: '',
        endDate: '',
    };

    listFilter_Groupby = [
        {
            title: 'Status',
            value: 'status',
        },
        {
            title: 'Assignee',
            value: 'assignee',
        },
        {
            title: 'groupwork',
            value: 'groupwork',
        },
    ];

    listFilter_Subtask = [
        {
            title: 'showtask',
            showvalue: 'showtask',
            value: 'hide',
        },
        {
            title: 'expandall',
            showvalue: 'expandall',
            value: 'show',
        },
    ];

    colorName = '';

    list_Tag: any = [];
    project_team: any = '';

    sortField = [
        {
            title: this.translate.instant('day.theongaytao'),
            value: 'CreatedDate',
        },
        {
            title: this.translate.instant('day.theothoihan'),
            value: 'Deadline',
        },
        {
            title: this.translate.instant('day.theongaybatdau'),
            value: 'StartDate',
        },
    ];

    ngOnInit() {

        // set ngày filter
        const today = new Date();
        this.filterDay = {
            endDate: new Date(today.setMonth(today.getMonth() + 1)),
            startDate: new Date(today.getFullYear(), today.getMonth() - 6, 1),
        };

        // giao tiếp service
        this.subscription = this.CommunicateService.currentMessage.subscribe(
            (message) => {
                if (message) {
                    this.LoadData(false);
                }
            }
        );
        // end giao tiếp service

        this.LoadFilterProject();

        this.column_sort = this.sortField[0];
        this.menuServices.GetRoleWeWork('' + this.UserID)
            .pipe(
                map(res => {
                    if (res && res.status == 1) {
                        this.list_role = res.data.dataRole;
                        this.IsAdminGroup = res.data.IsAdminGroup;
                    }
                    if (!this.CheckRoles(3)) {
                        // this.isAssignforme = false
                    }
                }),
            )
            .subscribe(() => {
                this.LoadData();
                this.GetField();
                this.mark_tag();
                this.LoadListAccount();
                this.LoadDetailProject();
            });

        this.weworkService.lite_milestone(this.ID_Project).subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status == 1) {
                this.list_milestone = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    ngOnDestroy(): void {
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }

    ngOnChanges() {
    }

    LoadDetailProject() {
        this._service.DeptDetail(this.ID_Project).subscribe((res) => {
            if (res && res.status == 1) {
                this.ProjectTeam = res.data;
            }
        });
    }

    CheckClosedProject() {
        if (this.list_role) {
            const x = this.list_role.find((x) => x.id_row == this.ID_Project);
            if (x) {
                if (x.locked) {
                    return false;
                }
            }
        }
        return true;
    }

    CheckRoles(roleID: number) {
        if (this.list_role) {
            const x = this.list_role.find((x) => x.id_row == this.ID_Project);
            if (x && roleID !== 3) {
                if (x.locked) {
                    return false;
                }
            }
            if (this.IsAdminGroup) {
                return true;
            }
            if (x) {
                if (x.admin == true || x.admin == 1 || +x.owner == 1 || +x.parentowner == 1) {
                    return true;
                } else {
                    // if (roleID == 3 || roleID == 4) {
                    //   if (x.isuyquyen && x.isuyquyen != "0") return true;
                    // }
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
                    const role = x.Roles.find((r) => r.id_role == roleID);
                    if (role) {
                        return true;
                    } else {
                        return false;
                    }
                }
            }
        } else {
            if (this.IsAdminGroup) {
                return true;
            }
        }
        return false;
    }

    CheckRoleskeypermit(key) {
        if (this.list_role) {
            const x = this.list_role.find((x) => x.id_row == this.ID_Project);
            if (x) {
                if (x.locked) {
                    return false;
                }
            }
            if (this.IsAdminGroup) {
                return true;
            }

            if (x) {
                if (x.admin == true || x.admin == 1 || +x.owner == 1 || +x.parentowner == 1) {
                    return true;
                } else {
                    // if (key == "id_nv") {
                    //   if (x.isuyquyen && x.isuyquyen != "0") return true;
                    // }
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
                    const role = x.Roles.find((r) => r.keypermit == key);
                    if (role) {
                        return true;
                    } else {
                        return false;
                    }
                }
            } else {
                return false;
            }
        } else if (this.IsAdminGroup) {
            return true;
        }
        return false;
    }

    /** SELECTION */
    CheckedNode(check: any, arr_model: any) {
        const checked = this.selection.selected.find(
            (x) => x.id_row == arr_model.id_row
        );
        const index = this.selection.selected.indexOf(arr_model, 0);
        if (!checked && check.checked) {
            this.selection.selected.push(arr_model);
        } else {
            this.selection.selected.splice(index, 1);
        }
    }

    /** Selects all rows if they are not all selected; otherwise clear selection. */
    masterToggle() {
    }

    async LoadData(loading = true) {
        this.clearList();

        this.data = [];
        // get option new field
        this.GetOptions_NewField();
        // get list new field
        this.weworkService.GetNewField().subscribe((res) => {
            if (res && res.status == 1) {
                this.listNewField = res.data;
            }
        });
        // Load data new field
        await this.LoadNewField();
        // Load data status dynamic
        await this.LoadDataStatusDynamic();
        // Load data work group
        await this.LoadDataWorkGroup();
        // get data work binding data
        // this.LoadDataTask();
        await this.LoadDataTaskNew(loading);

    }

    LoadDataWorkGroup() {
        this.weworkService.lite_workgroup(this.ID_Project)
            .pipe(
                tap((res) => {
                    if (res && res.status == 1) {
                        this.listType = res.data;
                        this.changeDetectorRefs.detectChanges();
                    }
                })
            )
            .subscribe();
    }

    LoadDataTaskNew(loading = true) {
        if (loading) {
            this.layoutUtilsService.showWaitingDiv();
        }
        const queryParams = new QueryParamsModelNew(
            this.filterConfiguration(),
            '',
            '',
            0,
            50,
            true
        );
        this._service.ListTask(queryParams)
            .subscribe((res) => {
                if (res && res.status === 1) {
                    this.listStatus = res.data;
                    this.prepareDragDrop(this.ListTasks);
                    // this.LoadListStatus();
                    this.changeDetectorRefs.detectChanges();
                }
                this.layoutUtilsService.OffWaitingDiv();

            }, (err) => (this.layoutUtilsService.OffWaitingDiv()));
    }


    async LoadNewField() {
        this.weworkService.GetValuesNewFields(this.ID_Project).subscribe(res => {
            if (res && res.status === 1) {
                this.DataNewField = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
    }


    LoadDataStatusDynamic() {
        this.weworkService.ListStatusDynamic(this.ID_Project).subscribe((res) => {
            if (res && res.status == 1) {
                this.status_dynamic = res.data;
                // load ItemFinal
                if (this.status_dynamic) {
                    const x = this.status_dynamic.find((val) => val.IsFinal == true);
                    if (x) {
                        this.ItemFinal = x.id_row;
                    } else {
                    }

                    const itemstatusdefault = this.status_dynamic.find(
                        (x) =>
                            x.isdefault == true &&
                            x.IsToDo == false &&
                            x.IsFinal == false &&
                            x.IsDeadline == false
                    );
                    if (itemstatusdefault) {
                        this.statusDefault = itemstatusdefault.id_row;
                    } else {
                        this.statusDefault = this.status_dynamic[0].id_row;
                    }
                    this.changeDetectorRefs.detectChanges();
                }

            }
        });
    }

    ClosedTask(value, node) {
        if (!this.KiemTraThayDoiCongViec(node, 'closetask', true)) {
            node.closed = !value;
            return;
        }
        this._service.ClosedTask(node.id_row, value).subscribe((res) => {
            this.LoadData();
            if (res && res.status == 1) {
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    GetDataNewField(id_work, id_field, isDropdown = false, getColor = false) {

        const x = this.DataNewField.find(
            (x) => x.WorkID == id_work && x.FieldID == id_field
        );
        if (x) {
            if (isDropdown) {
                const list = this.listNewfield.find(
                    (element) => element.FieldID == id_field && element.RowID == x.Value
                );
                if (list) {
                    if (getColor) {
                        return list.Color;
                    }
                    return list.Value;
                }
                return '--';
            }
            return x.Value;
        }
        return '-';
        // if()
    }

    UpdateValueField(value, node, field) {
        this.editmail = 0;
        if (field != 'date') {
            if (value == '' || value == null || value == undefined) {
                if (field.fieldname != 'checkbox') {
                    return;
                }
            }
        }
        if (!this.KiemTraThayDoiCongViec(node, field.fieldname)) {
            return;
        }

        const idWork = node.id_row;
        const _item = new UpdateWorkModel();
        _item.clear();
        _item.FieldID = field.id_row;
        _item.Value = value;
        _item.WorkID = idWork;
        _item.TypeID = field.typeid;
        this._service.UpdateNewField(_item).subscribe((res) => {
            if (res && res.status == 1) {
                this.LoadData();
            }
        });
    }

    LoadNhomCongViec(id) {
        const x = this.listType.find((x) => x.id_row == id);
        if (x) {
            return x.title;
        }
        return 'Chưa phân loại';
    }

    UpdateValue() {
    }

    filterConfiguration(): any {
        const filter: any = {};
        filter.id_project_team = this.ID_Project;
        filter.groupby = this.filter_groupby.value; // assignee
        filter.keyword = this.keyword;
        filter.TuNgay = this.f_convertDate(this.filterDay.startDate).toString();
        filter.DenNgay = this.f_convertDate(this.filterDay.endDate).toString();
        filter.collect_by = this.column_sort.value;
        filter.displayChild = 1;
        if (this.showclosedsubtask) {
            filter.subtask_done = this.showclosedsubtask;
        }
        if (this.showclosedtask) {
            filter.task_done = this.showclosedtask;
        }
        if (this.isAssignforme) {
            filter.forme = this.isAssignforme;
        } else {
            filter.everyone = !this.isAssignforme;
        }
        return filter;
    }

    getColorStatus(val) {
        const index = this.status_dynamic.find((x) => x.id_row == val);
        if (index) {
            return index.color;
        } else {
            return 'gray';
        }
    }

    // TenCot
    GetField() {
        this.weworkService.GetListField(this.ID_Project, 3, false).subscribe(
            (res) => {
                if (res && res.status == 1) {
                    this.listField = res.data;
                    this.ListColumns = res.data;
                    // xóa title khỏi cột
                    const colDelete = ['title'];
                    colDelete.forEach((element) => {
                        const indextt = this.ListColumns.findIndex(
                            (x) => x.fieldname == element
                        );
                        if (indextt >= 0) {
                            this.ListColumns.splice(indextt, 1);
                        }
                    });
                    this.ListColumns.sort((a, b) =>
                        a.id_project_team > b.id_project_team
                            ? -1
                            : b.id_project_team > a.id_project_team
                                ? 1
                                : 0
                    ); // nào chọn xếp trước
                    this.changeDetectorRefs.detectChanges();
                }
            }
        );
    }

    isItemFinal(id) {
        if (id == this.ItemFinal) {
            return true;
        }
        return false;
    }

    LoadListStatus(loading = false) {
        if (loading) {
            this.layoutUtilsService.showWaitingDiv();
        }
        this.listFilter.forEach((val) => {
            val.data = [];
        });

        if (this.ListTasks && this.ListTasks.length > 0) {

            this.ListTasks.forEach((element) => {
                element.isExpanded =
                    this.filter_subtask.value == 'show' ||
                    this.addNodeitem == element.id_row
                        ? true
                        : false;
                this.listFilter.forEach((val) => {
                    if (this.CheckDataStatus(val, element)) {
                        val.data.push(element);
                        if (element.end_date == null) {
                            this.Emtytask = false;
                        }

                    } else if (this.CheckDataAssigne(val, element)) {
                        if (
                            element.Users?.length == 1 ||
                            (this.UserNull(element.Users) && val.id_row == '') ||
                            (element.Users?.length > 1 && val.id_row == '0')
                        ) {
                            val.data.push(element);
                        }
                    } else if (this.CheckDataWorkGroup(val, element)) {
                        val.data.push(element);
                    }
                });
            });
        }
        this.listStatus = this.listFilter;
        if (loading) {
            this.layoutUtilsService.OffWaitingDiv();
        }
        this.changeDetectorRefs.detectChanges();
    }

    isShowStatus(status) {
        if (status.datawork.length > 0) {
            return true;
        }
        if (status.datawork.length == 0 && this.showemptystatus) {
            return true;
        }
        if (this.Emtytask && status.id_row == this.statusDefault) {
            if (this.newtask < 0) {
                this.newtask = this.statusDefault;
            }

            return true;
        }
        return false;
    }

    CheckCustomfield(node, item) {
        return this.editmail != (node.id_row + item.Id_row.toString());
    }

    TestUpdateKey(node) {
        this.UpdateByKey(node, 'title', node.title + ' +1');
    }

    CheckDataStatus(valuefilter, elementTask) {
        if (
            this.filter_groupby.value == 'status' &&
            valuefilter.id_row == +elementTask.status
        ) {
            // if (this.isAssignforme) {
            //     if (!this.FindUser(elementTask.Users, this.UserID)) {
            //         return false;
            //     }
            // }
            // if (!this.isAssignforme) {
            // // kiểm tra có phải người được giao hay người tạo hay không
            // if (
            //     this.isAssignForme(elementTask) || elementTask.createdby == this.UserID
            // ) {
            //     return true;
            // }
            // } else {
            return true;
            // }
        }
        return false;
    }

    // isAssignForme(elementTask) {
    //     if (
    //         elementTask.createdby == this.UserID ||
    //         this.FindUser(elementTask.Users, this.UserID) ||
    //         this.FindUser(elementTask.Follower, this.UserID) ||
    //         this.FindUser(elementTask.UserSubtask, this.UserID)
    //     ) {
    //         return true;
    //     }
    //     return false;
    // }

    CheckDataAssigne(valuefilter, elementTask) {
        if (this.filter_groupby.value == 'assignee') {
            // if (this.isAssignforme) {
            //     if (!this.FindUser(elementTask.Users, this.UserID)) {
            //         return false;
            //     }
            // }
            // if (!this.isAssignforme) {
            //     if (
            //         (this.FindUser(elementTask.Users, valuefilter.id_row) &&
            //             this.isAssignForme(elementTask)) ||
            //         (elementTask.createdby == this.UserID &&
            //             this.UserNull(elementTask.Users))
            //     ) {
            //         return true;
            //     }
            // } else {
            if (
                this.FindUser(elementTask.Users, valuefilter.id_row) ||
                (this.UserNull(elementTask.Users) && valuefilter.id_row == '') ||
                (elementTask.Users?.length > 1 && valuefilter.id_row == '0')
            ) {
                return true;
            }
            // }
        }
        return false;
    }

    FindUser(listUser, iduser) {
        if (listUser) {
            const x = listUser.find((x) => x.userid == iduser);
            if (x) {
                return true;
            }
        }
        return false;
    }

    UserNull(listUser) {
        if (listUser) {
            if (listUser.length > 0) {
                return false;
            }
        }
        return true;
    }

    CheckDataWorkGroup(valuefilter, elementTask) {
        if (
            this.filter_groupby.value == 'groupwork' &&
            elementTask.id_group == valuefilter.id_row
        ) {
            // if (this.isAssignforme) {
            //     if (!this.FindUser(elementTask.Users, this.UserID)) {
            //         return false;
            //     }
            // }
            // if (!this.isAssignforme) {
            //     // kiểm tra có phải người được giao hay người tạo hay không
            //     if (
            //         this.isAssignForme(elementTask)
            //     ) {
            //         return true;
            //     }
            // } else {
            return true;
            // }
        }
        return false;
    }

    drop1(event: CdkDragDrop<string[]>) {
        moveItemInArray(this.ListTasks, event.previousIndex, event.currentIndex);
    }

    drop2(event: CdkDragDrop<string[]>) {
        moveItemInArray(this.ListColumns, event.previousIndex, event.currentIndex);
        const item = this.ListColumns[event.currentIndex];
        const itemDrop = new DrapDropItem();
        itemDrop.id_row = 0;
        itemDrop.typedrop = 5;
        itemDrop.id_from = event.previousIndex;
        itemDrop.id_to = event.currentIndex;
        itemDrop.IsAbove = event.previousIndex > event.currentIndex ? true : false;
        itemDrop.fieldname = item.fieldname;
        itemDrop.status = 0;
        itemDrop.status_from = 0;
        itemDrop.status_to = 0;
        itemDrop.priority_from = 0; // neeus bang 2 thi xet
        itemDrop.id_parent = 0;
        itemDrop.id_project_team = +item.id_project_team;
        this.DragDropItemWork(itemDrop);
    }

    dragStarted(event: CdkDragStart, index: number) {
        this.previousIndex = index;
    }

    prepareDragDrop(nodes: any[]) {
        nodes.forEach((node) => {
            this.dropTargetIds.push(node.id_row);
            this.nodeLookup[node.id_row] = node;
            if (node.children == null) {
                node.children = [];
            }
            this.prepareDragDrop(node.children);

        });
    }

    DragDropItemWork(item) {
        const dropItem = new DrapDropItem();
        this._service.DragDropItemWork(item).subscribe((res) => {
        });
    }

    UpdateCol(fieldname) {
        // var item:any=[];
        // item.id_project_team = this.ID_Project;
        // item.columnname = fieldname;
        const item = new ColumnWorkModel();
        item.id_project_team = +this.ID_Project;
        item.columnname = fieldname;
        this._service.UpdateColumnWork(item).subscribe((res) => {
            if (res && res.status == 1) {
                this.GetField();
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    GetOptions_NewField() {
        this.weworkService.GetOptions_NewField(this.ID_Project, 0, 3).subscribe(
            (res) => {
                if (res && res.status == 1) {
                    this.listNewfield = res.data;
                }
            }
        );
    }

    getDropdownField(idField) {
        const list = this.listNewfield.filter((x) => x.FieldID == idField);
        if (list) {
            return list;
        }
        return [];
    }

    focusInput(idtask, idfield) {
        if (!this.CheckClosedProject()) {
            return;
        }
        this.editmail = idtask.toString() + idfield.toString();
    }

    setCheckField(event) {
    }

    onSelectFile(event) {
        if (event.target.files && event.target.files[0]) {
            const reader = new FileReader();
            reader.readAsDataURL(event.target.files[0]);
            let base64Str: any = '';
            reader.onload = (event) => {
                base64Str = event.target.result;
                const metaIdx = base64Str.indexOf(';base64,');
                const strBase64 = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64
                // var icon = { filename: filesAmount.name, strBase64: strBase64, base64Str: base64Str };
                // this.changeDetectorRefs.detectChanges();
            };
        }
    }

    // @debounce(50)
    dragMoved(event) {
        const e = this.document.elementFromPoint(
            event.pointerPosition.x,
            event.pointerPosition.y
        );

        if (!e) {
            this.clearDragInfo();
            return;
        }
        const container = e.classList.contains('node-item')
            ? e
            : e.closest('.node-item');
        if (!container) {
            this.clearDragInfo();
            return;
        }
        this.dropActionTodo = {
            targetId: container.getAttribute('data-id'),
        };
        const targetRect = container.getBoundingClientRect();
        const oneThird = targetRect.height / 3;

        if (event.pointerPosition.y - targetRect.top < oneThird) {
            // before
            this.dropActionTodo.action = 'before';
        } else if (event.pointerPosition.y - targetRect.top > 2 * oneThird) {
            // after
            this.dropActionTodo.action = 'after';
        } else {
            // inside
            this.dropActionTodo.action = 'inside';
        }
        this.showDragInfo();
    }

    drop(event) {
        const itemDrop = new DrapDropItem();
        const draggedItemId = event.item.data; // get data -- id
        const parentItemId = event.previousContainer.id; // từ thằng cha hiện tại
        const status = 0;
        const listdata = this.ListTasks;
        if (!this.dropActionTodo) {
            return;
        }
        // load list new data
        const draggedItem = this.nodeLookup[draggedItemId]; // lấy item từ node

        let listDatanew = this.ListTasks;
        // cách này chỉ dùng gọi data trong 1 bảng
        const stt = draggedItem.status;
        const newArr = this.listStatus.find((x) => x.id_row == stt);
        if (newArr) {
            listDatanew = newArr.data;
        }
        // get list data từ list bỏ đi
        // if(parentItemId=='main'){
        //   //get list bỏ đi
        //   //draggedItem.id_parent = '';
        //   // draggedItem.status = id_row?

        // }

        const targetListId = this.getParentNodeId(
            this.dropActionTodo.targetId,
            listDatanew,
            'main'
        ); // thằng cha mới nếu ngoài cùng thì = main

        // get list data nơi muốn đến
        // if(targetListId=='main' && parentItemId!='main' ){
        //   //get list muốn đến
        //   // draggedItem.status = id_row?
        //   var stt = draggedItem.status;
        //   var newArr = this.listStatus.find(x => x.id_row == stt);
        //   if(newArr){
        //     listDatanew = newArr.data;
        //   }
        // }
        const text =
            '\nmoving\n[' +
            draggedItemId +
            '] from list [' +
            parentItemId +
            ']' +
            ',,,' +
            '\n[' +
            this.dropActionTodo.action +
            ']\n[' +
            this.dropActionTodo.targetId +
            '] from list [' +
            targetListId +
            ']';
        itemDrop.id_from = draggedItemId;
        itemDrop.id_to = +this.dropActionTodo.targetId;
        if (this.dropActionTodo.action == 'before') {
            itemDrop.IsAbove = true;
        } else {
            itemDrop.IsAbove = false;
        }
        // Set công việc con 2 cấp
        // set parent cho node
        if (this.dropActionTodo.action == 'inside') {
            // nếu inside thì nhận node inside làm cha
            const nodeParent = this.nodeLookup[this.dropActionTodo.targetId]; // lấy item từ node
            if (nodeParent.id_parent == null) {
                nodeParent.id_parent = '';
            }
            if (nodeParent.id_parent != '') {
                return;
            } else {
                if (draggedItem.children == null) {
                    draggedItem.children = [];
                }
                if (draggedItem.children.length)
                    // nếu có node con thì out không cho phép ghép các node
                {
                    return;
                }
                draggedItem.id_parent = this.dropActionTodo.targetId;
            }
        } else {
            // khác thì lấy thằng cha của node mới
            if (targetListId == 'main') {
                // node ngoài cùng
                draggedItem.id_parent = '';
            } else {
                // node con
                if (draggedItem.children.length)
                    // nếu có node con thì out không cho phép ghép các node
                {
                    return;
                }
                draggedItem.id_parent = targetListId;
            }
        }
        // list data phai la list tất cả thằng cha trong bảng
        const oldItemContainer =
            parentItemId != 'main'
                ? this.nodeLookup[parentItemId].children
                : listDatanew; // lấy con từ thằng cha nế thằng cha main thì lấy nguyên cây
        const newContainer =
            targetListId != 'main'
                ? this.nodeLookup[targetListId].children
                : listDatanew; // lấy list muốn đưa tới chuẩn bị map vào list này

        const i = oldItemContainer.findIndex((c) => c.id_row == draggedItemId); // lấy index từ list cũ
        oldItemContainer.splice(i, 1); // cắt item từ list bỏ đi
        // set parent
        switch (this.dropActionTodo.action) {
            case 'before':
            case 'after':
                const targetIndex = newContainer.findIndex(
                    (c) => c.id_row == this.dropActionTodo.targetId
                ); // tìm id thằng được map - so sánh với thằng được làm mốc
                if (this.dropActionTodo.action == 'before') {
                    newContainer.splice(targetIndex, 0, draggedItem); // nếu trước nó thì index của thằng mới kéo thay vào index thằng làm mốc
                } else {
                    newContainer.splice(targetIndex + 1, 0, draggedItem); // nếu đứng sau thì index thằng làm mốc + 1
                }
                break;

            case 'inside': // đưa vào trong làm con của thằng node được chọn
                this.nodeLookup[this.dropActionTodo.targetId].children.push(
                    draggedItem
                ); // get ID node được chọn push item mới vào đó
                this.nodeLookup[this.dropActionTodo.targetId].isExpanded = true; // trạng thái đang mở node
                break;
        }
        itemDrop.typedrop = 1;
        itemDrop.status = +draggedItem.status;
        itemDrop.status_from = +draggedItem.status;
        itemDrop.status_to = +draggedItem.status;
        itemDrop.priority_from = 0; // neeus bang 2 thi xet
        itemDrop.id_row = +draggedItem.id_row;
        itemDrop.id_project_team = +draggedItem.id_project_team;
        itemDrop.id_parent = +draggedItem.id_parent;
        this.DragDropItemWork(itemDrop);
        this.clearDragInfo(true); // xóa thằng this.dropActionTodo  set nó về null
    }

    getParentNodeId(id: string, nodesToSearch: any[], parentId: string): string {
        const findNode = nodesToSearch.find((x) => x.id_row == id);
        if (findNode) {
            return parentId;
        } else {
            for (const node of nodesToSearch) {

                // if (node.id_row == id) return parentId;
                if (node.children == null) {
                    node.children = [];
                }
                const ret = this.getParentNodeId(id, node.children, node.id_row);
                if (ret) {
                    return ret;
                }
            }
            return null;
        }
    }

    showDragInfo() {
        this.clearDragInfo();
        if (this.dropActionTodo) {
            this.document
                .getElementById('node-' + this.dropActionTodo.targetId)
                .classList.add('drop-' + this.dropActionTodo.action);
        }
    }

    clearDragInfo(dropped = false) {
        if (dropped) {
            this.dropActionTodo = null;
        }
        this.document
            .querySelectorAll('.drop-before')
            .forEach((element) => element.classList.remove('drop-before'));
        this.document
            .querySelectorAll('.drop-after')
            .forEach((element) => element.classList.remove('drop-after'));
        this.document
            .querySelectorAll('.drop-inside')
            .forEach((element) => element.classList.remove('drop-inside'));
    }

    AddnewTask(val, task = false) {
        if (task) {
            this.newtask = val;
            this.addNodeitem = 0;
        } else {
            this.addNodeitem = val;
            this.newtask = -1;
        }
    }

    editTitle(val) {
        this.isEdittitle = val;
        const ele = document.getElementById('task' + val) as HTMLInputElement;
        setTimeout(() => {
            ele.focus();
        }, 50);
    }

    focusOutFunction(event, node) {
        this.isEdittitle = -1;
        if (event.target.value.trim() == node.title.trim() || event.target.value.trim() == '') {
            event.target.value = node.title;
            return;
        } else {
            node.title = event.target.value;
        }
        this.UpdateByKey(node, 'title', event.target.value.trim());
    }

    focusFunction(val) {
    }

    CloseAddnewTask(val) {
        if (val) {
            this.addNodeitem = 0;
            this.newtask = -1;
        }
    }

    AddTask(item) {
        // WorkModel

        const task = new WorkModel();
        task.status = item.id_row;
        task.title = this.taskinsert.title;
        task.id_project_team = this.ID_Project;
        task.Users = [];
        if (this.Assign.id_nv > 0) {
            const assign = this.AssignInsert(this.Assign);
            task.Users.push(assign);
        }
        const start = moment();
        if (
            moment(this.selectedDate.startDate).format('MM/DD/YYYY') != 'Invalid date'
        ) {
            task.start_date = moment(this.selectedDate.startDate).format(
                'MM/DD/YYYY'
            );
        }
        if (
            moment(this.selectedDate.endDate).format('MM/DD/YYYY') != 'Invalid date'
        ) {
            task.end_date = moment(this.selectedDate.endDate).format('MM/DD/YYYY');
            task.deadline = moment(this.selectedDate.endDate).format('MM/DD/YYYY');
        }

        this._service.InsertTask(task).subscribe((res) => {
            if (res && res.status == 1) {
                this.CloseAddnewTask(true);
                this.LoadData();
            }
        });
    }

    AssignInsert(assign) {
        let NV = new UserInfoModel();
        NV = assign;
        NV.id_user = assign.id_nv;
        NV.loai = 1;
        return NV;
    }

    bindStatus(val) {
        const stt = this.status_dynamic.find((x) => +x.id_row == +val);
        if (stt) {
            return stt.statusname;
        }
        return this.translate.instant('GeneralKey.chuagantinhtrang');
    }

    clickOutside() {
        if (this.addNodeitem > 0) {
            this.addNodeitem = 0;
        }
    }

    Themcot() {
        // this.ListColumns.push({
        //   fieldname: "cot" + this.cot,
        //   isbatbuoc: true,
        //   isnewfield: false,
        //   isvisible: false,
        //   position: this.ListColumns.length,
        //   title: "Cột" + this.cot,
        //   type: null
        // })
        // this.cot++;
    }

    // Assign
    ItemSelected(val: any, task, remove = false) {
        // chọn item
        if (remove) {
            val.id_nv = val.userid;
        }
        this.UpdateByKey(task, 'assign', val.id_nv, false);
    }

    LoadListAccount() {
        const filter: any = {};
        filter.id_project_team = this.ID_Project;
        this.weworkService.list_account(filter).subscribe((res) => {
            if (res && res.status == 1) {
                this.listUser = res.data;
                // this.setUpDropSearchNhanVien();
            }
            this.options_assign = this.getOptions_Assign();
            // this.changeDetectorRefs.detectChanges();
        });
    }

    stopPropagation(event) {
        event.stopPropagation();
    }

    getOptions_Assign() {
        const options_assign: any = {
            showSearch: true,
            keyword: '',
            data: this.listUser,
        };
        return options_assign;
    }

    Selectdate() {
        const dialogRef = this.dialog.open(DialogSelectdayComponent, {
            width: '500px',
            data: this.selectedDate,
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (result != undefined) {
                this.selectedDate.startDate = new Date(result.startDate);
                this.selectedDate.endDate = new Date(result.endDate);
            }
        });
    }

    ViewDetai(item) {
        this.router.navigate([
            '',
            {outlets: {auxName: 'aux/detail/' + item.id_row}},
        ]);
    }

    f_convertDate(v: any) {
        if (v != '' && v != undefined) {
            const a = new Date(v);
            return (
                ('0' + a.getDate()).slice(-2) +
                '/' +
                ('0' + (a.getMonth() + 1)).slice(-2) +
                '/' +
                a.getFullYear()
            );
        }
    }

    viewdate() {
        if (this.selectedDate.startDate == '' && this.selectedDate.endDate == '') {
            return 'Set due date';
        } else {
            const start = this.f_convertDate(this.selectedDate.startDate);
            const end = this.f_convertDate(this.selectedDate.endDate);
            return start + ' - ' + end;
        }
    }

    GroupBy(item) {
        if (item == this.filter_groupby) {
            return;
        }
        this.filter_groupby = item;
        this.UpdateInfoProject();
        this.LoadData();
    }

    ExpandNode(node) {
        if (this.filter_subtask.value == 'show') {
            return;
        } else {
            node.isExpanded = !node.isExpanded;
        }
    }

    ShowCloseTask() {
        this.showclosedtask = !this.showclosedtask;
        this.UpdateInfoProject();
        this.LoadData();
    }

    ShowClosesubTask() {
        this.showclosedsubtask = !this.showclosedsubtask;
        this.UpdateInfoProject();
        this.LoadData();
    }


    // loadSubtask() {
    //     const isExpanded = this.filter_subtask.value == 'show' ? true : false;
    //
    //     for (const i of this.listStatus) {
    //         i.datawork.forEach((element) => {
    //             element.isExpanded = isExpanded;
    //         });
    //     }
    // }

    Subtask(item) {

        if (item.value == this.filter_subtask.value) {
            return;
        }

        this.filter_subtask = item;
        // this.loadSubtask();
    }

    CreateTask(val) {
        const x = this.newtask;
        this.CloseAddnewTask(true);
        setTimeout(() => {
            this.newtask = x;
        }, 1000);
        this._service.InsertTask(val).subscribe((res) => {
            if (res && res.status == 1) {
                this.LoadData();
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    DeleteTask(task) {
        this._service.DeleteTask(task.id_row).subscribe((res) => {
            if (res && res.status == 1) {
                this.LoadData();
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    SelectFilterDate() {
        const dialogRef = this.dialog.open(DialogSelectdayComponent, {
            width: '500px',
            data: this.filterDay,
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (result != undefined) {
                this.filterDay.startDate = new Date(result.startDate);
                this.filterDay.endDate = new Date(result.endDate);
                this.UpdateInfoProject();
                this.LoadData();
            }
        });
    }

    clearList() {
        this.selection = new SelectionModel<WorkModel>(true, []);
    }

    IsAdmin() {
        if (this.IsAdminGroup) {
            return true;
        }
        if (this.list_role) {
            const x = this.list_role.find((x) => x.id_row == this.ID_Project);
            if (x) {
                if (x.admin == true || x.admin == 1 || +x.owner == 1 || +x.parentowner == 1) {
                    return true;
                }
            }
        }
        return false;
    }

    UpdateStatus(task, status) {
        if (+task.status == +status.id_row) {
            return;
        }
        // if (this.IsAdmin()) {
        //   this.UpdateByKey(task, "status", status.id_row);
        // } else {
        //   if (status.Follower) {
        //     if (status.Follower == this.UserID) {
        //       this.UpdateByKey(task, "status", status.id_row);
        //     } else {
        //       this.layoutUtilsService.showError(
        //         "Không có quyền thay đổi trạng thái"
        //       );
        //     }
        //   } else {
        //     this.UpdateByKey(task, "status", status.id_row);
        //   }
        // }
        this.UpdateByKey(task, 'status', status.id_row);
    }

    CreateEvent(node) {
        this.GoogleCalender(node, 'google', node.title + '+1');
    }

    GoogleCalender(task, key, value, isReloadData = true) {
        const item = new GoogleCalendarModel();
        item.id_row = task.id_row;
        this.WorkService.Sysn_Google_Calender(item).subscribe((res) => {
            if (res && res.status == 1) {
                this.LoadData();
            } else {
                if (isReloadData) {
                    setTimeout(() => {
                        this.LoadData();
                    }, 500);
                }
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    UpdateByKey(task, key, value, isReloadData = true) {
        if (!this.KiemTraThayDoiCongViec(task, key)) {
            return;
        }
        const item = new UpdateWorkModel();
        item.id_row = task.id_row;
        item.key = key;
        item.value = value;
        if (task.id_nv > 0) {
            item.IsStaff = true;
        }
        this._service._UpdateByKey(item).subscribe((res) => {
            if (res && res.status == 1) {
                this.LoadData();
            } else {
                // if (isReloadData) {
                setTimeout(() => {
                    this.LoadData();
                }, 500);
                // }
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    GetColorName(val) {
        // name
        this.weworkService.getColorName(val).subscribe((res) => {
            this.colorName = res.data.Color;
            return this.colorName;
        });
    }

    getTenAssign(val) {
        const list = val.split(' ');
        return list[list.length - 1];
    }

    Updateestimates(task, event) {
        this.UpdateByKey(task, 'estimates', event);
    }

    UpdateGroup(node, id_row) {
        if (id_row == 0) {
            this.UpdateByKey(node, 'id_group', null);
        } else {
            this.UpdateByKey(node, 'id_group', id_row);
        }
    }

    updateDate(task, date, field) {
        this.UpdateByKey(task, field, date);
    }

    updatePriority(task, field, value) {
        this.UpdateByKey(task, field, value);
    }

    UpdateTask(task) {
        this._service.UpdateTask(task.id_row).subscribe((res) => {
            this.LoadData();
            if (res && res.status == 1) {
                this.LoadData();
            }
        });
    }

    DeleteByKey(task, field) {
        this.UpdateByKey(task, field, null);
    }

    getAssignee(id_nv) {
        if (+id_nv > 0 && this.listUser) {
            const assign = this.listUser.find((x) => x.id_nv == id_nv);
            if (assign) {
                return assign;
            }
            return false;
        }
        return false;
    }

    getPriority(id) {
        const item = this.list_priority.find((x) => x.value == id);
        if (item) {
            return item;
        }
        return id;
    }

    duplicateNew(node, type) {
        this.Update_duplicateNew(node, type);
    }

    Update_duplicateNew(_item: WorkDuplicateModel, type) {
        const dialogRef = this.dialog.open(DuplicateTaskNewComponent, {
            width: '40vw',
            minHeight: '60vh',
            data: {_item, type},
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                // this.ngOnInit();
            } else {
                // this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
                this.LoadData();
            }
        });
    }

    duplicate(type: number) {
        const model = new WorkDuplicateModel();
        model.clear();

        model.type = type;
        model.type = type;
        this.Update_duplicate(model);
    }

    Update_duplicate(_item: WorkDuplicateModel) {
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam += 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = MessageType.Create;
        const dialogRef = this.dialog.open(DuplicateWorkComponent, {
            data: {_item},
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
                this.ngOnInit();
            }
        });
    }

    work() {
        const model = new WorkModel();
        model.clear();
    }

    assign(node) {
        const item = this.getOptions_Assign();
        const dialogRef = this.dialog.open(WorkAssignedComponent, {
            width: '500px',
            height: '500px',
            data: {item},
        });
        dialogRef.afterClosed().subscribe((res) => {
            this.UpdateByKey(node, 'assign', res.id_nv);
        });
    }

    Add_followers() {
        let saveMessageTranslateParam = '';
        const _item = new WorkModel();
        // _item = this.detail;
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? 'GeneralKey.capnhatthanhcong'
                : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
            _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(workAddFollowersComponent, {
            data: {_item},
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
                this.ngOnInit();
            }
        });
    }

    Delete() {
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
        });
    }

    // nhóm công việc
    Assignmore() {
        const item = this.getOptions_Assign();
        const dialogRef = this.dialog.open(WorkAssignedComponent, {
            width: '500px',
            height: '500px',
            data: {item, ID_Project: this.ID_Project},
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (res) {
                this.selection.selected.forEach((element) => {
                    this.UpdateByKey(element, 'assign', res.id_nv);
                });
            }
        });
    }

    // nhóm status
    UpdateStatuslist(status) {
        if (this.IsAdmin()) {
        } else {
            if (status.Follower) {
                if (status.Follower != this.UserID) {
                    this.layoutUtilsService.showError(
                        'Không có quyền thay đổi trạng thái'
                    );
                }
            }
        }

        this.selection.selected.forEach((element) => {
            this.UpdateByKey(element, 'status', status.id_row);
            // this.UpdateByKey(task, 'status', status.id_row);
        });
    }

    // nhóm start date
    updateStartDateList() {
        const date = moment(this.startDatelist).format('MM/DD/YYYY HH:mm');
        this.selection.selected.forEach((element) => {
            this.UpdateByKey(
                element,
                'start_date',
                moment(this.startDatelist).format('MM/DD/YYYY HH:mm')
            );
            // this.UpdateByKey(task, 'status', status.id_row);
        });
    }

    // nhóm độ ưu tiên
    updatePrioritylist(value) {
        this.selection.selected.forEach((element) => {
            this.UpdateByKey(element, 'clickup_prioritize', value);
        });
    }

    // nhóm xóa cv
    XoaCVList() {
        this.selection.selected.forEach((element) => {
            this.DeleteTask(element);
        });
    }

    getViewCheck(node) {
        const checked = this.selection.selected.find((x) => x.id_row == node);
        if (checked) {
            return 1;
        }
        return '';
    }

    Chontatca(node) {
        const list = node.data;
        list.forEach((element) => {
            const checked = this.selection.selected.find(
                (x) => x.id_row == element.id_row
            );
            if (!checked) {
                this.selection.selected.push(element);
            }
        });
    }

    // lisst dupliacte
    UpdateListDuplicate(type) {
        const time = this.selection.selected.length * 500;
        if (type == 1) {
            this.selection.selected.forEach((element) => {
                setTimeout(() => {
                    this.DuplicateTask(element, type, this.ID_Project);
                }, 100);
            });
        } else if (type == 2) {
            const saveMessageTranslateParam = 'GeneralKey.themthanhcong';
            const _saveMessage = this.translate.instant(saveMessageTranslateParam);
            const _messageType = MessageType.Create;
            const dialogRef = this.dialog.open(DuplicateTaskNewComponent, {
                data: {getOnlyIDproject: true},
            });
            dialogRef.afterClosed().subscribe((res) => {
                if (res) {
                    this.selection.selected.forEach((element) => {
                        this.DuplicateTask(element, type, res);
                    });
                }
            });
        }

        setTimeout(() => {
            this.clearList();
            this.LoadData();
        }, time);
    }

    DuplicateTask(task, type, id_project_team = 0) {
        const duplicate = new WorkDuplicateModel();
        duplicate.clear();
        duplicate.title = task.title;
        duplicate.type = type;
        duplicate.id = task.id_row;
        duplicate.description = task.description ? task.description : '';
        duplicate.id_parent = task.id_parent ? task.id_parent : 0;
        duplicate.id_project_team = id_project_team;
        if (task.deadline) {
            duplicate.deadline = task.deadline;
        }
        if (task.start_date) {
            duplicate.start_date = task.start_date;
        }
        if (task.id_nv) {
            duplicate.assign = task.id_nv;
        }
        duplicate.id_group = 0;
        duplicate.followers = [];
        duplicate.urgent = 'true';
        duplicate.required_result = 'true';
        duplicate.require = 'true';
        duplicate.Users = [];
        duplicate.duplicate_child = 'true';
        this.Create(duplicate);
    }

    Create(_item: WorkDuplicateModel) {
        this._service.DuplicateCU(_item).subscribe((res) => {
            if (res && res.status == 1) {
                this.layoutUtilsService.showActionNotification(
                    'Nhân bản thành công',
                    MessageType.Read,
                    3000,
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

    mark_tag() {
        this.weworkService.lite_tag(this.ID_Project).subscribe((res) => {
            if (res && res.status == 1) {
                this.list_Tag = res.data;
                // this.changeDetectorRefs.detectChanges();
            }
        });
    }

    ReloadData(event) {
        if (event) {
            this.ngOnInit();
        }
    }

    RemoveTag(tag, item) {
        const model = new UpdateWorkModel();
        model.id_row = item.id_row;
        model.key = 'Tags';
        model.value = tag.id_row;
        this.WorkService.UpdateByKey(model).subscribe((res) => {
            if (res && res.status == 1) {
                this.LoadData();
                // this.layoutUtilsService.showActionNotification(this.translate.instant('work.dachon'), MessageType.Read, 1000, false, false, 3000, 'top', 1);
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

    chinhsuanoidung(item) {
        if (this.filter_groupby.value == 'status') {
            this.chinhsuastt(item);
        }
        if (this.filter_groupby.value == 'groupwork') {
            this.chinhsuaNhomCV(item);
        }
    }

    chinhsuastt(item) {
        item.id_project_team = this.ID_Project;
        const dialogRef = this.dialog.open(StatusDynamicDialogComponent, {
            width: '40vw',
            minHeight: '200px',
            data: item,
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (res) {
                this.LoadData();
            }
        });
    }

    chinhsuaNhomCV(item) {
        let saveMessageTranslateParam = '';
        const _item = new WorkGroupModel();
        _item.clear();
        _item.id_project_team = '' + this.ID_Project;
        if (item && item.id_row) {
            _item.id_row = item.id_row;
            _item.title = item.statusname;
            _item.description = item.description;
        }
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? 'GeneralKey.capnhatthanhcong'
                : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
            _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(WorkGroupEditComponent, {
            data: {_item},
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                return;
            } else {
                this.layoutUtilsService.showActionNotification(
                    _saveMessage,
                    _messageType,
                    4000,
                    true,
                    false
                );
                this.LoadData();
            }
        });
    }

    getNhom() {
        return this.filter_groupby.value;
    }

    getDeadline(fieldname, date) {
        if (fieldname == 'deadline') {
            if (new Date(date) < new Date())
                // return 'text-danger'
            {
                return 'red-color';
            }
        }
        return '';
    }

    preventCloseOnClickOutTextArea(value) {
        this.textArea = value == '-' ? '' : value;
    }

    allowCloseOnClickOutTextArea(idWork, item, textVal) {
        if (textVal == this.textArea) {
            return;
        }
        this.UpdateValueField(this.textArea, idWork, item);
        // this.LoadData();
        // đóng matmenu Textarea
    }

    selectedCol(item) {
        const _item = new ColumnWorkModel();
        _item.id_project_team = this.ID_Project;
        _item.title = '';
        _item.columnname = item.fieldname;
        _item.isnewfield = true;
        _item.type = 3;
        const dialogRef = this.dialog.open(AddNewFieldsComponent, {
            width: '600px',
            data: _item,
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (result != undefined) {
                this.LoadData();
            }
        });
    }

    UpdateField(item) {
        const _item = new ColumnWorkModel();
        _item.id_row = item.id_row;
        _item.id_project_team = this.ID_Project;
        _item.title = item.title_newfield;
        _item.columnname = item.fieldname;
        _item.isnewfield = true;
        _item.type = 3;
        const dialogRef = this.dialog.open(AddNewFieldsComponent, {
            width: '600px',
            data: _item,
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (result != undefined) {
                this.LoadData();
            }
        });
    }

    Nguoitaocv(id) {
        if (this.listUser) {
            const x = this.listUser.find((x) => x.id_nv == id);
            if (x) {
                return x;
            }
        }
        return {};
    }

    themThanhvien() {
        const url = 'project/' + this.ID_Project + '/settings/members';
        this.router.navigateByUrl(url);
    }

    trackByFn(index, item) {
        return item.id_row;
    }

    update_hidden(item, isDelete = false) {
        let hidden = item.IsHidden ? 1 : 0;
        if (isDelete) {
        } else {
            hidden = item.IsHidden ? 0 : 1;
        }

        this._service.update_hidden(item.Id_row, 3, hidden, isDelete).subscribe((res) => {
            if (res && res.status == 1) {
                this.GetField();
            } else {
                this.layoutUtilsService.showError(res.error.message);
            }
        });
    }

    isUpdateStatusname(id = 1) {
        if (!(id > 0)) {
            return false;
        }
        if (
            this.filter_groupby.value == 'status' ||
            this.filter_groupby.value == 'groupwork'
        ) {
            return true;
        }
        return false;
    }

    getNhomCV(node) {
        if (node.id_group > 0) {
            return '- ' + node.work_group;
        }
        return '';
    }

    HasColunmHidden(list) {
        const x = list.filter((item) => item.IsHidden && item.isnewfield);
        if (x.length > 0) {
            return true;
        }
        return false;
    }

    getHeight() {
        const height = window.innerHeight - 125 - this.tokenStorage.getHeightHeader();
        return height;
    }

    SelectedField(item) {
        this.column_sort = item;
        this.UpdateInfoProject();
        this.LoadData();
    }

    Forme(val) {
        this.isAssignforme = val;
        this.UpdateInfoProject();
        this.LoadDataTaskNew();
    }

    getComponentName(id_row) {
        if (id_row) {
            return this.componentName + id_row;
        } else {
            return '';
        }
    }

    CheckClosedTask(item) {
        if (!this.CheckClosedProject()) {
            return false;
        }
        if (this.IsAdminGroup) {
            return true;
        }
        if (item.closed) {
            return false;
        } else {
            return true;
        }
    }

    KiemTraThayDoiCongViec(item, key, closeTask = false) {
        if (!this.CheckClosedTask(item) || closeTask) {
            this.layoutUtilsService.showError('Công việc đã đóng');
            return false;
        }
        if (this.IsAdmin()) {
            return true;
        } else if (item.createdby == this.UserID) {
            return true;
        } else {
            if (item.Users) {
                const index = item.Users.findIndex(x => x.userid == this.UserID);
                if (index >= 0) {
                    return true;
                }
            }
        }

        let txtError = '';
        switch (key) {
            case 'assign':
                txtError = 'Bạn không có quyền thay đổi người làm của công việc này.';
                break;
            case 'id_group':
                txtError = 'Bạn không có quyền thay đổi nhóm công việc của công việc này.';
                break;
            case 'status':
                txtError = 'Bạn không có quyền thay đổi trạng thái của công việc này.';
                break;
            case 'estimates':
                txtError = 'Bạn không có quyền thay đổi thời gian làm của công việc này.';
                break;
            case 'checklist':
                txtError = 'Bạn không có quyền chỉnh sửa checklist của công việc này.';
                break;
            case 'title':
                txtError = 'Bạn không có quyền đổi tên của công việc này.';
                break;
            case 'description':
                txtError = 'Bạn không có quyền đổi mô tả của công việc này.';
                break;
            default:
                txtError = 'Bạn không có quyền chỉnh sửa công việc này.';
                break;
        }
        this.layoutUtilsService.showError(txtError);
        return false;
    }

    // xử lý filter cho dự án
    LoadFilterProject() {
        const Info = JSON.parse(localStorage.getItem('closedTask-worklistnew'));
        if (Info && Info.length > 0) {
            const itemproject = Info.find(x => x.projectteam == this.ID_Project);
            if (itemproject) {
                this.LoadInfoProject(itemproject.data);
            } else {
                this.CreateInfoProject();
            }
        } else {
            localStorage.setItem('closedTask-worklistnew', JSON.stringify([]));
            this.CreateInfoProject();
        }
    }

    LoadInfoProject(item) {
        if (item.tasklocation) {
            this.tasklocation = item.tasklocation;
        }
        if (item.showclosedtask) {
            this.showclosedtask = item.showclosedtask;
        }
        if (item.showclosedsubtask) {
            this.showclosedsubtask = item.showclosedsubtask;
        }
        if (item.showemptystatus) {
            this.showemptystatus = item.showemptystatus;
        }
        if (item.column_sort) {
            this.column_sort = item.column_sort;
        }
        if (item.filter_groupby) {
            this.filter_groupby = item.filter_groupby;
        }
        if (item.filter_subtask) {
            this.filter_subtask = item.filter_subtask;
        }
        if (item.filterDay) {
            this.filterDay = item.filterDay;
        }
        if (item.isAssignforme) {
            this.isAssignforme = item.isAssignforme;
        }
    }

    CreateInfoProject() {
        const InfoNew = JSON.parse(localStorage.getItem('closedTask-worklistnew'));
        InfoNew.push({projectteam: this.ID_Project, data: this.InfoFilter()});
        localStorage.setItem('closedTask-worklistnew', JSON.stringify(InfoNew));
    }

    UpdateInfoProject() {
        const InfoNew = JSON.parse(localStorage.getItem('closedTask-worklistnew'));
        InfoNew.forEach(res => {
            if (res.projectteam == this.ID_Project) {
                res.data = this.InfoFilter();
            }
        });
        localStorage.setItem('closedTask-worklistnew', JSON.stringify(InfoNew));
    }

    InfoFilter() {
        return {
            tasklocation: this.tasklocation,
            showclosedtask: this.showclosedtask,
            showclosedsubtask: this.showclosedsubtask,
            showemptystatus: this.showemptystatus,
            column_sort: this.column_sort,
            filter_groupby: this.filter_groupby,
            filter_subtask: this.filter_subtask,
            filterDay: this.filterDay,
            isAssignforme: this.isAssignforme,
        };
    }
}

export interface DropInfo {
    targetId: string;
    action?: string;
}
