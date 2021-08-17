import { MenuHorizontalComponent } from './_layout/components/header/menu-horizontal/menu-horizontal.component';
import { TokenStorage } from './../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { Store } from '@ngrx/store';
import { PartialsModule } from './../_metronic/jeework_old/partials/partials.module';
import { MenuPhanQuyenServices } from './../_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service';
import { MenuConfigService } from './../_metronic/jeework_old/core/_base/layout/services/menu-config.service';
import { MenuAsideService } from './../_metronic/jeework_old/core/_base/layout/services/menu-aside.service';
import { HttpUtilsService } from './../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { ListDepartmentService } from './JeeWork_Core/List-department/Services/List-department.service';
import { JeeWork_CoreModule } from './JeeWork_Core/JeeWork_Core.module';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';
import { InlineSVGModule } from 'ng-inline-svg';
import { PagesRoutingModule } from './pages-routing.module';
import {
  NgbDropdownModule,
  NgbProgressbarModule,
} from '@ng-bootstrap/ng-bootstrap';
import { TranslationModule } from '../modules/i18n/translation.module';
import { LayoutComponent } from './_layout/layout.component';
import { ScriptsInitComponent } from './_layout/init/scipts-init/scripts-init.component';
import { HeaderMobileComponent } from './_layout/components/header-mobile/header-mobile.component';
import { AsideComponent } from './_layout/components/aside/aside.component';
import { FooterComponent } from './_layout/components/footer/footer.component';
import { HeaderComponent } from './_layout/components/header/header.component';
import { HeaderMenuComponent } from './_layout/components/header/header-menu/header-menu.component';
import { TopbarComponent } from './_layout/components/topbar/topbar.component';
import { ExtrasModule } from '../_metronic/partials/layout/extras/extras.module';
import { LanguageSelectorComponent } from './_layout/components/topbar/language-selector/language-selector.component';
import { CoreModule } from '../_metronic/core';
import { SubheaderModule } from '../_metronic/partials/layout/subheader/subheader.module';
import { AsideDynamicComponent } from './_layout/components/aside-dynamic/aside-dynamic.component';
import { HeaderMenuDynamicComponent } from './_layout/components/header/header-menu-dynamic/header-menu-dynamic.component';
import { AsideWeworkComponent } from './_layout/components/aside-wework/aside-wework.component';
import { AvatarModule } from 'ngx-avatar';
@NgModule({
  declarations: [
    LayoutComponent,
    ScriptsInitComponent,
    HeaderMobileComponent,
    AsideComponent,
    FooterComponent,
    HeaderComponent,
    HeaderMenuComponent,
    MenuHorizontalComponent,
    TopbarComponent,
    LanguageSelectorComponent,
    AsideDynamicComponent,
    HeaderMenuDynamicComponent,
    AsideWeworkComponent,
  ],
  providers: [
    TokenStorage
  ],
  imports: [
    CommonModule,
    PagesRoutingModule,
    TranslationModule,
    InlineSVGModule,
    ExtrasModule,
    NgbDropdownModule,
    NgbProgressbarModule,
    CoreModule,
    SubheaderModule,
    JeeWork_CoreModule,
    PartialsModule,
    AvatarModule, 
  ],
})
export class LayoutModule { }
