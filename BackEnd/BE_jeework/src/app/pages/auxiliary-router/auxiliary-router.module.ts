import {SubheaderService} from './../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import {ProjectsTeamService} from './../JeeWork_Core/projects-team/Services/department-and-project.service';
import {AuxiliaryRouterComponent} from './auxiliary-router.component';
import {AuxiliaryRouterJWComponent} from './auxiliary-router-jw.component';
import {JeeWork_CoreModule} from './../JeeWork_Core/JeeWork_Core.module';
import {NgModule} from '@angular/core';
import {CommonModule } from '@angular/common';
import {RouterModule, Routes} from '@angular/router';
import {FormsModule, ReactiveFormsModule} from '@angular/forms';
import {HttpClientModule} from '@angular/common/http';
import {TranslateModule} from '@ngx-translate/core';
import {AngularMultiSelectModule} from 'angular2-multiselect-dropdown';
import {MatDatepickerModule} from '@angular/material/datepicker';
import {MatExpansionModule} from '@angular/material/expansion';
import {DynamicComponentModule} from 'dps-lib';
const routes: Routes = [
    {
        path: 'task/:id',
        component: AuxiliaryRouterComponent,
    },
    {
        path: 'detail/:id',
        component: AuxiliaryRouterJWComponent,
    }
];

@NgModule({
    imports: [
        CommonModule,
        HttpClientModule,
        RouterModule.forChild(routes),
        FormsModule,
        ReactiveFormsModule,
        TranslateModule.forChild(),
        JeeWork_CoreModule,
        AngularMultiSelectModule,
        MatDatepickerModule,
        MatExpansionModule,
        DynamicComponentModule,
    ],
    providers: [ProjectsTeamService,
        SubheaderService],
    entryComponents: [],
    declarations: [AuxiliaryRouterComponent, AuxiliaryRouterJWComponent]
})
export class AuxiliaryRouterModule {
}
