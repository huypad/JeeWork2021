import { PartialsModule } from './../../../_metronic/jeework_old/partials/partials.module';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { JeeWorkLiteService } from '../services/wework.services';
import { DatePipe, CommonModule } from '@angular/common';
import { JeeWork_CoreModule } from '../JeeWork_Core.module';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
 import { FilterComponent } from './filter.component';
import { filterService } from './filter.service';
const routes: Routes = [
	{
		path: '',
		component: FilterComponent,
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
		JeeWork_CoreModule
	],
	providers: [
		JeeWorkLiteService,
		filterService,
		DatePipe,
	],
	entryComponents: [

	],
	declarations: [
		FilterComponent
	],
	exports: [
	]
})
export class FilterModule { }
