import { MatIconModule } from "@angular/material/icon";
import { MatMenuModule } from "@angular/material/menu";
import { AuthService } from "./modules/auth/_services/auth.service";
import { SplashScreenService } from "./_metronic/partials/layout/splash-screen/splash-screen.service";
import { DataTableService } from "./_metronic/jeework_old/core/_base/layout/services/datatable.service";
import { KtDialogService } from "./_metronic/jeework_old/core/_base/layout/services/kt-dialog.service";
import { PageConfigService } from "./_metronic/jeework_old/core/_base/layout/services/page-config.service";
import { LayoutRefService } from "./_metronic/jeework_old/core/_base/layout/services/layout-ref.service";
import { LayoutConfigService } from "./_metronic/jeework_old/core/_base/layout/services/layout-config.service";
import { UserProfileService } from "./_metronic/jeework_old/core/auth/_services/user-profile.service";
import { TokenStorage } from "./_metronic/jeework_old/core/auth/_services/token-storage.service";
import { AuthenticationService } from "./_metronic/jeework_old/core/auth/_services/auth.service";
import { AclService } from "./_metronic/jeework_old/core/auth/_services/acl.service";
import { LayoutUtilsService } from "./_metronic/jeework_old/core/utils/layout-utils.service";
import { CommonModule, DatePipe } from "@angular/common";
import { PermissionUrl } from "./_metronic/jeework_old/core/services/permissionurl";
import { TypesUtilsService } from "./_metronic/jeework_old/core/_base/crud/utils/types-utils.service";
import { MenuHorizontalService } from "./_metronic/jeework_old/core/_base/layout/services/menu-horizontal.service";
import { SubheaderService } from "./_metronic/partials/layout/subheader/_services/subheader.service";
import { MenuPhanQuyenServices } from "./_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service";
import { MenuConfigService } from "./_metronic/jeework_old/core/_base/layout/services/menu-config.service";
import { MenuAsideService } from "./_metronic/jeework_old/core/_base/layout/services/menu-aside.service";
import { HttpUtilsService } from "./_metronic/jeework_old/core/_base/crud/utils/http-utils.service";
import { ListDepartmentService } from "./pages/WeWork/List-department/Services/List-department.service";
import { WeWorkModule } from "./pages/WeWork/wework.module";
import { NgModule, APP_INITIALIZER } from "@angular/core";
import { BrowserAnimationsModule } from "@angular/platform-browser/animations";
import { HttpClientModule } from "@angular/common/http";
import { HttpClientInMemoryWebApiModule } from "angular-in-memory-web-api";
import { ClipboardModule } from "ngx-clipboard";
import { TranslateModule } from "@ngx-translate/core";
import { InlineSVGModule } from "ng-inline-svg";
import { NgbModule } from "@ng-bootstrap/ng-bootstrap";
import { AppRoutingModule } from "./app-routing.module";
import { AppComponent } from "./app.component";
import { environment } from "src/environments/environment";
import { MatTooltipModule } from "@angular/material/tooltip";
import {
  MatMomentDateModule,
  MAT_MOMENT_DATE_FORMATS,
  MomentDateAdapter,
  MAT_MOMENT_DATE_ADAPTER_OPTIONS,
} from "@angular/material-moment-adapter";

// Highlight JS
import { HighlightModule, HIGHLIGHT_OPTIONS } from "ngx-highlightjs";
import { SplashScreenModule } from "./_metronic/partials/layout/splash-screen/splash-screen.module";
// #fake-start#
import { FakeAPIService } from "./_fake/fake-api.service";
import { CommonService } from "./_metronic/jeework_old/core/services/common.service";
import { MAT_DATE_LOCALE } from "@angular/material/core";
// #fake-end#

function appInitializer(authService: AuthService) {
  return () => {
    return new Promise((resolve) => {
      authService.getUserByToken().subscribe().add(resolve);
    });
  };
}

@NgModule({
  declarations: [AppComponent],
  imports: [
    CommonModule,
    BrowserAnimationsModule,
    SplashScreenModule,
    TranslateModule.forRoot(),
    HttpClientModule,
    HighlightModule,
    ClipboardModule,
    // #fake-start#
    environment.isMockEnabled
      ? HttpClientInMemoryWebApiModule.forRoot(FakeAPIService, {
          passThruUnknownUrl: true,
          dataEncapsulation: false,
        })
      : [],
    // #fake-end#
    AppRoutingModule,
    InlineSVGModule.forRoot(),
    NgbModule,
    // WeWorkModule,
    MatTooltipModule,
    MatMenuModule,
    MatIconModule,
    MatMomentDateModule,
  ],
  providers: [
    AclService,
    AuthenticationService,
    TokenStorage,
    UserProfileService,
    LayoutConfigService,
    LayoutRefService,
    PageConfigService,
    KtDialogService,
    DataTableService,
    SplashScreenService,

    {
      provide: APP_INITIALIZER,
      useFactory: appInitializer,
      multi: true,
      deps: [AuthService],
    },
    {
      provide: HIGHLIGHT_OPTIONS,
      useValue: {
        coreLibraryLoader: () => import("highlight.js/lib/core"),
        languages: {
          xml: () => import("highlight.js/lib/languages/xml"),
          typescript: () => import("highlight.js/lib/languages/typescript"),
          scss: () => import("highlight.js/lib/languages/scss"),
          json: () => import("highlight.js/lib/languages/json"),
        },
      },
    },
    { 
      provide: MAT_MOMENT_DATE_ADAPTER_OPTIONS,
      useValue: { useUtc: true } 
    },
    { provide: MAT_DATE_LOCALE, useValue: 'en-GB' },
    // { provide: MAT_DATE_LOCALE, useValue: 'vi' },
    ListDepartmentService,
    HttpUtilsService,
    MenuAsideService,
    MenuConfigService,
    MenuPhanQuyenServices,
    SubheaderService,
    MenuHorizontalService,
    TypesUtilsService,
    LayoutUtilsService,
    DatePipe,
    // CookieService,
    PermissionUrl,
    AuthService,
    CommonService,
  ],
  entryComponents: [
  ],
  bootstrap: [AppComponent],
})
export class AppModule {}
