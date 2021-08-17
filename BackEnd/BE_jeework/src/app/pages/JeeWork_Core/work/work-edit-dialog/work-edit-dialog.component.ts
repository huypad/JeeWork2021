import { LayoutUtilsService } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { TranslateService } from '@ngx-translate/core';
// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Inject, Type } from '@angular/core';
// Material
import { MatDialog, MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
// RxJS
import { Observable, BehaviorSubject, Subscription } from 'rxjs';
// NGRX
// Service
import { WorkEditComponent } from '../work-edit/work-edit.component';

@Component({
	// tslint:disable-next-line:component-selector
	selector: 'kt-work-edit-dialog',
	templateUrl: './work-edit-dialog.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkEditDialogComponent implements OnInit, OnDestroy {
	ChildComponentInstance: any;
	ComponentTitle: string = '';
	staticForm: string = '';
	viewLoading: boolean = false;
	childComponentData: any;
	childComponentType: Type<any>;
	// Private password
	private componentSubscriptions: Subscription;
	constructor(
		public dialogRef: MatDialogRef<WorkEditDialogComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		public dialog: MatDialog,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private changeDetectorRefs: ChangeDetectorRef,
	) {
		this.ComponentTitle = this.translate.instant('work.taomoicongviec') //Tạo mới công việc
	}
	/**
	 * @ Lifecycle sequences => https://angular.io/guide/lifecycle-hooks
	 */
	/**
	 * On init
	 */
	async ngOnInit() {

		this.childComponentType = WorkEditComponent;
		this.childComponentData = this.data;
		if (this.data._item.id_row > 0)
			this.ComponentTitle = this.translate.instant('work.chinhsuacongviec');
		this.changeDetectorRefs.detectChanges();
	}
	/**
	 * On destroy
	 */
	ngOnDestroy() {
		if (this.componentSubscriptions) {
			this.componentSubscriptions.unsubscribe();
		}
	}
	close(data) {
		this.dialogRef.close(data);
	}

	onSubmit() {
		let data = this.ChildComponentInstance.onSubmit();
		if (data) {
			this.close(data);
		}
	}

	getInstance($event) {
		this.ChildComponentInstance = $event;
	}

}
