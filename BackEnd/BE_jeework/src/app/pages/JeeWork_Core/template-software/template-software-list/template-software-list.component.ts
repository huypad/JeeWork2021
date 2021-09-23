import {
    Component,
    OnInit,
    ElementRef,
    ViewChild,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
} from '@angular/core';
import {ActivatedRoute} from '@angular/router';
// Material
import {MatDialog} from '@angular/material/dialog';
import {MatPaginator} from '@angular/material/paginator';
import {MatSort} from '@angular/material/sort';
import {SelectionModel} from '@angular/cdk/collections';
// RXJS
import {tap} from 'rxjs/operators';
import {fromEvent, merge, BehaviorSubject} from 'rxjs';
import {TranslateService} from '@ngx-translate/core';
// Services
// Models
import {FormGroup, FormBuilder, Validators} from '@angular/forms';
import {isFulfilled} from 'q';
// import { LeaveRegistrationEditComponent } from '../leave-registration-edit/leave-registration-edit.component';
import {DatePipe} from '@angular/common';
import {TemplateSoftwareDataSource} from '../Model/data-sources/template-software.datasource';
import {templateSoftwareEditComponent} from '../template-software-edit/template-software-edit.component';
import {templateSoftwareService} from '../Services/template-software.service';
import {PaginatorState, SortState} from '../../../../_metronic/shared/crud-table';
import {GroupNameModel} from '../Model/template-software.model';
import {SubheaderService} from '../../../../_metronic/partials/layout';
import {LayoutUtilsService, MessageType} from '../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import {DanhMucChungService} from '../../../../_metronic/jeework_old/core/services/danhmuc.service';
import {QueryParamsModel} from '../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {TokenStorage} from '../../../../_metronic/jeework_old/core/auth/_services';

@Component({
    selector: 'kt-template-software-list',
    templateUrl: './template-software-list.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class templateSoftwareListComponent implements OnInit {
    // Table fields
    dataSource: TemplateSoftwareDataSource;
    displayedColumns = ['STT', 'ID_Mohinh', 'TenMohinh', 'CreatedDate', 'CreatedBy', 'UpdatedDate', 'UpdatedBy', 'IsDefault', 'Status', 'action'];
    sorting: SortState = new SortState();

    //Form
    itemForm: FormGroup;
    loadingSubject = new BehaviorSubject<boolean>(false);
    loadingControl = new BehaviorSubject<boolean>(false);
    loading1$ = this.loadingSubject.asObservable();
    hasFormErrors: boolean = false;
    selectedTab: number = 0;
    luu: boolean = true;
    Visable: boolean = false;
    capnhat: boolean = false;
    ID_NV: string = '';
    paginatorNew: PaginatorState = new PaginatorState();
    // Selection
    productsResult: GroupNameModel[] = [];
    showTruyCapNhanh: boolean = true;
    constructor(
        public templateService: templateSoftwareService,
        public dialog: MatDialog,
        public datepipe: DatePipe,
        private route: ActivatedRoute,
        private itemFB: FormBuilder,
        public subheaderService: SubheaderService,
        private layoutUtilsService: LayoutUtilsService,
        private translate: TranslateService,
        private tokenStorage: TokenStorage,
        private changeDetectorRefs: ChangeDetectorRef,
        private danhMucChungService: DanhMucChungService
    ) {
    }

    /** LOAD DATA */
    ngOnInit() {
        this.dataSource = new TemplateSoftwareDataSource(this.templateService);
        this.dataSource.entitySubject.subscribe(
            (res) => (this.productsResult = res)
        );
        this.loadDataList();

        this.dataSource.paginatorTotal$.subscribe(
            (res) => (this.paginatorNew.total = res)
        );
        this.changeDetectorRefs.detectChanges();

        setTimeout(() => {
            this.dataSource.loading$ = new BehaviorSubject<boolean>(false);
        }, 5000);
    }

    getTitle(): string {
        return this.translate.instant('template.mohinhduan');
    }

    //---------------------------------------------------------
    loadDataList() {
        const queryParams = new QueryParamsModel(
            this.filterConfiguration(),
            this.sorting.direction,
            this.sorting.column,
            this.paginatorNew.page - 1,
            this.paginatorNew.pageSize
        );
        this.dataSource.LoadMohinhDuan(queryParams);
    }

    loadPage() {
        var arrayData = [];
        this.dataSource.entitySubject.subscribe((res) => (arrayData = res));
        if (arrayData !== undefined && arrayData.length == 0) {
            var totalRecord = 0;
            this.dataSource.paginatorTotal$.subscribe((tt) => (totalRecord = tt));
            this.paginatorNew;
            if (totalRecord > 0) {
                const queryParams1 = new QueryParamsModel(
                    this.filterConfiguration(),
                    this.sorting.direction,
                    this.sorting.column,
                    this.paginatorNew.page - 1,
                    this.paginatorNew.pageSize
                );
                this.dataSource.LoadMohinhDuan(queryParams1);
            } else {
                const queryParams1 = new QueryParamsModel(
                    this.filterConfiguration(),
                    this.sorting.direction,
                    this.sorting.column,
                    this.paginatorNew.page = 0,
                    this.paginatorNew.pageSize
                );
                this.dataSource.LoadMohinhDuan(queryParams1);
            }
        }
    }

    /** FILTRATION */
    filterConfiguration(): any {
        const filter: any = {};
        filter.ID_NV = this.ID_NV;
        filter.Module = '0';
        return filter;
    }

    /** ACTIONS */
    paginate(paginator: PaginatorState) {
        this.loadDataList();
    }

    sortField(column: string) {
        const sorting = this.sorting;
        const isActiveColumn = sorting.column === column;
        if (!isActiveColumn) {
            sorting.column = column;
            sorting.direction = 'asc';
        } else {
            sorting.direction = sorting.direction === 'asc' ? 'desc' : 'asc';
        }
        // this.paginatorNew.page = 1;
        this.loadDataList();
    }

    getTooltipStatus(status) {
        if (!status) {
            return;
        }
        var text = 'Nhóm' + status.title + ': ';
        status.forEach((element) => {
            text += element.StatusName + ', ';
        });
        return text.slice(0, -2);
    }


    //========================Thêm mới nhóm người dùng=======================

    AddTemplate() {
        // const _model = new GroupNameModel();
        // _model.clear(); // Set all defaults fields
        this.Add({});
    }

    Add(_item : any) {
        let saveMessageTranslateParam = '';
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? this.translate.instant('GeneralKey.capnhatthanhcong')
                : this.translate.instant('GeneralKey.themthanhcong');
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        _item.id_template =_item.id_row > 0 ? _item.id_row : 0;
        const dialogRef = this.dialog.open(templateSoftwareEditComponent, {
            data: _item,
        });
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                this.loadDataList();
            } else {
                this.layoutUtilsService.showActionNotification(
                    _saveMessage,
                    _messageType,
                    4000,
                    true,
                    false
                );
                this.loadDataList();
            }
        });
    }

    lock(val: any, id: any) {
        this.templateService.SetDefault(id,val).subscribe(res => {
            if(res && res.status === 1){
                this.loadDataList();
            }else{
                this.loadDataList();
                this.layoutUtilsService.showError(res.error.message);
            }
        })
    }
    UpdateItem(item: any) {
        this.Add(item);
    }
    DeleteItem(item: any) {

    }

    //----------Hàm kiểm tra input------------------
    checkDate(v: any, row: any, index: any, col: string) {
        if (v.data == null) {
            this.dataSource.entitySubject.value[index]['cssClass'][col] = '';
            this.dataSource.entitySubject.value[index][col] = v.target.value;
        } else {
            if (v.data == '-') {
                this.dataSource.entitySubject.value[index]['cssClass'][col] =
                    'inp-error';
                return;
            } else {
                this.dataSource.entitySubject.value[index]['cssClass'][col] = '';
                this.dataSource.entitySubject.value[index][col] = v.target.value;
            }
        }
    }

    f_number(value: any) {
        return Number((value + '').replace(/,/g, ''));
    }

    f_currency(value: any, args?: any): any {
        let nbr = Number((value + '').replace(/,|-/g, ''));
        return (nbr + '').replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1,');
    }

    textPres(e: any, vi: any) {
        if (
            isNaN(e.key) &&
            //&& e.keyCode != 8 // backspace
            //&& e.keyCode != 46 // delete
            e.keyCode != 32 && // space
            e.keyCode != 189 &&
            e.keyCode != 45
        ) {
            // -
            e.preventDefault();
        }
    }

    text(e: any, vi: any) {
        if (
            !(
                (e.keyCode > 95 && e.keyCode < 106) ||
                (e.keyCode > 45 && e.keyCode < 58) ||
                e.keyCode == 8
            )
        ) {
            e.preventDefault();
        }
    }

    textNam(e: any, vi: any) {
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

    f_date(value: any, args?: any): any {
        let latest_date = this.datepipe.transform(value, 'dd/MM/yyyy');
        return latest_date;
    }


    //==========================================================
    getHeight(): any {
        let tmp_height = 0;
        tmp_height = window.innerHeight -  300 - this.tokenStorage.getHeightHeader();
        return tmp_height + 'px';
    }
}
