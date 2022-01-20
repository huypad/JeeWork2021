import { LayoutUtilsService, MessageType } from './../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Inject, Input, Output, EventEmitter, ViewEncapsulation, OnChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
// Material
import { MatDialog } from '@angular/material/dialog';
// RxJS
//Models
import { JeeWorkLiteService } from '../services/wework.services';
import { UpdateWorkModel } from '../work/work.model';
import { WorkService } from '../work/work.service';
import { Router } from '@angular/router';

@Component({
	selector: 'kt-choose-time',
	templateUrl: './choose-time.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})

export class ChooseTimeComponent implements OnInit, OnChanges {
	// Public properties
	@Input() Id?: number = 0;
	@Input() Id_key?: number = 0;
	@Input() Loai?: string = 'startdate';
	@Output() Timeselected= new EventEmitter<any>();
	timeinput: string = '';
	model: UpdateWorkModel;
	constructor(
		private FormControlFB: FormBuilder,
		public dialog: MatDialog,
		private layoutUtilsService: LayoutUtilsService,
		public weworkService: JeeWorkLiteService,
		private _service: WorkService,
		private router: Router,
		private changeDetectorRefs: ChangeDetectorRef) { }
	/**
	 * On init
	 */
	ngOnInit() {
	}
	ngOnChanges() {
	}
	dataChanged(date_input) {
		var date = new Date(date_input);
		let a = date;
		var day = a.getFullYear() + "/" + ("0" + (a.getMonth() + 1)).slice(-2) + "/" + ("0" + (a.getDate())).slice(-2) +" "
		+ a.getHours() +":"+ a.getMinutes() ;
		this.model = new UpdateWorkModel();
		this.model.id_row = this.Id;
		this.model.key = this.Loai;
		this.model.id_log_action = '' + this.Id_key;
		this.model.value = day;
		this.layoutUtilsService.showWaitingDiv();
		this._service.UpdateByKey(this.model).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1) {
				this.Timeselected.emit(res.data);
				this.changeDetectorRefs.detectChanges();
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999, true, false);
			}
		});
		// this.router.navigateByUrl("/tasks");
		this.changeDetectorRefs.detectChanges();
	}
}
