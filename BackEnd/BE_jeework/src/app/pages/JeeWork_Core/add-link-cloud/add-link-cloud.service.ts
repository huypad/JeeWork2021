import { environment } from 'src/environments/environment';
import { HttpUtilsService } from '../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { Injectable } from '@angular/core';
const API_filter = environment.APIROOTS + '/api/filter';

@Injectable()
export class filterService {
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	Update_filter(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_filter + '/Update', item, { headers: httpHeaders });
	}
	Insert_filter(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_filter + '/Insert', item, { headers: httpHeaders });
	}
	Get_list_filterkey(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_filter}/list_filterkey`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Delete_filter(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_filter}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Detail(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_filter}/detail?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}

}
