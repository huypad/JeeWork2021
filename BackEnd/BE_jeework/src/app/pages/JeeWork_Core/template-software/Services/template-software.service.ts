import { environment } from 'src/environments/environment';
import { QueryResultsModel } from '../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
 import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, BehaviorSubject, of } from 'rxjs'; 
import { Injectable } from '@angular/core';
import { map } from 'rxjs/operators';
import { _ParseAST } from '@angular/compiler';
import {QueryParamsModel} from '../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import {HttpUtilsService} from '../../../../_metronic/jeework_old/core/utils/http-utils.service';
const API_TEMPLATESOFTWARE = environment.APIROOTS + '/api/template';

@Injectable()
export class templateSoftwareService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	public Visible: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	//===========Nhóm người dùng===================
	//danh sách nhóm người dùng
	MohinhDuan(queryParams: QueryParamsModel): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_TEMPLATESOFTWARE + `/mo-hinh-du-an`;
		return this.http.get<any>(url, {
			headers: httpHeaders,
			params: httpParams,
		});
	}
	SetDefault(id,isdefault): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_TEMPLATESOFTWARE + `/set-default?id=${id}&isdefault=${isdefault}`;
		return this.http.get<any>(url, {
			headers: httpHeaders
		});
	}




}
