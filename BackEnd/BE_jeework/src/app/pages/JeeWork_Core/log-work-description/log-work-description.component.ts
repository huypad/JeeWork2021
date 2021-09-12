import {TimezonePipe} from './../pipe/timezone.pipe';
import {Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef} from '@angular/core';
import {MatDialogRef, MAT_DIALOG_DATA} from '@angular/material/dialog';
import {FormBuilder, FormGroup, Validators, FormControl, AbstractControl} from '@angular/forms';
import {TranslateService} from '@ngx-translate/core';
import {ReplaySubject, BehaviorSubject, Observable} from 'rxjs';
import {Router} from '@angular/router';
import {useAnimation} from '@angular/animations';
import {WorkService} from '../work/work.service';
import {WeWorkService} from '../services/wework.services';

@Component({
    selector: 'kt-log-work-description',
    templateUrl: './log-work-description.component.html',
    providers: [TimezonePipe],
})

export class LogWorkDescriptionComponent implements OnInit {
    item: any[];
    hasFormErrors: boolean = false;
    viewLoading: boolean = false;
    loadingAfterSubmit: boolean = false;
    disabledBtn: boolean = false;
    show_detail: boolean = false;
    id: number = 0;

    constructor(public dialogRef: MatDialogRef<LogWorkDescriptionComponent>,
                @Inject(MAT_DIALOG_DATA) public data: any,
                private fb: FormBuilder,
                private TimezonePipe: TimezonePipe,
                private changeDetectorRefs: ChangeDetectorRef,
                private _service: WorkService,
                private translate: TranslateService,
                public WeWorkService: WeWorkService,
                private router: Router,) {
    }

    /** LOAD DATA */
    ngOnInit() {
        this._service.LogDetailByWork(this.data.ID_log).subscribe(res => {
            if (res && res.status == 1) {
                this.item = res.data;
                this.item = this.item.filter(x => x.id_action == 16).sort((a, b) => this.compair(a.id_row, b.id_row) ? 1 : this.compair(b.id_row, a.id_row) ? -1 : 0);
                this.changeDetectorRefs.detectChanges();
            } else {
            }
        });
    }

    compair(a, b) {
        if (a < b) {
            return true;
        }
        return false;
    }

    /** UI */
    getTitle(): string {
        let result = this.translate.instant('filter.logdetaildescription');
        return result;
    }

    /** ACTIONS */
    ShowDetail(id_row) {
        this.id = id_row;
        this.show_detail = !this.show_detail;
    }

    filterConfiguration(): any {

        const filter: any = {};
        return filter;
    }

    close() {
        this.dialogRef.close();
    }

}
