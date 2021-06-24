import { ReportByProjectComponent } from './../report/report-by-project/report-by-project.component';
/// <reference path="../activities/activities.module.ts" />
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ProjectListTabComponent } from './project-list-tab/project-list-tab.component';
import { WorkCalendarModule } from '../work-calendar/work-calendar.module';
import { ProjectsTeamComponent } from './projects-team.component';
import { DocumentsModule } from '../documents/documents.module';
const routes: Routes = [
	{
		path: '',
		component: ProjectsTeamComponent,
		children: [
			{
				path: '',
				component: ProjectListTabComponent
			},
			{
				path: 'home/:view',
				component: ProjectListTabComponent
			},
			{
				path: 'calendar',
				loadChildren: () => WorkCalendarModule
			},
			{
				path: 'documents',
				loadChildren: () => DocumentsModule
			},
			{
				path: 'activities',
				loadChildren: () => import('../activities/activities.module').then(m => m.ActivitiesModule),
			},
			{
				path: 'discussions',
				loadChildren: () => import('../discussions/discussions.module').then(m => m.DiscussionsModule),
			},
			{
				path: 'report/:id',
				component: ReportByProjectComponent,
			},
			{
				path: 'settings',
				loadChildren: () => import('./project-team-config/project-team-config.module').then(m => m.ProjectTeamConfigModule),
			},
		]
	}
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule]
})
export class ProjectTeamRoutingModule { }
