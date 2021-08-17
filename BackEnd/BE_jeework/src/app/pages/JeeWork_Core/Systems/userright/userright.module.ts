import { TokenStorage } from './../../../../_metronic/jeework_old/core/services/token-storage.service';
import { CRUDTableModule } from './../../../../_metronic/shared/crud-table/crud-table.module';
import { PartialsModule } from './../../../../_metronic/jeework_old/partials/partials.module';
 
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
// Core
// Core => Services

import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';// Core => Utils

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

import { UserRightComponent } from './userright.component';
import { PermissionService } from './Services/userright.service';
import { GroupNameListComponent } from './groupname-list/groupname-list.component';
import { UsersListComponent } from './users-list/users-list.component';
import { DanhSachNguoiDungComponent } from './danh-sach-nguoi-dung/danh-sach-nguoi-dung.component';
import { GroupNameEditComponent } from './groupname-edit/groupname-edit.component';
import { DanhSachNguoiDungThemMoiComponent } from './danh-sach-nguoi-dung-them-moi/danh-sach-nguoi-dung-them-moi.component';
import { JeeWork_CoreModule } from '../../JeeWork_Core.module';
import { FunctionsGroupListComponent } from './functions-group/functions-group-list.component';
import { ChucNangUserListComponent } from './chucnanguser-list/chucnanguser-list.component';

const routes: Routes = [
	{
		path: '',
		component: UserRightComponent,
		children: [
			{
				path: '',
				component: UserRightComponent,
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
		// {
		// 	provide: MAT_DIALOG_DEFAULT_OPTIONS,
		// 	useValue: {
		// 		hasBackdrop: true,
		// 		panelClass: 'kt-mat-dialog-container__wrapper',
		// 		height: 'auto',
		// 		width: '900px',
		// 		disableClose: true,
		// 	}
		// },
		// HttpUtilsService,
		// CustomersService,
		// OrdersService,
		DanhMucChungService,
		// TypesUtilsService,
		// LayoutUtilsService,
		PermissionService,
		TokenStorage
	],
	entryComponents: [
		FunctionsGroupListComponent,
		DanhSachNguoiDungComponent,
		GroupNameEditComponent,
		DanhSachNguoiDungThemMoiComponent,
		ChucNangUserListComponent
	],
	declarations: [
		UserRightComponent,
		GroupNameListComponent,
		GroupNameEditComponent,
		UsersListComponent,
		FunctionsGroupListComponent,
		DanhSachNguoiDungComponent,
		DanhSachNguoiDungThemMoiComponent,
		ChucNangUserListComponent
	]
})
export class UserRightModule { }
