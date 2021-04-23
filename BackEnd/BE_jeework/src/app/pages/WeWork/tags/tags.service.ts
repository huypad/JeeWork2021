import { HttpUtilsService } from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { environment } from 'src/environments/environment'; 
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { Injectable } from '@angular/core';
const API_filter = environment.ApiRoots + 'api/tag';

@Injectable()
export class TagsService {
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	Update(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_filter + '/Update', item, { headers: httpHeaders });
	}
	Insert(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_filter + '/Insert', item, { headers: httpHeaders });
	}
	Delete(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_filter}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
}
