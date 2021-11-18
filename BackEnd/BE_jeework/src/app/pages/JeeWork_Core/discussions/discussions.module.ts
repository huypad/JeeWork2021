import { TimezonePipe } from './../pipe/timezone.pipe';
import { PartialsModule } from './../../../_metronic/jeework_old/partials/partials.module';
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
import { ListDepartmentService } from '../department/Services/List-department.service';
// import { JeeHRSubModule } from '../../JeeHRSub.module';
import { TopicViewComponent } from './topic-view/topic-view.component';
import { ViewTopicDetailComponent } from './topic-view-detail/topic-view-detail.component';
import { TopicListComponent } from './topic-list/topic-list.component';
import { DiscussionsComponent } from './discussions.component';
import { DiscussionsService } from './discussions.service';
import { AttachmentService } from '../services/attachment.service';
import { AvatarModule } from "ngx-avatar";
import { NgxDocViewerModule } from 'ngx-doc-viewer';
const routes: Routes = [
	{
		path: '',
		component: DiscussionsComponent,
		children: [
			{
				path: '',
				component: TopicListComponent,
				children: [
					{
						path: ':id',
						component: ViewTopicDetailComponent,
					}]
			},
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
		// JeeHRSubModule,
		AvatarModule,
		NgxDocViewerModule
	],
	providers: [
		DiscussionsService,
		AttachmentService,
		// TimezonePipe
	],
	entryComponents: [
		TopicViewComponent,
		ViewTopicDetailComponent
	],
	declarations: [
		DiscussionsComponent,
		TopicListComponent,
		TopicViewComponent,
		ViewTopicDetailComponent,  
		// TimezonePipe
	],
	exports: [
		TopicViewComponent
	]
})
export class DiscussionsModule { }
