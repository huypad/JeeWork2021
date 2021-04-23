import { PartialsModule } from './../../../_metronic/jeework_old/partials/partials.module';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { WeWorkService } from '../services/wework.services';
import { DatePipe, CommonModule } from '@angular/common';
import { WeWorkModule } from '../wework.module';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { WorkCalendarModule } from '../work-calendar/work-calendar.module';
import { UpdateByKeysComponent } from './update-by-keys.component';
import { UpdateByKeyService } from './update-by-keys.service';
const routes: Routes = [
	{
		path: '',
		component: UpdateByKeysComponent,
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
		WeWorkModule
	],
	providers: [
		WeWorkService,
		UpdateByKeyService,
		DatePipe,
	],
	entryComponents: [
		UpdateByKeysComponent
	],
	declarations: [
		UpdateByKeysComponent
	],
	exports: [
		UpdateByKeysComponent
	]
})
export class UpdateByKeysModule { }
