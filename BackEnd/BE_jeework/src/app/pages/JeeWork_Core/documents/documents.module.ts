import { PartialsModule } from './../../../_metronic/jeework_old/partials/partials.module';
import { DocumentsService } from './documents.service';
import { DocumentsComponent } from './documents.component';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { JeeWork_CoreModule } from '../JeeWork_Core.module';
// import { TagInputModule } from 'ngx-chips';
import { AngularMultiSelectModule } from 'angular2-multiselect-dropdown';

import { MatExpansionModule } from '@angular/material/expansion';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { DynamicComponentModule } from 'dps-lib';
// import { JeeHRSubModule } from '../../JeeHRSub.module';
import { ListDocumentsComponent } from './list-documents/list-documents.component';
const routes: Routes = [
	{
		path: '',
		component: DocumentsComponent,
		children: [
			{
				path: '',
				component: ListDocumentsComponent,
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
		// TagInputModule,
		AngularMultiSelectModule,
		MatDatepickerModule,
		MatExpansionModule,
		DynamicComponentModule,
		// JeeHRSubModule
	],
	providers: [
		DocumentsService,
        ListDocumentsComponent,

	],
	entryComponents: [
	],
	declarations: [
		DocumentsComponent,
		ListDocumentsComponent,
	]
})
export class DocumentsModule { }
