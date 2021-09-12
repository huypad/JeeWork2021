import {WorkListNewDetailComponent} from './../../projects-team/work-list-new/work-list-new-detail/work-list-new-detail.component';
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
import {ActivatedRoute, Router} from '@angular/router';
// Material
import {MatDialog} from '@angular/material/dialog';
import {MatPaginator, PageEvent} from '@angular/material/paginator';
import {SelectionModel} from '@angular/cdk/collections';
// RXJS
import {debounceTime, distinctUntilChanged, tap} from 'rxjs/operators';
import {fromEvent, merge, ReplaySubject, BehaviorSubject} from 'rxjs';
import {TranslateService} from '@ngx-translate/core';
// Services
import {LayoutUtilsService, MessageType} from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Models
import {CdkDragDrop, moveItemInArray, transferArrayItem} from '@angular/cdk/drag-drop';
import {QueryParamsModelNew} from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {TokenStorage} from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import {LogActivitiesComponent} from '../log-activities/log-activities.component';
import {ActivitiesService} from '../activities.service';
import {WeWorkService} from '../../services/wework.services';

@Component({
    selector: 'kt-list-activities',
    templateUrl: './list-activities.component.html',
    styleUrls: ['./list-activities.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class ListActivitiesComponent {
    @Input() ID_QuyTrinh: any;
    @Input() TenQuyTrinh: any;
    ID_milestone: number = 0;
    ListData: any[] = [];
    loadingSubject = new BehaviorSubject<boolean>(false);
    loadingControl = new BehaviorSubject<boolean>(false);
    loading1$ = this.loadingSubject.asObservable();
    //=================PageSize Table=====================
    pageEvent: PageEvent;
    pageSize: number;
    pageLength: number;
    item: any;
    list_priority: any = [];
    percentage: any;
    id_project_team: number = 0;
    language = 'vi';
    public listStatus: any[] = [
        {ID: 1, Title: 'In progres', Checked: false},
        {ID: 2, Title: 'Overdue', Checked: false},
        {ID: 3, Title: 'Done late', Checked: false},
        {ID: 4, Title: 'Done ontime', Checked: false},
    ];
    @ViewChild(MatPaginator, {static: true}) paginator: MatPaginator;
    @ViewChild('keyword', {static: true}) keyword: ElementRef;

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
        var text = '';
        text = value.action;
        if (text) {
            return text.replace('{0}', '');
        }
        return '';
    }

    ngOnInit() {
        var arr = this.router.url.split('/');
        if (+arr[2] > 0) {
            this.id_project_team = +arr[2];
        }

        this.activatedRoute.params.subscribe(params => {
            this.ID_QuyTrinh = +params.id;
            this.ID_milestone = +params.id_milestone;
        });
        this.loadDataList();
    }

    /** LOAD DATA */

    getColorProgressbar(status: number = 0): string {
        if (status < 50) {
            return 'warn';
        } else if (status < 100) {
            return 'info';
        } else {
            return 'success';
        }
    }

    applyFilter(text: string) {
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
        ;
    }

    filterConfiguration(): any {
        const filter: any = {};
        filter.keyword = this.keyword.nativeElement.value;
        filter.id_project_team = this.id_project_team;
        return filter;
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
        let _backUrl = `ListDepartment/Tab/` + this.ID_QuyTrinh;
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
        const dialogRef = this.dialog.open(LogActivitiesComponent, {data: {ID_Log}});
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
        this.router.navigate(['', {outlets: {auxName: 'aux/detail/' + item.object_id},}]);
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
}
