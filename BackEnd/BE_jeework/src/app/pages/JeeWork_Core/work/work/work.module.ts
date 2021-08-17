import { SubheaderService } from './../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import { PartialsModule } from './../../../_metronic/jeework_old/partials/partials.module';
import { MyWorksComponent } from './my-works/my-works.component';
import { MyStaffNewComponent } from './my-staff-new/my-staff-new.component';
import { AvatarModule } from 'ngx-avatar';
// import { MaterialPreviewModule } from './../../../../partials/content/general/material-preview/material-preview.module';
// Angular
import { NgModule, Component } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
//Share
//Component
import { WorkComponent } from './work.component';
//Service
import { WeWorkService } from '../services/wework.services';
import { DatePipe, CommonModule } from '@angular/common';
import { JeeWork_CoreModule } from '../JeeWork_Core.module';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { WorkService } from './work.service';
import { WorkCalendarModule } from '../work-calendar/work-calendar.module';
import { WorkListModule } from './work-list/work-list.module';
import { PopoverModule } from 'ngx-smart-popover';
import { MyStaffReportComponent } from './my-staff-report/my-staff-report.component';
import { RepeatedListModule } from './repeated-list/repeated-list.module';
// import { CheckListEditComponent } from './check-list-edit/check-list-edit.component';
import { AttachmentService } from '../services/attachment.service';
// import { ChangeReivewerComponent } from './change-reivewer/change-reivewer.component'; 
const routes: Routes = [
	{
		path: '',
		component: WorkComponent,
		children: [
		// {
		// 	path: '',
		// 	redirectTo: 'calendar'
		// },
		{
			path: '',
			loadChildren: () =>  import('./work-list/work-list.module').then(m => m.WorkListModule),
		},
		{
			path: 'team',
			component: MyStaffNewComponent,
			data: { selectedTab: 1 },
		},
		// {
		// 	path: 'team',
		// 	loadChildren: () => WorkListModule,
		// 	data: { selectedTab: 1 },
		// },
		// {
		// 	path: 'team/report',
		// 	component: MyStaffReportComponent
		// },
		{
			path: 'following',
			loadChildren: ()=> import('./work-list/work-list.module').then(m => m.WorkListModule),
			data: { selectedTab: 3 }
		},
		{
			path: 'filter/:id',
			loadChildren: () =>  import('./work-list/work-list.module').then(m => m.WorkListModule),
			data: { selectedTab: 2 }
		},
		{
			path: 'calendar',
			loadChildren: () => import('../work-calendar/work-calendar.module').then(m => m.WorkCalendarModule),
		},
		{
			path: 'repeated',
			loadChildren: () => import('./repeated-list/repeated-list.module').then(m => m.RepeatedListModule),
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
		PopoverModule,
		JeeWork_CoreModule,
		// MaterialPreviewModule,
		AvatarModule
	],
	providers: [
		WeWorkService,
		WorkService,
		DatePipe,
		AttachmentService,
		SubheaderService,
	],
	entryComponents: [
		MyStaffReportComponent,
		// CheckListEditComponent,
		// ChangeReivewerComponent

	],
	declarations: [
		WorkComponent,
		MyStaffNewComponent,
		MyStaffReportComponent,
		// CheckListEditComponent,
		// ChangeReivewerComponent,
	],
	exports: [
	]
})
export class WorkModule { }
