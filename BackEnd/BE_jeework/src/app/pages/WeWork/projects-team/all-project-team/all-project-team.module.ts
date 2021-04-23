import { SubheaderService } from './../../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import { PartialsModule } from './../../../../_metronic/jeework_old/partials/partials.module';
import { CommonService } from './../../../../_metronic/jeework_old/core/services/common.service';
import { AvatarModule } from 'ngx-avatar';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { DepartmentProjecGirdComponent } from '../department-project-gird/department-project-gird.component';
import { WeWorkModule } from '../../wework.module';
// import { TagInputModule } from 'ngx-chips';
import { AngularMultiSelectModule } from 'angular2-multiselect-dropdown';

import { MatExpansionModule } from '@angular/material/expansion';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatProgressBarModule } from '@angular/material/progress-bar';import { DynamicComponentModule } from 'dps-lib';
// import { JeeHRSubModule } from '../../../JeeHRSub.module';
import { AllProjectTeamComponent } from './all-project-team.component';
import { AllProjectTeamRoutingModule } from './all-project-team-routing.module';
import { ProjectsTeamService } from '../Services/department-and-project.service';
import { ProjectsTeamListComponent } from '../projects-team-list/projects-team-list.component';

@NgModule({
	imports: [
		CommonModule,
		HttpClientModule,
		PartialsModule,
		FormsModule,
		ReactiveFormsModule,
		TranslateModule.forChild(),
		WeWorkModule,
		// TagInputModule,
		AngularMultiSelectModule,
		MatDatepickerModule,
		MatExpansionModule,
		DynamicComponentModule,
		// JeeHRSubModule,
		AllProjectTeamRoutingModule,
		AvatarModule
	],
	providers: [
		ProjectsTeamService,
		CommonService,
		SubheaderService,
	],
	entryComponents: [
	],
	declarations: [
		AllProjectTeamComponent,
		ProjectsTeamListComponent,
		DepartmentProjecGirdComponent,
	],
	exports: [
		ProjectsTeamListComponent,
		AllProjectTeamComponent
	]
})
export class AllProjectTeamModule { }
