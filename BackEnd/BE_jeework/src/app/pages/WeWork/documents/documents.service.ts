import { QueryParamsModelNew } from './../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { environment } from 'src/environments/environment';
import { QueryParamsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { HttpUtilsService } from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { QueryResultsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { Injectable } from '@angular/core';

const API_Document = environment.ApiRoots + 'api/documents';
@Injectable({
  providedIn: 'root'
})
export class DocumentsService {

  lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	ReadOnlyControl: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }
	findListActivities(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_Document + '/List-activities';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	// LogDetail(id: any): Observable<any> {
	// 	const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	const url = `${API_Document}/log-detail?id=${id}`;
	// 	return this.http.get<any>(url, { headers: httpHeaders });
	// }
	ListDocuments(queryParams): Observable<any> {
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Document}/List`;
		return this.http.get<any>(url, { headers: httpHeaders,params: httpParams });
	}

	Upload_attachment(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Document + '/Insert', item, { headers: httpHeaders });
	}
}
