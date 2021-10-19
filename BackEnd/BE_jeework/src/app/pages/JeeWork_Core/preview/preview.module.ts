import { PartialsModule } from '../../../_metronic/jeework_old/partials/partials.module';
import { NgModule } from '@angular/core';
import { RouterModule, Routes } from '@angular/router';
import { WeWorkService } from '../services/wework.services';
import { DatePipe, CommonModule } from '@angular/common';
import { JeeWork_CoreModule } from '../JeeWork_Core.module';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { TranslateModule } from '@ngx-translate/core';
import { PreviewComponent } from './preview.component';
const routes: Routes = [
	{
		path: '',
		component: PreviewComponent,
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
		JeeWork_CoreModule
	],
	providers: [
		WeWorkService,
		DatePipe,
	],
	entryComponents: [

	],
	declarations: [
	],
	exports: [
	]
})
export class PreviewModule { }
