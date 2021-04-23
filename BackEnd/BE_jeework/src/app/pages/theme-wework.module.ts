// import { JeeHRSubModule } from './../../pages/JeeHR/JeeHRSub.module';
// import { WeWorkModule } from './../../pages/JeeHR/WeWork/wework.module';
// import { NgxPermissionsModule } from 'ngx-permissions';
// // Angular
// import { NgModule } from '@angular/core';
// import { CommonModule } from '@angular/common';
// import { RouterModule } from '@angular/router';
// import { FormsModule } from '@angular/forms';
// // Angular Material 
// import { MatTooltipModule } from '@angular/material/tooltip';
// import { MatButtonModule } from '@angular/material/button'; 
// import { MatProgressBarModule } from '@angular/material/progress-bar'; 
// import { MatTabsModule } from '@angular/material/tabs'; 
// // NgBootstrap
// import { NgbProgressbarModule, NgbTooltipModule } from '@ng-bootstrap/ng-bootstrap';
// // Translation
// import { TranslateModule } from '@ngx-translate/core';
// // Loading bar
// import { LoadingBarModule } from '@ngx-loading-bar/core';
// // NGRX
// import { StoreModule } from '@ngrx/store';
// import { EffectsModule } from '@ngrx/effects';
// // Ngx DatePicker
// import { NgxDaterangepickerMd } from 'ngx-daterangepicker-material';
// // Perfect Scrollbar
// import { PerfectScrollbarModule } from 'ngx-perfect-scrollbar';
// // SVG inline
// import { InlineSVGModule } from 'ng-inline-svg';
// // Core Module
// import { CoreModule } from '../../../core/core.module';
// import { HeaderComponent } from './header/header.component';
// import { FooterComponent } from './footer/footer.component';
// import { SubheaderComponent } from './subheader/subheader.component';
// import { BrandComponent } from './brand/brand.component';
// import { TopbarComponent } from './header/topbar/topbar.component';
// import { MenuHorizontalComponent } from './header/menu-horizontal/menu-horizontal.component';
// import { PartialsModule } from '../../partials/partials.module';
// import { BaseComponent } from './base/base.component';
// import { PagesRoutingModule } from '../../pages/pages-routing-wework.module';
// import { PagesModule } from '../../pages/pages.module';
// import { HtmlClassService } from './html-class.service';
// import { HeaderMobileComponent } from './header/header-mobile/header-mobile.component';
// import { ErrorPageComponent } from './content/error-page/error-page.component';
// import { PermissionEffects, permissionsReducer, RoleEffects, rolesReducer } from '../../../core/auth';
// import { BaseTokenComponent } from './base-token/base-token.component';
// import { ViewModule } from '../../view.module';

// @NgModule({
// 	declarations: [
	
// 	],
// 	exports: [
	
// 	],
// 	providers: [
// 		HtmlClassService,
// 	],
// 	imports: [
// 		CommonModule,
// 		RouterModule,
// 		NgxPermissionsModule.forChild(),
// 		StoreModule.forFeature('roles', rolesReducer),
// 		StoreModule.forFeature('permissions', permissionsReducer),
// 		EffectsModule.forFeature([PermissionEffects, RoleEffects]),
// 		PagesRoutingModule,
// 		PagesModule,
// 		PartialsModule,
// 		CoreModule,
// 		PerfectScrollbarModule,
// 		FormsModule,
// 		MatProgressBarModule,
// 		MatTabsModule,
// 		MatButtonModule,
// 		MatTooltipModule,
// 		TranslateModule.forChild(),
// 		LoadingBarModule,
// 		NgxDaterangepickerMd,
// 		InlineSVGModule,

// 		// ng-bootstrap modules
// 		NgbProgressbarModule,
// 		NgbTooltipModule,
// 		ViewModule,
// 		WeWorkModule,
// 		JeeHRSubModule
// 	]
// })
// export class ThemeModule {
// }
