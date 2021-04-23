

// Angular
import { Component, ElementRef, OnInit, ViewChild } from '@angular/core';
// Object-Path
import * as objectPath from 'object-path';
// Layout
import { SplashScreenService } from './../../../../partials/layout/splash-screen/splash-screen.service';
import { LayoutConfigService } from './../../../core/_base/layout/services/layout-config.service';
import { TokenStorage } from './../../../core/auth/_services/token-storage.service';

@Component({
	selector: 'kt-splash-screen',
	templateUrl: './splash-screen.component.html',
	styleUrls: ['./splash-screen.component.scss']
})
export class SplashScreenComponent implements OnInit {
	// Public proprties
	loaderLogo: string;
	loaderType: string;
	loaderMessage: string;
	logo_custemer: string = '';
	@ViewChild('splashScreen', {static: true}) splashScreen: ElementRef;

	/**
	 * Component constructor
	 *
	 * @param el: ElementRef
	 * @param layoutConfigService: LayoutConfigService
	 * @param splashScreenService: SplachScreenService
	 */
	constructor(
		private el: ElementRef,
		private layoutConfigService: LayoutConfigService,
		private splashScreenService: SplashScreenService,
		private tokenStorage: TokenStorage,) {
	}

	/**
	 * @ Lifecycle sequences => https://angular.io/guide/lifecycle-hooks
	 */

	/**
	 * On init
	 */
	ngOnInit() {
		// init splash screen, see loader option in layout.config.ts
		const loaderConfig = this.layoutConfigService.getConfig('loader');
		this.loaderLogo = objectPath.get(loaderConfig, 'logo');
		this.loaderType = objectPath.get(loaderConfig, 'type');
		this.loaderMessage = objectPath.get(loaderConfig, 'message');

		this.splashScreenService.init(this.splashScreen);

		this.tokenStorage.getLogoCus().subscribe(res => {
			if(res != undefined && res != null){
				this.logo_custemer = res;
			}
		})
	}
}
