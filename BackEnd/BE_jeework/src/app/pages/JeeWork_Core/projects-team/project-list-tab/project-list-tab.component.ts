import { MatDialog } from '@angular/material/dialog';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Input, OnChanges } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
// RXJS
import { TranslateService } from '@ngx-translate/core';
// Services
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';// import { ProcessWorkService } from '../Services/process-work.service';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Models
import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';

// import { DynamicSearchFormService } from '../../../../dynamic-search-form/dynamic-search-form.service';
import { ProjectsTeamService } from '../Services/department-and-project.service';

@Component({
	selector: 'kt-project-list-tab',
	templateUrl: './project-list-tab.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class ProjectListTabComponent implements OnInit, OnChanges {
	@Input() ID_Project: number = 0;
	Show_1: boolean = false;
	Show_2: boolean = false;
	Show_3: boolean = false;
	Show_4: boolean = false;
	Show_5: boolean = false;
	Show_6: boolean = false;
	Show_10: boolean = false;

	ngOnChanges(){
	}
	constructor(
		public _Services: ProjectsTeamService,
 		public dialog: MatDialog,
		private router: Router, 
		public subheaderService: SubheaderService,
		private activatedRoute: ActivatedRoute,
		private changeDetectorRefs: ChangeDetectorRef,
 		) { 
			
		}

	/** LOAD DATA */
	ngOnInit() {
		this.activatedRoute.params.subscribe(params => {
			var temp = this.router.url;
			if (params.id) {
				this.ID_Project = +params.id;
				// this.Show_10 = true;
				this.router.navigateByUrl(temp+'/home/clickup');
			}
			else {
				var arr = temp.split("/");
				this.ID_Project = +arr[2];// =>/home/...
				this.Show_1 = this.Show_2 = this.Show_3 = this.Show_4 = this.Show_6;
				switch (params.view) {
					case 'board': {
						this.Show_10 = false;
						this.Show_2 = true;
						break;
					}
					case 'gantt': {
						this.Show_10 = false;
						this.Show_4 = true;
						break;
					}
					// case 'stream': {
					// 	this.Show_3 = true;
					// 	break;
					// }
					// case 'period': {
					// 	this.Show_6 = true;
					// 	break;
					// }
					case 'clickup': {
						this.Show_10 = true;
						break;
					}
					default: {
						this.Show_10 = true;
						break;
					}
				}
			}
			this.changeDetectorRefs.detectChanges();
		});
	}
}
