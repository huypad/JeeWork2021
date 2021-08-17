import { TokenStorage } from 'src/app/_metronic/jeework_old/core/auth/_services';
import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, ElementRef, Input, HostListener } from '@angular/core';
import { Router, ActivatedRoute } from "@angular/router";
import { TranslateService } from '@ngx-translate/core';
import { FormGroup, FormBuilder } from '@angular/forms';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { WorkService } from '../../work/work.service';
import { MatDialog } from '@angular/material/dialog';
 import { ExportDialogComponent } from './export_dialog/export_dialog.component';
@Component({
	selector: 'm-project-team-config',
	templateUrl: './project-team-config.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProjectTeamConfigComponent implements OnInit {
	id_project_team: number;

	constructor(
		public dialog: MatDialog,
		private changeDetect: ChangeDetectorRef,
		private translate: TranslateService,
		private tokenStorage: TokenStorage,
		private itemFB: FormBuilder,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private el: ElementRef,
		private activatedRoute: ActivatedRoute,
		private router: Router,
		private _service: WorkService
	) { }

	ngOnInit() {

		var arr = this.router.url.split("/");
		this.id_project_team = +arr[2];
	}


	export() {
		
		this.dialog.open(ExportDialogComponent, { data: {}, width: '500px' }).afterClosed().subscribe(res => {
			if (res) {
				res.filter.id_project_team = this.id_project_team;
				this._service.ExportExcel(res).subscribe(response => {
					var headers = response.headers;
					let filename = headers.get('x-filename');
					let type = headers.get('content-type');
					let blob = new Blob([response.body], { type: type });
					const fileURL = URL.createObjectURL(blob);
					var link = document.createElement('a');
					link.href = fileURL;
					link.download = filename;
					link.click();
					//window.open(fileURL, '_blank');
				});
			}
		});
	}

	getHeight(){
		return (window.innerHeight - 60 - this.tokenStorage.getHeightHeader()) ;
	}
}
