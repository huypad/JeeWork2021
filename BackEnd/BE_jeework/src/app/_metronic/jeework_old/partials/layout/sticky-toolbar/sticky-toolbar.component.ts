
// Angular
import {Component} from '@angular/core';
// Layout
import { OffcanvasOptions } from './../../../core/_base/layout/directives/offcanvas.directive';
import { LayoutConfigService } from './../../../core/_base/layout/services/layout-config.service';
@Component({
	selector: 'kt-sticky-toolbar',
	templateUrl: './sticky-toolbar.component.html',
	styleUrls: ['./sticky-toolbar.component.scss'],
})
export class StickyToolbarComponent {
	// Public properties
	demoPanelOptions: OffcanvasOptions = {
		overlay: true,
		baseClass: 'kt-demo-panel',
		closeBy: 'kt_demo_panel_close',
		toggleBy: 'kt_demo_panel_toggle',
	};

	baseHref: string;

	constructor(private layoutConfigService: LayoutConfigService) {
		this.baseHref = 'https://keenthemes.com/metronic/preview/angular/';
	}

	isActiveDemo(demo) {
		return demo === this.layoutConfigService.getConfig('demo');
	}
}
