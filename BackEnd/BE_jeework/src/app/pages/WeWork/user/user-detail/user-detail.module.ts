import { UserWorkComponent } from './../user-work/user-work.component';
import { UyquyenComponent } from "./../uyquyen/uyquyen.component";
import { SubheaderService } from "./../../../../_metronic/jeework_old/core/_base/layout/services/subheader.service";
import { PartialsModule } from "./../../../../_metronic/jeework_old/partials/partials.module";
import { AvatarModule } from "ngx-avatar";
import { NgModule } from "@angular/core";
import { CommonModule } from "@angular/common";
import { RouterModule, Routes } from "@angular/router";
import { FormsModule, ReactiveFormsModule } from "@angular/forms";
import { HttpClientModule } from "@angular/common/http";
import { TranslateModule } from "@ngx-translate/core";
import { WeWorkModule } from "../../wework.module";
// import { TagInputModule } from 'ngx-chips';
import { AngularMultiSelectModule } from "angular2-multiselect-dropdown";

import { MatExpansionModule } from "@angular/material/expansion";
import { MatDatepickerModule } from "@angular/material/datepicker";
import { MatProgressBarModule } from "@angular/material/progress-bar";
import { DynamicComponentModule } from "dps-lib";
import { EditorModule } from "@tinymce/tinymce-angular";
import { UserDetailComponent } from "./user-detail.component";
import { UserService } from "../Services/user.service";
import { WorkEditPageComponent } from "../../work/work-edit-page/work-edit-page.component";
const routes: Routes = [
  {
    path: "",
    component: UserDetailComponent,
    children: [
      {
        path: "",
        component: UserWorkComponent,
      },
      {
        path: "uyquyen",
        component: UyquyenComponent,
      },
      {
        path: "detail/:id",
        component: WorkEditPageComponent,
      },
    ],
  },
];
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
    EditorModule,
    AvatarModule,
  ],
  providers: [UserService, SubheaderService],
  entryComponents: [],
  declarations: [UyquyenComponent, UserDetailComponent, UserWorkComponent],
})
export class UserDetailModule {}
