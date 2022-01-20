import { JeeWorkLiteService } from './../../services/wework.services';
import {element} from 'protractor';
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
    EventEmitter,
    Output
} from '@angular/core';
import {ActivatedRoute, Router} from '@angular/router';
// Material
import {MatPaginator, PageEvent} from '@angular/material/paginator';
import {MatDialog} from '@angular/material/dialog';
import {SelectionModel} from '@angular/cdk/collections';
// RXJS
import {debounceTime, distinctUntilChanged, tap} from 'rxjs/operators';
import {fromEvent, merge, ReplaySubject, BehaviorSubject} from 'rxjs';
import {TranslateService} from '@ngx-translate/core';
// Services
// import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service'; 
import {LayoutUtilsService, MessageType} from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Models
// import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
// import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {TokenStorage} from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
// import { WorkEditDialogComponent } from '../../work/work-edit-dialog/work-edit-dialog.component';
import {ViewTopicDetailComponent} from '../topic-view-detail/topic-view-detail.component';
import {DiscussionsService} from '../discussions.service';
import * as moment from 'moment';

@Component({
    selector: 'kt-topic-view',
    templateUrl: './topic-view.component.html',
    styleUrls: ['./topic-view.component.scss'],
    changeDetection: ChangeDetectionStrategy.OnPush
})
export class TopicViewComponent {
    @Input() ID_Project: any;
    @Input() data: any[];
    @Input() selectedItem: any = undefined;
    @Output() ItemSelected = new EventEmitter<any>();
    selectedId: number = 0;
    loadingSubject = new BehaviorSubject<boolean>(false);
    loadingControl = new BehaviorSubject<boolean>(false);
    loading1$ = this.loadingSubject.asObservable();
    //=================PageSize Table=====================
    pageEvent: PageEvent;
    pageSize: number;
    pageLength: number;
    item: any;
    percentage: any;
    Disabled_checkall: boolean = true;
    Disabled_DuHoSo: boolean = true;
    Disabled_Item: boolean = true;
    ChildComponentInstance: any;
    childComponentType: any = ViewTopicDetailComponent;
    childComponentData: any = {};
    @ViewChild(MatPaginator, {static: true}) paginator: MatPaginator;
    @ViewChild('keyword', {static: true}) keyword: ElementRef;
    id_project_team: number = 0;

    constructor(public _services: DiscussionsService,
                public dialog: MatDialog,
                private route: ActivatedRoute,
                private layoutUtilsService: LayoutUtilsService,
                private translate: TranslateService,
                private activatedRoute: ActivatedRoute,
                private changeDetectorRefs: ChangeDetectorRef,
                public WeWorkService: JeeWorkLiteService,
                private router: Router,
                private tokenStorage: TokenStorage,) {
    }

    ngOnChanges() {
        if (this.selectedItem != undefined) {
            this.selectedId = this.selectedItem.id_row;
        } else {
            this.selectedId = 0;
        }
        var arr = this.router.url.split('/');
        this.id_project_team = +arr[2];
        if ('' + this.id_project_team != 'NaN' && this.data.length > 0) {
            this.data = this.data.filter(x => x.id_project_team == this.id_project_team);
        }
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
        let _backUrl = `ListDepartment/Tab/` + this.ID_Project;
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

    selected(item) {

        this.ItemSelected.emit(item);
        this.selectedId = item.id_row;
        this.selectedItem = item;
    }

    getItemCss(): string {
        return 'brand';
    }

    // selected($event) {
    // 	this.selectedItem = $event;
    // 	let temp: any = {};
    // 	temp.Id = this.selectedItem.id_row;
    // 	this.childComponentData.DATA = temp
    // 	if (this.ChildComponentInstance != undefined)
    // 		this.ChildComponentInstance.ngOnChanges();
    // }
    close_detail() {
        this.selectedItem = undefined;
    }

    getInstance($event) {
        this.ChildComponentInstance = $event;
    }

    getHeight(): any {
        let obj = window.location.href.split('/').find(x => x == 'wework');
        let tmp_height = 0;
        // if (obj) {
        // 	tmp_height = window.innerHeight - 133;//300
        // } else {
        // 	tmp_height = window.innerHeight - 175;//300
        // }
        tmp_height = window.innerHeight - 120 - this.tokenStorage.getHeightHeader();
        return tmp_height + 'px';
    }

    favourite(item) {
        this.layoutUtilsService.showWaitingDiv();
        this._services.favouriteTopic(item.id_row).subscribe(res => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 1) {
                //favorite true == 1// false == 0
                this.data.forEach(element => {
                    if (element.id_row == item.id_row) {
                        if (res.data) {
                            element.favourite = 1;
                        } else {
                            element.favourite = 0;
                        }
                    }
                });
                this.changeDetectorRefs.detectChanges();
                this.layoutUtilsService.showActionNotification(this.translate.instant('GeneralKey.capnhatthanhcong'), MessageType.Read, 2000, true, false, 3000, 'top', 1);
            } else {
                this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
            }

        });
    }

    convertDate(d: any) {
        return moment(d + 'z').format("DD/MM/YYYY HH:mm");
    }
}
