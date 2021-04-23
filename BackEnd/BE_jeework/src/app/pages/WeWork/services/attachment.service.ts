import { environment } from 'src/environments/environment';
import { HttpUtilsService } from './../../../_metronic/jeework_old/core/utils/http-utils.service';
import { QueryParamsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, BehaviorSubject, of } from 'rxjs';
import { map, retry } from 'rxjs/operators';
import { Injectable } from '@angular/core';

const API_attachment = environment.ApiRoots + 'api/attachment';
@Injectable()
export class AttachmentService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	ReadOnlyControl: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }
	delete_attachment(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_attachment}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}

	Upload_attachment(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_attachment + '/Insert', item, { headers: httpHeaders });
	}
}
