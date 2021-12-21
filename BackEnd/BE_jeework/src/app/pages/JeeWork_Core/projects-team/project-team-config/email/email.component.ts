import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { LayoutUtilsService, MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Inject, HostListener, Input, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
// Services
// Models
import { AbstractControl, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { WeWorkService } from '../../../services/wework.services';
import { ProjectsTeamService } from '../../Services/department-and-project.service';

@Component({
	selector: 'kt-email',
	templateUrl: './email.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class EmailComponent {
	item1: any = {};
	IsProject: boolean;
	constructor(
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: ProjectsTeamService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: WeWorkService,
		private router: Router) { }
	/** LOAD DATA */
	ngOnInit() {
		var arr = this.router.url.split("/");
		let id_project_team = +arr[2];

		this.layoutUtilsService.showWaitingDiv();
		this._service.get_config_email(id_project_team).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1) {
				this.item1 = res.data;
				this.IsProject = this.item1.is_project;
			}
			else
				          this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
			this.changeDetectorRefs.detectChanges();
		});
	}

	update(event, key): any {
		event.preventDefault();
		let val = !this.item1[key];
		this.layoutUtilsService.showWaitingDiv();
		this._service.UpdateByKey(this.item1.id_row, key, val).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1)
				this.ngOnInit();
			else
				          this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
		})
	}
}
