import { PartialsModule } from './../../../../_metronic/jeework_old/partials/partials.module';
// Angular
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
//Share
//Component
//Service
import { DynamicComponentModule } from 'dps-lib';
import { DatePipe, CommonModule } from '@angular/common';
import { JeeWork_CoreModule } from '../../JeeWork_Core.module';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { RepeatedListComponent } from './repeated-list.component';
// import { JeeHRSubModule } from '../../../JeeHRSub.module';
import { WorkService } from '../work.service';
import { JeeWorkLiteService } from '../../services/wework.services';
const routes: Routes = [
	{
		path: '',
		component: RepeatedListComponent,
		children: []
	}
];

@NgModule({
	imports: [
		// CommonModule,
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
		DatePipe,
		JeeWorkLiteService,
		WorkService,
	],
	entryComponents: [
	],
	declarations: [
	],
	exports: [
	]
})
export class RepeatedListModule { }
