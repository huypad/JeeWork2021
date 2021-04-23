import { LayoutService } from './../../../../../core/services/layout.service';
import { LayoutConfigService } from './../../../../core/_base/layout/services/layout-config.service';
// Angular
import { Component, Input, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
// Layout
import objectPath from 'object-path';

@Component({
	selector: 'kt-notification',
	templateUrl: './notification.component.html',
	styleUrls: ['notification.component.scss'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotificationComponent implements OnInit  {
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
	lstNotification: any[]=[];
	total : number = 0;
	shake = false;

	desktopHeaderDisplay: boolean;
	/**
	 * Component constructor
	 *
	 * @param sanitizer: DomSanitizer
	 */
	constructor(
		private layoutConfigService: LayoutConfigService,
		private layout: LayoutService,
		private sanitizer: DomSanitizer,
		private changeDetectorRefs: ChangeDetectorRef,
		) {
	}

	ngOnInit() {
		const config = this.layout.getConfig();
		this.desktopHeaderDisplay = objectPath.get(config, 'header.self.fixed.desktop');
		// setInterval(() => {
		// 	this.loadNotification();
		// }, 30000);
		// if(this.dungchungservice.CheckRoles(65)){//có quyền xem thông báo
		// 	this.loadNotification();
		// }
		
	}
	loadNotification(){
		// this.dungchungservice.getNotification().subscribe(res=>{
		// 	if(res.status==1&&res.data){
		// 		this.lstNotification = res.data.Notis;
		// 		this.total = res.data.ToTal;
		// 		// if (this.total > 0) {
		// 		// 	this.shake = true;					
		// 		// }else{
		// 		// 	this.shake = false;
		// 		// }				
		// 	}
		// 	this.changeDetectorRefs.detectChanges();
		// })
	}
	backGroundStyle(): string {
		if (!this.bgImage) {
			return 'none';
		}

		return 'url(' + this.bgImage + ')';
	}
}
