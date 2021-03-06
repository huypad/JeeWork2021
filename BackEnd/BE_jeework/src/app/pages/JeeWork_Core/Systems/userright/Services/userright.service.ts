import { HttpUtilsService } from './../../../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { QueryParamsModel } from './../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { environment } from 'src/environments/environment';
import { QueryResultsModel } from './../../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
 import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, BehaviorSubject, of } from 'rxjs'; 
import { GroupNameModel, UserModel } from '../Model/userright.model';
import { Injectable } from '@angular/core';
import { map } from 'rxjs/operators';
import { _ParseAST } from '@angular/compiler';
const API_PRODUCTS_URL = environment.APIROOTS + '/api/ww_userrights';

@Injectable()
export class PermissionService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	public Visible_Group: boolean;
	public Visible_User: boolean;
	public Visible_UserGroup: boolean;
	public Visible_UserSystem: boolean;
	public Visible_Functions: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	//===========Nhóm người dùng===================
	//danh sách nhóm người dùng
	findDataGroup(queryParams: QueryParamsModel): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_PRODUCTS_URL + `/Get_DSNhom`;
		return this.http.get<any>(url, {
			headers: httpHeaders,
			params: httpParams,
		});
	}
	// CREATE =>  POST: add a new oduct to the server
	CreateNhomNguoiDung(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_PRODUCTS_URL + '/Insert_Nhom', item, { headers: httpHeaders });
	}
	// Delete
	deleteItemNhomNguoiDung(id: number, ten: string): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_PRODUCTS_URL}/Delete_Nhom?id=${id}&TenNhom=${ten}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	findData_Functions(queryParams: QueryParamsModel): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_PRODUCTS_URL + `/Get_ListFunctions`;
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams,
		});
	}
	//Update quyền nhóm người dùng
	UpdatePermision(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_PRODUCTS_URL + '/Save_Permision', item, { headers: httpHeaders });
	}
	//============Người dùng ========================
	//Danh sách người dùng
	findDataUsers(queryParams: QueryParamsModel): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_PRODUCTS_URL + `/Get_DSNguoiDung`;
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams,
		});
	}
	//Danh sách người dùng nhóm  
	findData_UserGroup(queryParams: QueryParamsModel): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_PRODUCTS_URL + `/Get_UserGroup`;
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams,
		});
	}
	//Update danh sách nhóm
	UpdateDanhSachNhom(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_PRODUCTS_URL + '/Insert_User', item, { headers: httpHeaders });
	}
	// Delete danh sách nhóm
	deleteDanhSachNhom(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_PRODUCTS_URL + '/Delete_User', item, { headers: httpHeaders });
	}
}
