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
}
