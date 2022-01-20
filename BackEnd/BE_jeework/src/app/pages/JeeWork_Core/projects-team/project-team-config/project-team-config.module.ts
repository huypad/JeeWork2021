import { PartialsModule } from './../../../../_metronic/jeework_old/partials/partials.module';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';

//Service

// Products
import { ProjectTeamConfigComponent } from './project-team-config.component';
import { ProjectTeamConfigRoutingModule } from './project-team-config-routing.module';
import { RouterModule } from '@angular/router';
import { JeeWork_CoreModule } from '../../JeeWork_Core.module';
import { EmailComponent } from './email/email.component';
import { MembersComponent } from './members/members.component';
import { SettingComponent } from './setting/setting.component';
import { PopoverModule } from 'ngx-smart-popover';
import { AddUsersDialogComponent } from './add-users-dialog/add-users-dialog.component';
import { JeeWorkLiteService } from '../../services/wework.services';
import { EditorModule } from '@tinymce/tinymce-angular';
import { RoleComponent } from './role/role.component';
import { ImportComponent } from './import/import.component';
import { ExportDialogComponent } from './export_dialog/export_dialog.component';
import { RepeatedListComponent } from '../../work/repeated-list/repeated-list.component';
import { MatChipsModule } from '@angular/material/chips';
import { AvatarModule } from 'ngx-avatar';
import { WorkGroupComponent } from './work-group/work-group.component';
import { ConfigNotifyComponent } from './config-notify/config-notify.component';

@NgModule({
	imports: [
		CommonModule,
		RouterModule,
		HttpClientModule,
		PartialsModule,
		FormsModule,
		ReactiveFormsModule,
		TranslateModule.forChild(),
		ProjectTeamConfigRoutingModule,
		JeeWork_CoreModule,
		PopoverModule,
		EditorModule,
		MatChipsModule,
		AvatarModule

	],
	providers: [
		JeeWorkLiteService,
	],
	entryComponents: [
		AddUsersDialogComponent,
		ExportDialogComponent
	],
	declarations: [
		ProjectTeamConfigComponent,
		SettingComponent,
		EmailComponent,
		ConfigNotifyComponent,
		MembersComponent,
		AddUsersDialogComponent,
		RoleComponent,
		ImportComponent,
		ExportDialogComponent,
		WorkGroupComponent,
		// RepeatedListComponent
	],
	exports: [
		ProjectTeamConfigComponent,
		SettingComponent,
		EmailComponent,
		ConfigNotifyComponent,
		MembersComponent,
		AddUsersDialogComponent,
		RoleComponent,
		ImportComponent,
		ExportDialogComponent,
		WorkGroupComponent,
		// RepeatedListComponent
	]
})
export class ProjectTeamConfigModule { }
