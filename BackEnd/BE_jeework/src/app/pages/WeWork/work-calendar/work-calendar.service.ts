import { QueryParamsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { HttpUtilsService } from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { environment } from 'src/environments/environment'; 
import { HttpClient } from '@angular/common/http';
import { Observable,  BehaviorSubject } from 'rxjs';
import { Injectable } from '@angular/core';
const API_ROOT_URL = environment.ApiRoots + 'api/work';
const API_ROOT_URL_CU= environment.ApiRoots + 'api/work-click-up';

@Injectable()
export class WorkCalendarService {	
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	getEvents(queryParams: QueryParamsModel): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParms = this.httpUtils.getFindHTTPParams(queryParams)

		return this.http.get<any>(API_ROOT_URL_CU + '/list-event', 
		{ headers: httpHeaders,		params:httpParms });

	}
	get_listeventbyproject(queryParams: QueryParamsModel): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParms = this.httpUtils.getFindHTTPParams(queryParams)
		return this.http.get<any>(API_ROOT_URL_CU + '/list-event-by-project', 
		{ headers: httpHeaders,		params:httpParms });

	}
}
