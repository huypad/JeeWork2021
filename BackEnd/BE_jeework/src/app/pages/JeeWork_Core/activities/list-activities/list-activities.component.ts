import { WorkListNewDetailComponent } from './../../projects-team/work-list-new/work-list-new-detail/work-list-new-detail.component';
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
    SimpleChange
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { MatPaginator, PageEvent } from '@angular/material/paginator';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { fromEvent, merge, ReplaySubject, BehaviorSubject } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
// Services
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Models
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { LogActivitiesComponent } from '../log-activities/log-activities.component';
import { ActivitiesService } from '../activities.service';
import { WeWorkService } from '../../services/wework.services';
import { FormControl } from '@angular/forms';
import { DialogSelectdayComponent } from '../../report/dialog-selectday/dialog-selectday.component';
import { SearchBoxCustomComponent } from '../../projects-team/work-list-new/field-custom/search-box-custom/search-box-custom.component';

@Component({
    selector: 'kt-list-activities',
    templateUrl: './list-activities.component.html',
    styleUrls: ['./list-activities.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ListActivitiesComponent {
    @Input() ID_QuyTrinh: any;
    @Input() TenQuyTrinh: any;
    ID_milestone = 0;
    ListData: any[] = [];
    loadingSubject = new BehaviorSubject<boolean>(false);
    loadingControl = new BehaviorSubject<boolean>(false);
    loading1$ = this.loadingSubject.asObservable();
    // =================PageSize Table=====================
    pageEvent: PageEvent;
    pageSize: number;
    pageLength: number;
    item: any;
    list_priority: any = [];
    listProject: any = [];
    percentage: any;
    id_project_team = 0;
    selecteddate = 7;
    showproject = true;
    language = 'vi';
    filterDay = {
        startDate: new Date(),
        endDate: new Date(),
    };
    public projectFilterCtrl: FormControl = new FormControl();
    public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    public listStatus: any[] = [
        { ID: 1, Title: 'In progres', Checked: false },
        { ID: 2, Title: 'Overdue', Checked: false },
        { ID: 3, Title: 'Done late', Checked: false },
        { ID: 4, Title: 'Done ontime', Checked: false },
    ];
    @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
    @ViewChild('custombox', { static: true }) custombox: SearchBoxCustomComponent;

    constructor(public _actServices: ActivitiesService,
        public dialog: MatDialog,
        private layoutUtilsService: LayoutUtilsService,
        private weworkService: WeWorkService,
        private translate: TranslateService,
        private activatedRoute: ActivatedRoute,
        private changeDetectorRefs: ChangeDetectorRef,
        private router: Router,
        private tokenStorage: TokenStorage,) {
        this.language = localStorage.getItem('language');
        this.list_priority = this.weworkService.list_priority;
    }

    getActionActivities(value) {
        let text = '';
        text = value.action;
        if (text) {
            return text.replace('{0}', '');
        }
        return '';
    }

    ngOnInit() {

        this.projectFilterCtrl.valueChanges.pipe().subscribe(() => {
            this.filterProject();
        });

        const arr = this.router.url.split('/');
        if (+arr[2] > 0) {
            this.id_project_team = +arr[2];
            this.showproject = false;
        }
        this.SelectedDate(this.selecteddate);

        this.activatedRoute.params.subscribe(params => {
            this.ID_QuyTrinh = +params.id;
            this.ID_milestone = +params.id_milestone;
        });

        this.weworkService.lite_project_team_byuser('').subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
                this.listProject = res.data;
                this.listProject.unshift(
                    {
                        title: this.translate.instant('filter.tatcaduan'),
                        title_full: this.translate.instant('filter.tatcaduan'),
                        id_row: 0
                    }
                );
                this.setUpDropSearchProject();
                this.changeDetectorRefs.detectChanges();
            }
        });

        this.loadDataList();
    }

    /** LOAD DATA */
    selectedProject(item) {
        this.id_project_team = item.id_row;
        this.loadDataList();
    }

    setUpDropSearchProject() {
        this.projectFilterCtrl.setValue('');
        this.filterProject();
    }

    protected filterProject() {
        if (!this.listProject && this.listProject.length === 0) {
            return;
        }
        let search = this.projectFilterCtrl.value;
        if (!search) {
            this.filtereproject.next(this.listProject);
            return;
        } else {
            search = search.toLowerCase();
        }
        this.filtereproject.next(
            this.listProject.filter(
                (bank) => bank.title_full.toLowerCase().indexOf(search) > -1
            )
        );
    }

    getColorProgressbar(status: number = 0): string {
        if (status < 50) {
            return 'warn';
        } else if (status < 100) {
            return 'info';
        } else {
            return 'success';
        }
    }

    applyFilter() {
        this.loadDataList();
    }

    loadDataList(page: boolean = false) {
        const queryParams = new QueryParamsModelNew(
            this.filterConfiguration(),
            '',
            '',
            1,
            50,
            true
        );
        this.layoutUtilsService.showWaitingDiv();
        this._actServices.findListActivities(queryParams)
            .pipe(
                tap(resultFromServer => {
                    this.layoutUtilsService.OffWaitingDiv();
                    // this.pageLength = resultFromServer.page.TotalCount;
                    if (resultFromServer.status == 1) {
                        if (resultFromServer.data.length > 0) {
                            this.ListData = resultFromServer.data;
                        } else {
                            this.ListData = [];
                        }
                    } else {
                        this.ListData = [];
                    }
                    this.changeDetectorRefs.detectChanges();
                })
            ).subscribe();

    }

    filterConfiguration(): any {
        const filter: any = {};
        filter.keyword = this.custombox.keyword;
        filter.id_project_team = this.id_project_team;
        filter.TuNgay = this.f_convertDate(this.filterDay.startDate).toString();
        filter.DenNgay = this.f_convertDate(this.filterDay.endDate).toString();
        return filter;
    }

    SelectedDate(day: number) {
        this.selecteddate = day;
        let startdate = new Date();
        this.filterDay.startDate = new Date(startdate.setDate(startdate.getDate() - (day - 1)));
        this.filterDay.endDate = new Date();
        this.loadDataList();
    }

    getTimefilter() {
        const TuNgay = this.f_convertDate(this.filterDay.startDate).toString();
        const DenNgay = this.f_convertDate(this.filterDay.endDate).toString();
        switch (this.selecteddate) {
            case 0:
                return TuNgay + ' - ' + DenNgay;
                break;
            case 1:
                return 'Hôm nay';
                break;
            default:
                return this.selecteddate + ' ngày';
        }
    }

    SelectFilterDate() {
        const dialogRef = this.dialog.open(DialogSelectdayComponent, {
            width: '500px',
            data: this.filterDay,
        });
        const today = new Date();
        // endDate: new Date(today.setMonth(today.getMonth() + 1)),
        // startDate: new Date(today.getFullYear(), today.getMonth() - 6, 1),
        dialogRef.afterClosed().subscribe((result) => {
            if (result != undefined) {
                this.selecteddate = 0;
                this.filterDay.startDate = new Date(today.getFullYear(), today.getMonth() - 6, 1);
                this.filterDay.endDate = new Date(today.setMonth(today.getMonth() + 1));
                this.loadDataList();
            }
        });
    }

    getMatIcon(item: any): string {
        let _icon = '';

        if (item.is_quahan > 0) {
            _icon = 'watch_later';
        } else if (item.is_danglam > 0 || item.is_htquahan > 0) {
            _icon = 'check_circle';
        } else {
            _icon = 'watch_later';
        }
        return _icon;
    }

    buildColor(_color) {
        return (_color && _color.is_htquahan == 1) ? '#4CAF50' : (_color && _color.is_htdunghan == 1) ? '#dbaa07' : (_color && _color.is_quahan == 1) ? '#5969c5' : '#5969c5';
    }

    goBack() {
        const _backUrl = `ListDepartment/Tab/` + this.ID_QuyTrinh;
        this.router.navigateByUrl(_backUrl);
    }

    getItemCssClassByLocked(status: boolean): string {
        switch (status) {
            case true:
                return 'success';
        }
    }

    getItemLockedString(condition: boolean): string {
        switch (condition) {
            case true:
                return 'Important';
        }
    }

    getItemCssClassByOverdue(status: number = 0): string {

        switch (status) {
            case 1:
                return 'metal';
        }
    }

    getItemOverdue(condition: number): string {
        switch (condition) {
            case 1:
                return 'Overdue';
        }
    }

    getItemCssClassByurgent(status: boolean): string {

        switch (status) {
            case true:
                return 'brand';
        }
    }

    getItemurgent(condition: boolean): string {
        switch (condition) {
            case true:
                return 'Urgent';
        }
    }

    View_Log(ID_Log: number) {
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam += ID_Log > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = ID_Log > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(LogActivitiesComponent, { data: { ID_Log } });
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                // this.ngOnInit();
            } else {
                this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
                // this.ngOnInit();
            }
        });
    }

    Viewdetail(item) {
    debugger
        this.router.navigate(['', { outlets: { auxName: 'aux/detail/' + item.object_id }, }]);
    }

    f_convertDate(v: any) {
        if (v != '' && v != undefined) {
            let a = new Date(v);
            return (
                ('0' + a.getDate()).slice(-2) +
                '/' +
                ('0' + (a.getMonth() + 1)).slice(-2) +
                '/' +
                a.getFullYear()
            );
        }
    }

    getPriority(id) {
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

    getHeight() {
        return (window.innerHeight - 120 - this.tokenStorage.getHeightHeader()) + 'px';
    }

    getItemproject() {
        const itemproject = this.listProject.find(item => item.id_row === this.id_project_team);
        if (itemproject) {
            return itemproject.title_full;
        }
        return this.translate.instant('filter.tatcaduan');
    }
}
