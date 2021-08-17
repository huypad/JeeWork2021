import { DanhMucChungService } from './../../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { LayoutUtilsService } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Inject, HostListener, Input, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
// Services
// Models
import { AbstractControl, FormBuilder, FormControl, FormGroup, Validators } from '@angular/forms';
import { WeWorkService } from '../../../services/wework.services';
import { WorkService } from '../../../work/work.service';
import { SelectionModel } from '@angular/cdk/collections';

@Component({
	selector: 'kt-import',
	templateUrl: './import.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class ImportComponent {
	id_project_team: number;
	item: any;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	listdata = [1, 2, 3, 4, 5, 6];
	horizontalLayoutActive: boolean = false;
	private currentDraggableEvent: DragEvent;
	//=======================================================================
	// Filter fields
	selectedValue: any;
	IndexField: string = '';
	selection = new SelectionModel<any>(true, []);
	productsResult: any[] = [];
	ObjImage: any = { h1: "", h2: "" };
	Image: any;
	ShowDungGio: boolean = false;
	//===========Tab 1
	listColumn: any[] = [];
	listColumnABC_Add: any[] = [];
	listColumnABC: any[] = ['A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'AA', 'AB', 'AC', 'AD', 'AE', 'AF', 'AG', 'AH', 'AI', 'AJ', 'AK', 'AL', 'AM', 'AN', 'AO', 'AP', 'AQ', 'AR', 'AS', 'AT', 'AU', 'AV', 'AW', 'AX', 'AY', 'AZ'];
	// listColumnABC: any[] = [{id:"A"},{id:"B"},{id:"C"},{id:"D"},{id:"E"},{id:"F"},{id:"G"}];
	listFieldNameByPageName: any[] = [];
	showApDung: boolean = false;
	//===========Tab 2
	listColumnSort: any[] = [];
	listFieldNameSortByPageName: any[] = [];
	showApDungSort: boolean = false;
	showBTCot: boolean = true;
	showBTSort: boolean = false;
	isShowINFO: boolean = false;
	isShowSEO: boolean = false;
	thongbaoloi: string = '';
	updatecodeauto: boolean = false;
	updatecodeold: boolean = false;
	updatefields: boolean = false;
	isPremium: boolean = false;

	TenFile: string = '';
	ID_NV: string = '';
	id_menu: number = 40632;
	@ViewChild('tensheet', { static: true }) tensheet: ElementRef;
	displayedColumns: string[] = ['position', 'name', 'weight', 'symbol'];
	dataSource: any[] = [];
	constructor(
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: WorkService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		private danhMucChungService: DanhMucChungService,
		public weworkService: WeWorkService,
		private router: Router) { }
	/** LOAD DATA */
	ngOnInit() {
		var arr = this.router.url.split("/");
		this.id_project_team = +arr[2];



	}

	download() {
		window.open(this._service.DownloadFileImport());
		// this._service.DownloadFileImport().subscribe(res => {
		// 	if (res && res.status == 1)
		// 		window.open(res.file);
		// });
	}
	Import(preview: boolean = false) {
		if (!this.Image) {
			this.layoutUtilsService.showError(this.translate.instant('notify.vuilongchonfiledulieu'));
			return;
		}
		let data = {
			"File": this.Image.strBase64,
			"FileName": this.TenFile,
			"Sheet": "Sheet1",
			"id_project_team": this.id_project_team,
			"review": preview
		}
		if (preview)
			this.dataSource = [];
			this.layoutUtilsService.showWaitingDiv();
			this._service.ImportData(data).subscribe(res => {
				this.layoutUtilsService.OffWaitingDiv();
				if (res && res.status == 1) {
					if (preview)
						this.dataSource = res.data;
					else
						this.layoutUtilsService.showInfo(this.translate.instant('notify.importthanhcong') +" " + res.data.success + " "+this.translate.instant('notify.trongso')+" " + res.data.total);
				}
				this.changeDetectorRefs.detectChanges();
			});
	}
	FileSelected(evt: any) {
		if (evt.target.files && evt.target.files.length) {//Nếu có file
			let file = evt.target.files[0]; // Ví dụ chỉ lấy file đầu tiên
			this.TenFile = file.name;
			let reader = new FileReader();
			reader.readAsDataURL(evt.target.files[0]);
			let base64Str;
			// var b = this.indexItem;

			setTimeout(() => {
				base64Str = reader.result as String;
				var metaIdx = base64Str.indexOf(';base64,');
				base64Str = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64
				this.ObjImage.h1 = base64Str;

			}, 1000);
			var hinhanh: any = {};
			setInterval(() => {
				//let v = localStorage.getItem("h" + b);
				let v;
				v = this.ObjImage.h1

				if (v) {
					if (v != "") {
						hinhanh.strBase64 = v;
						this.changeDetectorRefs.detectChanges();
						this.ObjImage.h1 = ""
					}
					if (hinhanh.strBase64 != "") {
						this.Image = hinhanh;
						if (this.Image.strBase64 != '') { this.isShowINFO = true; }
						this.changeDetectorRefs.detectChanges();
					}
				}
			}, 100);
		}
		else {
			this.Image.strBase64 = "";
		}
	}
	onDrop_Columns($event, listColumn)
	{

	}
}
