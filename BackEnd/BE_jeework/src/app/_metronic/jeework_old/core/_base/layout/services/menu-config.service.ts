// Angular
import { Injectable } from '@angular/core';
// RxJS
import { Subject } from 'rxjs';
import { MenuPhanQuyenServices } from './menu-phan-quyen.service';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
const Module = '' + environment.Module;

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
		console.log("Roles new", res);
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
			// Menu chính
			if (dt.length > 0) {
				dt.forEach((item, index) => {
					//Có quyền thêm quy trình động mới hiển thị nút thêm
					if (!item.IsShowAdd && +item.GroupName > 0 && item.Child.length == 0) return;
					let src = "";
					if (item.Title != "" && item.Title != null) {//menu gốc
						if (environment.Module == "QLBTSC") {//Quản lý SCBT k có đa ngôn ngữ
							src = item.Summary;//hiện tại lấy summary để lấy tên hiển thị
						} else {
							src = this.translate.instant('MainMenu.' + '' + item.Title);
						}

					} else {//menu phân loại
						src = item.Title_;
					}
					let parentMenu = {
						title: src,
						root: item.Child.length == 0,
						icon: '' + item.Icon,
						page: '',
						target: '' + item.Target, // bổ sung vào để phân biệt kiểu target
						id_phanloai: +item.GroupName ? +item.GroupName : -1, //ID theo phân loại
						isproject: +item.GroupName ? false : true, //ID theo phân loại
						showAdd: item.IsShowAdd, //hiển thị icon để thêm nhiệm vụ theo phân loại, có quyền thiết lập quy trình động
						alignment: 'left',//dành cho header menu
					};
					if (item.Child && item.Child.length > 0) {
						parentMenu["bullet"] = 'dot';
						parentMenu["submenu"] = [];
						item.Child.forEach((itemE, indexE) => {
							let srcSub = 'SubMenu.' + '' + itemE.Title;//for sub menu
							let child = {
								//title: '' + itemE.Summary,
								title: '' + srcSub,
								page: '' + itemE.ALink,
								target: '' + itemE.Target // bổ sung vào để phân biệt kiểu target
							};
							parentMenu["submenu"].push(child);
						});
					}
					config.aside.items.push(parentMenu);
					config.header.items.push(parentMenu);
				});
			}
			// Các menu project wework
			if (spaceww.length > 0) {
				spaceww.forEach((item, index) => {
					// if (item.Data.length == 0) return;
					let parentMenu = {
						title: '' + item.Title,
						root: item.Data.length == 0,
						icon: '' + item.Icon,
						page: '',
						// target: '' + item.Target,
						id_phanloai: 1,
						// showAdd: item.IsShowAdd,
						alignment: 'left',//dành cho header menu
						id: '' + item.RowID,

					};

					if (item.Data && item.Data.length > 0) {
						parentMenu["bullet"] = 'dot';
						parentMenu["submenu"] = [];
						item.Data.forEach((itemE, indexE) => {
							let srcSub = 'SubMenu.' + '' + itemE.Title;//for sub menu
							let child = {
								//title: '' + itemE.Summary,
								title: '' + itemE.Title,
								page: '/project' + itemE.ALink + '/home/clickup',
								target: '' + itemE.Target, // bổ sung vào để phân biệt kiểu target
								id_phanloai: 0,
								id: '' + itemE.ID_Row,
								Locked: itemE.Locked,
								Is_Project: itemE.Is_Project,
								Status: itemE.Status,
								Default_View: itemE.Default_View //1: streamview; 2: period view, 3: board view, 4: list view, 5: gantt
							};
							parentMenu["submenu"].push(child);
						});
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

	fs_AssignWF(res: any) {
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
			let dt = res.data;
			let arr = [];
			// Menu chính
			if (dt.length > 0) {
				dt.forEach((item, index) => {
					//Có quyền thêm quy trình động mới hiển thị nút thêm
					if (!item.IsShowAdd && +item.GroupName > 0 && item.Child.length == 0) return;
					let src = "";
					if (item.Title != "" && item.Title != null) {//menu gốc
						if (environment.Module == "QLBTSC") {//Quản lý SCBT k có đa ngôn ngữ
							src = item.Summary;//hiện tại lấy summary để lấy tên hiển thị
						} else {
							src = this.translate.instant('MainMenu.' + '' + item.Title);
						}

					} else {//menu phân loại
						src = item.Title_;
					}
					let parentMenu = {
						title: src,
						root: item.Child.length == 0,
						icon: '' + item.Icon,
						page: '',
						target: '' + item.Target, // bổ sung vào để phân biệt kiểu target
						id_phanloai: +item.GroupName ? +item.GroupName : -1, //ID theo phân loại
						isproject: +item.GroupName ? false : true, //ID theo phân loại
						showAdd: item.IsShowAdd, //hiển thị icon để thêm nhiệm vụ theo phân loại, có quyền thiết lập quy trình động
						alignment: 'left',//dành cho header menu
					};
					if (item.Child && item.Child.length > 0) {
						parentMenu["bullet"] = 'dot';
						parentMenu["submenu"] = [];
						item.Child.forEach((itemE, indexE) => {
							let srcSub = "";
							if (itemE.Title != "" && itemE.Title != null) {//menu gốc
								srcSub = this.translate.instant('SubMenu.' + '' + itemE.Title);
							} else {//menu phân loại
								srcSub = itemE.Title_;
							}
							let child = {
								title: srcSub,
								page: '' + itemE.ALink,
								target: '' + itemE.Target, // bổ sung vào để phân biệt kiểu target,
								id_phanloai: 0,
								showAdd: false,
								type: itemE.Title,
							};
							parentMenu["submenu"].push(child);
						});
					}
					config.aside.items.push(parentMenu);
					config.header.items.push(parentMenu);
				});
			}
		}
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
