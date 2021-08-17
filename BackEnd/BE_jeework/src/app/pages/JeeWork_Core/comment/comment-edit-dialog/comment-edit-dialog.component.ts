import { LayoutUtilsService } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Inject, Type } from '@angular/core';
// Material
import { MatDialog, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
// RxJS
import { Observable, BehaviorSubject, Subscription } from 'rxjs';
// NGRX
// Service
import { CommentService } from '../comment.service';

@Component({
	// tslint:disable-next-line:component-selector
	selector: 'kt-comment-edit-dialog',
	templateUrl: './comment-edit-dialog.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})
export class CommentEditDialogComponent implements OnInit {
	comment: any = {};
	viewLoading:boolean=false;
	constructor(
		public dialogRef: MatDialogRef<CommentEditDialogComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		public dialog: MatDialog,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private service: CommentService
	) {
	}
	/**
	 * @ Lifecycle sequences => https://angular.io/guide/lifecycle-hooks
	 */
	/**
	 * On init
	 */
	async ngOnInit() {
		this.comment = this.data;
		this.changeDetectorRefs.detectChanges();
	}
	close(data = undefined) {
		this.dialogRef.close(data);
	}

	onSubmit() {
		this.service.update(this.comment).subscribe(res => {
			if (res && res.status == 1) {
				this.close(res.data);
			}
		});
	}
}
