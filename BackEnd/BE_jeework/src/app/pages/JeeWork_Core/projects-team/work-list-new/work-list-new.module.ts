import { WorksDashBoardComponent } from './works-dash-board/works-dash-board.component';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FieldsCustomModule } from './field-custom/fields-custom.module';
import { WorksListGroup2Component } from './works-dash-board/works-list-group-2/works-list-group-2.component';
import { JeeWork_CoreModule } from '../../JeeWork_Core.module';
@NgModule({
	imports: [
		CommonModule,
		FieldsCustomModule,
		JeeWork_CoreModule,
	],
	providers: [
	],
	entryComponents: [

		WorksDashBoardComponent,
		WorksListGroup2Component,
	],
	declarations: [
		WorksDashBoardComponent,
		WorksListGroup2Component,
	],
	exports: [
		WorksDashBoardComponent,
		WorksListGroup2Component,
	]
})
export class WorkListNewModule { }
