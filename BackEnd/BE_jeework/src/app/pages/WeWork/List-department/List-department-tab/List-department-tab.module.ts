import { SubheaderService } from './../../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import { PartialsModule } from './../../../../_metronic/jeework_old/partials/partials.module';
import { AvatarModule } from 'ngx-avatar';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { ChartsModule } from 'ng2-charts';
import { ListDepartmentService } from '../Services/List-department.service';
import { DepartmentTabComponent } from '../List-department-tab/List-department-tab.component';
import { TabMucTieuComponent } from '../tab-muc-tieu/tab-muc-tieu.component';
import { MilestoneDetailComponent } from '../milestone-detail/milestone-detail.component';
import { WeWorkModule } from '../../wework.module';
// import { JeeHRSubModule } from '../../../JeeHRSub.module';
import { DepartmentProjectTabComponent } from '../../projects-team/department-project-tab/department-project-tab.component';
import { ReportService } from '../../report/report.service';
import { ReportTabDashboardComponent } from '../../report/report-tab-dashboard/report-tab-dashboard.component';
import { ReportTabComponent } from './report-tab/report-tab.component';
const routes: Routes = [
	{
		path: '',
		component: DepartmentTabComponent,
		children: [
			{
				path: '',
				redirectTo: 'projects'
			},
			{
				path: 'projects',
				component: DepartmentProjectTabComponent,
			},
			{
				path: 'milestones',
				component: TabMucTieuComponent,
			},
			{
				path: 'milestones/:id',
				component: MilestoneDetailComponent,//Các tab trong tquy trình
			},
			{
				path: 'report/:id',
				// component: ReportTabComponent,
				component: ReportTabDashboardComponent,
			}
		]
	}
];

@NgModule({
	imports: [
		CommonModule,
		HttpClientModule,
		 PartialsModule,
		RouterModule.forChild(routes),
		FormsModule,
		ReactiveFormsModule,
		TranslateModule.forChild(),
		// JeeHRSubModule,
		WeWorkModule,
		ChartsModule,
		AvatarModule
	],
	providers: [
		ListDepartmentService,
		ReportService,
		SubheaderService
	],
	entryComponents: [
		DepartmentTabComponent,
	],
	declarations: [
		DepartmentTabComponent,
		TabMucTieuComponent,
		MilestoneDetailComponent,
		ReportTabComponent,
	]
})
export class DepartmentTabModule { }
