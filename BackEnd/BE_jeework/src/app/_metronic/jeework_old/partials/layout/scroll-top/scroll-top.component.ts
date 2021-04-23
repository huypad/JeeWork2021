import { ScrollTopOptions } from './../../../core/_base/layout/directives/scroll-top.directive';
// Angular
import { Component } from '@angular/core';
// Layout

@Component({
	selector: 'kt-scroll-top',
	templateUrl: './scroll-top.component.html',
})
export class ScrollTopComponent {
	// Public properties
	scrollTopOptions: ScrollTopOptions = {
		offset: 300,
		speed: 600
	};
}
