import {TokenStorage} from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import {MenuHorizontalService} from './../../../../_metronic/jeework_old/core/_base/layout/services/menu-horizontal.service';
import {SubheaderService} from './../../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import {
    Component,
    OnInit,
    ElementRef,
    ViewChild,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    HostListener,
    OnDestroy,
    Input,
    EventEmitter,
    Output
} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
import {DatePipe} from '@angular/common';
import {TranslateService} from '@ngx-translate/core';
// Material
import {MatSort} from '@angular/material/sort';
import {MatDialog} from '@angular/material/dialog';
import {MatPaginator, PageEvent} from '@angular/material/paginator';
import {SelectionModel} from '@angular/cdk/collections';
import {CdkDragStart, CdkDropList, moveItemInArray} from '@angular/cdk/drag-drop';
// RXJS
import {debounceTime, distinctUntilChanged, tap} from 'rxjs/operators';
import {ReplaySubject, fromEvent, merge, BehaviorSubject} from 'rxjs';
//Datasource
import {QueryParamsModelNew} from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {WorkService} from '../work.service';
import {RepeatedModel} from '../work.model';
import {Moment} from 'moment';
import * as moment from 'moment';
import {WorkDataSource} from '../work.datasource';
import {FormControl} from '@angular/forms';
import {DepartmentModel} from '../../department/Model/List-department.model';
import {LayoutUtilsService, MessageType} from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { JeeWorkLiteService } from '../../services/wework.services';
import {RepeatedEditComponent} from '../repeated-edit/repeated-edit.component';

//Model

@Component({
    selector: 'kt-repeated-list',
    templateUrl: './repeated-list.component.html',
    styleUrls: ['./repeated-list.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush,
    providers: [DatePipe]
})
export class RepeatedListComponent implements OnInit {
    @Input() item: any;
    @Output() ItemSelected = new EventEmitter<any>();
    UserID: number;
    // item: any = {};
    TuNgay: Moment;
    DenNgay: Moment;
    id_project_team: string = '';
    text_filter: string = '';
    collect_by: string = '0';
    hoanthanh: string = '0';
    data: any[] = [];
    selectedItem: any;
    listProject: any[] = [];
    public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    public projectFilterCtrl: FormControl = new FormControl();
    dataSource: WorkDataSource;
    @ViewChild(MatPaginator, {static: true}) paginator: MatPaginator;
    @ViewChild(MatSort, {static: true}) sort: MatSort;
    flag: boolean = true;
    keyword: string = '';
    // Selection
    selection = new SelectionModel<DepartmentModel>(true, []);
    productsResult: DepartmentModel[] = [];
    listUser: any[] = [];
    public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    public bankFilterCtrl: FormControl = new FormControl();
    listData_week: any[] = [];
    listData_month: any[] = [];
    pageEvent: PageEvent;
    pageSize: number;
    pageLength: number;
    listStatus: any[] = [];
    _HasItem: boolean;
    id_project = 0;

    constructor(
        public dialog: MatDialog,
        private activatedRoute: ActivatedRoute,
        private router: Router,
        private translate: TranslateService,
        public weworkService: JeeWorkLiteService,
        private _service: WorkService,
        private datePipe: DatePipe,
        private tokenStorage: TokenStorage,
        private layoutUtilsService: LayoutUtilsService,
        private changeDetectorRefs: ChangeDetectorRef,
        public menuHorService: MenuHorizontalService) {
        this.text_filter = this.translate.instant('filter.tatca');
        var x = (this.router.url).split('/');
        if (+x[2] > 0) {
            this.id_project = +x[2];
        } else {
        }

    }

    /** LOAD DATA */
    ngOnInit() {
        this.layoutUtilsService.showWaitingDiv();
        setTimeout(() => {
            this.layoutUtilsService.OffWaitingDiv();
            this.loadDataList(true);
        }, 1000);
        var now = moment();
        this.DenNgay = now;
        this.TuNgay = moment(now).add(-1, 'months');

        this.weworkService.lite_project_team_byuser('').subscribe(res => {
            if (res && res.status === 1) {
                this.listProject = res.data;
                this.setUpDropSearchProject();
                this.changeDetectorRefs.detectChanges();
            }
            ;
        });

        const filter: any = {};
        // filter.key = 'id_project_team';
        // filter.value = 1;
        this.weworkService.list_account({}).subscribe(res => {
            this.changeDetectorRefs.detectChanges();

            if (res && res.status === 1) {
                this.listUser = res.data;
                this.setUpDropSearchNhanVien();
                this.changeDetectorRefs.detectChanges();
            }
            ;
        });
    }

    loadDataList(page: boolean = false) {
        const queryParams = new QueryParamsModelNew(
            this.filterConfiguration(),
            // this.sort.direction,
            // this.sort.active,
            // this.paginator.pageIndex,
            // this.paginator.pageSize
            // page ? this.paginator.pageIndex : this.paginator.pageIndex = 0,
            // this.paginator.pageSize
        );
        this.layoutUtilsService.showWaitingDiv();
        this._service.findDataRepeated(queryParams)
            .pipe(
                tap(resultFromServer => {
                    this.layoutUtilsService.OffWaitingDiv();
                    this.pageLength = resultFromServer.page?.TotalCount;
                    if (resultFromServer.status == 1) {
                        if (resultFromServer.data.length > 0) {
                            var listRepeat = resultFromServer.data;
                            if (this.id_project > 0) {
                                listRepeat = listRepeat.filter(x => x.id_project_team == this.id_project);
                            }
                            this.listData_week = listRepeat.filter(x => x.frequency == 1);
                            this.listData_month = listRepeat.filter(x => x.frequency == 2);
                            this.item = listRepeat;
                            this._HasItem = listRepeat.length > 0 ? true : false;
                        } else {
                            this.listData_week = [];
                            this.listData_month = [];
                            this._HasItem = false;
                        }
                    } else {
                        this.listData_week = [];
                        this.listData_month = [];
                        this._HasItem = false;
                    }
                    this.changeDetectorRefs.detectChanges();
                })
            ).subscribe();
        ;
    }

    loadPage() {
        var arrayData = [];
        this.dataSource.entitySubject.subscribe(res => arrayData = res);
        // if (arrayData !== undefined && arrayData.length == 0) {
        var totalRecord = 0;
        this.dataSource.paginatorTotal$.subscribe(tt => totalRecord = tt);
        if (totalRecord > 0) {
            const queryParams1 = new QueryParamsModelNew(
                this.filterConfiguration(),
                // this.sort.direction,
                // this.sort.active,
                // this.paginator.pageIndex = this.paginator.pageIndex - 1,
                // this.paginator.pageSize
            );
            this.dataSource.loadRepeatedList(queryParams1);
        } else {
            const queryParams1 = new QueryParamsModelNew(
                this.filterConfiguration(),
                // this.sort.direction,
                // this.sort.active,
                // this.paginator.pageIndex = 0,
                // this.paginator.pageSize
            );
            this.dataSource.loadRepeatedList(queryParams1);
        }
        // }
    }

    getColorProgressbar(status: number = 0): string {
        if (status < 50) {
            return 'metal';
        } else if (status < 100) {
            return 'brand';
        } else {
            return 'success';
        }
    }

    setUpDropSearchNhanVien() {
        this.bankFilterCtrl.setValue('');
        this.filterBanks();
        this.bankFilterCtrl.valueChanges
            .pipe()
            .subscribe(() => {
                this.filterBanks();
            });
    }

    Update_Status(val: any) {

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
            this.listUser.filter(bank => bank.hoten.toLowerCase().indexOf(search) > -1)
        );
    }

    filterConfiguration(): any {
        let filter: any = {};
        // filter.TuNgay = this.TuNgay.format("DD/MM/YYYY");
        // filter.DenNgay = this.DenNgay.format("DD/MM/YYYY");
        // if (this.id_project_team > 0) {
        filter.id_project_team = this.id_project_team;
        // }
        // if (this.collect_by == '1')
        // 	filter.collect_by = 'deadline';
        // else
        // 	filter.collect_by = 'CreatedDate';
        return filter;
    }

    setUpDropSearchProject() {
        this.projectFilterCtrl.setValue('');
        this.filterProject();
        this.projectFilterCtrl.valueChanges
            .pipe()
            .subscribe(() => {
                this.filterProject();
            });
    }

    protected filterProject() {
        if (!this.listProject) {
            return;
        }
        let search = this.projectFilterCtrl.value;
        if (!search) {
            this.filtereproject.next(this.listProject.slice());
            return;
        } else {
            search = search.toLowerCase();
        }
        // filter the banks
        this.filtereproject.next(
            this.listProject.filter(bank => bank.title.toLowerCase().indexOf(search) > -1)
        );
    }

    filterByProjectteam(item) {
        if (item == '') {
            this.text_filter = this.translate.instant('filter.tatca');
            this.id_project_team = '';
            this.loadDataList();
        } else {
            this.text_filter = item.title;
            this.id_project_team = item.id_row;
            this.loadDataList();
        }
    }

    selected($event) {
        this.selectedItem = $event;
        let temp: any = {};
        temp.Id = this.selectedItem.id_row;
        var url = '/users/' + this.UserID + '/detail/' + temp.Id;
        this.router.navigateByUrl(url);
    }

    close_detail() {
        this.selectedItem = undefined;
        if (!this.changeDetectorRefs['destroyed']) {
            this.changeDetectorRefs.detectChanges();
        }
    }

    getHeight(): any {
        let obj = window.location.href.split('/').find(x => x == 'wework');

        if (obj) {
            let tmp_height = 0;
            tmp_height = window.innerHeight - 197 - this.tokenStorage.getHeightHeader();
            return tmp_height + 'px';
        } else {
            let tmp_height = 0;
            tmp_height = window.innerHeight - 120 - 60 - this.tokenStorage.getHeightHeader();
            return tmp_height + 'px';
        }
    }

    getWidth(): any {
        let tmp_width = 0;
        tmp_width = window.innerWidth - 800;
        return tmp_width + 'px';
    }

    Locked(id: any, item: any) {
        const model = new RepeatedModel();
        model.id_row = item.id_row;
        model.Locked = id;

        this.layoutUtilsService.showWaitingDiv();
        this._service.Locked(model.id_row, model.Locked).subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 0) {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
                this.ngOnInit();
            }
        });
    }

    updateProject(id: any, item: any) {
        const model = new RepeatedModel();
        model.id_row = item.id_row;
        model.id_project_team = id;
        this.layoutUtilsService.showWaitingDiv();
        this._service.updateProject(model.id_row, model.id_project_team).subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 0) {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
                this.ngOnInit();
            } else {
            }
        });
    }

    numSubtasks(val) {
        var x = val.filter(x => x.IsTodo == false);
        return x.length;
    }

    numTodo(val) {
        var x = val.filter(x => x.IsTodo == true);
        return x.length;
    }

    Assign(id: any, item: any) {
        const model = new RepeatedModel();

        model.id_row = item.id_row;
        model.assign = id;
        this.layoutUtilsService.showWaitingDiv();
        this._service.assign(model.id_row, model.assign).subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 0) {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
                this.ngOnInit();
            } else {
            }
        });
    }

    AddRepeated(Type: number) {
        const models = new RepeatedModel();
        models.clear();
        models.frequency = '' + Type;
        this.UpdateRepeated(models, false);
    }

    UpdateRepeated(_item: RepeatedModel, isCopy: boolean) {
        let saveMessageTranslateParam = '';
        _item.IsCopy = isCopy;
        _item['UpdateSubtask'] = '';
        saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(RepeatedEditComponent, {data: {_item, id_project: this.id_project}});
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
            } else {
                this.ngOnInit();
            }
        });
    }

    UpdateSubtask(_item: RepeatedModel, subtask: boolean) {
        let saveMessageTranslateParam = '';
        _item.IsCopy = false;
        _item['UpdateSubtask'] = subtask == true ? 'subtask' : 'todolist';
        saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(RepeatedEditComponent, {data: {_item, id_project: this.id_project}});
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
            } else {
                this.ngOnInit();
            }
        });
    }

    Deleted(_item: RepeatedModel) {
        // _item.id_row = this.item.id_row;
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant('department.confirmxoa');
        const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
        const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
        const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                return;
            }
            this._service.DeleteRepeatedTask(_item.id_row).subscribe(res => {
                if (res && res.status === 1) {
                    this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
                    this.ngOnInit();
                    // let _backUrl = `depts`;
                    // this.router.navigateByUrl(_backUrl);
                } else {
                    this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
                    this.ngOnInit();
                }
            });
        });
    }

    Forcerun(_item: RepeatedModel) {
        const _title = this.translate.instant('repeated.forcerun');
        const _description = this.translate.instant('repeated.forcerun_confirm');
        const _waitDesciption = this.translate.instant('repeated.forcerun_doing');
        const _deleteMessage = this.translate.instant('repeated.forcerun_thanhcong');
        const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
        dialogRef.afterClosed().subscribe(res => {
            if (!res) {
                return;
            }
            this._service.Forcerun(_item.id_row).subscribe(res => {
                if (res && res.status === 1) {
                    this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
                } else {
                    this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
                    this.ngOnInit();
                }
            });
        });
    }
}
