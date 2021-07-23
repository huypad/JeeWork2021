import { SubheaderService } from './../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import { ProjectsTeamService } from './../WeWork/projects-team/Services/department-and-project.service';
import { AuxiliaryRouterComponent } from './auxiliary-router.component';
import { WeWorkModule } from './../WeWork/wework.module';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
// import { TagInputModule } from 'ngx-chips';
import { AngularMultiSelectModule } from 'angular2-multiselect-dropdown';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatExpansionModule } from '@angular/material/expansion';
import { DynamicComponentModule } from 'dps-lib';
// import { JeeHRSubModule } from '../../JeeHRSub.module';
const routes: Routes = [
	{
		path: 'task/:id',
		component: AuxiliaryRouterComponent,
	}
];

@NgModule({
	imports: [
		CommonModule,
		HttpClientModule,
		RouterModule.forChild(routes),
		FormsModule,
		ReactiveFormsModule,
		TranslateModule.forChild(),
		WeWorkModule,
		// TagInputModule,
		AngularMultiSelectModule,
		MatDatepickerModule,
		MatExpansionModule,
		DynamicComponentModule,
		// JeeHRSubModule
	],
	providers: [ ProjectsTeamService,
		SubheaderService ],
	entryComponents: [ ],
	declarations: [AuxiliaryRouterComponent ]
})
export class AuxiliaryRouterModule { }
