import { LayoutUtilsService, MessageType } from '../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { WeWorkService } from '../../services/wework.services';
import { useAnimation } from '@angular/animations';
import { DatePipe } from '@angular/common';

@Component({
	selector: 'kt-preview-list',
	templateUrl: './preview-list.component.html',
})
export class previewlistComponent implements OnInit {
	constructor(public dialogRef: MatDialogRef<previewlistComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: WeWorkService,
		public datepipe: DatePipe,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {

	}
}
