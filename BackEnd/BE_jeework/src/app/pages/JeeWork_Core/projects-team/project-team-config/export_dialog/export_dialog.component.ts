import { QueryParamsModelNew } from './../../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { TokenStorage } from 'src/app/_metronic/jeework_old/core/auth/_services';
import { Component, ChangeDetectionStrategy, ChangeDetectorRef, Inject, HostListener, Input, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatPaginator,PageEvent } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatDialog,MatDialogRef,MAT_DIALOG_DATA } from '@angular/material/dialog';
import { SelectionModel } from '@angular/cdk/collections';

import { WeWorkService } from '../../../services/wework.services';
import { PopoverContentComponent } from 'ngx-smart-popover';
import * as moment  from 'moment';
@Component({
	selector: 'kt-export_dialog',
	templateUrl: './export_dialog.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class ExportDialogComponent {
	filter: any = {};
	sortField: string = 'created_date';
	viewLoading:boolean=false;
	constructor(
		public dialogRef: MatDialogRef<ExportDialogComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		public weworkService: WeWorkService,
		public dialog: MatDialog,
		private route: ActivatedRoute,
		private changeDetectorRefs: ChangeDetectorRef,
		private router: Router,
		private tokenStorage: TokenStorage,) {
	}

	ngOnInit() {
		var now = moment();
		this.filter.DenNgay = now;
		this.filter.TuNgay = moment(now).add(-1, 'months');
		this.filter.displayChild = 0;
	}
	onSubmit() {
		let filter: any = {
			displayChild: this.filter.displayChild
		};
		if (this.filter.TuNgay)
			filter.TuNgay = moment(this.filter.TuNgay).format("DD/MM/YYYY")
		if (this.filter.DenNgay)
			filter.DenNgay = moment(this.filter.DenNgay).format("DD/MM/YYYY")
		var params = new QueryParamsModelNew(filter, "asc", this.sortField);
		this.dialogRef.close(params);
	}
	close() {
		this.dialogRef.close();
	}
}
