
 
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
// Core
// Core => Services


// Products

//import { AlertComponent } from '../_shared/alert/alert.component';

// Material
import { MatChipsModule } from '@angular/material/chips';
import { MatPaginatorModule } from '@angular/material/paginator';
import { MatSortModule } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatFormFieldModule } from '@angular/material/form-field';
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
import { MatTreeModule } from '@angular/material/tree';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { MatDialogModule,MAT_DIALOG_DEFAULT_OPTIONS } from '@angular/material/dialog';
import { MatTabsModule } from '@angular/material/tabs'; 

import { MomentDateAdapter, MAT_MOMENT_DATE_FORMATS } from '@angular/material-moment-adapter';

import { TemplateSoftwareComponent } from './template-software.component';

import { templateSoftwareEditComponent } from './template-software-edit/template-software-edit.component';

import { templateSoftwareService } from './Services/template-software.service';
import {CRUDTableModule} from '../../../_metronic/shared/crud-table';
import {DanhMucChungService} from '../../../_metronic/jeework_old/core/services/danhmuc.service';
import {TokenStorage} from '../../../_metronic/jeework_old/core/services/token-storage.service';
import {templateSoftwareListComponent} from './template-software-list/template-software-list.component';
import {JeeWork_CoreModule} from '../JeeWork_Core.module';
import {PartialsModule} from '../../../_metronic/jeework_old/partials/partials.module';

const routes: Routes = [
	{
		path: '',
		component: TemplateSoftwareComponent,
		children: [
			{
				path: '',
				component: TemplateSoftwareComponent,
			},
		]
	}
];

@NgModule({
	imports: [
		MatDialogModule,
		CommonModule,
		HttpClientModule,
		PartialsModule,
		RouterModule.forChild(routes),
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
		JeeWork_CoreModule,
		CRUDTableModule
	],
	providers: [
		DanhMucChungService,
		templateSoftwareService,
		TokenStorage
	],
	entryComponents: [
		templateSoftwareEditComponent,
	],
	declarations: [
		TemplateSoftwareComponent,
		templateSoftwareListComponent,
		templateSoftwareEditComponent,
	]
})
export class TemplateSoftwareModule { }
