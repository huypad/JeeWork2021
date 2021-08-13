import { WorkListNewDetailComponent } from './../work-list-new/work-list-new-detail/work-list-new-detail.component';
import { WeWorkService } from './../../services/wework.services';
import { QuickStatusComponent } from './quick-status/quick-status.component';
import { DialogData } from './../../report/report-tab-dashboard/report-tab-dashboard.component';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { WorkService } from './../../work/work.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Inject, HostListener, Input, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router, NavigationStart, NavigationEnd } from '@angular/router';
// Material
// RXJS
import { TranslateService } from '@ngx-translate/core';
// Services
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service'; import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Models
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import 'dayjs/locale/vi' // load on demand
import ItemMovement from "gantt-schedule-timeline-calendar/dist/ItemMovement.plugin.js";
import Selection from "gantt-schedule-timeline-calendar/dist/Selection.plugin.js";
import { ProjectsTeamService } from '../Services/department-and-project.service';
import { UpdateWorkModel } from '../../work/work.model';
import { stringify } from 'node:querystring';
import { GanttEditorComponent, GanttEditorOptions } from 'ng-gantt';

// import inforModal from "./inforModal"
@Component({
	selector: 'kt-gantt-chart-2',
	templateUrl: './gantt-chart-2.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})

export class GanttChart2Component implements OnInit {
	@Input() ID_Project: number = 0;
	isMenuVisible = false;
	firstDemoLoaded = false;
	searchText = '';
	@ViewChild("editor") editor: GanttEditorComponent;
	public editorOptions: GanttEditorOptions;
	title = "ng-gstc-test";
	gstcState: any;
	public data: any;
	view = 'gantt';
	fromDate = (new Date()).getFullYear() + '/05/01';
	toDate = (new Date()).getFullYear() + 1 + '/04/30';
	inforModal;
	status_dynamic: any = [];
	// LEFT SIDE LIST COLUMNS
	columns: any = {
		percent: 100,
		resizer: {
			width: 10,
			inRealTime: true,
			dots: 6
		},
		minWidth: 50,
		data: {
			ck: {
				id: "ck",
				data: function (item) {
					if (item.id[0] == "G")//group
						return "";
					if (item.status) {
						return ` <div onclick="Window.myComponent.onClick123('${item.id}')" class="url" style="background:${item.color}; width: 20px;
						height: 20px;
						display: flex;
						margin: 5px auto;"> </div>`;
					}
				},
				isHTML: true,
				width: 80,
				header: { content: "Check" }
			},

			label: {
				id: "label",
				data: function (item) {
					if (item.id[0] == "G")//group
						return `${item.label}`;
					if (item.status) {
						return ` <span onclick="Window.myComponent.Viewdetail('${item.id}')" class="url"> ${item.label} </span>`;
					}
				},
				isHTML: true,
				expander: true,
				width: 230,
				minWidth: 100,
				header: {
					content: "Công việc"
				}
			},
			start_date: { id: "start_date", data: "start_date", width: 80, header: { content: "Ngày BĐ" } },
			deadline: { id: "deadline", data: "deadline", width: 80, header: { content: "Hạn chót" } },
			end_date: { id: "end_date", data: "end_date", width: 80, header: { content: "Finished at" } },
			status: {
				id: "status",
				data: function (item) {
					if (item.id[0] == "G")//group
						return "";
					if (item.status) {
						return `<span class="btn-sm text-white" style="background:${item.color}">${item.status}</span>`;
					}
				},
				isHTML: true,
				width: 100,
				header: { content: "Trạng thái" }
			},
		}
	};
	config: any;
	locale: any = {
		name: 'vi',
		weekdays: 'CN_T2_T3_T4_T5_T6_T7'.split('_'),
		months: 'Tháng 1_Tháng 2_Tháng 3_Tháng 4_Tháng 5_Tháng 6_Tháng 7_Tháng 8_Tháng 9_Tháng 10_Tháng 11_Tháng 12'.split('_'),
		weekStart: 1,
		weekdaysShort: 'CN_T2_T3_T4_T5_T6_T7'.split('_'),
		monthsShort: 'Th01_Th02_Th03_Th04_Th05_Th06_Th07_Th08_Th09_Th10_Th11_Th12'.split('_'),
		weekdaysMin: 'CN_T2_T3_T4_T5_T6_T7'.split('_'),
		ordinal: n => n,
		formats: {
			LT: 'HH:mm',
			LTS: 'HH:mm:ss',
			L: 'DD/MM/YYYY',
			LL: 'D MMMM [năm] YYYY',
			LLL: 'D MMMM [năm] YYYY HH:mm',
			LLLL: 'dddd, D MMMM [năm] YYYY HH:mm',
			l: 'DD/M/YYYY',
			ll: 'D MMM YYYY',
			lll: 'D MMM YYYY HH:mm',
			llll: 'ddd, D MMM YYYY HH:mm'
		},
		relativeTime: {
			future: '%s tới',
			past: '%s trước',
			s: 'vài giây',
			m: 'một phút',
			mm: '%d phút',
			h: 'một giờ',
			hh: '%d giờ',
			d: 'một ngày',
			dd: '%d ngày',
			M: 'một tháng',
			MM: '%d tháng',
			y: 'một năm',
			yy: '%d năm'
		},
	}

	constructor(public _service: ProjectsTeamService,
		private danhMucService: DanhMucChungService,
		public dialog: MatDialog,
		private route: ActivatedRoute,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private activatedRoute: ActivatedRoute,
		private tokenStorage: TokenStorage,
		private changeDetectorRefs: ChangeDetectorRef,
		private router: Router,
		private _workservice: WorkService,
		private WeWorkService: WeWorkService
	) {
		if ((new Date()).getMonth() < 5) {
			this.fromDate = ((new Date()).getFullYear() - 1) + '/05/01';
			this.toDate = (new Date()).getFullYear() + '/04/30';
		};
		Window["myComponent"] = this;
	}
	ngOnInit() {
		// let now = new Date();
		// let from = (moment(now).add("M", 1).toDate()).getTime();
		// let to = (moment(now).add("M", -1).toDate()).getTime();

		// this.config = {
		// 	// height: window.innerHeight - 125,
		// 	height: window.innerHeight - 159, //800
		// 	viewMode: 'day',
		// 	list: {
		// 		rows: {},
		// 		rowHeight: 40,
		// 		columns: this.columns,
		// 		expander: {
		// 			padding: 18,
		// 			size: 20,
		// 			icon: {
		// 				width: 16,
		// 				height: 16
		// 			},

		// 		},

		// 	},

		// 	scroll: {
		// 		smooth: false,
		// 		top: 0,
		// 		left: 0,
		// 		xMultiplier: 3,
		// 		yMultiplier: 3,
		// 		percent: {
		// 			top: 0,
		// 			left: 0
		// 		},
		// 		compensation: {
		// 			x: 0,
		// 			y: 0
		// 		}
		// 	},

		// 	chart: {
		// 		items: {
		// 		},
		// 		time: {
		// 			centerGlobal: (new Date()).getTime(),//now.getTime(),
		// 			compressMode: false,
		// 			finalFrom: new Date(this.fromDate).getTime(),
		// 			finalTo: new Date(this.toDate).getTime(),
		// 			from: new Date(this.fromDate).getTime(),
		// 			to: new Date(this.toDate).getTime(),

		// 			// leftGlobal: 1594690260608,
		// 			levels: [],
		// 			period: "day",
		// 			// rightGlobal: 1601304678016,
		// 			zoom: 0
		// 		}
		// 	},
		// 	locale: this.locale,
		// 	plugins: [
		// 		// drag x Horizontal, y portrait
		// 		ItemMovement({
		// 			moveable: 'x',
		// 			resizerContent: '<div class="resizer">-></div>',
		// 			ghostNode: false,
		// 			collisionDetection: false,
		// 			snapStart(time, diff, item) {
		// 				if (Math.abs(diff) > 14400000) {
		// 					return time + diff
		// 				}
		// 				return time
		// 			},
		// 			snapEnd(time, diff, item) {
		// 				if (Math.abs(diff) > 14400000) {
		// 					return time + diff
		// 				}
		// 				return time
		// 			}
		// 		}),
		// 		Selection({
		// 			items: false,
		// 			rows: false,
		// 			grid: true,
		// 			rectStyle: { opacity: '0.0' },
		// 			canSelect(type, currentlySelecting) {

		// 				if (type === 'chart-timeline-grid-row-block') {
		// 					return currentlySelecting.filter(selected => {
		// 						if (!selected.row.canSelect) return false;
		// 						for (const item of selected.row._internal.items) {
		// 							if (
		// 								(item.time.start >= selected.time.leftGlobal && item.time.start <= selected.time.rightGlobal) ||
		// 								(item.time.end >= selected.time.leftGlobal && item.time.end <= selected.time.rightGlobal) ||
		// 								(item.time.start <= selected.time.leftGlobal && item.time.end >= selected.time.rightGlobal)
		// 							) {
		// 								return false;
		// 							}
		// 						}
		// 						return true;
		// 					});
		// 				}
		// 				return currentlySelecting;
		// 			},
		// 			canDeselect(type, currently, all) {
		// 				if (type === 'chart-timeline-grid-row-blocks') {
		// 					return all.selecting['chart-timeline-grid-row-blocks'].length ? [] : currently;
		// 				}
		// 				return [];
		// 			}
		// 		})
		// 	],
		// 	modal: {
		// 		visible: false,
		// 		title: '',
		// 		data: {}
		// 	},
		// 	subs: [],
		// 	watch: {},
		// 	computed: {},
		// 	methods: {
		// 		addListenClick(element, data) {
		// 			const onClick = (e) => {
		// 				e.preventDefault()
		// 				this.modal = {
		// 					visible: true,
		// 					title: data.item.label,
		// 					data
		// 				}
		// 				return false
		// 			}
		// 			element.addEventListener('contextmenu', onClick);
		// 			return {
		// 				update(element, newData) {
		// 					data = newData;
		// 				},
		// 				destroy(element, data) {
		// 					element.removeEventListener('click', onClick);

		// 				}
		// 			};
		// 		},
		// 		closeModal() {
		// 			this.modal = {
		// 				visible: false,
		// 				title: '',
		// 				data: {}
		// 			}
		// 		}
		// 	}
		// }
		this.data = this.initialData();
		let rows: any = {};
		let items: any = {};
		var query = new QueryParamsModelNew({ "id_project_team": this.ID_Project });
		query.more = true;
		this._service.findGantt(query).subscribe(res => {
			if (res && res.status == 1) {
				rows = res.data.rows.reduce(function (result, row, index, array) {
					result[row.id] = row; //a, b, c
					return result;
				}, {});
				items = res.data.items.reduce(function (result, item, index, array) {
					result[item.id] = item;
					return result;
				}, {});

			}
			this.gstcState.data.config.list.rows = rows;
			this.gstcState.data.config.chart.items = items;
			debugger
			this.gstcState.data.config.locale = this.locale;
			this.gstcState.update('config', config => {
				return config;
			});

			this.changeDetectorRefs.detectChanges();
		})

		this.WeWorkService.ListStatusDynamic(this.ID_Project).subscribe(res => {
			if (res && res.status === 1) {
				this.status_dynamic = res.data;
			};
		})
		this.editorOptions = {
			vFormat: "day",
			vEditable: true,
			vEventsChange: {
				taskname: () => {
					console.log("taskname");
				}
			}
		};
	}
	initialData() {
		return [
		  {
			pID: 1,
			pName: "Define Chart API",
			pStart: "",
			pEnd: "",
			pClass: "ggroupblack",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 0,
			pGroup: 1,
			pParent: 0,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: "Some Notes text"
		  },
		  {
			pID: 11,
			pName: "Chart Object",
			pStart: "2017-02-20",
			pEnd: "2017-02-20",
			pClass: "gmilestone",
			pLink: "",
			pMile: 1,
			pRes: "Shlomy",
			pComp: 100,
			pGroup: 0,
			pParent: 1,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 12,
			pName: "Task Objects",
			pStart: "",
			pEnd: "",
			pClass: "ggroupblack",
			pLink: "",
			pMile: 0,
			pRes: "Shlomy",
			pComp: 40,
			pGroup: 1,
			pParent: 1,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 121,
			pName: "Constructor Proc #1234 of February 2017",
			pStart: "2017-02-21",
			pEnd: "2017-03-09",
			pClass: "gtaskblue",
			pLink: "",
			pMile: 0,
			pRes: "Brian T.",
			pComp: 60,
			pGroup: 0,
			pParent: 12,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 122,
			pName: "Task Variables",
			pStart: "2017-03-06",
			pEnd: "2017-03-11",
			pClass: "gtaskred",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 60,
			pGroup: 0,
			pParent: 12,
			pOpen: 1,
			pDepend: 121,
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 123,
			pName: "Task by Minute/Hour",
			pStart: "2017-03-09",
			pEnd: "2017-03-14 12: 00",
			pClass: "gtaskyellow",
			pLink: "",
			pMile: 0,
			pRes: "Ilan",
			pComp: 60,
			pGroup: 0,
			pParent: 12,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 124,
			pName: "Task Functions",
			pStart: "2017-03-09",
			pEnd: "2017-03-29",
			pClass: "gtaskred",
			pLink: "",
			pMile: 0,
			pRes: "Anyone",
			pComp: 60,
			pGroup: 0,
			pParent: 12,
			pOpen: 1,
			pDepend: "123SS",
			pCaption: "This is a caption",
			pNotes: null
		  },
		  {
			pID: 2,
			pName: "Create HTML Shell",
			pStart: "2017-03-24",
			pEnd: "2017-03-24",
			pClass: "gtaskyellow",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 20,
			pGroup: 0,
			pParent: 0,
			pOpen: 1,
			pDepend: 122,
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 3,
			pName: "Code Javascript",
			pStart: "",
			pEnd: "",
			pClass: "ggroupblack",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 0,
			pGroup: 1,
			pParent: 0,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 31,
			pName: "Define Variables",
			pStart: "2017-02-25",
			pEnd: "2017-03-17",
			pClass: "gtaskpurple",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 30,
			pGroup: 0,
			pParent: 3,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 32,
			pName: "Calculate Chart Size",
			pStart: "2017-03-15",
			pEnd: "2017-03-24",
			pClass: "gtaskgreen",
			pLink: "",
			pMile: 0,
			pRes: "Shlomy",
			pComp: 40,
			pGroup: 0,
			pParent: 3,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 33,
			pName: "Draw Task Items",
			pStart: "",
			pEnd: "",
			pClass: "ggroupblack",
			pLink: "",
			pMile: 0,
			pRes: "Someone",
			pComp: 40,
			pGroup: 2,
			pParent: 3,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 332,
			pName: "Task Label Table",
			pStart: "2017-03-06",
			pEnd: "2017-03-09",
			pClass: "gtaskblue",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 60,
			pGroup: 0,
			pParent: 33,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 333,
			pName: "Task Scrolling Grid",
			pStart: "2017-03-11",
			pEnd: "2017-03-20",
			pClass: "gtaskblue",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 0,
			pGroup: 0,
			pParent: 33,
			pOpen: 1,
			pDepend: "332",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 34,
			pName: "Draw Task Bars",
			pStart: "",
			pEnd: "",
			pClass: "ggroupblack",
			pLink: "",
			pMile: 0,
			pRes: "Anybody",
			pComp: 60,
			pGroup: 1,
			pParent: 3,
			pOpen: 0,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 341,
			pName: "Loop each Task",
			pStart: "2017-03-26",
			pEnd: "2017-04-11",
			pClass: "gtaskred",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 60,
			pGroup: 0,
			pParent: 34,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 342,
			pName: "Calculate Start/Stop",
			pStart: "2017-04-12",
			pEnd: "2017-05-18",
			pClass: "gtaskpink",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 60,
			pGroup: 0,
			pParent: 34,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 343,
			pName: "Draw Task Div",
			pStart: "2017-05-13",
			pEnd: "2017-05-17",
			pClass: "gtaskred",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 60,
			pGroup: 0,
			pParent: 34,
			pOpen: 1,
			pDepend: "",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 344,
			pName: "Draw Completion Div",
			pStart: "2017-05-17",
			pEnd: "2017-06-04",
			pClass: "gtaskred",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 60,
			pGroup: 0,
			pParent: 34,
			pOpen: 1,
			pDepend: "342,343",
			pCaption: "",
			pNotes: ""
		  },
		  {
			pID: 35,
			pName: "Make Updates",
			pStart: "2017-07-17",
			pEnd: "2017-09-04",
			pClass: "gtaskpurple",
			pLink: "",
			pMile: 0,
			pRes: "Brian",
			pComp: 30,
			pGroup: 0,
			pParent: 3,
			pOpen: 1,
			pDepend: "333",
			pCaption: "",
			pNotes: ""
		  }
		];
	  }
	UpdateCheck(item) {
	}
	// GET THE GANTT INTERNAL STATE
	onState(state) {
		this.gstcState = state;
		this.gstcState.subscribe("config.list.rows", rows => {
		});

		this.gstcState.subscribe("config.chart.items", items => {
		});

		this.gstcState.subscribe(
			"config.chart.items.:id",
			(bulk, eventInfo) => {
				if (eventInfo.type === "update" && eventInfo.params.id) {
					const itemId = eventInfo.params.id;
					this.changeDetectorRefs.detectChanges();
					this.dataChanged(this.gstcState.get("config.chart.items." + itemId))
				}
			},
			{ bulk: true }
		);
	}

	f_convertDate(p_Val: any) {
		let a = p_Val === "" ? new Date() : new Date(p_Val);
		return a.getFullYear() + "/" + ("0" + (a.getMonth() + 1)).slice(-2) + "/" + ("0" + (a.getDate())).slice(-2);
	}

	dataChanged(input) {
		var startDate = this.f_convertDate(input.time.start);
		var endDate = this.f_convertDate(input.time.end);

		var model = new UpdateWorkModel();
		model.id_row = input.id;
		model.key = 'start_date'; //deadline
		model.id_log_action = '11';//17
		model.value = startDate;
		this._workservice.UpdateByKey(model).subscribe(res => {
			if (res && res.status == 1) {
				this.changeDetectorRefs.detectChanges();
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999, true, false, 0);
			}
		});
		var model_end = new UpdateWorkModel();
		model_end.id_row = input.id;
		model_end.key = 'deadline'; //deadline
		model_end.id_log_action = '17';//17
		model_end.value = endDate;
		this._workservice.UpdateByKey(model_end).subscribe(res => {
			if (res && res.status == 1) {
				this.changeDetectorRefs.detectChanges();
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999, true, false);
			}
		});
		// this.router.navigateByUrl("/tasks");
		this.changeDetectorRefs.detectChanges();
	}

	getHeight() {
		return window.innerHeight - 53 - this.tokenStorage.getHeightHeader() + 'px'
	}


	updateDate(item) {
		if (item == 'pre') {
			this.fromDate = (new Date(this.fromDate).getFullYear() - 1) + "/05/01";
			this.toDate = (new Date(this.toDate).getFullYear() - 1) + "/04/30";
		}
		if (item == 'next') {
			this.fromDate = (new Date(this.fromDate).getFullYear() + 1) + "/05/01";
			this.toDate = (new Date(this.toDate).getFullYear() + 1) + "/04/30";
		}

		this.ngOnInit();
		// this.changeDetectorRefs.detectChanges();
	}

	onClick123(val = '') {
		return;
		// không setup update trạng thái
		const dialogRef = this.dialog.open(QuickStatusComponent, {
			width: '300px',
			data: this.status_dynamic,
		});

		dialogRef.afterClosed().subscribe(result => {
			if (result) {
				const item = new UpdateWorkModel();
				item.id_row = +val.replace("W", "");
				item.key = 'status';
				item.value = result.id_row;
				// if (task.id_nv > 0) {
				// 	item.IsStaff = true;
				// }
				this._service._UpdateByKey(item).subscribe(res => {
					if (res && res.status == 1) {
						this.ngOnInit();
					}
					else {
						this.ngOnInit();
						this.layoutUtilsService.showError(res.error.message);
					}
				})
			}
		});
	}

	Viewdetail(value) {
		this.router.navigate(['', { outlets: { auxName: 'aux/detail/'+value.id_row }, }]);
		// var item: any = {};
		// item.id_row = value.replace("W", "");
		// item.id_project_team = this.ID_Project;
		// // this.DataID = this.data.id_row;
		// // this.Id_project_team = this.data.id_project_team;
		// const dialogRef = this.dialog.open(WorkListNewDetailComponent, {
		// 	width: '90vw',
		// 	height: '90vh',
		// 	data: item
		// });

		// dialogRef.afterClosed().subscribe(result => {
		// 	this.ngOnInit();
		// });
	}

	test() {
	}

	updateView(value) {
		this.view = value;
	}

}
