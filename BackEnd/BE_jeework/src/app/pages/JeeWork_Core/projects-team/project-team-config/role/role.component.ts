import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { LayoutUtilsService } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Inject, HostListener, Input, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
// Services
// Models
import { AbstractControl, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { WeWorkService } from '../../../services/wework.services';
import { ProjectsTeamService } from '../../Services/department-and-project.service';

@Component({
	selector: 'kt-role',
	templateUrl: './role.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class RoleComponent {
	roles: any = [];
	IsProject: boolean;
	id_project_team: number;
	constructor(
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: ProjectsTeamService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private danhMucChungService: DanhMucChungService,
		public weworkService: WeWorkService,
		private router: Router) { }
	/** LOAD DATA */
	ngOnInit() {
		var arr = this.router.url.split("/");
		this.id_project_team = +arr[2];

		this.layoutUtilsService.showWaitingDiv();
		this._service.ListRole(this.id_project_team).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1) {
				this.roles = res.data;
			}
			else
				this.layoutUtilsService.showError(res.error.message);
			this.changeDetectorRefs.detectChanges();
		});
	}

	update(event, id_row, key, val: any = []): any {
		event.preventDefault();
		this.layoutUtilsService.showWaitingDiv();
		this._service.UpdateRole(this.id_project_team, key, id_row).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1)
				this.ngOnInit();
			else
				this.layoutUtilsService.showError(res.error.message);
		})
	}
}
