// Angular
import { Injectable } from '@angular/core';
// RxJS
import { BehaviorSubject } from 'rxjs';
// Object path
import * as objectPath from 'object-path';
// Services
import { MenuConfigService } from './menu-config.service';

@Injectable()
export class MenuHorizontalService {
	// Public properties
	menuList$: BehaviorSubject<any[]> = new BehaviorSubject<any[]>([]);

	/**
	 * Service constructor
	 *
	 * @param menuConfigService: MenuConfigService
	 */
	constructor(private menuConfigService: MenuConfigService) {
		// this.loadMenu(); //Thiên đóng ngày 24/02/2022 vì gọi lặp đi lặp lại
	}

	/**
	 * Load menu list
	 */
	async loadMenu() {
		// get menu list
		const menuItems: any[] = objectPath.get(await this.menuConfigService.getMenus(), 'header.items');
		this.menuList$.next(menuItems);
	}
}
