import {SubheaderService} from './../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import {ProjectsTeamService} from './../JeeWork_Core/projects-team/Services/department-and-project.service';
import {AuxiliaryRouterComponent} from './auxiliary-router.component';
import {AuxiliaryRouterJWComponent} from './auxiliary-router-jw.component';
import {JeeWork_CoreModule} from './../JeeWork_Core/JeeWork_Core.module';
import {NgModule} from '@angular/core';
import {CommonModule} from '@angular/common';
import {RouterModule, Routes} from '@angular/router';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';

import {TranslateModule} from '@ngx-translate/core';
import {JeeCommentModule} from '../JeeWork_Core/jee-comment/jee-comment.module';
import {MatFormFieldModule} from '@angular/material/form-field';
import {MAT_DIALOG_DEFAULT_OPTIONS, MatDialogModule} from '@angular/material/dialog';
import {MatIconModule, MatIconRegistry} from '@angular/material/icon';
import {DateAdapter, MAT_DATE_LOCALE, MatNativeDateModule} from '@angular/material/core';
import {MAT_MOMENT_DATE_ADAPTER_OPTIONS, MatMomentDateModule, MomentDateAdapter} from '@angular/material-moment-adapter';

import {HttpUtilsService} from '../../_metronic/jeework_old/core/utils/http-utils.service';
import {TypesUtilsService} from '../../_metronic/jeework_old/core/_base/crud';
import {LayoutUtilsService} from '../../_metronic/jeework_old/core/utils/layout-utils.service';
import { JeeWorkLiteService } from '../JeeWork_Core/services/wework.services';
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
        JeeWork_CoreModule,
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
        JeeWorkLiteService,
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
