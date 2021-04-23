export interface LayoutConfigModel {
	demo: string;
	self: {
		layout?: string;
		body?: {
			'background-image'?: string,
			'class'?: string,
			'background-position'?: string,
			'background-size'?: string
		};
		logo: any | string;
		'font_size'?:string;
	};
	portlet?: {
		sticky: {
			offset: number
		}
	};
	loader: {
		enabled: boolean;
		type?: string | 'default' | 'spinner-message' | 'spinner-logo';
		logo?: string;
		message?: string;
	};
	colors: {
		state?: any;
		base: {
			label: string[];
			shape: string[]
		}
	};
	width?: string;
	header: {
		self: {
			skin?: string;
			width?: string;
			fixed: {
				desktop: any;
				mobile: boolean
			};
			'font_size'?:string;
		};
		// not implemented yet
		topbar?: {
			self?: {
				width?: string;
				'font_size'?:string;
			}
			search?: {
				display: boolean;
				layout: 'offcanvas' | 'dropdown';
				dropdown?: {
					style: 'light' | 'dark';
				}
			};
			notifications?: {
				display: boolean;
				layout: 'offcanvas' | 'dropdown';
				dropdown: {
					style: 'light' | 'dark';
				}
			};
			'quick-actions'?: {
				display: boolean;
				layout: 'offcanvas' | 'dropdown';
				dropdown: {
					style: 'light' | 'dark';
				}
			};
			user?: {
				display: boolean;
				layout: 'offcanvas' | 'dropdown';
				dropdown: {
					style: 'light' | 'dark';
				}
			};
			languages?: {
				display: boolean
			};
			cart?: {
				display: boolean
			};
			'my-cart'?: any
			'quick-panel'?: {
				display: boolean
			};
			'icon-style'?:string;
			'font_size'?:string;
		};
		search?: {
			display: boolean
		};
		menu?: {
			self: {
				display: boolean;
				layout?: string;
				'root-arrow'?: boolean;
				width?: string;
				'font_size'?:string;
			};
			desktop: {
				arrow: boolean;
				toggle: string;
				submenu: {
					skin?: string;
					arrow: boolean
				}
			};
			mobile: {
				submenu: {
					skin: string;
					accordion: boolean
				}
			}
		}
	};
	brand?: {
		self: {
			skin: string;
			'font_size'?:string;
		}
	};
	aside?: {
		self: {
			skin?: string;
			display: boolean;
			fixed?: boolean | any;
			minimize?: {
				toggle: boolean;
				default: boolean
			}
			'font_size'?:string;
		};
		footer?: {
			self: {
				display: boolean;
				'font_size'?:string;
			}
		};
		menu: {
			'root-arrow'?: boolean;
			dropdown: boolean;
			scroll: boolean;
			submenu: {
				accordion: boolean;
				dropdown: {
					arrow: boolean;
					'hover-timeout': number
				}
			},
			'icon-style'?:string
		}
	};
	'aside-secondary'?: {
		self: {
			display: boolean;
			layout?: string;
			expanded?: boolean;
			'font_size'?:string;
		}
	};
	subheader?: {
		display: boolean;
		fixed?: boolean;
		width?: string;
		layout?: string;
		style?: 'light' | 'solid' | 'transparent';
		daterangepicker?: {
			display: boolean
		}
	};
	content?: any;
	toolbar?:any;
	footer?: {
		self?: any;
	};
	'quick-panel'?: {
		display?: boolean
	};
}
