import { CRUDTableModule } from './../../_metronic/shared/crud-table/crud-table.module';
import { DynamicFormService } from './../dynamic-form/dynamic-form.service';
import { ActionNotificationComponent } from './../../_metronic/jeework_old/_shared/action-natification/action-notification.component';
import { NgxDocViewerModule } from 'ngx-doc-viewer';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { PartialsModule } from './../../_metronic/jeework_old/partials/partials.module';
import { AuthService } from './../../modules/auth/_services/auth.service';
import { TokenStorage } from './../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { CommonService } from './../../_metronic/jeework_old/core/services/common.service';
import { LayoutUtilsService } from './../../_metronic/jeework_old/core/utils/layout-utils.service';
import { TypesUtilsService } from './../../_metronic/jeework_old/core/_base/crud/utils/types-utils.service';
import { DanhMucChungService } from './../../_metronic/jeework_old/core/services/danhmuc.service';
import { HttpUtilsService } from './../../_metronic/jeework_old/core/utils/http-utils.service';
import { DeleteEntityDialogComponent } from './../../_metronic/jeework_old/_shared/delete-entity-dialog/delete-entity-dialog.component';
import { MatRadioModule } from '@angular/material/radio';
import { MAT_DATE_FORMATS,DateAdapter } from '@angular/material/core';

//
import { ProjectTeamEditStatusComponent } from './projects-team/project-team-edit-status/project-team-edit-status.component';
import { DepartmentEditNewComponent } from './List-department/department-edit-new/department-edit-new.component';
import { AppFocusBlockComponent } from './projects-team/work-list-new/app-focus-block/app-focus-block.component';
import { AddNewFieldsComponent } from './projects-team/work-list-new/add-new-fields/add-new-fields.component';
import { ReportByProjectComponent } from './report/report-by-project/report-by-project.component';
// import { CheckListEditComponent } from './work/check-list-edit/check-list-edit.component';
import { ListTaskCUComponent } from './projects-team/work-list-new/list-task-cu/list-task-cu.component';
import { ViewCommentComponent } from './projects-team/work-list-new/view-comment/view-comment.component';
import { WorkListNewDetailComponent } from './projects-team/work-list-new/work-list-new-detail/work-list-new-detail.component';
import { DuplicateProjectComponent } from './projects-team/duplicate-project/duplicate-project.component';
import { ClosedProjectComponent } from './projects-team/closed-project/closed-project.component';
import { AddStatusComponent } from './projects-team/work-list-new/add-status/add-status.component';
import { DuplicateTaskNewComponent } from './projects-team/work-list-new/duplicate-task-new/duplicate-task-new.component';
import { WorkListNewComponent } from './projects-team/work-list-new/work-list-new.component';
import { AddTaskComponent } from './projects-team/work-list-new/add-task/add-task.component';
import { RepeatedListComponent } from './work/repeated-list/repeated-list.component';
import { Department_WorkListComponent } from './projects-team/department-work-list/department-work-list.component';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';

import { MatMomentDateModule, MAT_MOMENT_DATE_FORMATS, MomentDateAdapter } from '@angular/material-moment-adapter';
// import { MY_FORMATS_EDIT } from '../datepicker';
import { NgxMatSelectSearchModule } from 'ngx-mat-select-search';
import { DiagramModule } from '@syncfusion/ej2-angular-diagrams';
import { DndModule } from 'ngx-drag-drop';
import { DragDropModule } from '@angular/cdk/drag-drop';
import { OwlNativeDateTimeModule, OwlDateTimeModule } from 'ng-pick-datetime';
import { PerfectScrollbarModule } from 'ngx-perfect-scrollbar';
import { NgbModule, NgbProgressbarModule } from '@ng-bootstrap/ng-bootstrap';
import { DropdownTreeModule, DynamicComponentModule, ImageControlModule } from 'dps-lib';
import { WeWorkService } from './services/wework.services';
// import { TagInputModule } from 'ngx-chips';
import { AngularMultiSelectModule } from 'angular2-multiselect-dropdown';

import { DepartmentEditComponent } from './List-department/List-department-edit/List-department-edit.component';
import { WorkItemComponent } from './work/work-item/work-item.component';
import { StreamViewComponent } from './work/stream-view/stream-view.component';
import { WorkService } from './work/work.service';
import { ListViewComponent } from './work/list-view/list-view.component';
import { WorkEditComponent } from './work/work-edit/work-edit.component';
import { EditorModule } from '@tinymce/tinymce-angular';
import { ListDepartmentService } from './List-department/Services/List-department.service';
import { DepartmentProjectTabComponent } from './projects-team/department-project-tab/department-project-tab.component';

import { WorkEditDialogComponent } from './work/work-edit-dialog/work-edit-dialog.component';
import { CommentService } from './comment/comment.service';
import { CommentComponent } from './comment/comment.component';
import { EmotionDialogComponent } from './emotion-dialog/emotion-dialog.component';
import { PopoverModule } from 'ngx-smart-popover';
import { CommentEditDialogComponent } from './comment/comment-edit-dialog/comment-edit-dialog.component';
import { UpdateByKeysComponent } from './update-by-keys/update-by-keys-edit/update-by-keys-edit.component';
import { UpdateByKeyService } from './update-by-keys/update-by-keys.service';
import { ChooseUsersComponent } from './choose-users/choose-users.component';
import { ChooseTimeComponent } from './choose-time/choose-time.component';
import { ChooseMilestoneAndTagComponent } from './choose-milestone-and-tags/choose-milestone-and-tags.component';
import { milestoneDetailEditComponent } from './List-department/milestone-detail-edit/milestone-detail-edit.component';
import { filterEditComponent } from './filter/filter-edit/filter-edit.component';
import { filterService } from './filter/filter.service';
import { MyStaffComponent } from './mystaff/mystaff.component';
import { WorkEditPageComponent } from './work/work-edit-page/work-edit-page.component';
import { WorkDetailComponent } from './work/work-detail/work-detail.component';
import { WorkDetailTabRightComponent } from './work/work-detail-tab-right/work-detail-tab-right.component';
import { ColorPickerComponent } from './color-picker/color-picker.component';
import { TagsEditComponent } from './tags/tags-edit/tags-edit.component';
import { TagsService } from './tags/tags.service';
import { RepeatedEditComponent } from './work/repeated-edit/repeated-edit.component';
import { ReportComponent } from './report/report.component';
import { UpdateStatusProjectComponent } from './projects-team/update-status-project/update-status-project.component';
import { TopicEditComponent } from './discussions/topic-edit/topic-edit.component';
import { DuplicateWorkComponent } from './work/work-duplicate/work-duplicate.component';
import { workAddFollowersComponent } from './work/work-add-followers/work-add-followers.component';
import { CustomUserComponent } from './report/custom-user/custom-user.component';

import { PeriodViewComponent } from './work/period-view/period-view.component';
import { AngularGanttScheduleTimelineCalendarModule } from 'angular-gantt-schedule-timeline-calendar';
import { WorkGroupEditComponent } from './work/work-group-edit/work-group-edit.component';
import { AuthorizeEditComponent } from './user/authorize-edit/authorize-edit.component';
import { DialogSelectdayComponent } from './report/dialog-selectday/dialog-selectday.component';
import { WorkAssignedComponent } from './work/work-assigned/work-assigned.component';
import { ProjectTeamEditComponent } from './projects-team/project-team-edit/project-team-edit.component';
import { ProjectsTeamService } from './projects-team/Services/department-and-project.service';
import { ReportTabDashboardComponent } from './report/report-tab-dashboard/report-tab-dashboard.component';
import { ItemGroupComponent } from './report/item-group/item-group.component';
import { CrossbarChartComponent } from './report/crossbar-chart/crossbar-chart.component';
import { ChartsModule } from 'ng2-charts';
import { AttachmentService } from './services/attachment.service';
import { AvatarModule } from "ngx-avatar";
import { CommentNewComponent } from './projects-team/work-list-new/comment-new/comment-new.component';
import {MatStepperModule} from '@angular/material/stepper';
import { ColorPicker2Component } from './color-picker2/color-picker2.component';
import { AvatarUserComponent } from './custom-avatar/avatar-user/avatar-user.component';
import { AvatarListUsersComponent } from './custom-avatar/avatar-list-users/avatar-list-users.component';
import { StatusDynamicDialogComponent } from './status-dynamic/status-dynamic-dialog/status-dynamic-dialog.component';
import { InputCustomComponent } from './custom-selector/input-custom/input-custom.component';
import { CheckListEditComponent } from './work/check-list-edit/check-list-edit.component';
import { ChangeReivewerComponent } from './work/change-reivewer/change-reivewer.component';
import { CuTagComponent } from './choose-milestone-and-tags/tags-new/cu-tag/cu-tag.component';
import { FullCalendarModule } from '@fullcalendar/angular';
// import { ChangeReivewerComponent } from './work/change-reivewer/change-reivewer.component';
// import { TagCloudModule } from 'angular-tag-cloud-module';

// Material module
import { CoreModule } from '../../_metronic/core';
import { GeneralModule } from '../../_metronic/partials/content/general/general.module';
import {
  MatRippleModule,
  MatNativeDateModule,
  MAT_DATE_LOCALE,
} from '@angular/material/core';
import { MatDialogModule, MAT_DIALOG_DEFAULT_OPTIONS } from '@angular/material/dialog';
import { MatIconRegistry, MatIconModule } from '@angular/material/icon';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatDatepickerModule } from '@angular/material/datepicker';



// Data table

import { MAT_MOMENT_DATE_ADAPTER_OPTIONS } from '@angular/material-moment-adapter';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatButtonModule } from '@angular/material/button';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatSelectModule } from '@angular/material/select';
import { MatCardModule } from '@angular/material/card';
import { MatMenuModule } from '@angular/material/menu';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatTableModule } from '@angular/material/table';
import { MatListModule } from '@angular/material/list';
import { MatSidenavModule } from '@angular/material/sidenav';
import { MatSliderModule } from '@angular/material/slider';
import { MatGridListModule } from '@angular/material/grid-list';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatTreeModule } from '@angular/material/tree';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatChipsModule } from '@angular/material/chips';
import { MatSortModule } from '@angular/material/sort';
import { MatToolbarModule } from '@angular/material/toolbar';
import {
  MatBottomSheetModule,
  MatBottomSheetRef,
  MAT_BOTTOM_SHEET_DATA,
} from '@angular/material/bottom-sheet';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatDividerModule } from '@angular/material/divider';
@NgModule({
	imports: [
		MatFormFieldModule,
		CommonModule,
		HttpClientModule,
		PartialsModule,
		FormsModule,
		RouterModule,
		ReactiveFormsModule,
		TranslateModule.forChild(),
		MatDialogModule,
		MatButtonModule,
		MatMenuModule,
		MatSelectModule,
		MatInputModule,
		MatTableModule,
		MatAutocompleteModule,
		MatRadioModule,
		MatIconModule,
		MatNativeDateModule,
		MatProgressBarModule,
		MatDatepickerModule,
		MatCardModule,
		MatPaginatorModule,
		MatSortModule,
		MatListModule,
		MatCheckboxModule,
		MatProgressSpinnerModule,
		MatSnackBarModule,
		MatTabsModule,
		MatStepperModule,
		MatTooltipModule,
		MatMomentDateModule,
		NgxMatSelectSearchModule,
		MatSidenavModule,
		DiagramModule,
		MatSlideToggleModule,
		MatSidenavModule,
		DndModule,
		// DynamicSearchFormModule,
		// CommentFormModule,
		MatSliderModule,
		OwlDateTimeModule,
		OwlNativeDateTimeModule,
		DragDropModule,
		// DynamicFormModule,
		NgbModule,
		NgbProgressbarModule,
		MatProgressBarModule,
		DropdownTreeModule,
		MatTreeModule,
		MatSnackBarModule,
		MatTabsModule,
		MatTooltipModule,
		MatMomentDateModule,
		NgxMatSelectSearchModule,
		MatExpansionModule,
		DynamicComponentModule,
		PerfectScrollbarModule,
		EditorModule,
		PopoverModule,
		MatChipsModule,
		ChartsModule,
		MatToolbarModule,
		AvatarModule,
		FullCalendarModule,
		CRUDTableModule,
		// NgxDocViewerModule 
		// TagCloudModule
	],
	// { provide: DateAdapter, useClass: MomentDateAdapter, deps: [MAT_DATE_LOCALE] },
	// 	{ provide: MAT_DATE_FORMATS, useValue: MY_FORMATS_EDIT },
	providers: [
		{ provide: MAT_DATE_LOCALE, useValue: 'vi' },
		{ provide: DateAdapter, useClass: MomentDateAdapter, deps: [MAT_DATE_LOCALE] },
		// { provide: MAT_DATE_FORMATS, useValue: MY_FORMATS_EDIT },
		// { provide: MatPaginatorIntl, useValue: CustomPaginator() },
		{
			provide: MAT_DIALOG_DEFAULT_OPTIONS,
			useValue: {
				hasBackdrop: true,
				panelClass: 'kt-mat-dialog-container__wrapper',
				height: 'auto',
				width: '700px'
			}
		},
		MatIconRegistry,
		{ provide: MatBottomSheetRef, useValue: {} },
		{ provide: MAT_BOTTOM_SHEET_DATA, useValue: {} },
		{ provide: MAT_MOMENT_DATE_ADAPTER_OPTIONS, useValue: { useUtc: true } },
		HttpUtilsService,
		DanhMucChungService,
		TypesUtilsService,
		LayoutUtilsService,
		WeWorkService,
		WorkService,
		ListDepartmentService,
		ProjectsTeamService,
		CommentService,
		UpdateByKeyService,
		filterService,
		TagsService,
		AngularGanttScheduleTimelineCalendarModule,
		CommonService,
		AuthService,
		TokenStorage,
		AttachmentService,
		DynamicFormService
	],
	entryComponents: [
		// DeleteEntityDialogComponent,
		// FetchEntityDialogComponent,
		// UpdateStatusDialogComponent,
		DepartmentEditComponent,
		DepartmentEditNewComponent,
		ProjectTeamEditComponent,
		WorkEditComponent,
		WorkDetailComponent,
		WorkEditPageComponent,
		WorkItemComponent,
		StreamViewComponent,
		ListViewComponent,
		PeriodViewComponent,
		DepartmentProjectTabComponent,
		WorkEditDialogComponent,
		CommentComponent,CommentNewComponent,
		EmotionDialogComponent,
		CommentEditDialogComponent,
		milestoneDetailEditComponent,
		filterEditComponent,
		MyStaffComponent,
		WorkDetailTabRightComponent,
		ColorPickerComponent,
		TagsEditComponent,
		RepeatedEditComponent,
		UpdateStatusProjectComponent,
		TopicEditComponent,
		DuplicateWorkComponent,
		workAddFollowersComponent,
		WorkGroupEditComponent,
		AuthorizeEditComponent,
		ActionNotificationComponent,
		DialogSelectdayComponent,
		WorkAssignedComponent,
		ReportTabDashboardComponent,
		ReportByProjectComponent,
		ItemGroupComponent, CrossbarChartComponent,
		Department_WorkListComponent,
		RepeatedListComponent,
		WorkListNewComponent,
		DuplicateTaskNewComponent,
		AddStatusComponent,
		AddTaskComponent,ColorPicker2Component,
		ClosedProjectComponent,
		DuplicateProjectComponent,
		DeleteEntityDialogComponent,
		WorkListNewDetailComponent,
		AvatarUserComponent,
		AvatarListUsersComponent,
		ListTaskCUComponent,
		StatusDynamicDialogComponent,
		// ChangeReivewerComponent
		CheckListEditComponent,
		ChangeReivewerComponent,
		CuTagComponent,
		AddNewFieldsComponent,
		AppFocusBlockComponent,
		ProjectTeamEditStatusComponent,
	],
	declarations: [
		RepeatedListComponent,
		DepartmentEditComponent,
		DepartmentEditNewComponent,
		ProjectTeamEditComponent,
		WorkEditComponent,
		WorkDetailComponent,
		WorkEditPageComponent,
		WorkItemComponent,
		StreamViewComponent,
		ListViewComponent,
		PeriodViewComponent,
		DepartmentProjectTabComponent,
		WorkEditDialogComponent,
		CommentComponent,CommentNewComponent,
		EmotionDialogComponent,
		CommentEditDialogComponent,
		ChooseUsersComponent,
		CustomUserComponent,
		ChooseTimeComponent,
		ChooseMilestoneAndTagComponent,
		UpdateByKeysComponent,
		milestoneDetailEditComponent,
		filterEditComponent,
		MyStaffComponent,
		WorkDetailTabRightComponent,
		ColorPickerComponent,
		TagsEditComponent,
		RepeatedEditComponent,
		ReportComponent,
		UpdateStatusProjectComponent,
		TopicEditComponent,
		DuplicateWorkComponent,
		workAddFollowersComponent,
		WorkGroupEditComponent,
		ActionNotificationComponent,
		AuthorizeEditComponent,
		DialogSelectdayComponent,
		WorkAssignedComponent,
		ReportTabDashboardComponent,
		ReportByProjectComponent,
		ItemGroupComponent, CrossbarChartComponent,
		Department_WorkListComponent,
		WorkListNewComponent,
		AddTaskComponent,
		AddStatusComponent,
		DuplicateTaskNewComponent,
		ColorPicker2Component,
		ClosedProjectComponent,
		DuplicateProjectComponent,
		DeleteEntityDialogComponent,
		WorkListNewDetailComponent,
		ViewCommentComponent,
		AvatarUserComponent,
		AvatarListUsersComponent,
		ListTaskCUComponent,
		StatusDynamicDialogComponent,
		InputCustomComponent,
		CheckListEditComponent,
		ChangeReivewerComponent,
		CuTagComponent,
		AddNewFieldsComponent,
		AppFocusBlockComponent,
		ProjectTeamEditStatusComponent,
		// ChangeReivewerComponent

	],
	exports: [
		CRUDTableModule,
		MatDialogModule,
		MatButtonModule,
		MatMenuModule,
		MatSelectModule,
		MatInputModule,
		MatTableModule,
		MatAutocompleteModule,
		MatRadioModule,
		MatIconModule,
		MatNativeDateModule,
		MatProgressBarModule,
		MatDatepickerModule,
		MatCardModule,
		MatPaginatorModule,
		MatSortModule,
		MatCheckboxModule,
		MatProgressSpinnerModule,
		MatSnackBarModule,
		MatTabsModule,
		MatListModule,
		MatTooltipModule,
		MatMomentDateModule,
		NgxMatSelectSearchModule,
		DiagramModule,
		MatSlideToggleModule,
		MatSidenavModule,
		DndModule,
		// DynamicSearchFormModule,
		// CommentFormModule,
		MatSliderModule,
		OwlDateTimeModule,
		OwlNativeDateTimeModule,
		DragDropModule,
		// DynamicFormModule,
		NgbModule,
		NgbProgressbarModule,
		MatProgressBarModule,
		DropdownTreeModule,
		MatTreeModule,
		PerfectScrollbarModule,
		// TagInputModule,
		AngularMultiSelectModule,
		MatExpansionModule,
		ImageControlModule,
		DepartmentEditComponent,
		DepartmentEditNewComponent,
		ProjectTeamEditComponent,
		WorkEditComponent,
		WorkDetailComponent,
		WorkEditPageComponent,
		WorkItemComponent,
		StreamViewComponent,
		ListViewComponent,
		PeriodViewComponent,
		DepartmentProjectTabComponent,
		WorkEditDialogComponent,
		CommentComponent,CommentNewComponent,
		EmotionDialogComponent,
		CommentEditDialogComponent,
		ChooseUsersComponent,
		CustomUserComponent,
		ChooseTimeComponent,
		ChooseMilestoneAndTagComponent,
		UpdateByKeysComponent,
		milestoneDetailEditComponent,
		filterEditComponent,
		MyStaffComponent,
		WorkDetailTabRightComponent,
		ColorPickerComponent,
		TagsEditComponent,
		RepeatedEditComponent,
		UpdateStatusProjectComponent,
		TopicEditComponent,
		DuplicateWorkComponent,
		workAddFollowersComponent,
		WorkGroupEditComponent,
		ActionNotificationComponent,
		AuthorizeEditComponent,
		DialogSelectdayComponent,
		WorkAssignedComponent,
		ReportTabDashboardComponent,
		ReportByProjectComponent,
		ItemGroupComponent, CrossbarChartComponent,
		Department_WorkListComponent,
		RepeatedListComponent,
		WorkListNewComponent,
		AddTaskComponent,
		AddStatusComponent,
		DuplicateTaskNewComponent,ColorPicker2Component,
		ClosedProjectComponent,
		DuplicateProjectComponent,
		DeleteEntityDialogComponent,
		WorkListNewDetailComponent,
		AvatarUserComponent,
		AvatarListUsersComponent,
		ListTaskCUComponent,
		StatusDynamicDialogComponent,
		// ChangeReivewerComponent
		CheckListEditComponent,
		ChangeReivewerComponent,
		CuTagComponent,
		AddNewFieldsComponent,
		AppFocusBlockComponent,
		ProjectTeamEditStatusComponent,

	]
})
export class WeWorkModule { }
