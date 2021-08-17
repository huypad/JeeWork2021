import { PartialsModule } from "./../../../_metronic/jeework_old/partials/partials.module";
import { AvatarModule } from "ngx-avatar";
import { ReportService } from "./report.service";
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule, Routes } from "@angular/router";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { HttpClientModule } from "@angular/common/http";
import { JeeWork_CoreModule } from "../JeeWork_Core.module";
// import { TagInputModule } from 'ngx-chips';
import { AngularMultiSelectModule } from "angular2-multiselect-dropdown";

import { MatDatepickerModule } from "@angular/material/datepicker";
import {
  MatDialogModule,
  MAT_DIALOG_DEFAULT_OPTIONS,
} from "@angular/material/dialog";
import { MatExpansionModule } from "@angular/material/expansion";

import { DynamicComponentModule } from "dps-lib";
import { ReportComponent } from "./report.component";
import { ReportListTabComponent } from "./report-list-tab/report-list-tab.component";
import { ChartsModule } from "ng2-charts";
import { TranslateModule } from "@ngx-translate/core";
import { ReportTabDashboardComponent } from "./report-tab-dashboard/report-tab-dashboard.component";
import { ReportTabMemberComponent } from "./report-tab-member/report-tab-member.component"; 
const routes: Routes = [
  {
    path: "",
    component: ReportComponent,
    children: [
      {
        path: "",
        component: ReportListTabComponent,
        children: [
          {
            path: "",
            component: ReportTabDashboardComponent,
          },
          {
            path: "member",
            component: ReportTabMemberComponent,
          },
        ],
      },
    ],
  },
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
    AngularMultiSelectModule,
    MatDatepickerModule,
    MatExpansionModule,
    DynamicComponentModule,
    ChartsModule,
    MatDialogModule,
    AvatarModule,
  ],
  providers: [ReportService],
  entryComponents: [
  ],
  declarations: [ReportListTabComponent, ReportTabMemberComponent],
  exports: [],
})
export class ReportModule {}
