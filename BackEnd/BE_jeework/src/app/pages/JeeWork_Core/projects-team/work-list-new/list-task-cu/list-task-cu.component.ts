import { delay, filter, take } from 'rxjs/operators';
import { SubheaderService } from './../../../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import { TokenStorage } from './../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { MenuPhanQuyenServices } from './../../../../../_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service';
import { MessageType, LayoutUtilsService } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { QueryParamsModelNew } from './../../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { filterEditComponent } from './../../../filter/filter-edit/filter-edit.component';
import { workAddFollowersComponent } from './../../../work/work-add-followers/work-add-followers.component';
import { WorkAssignedComponent } from './../../../work/work-assigned/work-assigned.component';
import { DuplicateWorkComponent } from './../../../work/work-duplicate/work-duplicate.component';
import { DuplicateTaskNewComponent } from './../duplicate-task-new/duplicate-task-new.component';
import { WorkListNewDetailComponent } from './../work-list-new-detail/work-list-new-detail.component';
import { DialogSelectdayComponent } from './../../../report/dialog-selectday/dialog-selectday.component';
import { DropInfo } from './../work-list-new.component';
import { WorkModel, UserInfoModel, UpdateWorkModel, WorkDuplicateModel, FilterModel } from './../../../work/work.model';
import { SelectionModel } from '@angular/cdk/collections';
import { WeWorkService } from './../../../services/wework.services';
import { TranslateService } from '@ngx-translate/core';
import { FormBuilder } from '@angular/forms';
import { MatDialog } from '@angular/material/dialog';
import { MatTable } from '@angular/material/table';
import { MatSort } from '@angular/material/sort';
import { Router, ActivatedRoute } from '@angular/router';
import { WorkService } from './../../../work/work.service';
import { ProjectsTeamService } from './../../Services/department-and-project.service';
import { DOCUMENT, DatePipe } from '@angular/common';
import { DrapDropItem, ColumnWorkModel } from './../drap-drop-item.model';
import { CdkDragDrop, moveItemInArray, CdkDropList, CdkDragStart } from '@angular/cdk/drag-drop';
import { Component, OnInit, Input, Inject, ChangeDetectorRef, ViewChild, OnChanges, HostListener } from '@angular/core';
import * as moment from 'moment';
import { BehaviorSubject, of, ReplaySubject, SubscriptionLike } from 'rxjs';
import { CommunicateService } from '../work-list-new-service/communicate.service';

@Component({
    selector: 'kt-list-task-cu',
    templateUrl: './list-task-cu.component.html',
    styleUrls: ['./list-task-cu.component.scss']
})
export class ListTaskCUComponent implements OnInit, OnChanges {
    constructor(
        @Inject(DOCUMENT) private document: Document, // multi level
        private _service: ProjectsTeamService,
        private WorkService: WorkService,
        private router: Router,
        public dialog: MatDialog,
        private route: ActivatedRoute,
        private itemFB: FormBuilder,
        private CommunicateService: CommunicateService,
        public subheaderService: SubheaderService,
        private layoutUtilsService: LayoutUtilsService,
        private changeDetectorRefs: ChangeDetectorRef,
        private translate: TranslateService,
        public datepipe: DatePipe,
        private tokenStorage: TokenStorage,
        private WeWorkService: WeWorkService,
        private menuServices: MenuPhanQuyenServices
    ) {
        this.taskinsert.clear();
        // this.filter_groupby = this.getMystaff?this.listFilter_Groupby[1]:this.listFilter_Groupby[0];
        this.filter_subtask = this.listFilter_Subtask[1];
        this.list_priority = this.WeWorkService.list_priority;
        this.UserID = +localStorage.getItem('idUser');
    }
    @Input() ID_Project = 1;
    @Input() ID_NV = 0;
    @Input() selectedTab = 0;
    @Input() idFilter = 0;
    @Input() myWorks = false;
    @Input() getMystaff = false;
    @Input() detailWork = 0;
    subscription: SubscriptionLike;
    dataSource;
    dataSource2;
    data: any = [];
    ListColumns: any = [];
    listFilter: any = [];
    ListTasks: any = [];
    ListTags: any = [];
    // col
    displayedColumnsCol: string[] = [];
    @ViewChild(MatSort, { static: true }) sort: MatSort;
    previousIndex: number;
    ListAction: any = [];
    addNodeitem = 0;
    newtask = -1;
    options_assign: any = {};
    filter_groupby: any = [];
    filter_subtask: any = [];
    danhsachboloc: any = [];
    Assign_me = -1;
    keyword = '';
    // view setting
    tasklocation = true;
    showsubtask = false;
    showclosedtask = false;
    showclosedsubtask = false;
    showtaskmutiple = true;
    showemptystatus = false;
    // viewTaskOrder = false;
    isAssignforme = false;
    filterwork = 1;
    status_dynamic: any = [];
    ListAllStatusDynamic: any = [];
    list_priority: any[];
    UserID = 0;
    isEdittitle = -1;
    startDatelist: Date = new Date();
    selection = new SelectionModel<WorkModel>(true, []);
    list_role: any = [];
    ItemFinal = 0;
    ProjectTeam: any = {};
    private readonly componentName: string = 'kt-task_';
    IsAdminGroup = false;
    public filteredDanhSachCongViec: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    filterDay = {
        startDate: new Date('09/01/2020'),
        endDate: new Date('09/30/2020'),
    };
    loadingSubject = new BehaviorSubject<boolean>(false);
    public column_sort: any = [];
    DanhSachCongViec: any = [];
    reloadData = false;
    listField: any = [];
    listStatus: any = [];
    // dropTargetIds = [];
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
            title: 'Project',
            value: 'project'
        },
        {
            title: 'Assignee',
            value: 'member'
        }
    ];
    listFilter_Subtask = [
        {
            title: 'Không xem công việc con',
            value: '0',
        },
        {
            title: 'Xem tất cả công việc (bao gồm công việc con)',
            value: '1',
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
        // giao tiếp service
        this.subscription = this.CommunicateService.currentMessage.subscribe(message => {
            if (message) {
                // this.LoadWork();
            }
        });
        // end giao tiếp service
        // get filter groupby
        this.filter_groupby = this.getMystaff ? this.listFilter_Groupby[1] : this.listFilter_Groupby[0];
        const today = new Date();
        this.filterDay = {
            endDate: new Date(today.setMonth(today.getMonth())),
            startDate: new Date(today.getFullYear(), today.getMonth() - 3, 1),
        };
        this.column_sort = this.sortField[0];
        this.route.params.subscribe(res => {
            if (this.selectedTab === 2) {
                if (res && res.id) {
                    this.idFilter = res.id;
                }
            }
            this.DanhSachCongViec = [];
            // this.LoadWork();
        });
        this.selection = new SelectionModel<WorkModel>(true, []);
        this.menuServices.GetRoleWeWork('' + this.UserID).subscribe(res => {
            if (res && res.status === 1) {
                this.list_role = res.data.dataRole;
                this.IsAdminGroup = res.data.IsAdminGroup;
            }
        });
        this.LoadFilterProject();
        this.BindDataLite();
        this.LoadTask();
        this.changeDetectorRefs.detectChanges();
    }

    ngOnChanges() {
        if (this.detailWork > 0) {
            this._service.WorkDetail(this.detailWork).subscribe(res => {
                if (res && res.status === 1) {
                    const item = res.data;
                    const dialogRef = this.dialog.open(WorkListNewDetailComponent, {
                        width: '90vw',
                        height: '90vh',
                        data: item,
                        disableClose: true
                    });
                    dialogRef.afterClosed().subscribe(() => {
                        this.detailWork = 0;
                        this.LoadListStatusByProject();
                    });
                }
            });
        }
    }
    ngOnDestroy(): void {
        // Called once, before the instance is destroyed.
        // Add 'implements OnDestroy' to the class.
        if (this.subscription) {
            this.subscription.unsubscribe();
        }
    }
    LoadTask() {
        this.clearList();
        const queryParams = new QueryParamsModelNew(
            this.filterConfiguration(),
            '',
            '',
            0,
            50,
            true
        );
        debugger
        var api_service = this._service.ListByFilter(queryParams);
        if (this.selectedTab === 2 && !this.getMystaff) {
            api_service = this._service.ListByFilter(queryParams);
        }
        else {
            if (!this.getMystaff) {
                api_service = this._service.ListByUserCU(queryParams);
            }
            else
                api_service = this._service.ListByManageCU(queryParams);
        }
        this.loadingSubject.next(true);
        this.DanhSachCongViec = [];
        this.layoutUtilsService.showWaitingDiv();
        api_service.subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status === 1) {
                this.DanhSachCongViec = res.data;
                this.filterDanhSach();
                this.changeDetectorRefs.detectChanges();
            }
        });
    }
    BindDataLite() {
        this.LoadListStatusByProject();
        this.GetField();
        this.mark_tag();
        this.LoadFilter();
        this.LoadListAccount();
        this.LoadDetailProject();
        this.LoadListAllStatusDynamic();
    }
    LoadListAllStatusDynamic() {
        this.WeWorkService.ListAllStatusDynamic().subscribe(res => {
            if (res && res.status === 1) {
                this.ListAllStatusDynamic = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
    }
    LoadListStatusByProject() {
        this.data = [];
        this.WeWorkService.ListStatusDynamic(this.ID_Project).subscribe(res => {
            if (res && res.status === 1) {
                this.status_dynamic = res.data;
                // load ItemFinal
                const x = this.status_dynamic.find(val => val.IsFinal === true);
                if (x) {
                    this.ItemFinal = x.id_row;
                } else {
                }
                this.changeDetectorRefs.detectChanges();
            }
        });
    }
    LoadDetailProject() {
        this._service.Detail(this.ID_Project).subscribe(res => {
            if (res && res.status === 1) {
                this.ProjectTeam = res.data;
            }
        });
    }
    LoadFilter() {
        this.WorkService.Filter().subscribe(res => {
            if (res && res.status === 1) {
                this.danhsachboloc = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
    }
    getListStatus(team) {
        if (this.ListAllStatusDynamic) {
            const item = this.ListAllStatusDynamic.find(x => +x.id_row === team);
            if (item) {
                return item.status;
            }
        }
        return [];
    }
    LoadListAccount() {
        const filter: any = {};
        this.WeWorkService.list_account(filter).subscribe(res => {
            if (res && res.status === 1) {
                this.listUser = res.data;
                this.changeDetectorRefs.detectChanges();
            }
            this.options_assign = this.getOptions_Assign();
        });
    }
    Forme(val) {
        this.isAssignforme = val;
        this.UpdateInfoProject();
        // this.LoadDataTaskNew();
    }
    GetField() {
        this.WeWorkService.GetListField(0, 3, false).subscribe(res => {
            if (res && res.status === 1) {
                this.listField = res.data;
                const colDelete = ['title', 'id_row', 'id_parent'];
                colDelete.forEach(element => {
                    const indextt = this.listField.findIndex(x => x.fieldname === element);
                    if (indextt >= 0) {
                        this.listField.splice(indextt, 1);
                    }
                });
                this.changeDetectorRefs.detectChanges();
            } else {
                this.layoutUtilsService.showInfo(res.error.message);
            }
        });
    }
    unique(arr) {
        const formArr = arr.sort();
        const newArr = [formArr[0]];
        for (let i = 1; i < formArr.length; i++) {
            if (formArr[i] !== formArr[i - 1]) {
                newArr.push(formArr[i]);
            }
        }
        return newArr;
    }

    clearList() {
        this.selection = new SelectionModel<WorkModel>(true, []);
    }

    CheckRoles(roleID: number, id_project_team) {
        const x = this.list_role.find(x => x.id_row === id_project_team);
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
                if (x.admin === true || +x.admin === 1 || +x.owner === 1 || +x.parentowner === 1) {
                    return true;
                } else {
                    // if (roleID === 3 || roleID === 4) {
                    //   if (x.isuyquyen) {
                    //     return true;
                    //   }
                    // }
                    if (roleID === 7 || roleID === 9 || roleID === 11 || roleID === 12 || roleID === 13) {
                        if (x.Roles.find((r) => r.id_role === 15)) {
                            return false;
                        }
                    }
                    if (roleID === 10) {
                        if (x.Roles.find((r) => r.id_role === 16)) {
                            return false;
                        }
                    }
                    if (roleID === 4 || roleID === 14) {
                        if (x.Roles.find((r) => r.id_role === 17)) {
                            return false;
                        }
                    }
                    const r = x.Roles.find(r => r.id_role === roleID);
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

    CheckRoleskeypermit(key, id_project_team) {
        const x = this.list_role.find(x => x.id_row === id_project_team);
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
                if (x.admin === true || +x.admin === 1 || +x.owner === 1 || +x.parentowner === 1) {
                    return true;
                } else {
                    // if (key === 'id_nv') {
                    //   if (x.isuyquyen) {
                    //     return true;
                    //   }
                    // }
                    if (key === 'title' || key === 'description' || key === 'status' || key === 'checklist' || key === 'delete') {
                        if (x.Roles.find((r) => r.id_role === 15)) {
                            return false;
                        }
                    }
                    if (key === 'deadline') {
                        if (x.Roles.find((r) => r.id_role === 16)) {
                            return false;
                        }
                    }
                    if (key === 'id_nv' || key === 'assign') {
                        if (x.Roles.find((r) => r.id_role === 17)) {
                            return false;
                        }
                    }
                    const r = x.Roles.find(role => role.keypermit === key);
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

    /** SELECTION */
    CheckedNode(check: any, arr_model: any) {
        const checked = this.selection.selected.find(x => x.id_row === arr_model.id_row);
        const index = this.selection.selected.indexOf(arr_model, 0);
        if (!checked && check.checked) {
            this.selection.selected.push(arr_model);
        } else {
            this.selection.selected.splice(index, 1);
        }
    }
    SelectedField(item) {
        this.column_sort = item;
        // this.LoadWork();
        this.LoadTask();
    }
    filterConfiguration2(): any {
        const filter: any = {};

        return filter;
    }
    filterConfiguration(): any {
        const filter: any = {};
        debugger
        filter.groupby = this.filter_groupby.value;
        filter.keyword = this.keyword;
        filter.id_nv = this.ID_NV;
        filter.displayChild = this.filter_subtask.value;
        filter.subtask_done = this.showclosedsubtask ? 1 : 0;
        filter.task_done = this.showclosedtask ? 1 : 0;
        if (this.filterwork > 0) {
            filter.filter = this.filterwork;
        }
        // công việc tôi theo dõi = 3
        if (this.selectedTab === 3) {
            filter.filter = 3;
        }
        filter.TuNgay = (this.f_convertDate(this.filterDay.startDate)).toString();
        filter.DenNgay = (this.f_convertDate(this.filterDay.endDate)).toString();
        filter.collect_by = this.column_sort.value;

        if (this.idFilter > 0) {
            filter.id_filter = this.idFilter;
        }
        // switch (this.selectedTab) {
        //     case 1:
        //     case 3:
        //         {
        //             // công việc assign cho tôi = 1
        //             if (this.filterwork > 0) {
        //                 filter.filter = this.filterwork;
        //             }
        //             // công việc tôi theo dõi = 3
        //             if (this.selectedTab === 3) {
        //                 filter.filter = 3;
        //             }
        //             filter.TuNgay = (this.f_convertDate(this.filterDay.startDate)).toString();
        //             filter.DenNgay = (this.f_convertDate(this.filterDay.endDate)).toString();
        //             filter.collect_by = this.column_sort.value;

        //             if (this.idFilter > 0) {
        //                 filter.id_filter = this.idFilter;
        //             }
        //             break;
        //         }
        //     case 2: {

        //         if (this.idFilter > 0) {
        //             filter.id_filter = this.idFilter;
        //         }
        //         break;
        //     }
        // }
        return filter;
    }

    protected filterDanhSach() {
        // filter the 
        this.filteredDanhSachCongViec.next(this.DanhSachCongViec);
    }
    ChangeData() {
        // this.LoadWork();
        this.LoadTask();
    }
    getColorStatus(id_project_team, val) {
        const item = this.ListAllStatusDynamic.find(x => +x.id_row === id_project_team);
        let index;
        if (item) {
            index = item.status.find(x => x.id_row === val);
        }
        if (index) {
            return index.color;
        } else {
            return 'gray';
        }
    }
    getTasklocation(id_project_team) {
        if (this.ListAllStatusDynamic) {
            const item = this.ListAllStatusDynamic.find(x => +x.id_row === id_project_team);
            if (item) {
                return item.space_name;
            } else {
                return '';
            }
        }
        return '';
    }
    isItemFinal(id) {
        if (id === this.ItemFinal) {
            return true;
        }
        return false;
    }

    drop1(event: CdkDragDrop<string[]>) {
        moveItemInArray(this.ListTasks, event.previousIndex, event.currentIndex);
    }

    drop2(event: CdkDragDrop<string[]>) {
        moveItemInArray(this.listField, event.previousIndex, event.currentIndex);
        const item = (this.listField[event.currentIndex]);
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
    DragDropItemWork(item) {
        const dropItem = new DrapDropItem();
        this._service.DragDropItemWork(item).subscribe(res => {
        });
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
        const ele = (document.getElementById('task' + val) as HTMLInputElement);
        setTimeout(() => {
            ele.focus();
        }, 50);
    }
    focusOutFunction(event, node) {
        this.isEdittitle = -1;
        if (event.target.value.trim() === node.title.trim() || event.target.value.trim() === '') {
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
    AssignInsert(assign) {
        let NV = new UserInfoModel();
        NV = assign;
        NV.id_user = assign.id_nv;
        NV.loai = 1;
        return NV;
    }
    bindStatus(id_project_team, val) {
        const item = this.ListAllStatusDynamic.find(x => +x.id_row === id_project_team);
        let index;
        if (item) {
            index = item.status.find(x => x.id_row === val);
        }
        if (index) {
            return index.statusname;
        }
        return this.translate.instant('GeneralKey.chuagantinhtrang');
    }

    clickOutside() {
        if (this.addNodeitem > 0) {
            this.addNodeitem = 0;
        }
    }
    Themcot() {
        this.ListColumns.push({
            fieldname: 'cot' + this.cot,
            isbatbuoc: true,
            isnewfield: false,
            isvisible: false,
            position: this.ListColumns.length,
            title: 'Cột' + this.cot,
            type: null
        });
        this.cot++;

    }
    // Assign
    ItemSelected(val: any, task) { // chọn item
        if (val.id_user) {
            val.id_nv = val.id_user;
        }
        this.UpdateByKey(task, 'assign', val.id_nv);
    }

    loadOptionprojectteam(node) {
        const id_project_team = node.id_project_team;
        this.LoadUserByProject(id_project_team);
    }
    LoadUserByProject(id_project_team) {
        const filter: any = {};
        filter.id_project_team = id_project_team;
        this.WeWorkService.list_account(filter).subscribe(res => {
            if (res && res.status === 1) {
                this.listUser = res.data;
                this.options_assign = this.getOptions_Assign();
                this.changeDetectorRefs.detectChanges();
            }
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
            data: this.selectedDate
        });

        dialogRef.afterClosed().subscribe(result => {
            if (result != undefined) {
                this.selectedDate.startDate = new Date(result.startDate);
                this.selectedDate.endDate = new Date(result.endDate);
            }
        });
    }
    SelectFilterDate() {
        const dialogRef = this.dialog.open(DialogSelectdayComponent, {
            width: '500px',
            data: this.filterDay
        });

        dialogRef.afterClosed().subscribe(result => {
            if (result != undefined) {
                this.filterDay.startDate = new Date(result.startDate);
                this.filterDay.endDate = new Date(result.endDate);
                // this.LoadWork();
                this.LoadTask();
            }
        });
    }

    OpenDetail(item) {
        this.router.navigate(['', { outlets: { auxName: 'aux/detail/' + item.id_row }, }]);
    }
    f_convertDate(v: any) {
        if (v != '' && v != undefined) {
            const a = new Date(v);
            return ('0' + (a.getDate())).slice(-2) + '/' + ('0' + (a.getMonth() + 1)).slice(-2) + '/' + a.getFullYear();
        }
    }
    viewdate() {
        if (this.selectedDate.startDate === '' && this.selectedDate.endDate === '') {
            return 'Set due date';
        } else {
            const start = this.f_convertDate(this.selectedDate.startDate);
            const end = this.f_convertDate(this.selectedDate.endDate);
            return start + ' - ' + end;
        }
    }
    GroupBy(item) {
        this.filter_groupby = item;
        this.LoadListStatusByProject();
        // this.LoadWork();
        this.LoadTask();
    }
    ExpandNode(node) {
        node.isExpanded = !node.isExpanded;
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
        if (item.filter_groupby && !item.filter_groupby.iscustom) {
            this.filter_groupby = item.filter_groupby;
        }
        if (item.filter_subtask) {
            this.filter_subtask = item.filter_subtask;
        }
        if (item.filterDay) {
            this.filterDay = item.filterDay;
        }
        if (item.isAssignforme) {
            this.filterwork == 0;
            this.isAssignforme = item.isAssignforme;
        }
    }
    CreateInfoProject() {
        const InfoNew = JSON.parse(localStorage.getItem("personal-task"));
        InfoNew.push({ projectteam: this.ID_Project, data: this.InfoFilter() });
        localStorage.setItem("personal-task", JSON.stringify(InfoNew));
    }

    UpdateInfoProject() {
        const InfoNew = JSON.parse(localStorage.getItem("personal-task"));
        InfoNew.forEach((res) => {
            if (res.projectteam == this.ID_Project) {
                res.data = this.InfoFilter();
            }
        });
        localStorage.setItem("personal-task", JSON.stringify(InfoNew));
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
            //   isAssignforme: this.isAssignforme,
        };
    }
    LoadFilterProject() {
        const Info = JSON.parse(localStorage.getItem("personal-task"));
        if (Info && Info.length > 0) {
            const itemproject = Info.find((x) => x.projectteam == this.ID_Project);
            if (itemproject) {
                this.LoadInfoProject(itemproject.data);
            } else {
                this.CreateInfoProject();
            }
        } else {
            localStorage.setItem("personal-task", JSON.stringify([]));
            this.CreateInfoProject();
        }
    }
    ShowCloseTask() {
        this.showclosedtask = !this.showclosedtask;
        // this.LoadWork();
        this.LoadTask();
    }

    ShowClosesubTask() {
        this.showclosedsubtask = !this.showclosedsubtask;
        // this.LoadWork();
        this.LoadTask();
    }

    LoadClosedTask(val) {
        if (this.showclosedtask) {
            return val.filter(x => x.status != this.ItemFinal);
        }
        return val;
    }
    Subtask(item) {
        if (item.value === this.filter_subtask.value) {
            return;
        }
        this.filter_subtask = item;
        // this.LoadWork();
        this.LoadTask();
    }
    CreateTask(val) {
        const x = this.newtask;
        this.CloseAddnewTask(true);
        // this.layoutUtilsService.showWaitingDiv();
        setTimeout(() => {
            this.newtask = x;
        }, 1000);
        this._service.InsertTask(val).subscribe(res => {
            // this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status === 1) {
                this.CloseAddnewTask(true);
                this.LoadListStatusByProject();
                // this.LoadWork();
                this.LoadTask();
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
        // this.AddnewTask(val, true);
    }
    DeleteTask(task) {
        this._service.DeleteTask(task.id_row).subscribe(res => {
            if (res && res.status === 1) {
                this.LoadListStatusByProject();
                // this.LoadWork();
                this.LoadTask();
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }
    IsAdmin(id_project_team) {
        if (this.IsAdminGroup) {
            return true;
        }
        if (this.list_role) {
            const x = this.list_role.find((x) => x.id_row === id_project_team);
            if (x) {
                if (x.admin === true || +x.admin === 1 || +x.owner === 1 || +x.parentowner === 1) {
                    return true;
                }
            }
        }
        return false;
    }

    Updateestimates(task, event) {
        this.UpdateByKey(task, 'estimates', event);
    }

    UpdateStatus(task, status) {
        if (+task.status === +status.id_row) {
            return;
        }
        this.UpdateByKey(task, 'status', status.id_row);
    }

    UpdateByKey(task, key, value) {
        const item = new UpdateWorkModel();
        item.id_row = task.id_row;
        item.key = key;
        item.value = value;
        if (task.id_nv > 0) {
            item.IsStaff = true;
        }
        this._service._UpdateByKey(item).subscribe(res => {
            if (res && res.status === 1) {
                // this.LoadListStatusByProject();
                // this.LoadWork();

            } else {
                // this.LoadWork();
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
        this.LoadTask();
    }

    GetColorName(val) {
        // name
        this.WeWorkService.getColorName(val).subscribe(res => {
            this.colorName = res.data.Color;
            return this.colorName;
        });
    }

    getTenAssign(val) {
        const list = val.split(' ');
        return list[list.length - 1];
    }

    updateDate(task, date, field) {
        if (date) {
            this.UpdateByKey(task, field, moment(date).format('MM/DD/YYYY HH:mm'));
        } else {
            this.UpdateByKey(task, field, null);
        }
    }

    updatePriority(task, field, value) {
        this.UpdateByKey(task, field, value);
    }

    UpdateTask(task) {
        this._service.UpdateTask(task.id_row).subscribe(res => {
            if (res && res.status === 1) {
                this.LoadListStatusByProject();
                this.LoadTask();
            }
        });
    }

    DeleteByKey(task, field) {
        this.UpdateByKey(task, field, null);
    }

    getAssignee(id_nv) {
        if (+id_nv > 0 && this.listUser) {
            const assign = this.listUser.find(x => x.id_nv === id_nv);
            if (assign) {
                return assign;
            }
            return false;
        }
        return false;
    }

    getPriority(id) {
        const item = this.list_priority.find(x => x.value === id);
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
            data: { _item, type }
        });
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                // this.ngOnInit();
            } else {
                // this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
                this.LoadListStatusByProject();
                this.LoadTask();
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
        const dialogRef = this.dialog.open(DuplicateWorkComponent, { data: { _item } });
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                this.ngOnInit();
            } else {
                this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
                this.ngOnInit();
            }
        });
    }

    work() {
        const model = new WorkModel();
        model.clear();
    }

    assign(node) {
        this.loadOptionprojectteam(node);
        const item = this.options_assign;
        const dialogRef = this.dialog.open(WorkAssignedComponent, {
            width: '500px',
            data: { item, ID_Project: node.id_project_team }
        });
        dialogRef.afterClosed().subscribe(res => {
            if (res) {
                this.UpdateByKey(node, 'assign', res.id_nv);
            }
        });
    }

    Add_followers() {
        let saveMessageTranslateParam = '';
        const _item = new WorkModel();
        // _item = this.detail;
        saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(workAddFollowersComponent, { data: { _item } });
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                this.ngOnInit();
            } else {
                this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
                this.ngOnInit();
            }
        });
    }

    Delete() {
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
        const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
        const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
        const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                return;
            }
        });
    }

    Create(_item: WorkDuplicateModel) {
        this._service.DuplicateCU(_item).subscribe(res => {
            if (res && res.status === 1) {
                this.layoutUtilsService.showInfo('Nhân bản thành công');
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }

    mark_tag() {
        this.WeWorkService.lite_tag(this.ID_Project).subscribe(res => {
            if (res && res.status === 1) {
                this.list_Tag = res.data;
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    ReloadData(event) {
        // this.ngOnInit();
        this.LoadTask();
    }

    RemoveTag(tag, item) {
        const model = new UpdateWorkModel();
        model.id_row = item.id_row;
        model.key = 'Tags';
        model.value = tag.id_row;
        this.WorkService.UpdateByKey(model).subscribe(res => {
            if (res && res.status === 1) {
                this.LoadListStatusByProject();
                this.LoadTask();
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
            }
        });
    }
    showTitleFilter(id) {
        if (this.idFilter > 0) {
            const x = this.danhsachboloc.find(x => x.id_row === id);
            if (x) {
                return x.title;
            } else {
                return 'Không tìm thấy bộ lọc';
            }
        } else {
            return 'Bộ lọc không hợp lệ';
        }
    }

    ChangeFilter(item) {
        const url = 'tasks/filter/' + item;
        this.router.navigateByUrl(url);
    }

    UpdateFilter() {
        const model = new FilterModel();
        model.clear();
        model.id_row = this.idFilter;
        this.Update(model);
    }

    addFilter() {
        const model = new FilterModel();
        model.clear(); // Set all defaults fields
        this.Update(model);
    }

    Update(_item: FilterModel) {
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(filterEditComponent, { data: { _item } });
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                return;
            } else {
                this.ngOnInit();
                this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
                // this.changeDetect.detectChanges();
            }
        });
    }

    getHeight() {
        let tmpHeight = 0;
        tmpHeight = window.innerHeight - 155 - this.tokenStorage.getHeightHeader();
        const link = this.router.url.includes('tasks');
        if (link) {
            tmpHeight += 45;
        }
        return tmpHeight;
    }

    IsaddTask() {
        if (this.getMystaff && this.filter_groupby.value === 'member') {
            return false;
        }
        return true;
    }
    trackByFn(index, item) {
        return item.id_row;
    }

    getNhom() {
        if (this.filter_groupby.value === 'member') {
            return 'assignee';
        }
        return 'status';
    }

    logItem(node) {
    }

    getDeadline(fieldname, date) {
        if (fieldname === 'deadline') {
            if (new Date(date) < new Date()) {
                // return 'text-danger'
                return 'red-color';
            }
        }
        return '';
    }

    Nguoitaocv(id) {
        if (this.listUser) {
            const x = this.listUser.find(x => x.id_nv === id);
            if (x) {
                return x;
            }
        }
        return {};
    }

    getComponentName(id_row) {
        if (id_row) {
            return this.componentName + id_row;
        } else {
            return '';
        }
    }

    CheckClosedTask(item) {
        // if (this.IsAdminGroup) {
        //     return true;
        // }
        if (item.closed) {
            return false;
        } else {
            return true;
        }
    }
}
