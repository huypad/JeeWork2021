import { AuthService } from './../../../../modules/auth/_services/auth.service';
import { TokenStorage } from './../auth/_services/token-storage.service';
import { HttpUtilsService } from './../utils/http-utils.service';
import { environment } from 'src/environments/environment';
import { Injectable } from '@angular/core';
import { FormGroup, FormBuilder } from '@angular/forms';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
// import { Idle, DEFAULT_INTERRUPTSOURCES } from '@ng-idle/core';
import { QueryParamsModel } from '../models/query-models/query-params.model';
import { LayoutUtilsService } from '../utils/layout-utils.service';
import { QueryResultsModel } from '../models/query-models/query-results.model';
// import { AuthService } from 'app/core/auth';

@Injectable()
export class CommonService {
	Form: FormGroup;
	fixedPoint: number = 0;

	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	lastFilterDSExcel$: BehaviorSubject<any[]> = new BehaviorSubject([]);
	lastFilterInfoExcel$: BehaviorSubject<any> = new BehaviorSubject(undefined);
	lastFileUpload$: BehaviorSubject<{}> = new BehaviorSubject({});
	data_import: BehaviorSubject<any[]> = new BehaviorSubject([]);
	ReadOnlyControl: boolean;

	constructor(private layoutUtilsService: LayoutUtilsService,
		private http: HttpClient,
		private httpUtils: HttpUtilsService,
		private fb: FormBuilder,
		private tokenStorage: TokenStorage,
		// private idle: Idle,
		private auth: AuthService, ) { }

	ValidateChangeNumberEvent(columnName: string, item: any, event: any) {
		var count = 0;
		for (let i = 0; i < event.target.value.length; i++) {
			if (event.target.value[i] == '.') {
				count += 1;
			}
		}
		var regex = /[a-zA-Z -!$%^&*()_+|~=`{}[:;<>?@#\]]/g;
		var found = event.target.value.match(regex);
		if (found != null) {
			const message = 'Dữ liệu không gồm chữ hoặc kí tự đặc biệt';
			this.layoutUtilsService.showError(message);
			return false;;
		}
		if (count >= 2) {
			const message = 'Dữ liệu không thể có nhiều hơn 2 dấu .';
			this.layoutUtilsService.showError(message);
			return false;;
		}
		return true;
	}

	/**
	 * Phonenumber: type= 'phone'
	 * Domain: type= 'domain'
	 * fax: type='fax'
	 */
	ValidateFormatRegex(type: string): any {
		if (type == 'phone') {
			return /[0][0-9]{9}/;
		}
		if (type == 'domain') {
			return /^[a-zA-Z0-9][a-zA-Z0-9-]{1,61}[a-zA-Z0-9]\.[a-zA-Z]{2,}$/;
		}
		if (type == 'fax') {
			return /[0][0-9]{9}/; //^(\+?\d{1,}(\s?|\-?)\d*(\s?|\-?)\(?\d{2,}\)?(\s?|\-?)\d{3,}\s?\d{3,})$
		}
		if (type == 'password') {
			return /^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$/; //^(\+?\d{1,}(\s?|\-?)\d*(\s?|\-?)\(?\d{2,}\)?(\s?|\-?)\d{3,}\s?\d{3,})$
		}
		if (type == 'integer')
			return /^-?[0-9][^\.]*$/;
	}

	formatNumber(item: string) {
		if (item == '' || item == null || item == undefined) return '';
		return Number(Math.round(parseFloat(item + 'e' + this.fixedPoint)) + 'e-' + this.fixedPoint).toFixed(this.fixedPoint)
	}

	f_currency(value: string): any {
		if (value == null || value == undefined || value == '') value = '0.00';
		let nbr = Number((value.substring(0, value.length - (this.fixedPoint + 1)) + '').replace(/,/g, ""));
		return (nbr + '').replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,") + value.substr(value.length - (this.fixedPoint + 1), (this.fixedPoint + 1));
	}

	f_currency_V2(value: string): any {
		if (value == '-1') return '';
		if (value == null || value == undefined || value == '') value = '0';
		let nbr = Number((value + '').replace(/,/g, ""));
		return (nbr + '').replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,");
	}

	numberOnly(event): boolean {
		const charCode = (event.which) ? event.which : event.keyCode;
		if (charCode > 31 && (charCode < 48 || charCode > 57)) {
			return false;
		}
		return true;
	}

	getFormatDate(v: string = '') {
		if (v != '') {
			return v.includes('T') ? v.replace(/(\d{4})(-)(\d{2})(-)(\d{2})(T)(\d{2})(:)(\d{2})(:)(\d{2}).*$/g, "$5/$3/$1") : v.replace(/(\d{4})(-)(\d{2})(-)(\d{2})/g, "$5/$3/$1");
		}
		return '';
	}

	f_convertDate(v: any = "") {
		let a = v === "" ? new Date() : new Date(v);
		return a.getFullYear() + "-" + ("0" + (a.getMonth() + 1)).slice(-2) + "-" + ("0" + (a.getDate())).slice(-2) + "T00:00:00.0000000";
	}

	//#region form helper
	buildForm(data: any) {
		this.Form = this.fb.group(data);
	}
	/**
	 * Checking control validation
	 *
	 * @param controlName: string => Equals to formControlName
	 * @param validationType: string => Equals to valitors name
	 */
	isControlHasError(controlName: string, validationType: string): boolean {
		const control = this.Form.controls[controlName];
		if (!control) {
			return false;
		}

		const result = control.hasError(validationType) && (control.dirty || control.touched);
		return result;
	}
	//#endregion

	//#region file đính kèm
	public download_dinhkem(Id: number): Observable<any> {
		var _token = '';
		this.tokenStorage.getAccessToken().subscribe(t => { _token = t; });
		let headers = new HttpHeaders({
			'Authorization': 'Bearer ' + _token,
		})
		headers.append("Content-Type", "multipart/form-data");
		return this.http.get(environment.ApiRootsLanding + '/lite/download-dinhkem?id=' + Id, { headers });//, responseType: 'blob' 
	}

	view_dinhkem(Id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/view-dinhkem?id=' + Id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	get_dinhkem(Loai: number, Id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/get-dinhkem?loai=' + Loai + '&id=' + Id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion

	//#region ckeditor
	ckeditor: any = {
		config: {
			language: 'vi',
			uiColor: '#FF8F35',
			// Define the toolbar: https://ckeditor.com/docs/ckeditor4/latest/features/toolbar.html
			// The standard preset from CDN which we used as a base provides more features than we need.
			// Also by default it comes with a 2-line toolbar. Here we put all buttons in a single row.
			toolbar: [
				{ name: 'document', items: ['Print'] },
				{ name: 'clipboard', items: ['Undo', 'Redo'] },
				{ name: 'styles', items: ['Format', 'Font', 'FontSize'] },
				{ name: 'basicstyles', items: ['Bold', 'Italic', 'Underline', 'Strike', 'RemoveFormat', 'CopyFormatting'] },
				{ name: 'colors', items: ['TextColor', 'BGColor'] },
				{ name: 'align', items: ['JustifyLeft', 'JustifyCenter', 'JustifyRight', 'JustifyBlock'] },
				{ name: 'links', items: ['Link', 'Unlink'] },
				{ name: 'paragraph', items: ['NumberedList', 'BulletedList', '-', 'Outdent', 'Indent', '-', 'Blockquote'] },
				{ name: 'insert', items: ['Image', 'Table'] },
				{ name: 'tools', items: ['Maximize', 'Source'] },
				{ name: 'editing', items: ['Scayt'] }
			],
			extraPlugins: 'colordialog,tableresize',
			// Make the editing area bigger than default.
			height: 300,
		},
		editorUrl: './assets/plugins/ckeditor/ckeditor.js'
	}
	//#endregion

	//#region ***filter***
	getFilterGroup(column: string, url: string): Observable<any> {
		return this.http.get<any>(environment.ApiRootsLanding + url + `${column}`);
	}
	//#endregion

	//#region ds lite
	ListLoaiChungThu(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/loai-chung-thu';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	ListGioiTinh(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/gioi-tinh';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	getListNhomNguoiDung(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/nhom-nguoi-dung';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	ListChucVu(Id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DM_ChucVu?Id=' + Id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	TreeChucVu(dv, locked: boolean = false): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/lite/tree-chuc-vu?Donvi=${dv}&Locked=${locked}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}

	//#endregion


	//#region quyền
	getTreeQuyen(itemId: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/lite/tree-quyen?IdGroup=${itemId}`, { headers: httpHeaders });
	}
	CheckRole(IDRole): any {
		var roles = JSON.parse(localStorage.getItem('userRoles'));
		if(roles)
			return roles.filter(x => x == IDRole);
		return [];
	}
	CheckRole_WeWork(IDRole): any {
		var roles = JSON.parse(localStorage.getItem('WeWorkRoles'));
		if(roles)
			return roles.filter(x => x == IDRole);
		return [];
	}
	//#endregion

	//#region danh mục DM_DanhMuc
	getLoaiDanhMuc(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/loai-danh-muc';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DanhMucKhac(Id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=' + Id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMLoaiDonVi(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=1';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMLoaiTaiNguyen(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=2';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMLoaiSo(iddv: number = 0, locked: boolean = false, IdRequired: number = 0): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/lite/danh-muc-theo-user?loai=3&iddv=${iddv}&Locked=${locked}&IdRequired=${IdRequired}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMLoaiVanBan(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=4';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMDotNghi(iddv: number = 0, locked: boolean = false, IdRequired: number = 0): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/lite/danh-muc-theo-user?loai=5&iddv=${iddv}&Locked=${locked}&IdRequired=${IdRequired}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMLoaiDoiTuong(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=6';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMDoKhan(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=7';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMDoMat(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=8';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMPhuongThucNhan(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=9';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMLoaiCongViec(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=10';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMLinhVuc(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=11';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMThoiHan(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=12';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMHinhThucCV(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=13';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMDoUuTien(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-muc-theo-user?loai=14';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DM_YKien_Lite(queryParams): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DM_YKien_Lite';
		const httpParms = this.httpUtils.getFindHTTPParams(queryParams)
		return this.http.post<any>(url, httpParms, { headers: httpHeaders });
	}
	DM_SoVanBan(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DM_SoVanBan';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion

	//#region Vai trò
	ListVaiTroByDonVi(Id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/vai-tro?IdDonVi=' + Id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	ListVaiTroAll(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/vai-tro';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	ListVaiTroPhanTrang(queryParams: QueryParamsModel): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		return this.http.post<any>(environment.ApiRootsLanding + '/nhom-nguoi-dung/list', httpParams, { headers: httpHeaders });
	}
	//#endregion

	//#region hồ sơ
	DanhMucHoSo(Id, iddv: number = 0, locked: boolean = false, idrequired: number = 0): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/lite/DM_HoSo_Lite?Parent=${Id}&iddv=${iddv}&Locked=${locked}&IdRequired=${idrequired}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	TreeHoSo(DonVi: number = 0, locked: boolean = false): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/lite/Lite_HoSoTree?DonVi=${DonVi}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion

	//#region đơn vị

	GetTreeDonVi() {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/lite/LayTreeDonVi`, { headers: httpHeaders });
	}
	TreeDonVi(type: number = 0, idParent: number = 0, locked: boolean = false): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		var url = environment.ApiRootsLanding + `/lite/DM_PhongBan_Tree?Id=${idParent}&Locked=${locked}`;
		if (type == 1) {//đơn vị liên thông
			url = environment.ApiRootsLanding + '/lite/DM_DonViLienThong_Tree';
		}
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	TreeDonViByParent(Id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DM_PhongBan_Tree?Id=' + Id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion

	//#region đv liên thông
	DanhMucDonViLienThong(locked: boolean = false, idrequired: number = 0): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/lite/DM_DonViLienThong_Lite?Locked=${locked}&IdRequired=${idrequired}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	TreeDonViLienThong(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/Lite_DonViLienThongTree';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion

	//#region đv ngoài
	DanhMucDonViNgoai(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DM_DonViNgoai_Lite';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	TreeDonViNgoai(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/Lite_DonViNgoaiTree';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion

	//#region user
	GetThongBao(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/user/GetThongBao`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	GetThongBaoLastest(lastID: string): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/user/GetThongBao?lastid=${lastID}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	GetThongBaoPage(pagesize:number, pageindex: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/user/GetThongBaoPage?more=false&record=${pagesize}&page=${pageindex}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	GetThongBaoNoiBo(pagesize:number, pageindex: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/user/GetThongBaoNoiBo?more=false&record=${pagesize}&page=${pageindex}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	ReadNotify(Id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/user/ReadNotify?Id='+Id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	GetInfoUser(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/user/GetInfoUser';
		return this.http.get<any>(url, { headers: httpHeaders });
	}

	UpdateInfoUser(Item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/user/UpdateInfoUser';
		return this.http.post<any>(url, Item, { headers: httpHeaders });
	}

	changePassword(data): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/user/change-password';
		return this.http.post<any>(url, data, { headers: httpHeaders });
	}
	//#endregion

	//#region quy trình
	excute(data): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		if (data.Method == "get") {
			let url = "";
			if (data.DefaultValues) {
				for (var i = 0; i < data.DefaultValues.length; i++) {
					url += !url ? "?" : "&"
					url += data.DefaultValues[i].ColumnName + "=" + data.DefaultValues[i].DefaultValue;
					if (data.DefaultValues[i].ColumnNameAs) {
						url += !url ? "?" : "&"
						url += data.DefaultValues[i].ColumnNameAs + "=" + data.DefaultValues[i].DefaultValue;
					}
				}
			}
			url = environment.ApiRootsLanding + "/" + data.Url + url;
			return this.http.get<any>(url, { headers: httpHeaders });
		} else {
			let url = environment.ApiRootsLanding + "/" + data.Url;
			if (data.DefaultValues) {
				for (var i = 0; i < data.DefaultValues.length; i++) {
					let value = data.DefaultValues[i].DefaultValue;
					if (data.DefaultValues[i].IdControl == -3)//json
						value = JSON.parse(value);
					if (data.DefaultValues[i].IdControl == 2)//số
						value = +value;
					if (data.DefaultValues[i].IdControl == 7)//bool
						value = value == 1;
					data[data.DefaultValues[i].ColumnName] = value;
					if (data.DefaultValues[i].ColumnNameAs) {
						data[data.DefaultValues[i].ColumnNameAs] = value;
					}
				}
			}
			return this.http.post<any>(url, data, { headers: httpHeaders });
		}
	}
	getActionValue(id, IdStep): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/df/get-action?Id=${id}&IdStep=${IdStep}`;
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders
		});
	}
	getNextByAction(id, idDoiTuong): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/df/get-step-by-action?id=${id}&idDoiTuong=${idDoiTuong}`;
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders
		});
	}

	getStep(id): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/step/Lite?id=' + id;
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders
		});
	}
	getObjectDetailById(loai, id) {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		let url = "";
		if (loai == 2 || loai == 1)
			url = environment.ApiRootsLanding + `/van-ban/Detail?id=${id}`;
		if (loai == 4)
			url = environment.ApiRootsLanding + `/cong-viec/Detail?id=${id}`;
		if (loai == 3)
			url = environment.ApiRootsLanding + `/qlhoso/Detail?id=${id}`;
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders
		});
	}
	CheckersByStep(DoiTuong, IdPA, IdStep): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/no-checker/CheckersByStep?IdPA=${IdPA}&IdStep=${IdStep}&DoiTuong=${DoiTuong}`, { headers: httpHeaders });
	}
	FixProcess(data): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();

		return this.http.post<any>(environment.ApiRootsLanding + '/no-checker/FixProcess', data, { headers: httpHeaders });
	}
	LoaiBuoc(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/loai-buoc';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//FormXuLy(id): Observable<any> {
	//	const httpHeaders = this.httpUtils.getHTTPHeaders();
	//	const url = environment.ApiRootsLanding + '/lite/form-xu-ly?DoiTuong='+id;
	//	return this.http.get<any>(url, { headers: httpHeaders });
	//}
	Quyen(id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/quyen-form?DoiTuong=' + id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Log_HanhDong(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/Log_HanhDong';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Log_LoaiLog(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/Log_Loai';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion

	//#region DuLam get dropdown  danh-sach-nguoi-lite
	//drop người dùng theo đơn vị
	getDSNguoiDungLite(useVaiTro: boolean = true, idDV: number = 0): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + `/lite/danh-sach-nguoi-lite?useVaiTro=${useVaiTro}&idDV=${idDV}`;
		return this.http.post<any>(url, null, { headers: httpHeaders });
	}
	//nhom lam viec lite
	DMNhomLamViec(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/dm-nhom-lam-viec-lite?loai=1';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DMNhomLamViecLienThong(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/dm-nhom-lam-viec-lite?loai=2';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//drop  đơn vị (auto complete)
	getDSDonViLite(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-sach-don-vi-lite';
		return this.http.post<any>(url, null, { headers: httpHeaders });
	}
	getTreeNguoiDungDonVi(itemId: any, useVaiTro: boolean = true): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/lite/tree-nguoi-dung-don-vi?Id=${itemId}&useVaiTro=${useVaiTro}`, { headers: httpHeaders });
	}
	getDonViTheoParent(Id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/lite/Lite_DanhSachDonVi_Prent?Id=${Id}`, { headers: httpHeaders });
	}
	//#region drop  đơn vị liên thông
	getDSDonViLienThongLite(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-sach-don-vi-lien-thong-dropdown-lite';
		return this.http.post<any>(url, null, { headers: httpHeaders });
	}
	//#endregion

	getDanhSachVanBan(TableName: string, Id: number) {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DS_VanBan?TableName=' + TableName + '&Id=' + Id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}

	getDanhSachCongViec(TableName: string, Id: number) {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DS_CongViec?TableName=' + TableName + '&Id=' + Id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}

	getDanhSachChonVanBan(queryParams: QueryParamsModel): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParms = this.httpUtils.getFindHTTPParams(queryParams)
		const url = environment.ApiRootsLanding + '/lite/DS_Chon_VanBan';
		return this.http.post<any>(url, httpParms, { headers: httpHeaders });
	}

	getDanhSachChonCongViec(queryParams: QueryParamsModel): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParms = this.httpUtils.getFindHTTPParams(queryParams)
		const url = environment.ApiRootsLanding + '/lite/DS_Chon_CongViec';
		return this.http.post<any>(url, httpParms, { headers: httpHeaders });
	}
	getDanhSachChonHoSo(queryParams: QueryParamsModel): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParms = this.httpUtils.getFindHTTPParams(queryParams)
		const url = environment.ApiRootsLanding + '/qlhoso/list_TraCuu';
		return this.http.post<any>(url, httpParms, { headers: httpHeaders });
	}
	// getData(queryParams: QueryParamsModel): Observable<QueryResultsModel> {
	// 	const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	const httpParms = this.httpUtils.getFindHTTPParams(queryParams)

	// 	return this.http.post<any>(API_ROOT_URL + '/list', httpParms, { headers: httpHeaders });

	// }
	//#region config
	getConfig(code: string[]): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/get-config';
		return this.http.post<any>(url, code, { headers: httpHeaders });
	}
	//#endregion
	//#region công việc
	cv_status(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/cv-status';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	cv_status_duyet(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/cv-status-duyet';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	cv_danhgia(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/cv-danh-gia';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion
	//#region văn bản
	table_status(loai): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/table-status?idTable=' + loai;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion
	getDSYKien(Id, Loai): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DS_YKien?Id=' + Id + '&Loai=' + Loai;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	getDSYKienLastest(Id, Loai, LastID): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DS_YKien?Id=' + Id + '&Loai=' + Loai + '&lastid = '+LastID;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	getDSYKienInsert(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DS_YKien_Insert';
		return this.http.post<any>(url, item, { headers: httpHeaders });
	}
	ExportWord_DanhSachYKien(Title: string = "HỒ SƠ", Id: number, TableName: string = "Tbl_HoSo", Ma: string = "MaHoSo", Ten: string = "TieuDe"): string {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/ExportWord_DanhSachYKien?Title=' + Title + '&Id=' + Id + '&TableName=' + TableName + '&Ma=' + Ma + '&Ten=' + Ten;
		return url;
	}

	DownloadFile_YKienXuLy(link): string {
		const url = environment.ApiRootsLanding + '/lite/DownLoadFile_YKienXuLy?link=' + link;
		return url;
	}
	DMPhongHop(id: number = 0): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/DM_PhongHop?Id=' + id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//Tinh trang lich
	TrangThaiLich(IsPheDuyet: boolean): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/trang-thai-lich?pheduyet=' + IsPheDuyet;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//Loai xu ly
	LoaiXuLy(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/loai-xu-ly';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//Loai xu ly
	DonViUserNhanLich(idUser: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-sach-don-vi-user-nhan-lich?id=' + idUser;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#region lịch sử, theo dõi
	getLichSu(loai: number, itemId: any): Observable<any> {
		let url = '';
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		switch (loai) {
			case 4:
				{
					//công việc
					url = environment.ApiRootsLanding + `/cong-viec/get-lichsu?id=${itemId}`;
					break;
				}
			case 1:
			case 2:
				{
					//vb đi
					url = environment.ApiRootsLanding + `/van-ban/get-lichsu?loai=${loai}&id=${itemId}`;
					break;
				}
		}
		return this.http.get<any>(url, { headers: httpHeaders });
	}


	theoDoiLienThong(itemId: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/van-ban/theo-doi-lien-thong?id=${itemId}`, { headers: httpHeaders });
	}
	//#endregion
	//#region thông tin xử lý
	getThongTinXuLy(loai: number, itemId: any): Observable<any> {
		let url = '';
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		switch (loai) {
			case 4:
				{
					//công việc
					url = environment.ApiRootsLanding + `/cong-viec/thong-tin-xu-ly?id=${itemId}`;
					break;
				}
			case 1:
			case 2:
				{
					//văn bản đi
					url = environment.ApiRootsLanding + `/van-ban/thong-tin-xu-ly?id=${itemId}&loai=${loai}`;
					break;
				}
			case 1:
				{
					//văn bản đến
					url = environment.ApiRootsLanding + `/van-ban/thong-tin-xu-ly?id=${itemId}&loai=${loai}`;
					break;
				}
		}
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion
	//#region Lịch lãnh đạo
	DSLanhDao(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = environment.ApiRootsLanding + '/lite/danh-sach-lanh-dao-lite';
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion

	getTreeDonViNgoaiLienThong(type: any = 0): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/lite/tree-donvi-ngoai-lienthong?type=${type}`, { headers: httpHeaders });
	}
	DMUserNguoiKy(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/lite/DMUserNguoiKy`, { headers: httpHeaders });
	}
	DMUserVaiTroNguoiKy(iddv: number = 0, id: number = 0): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/lite/DMUserVaiTroNguoiKy?iddv=${iddv}&id=${id}`, { headers: httpHeaders });
	}

	DMSoVanBan(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/lite/DM_SoVanBan`, { headers: httpHeaders });
	}

	SoNgayLamViec(Tungay: string, Denngay: string): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/lite/TinhSoNgayLamViec?Tungay=${Tungay}&Denngay=${Denngay}`, { headers: httpHeaders });
	}

	//CheckExpireLogin(): Observable<any> {
	//	const httpHeaders = this.httpUtils.getHTTPHeaders();
	//	return this.http.get<any>(environment.ApiRootsLanding + `/user/CheckExpireLogin`, { headers: httpHeaders });
	//}

	TimesOutExpire() {
		// if (localStorage.getItem('TIME_LOGOUT') != undefined && +localStorage.getItem('TIME_LOGOUT') != 0) {
		// 	// sets an idle timeout of 5 seconds, for testing purposes.
		// 	this.idle.setIdle(+localStorage.getItem('TIME_LOGOUT') / 1000);
		// 	//this.idle.setIdle(3);
		// 	// sets a timeout period of 5 seconds. after 10 seconds of inactivity, the user will be considered timed out.
		// 	this.idle.setTimeout(1);
		// 	// sets the default interrupts, in this case, things like clicks, scrolls, touches to the document
		// 	this.idle.setInterrupts(DEFAULT_INTERRUPTSOURCES);

		// 	this.idle.onTimeout.subscribe(() => {
		// 		//this.idleState = 'Timed out!';
		// 		this.layoutUtilsService.showInfo('Bạn đã không thao tác trong ' + (+localStorage.getItem('TIME_LOGOUT') / 1000 / 60) + ' phút, phần mềm sẽ đăng xuất');
		// 		this.auth.logout(true);
		// 		//this.timedOut = true;
		// 	});

		// 	this.idle.watch();
		// 	//this.timedOut = false;
		// }
	}
	/*Gửi thông báo nhắc nhở công việc*/
	sendRemind(id: number, sendEmail:boolean, sendSMS: boolean): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + `/cong-viec/gui-nhac-nho?id=${id}&sendEmail=${sendEmail}&sendSMS=${sendSMS}`, { headers: httpHeaders });
	}
	ThongKeDasboard() {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + '/thong-ke/thong-ke-dasboard', { headers: httpHeaders });
	}
	BieuDoThongKeVanBan() {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.ApiRootsLanding + '/thong-ke/bieudo-vanban', { headers: httpHeaders });
	}
	LastestFeedbackDasboard(queryParams: QueryParamsModel) {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParms = this.httpUtils.getFindHTTPParams(queryParams)
		return this.http.post<any>(environment.ApiRootsLanding + '/thong-bao/get-thong-bao-dashboard',httpParms, { headers: httpHeaders });
	}
}
