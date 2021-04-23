import { UserProfileService } from './../../../../core/auth/_services/user-profile.service'; 
// Angular
import { Component, Input, OnInit, Inject, ViewChild, ElementRef, ChangeDetectorRef, HostBinding } from '@angular/core';
// State
 import { DOCUMENT } from '@angular/common'; 
 
@Component({
	selector: 'kt-list-module',
	templateUrl: './list-module.component.html',
	// styleUrls: ['notification.component.scss'],
})
export class ListModuleComponent {
	// Show dot on top of the icon
	@Input() dot: string;

	// Show pulse on icon
	@Input() pulse: boolean;

	@Input() pulseLight: boolean;

	// Set icon class name
	@Input() icon: string = 'flaticon2-bell-alarm-symbol';
	@Input() iconType: '' | 'success';

	// Set true to icon as SVG or false as icon class
	@Input() useSVG: boolean;

	// Set bg image path
	@Input() bgImage: string;

	// Set skin color, default to light
	@Input() skin: 'light' | 'dark' = 'light';

	@Input() type: 'brand' | 'success' = 'success';
	lstNotification: any[] = [];
	total: number = 0;
	shake = false;

	listModule: any[] = [];
	/**
	 * Component constructor
	 *
	 * @param sanitizer: DomSanitizer
	 */
	constructor(
		private userProfileService: UserProfileService,
		@Inject(DOCUMENT) private document: Document,
 		private changeDetectorRefs: ChangeDetectorRef, ) {
		this.loadModule();
	}

	loadModule() {
		
	}

	LoadMenu(val: any) {
		window.open('' + val.Link, '_blank');
	}

}
