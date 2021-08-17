import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Inject, HostListener, Input, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatPaginator,PageEvent } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatDialog,MatDialogRef,MAT_DIALOG_DATA } from '@angular/material/dialog';
// RXJS
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { TranslateService } from '@ngx-translate/core';
// Services
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Models
import { CdkDragDrop, moveItemInArray, transferArrayItem } from '@angular/cdk/drag-drop';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { ListDepartmentService } from '../Services/List-department.service';
import { MilestoneModel } from '../Model/List-department.model';
import { milestoneDetailEditComponent } from '../milestone-detail-edit/milestone-detail-edit.component';


@Component({
	selector: 'kt-milestone-detail',
	templateUrl: './milestone-detail.component.html',
	styleUrls: ['./milestone-detail.component.scss'],
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class MilestoneDetailComponent {
	@Input() TenQuyTrinh: any;
	ID_milestone: number = 0;
	ListData: any[] = [];
	//1: Tạo ; 2: Chuyển đến; 3: Chuyển ngược; 4: Xóa; 5: Giao người thực hiện; 6: Chọn người theo dõi; 7:Thực hiện nhiệm vu; 8: Tạm dừng; 9:Hoàn tất;
	//10: Đanh dấu hoàn thành; 12: Thất bai
	//=================PageSize Table=====================
	pageEvent: PageEvent;
	pageSize: number;
	pageLength: number;
	item: any = {};
	percentage: any;
	tong: any;
	htdunghan: any;
	htquahan: any;
	quahan: any;
	danglam: any;
	percentage_mt: any;
	tong_mt: any;
	htdunghan_mt: any;
	htquahan_mt: any;
	quahan_mt: any;
	danglam_mt: any;
	image: any='';
	hoten: any='';
	username: any='';
	mobile: any='';
	Disabled_checkall: boolean = true;
	Disabled_DuHoSo: boolean = true;
	Disabled_Item: boolean = true;
	chk_all: boolean = true;
	chk_duhoso: boolean = false;
	list_of_task: any[] = [];
	public listStatus: any[] = [
		{ ID: 'is_danglam', Title: this.translate.instant('projects.dangthuchien'), Checked: false },
		{ ID: 'is_quahan', Title: this.translate.instant('filter.quahan'), Checked: false },
		{ ID: 'is_htquahan', Title: this.translate.instant('filter.htquahan'), Checked: false },
		{ ID: 'is_htdunghan', Title: this.translate.instant('filter.htdunghan'), Checked: false },
	];
	@ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
	constructor(public _deptServices: ListDepartmentService,
		public dialog: MatDialog,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private activatedRoute: ActivatedRoute,
		private changeDetectorRefs: ChangeDetectorRef,
		private router: Router,
		private tokenStorage: TokenStorage,) {
	}

	ngOnInit() {
		this.activatedRoute.params.subscribe(params => {
			this.ID_milestone = +params.id;
		});
		this.LoadMilestone_Detail();
	}
	getColorProgressbar(status: number = 0): string {
		if (status < 50)
			return 'warn';
		else
			if (status < 100)
				return 'info';
			else
				return 'success';
	}
	CountByMucTieu: any = {};
	Count: any = {};
	person_in_charge: any = {};
	LoadMilestone_Detail() {
		this._deptServices.Get_MilestoneDetail(this.ID_milestone).subscribe(res => {
			if (res && res.status == 1) {
				this.item = res.data;
				this.CountByMucTieu = this.item.CountByMucTieu;
				this.Count = this.item.Count;
				this.list_of_task = res.data.List;
				this.person_in_charge = this.item.person_in_charge;
				this.percentage = this.Count.percentage;
				this.tong = this.Count.tong;
				this.htdunghan = this.Count.htdunghan;
				this.htquahan = this.Count.htquahan;
				this.quahan = this.Count.quahan;
				this.danglam = this.Count.danglam;
				this.tong_mt = this.CountByMucTieu.tong;
				this.htdunghan_mt = this.CountByMucTieu.htdunghan;
				this.htquahan_mt = this.CountByMucTieu.htquahan;
				this.quahan_mt = this.CountByMucTieu.quahan;
				this.danglam_mt = this.CountByMucTieu.danglam;
				this.image = this.person_in_charge.image;
				this.hoten = this.person_in_charge.hoten;
				this.username = this.person_in_charge.username;
				this.mobile = this.person_in_charge.mobile;
				if (!this.chk_all) {
					this.filterList();
				}
				this.changeDetectorRefs.detectChanges();
			} else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		})
	}

	loadDataList(page: boolean = false) {
		const queryParams = new QueryParamsModelNew(
			this.filterConfiguration(),
			'',
			'',
			page ? this.paginator.pageIndex : this.paginator.pageIndex = 0,
			this.paginator.pageSize
		);

		this.layoutUtilsService.showWaitingDiv();
		this._deptServices.getActivityLog(queryParams)
			.pipe(
				tap(resultFromServer => {
					this.layoutUtilsService.OffWaitingDiv();
					this.pageLength = resultFromServer.page.TotalCount;
					if (resultFromServer.status == 1) {
						if (resultFromServer.data.length > 0) {

							this.ListData = resultFromServer.data;
						}
						else {
							this.ListData = [];
						}
					}
					else {
						this.ListData = [];
					}
					this.changeDetectorRefs.detectChanges();
				})
			).subscribe();;
	}

	filterConfiguration(): any {

		const filter: any = {};
		// filter.ProcessID = this.ID_QuyTrinh;
		filter.ProcessID = '3';

		return filter;
	}

	getMatIcon(item: any): string {
		let _icon = '';

		if (item.is_quahan > 0) {
			_icon = 'watch_later';
		} else
			if (item.is_danglam > 0 || item.is_htquahan > 0) {
				_icon = 'check_circle';
			} else {
				_icon = 'watch_later';
			}
		return _icon;
	}
	Checked(id: string, check: any) {
		this.chk_all = false;
		this.listStatus.forEach(res => {
			if (res.ID == id) {
				res.Checked = check;
				return;
			}
		})
		this.LoadMilestone_Detail();
	}

	filterList() {
		this.item.List = this.item.List.filter(element => {
			for (let i of this.listStatus) {
				if (i.Checked) {
					if (element[i.ID] == 1) {
						return element;
					}
				}
			}
		});
	}
	buildColor(_color) {
		return (_color && _color.is_htquahan == 1) ? 'rgb(255, 193, 7)' : (_color && _color.is_htdunghan == 1) ? 'rgb(76, 175, 80)' : (_color && _color.is_quahan == 1) ? 'rgb(244, 67, 54)' : 'rgb(63, 81, 181)';
	}
	GetListCheck(): any {

		var chuoicheck = "";
		for (var i = 0; i < this.listStatus.length; i++) {
			if (this.listStatus[i].Checked) {
				chuoicheck += "," + this.listStatus[i].ID;
			}
		}
		chuoicheck = chuoicheck.substring(1);
		return chuoicheck;
	}

	ViewDetail(id_row){
		this.router.navigate(['', { outlets: { auxName: 'aux/detail/'+ id_row }, }]);
		// this.ProjectsTeamService.WorkDetail(id_row).subscribe(res => {
		// 	if (res && res.status == 1) {
		// 		const dialogRef = this.dialog.open(WorkListNewDetailComponent, {
		// 			width: '90vw',
		// 			height: '90vh',
		// 			data: res.data,
		// 		  });

		// 		  dialogRef.afterClosed().subscribe(result => {
		// 			  this.ngOnInit();
		// 		  });
		// 	}
		// 	else this.layoutUtilsService.showError(res.error.message)
		// });
	}

	CheckedAll() {
		this.listStatus = [
			{ ID: 'is_danglam', Title: this.translate.instant('projects.dangthuchien'), Checked: false },
			{ ID: 'is_quahan', Title: this.translate.instant('filter.quahan'), Checked: false },
			{ ID: 'is_htquahan', Title: this.translate.instant('filter.htquahan'), Checked: false },
			{ ID: 'is_htdunghan', Title: this.translate.instant('filter.htdunghan'), Checked: false },
		];
		this.chk_all = true;
		this.LoadMilestone_Detail();
	}
	goBack() {
		window.history.back();
	}
	// get quan trọng khẩn cấp: 1:quan trọng khẩn cấp,2:quan trọng,3 khẩn cấp, 4 bình thường
	getItemCssClassByLocked(status: number): string {
		if(status < 2 && status > 0){
			return 'success';
		}
	}
	getItemLockedString(condition: number): string {
		if(condition < 2 && condition > 0){
			return this.translate.instant('filter.quantrong');
		}
	}
	getItemCssClassByOverdue(status: number = 0): string {

		switch (status) {
			case 1:
				return 'metal';
		}
	}
	getItemOverdue(condition: number): string {
		switch (condition) {
			case 1:
				return 'Overdue';
		}
	}


	getItemCssClassByurgent(status: number): string {

		switch (status) {
			case 3:
				return 'brand';
			case 1:
				return 'brand';
		}
	}
	getItemurgent(condition: number): string {
		switch (condition) {
			case 3:
				return this.translate.instant('filter.khancap');
			case 1:
				return this.translate.instant('filter.khancap');
		}
	}
	Add() {
		const ObjectModels = new MilestoneModel();
		ObjectModels.clear(); // Set all defaults fields
		this.Update(ObjectModels);
	}
	Update(_item: MilestoneModel) {
		let saveMessageTranslateParam = '';
		_item.id_row = this.item.id_row;
		_item.title = this.item.title;
		_item.description = this.item.description;
		_item.deadline = this.item.deadline;
		_item.id_project_team = this.item.id_project_team;
		_item = this.item;
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(milestoneDetailEditComponent, { data: { _item } });

		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.ngOnInit();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.changeDetectorRefs.detectChanges();
			}
		});
	}

	options = {
		cutoutPercentage: 80,
		tooltips: { enabled: false },
		hover: { mode: null },
	};


}
