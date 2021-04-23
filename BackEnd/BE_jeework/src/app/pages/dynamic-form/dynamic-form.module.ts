import { DanhMucChungService } from './../../_metronic/jeework_old/core/services/danhmuc.service';
import { LayoutUtilsService } from './../../_metronic/jeework_old/core/utils/layout-utils.service';
import { HttpUtilsService } from './../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { TypesUtilsService } from './../../_metronic/jeework_old/core/_base/crud/utils/types-utils.service';
import { PartialsModule } from './../../_metronic/jeework_old/partials/partials.module';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
// Core
// Core => Utils

import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatInputModule } from '@angular/material/input';
import { MatTooltipModule } from '@angular/material/tooltip';
import { MatButtonModule } from '@angular/material/button';
import { MatMenuModule, MatMenuTrigger } from '@angular/material/menu';
import { MatSelectModule } from '@angular/material/select';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatRadioModule } from '@angular/material/radio';
import { MatIconModule } from '@angular/material/icon';
import { MatNativeDateModule } from '@angular/material/core';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBarModule } from '@angular/material/snack-bar';
import { MatDialogModule,MAT_DIALOG_DEFAULT_OPTIONS } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs';
import { MatSlideToggleModule } from '@angular/material/slide-toggle';
import { MAT_DATE_LOCALE,MAT_DATE_FORMATS,DateAdapter } from '@angular/material/core';

import { MomentDateAdapter, MAT_MOMENT_DATE_FORMATS } from '@angular/material-moment-adapter';
import { DynamicFormComponent, DynamicFormMoveComponent, DynamicFormCreateComponent, DynamicFormCopyComponent} from './dynamic-form.component'
import { DynamicFormService } from './dynamic-form.service'
import { DropdownTreeModule } from 'dps-lib';
import { ImageControlModule } from 'dps-lib';
import { EditorModule } from '@tinymce/tinymce-angular'; 
import { NgxMatSelectSearchModule } from 'ngx-mat-select-search';

@NgModule({
    imports: [
        MatDialogModule,
        CommonModule,
        HttpClientModule,
        PartialsModule,
        FormsModule,
        ReactiveFormsModule,
        TranslateModule.forChild(),
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
        MatTooltipModule,
        DropdownTreeModule,
        MatSlideToggleModule,
        ImageControlModule,
        EditorModule,
        NgxMatSelectSearchModule,
    ],
    providers: [
        { provide: MAT_DATE_LOCALE, useValue: 'vi' },
        { provide: DateAdapter, useClass: MomentDateAdapter, deps: [MAT_DATE_LOCALE] },
        { provide: MAT_DATE_FORMATS, useValue: MAT_MOMENT_DATE_FORMATS },
        {
            provide: MAT_DIALOG_DEFAULT_OPTIONS,
            useValue: {
                hasBackdrop: true,
                panelClass: 'm-mat-dialog-container__wrapper',
                height: 'auto',
                width: '900px'
            }
        },
        HttpUtilsService,
        TypesUtilsService,
        LayoutUtilsService,
        DynamicFormService,
        DanhMucChungService,
    ],
    entryComponents: [
        DynamicFormComponent,
        DynamicFormMoveComponent,
        DynamicFormCreateComponent,
        DynamicFormCopyComponent,
    ],
    declarations: [
        DynamicFormComponent,
        DynamicFormMoveComponent,
        DynamicFormCreateComponent,
        DynamicFormCopyComponent,
    ],
    exports: [
        DynamicFormComponent,
        DynamicFormMoveComponent,
        DynamicFormCreateComponent,
        DynamicFormCopyComponent,
    ]
})
export class DynamicFormModule { }
