import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { LayoutUtilsService, MessageType } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Inject, HostListener, Input, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
// Services
// Models
import { AbstractControl, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { WeWorkService } from '../../../services/wework.services';
import { ProjectsTeamService } from '../../Services/department-and-project.service';
import { TranslationService } from 'src/app/modules/i18n/translation.service';
import { ConfigNotifyModel } from '../../Model/department-and-project.model';

@Component({
	selector: 'kt-config-notify',
	templateUrl: './config-notify.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class ConfigNotifyComponent {
	item1: any = {};
	IsProject: boolean;
	id_project_team: number = 0;
	listconfig: any[] = [];
	langcode = 'vi';
	constructor(
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: ProjectsTeamService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: WeWorkService,
		private router: Router) { }
	/** LOAD DATA */
	ngOnInit() {
		// this.translate.use(this.translate.getDefaultLang());
		// this.translate.use(lang);
		// localStorage.setItem('language', lang);
		var arr = this.router.url.split("/");
		this.id_project_team = +arr[2];
		this.LoadDataList();
	}
	LoadDataList() {
		this.langcode = localStorage.getItem('language');
		if (this.langcode == null)
			this.langcode = this.translate.getDefaultLang();
		debugger
		this._service.get_list_config(this.id_project_team, this.langcode).subscribe(res => {
			this.listconfig = res.data;
			this.changeDetectorRefs.detectChanges();
			// this.refreshDataSource();
		});
	}
	applySelectedColumns() {
		// const _selectedColumns: string[] = [];
		// this.selectedColumns.selected.sort((a, b) => { return a.stt > b.stt ? 1 : 0; }).forEach(col => { _selectedColumns.push(col.name) });
		// this.displayedColumns = _selectedColumns;
	}

	refreshDataSource() {
		// this.dataSource = new MatTableDataSource(this.listThongBao);
		// this.changeDetectorRefs.detectChanges();
		// this.selection.clear();
		// for (var i = 0; i < this.dataSource.data.length; i++) {
		// 	this.selection.select(this.dataSource.data[i]);
		// }
		// this.changeDetectorRefs.detectChanges();
	}
	ChangIsNotify(val: any, row: any, isnotify: boolean): void {
		let list: ConfigNotifyModel[] = [];
		let _prod: ConfigNotifyModel = new ConfigNotifyModel();
		_prod.id_row = row.id_row;
		_prod.id_project_team = this.id_project_team;
		_prod.values = val.checked;
		if (isnotify) {
			_prod.isnotify = true;
			_prod.isemail = false;
		}
		else {
			_prod.isnotify = false;
			_prod.isemail = true;
		}
		// list.push(_prod);
		debugger
		this._service.save_notify(_prod).subscribe(res => {
			if (res && res.status == 1) {
				this.LoadDataList();
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
			}
		});
	}

	ChangeIsEmail(val: any, row: any) {
		// this.loadingSubject.next(true);
		// let list: ConfigNotifyByEmailModel[] = [];
		// let _prod: ConfigNotifyByEmailModel = new ConfigNotifyByEmailModel();
		// _prod.ID = row.NotifyID
		// _prod.On = val.checked;
		// list.push(_prod);

		// this.configNotifyByEmailService.CreateConfigNotifyByEmail(list).subscribe(res => {
		// 	if (res && res.status == 1) {
		// 		this.LoadDataList();
		// 	}
		// 	else {
		// 		this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 999999999, true, false, 3000, 'top', 0);
		// 	}
		// 	this.loadingSubject.next(true);
		// });
	}
	Setting(val: any) {
		// if (val.EmailID == "ContractExpire") {
		// 	this.danhMucService.CheckRole(69).subscribe(res => {
		// 		if (res.status == 0) {
		// 			this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 10000, true, false, 3000, 'top', 0);
		// 			return;
		// 		} else {
		// 			const dialogRef = this.dialog.open(CauHinhNhacNhoHopDongComponent, {
		// 				data: {
		// 					_id: val.ID,//204
		// 					_title: val.Title,
		// 					_status: res.status
		// 				},
		// 				width: '40%'
		// 			});
		// 			dialogRef.afterClosed().subscribe(res => {
		// 			});
		// 		}
		// 	});
		// } else if (val.EmailID == "StaffListOff") {
		// 	const dialogRef = this.dialog.open(CauHinhThoiGianCaLamViecListComponent, {
		// 		data: {
		// 		},
		// 		width: '40%'
		// 	});
		// 	dialogRef.afterClosed().subscribe(res => {
		// 	});
		// } else {

		// }

	}

	SettingNguoiNhan(val: any) {
		// const dialogRef = this.dialog.open(CauHinhNguoiNhanListComponent, {
		// 	data: {
		// 		_id: val.ID,
		// 		_title: val.Title,
		// 	},
		// 	width: '40%'
		// });
		// dialogRef.afterClosed().subscribe(res => {
		// });
	}
}
