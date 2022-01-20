import { SubheaderService } from './../../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import { PartialsModule } from './../../../../_metronic/jeework_old/partials/partials.module';
import { MyWorksComponent } from './../my-works/my-works.component';
// Angular
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
//Share
//Component
//Service
import { JeeWorkLiteService } from '../../services/wework.services';
// import { WorkListComponent } from '../work-list/work-list.component';
// import { WorkEditPageComponent } from '../work-edit-page/work-edit-page.component';
import { DynamicComponentModule } from 'dps-lib';
import { DatePipe, CommonModule } from '@angular/common';
import { JeeWork_CoreModule } from '../../JeeWork_Core.module';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { WorkService } from '../work.service';
// import { JeeHRSubModule } from '../../../JeeHRSub.module';
const routes: Routes = [
	{
		path: '',
		component: MyWorksComponent, 
		// path: '',
		// component: WorkListComponent,
		// children: [{
		// 	path: 'detail/:id',
		// 	component: WorkEditPageComponent,
		// }
		// ]
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
		JeeWork_CoreModule,
		DynamicComponentModule
		// JeeHRSubModule,
	],
	providers: [
		JeeWorkLiteService,
		WorkService,
		DatePipe,
		SubheaderService
	],
	entryComponents: [
	],
	declarations: [
		// WorkListComponent,
		MyWorksComponent
	],
	exports: [
	]
})
export class WorkListModule { }
