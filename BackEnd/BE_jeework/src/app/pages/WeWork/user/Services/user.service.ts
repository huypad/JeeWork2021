import { QueryParamsModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { HttpUtilsService } from './../../../../_metronic/jeework_old/core/utils/http-utils.service';
import { QueryResultsModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, BehaviorSubject, of } from 'rxjs';
import { map, retry } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { AuthorizeModel } from '../Model/user.model';

const API = environment.APIROOTS + '/api/wuser';
const API_authorize = environment.APIROOTS + '/api/authorizecontroler';
const API_Work=environment.APIROOTS + '/api/work';
const API_Work_CU=environment.APIROOTS + '/api/work-click-up';

@Injectable()
export class UserService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	ReadOnlyControl: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	findData(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API + '/List';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}

	findDataWork(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_Work + '/List-by-user';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	ExportExcelByUsers(params): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		let httpparams = this.httpUtils.getFindHTTPParams(params)
		return this.http.get(API_Work_CU + '/ExportExcelByUsers', {
			headers: httpHeaders,
			params: httpparams,
			responseType: 'blob',
			observe: 'response'
		});
		//.pipe(
		//	map((res: any) => {
		//		var headers = res.headers;
		//		filename = headers.get('x-filename');
		//		let blob = new Blob([res.body], { type: 'application/vnd.ms-excel' });
		//		return blob;
		//	})
		//);
	}
	Detail(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API}/detail?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#region ủy quyền
	uyquyen(data): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API}/uy-quyen`;
		return this.http.post<any>(url, data, { headers: httpHeaders });
	}
	//#endregion

	find_ListAuthorize(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API + '/ListAuthorize';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	UpdateAuthorize(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_authorize + '/Update', item, { headers: httpHeaders });
	}
	InsertAuthorize(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API + '/Authorize', item, { headers: httpHeaders });
	}
}
