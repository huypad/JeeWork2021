import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service'

import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { WeWorkService } from '../../services/wework.services';
import { useAnimation } from '@angular/animations';
import { ActivitiesService } from '../activities.service';

@Component({
	selector: 'kt-log-activities',
	templateUrl: './log-activities.component.html',
})
export class LogActivitiesComponent implements OnInit {
	item: any;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	disabledBtn: boolean = false;
	IsEdit: boolean;
	IsProject: boolean;
	//====================Người Áp dụng====================
	public bankFilterCtrlAD: FormControl = new FormControl();
	public filteredBanksAD: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	//====================Người theo dõi===================
	public bankFilterCtrlTD: FormControl = new FormControl();
	public filteredBanksTD: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public datatree: BehaviorSubject<any[]> = new BehaviorSubject([]);
	title: string = '';
	selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
	ID_Log: number = 0;
	colorCtr: AbstractControl = new FormControl(null);
	tendapb: string = '';
	mota: string = '';
	constructor(public dialogRef: MatDialogRef<LogActivitiesComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _Logservice: ActivitiesService,
		private translate: TranslateService,
		public weworkService: WeWorkService,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {
		this.ID_Log = this.data.ID_Log;
		this._Logservice.LogDetail(this.ID_Log).subscribe(res => {
			if (res && res.status == 1) {
				this.item = res.data;
				this.changeDetectorRefs.detectChanges();
			}
			else {
			}
		});
	}
	/** UI */
	getTitle(): string {
		let result = this.translate.instant('filter.logdetail');
		return result;
	}
	/** ACTIONS */

	filterConfiguration(): any {

		const filter: any = {};
		return filter;
	}

	close() {
		this.dialogRef.close();
	}

}
