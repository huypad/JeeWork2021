import { PartialsModule } from './../../../../_metronic/jeework_old/partials/partials.module';
// Angular
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
//Share
//Component
//Service
import { WeWorkService } from '../../services/wework.services';
import { DynamicComponentModule } from 'dps-lib';
import { DatePipe, CommonModule } from '@angular/common';
import { JeeWork_CoreModule } from '../../JeeWork_Core.module';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
// import { JeeHRSubModule } from '../../../JeeHRSub.module';
import { WorkService } from '../../work/work.service';
import { WorkEditPageComponent } from '../../work/work-edit-page/work-edit-page.component';
import { Department_WorkListComponent } from './department-work-list.component';
const routes: Routes = [
	{
		path: '',
		// component: Department_WorkListComponent,
		children: [{
			path: 'detail/:id',
			component: WorkEditPageComponent,
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
		JeeWork_CoreModule,
		DynamicComponentModule,
		// JeeHRSubModule,
	],
	providers: [
		WeWorkService,
		WorkService,
		DatePipe
	],
	entryComponents: [
	],
	declarations: [
		// Department_WorkListComponent,
	],
	exports: [
	]
})
export class DepartmentWorkListModule { }
