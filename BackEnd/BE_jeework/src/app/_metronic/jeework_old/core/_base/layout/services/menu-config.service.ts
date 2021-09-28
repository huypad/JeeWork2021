// Angular
import { Injectable } from '@angular/core';
// RxJS
import { Subject } from 'rxjs';
import { MenuPhanQuyenServices } from './menu-phan-quyen.service';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
const Module = '' + environment.MODULE;

@Injectable()
export class MenuConfigService {
	// Public properties
	onConfigUpdated$: Subject<any>;
	// Private properties
	private menuConfig: any;

	/**
	 * Service Constructor
	 */
	constructor(
		private menuPhanQuyenServices: MenuPhanQuyenServices,
		private translate: TranslateService) {
		// register on config changed event and set default config
		this.onConfigUpdated$ = new Subject();
	}

	/**
	 * Returns the menuConfig
	 */
	async getMenus() {
		//lấy menu phân quyền
		let res = await this.layMenu().then();
		let menu;
		menu = this.fs_Assign(res);
		return menu;
		// return this.menuConfig;
	}
	layMenu() {
		return this.menuPhanQuyenServices.layMenuChucNang(Module).toPromise();
	}
	async GetRole_WeWork(username) {
		let res = await this.AllRoles_WeWork(username).then();
		localStorage.setItem('WeWorkRoles', JSON.stringify(res));
	}

	AllRoles_WeWork(username) {
		return this.menuPhanQuyenServices.WW_Roles(username).toPromise();
	}
	fs_Assign(res: any) {
		let config = {
			header: {
				self: {},
				items: []
			},
			aside: {
				self: {},
				items: []
			}
		};
		if (res && res.status == 1) {
			let dt = res.data.data;
			let spaceww = res.data.dataww;
			let arr = [];
			// Các menu project wework
			if (spaceww.length > 0) {
				spaceww.forEach((item, index) => {
					let parentMenu = {
						title: '' + item.Title,
						root: item.list.length == 0,
						icon: '' + item.Icon,
						page: 'pad',
						id_phanloai: 1,
						alignment: 'left',//dành cho header menu
						id: '' + item.RowID,
						IsFolder: item.IsFolder,
						type: item.type,
						submenu: [],
					};
					if (item.folder && item.folder.length > 0) {
						parentMenu["submenu"] = [];
						item.folder.forEach((itemE, indexE) => {
							let _folder = {
								title: '' + itemE.Title,
								root: item.list.length == 0,
								icon: '' + itemE.Icon,
								page: 'pad',
								id_phanloai: 1,
								alignment: 'left',
								id: '' + itemE.RowID,
								IsFolder: itemE.IsFolder,
								type: itemE.type

							};
							parentMenu["submenu"].push(_folder);
							// _folder["bullet"] = 'dot';
							_folder["submenu"] = [];
							itemE.list.forEach((itemS, indexE) => {
								let child = {
									title: '' + itemS.Title,
									page: '/project/' + itemS.ID_Row + '/home/clickup',
									target: '' + itemS.Target, // bổ sung vào để phân biệt kiểu target
									id_phanloai: 0,
									id: '' + itemS.ID_Row,
									Locked: itemS.Locked,
									Is_Project: itemS.Is_Project,
									Status: itemS.Status,
									Default_View: itemS.Default_View, //1: streamview; 2: period view, 3: board view, 4: list view, 5: gantt,
									type: itemS.type
								};
								_folder["submenu"].push(child);
							});
						});
					}
					else
						parentMenu["submenu"] = [];
					// else {
					if (item.list && item.list.length > 0) {
						parentMenu["bullet"] = 'dot';
						// parentMenu["submenu"] = [];
						item.list.forEach((itemE, indexE) => {
							let srcSub = 'SubMenu.' + '' + itemE.Title;//for sub menu
							let child = {
								//title: '' + itemE.Summary,
								title: '' + itemE.Title,
								page: '/project/' + itemE.ID_Row + '/home/clickup',
								target: '' + itemE.Target, // bổ sung vào để phân biệt kiểu target
								id_phanloai: 0,
								id: '' + itemE.ID_Row,
								Locked: itemE.Locked,
								Is_Project: itemE.Is_Project,
								Status: itemE.Status,
								Default_View: itemE.Default_View, //1: streamview; 2: period view, 3: board view, 4: list view, 5: gantt
								type: itemE.type
							};
							parentMenu["submenu"].push(child);
						});
						// }
					}
					config.aside.items.push(parentMenu);
				});
			}
		}
		return config;
	}

	fs_AssignWMS(dt: any) {
		let config = {
			header: {
				self: {},
				items: []
			},
			aside: {
				self: {},
				items: []
			}
		};
		// let arr = [];
		dt.forEach((item, index) => {
			if (item.Child.length > 0) {
				let _module = {
					title: '' + item.MenuName,
					root: item.Child ? item.Child.length > 0 : true,
					icon: '' + item.Icon,
				}
				if (item.Child.length > 0) {
					_module["bullet"] = 'dot';
					_module["submenu"] = [];
					item.Child.forEach((itemE, indexE) => {
						let _mainmenu = {
							title: '' + itemE.MenuName,
							icon: '' + itemE.Icon,
							root: itemE.Child ? itemE.Child.length == 0 : true,
							page: '' + itemE.Link,
						};
						if (itemE.Child.length > 0 || itemE.Link == '/') {
							_mainmenu["bullet"] = 'dot';
							_mainmenu["submenu"] = [];
							itemE.Child.forEach((itemEE, indexEE) => {
								let _submenu = {
									title: '' + itemEE.MenuName,
									page: '' + itemEE.Link,
									// idLoaiHH: itemEE.Position
								};
								_mainmenu["submenu"].push(_submenu);
							});
							_module["submenu"].push(_mainmenu);
						} else {
							let _submenu = {
								title: '' + itemE.MenuName,
								page: '' + itemE.Link,
								icon: '' + itemE.Icon,
								// idLoaiHH: itemE.Position
							};
							_module["submenu"].push(_submenu);
						}
					});
				}
				config.aside.items.push(_module);
			}
			else {
				if (item.Link != '#') {
					let _module = {
						title: '' + item.MenuName,
						root: item.Child ? item.Child.length == 0 : true,
						icon: '' + item.Icon,
						page: '' + item.Link
					};
					config.aside.items.push(_module);
				}
			}
		});
		return config;
	}

	async GetWMSRolesToLocalStorage() {

	}

	/**
	 * Load config
	 *
	 * @param config: any
	 */
	loadConfigs(config: any) {
		this.menuConfig = config;
		this.onConfigUpdated$.next(this.menuConfig);
	}
}
