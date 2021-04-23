import { LayoutUtilsService } from './../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA, MatDialog } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { Router } from '@angular/router';

import { WeWorkService } from '../services/wework.services';

@Component({
	selector: 'kt-emotion-dialog',
	templateUrl: './emotion-dialog.component.html',
})
export class EmotionDialogComponent implements OnInit {
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	ListEmotion: any[] = [];
	constructor(public dialogRef: MatDialogRef<EmotionDialogComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		public dialog: MatDialog,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private router: Router,
		private service: WeWorkService) { }
	/** LOAD DATA */
	ngOnInit() {
		debugger
		this.service.lite_emotion().subscribe(res => {
			if(res && res.data)
				this.ListEmotion = res.data;
		})
	}
	close() {
		this.dialogRef.close();
	}

	select(key) {
		this.dialogRef.close(key);
	}
	
}
