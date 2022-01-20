import {LayoutUtilsService} from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import {MatDialog} from '@angular/material/dialog';
import {DialogSelectdayComponent} from './../dialog-selectday/dialog-selectday.component';
import { JeeWorkLiteService } from './../../services/wework.services';
import {tap} from 'rxjs/operators';
import {QueryParamsModelNew} from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {ReportService} from './../report.service';
import {Component, OnInit, ChangeDetectorRef} from '@angular/core';

const observer = {
    next: (val) => console.log(val),
    err: (err) => console.log(err),
    complete: () => {
    },
};

@Component({
    selector: 'kt-report-tab-member',
    templateUrl: './report-tab-member.component.html',
})
export class ReportTabMemberComponent implements OnInit {

    constructor(
        public reportService: ReportService,
        private detectChange: ChangeDetectorRef,
        public weworkService: JeeWorkLiteService,
        private layoutUtilsService: LayoutUtilsService,
        public dialog: MatDialog,
    ) {
    }
    selectedDate = {
        startDate: new Date(),
        endDate: new Date(),
    };

    ngOnInit() {
        var today = new Date();
        this.selectedDate = {
            startDate: new Date(today.getFullYear(),today.getMonth() - 1, 1),
            endDate: new Date(today.setMonth(today.getMonth())),
        };
        this.ThongKeHeThong();
    }

    filterConfiguration(): any {
        const filter: any = {};
        // filter.TuNgay = this.selectedDate.startDate;
        // filter.DenNgay = this.selectedDate.endDate;
        filter.TuNgay = (this.f_convertDate(this.selectedDate.startDate)).toString();
        filter.DenNgay = (this.f_convertDate(this.selectedDate.endDate)).toString();

        return filter;
    }

    _ThongKeHeThong: any = {};

    ThongKeHeThong() {
        this.layoutUtilsService.showWaitingDiv();
        const queryParams = new QueryParamsModelNew(
            this.filterConfiguration(),
        );
        //queryParams.sortField = this.column_sort.value;
        this.reportService.ThongKeHeThong(queryParams).pipe(
            tap(res => {
                if (res && res.status == 1) {
                    this._ThongKeHeThong = res.data;
                }
                this.layoutUtilsService.OffWaitingDiv();
                this.detectChange.detectChanges();

            })
        ).subscribe(observer);
    }

    XuatExcelThongKe() {
        var sanhsachthanhvien: any = [];
        this._ThongKeHeThong.dataUser.forEach(element => {
            sanhsachthanhvien.push({
                thanhvien: element.hoten,
                soduan: element.soduan,
                phongban: element.sophongban,
                congviec: element.num_work,
                hoanthanh: (parseInt(element.hoanthanh) + parseInt(element.ht_quahan)),
                quahan: element.quahan,
                dangthuchien: element.danglam,
            });
        });
        var Thongkehethong = {
            soduan: this._ThongKeHeThong.dataThongKe.Soduan,
            soduandadong: this._ThongKeHeThong.dataThongKe.Soduandadong,
            soduandangchay: this._ThongKeHeThong.dataThongKe.Soduandangchay,
            phongban: this._ThongKeHeThong.dataThongKe.Sophongban,
            congviec: this._ThongKeHeThong.dataThongKe.Socongviec,
            convieccon: this._ThongKeHeThong.dataThongKe.Socongvieccon,
            tongthanhvien: this._ThongKeHeThong.dataThongKe.Sothanhvien,
            sanhsachthanhvien: sanhsachthanhvien,
        };
        this.reportService.ExportReportExcelHeThong(Thongkehethong).pipe(
            tap(res => {
                const linkSource = `data:application/octet-stream;base64,${res.data.FileContents}`;
                const downloadLink = document.createElement('a');
                const fileName = res.data.FileDownloadName;

                downloadLink.href = linkSource;
                downloadLink.download = fileName;
                downloadLink.click();

            })
        ).subscribe(observer);
    }

    f_convertDate(p_Val: any) {
        let a = p_Val === '' ? new Date() : new Date(p_Val);
        return ('0' + (a.getDate())).slice(-2) + '/' + ('0' + (a.getMonth() + 1)).slice(-2) + '/' + a.getFullYear();
    }

    sum(a, b) {
        return (parseInt(a) + parseInt(b));
    }

    openDialog(): void {
        const dialogRef = this.dialog.open(DialogSelectdayComponent, {
            width: '500px',
            data: this.selectedDate
        });

        dialogRef.afterClosed().subscribe(result => {
            if (result != undefined) {
                this.selectedDate.startDate = new Date(result.startDate);
                this.selectedDate.endDate = new Date(result.endDate);
                // this.filter.TuNgay = (this.f_convertDate(this.selectedDate.startDate)).toString();
                // this.filter.DenNgay = (this.f_convertDate(this.selectedDate.endDate)).toString();
                this.ThongKeHeThong();
            }
        });
    }
}
