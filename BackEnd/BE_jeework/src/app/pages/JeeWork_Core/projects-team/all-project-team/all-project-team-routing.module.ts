import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { ActivitiesModule } from '../../activities/activities.module';
import { DiscussionsModule } from '../../discussions/discussions.module';
import { ProjectsTeamListComponent } from '../projects-team-list/projects-team-list.component';
import { AllProjectTeamComponent } from './all-project-team.component';

const routes: Routes = [
	{
		path: '',
		component: AllProjectTeamComponent,
		children: [

			{
				path: '',
				redirectTo: 'projects'
			},
			{
				path: 'projects',
				component: ProjectsTeamListComponent,
			},
			{
				path: 'activities',
				// loadChildren: () => ActivitiesModule//'../activities/activities.module#ActivitiesModule',
				loadChildren: () =>  import('../../activities/activities.module').then(m => m.ActivitiesModule),
			},
			{
				path: 'discussions',
				// loadChildren: () => DiscussionsModule//'../discussions/discussions.module#DiscussionsModule',
				loadChildren: () => import('../../discussions/discussions.module').then(m => m.DiscussionsModule),
			},
		]
	}
];

@NgModule({
	imports: [RouterModule.forChild(routes)],
	exports: [RouterModule]
})
export class AllProjectTeamRoutingModule { }
