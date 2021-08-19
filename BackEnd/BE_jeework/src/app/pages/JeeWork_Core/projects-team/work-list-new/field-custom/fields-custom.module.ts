import { PrioritizeComponent } from './prioritize/prioritize.component';
import { TaskCommentComponent } from './task-comment/task-comment.component';
import { UserGroupComponent } from './user-group/user-group.component';
import { PartialsModule } from '../../../../../_metronic/jeework_old/partials/partials.module';
import { MatMenuModule } from '@angular/material/menu';
import { OwlDateTimeModule } from "ng-pick-datetime";
import { PopoverModule } from "ngx-smart-popover";
import { AvatarModule } from "ngx-avatar";
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { HttpClientModule } from "@angular/common/http";
import { TranslateModule } from "@ngx-translate/core";
// import { TagInputModule } from 'ngx-chips';
import { AngularMultiSelectModule } from "angular2-multiselect-dropdown";

import { MatToolbarModule } from "@angular/material/toolbar";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatExpansionModule } from "@angular/material/expansion";
// import { WorkStreamComponent } from "./work-stream/work-stream.component";
import {
  AngularGanttScheduleTimelineCalendarModule,
  GSTCComponent,
} from "angular-gantt-schedule-timeline-calendar";
import { DynamicComponentModule } from "dps-lib";
import { EditorModule } from "@tinymce/tinymce-angular";
import { ChartsModule } from "ng2-charts";
import { MatChipsModule } from "@angular/material/chips";
import { NgDragDropModule } from "ng-drag-drop";
import { MatMomentDateModule } from "@angular/material-moment-adapter";
import { NO_ERRORS_SCHEMA, CUSTOM_ELEMENTS_SCHEMA } from '@angular/core';
import { NgGanttEditorModule } from 'ng-gantt';
import { JeeWork_CoreModule } from '../../../JeeWork_Core.module';
@NgModule({
  imports: [
    CommonModule,
    HttpClientModule,
    PartialsModule,
    FormsModule,
    ReactiveFormsModule,
    TranslateModule.forChild(),
    //JeeWork_CoreModule,
    AngularMultiSelectModule,
    MatDatepickerModule,
    MatExpansionModule,
    DynamicComponentModule,
    EditorModule,
    MatMenuModule,
    AngularGanttScheduleTimelineCalendarModule,
    ChartsModule,
    AvatarModule,
    PopoverModule,
    MatToolbarModule,
    MatChipsModule,
  ],
  providers: [ 
  ],
  entryComponents: [ 
    UserGroupComponent,
    TaskCommentComponent,
    PrioritizeComponent
  ],
  declarations: [ 
    UserGroupComponent,
    TaskCommentComponent,
    PrioritizeComponent
  ],
  exports: [
    UserGroupComponent,
    TaskCommentComponent,
    PrioritizeComponent
  ],
  schemas: [
    CUSTOM_ELEMENTS_SCHEMA,
    NO_ERRORS_SCHEMA
  ]
})
export class FieldsCustomModule { }
