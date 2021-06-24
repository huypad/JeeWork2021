import { UserProfileService } from './../../../../_metronic/jeework_old/core/auth/_services/user-profile.service';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service'; 
import { WeWorkService } from './../../services/wework.services';
import { Component, OnInit, ChangeDetectionStrategy, ChangeDetectorRef } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { MatDialog } from '@angular/material/dialog';
import { UserService } from '../Services/user.service';
import { Moment } from 'moment';
import * as moment from 'moment';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { PlatformLocation } from '@angular/common';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { AuthorizeModel } from '../Model/user.model';
import { AuthorizeEditComponent } from '../authorize-edit/authorize-edit.component'; 

@Component({
	selector: 'kt-user-detail',
	templateUrl: './user-detail.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class UserDetailComponent implements OnInit {
	UserID: number;
	item: any;
	TuNgay: Moment;
	DenNgay: Moment;
	id_project_team: number = 0;
	data: any = [];
	selectedItem: any;
	isprint: boolean = true;
	profile: any;
	Ten: string = '';
	ChucVu: string = '';
	Image: string = '';
	Is_authorize: boolean = false;
	list_authorize: any  = [];
	projects: any = [];
	Count: any = [];
	staffs: any = [];
	uyquyens: any = [];
	image: any = [];
	hoten: any = [];
	tenchucdanh: any = [];
	UserID_authorize: number = 0;
	customStyle:any = {};
	keyword:string = "";
	constructor(
		public _Services: UserService,
		private changeDetect: ChangeDetectorRef,
		private translate: TranslateService,
		private router: Router,
		private layoutUtilsService: LayoutUtilsService,
		public dialog: MatDialog,
		private activatedRoute: ActivatedRoute,
		location: PlatformLocation,
		private tokenStore: TokenStorage,
		public WeWorkService: WeWorkService,
		private userProfileService: UserProfileService,
	) {
		location.onPopState(() => {
			this.close_detail();
		});
	}
	ngOnInit() {
		let id: any;

		this.activatedRoute.params.subscribe(res => {
			var now = moment();
			this.DenNgay = now;
			this.TuNgay = moment(now).add(-1, 'months');
			this.UserID = res.idu;
			this._Services.Detail(this.UserID).subscribe(res => {
				if (res && res.status == 1)
					this.item = res.data;
				this.projects = this.item.projects;
				this.Count = this.item.Count;
				this.staffs = this.item.staffs;
				this.uyquyens = this.item.uyquyens;
				this.hoten = this.item.hoten;
				this.tenchucdanh = this.item.tenchucdanh;
				this.image = this.item.image;
				this.loadDataList();
				this.changeDetect.detectChanges();
			});
		});
		this.tokenStore.getIDUser().subscribe(res => {
			id = +res;
			// this.userProfileService.getHinhAnhByID(+id).subscribe(res => {

			// 	this.profile = res;
			// 	let UserData = {
			// 		HoTen: res.HoTen,
			// 		Image: res.Image,
			// 		ChucVu: res.ChucVu,
			// 		Username: localStorage.getItem('Username')
			// 	};
			// 	this.tokenStore.setUserData(UserData);
			// 	if (this.profile == undefined) {
			// 	}
			// 	else {
			// 		this.Image = this.profile.Image;
			// 		this.Ten = this.profile.HoTen;
			// 		this.ChucVu = this.profile.ChucVu;
			// 	}
			// 	this.changeDetect.detectChanges();
			// });
		});
		if (this.UserID == id)
			this.Is_authorize = true;
		this.loadDataAuthorize();
	}
	loadDataList() {
		const queryParams = new QueryParamsModelNew(
			this.filterConfiguration(),
			'',
			'',
			0,
			50,
			true
		);
		this.data = [];
		this._Services.findDataWork(queryParams).subscribe(res => {
			if (res && res.status === 1) {
				this.data = res.data;
			}
			this.changeDetect.detectChanges();
		});
	}
	export() {
		const queryParams = new QueryParamsModelNew(
			this.filterConfiguration(),
			'',
			'',
			0,
			50,
			true
		);
		this._Services.ExportExcelByUsers(queryParams).subscribe(response => {
			var headers = response.headers;
			let filename = headers.get('x-filename');
			let type = headers.get('content-type');
			let blob = new Blob([response.body], { type: type });
			const fileURL = URL.createObjectURL(blob);
			var link = document.createElement('a');
			link.href = fileURL;
			link.download = filename;
			link.click();
			//window.open(fileURL, '_blank');
		});
	}
	filterConfiguration(): any {
		const filter: any = { id_nv: this.UserID };
		// filter.TuNgay = this.TuNgay.format("DD/MM/YYYY");
		// filter.DenNgay = this.DenNgay.format("DD/MM/YYYY");
		if (this.id_project_team > 0) {
			filter.id_project_team = this.id_project_team;
		}
		return filter;
	}
	selected($event) {
		this.selectedItem = $event;
		let temp: any = {};
		temp.Id = this.selectedItem.id_row;

		var url = '/users/' + this.UserID + '/detail/' + temp.Id;
		this.router.navigateByUrl(url);
	}
	close_detail() {
		this.selectedItem = undefined;
		if (!this.changeDetect['destroyed'])
			this.changeDetect.detectChanges();
	}
	getPercentWork(value, total) {
		if (total == 0) {
			return 0;
		}
		return (value / total) * 100 + '%';
	}
	in() {
		this.isprint = false;
		this.changeDetect.detectChanges();
		window.print();
		this.isprint = true;
	}
	loadDataAuthorize() {
		const queryParams = new QueryParamsModelNew(
			'',
			'',
			'',
			0,
			50,
			true
		);
		this._Services.find_ListAuthorize(queryParams).subscribe(res => {
			if (res && res.status === 1) {

				this.list_authorize = res.data;
				console.log(res.data);
				if(this.list_authorize && this.list_authorize[0])
					this.UserID_authorize = this.list_authorize[0].id_user;
			}
			this.changeDetect.detectChanges();
		});
	}
	uyquyen() {
		let saveMessageTranslateParam = '';
		var _item = new AuthorizeModel();
		_item.clear();
		_item.id_user = this.UserID_authorize;
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(AuthorizeEditComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.ngOnInit();
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.ngOnInit();
			}
		});
	}

	getHeight() {
		let tmp_height = 0;
		tmp_height = window.innerHeight -this.tokenStore.getHeightHeader();
		return tmp_height + 'px';
	}
}
