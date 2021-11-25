import {SubheaderService} from './../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import {ProjectsTeamService} from './../JeeWork_Core/projects-team/Services/department-and-project.service';
import {AuxiliaryRouterComponent} from './auxiliary-router.component';
import {AuxiliaryRouterJWComponent} from './auxiliary-router-jw.component';
import {JeeWork_CoreModule} from './../JeeWork_Core/JeeWork_Core.module';
import {NgModule} from '@angular/core';
import {CommonModule} from '@angular/common';
import {RouterModule, Routes} from '@angular/router';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import {HttpClientModule} from '@angular/common/http';
import {TranslateModule} from '@ngx-translate/core';
import {AngularMultiSelectModule} from 'angular2-multiselect-dropdown';
import {MatDatepickerModule} from '@angular/material/datepicker';
import {MatExpansionModule} from '@angular/material/expansion';
import {DropdownTreeModule, DynamicComponentModule} from 'dps-lib';
import {JeeCommentModule} from '../JeeWork_Core/jee-comment/jee-comment.module';
import {MatFormFieldModule} from '@angular/material/form-field';
import {MAT_DIALOG_DEFAULT_OPTIONS, MatDialogModule} from '@angular/material/dialog';
import {MatButtonModule} from '@angular/material/button';
import {MatMenuModule} from '@angular/material/menu';
import {MatSelectModule} from '@angular/material/select';
import {MatInputModule} from '@angular/material/input';
import {MatTableModule} from '@angular/material/table';
import {MatAutocompleteModule} from '@angular/material/autocomplete';
import {MatRadioModule} from '@angular/material/radio';
import {MatIconModule, MatIconRegistry} from '@angular/material/icon';
import {DateAdapter, MAT_DATE_LOCALE, MatNativeDateModule} from '@angular/material/core';
import {MatProgressBarModule} from '@angular/material/progress-bar';
import {MatCardModule} from '@angular/material/card';
import {MatPaginatorModule} from '@angular/material/paginator';
import {MatSortModule} from '@angular/material/sort';
import {MatListModule} from '@angular/material/list';
import {MatCheckboxModule} from '@angular/material/checkbox';
import {MatProgressSpinnerModule} from '@angular/material/progress-spinner';
import {MatSnackBarModule} from '@angular/material/snack-bar';
import {MatTabsModule} from '@angular/material/tabs';
import {MatStepperModule} from '@angular/material/stepper';
import {MatTooltipModule} from '@angular/material/tooltip';
import {MAT_MOMENT_DATE_ADAPTER_OPTIONS, MatMomentDateModule, MomentDateAdapter} from '@angular/material-moment-adapter';
import {NgxMatSelectSearchModule} from 'ngx-mat-select-search';
import {MatSidenavModule} from '@angular/material/sidenav';
import {MatSlideToggleModule} from '@angular/material/slide-toggle';
import {DndModule} from 'ngx-drag-drop';
import {MatSliderModule} from '@angular/material/slider';
import {OwlDateTimeModule, OwlNativeDateTimeModule} from 'ng-pick-datetime';
import {DragDropModule} from '@angular/cdk/drag-drop';
import {NgbModule, NgbProgressbarModule} from '@ng-bootstrap/ng-bootstrap';
import {MatTreeModule} from '@angular/material/tree';
import {PerfectScrollbarModule} from 'ngx-perfect-scrollbar';
import {EditorModule} from '@tinymce/tinymce-angular';
import {PopoverModule} from 'ngx-smart-popover';
import {MatChipsModule} from '@angular/material/chips';
import {ChartsModule} from 'ng2-charts';
import {MatToolbarModule} from '@angular/material/toolbar';
import {AvatarModule} from 'ngx-avatar';
import {FullCalendarModule} from '@fullcalendar/angular';
import {CRUDTableModule} from '../../_metronic/shared/crud-table';
import {NgxPrintModule} from 'ngx-print';
import {ApplicationPipesModule} from '../JeeWork_Core/pipe/pipe.module';
import {FieldsCustomModule} from '../JeeWork_Core/projects-team/work-list-new/field-custom/fields-custom.module';
import {HttpUtilsService} from '../../_metronic/jeework_old/core/utils/http-utils.service';
import {TypesUtilsService} from '../../_metronic/jeework_old/core/_base/crud';
import {LayoutUtilsService} from '../../_metronic/jeework_old/core/utils/layout-utils.service';
import {WeWorkService} from '../JeeWork_Core/services/wework.services';
import {WorkService} from '../JeeWork_Core/work/work.service';
import {ListDepartmentService} from '../JeeWork_Core/department/Services/List-department.service';
import {CommentService} from '../JeeWork_Core/comment/comment.service';
import {UpdateByKeyService} from '../JeeWork_Core/update-by-keys/update-by-keys.service';
import {filterService} from '../JeeWork_Core/filter/filter.service';
import {TagsService} from '../JeeWork_Core/tags/tags.service';
import {CommonService} from '../../_metronic/jeework_old/core/services/common.service';
import {TokenStorage} from '../../_metronic/jeework_old/core/auth/_services';
import {AttachmentService} from '../JeeWork_Core/services/attachment.service';
import {TemplateCenterService} from '../JeeWork_Core/template-center/template-center.service';
import {AutomationService} from '../JeeWork_Core/automation/automation.service';
import {MAT_BOTTOM_SHEET_DATA, MatBottomSheetRef} from '@angular/material/bottom-sheet';

const routes: Routes = [
    {
        path: 'task/:id',
        component: AuxiliaryRouterComponent,
        pathMatch: 'full',
    },
    {
        path: 'detail/:id',
        component: AuxiliaryRouterJWComponent,
        pathMatch: 'full',
    }
];

@NgModule({
    imports: [
        RouterModule.forChild(routes),
        JeeCommentModule,
        MatFormFieldModule,
        CommonModule,
        ReactiveFormsModule,
        TranslateModule.forChild(),
        JeeWork_CoreModule
    ],
    providers: [
        {provide: MAT_DATE_LOCALE, useValue: 'vi'},
        {provide: DateAdapter, useClass: MomentDateAdapter, deps: [MAT_DATE_LOCALE]},
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
        {provide: MatBottomSheetRef, useValue: {}},
        {provide: MAT_BOTTOM_SHEET_DATA, useValue: {}},
        {provide: MAT_MOMENT_DATE_ADAPTER_OPTIONS, useValue: {useUtc: true}},
        ProjectsTeamService,
        SubheaderService,
        HttpUtilsService,
        TypesUtilsService,
        LayoutUtilsService,
        WeWorkService,
        WorkService,
        ListDepartmentService,
        CommentService,
        UpdateByKeyService,
        filterService,
        TagsService,
        CommonService,
         TokenStorage,
        AttachmentService,
        TemplateCenterService,
        AutomationService,
    ],
    entryComponents: [],
    declarations: [AuxiliaryRouterComponent, AuxiliaryRouterJWComponent]
})
export class AuxiliaryRouterModule {
}
