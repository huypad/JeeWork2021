import { WorkGroupEditComponent } from '../../../../work/work-group-edit/work-group-edit.component';
import {
    LayoutUtilsService,
    MessageType,
} from '../../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { SubheaderService } from '../../../../../../_metronic/partials/layout/subheader/_services/subheader.service';
import { TokenStorage } from '../../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { QueryParamsModelNew } from '../../../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { MenuPhanQuyenServices } from '../../../../../../_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service';
import { AttachmentService } from '../../../../services/attachment.service';
import {
    AttachmentModel,
    FileUploadModel,
} from '../../../Model/department-and-project.model';
import { AddNewFieldsComponent } from '../../add-new-fields/add-new-fields.component';
import { StatusDynamicDialogComponent } from '../../../../status-dynamic/status-dynamic-dialog/status-dynamic-dialog.component';
import { WorkService } from '../../../../work/work.service';
import { DuplicateTaskNewComponent } from '../../duplicate-task-new/duplicate-task-new.component';
import { WorkListNewDetailComponent } from '../../work-list-new-detail/work-list-new-detail.component';
import { DialogSelectdayComponent } from '../../../../report/dialog-selectday/dialog-selectday.component';
import {
    WorkModel,
    UpdateWorkModel,
    UserInfoModel,
    WorkDuplicateModel,
    WorkGroupModel,
} from '../../../../work/work.model';
import { ColumnWorkModel, DrapDropItem } from '../../drap-drop-item.model';
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
import { element } from 'protractor';
import { JeeWorkLiteService } from '../../../../services/wework.services';
import { DatePipe, DOCUMENT } from '@angular/common';
import { TranslateService } from '@ngx-translate/core';
import { FormBuilder, FormControl } from '@angular/forms';
import { Router, ActivatedRoute } from '@angular/router';
import { ProjectsTeamService } from '../../../Services/department-and-project.service';
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
    SimpleChange,
    SimpleChanges,
    Output,
    EventEmitter,
} from '@angular/core';
import { MatTable } from '@angular/material/table';
import { MatDialog } from '@angular/material/dialog';
import { MatSort } from '@angular/material/sort';
import { cloneDeep, find, values } from 'lodash';
import * as moment from 'moment';
import { SelectionModel } from '@angular/cdk/collections';
import { workAddFollowersComponent } from '../../../../work/work-add-followers/work-add-followers.component';
// import { WorkEditDialogComponent } from "../../../work/work-edit-dialog/work-edit-dialog.component";
import { WorkAssignedComponent } from '../../../../work/work-assigned/work-assigned.component';
import { DuplicateWorkComponent } from '../../../../work/work-duplicate/work-duplicate.component';
import { OverlayContainer } from '@angular/cdk/overlay';
import { BehaviorSubject, of, throwError } from 'rxjs';

@Component({
    selector: 'app-works-list-group-2',
    templateUrl: './works-list-group-2.component.html',
    styleUrls: ['./works-list-group-2.component.scss'],
})
export class WorksListGroup2Component implements OnInit, OnChanges {
    constructor(
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
        private WeWorkService: JeeWorkLiteService,
        private menuServices: MenuPhanQuyenServices,
        private overlayContainer: OverlayContainer,
        private _attservice: AttachmentService
    ) {
        this.taskinsert.clear();
        this.filter_subtask = this.listFilter_Subtask[0];
        this.list_priority = this.WeWorkService.list_priority;
        this.UserID = +localStorage.getItem('idUser');
    }
    @Input() ID_Project = 0;
    @Input() Id_Department = 0;
    @Input() filter: any = {};
    @Input() listField: any = [];
    @Input() listNewfield: any = [];
    @Input() groupby = '';
    @Input() project_data: any = null;
    @Input() showemptystatus = false;
    @Input() tasklocation = false;
    @Input() showsubtask = true;
    @Input() showclosedtask = true;
    @Input() showclosedsubtask = true;
    @Input() showtaskmutiple = true;
    @Input() type = 1;
    @Output() pageReload = new EventEmitter<any>();
    @Output() ColReload = new EventEmitter<any>();
    ListtopicObjectID$: BehaviorSubject<any> = new BehaviorSubject<any>([]);
    dataLoader$: BehaviorSubject<any> = new BehaviorSubject<any>([]);
    data: any = [];
    ListColumns: any = [];
    listFilter: any = [];
    ListTasks: any = [];
    ListTags: any = [];
    ListUsers: any = [];
    editmail = 0;
    isAssignforme = false;
    loadding = true;
    statusDefault = 0;
    // col
    displayedColumnsCol: string[] = [];
    @ViewChild(MatSort, { static: true }) sort: MatSort;
    previousIndex: number;
    ListAction: any = [];
    addNodeitem = 0;
    newtask = -1;
    options_assign: any = {};
    filter_subtask: any = [];
    list_milestone: any = [];
    Assign_me = -1;
    // view setting
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
    public column_sort: any = [];
    IsAdminGroup = false;
    listStatus: any = [];
    // list da nhi???m
    // nodes: any[] = demoData;
    // ids for connected drop lists
    dropTargetIds = [];
    nodeLookup = {};
    dropActionTodo: DropInfo = null;
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

    ngOnInit() {
        // this.selection = new SelectionModel<WorkModel>(true, []);
        this.menuServices.GetRoleWeWork('' + this.UserID)
            .pipe(
                map(res => {
                    if (res && res.status == 1) {
                        this.list_role = res.data.dataRole;
                        this.IsAdminGroup = res.data.IsAdminGroup;
                    }
                    if (!this.CheckRoles(3)) {
                        this.isAssignforme = true;
                        this.Emtytask = true;
                        // this.LoadListStatus();
                    } else {
                        this.isAssignforme = false;
                        // this.LoadListStatus();
                    }
                })
            )
            .subscribe(() => {
                this.mark_tag();
                this.LoadListAccount();
                this.LoadDetailProject();
                this.LoadDataTaskNew();
            });

        this.WeWorkService.lite_milestone(this.ID_Project).subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status == 1) {
                this.list_milestone = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    ngOnChanges(changes: SimpleChanges) {
        // if (changes.project_data) {
        //     this.dataLoader$.next('');
        //     this.dataLoader$.next(this.project_data);
        //     this.LoadBindingData();
        // }
        if (changes.ID_Project) {
            this.LoadData();
        }
        if (changes.listField) {
            this.LoadUpdateColNew(this.listField);
            this.changeDetectorRefs.detectChanges();
        }
        if (changes.filter) {
            this.LoadDataTaskNew();
        }

    }

    LoadDetailProject() {
        this._service.Detail(this.ID_Project).subscribe((res) => {
            if (res && res.status == 1) {
                this.ProjectTeam = res.data;
            }
        });
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
                    //   if (x.isuyquyen && x.isuyquyen != '0') { return true; }
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
                    const r = x.Roles.find((r) => r.id_role == roleID);
                    if (r) {
                        return true;
                    } else {
                        return false;
                    }
                }
            }
        } else if (this.IsAdminGroup) {
            return true;
        }
        return false;
    }

    CheckClosedProject() {
        const x = this.list_role.find((x) => x.id_row == this.ID_Project);
        if (x) {
            if (x.locked) {
                return false;
            }
        }
        return true;
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
                if (x.admin == true) {
                    return true;
                } else {
                    // if (key == 'id_nv') {
                    //   if (x.isuyquyen && x.isuyquyen != '0') { return true; }
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

    LoadData() {
        // this.clearList();

        // this.layoutUtilsService.showWaitingDiv();
        // this.GetOptions_NewField();
        this.WeWorkService.GetNewField().subscribe((res) => {
            if (res && res.status == 1) {
                this.listNewField = res.data;
            }
        });

        this.WeWorkService.ListStatusDynamic(this.ID_Project).subscribe((res) => {
            if (res && res.status == 1) {
                this.status_dynamic = res.data;
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
        });

        this.WeWorkService.lite_workgroup(this.ID_Project)
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

    UpdateValueField(value, idWork, field) {
        this.editmail = 0;
        if (field.fieldname != 'date') {
            if (value == '' || value == null || value == undefined) {
                if (field.fieldname != 'checkbox') {
                    return;
                }
            }
        } else {
            if (value) {
                value = moment(value).format('DD/MM/YYYY HH:mm');
            }
        }
        const _item = new UpdateWorkModel();
        _item.clear();
        _item.FieldID = field.Id_row;
        _item.Value = value != null ? value : '';
        _item.WorkID = idWork;
        _item.TypeID = field.TypeID;
        this._service.UpdateNewField(_item).subscribe((res) => {
            if (res && res.status == 1) {
                this.ReloadData();
                // this.LoadData();
            }
        });
    }

    LoadNhomCongViec(id) {
        const x = this.listType.find((x) => x.id_row == id);
        if (x) {
            return x.title;
        }
        return 'Ch??a ph??n lo???i';
    }

    UpdateValue() {
    }

    filterConfiguration(): any {
        const filter: any = this.filter;
        filter.id_project_team = this.ID_Project;
        filter.displayChild = 1;
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

    // listField: any = [];
    GetField() {
        this.WeWorkService.GetListField(this.ID_Project, 3, false).subscribe(
            (res) => {
                if (res && res.status == 1) {
                    this.listField = res.data;
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

    LoadDataTaskNew(loading = true) {
        if (loading) {
            // this.layoutUtilsService.showWaitingDiv();
        }
        const queryParams = new QueryParamsModelNew(
            this.filterConfiguration(),
            '',
            '',
            0,
            50,
            true
        );
        this.LoadNewField();
        this._service.ListTask(queryParams)
            .subscribe((res) => {
                if (res && res.status === 1) {
                    this.listStatus = res.data;
                    var itemPush = [];
                    this.listStatus.forEach(element => {
                        itemPush = itemPush.concat(element.datawork)
                    });
                    this.ListTasks = itemPush;
                    this.prepareDragDrop(this.ListTasks);
                    // this.LoadListStatus();
                    this.changeDetectorRefs.detectChanges();
                }
                this.layoutUtilsService.OffWaitingDiv();

            }, (err) => (this.layoutUtilsService.OffWaitingDiv()));

    }
    LoadNewField() {
        this.WeWorkService.GetValuesNewFields(this.ID_Project).subscribe(res => {
            if (res && res.status === 1) {
                this.DataNewField = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
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

    CheckDataStatus(valuefilter, elementTask) {
        if (this.groupby == 'status' && valuefilter.id_row == +elementTask.status) {
            if (this.isAssignforme) {
                // ki???m tra c?? ph???i ng?????i ???????c giao hay ng?????i t???o hay kh??ng
                if (
                    this.isAssignForme(elementTask) ||
                    elementTask.createdby == this.UserID
                ) {
                    return true;
                }
            } else {
                return true;
            }
        }
        return false;
    } fv

    isAssignForme(elementTask) {
        if (
            elementTask.createdby == this.UserID ||
            this.FindUser(elementTask.User, this.UserID) ||
            this.FindUser(elementTask.Follower, this.UserID) ||
            this.FindUser(elementTask.UserSubtask, this.UserID)
        ) {
            return true;
        }
        return false;
    }

    CheckDataAssigne(valuefilter, elementTask) {
        if (this.groupby == 'assignee') {
            if (this.isAssignforme) {
                if (
                    (this.FindUser(elementTask.User, valuefilter.id_row) &&
                        this.isAssignForme(elementTask)) ||
                    (elementTask.createdby == this.UserID &&
                        this.UserNull(elementTask.User))
                ) {
                    return true;
                }
            } else {
                if (
                    this.FindUser(elementTask.User, valuefilter.id_row) ||
                    (this.UserNull(elementTask.User) && valuefilter.id_row == '') ||
                    (elementTask.User?.length > 1 && valuefilter.id_row == '0')
                ) {
                    return true;
                }
            }
        }
        return false;
    }

    FindUser(listUser, iduser) {
        if (listUser) {
            const x = listUser.find((x) => x.id_user == iduser);
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
            this.groupby == 'groupwork' &&
            elementTask.id_group == valuefilter.id_row
        ) {
            if (this.isAssignforme) {
                // ki???m tra c?? ph???i ng?????i ???????c giao hay ng?????i t???o hay kh??ng
                if (
                    this.isAssignForme(elementTask)

                ) {
                    return true;
                }
            } else {
                return true;
            }
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
        if (nodes) {
            nodes.forEach((node) => {
                this.dropTargetIds.push(node.id_row);
                this.nodeLookup[node.id_row] = node;
                if (node.children == null) {
                    node.children = [];
                }
                this.prepareDragDrop(node.children);
            });
        }
    }

    DragDropItemWork(item) {
        const dropItem = new DrapDropItem();
        this._service.DragDropItemWork(item).subscribe((res) => {
        });
    }

    UpdateCol(fieldname) {
        const item = new ColumnWorkModel();
        item.id_department = +this.Id_Department;
        item.columnname = fieldname;
        this._service.UpdateColumnWork(item).subscribe((res) => {
            if (res && res.status == 1) {
                this.ReloadColData();
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    LoadUpdateColNew(dataCol) {
        this.ListColumns = dataCol;
        // x??a title kh???i c???t
        const colDelete = [
            'title',
            'id_row',
            'id_project_team',
            'id_parent',
        ];
        colDelete.forEach((element) => {
            const indextt = this.ListColumns.findIndex(
                (x) => x.fieldname == element
            );
            if (indextt >= 0) {
                this.ListColumns.splice(indextt, 1);
            }
        });

        this.ListColumns.sort((a, b) =>
            a.id_department > b.id_department
                ? -1
                : b.id_department > a.id_department
                    ? 1
                    : 0
        );
        this.changeDetectorRefs.detectChanges();
    }

    ShowCol(item, IdDepartment) {
        if (!item.IsHidden) {
            if (item.id_department == IdDepartment) {
                return true;
            }
        }
        return false;
    }
    ClosedTask(value, node) {
        this._service.ClosedTask(node.id_row, value).subscribe((res) => {
            this.ReloadData();
            if (res && res.status == 1) {
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }
    getDropdownField(idField) {
        const list = this.listNewfield.filter((x) => x.FieldID == idField);
        if (list) {
            return list;
        }
        return [];
    }
    focusInput(idtask, idfield) {
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
                const strBase64 = base64Str.substr(metaIdx + 8); // C???t meta data kh???i chu???i base64
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
        // if (this.CheckRoles(15)) {
        //     this.isAssignforme = true;
        // }
        const itemDrop = new DrapDropItem();
        const draggedItemId = event.item.data; // get data -- id
        const parentItemId = event.previousContainer.id; // t??? th???ng cha hi???n t???i
        const status = 0;
        const listdata = this.ListTasks;
        if (!this.dropActionTodo) { //|| this.dropActionTodo.targetId == draggedItemId 
            return;
        }
        // load list new data
        const draggedItem = this.nodeLookup[draggedItemId]; // l???y item t??? node

        let listDatanew = this.ListTasks;
        // c??ch n??y ch??? d??ng g???i data trong 1 b???ng
        const stt = draggedItem.status;
        const newArr = this.listStatus.find((x) => x.id_row == stt);
        if (newArr) {
            listDatanew = newArr.datawork;
        }
        // get list data t??? list b??? ??i
        // if(parentItemId=='main'){
        //   //get list b??? ??i
        //   //draggedItem.id_parent = '';
        //   // draggedItem.status = id_row?

        // }

        const targetListId = this.getParentNodeId(
            this.dropActionTodo.targetId,
            listDatanew,
            'main'
        ); // th???ng cha m???i n???u ngo??i c??ng th?? = main

        // get list data n??i mu???n ?????n
        // if(targetListId=='main' && parentItemId!='main' ){
        //   //get list mu???n ?????n
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
        // Set c??ng vi???c con 2 c???p
        // set parent cho node
        if (this.dropActionTodo.action == 'inside') {
            // n???u inside th?? nh???n node inside l??m cha
            const nodeParent = this.nodeLookup[this.dropActionTodo.targetId]; // l???y item t??? node
            if (nodeParent.id_parent > 0) {
                return;
            } else {
                if (draggedItem.children?.length > 0) {
                    // n???u c?? node con th?? out kh??ng cho ph??p gh??p c??c node
                    return;
                }
                draggedItem.id_parent = this.dropActionTodo.targetId;
            }
        } else {
            // kh??c th?? l???y th???ng cha c???a node m???i
            if (targetListId == 'main') {
                // node ngo??i c??ng
                draggedItem.id_parent = '';
            } else {
                // node con
                if (draggedItem.children?.length > 0) {
                    // n???u c?? node con th?? out kh??ng cho ph??p gh??p c??c node
                    return;
                }
                draggedItem.id_parent = targetListId;
            }
        }
        // list data phai la list t???t c??? th???ng cha trong b???ng
        const oldItemContainer =
            parentItemId != 'main'
                ? this.nodeLookup[parentItemId].children
                : listDatanew; // l???y con t??? th???ng cha n??? th???ng cha main th?? l???y nguy??n c??y
        const newContainer =
            targetListId != 'main'
                ? this.nodeLookup[targetListId].children
                : listDatanew; // l???y list mu???n ????a t???i chu???n b??? map v??o list n??y

        const i = oldItemContainer.findIndex((c) => c.id_row == draggedItemId); // l???y index t??? list c??
        oldItemContainer.splice(i, 1); // c???t item t??? list b??? ??i
        // set parent
        switch (this.dropActionTodo.action) {
            case 'before':
            case 'after':
                const targetIndex = newContainer.findIndex(
                    (c) => c.id_row == this.dropActionTodo.targetId
                ); // t??m id th???ng ???????c map - so s??nh v???i th???ng ???????c l??m m???c
                if (this.dropActionTodo.action == 'before') {
                    newContainer.splice(targetIndex, 0, draggedItem); // n???u tr?????c n?? th?? index c???a th???ng m???i k??o thay v??o index th???ng l??m m???c
                } else {
                    newContainer.splice(targetIndex + 1, 0, draggedItem); // n???u ?????ng sau th?? index th???ng l??m m???c + 1
                }
                break;

            case 'inside': // ????a v??o trong l??m con c???a th???ng node ???????c ch???n
                this.nodeLookup[this.dropActionTodo.targetId].children.push(
                    draggedItem
                ); // get ID node ???????c ch???n push item m???i v??o ????
                this.nodeLookup[this.dropActionTodo.targetId].isExpanded = true; // tr???ng th??i ??ang m??? node
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
        this.clearDragInfo(true); // x??a th???ng this.dropActionTodo  set n?? v??? null
    }

    getParentNodeId(id: string, nodesToSearch: any[], parentId: string): string {
        const findNode = nodesToSearch.find((x) => x.id_row == id);
        if (findNode) {
            return parentId;
        } else {
            for (const node of nodesToSearch) {
                // if (node.id_row == id) return parentId;
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
                this.ReloadData();
                // this.LoadData();
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
        //   title: "C???t" + this.cot,
        //   type: null
        // })
        // this.cot++;
    }

    // Assign
    ItemSelected(val: any, task, remove = false) {
        // ch???n item
        if (remove) {
            val.id_nv = val.id_user;
        }
        this.UpdateByKey(task, 'assign', val.id_nv, false);
    }

    LoadListAccount() {
        const filter: any = {};
        // filter.key = 'id_project_team';
        // filter.value = this.ID_Project;
        filter.id_project_team = this.ID_Project;
        this.WeWorkService.list_account(filter).subscribe((res) => {
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
            { outlets: { auxName: 'aux/detail/' + item.id_row } },
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

    ExpandNode(node) {
        if (this.filter_subtask.value == 'show') {
            return;
        } else {
            node.isExpanded = !node.isExpanded;
        }
    }

    ShowCloseTask() {
        this.showclosedtask = !this.showclosedtask;
    }

    LoadClosedTask(val) {
        if (val == this.ItemFinal) {
            return this.showclosedtask;
        }
        return true;
    }

    loadSubtask() {
        const isExpanded = this.filter_subtask.value == 'show' ? true : false;
        for (const i of this.listStatus) {
            i.data.forEach((element) => {
                element.isExpanded = isExpanded;
            });
        }
    }

    Subtask(item) {
        if (item.value == this.filter_subtask.value) {
            return;
        }
        this.filter_subtask = item;
        this.loadSubtask();
    }

    CreateTask(val) {
        const x = this.newtask;
        this.CloseAddnewTask(true);
        setTimeout(() => {
            this.newtask = x;
        }, 1000);
        this._service.InsertTask(val).subscribe((res) => {
            if (res && res.status == 1) {
                this.ReloadData();
                // this.LoadData();
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    DeleteTask(task) {
        this._service.DeleteTask(task.id_row).subscribe((res) => {
            if (res && res.status == 1) {
                this.ReloadData();
                // this.LoadData();
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
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
                if (x.admin == true) {
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

        this.UpdateByKey(task, 'status', status.id_row);
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
            this.ReloadData();
            if (res && res.status == 1) {
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    GetColorName(val) {
        // name
        this.WeWorkService.getColorName(val).subscribe((res) => {
            this.colorName = res.data.Color;
            return this.colorName;
        });
    }

    getTenAssign(val) {
        const list = val.split(' ');
        return list[list.length - 1];
    }

    Updateestimates(task, event) {
        if (!this.KiemTraThayDoiCongViec(task, 'estimates')) {
            return;
        }
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
        let valuedate;
        if (date) {
            valuedate = moment(date).format('MM/DD/YYYY HH:mm');
            // this.UpdateByKey(task, field, moment(date).format("MM/DD/YYYY HH:mm"));
        } else {
            valuedate = null;
            // this.UpdateByKey(task, field, null);
        }
        this.UpdateByKey(task, field, valuedate, false);
    }

    updatePriority(task, field, value) {
        this.UpdateByKey(task, field, value);
    }

    UpdateTask(task) {
        this._service.UpdateTask(task.id_row).subscribe((res) => {
            this.ReloadData();
            // if (res && res.status == 1) {
            //     this.ReloadData(true);
            //     // this.LoadData();
            // }
        });
    }

    DeleteByKey(task, field) {
        if (!this.KiemTraThayDoiCongViec(task, field)) {
            return;
        }
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
            data: { _item, type },
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
            } else {
                this.ReloadData(true);
                // this.LoadData();
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
            data: { item },
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

    // nh??m c??ng vi???c
    Assignmore() {
        const item = this.getOptions_Assign();
        const dialogRef = this.dialog.open(WorkAssignedComponent, {
            width: '500px',
            height: '500px',
            data: { item, ID_Project: this.ID_Project },
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (res) {
                this.selection.selected.forEach((element) => {
                    this.UpdateByKey(element, 'assign', res.id_nv);
                });
            }
        });
    }

    // nh??m status
    UpdateStatuslist(status) {
        if (this.IsAdmin()) {
        } else {
            if (status.Follower) {
                if (status.Follower != this.UserID) {
                    this.layoutUtilsService.showError(
                        'Kh??ng c?? quy???n thay ?????i tr???ng th??i'
                    );
                }
            }
        }

        this.selection.selected.forEach((element) => {
            this.UpdateByKey(element, 'status', status.id_row);
            // this.UpdateByKey(task, 'status', status.id_row);
        });
    }

    // nh??m start date
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

    // nh??m ????? ??u ti??n
    updatePrioritylist(value) {
        this.selection.selected.forEach((element) => {
            this.UpdateByKey(element, 'clickup_prioritize', value);
        });
    }

    // nh??m x??a cv
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
                data: { getOnlyIDproject: true },
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
            this.ReloadData(true);
            // this.LoadData();
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
                    'Nh??n b???n th??nh c??ng',
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
        this.WeWorkService.lite_tag(this.ID_Project).subscribe((res) => {
            if (res && res.status == 1) {
                this.list_Tag = res.data;
                // this.changeDetectorRefs.detectChanges();
            }
        });
    }

    ReloadData(event = false) {
        if (event) {
            this.pageReload.emit(true);
        } else {
            this.LoadDataTaskNew();
        }
    }

    ReloadColData(event = true) {
        this.ColReload.emit(true);
    }

    RemoveTag(tag, item) {
        const model = new UpdateWorkModel();
        model.id_row = item.id_row;
        model.key = 'Tags';
        model.value = tag.id_row;
        this.WorkService.UpdateByKey(model).subscribe((res) => {
            if (res && res.status == 1) {
                this.ReloadData();
                // this.LoadData();
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
        if (this.groupby == 'status') {
            this.chinhsuastt(item);
        }
        if (this.groupby == 'groupwork') {
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
                this.ReloadData();
                // this.LoadData();
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
            data: { _item },
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
                this.ReloadData();
                // this.LoadData();
            }
        });
    }

    getNhom() {
        return this.groupby;
    }

    getDeadline(fieldname, date) {
        if (fieldname == 'deadline') {
            if (new Date(date) < new Date()) {
                // return 'text-danger'
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
        // this.ReloadData(true);
        // this.LoadData();
        // ????ng matmenu Textarea
    }

    selectedCol(item) {
        const _item = new ColumnWorkModel();
        _item.id_department = this.Id_Department;
        _item.title = '';
        _item.columnname = item.fieldname;
        _item.isnewfield = true;
        _item.type = this.type;
        const dialogRef = this.dialog.open(AddNewFieldsComponent, {
            width: '600px',
            data: _item,
        });

        dialogRef.afterClosed().subscribe((result) => {
            if (result != undefined) {
                this.ReloadColData(true);
                // this.LoadData();
            }
        });
    }

    UpdateField(item) {
        const _item = new ColumnWorkModel();
        _item.id_row = item.Id_row;
        _item.id_department = this.Id_Department;
        _item.title = item.Title_NewField;
        _item.columnname = item.fieldname;
        _item.isnewfield = true;
        _item.type = this.type;
        const dialogRef = this.dialog.open(AddNewFieldsComponent, {
            width: '600px',
            data: _item,
        });
        dialogRef.afterClosed().subscribe((result) => {
            if (result != undefined) {
                this.ReloadColData(true);
                // this.LoadData();
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
        this._service.update_hidden(item.Id_row, this.type, hidden, isDelete).subscribe((res) => {
            if (res && res.status == 1) {
                this.ReloadColData();
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    isUpdateStatusname(id = 1) {
        if (!(id > 0)) {
            return false;
        }
        if (this.groupby == 'status' || this.groupby == 'groupwork') {
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
        // if (this.IsAdminGroup) {
        //     return true;
        // }
        if (item.closed) {
            return false;
        } else {
            return true;
        }
    }

    KiemTraThayDoiCongViec(item, key) {
        if (!this.CheckClosedTask(item)) {
            this.layoutUtilsService.showError('C??ng vi???c ???? ????ng');
            return false;
        }
        if (this.IsAdmin()) {
            return true;
        } else if (item.createdby == this.UserID) {
            return true;
        } else {
            if (item.User) {
                const index = item.User.findIndex(x => x.id_user == this.UserID);
                if (index >= 0) {
                    return true;
                }
            }
        }
        ;
        var txtError = '';
        switch (key) {
            case 'assign':
                txtError = 'B???n kh??ng c?? quy???n thay ?????i ng?????i l??m c???a c??ng vi???c n??y.';
                break;
            case 'id_group':
                txtError = 'B???n kh??ng c?? quy???n thay ?????i nh??m c??ng vi???c c???a c??ng vi???c n??y.';
                break;
            case 'status':
                txtError = 'B???n kh??ng c?? quy???n thay ?????i tr???ng th??i c???a c??ng vi???c n??y.';
                break;
            case 'estimates':
                txtError = 'B???n kh??ng c?? quy???n thay ?????i th???i gian l??m c???a c??ng vi???c n??y.';
                break;
            case 'checklist':
                txtError = 'B???n kh??ng c?? quy???n ch???nh s???a checklist c???a c??ng vi???c n??y.';
                break;
            case 'title':
                txtError = 'B???n kh??ng c?? quy???n ?????i t??n c???a c??ng vi???c n??y.';
                break;
            case 'description':
                txtError = 'B???n kh??ng c?? quy???n ?????i m?? t??? c???a c??ng vi???c n??y.';
                break;
            default:
                txtError = 'B???n kh??ng c?? quy???n ch???nh s???a c??ng vi???c n??y.';
                break;
        }
        this.layoutUtilsService.showError(txtError);
        return false;
    }

    CheckCustomfield(node, item) {
        return this.editmail != (node.id_row + item.Id_row.toString());
    }
    TestUpdateKey(node) {
        this.UpdateByKey(node, 'title', node.title + ' +1');
    }
}

export interface DropInfo {
    targetId: string;
    action?: string;
}
