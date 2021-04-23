import { PartialsModule } from './../../../_metronic/jeework_old/partials/partials.module';
import { NgModule } from '@angular/core';
import { CommonModule, } from '@angular/common';
import { RouterModule, Routes } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { TranslateModule } from '@ngx-translate/core';
import { WeWorkModule } from '../wework.module';
// import { TagInputModule } from 'ngx-chips';
import { AngularMultiSelectModule } from 'angular2-multiselect-dropdown';

import { MatExpansionModule } from '@angular/material/expansion';
import { MatDatepickerModule } from '@angular/material/datepicker';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { DynamicComponentModule } from 'dps-lib';
// import { JeeHRSubModule } from '../../JeeHRSub.module';
import { UserComponent } from './user.component';
import { ListUserComponent } from './list-user/list-user.component';
import { UserService } from './Services/user.service';
import { UserDetailModule } from './user-detail/user-detail.module';
import { AvatarModule } from "ngx-avatar";

const routes: Routes = [
	{
		path: '',
		component: UserComponent,
		children: [
			{
				path: '',
				component: ListUserComponent,
				children: []
			},
			{
				path: ':idu',
				// loadChildren: () => UserDetailModule
				loadChildren: () => import('./user-detail/user-detail.module').then(m => m.UserDetailModule),
			}]
	}]
@NgModule({
	imports: [
		RouterModule.forChild(routes),
		CommonModule,
		HttpClientModule,
		 PartialsModule,
		FormsModule,
		ReactiveFormsModule,
		TranslateModule.forChild(),
		WeWorkModule,
		// TagInputModule,
		AngularMultiSelectModule,
		MatDatepickerModule,
		MatExpansionModule,
		DynamicComponentModule,
		// JeeHRSubModule,
		MatDatepickerModule,
		AvatarModule
	],
	providers: [
		UserService
	],
	entryComponents: [
	],
	declarations: [
		UserComponent,
		ListUserComponent
	]
})
export class UserModule { }
