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
    SimpleChange,
} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
// Material
import {MatPaginator, PageEvent} from '@angular/material/paginator';
import {MatSort} from '@angular/material/sort';
import {
    MatDialog,
    MatDialogRef,
    MAT_DIALOG_DATA,
} from '@angular/material/dialog';
// RXJS
import {fromEvent, merge, ReplaySubject, BehaviorSubject, SubscriptionLike} from 'rxjs';
import {TranslateService} from '@ngx-translate/core';
// Services
import {
    LayoutUtilsService,
    MessageType,
} from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Models
import {QueryParamsModelNew} from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {TokenStorage} from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import {ViewTopicDetailComponent} from '../topic-view-detail/topic-view-detail.component';
import {DiscussionsService} from '../discussions.service';
import {PlatformLocation} from '@angular/common';
import {TopicEditComponent} from '../topic-edit/topic-edit.component';
import {TopicModel} from '../../projects-team/Model/department-and-project.model';
import {FormControl} from '@angular/forms';
import {WeWorkService} from '../../services/wework.services';
import {SearchBoxCustomComponent} from '../../projects-team/work-list-new/field-custom/search-box-custom/search-box-custom.component';

@Component({
    selector: 'kt-topic-list',
    templateUrl: './topic-list.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicListComponent {

    constructor(
        public _services: DiscussionsService,
        public dialog: MatDialog,
        private route: ActivatedRoute,
        private layoutUtilsService: LayoutUtilsService,
        private activatedRoute: ActivatedRoute,
        private changeDetectorRefs: ChangeDetectorRef,
        private router: Router,
        private translate: TranslateService,
        private tokenStorage: TokenStorage,
        public weworkService: WeWorkService,
        location: PlatformLocation
    ) {
        this.sortfield = this.listSort[0];
        location.onPopState(() => {
            this.close_detail();
        });
    }

    @Input() ID_QuyTrinh: any;
    data: any[] = [];
    loadingSubject = new BehaviorSubject<boolean>(false);
    loadingControl = new BehaviorSubject<boolean>(false);
    loading1$ = this.loadingSubject.asObservable();
    // =================PageSize Table=====================
    pageEvent: PageEvent;
    ID_Project = 0;
    pageSize: number;
    pageLength: number;
    item: any;
    sortfield: any = [];
    itemProject: any = [];
    ChildComponentInstance: any;
    selectedItem: any = undefined;
    childComponentType: any = ViewTopicDetailComponent;
    childComponentData: any = {};
    @ViewChild(MatPaginator, {static: true}) paginator: MatPaginator;
    @ViewChild('keyword', {static: true}) keyword: ElementRef;
    @ViewChild('custombox', {static: true}) custombox: SearchBoxCustomComponent;
    filterTinhTrang: string;
    subscription: SubscriptionLike;
    public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    public listTopic: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    public projectFilterCtrl: FormControl = new FormControl();
    listProject: any[] = [];
    showproject = false;
    listSort = [
        {
            // CreatedDate
            title: this.translate.instant('day.theongaytao'),
            value: 'CreatedDate',
            sortOder: 'asc',
        },
        {
            // title
            title: this.translate.instant('GeneralKey.tieude'),
            value: 'title',
            sortOder: 'asc',
        },
        {
            // UpdatedBy
            title: this.translate.instant('day.theongaycapnhat'),
            value: 'UpdatedDate',
            sortOder: 'asc',
        },
        {
            // CreatedDate
            title: this.translate.instant('topic.thaoluancunhat'),
            value: 'CreatedDate',
            sortOder: 'desc',
        },
    ];

    ngOnInit() {
        this.projectFilterCtrl.valueChanges.pipe().subscribe(() => {
            this.filterProject();
        });

        if (this._services.currentMessage != undefined) {
            this.subscription = this._services.currentMessage.subscribe(message => {
                if (message) {
                    this.LoadData();
                }
            });
        }
        this.activatedRoute.params.subscribe((params) => {
            this.ID_QuyTrinh = +params.id;
            console.log('quytrinh',this.ID_QuyTrinh);
        });

        const arr = this.router.url.split('/');
        if (arr[1] === 'project') {
            this.showproject = false;
            this._services.TopicDetail(arr[4]).subscribe(res => {
                if (res && res.status == 1) {
                    this.selectedItem = arr[4];
                }
            });
        }
        if (arr[1] === 'wework') {
            this.showproject = true;
            this._services.TopicDetail(arr[3]).subscribe(res => {
                if (res && res.status == 1) {
                    this.selectedItem = arr[3];
                }
            });
        }
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
                )
                this.setUpDropSearchProject();
                this.changeDetectorRefs.detectChanges();
            }
        });
        this.loadDataList();
    }

    setUpDropSearchProject() {
        this.projectFilterCtrl.setValue('');
        this.filterProject();
    }

    LoadData() {
        this.selectedItem = undefined;
        const arr = this.router.url.split('/');
        if (arr[1] === 'project') {
            this._services.TopicDetail(arr[4]).subscribe(res => {
                if (res && res.status === 1) {
                    this.selectedItem = arr[4];
                } else {
                    this.selectedItem = undefined;
                }
            }, (error => this.selectedItem = undefined));
        }
        if (arr[1] === 'wework') {
            this._services.TopicDetail(arr[3]).subscribe(res => {
                    if (res && res.status === 1) {
                        this.selectedItem = arr[3];
                    } else {
                        this.selectedItem = undefined;
                    }
                },
                (error => this.selectedItem = undefined));
        }
        this.loadDataList();
    }

    loadDataList(page: boolean = false) {
        const queryParams = new QueryParamsModelNew(
            this.filterConfiguration(),
            '',
            '',
            0,
            50,
            true
        );
        queryParams.sortField = this.sortfield.value;
        queryParams.sortOrder = this.sortfield.sortOder;
        this.data = [];
        this._services.findListTopic(queryParams).subscribe((res) => {
            if (res && res.status === 1) {
                this.data = res.data;
                this.filterListTopic();
            }
            this.changeDetectorRefs.detectChanges();
        });
    }

    filterConfiguration(): any {
        const filter: any = {};
        filter.keyword = this.custombox.keyword;
        return filter;
    }

    selectedField(item) {
        this.sortfield = item;
        this.loadDataList();
    }

    selectedProject(item) {
        this.itemProject = item;
        this.filterListTopic();
    }

    goBack() {
        window.history.back();
    }

    getHeight(): any {
        let tmp_height = 0;
        tmp_height = window.innerHeight - 63 - this.tokenStorage.getHeightHeader(); // 320
        return tmp_height + 'px';
    }

    selected($event) {
        this.selectedItem = $event;
        const temp: any = {};
        temp.Id = this.selectedItem.id_row;
        let _backUrl = `/wework/discussions`;
        const arr = this.router.url.split('/');
        if (arr[1] === 'project') {
            _backUrl =
                arr[0] +
                '/' +
                arr[1] +
                '/' +
                arr[2] +
                '/' +
                arr[3];
        }
        this.router.navigateByUrl(_backUrl).then(() => {
            this.router.navigateByUrl(_backUrl + '/' + this.selectedItem.id_row);
        });
        // this.childComponentData.DATA = temp
        // if (this.ChildComponentInstance != undefined)
        // 	this.ChildComponentInstance.ngOnChanges();
    }

    close_detail() {
        this.selectedItem = undefined;
    }

    getInstance($event) {
        this.ChildComponentInstance = $event;
    }

    applyFilter() {
        this.loadDataList();
    }

    AddTopic() {
        const models = new TopicModel();
        models.clear();
        const arr = this.router.url.split('/');
        const id_project_team = arr[2];
        models.id_project_team = id_project_team;
        this.UpdateTopic(models);
    }

    UpdateTopic(_item: TopicModel) {
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? 'GeneralKey.capnhatthanhcong'
                : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
            _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(TopicEditComponent, {data: {_item}});
        dialogRef.afterClosed().subscribe((res) => {
            this.ngOnInit();
            if (!res) {
                this.changeDetectorRefs.detectChanges();
            } else {
                this.layoutUtilsService.showActionNotification(
                    _saveMessage,
                    _messageType,
                    4000,
                    true,
                    false
                );
                this.changeDetectorRefs.detectChanges();
            }
        });
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

    protected filterListTopic() {
        if (!this.data) {
            return;
        }
        let idProject = 0;
        if (this.itemProject?.id_row > 0) {
            idProject = this.itemProject.id_row;
        }
        if (idProject > 0) {
            this.listTopic.next(
                this.data.filter(
                    x => x.id_project_team === idProject
                )
            );
        } else {
            this.listTopic.next(
                this.data
            );
        }
    }
}
