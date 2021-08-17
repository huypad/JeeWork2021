import { PartialsModule } from './../../../../_metronic/jeework_old/partials/partials.module';
// Angular
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
//Share
//Component
//Service
import { DynamicComponentModule } from 'dps-lib';
import { DatePipe, CommonModule } from '@angular/common';
import { WeWorkModule } from '../../wework.module';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { RepeatedListComponent } from './repeated-list.component';
// import { JeeHRSubModule } from '../../../JeeHRSub.module';
import { WorkService } from '../work.service';
import { WeWorkService } from '../../services/wework.services';
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
		WeWorkModule,
		DynamicComponentModule,
		// JeeHRSubModule,
	],
	providers: [
		DatePipe,
		WeWorkService,
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
