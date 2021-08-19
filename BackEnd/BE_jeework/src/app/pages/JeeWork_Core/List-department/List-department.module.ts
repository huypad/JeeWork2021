import { PartialsModule } from './../../../_metronic/jeework_old/partials/partials.module';
import { AvatarModule } from 'ngx-avatar';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';

import { ListDepartmentComponent } from './List-department.component';
import { ListDepartmentService } from './Services/List-department.service';
import { JeeWork_CoreModule } from '../JeeWork_Core.module';
import { ListDepartmentListComponent } from './List-department-list/List-department-list.component';
const routes: Routes = [
	{
		path: '',
		component: ListDepartmentComponent,
		children: [
			{
				path: '',
				component: ListDepartmentListComponent,
			},
			{
				path: ':id',
				// loadChildren: () => DepartmentTabModule,
				loadChildren: () => import('./List-department-tab/List-department-tab.module').then(m => m.DepartmentTabModule),
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
		AvatarModule
	],
	providers: [
		ListDepartmentService
	],
	entryComponents: [
	],
	declarations: [
		ListDepartmentComponent,
		ListDepartmentListComponent, 
	]
})
export class DepartmentModule { }
