import { PartialsModule } from './../../../_metronic/jeework_old/partials/partials.module';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';

import { JeeWork_CoreModule } from '../JeeWork_Core.module';
import { WorkCalendarComponent } from './work-calendar.component';
import { WorkCalendarService } from './work-calendar.service';
// angular calendar

import { FullCalendarModule } from '@fullcalendar/angular';
import dayGridPlugin from '@fullcalendar/daygrid';
import timeGridPlugin from '@fullcalendar/timegrid';
import listPlugin from '@fullcalendar/list';
import interactionPlugin from '@fullcalendar/interaction';
FullCalendarModule.registerPlugins([
	dayGridPlugin,
	timeGridPlugin,
	listPlugin,
	interactionPlugin
  ])

const routes: Routes = [
	{
		path: '',
		component: WorkCalendarComponent
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
		FullCalendarModule,
	],
	providers: [
		WorkCalendarService
	],
	entryComponents: [
	],
	declarations: [
		WorkCalendarComponent,
	]
})
export class WorkCalendarModule { }
