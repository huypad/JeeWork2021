import { RepeatedListComponent } from './../../work/repeated-list/repeated-list.component';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProjectTeamConfigComponent } from './project-team-config.component';
import { SettingComponent } from './setting/setting.component';
import { MembersComponent } from './members/members.component';
import { EmailComponent } from './email/email.component';
import { RoleComponent } from './role/role.component';
import { ImportComponent } from './import/import.component';
const routes: Routes = [
	{
		path: '',
		component: ProjectTeamConfigComponent,
		children: [
			{
				path: '',
				component: SettingComponent
			},
			{
				path: 'members',
				component: MembersComponent
			},
			{
				path: 'acl',
				component: RoleComponent
			},
			{
				path: 'email',
				component: EmailComponent
			},
			{
				path: 'repeated',
				component: RepeatedListComponent
			},
			{
				path: 'import',
				component: ImportComponent
			}
		]
	}
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule]
})
export class ProjectTeamConfigRoutingModule { }
