import { LayoutService } from './../../../../../core/services/layout.service';
import { UserProfileService } from './../../../../core/auth/_services/user-profile.service';
import { TokenStorage } from './../../../../core/auth/_services/token-storage.service';
import { LayoutConfigService } from './../../../../core/_base/layout/services/layout-config.service';
import { LayoutUtilsService } from './../../../../core/utils/layout-utils.service';
import { environment } from './../../../../../../../environments/environment';
// Angular
import { Component, Input, OnInit, ChangeDetectionStrategy, ChangeDetectorRef, Inject, Type, ComponentFactoryResolver } from '@angular/core';
import { DomSanitizer } from '@angular/platform-browser';
import objectPath from 'object-path';
import { Router } from '@angular/router';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';


@Component({
	selector: 'kt-notification-general',
	templateUrl: './notification-general.component.html',
	styleUrls: ['notification-general.component.scss'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class NotificationGeneralComponent implements OnInit {
	extrasNotificationsDropdownStyle: 'light' | 'dark' = 'dark';

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

	desktopHeaderDisplay: boolean;
	/**
	 * Component constructor
	 *
	 * @param sanitizer: DomSanitizer
	 */

	ID_NV: number = 0;
	AppCode: string = '';
	_component: ItemComponent
	_datacomponent: {};
	lstReminders: any[] = [];
	totalRem: number = 0;
	constructor(
		private layoutConfigService: LayoutConfigService,
		private layout: LayoutService,
		private sanitizer: DomSanitizer,
		private changeDetectorRefs: ChangeDetectorRef,
		private layoutUtilsService: LayoutUtilsService,
		private userProfileService: UserProfileService,
		private tokenStorage: TokenStorage,
		private router: Router,
		public dialog: MatDialog,
		private componentFactoryResolver: ComponentFactoryResolver
	) {
	}

	ngOnInit() {
		this.tokenStorage.getIDUser().subscribe(res => {
			this.ID_NV = +res;
		});

		const config = this.layout.getConfig();
		this.desktopHeaderDisplay = objectPath.get(config, 'header.self.fixed.desktop');
		setInterval(() => {
			this.loadNotification();
		}, 30000);
		this.loadNotification();

		this.extrasNotificationsDropdownStyle = this.layout.getProp(
			'extras.notifications.dropdown.style'
		  );
	}
	loadNotification() {
		if (environment.Module == 'wework') {
			this.AppCode = 'WW';
		} else if (environment.Module == 'QLBTSC') {
			this.AppCode = 'AMS';
		} else if (environment.Module == 'WMS') {
			this.AppCode = 'WMS';
		} else if (environment.Module == 'Workflow') {
			this.AppCode = 'WF';
		} else {
			this.AppCode = 'Land';
		}
		
		// this.userProfileService.Get_DSThongBao(this.AppCode, localStorage.getItem('language')).subscribe(res => {
		// 	if (res.data && res.status == 1) {
		// 		this.lstNotification = res.data;
		// 		this.total = res.TongSoLuong;
		// 	}
		// 	this.changeDetectorRefs.detectChanges();
		// })
		// this.userProfileService.Get_DSNhacNho(this.AppCode, localStorage.getItem('language')).subscribe(res => {
		// 	if (res.data && res.status == 1) {
		// 		this.lstReminders = res.data;
		// 		this.totalRem = res.TongSoLuong;
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
	ChangeLink(item: any) {
		if (item.ComponentName != null && item.ComponentName != "") {
			this.LoadComponent(item.ComponentName);
			// this.LoadDataComponent(item.Component);
			let _dataComponent = JSON.parse(item.Component);
			const dialogRef = this.dialog.open(this._component.component, { data: _dataComponent, height: '70%' });
			dialogRef.afterClosed().subscribe(res => {
				if (!res) {
					return;
				}
			});
		} else {
			if(this.AppCode == item.AppCode){
				if (item.Target == "_blank") {
					window.open(item.Link, '_blank')
				} else {
					this.router.navigate([item.Link]);
				}
			}else{
				if (item.Target == "_blank") {
					window.open(item.Link, '_blank')
				} else {
					window.location.href = item.Link;
				}
			}
		}
	}
	LoadComponent(tab: any) {
		var _popup = {
			// "QuanLyDuyetNghiPhepComponent": QuanLyDuyetNghiPhepComponent,
			// "QuanLyDuyetTangCaComponent": QuanLyDuyetTangCaComponent,
			// "QLDieuXeEditComponent": QLDieuXeEditComponent,
			// "YeuCauCapPhatTaiSanEditComponent": YeuCauCapPhatTaiSanEditComponent,
			// "YeuCauThuHoiTaiSanEditComponent": YeuCauThuHoiTaiSanEditComponent,
		}
		if (_popup[tab]) {
			this._component = new ItemComponent(_popup[tab]);
		}
		else {
			this._component = null;
		}
	}
	LoadDataComponent(data: any) {
		this._datacomponent = {};
		data = data.split(";");
		data.map((item, index) => {
			item = item.split(":");
			this._datacomponent[item[0]] = item[1];
		})
	}

	// noti new
	setActiveTabId(tabId) {
		this.activeTabId = tabId;
	  }
	activeTabId:
    | 'topbar_notifications_notifications'
    | 'topbar_notifications_events'
    | 'topbar_notifications_logs' = 'topbar_notifications_notifications';
	getActiveCSSClasses(tabId) {
		if (tabId !== this.activeTabId) {
		  return '';
		}
		return 'active show';
	  }
}


export class ItemComponent {
	component: Type<any>
	constructor(component: Type<any>) {
		this.component = component;
	}
}
