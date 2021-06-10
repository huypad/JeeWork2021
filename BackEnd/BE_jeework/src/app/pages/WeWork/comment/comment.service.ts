import { HttpUtilsService } from './../../../_metronic/jeework_old/core/utils/http-utils.service';
import { QueryParamsModel } from './../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, BehaviorSubject, of } from 'rxjs';
import { map, retry } from 'rxjs/operators';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';

const API = environment.APIROOTS + '/api/comment';


@Injectable()
export class CommentService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	ReadOnlyControl: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	getDSYKien(Id, Loai): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		let p = new QueryParamsModel({ object_type: Loai, object_id: Id });
		let params = this.httpUtils.getFindHTTPParams(p);
		const url = API + '/List';
		return this.http.get<any>(url, {
			headers: httpHeaders,
			params: params
		});
	}

	getDSYKienInsert(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API + '/Insert';
		return this.http.post<any>(url, item, { headers: httpHeaders });
	}
	update(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API + '/Update';
		return this.http.post<any>(url, item, { headers: httpHeaders });
	}
	like(id, type = 1): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API + '/like?id=' + id + '&type=' + type;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	remove(id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API + '/Delete?id=' + id;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
}
