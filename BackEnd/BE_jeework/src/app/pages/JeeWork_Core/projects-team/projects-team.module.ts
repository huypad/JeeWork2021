import { QuickStatusComponent } from './gantt-chart-2/quick-status/quick-status.component';
import { SubheaderService } from './../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import { MatMenuModule } from '@angular/material/menu';
import { OwlDateTimeModule } from 'ng-pick-datetime';
import { PartialsModule } from './../../../_metronic/jeework_old/partials/partials.module';
import { CommonService } from './../../../_metronic/jeework_old/core/services/common.service';
import { PopoverModule } from 'ngx-smart-popover';
import { AvatarModule } from 'ngx-avatar';
import { JeeWorkLiteService } from './../services/wework.services';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { JeeWork_CoreModule } from '../JeeWork_Core.module';
import { AngularMultiSelectModule } from 'angular2-multiselect-dropdown';

import { MatToolbarModule } from '@angular/material/toolbar';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatExpansionModule } from '@angular/material/expansion';
import { WorkKanBanComponent } from './work-kanban/work-kanban.component';
import {
  AngularGanttScheduleTimelineCalendarModule,
} from 'angular-gantt-schedule-timeline-calendar';
import { GanttChart2Component } from './gantt-chart-2/gantt-chart-2.component';
import { DynamicComponentModule } from 'dps-lib';
import { ListDepartmentService } from '../department/Services/List-department.service';
import { ProjectListTabComponent } from './project-list-tab/project-list-tab.component';
import { EditorModule } from '@tinymce/tinymce-angular';
import { ProjectTeamRoutingModule } from './project-team-routing.module';
import { ProjectsTeamService } from './Services/department-and-project.service';
import { ProjectsTeamComponent } from './projects-team.component';
import { ChartsModule } from 'ng2-charts';
import { WorkService } from '../work/work.service';
import { ClickOutsideDirective } from './work-list-new/clickoutside.directive';
import { MatChipsModule } from '@angular/material/chips';
import { NgDragDropModule } from 'ng-drag-drop';
import { MatMomentDateModule } from '@angular/material-moment-adapter';
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { WorkProcessEditComponent } from './work-list-new/work-process-edit/work-process-edit.component';
import { NgGanttEditorModule } from 'ng-gantt';
import { ActivitiesService } from '../activities/activities.service';
@NgModule({
  imports: [
    CommonModule,
    HttpClientModule,
    PartialsModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule.forChild(),
    JeeWork_CoreModule,
    AngularMultiSelectModule,
    MatDatepickerModule,
    MatExpansionModule,
    DynamicComponentModule,
    EditorModule,
    MatMenuModule,
    ProjectTeamRoutingModule,
    AngularGanttScheduleTimelineCalendarModule,
    ChartsModule,
    AvatarModule,
    PopoverModule,
    MatToolbarModule,
    MatChipsModule,
    NgDragDropModule.forRoot(),
    MatMomentDateModule,
    OwlDateTimeModule,
    NgGanttEditorModule,
  ],
  providers: [
    ProjectsTeamService,
    ListDepartmentService,
    WorkService,
    CommonService,
    JeeWorkLiteService,
    SubheaderService,
  ],
  entryComponents: [
    QuickStatusComponent,
    WorkProcessEditComponent,
  ],
  declarations: [
    ProjectsTeamComponent,
    ProjectListTabComponent,
    WorkKanBanComponent,
    GanttChart2Component,
    ClickOutsideDirective,
    QuickStatusComponent,
    WorkProcessEditComponent,
  ],
  schemas: [
    CUSTOM_ELEMENTS_SCHEMA,
    NO_ERRORS_SCHEMA
  ]
})
export class ProjectsTeamModule { }
