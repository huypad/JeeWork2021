import { LayoutUtilsService } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { TokenStorage } from './../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { TranslateService } from '@ngx-translate/core';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Inject, HostListener, Input, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router, Route } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
// Models

import { PopoverContentComponent } from 'ngx-smart-popover';
import { AddUsersDialogComponent } from '../add-users-dialog/add-users-dialog.component';
import { ProjectsTeamService } from '../../Services/department-and-project.service';
import { WeWorkService } from '../../../services/wework.services';
@Component({
	selector: 'kt-members',
	templateUrl: './members.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class MembersComponent {
	id_project_team: number;
	admins: any[] = [];
	members: any[] = [];
	options: any = {};
	IsAdmin: boolean = false;
	customStyle:any = {};

	@ViewChild('myPopoverA', { static: true }) myPopoverA: PopoverContentComponent;
	constructor(public _services: ProjectsTeamService,
		public dialog: MatDialog,
		private layoutUtilsService: LayoutUtilsService,
		private activatedRoute: ActivatedRoute,
		private translate: TranslateService,
		private changeDetectorRefs: ChangeDetectorRef,
		private router: Router,
		public WeWorkService: WeWorkService,
		private tokenStorage: TokenStorage) {
	}

	ngOnInit() {
		var arr = this.router.url.split("/");
		this.id_project_team = +arr[2];
		this.options = this.getOptions();
		this.layoutUtilsService.showWaitingDiv();
		this._services.List_user(this.id_project_team).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1) {
				this.admins = res.data.filter(x => x.admin);
				this.members = res.data.filter(x => !x.admin);
			}
			this.changeDetectorRefs.detectChanges();
		});
	}
	getOptions() {
		var options: any = {};
		var filter: any = {};
		filter.key = 'id_project_team';
		filter.value = this.id_project_team;
		options.filter = filter;
		if (this.IsAdmin)
			options.excludes = this.admins.map(x => x.id_nv);
		else
			options.excludes = this.members.map(x => x.id_nv);
		return options;
	}

	initAddUser($event, admin = false) {
		this.options = this.getOptions();
		this.IsAdmin = admin;
		let el = $event.currentTarget.offsetParent;
		this.myPopoverA.show();
		this.myPopoverA.top = el.offsetTop + 50;
		this.myPopoverA.left = el.offsetLeft;
		this.changeDetectorRefs.detectChanges();
	}
	ItemSelected(user) {
		this.myPopoverA.hide();
		this.addMember(user.id_nv);
		//this.layoutUtilsService.showInfo("ItemSelected " + user.id_nv);
	}
	initAddMembers(admin = false) {
		var title = admin ? this.translate.instant('GeneralKey.themnhieuquanlyduan') :  this.translate.instant('GeneralKey.themnhieuthanhvien');
		const dialogRef = this.dialog.open(AddUsersDialogComponent, { data: { title: title, filter: {}, excludes: [] }, width: '500px' });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.addMembers(res, admin);
			}
		});
	}
	addMembers(users, admin) {
		this.layoutUtilsService.showWaitingDiv();
		var data = {
			id_row: this.id_project_team,
			Users: users.map(x => {
				return {
					id_user: x,
					admin: admin
				}
			})
		}
		this._services.Add_user(data).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1)
				this.ngOnInit();
			else
				this.layoutUtilsService.showError(res.error.message);
		})
		//this.layoutUtilsService.showInfo("add " + (admin ? "admins" : "members"));
	}
	addMember(id_nv) {
		this.layoutUtilsService.showWaitingDiv();
		var data = {
			id_row: this.id_project_team,
			Users: [{
				id_user: id_nv,
				admin: this.IsAdmin
			}]
		}
		this._services.Add_user(data).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1)
				this.ngOnInit();
			else
				this.layoutUtilsService.showError(res.error.message);
		})
		//this.layoutUtilsService.showInfo("add " + (admin ? "admin" : "member"));
	}
	chatWith(id_nv) {
		this.layoutUtilsService.showInfo("chatWith " + id_nv);
	}
	viewProfile(id_nv) {
		this.layoutUtilsService.showInfo("viewProfile " + id_nv);
	}
	delete(id_row) {
		this.layoutUtilsService.showWaitingDiv();
		this._services.Delete_user(id_row).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1)
				this.ngOnInit();
			else
				this.layoutUtilsService.showError(res.error.message);
		})
		//this.layoutUtilsService.showInfo("delete " + id_row);
	}
	updateRule(id_row, admin = true) {
		this.layoutUtilsService.showWaitingDiv();
		this._services.update_user(id_row, admin).subscribe(res => {
			this.layoutUtilsService.OffWaitingDiv();
			if (res && res.status == 1)
				this.ngOnInit();
			else
				this.layoutUtilsService.showError(res.error.message);
		})
		//this.layoutUtilsService.showInfo("updateRule " + id_row + ", " + admin);
	}
}
