import { PartialsModule } from './../../../_metronic/jeework_old/partials/partials.module';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { WeWorkModule } from '../wework.module';
// import { TagInputModule } from 'ngx-chips';
import { AngularMultiSelectModule } from 'angular2-multiselect-dropdown';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatExpansionModule } from '@angular/material/expansion';
import { DynamicComponentModule } from 'dps-lib';
// import { JeeHRSubModule } from '../../JeeHRSub.module';
import { ActivitiesComponent } from './activities.component';
import { ListActivitiesComponent } from './list-activities/list-activities.component';
import { LogActivitiesComponent } from './log-activities/log-activities.component';
import { ActivitiesService } from './activities.service';
const routes: Routes = [
	{
		path: '',
		component: ActivitiesComponent,
		children: [
			{
				path: '',
				component: ListActivitiesComponent,
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
		WeWorkModule,
		AngularMultiSelectModule,
		MatDatepickerModule,
		MatExpansionModule,
		DynamicComponentModule,
	],
	providers: [
		ActivitiesService,
	],
	entryComponents: [
		ListActivitiesComponent,
		LogActivitiesComponent,
	],
	declarations: [
		ActivitiesComponent,
		ListActivitiesComponent,
		LogActivitiesComponent,
	]
})
export class ActivitiesModule { }
