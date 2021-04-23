import { HttpUtilsService } from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { environment } from 'src/environments/environment'; 
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { Injectable } from '@angular/core';
const API_ROOT_URL = environment.ApiRoots + 'api/work';
const API_checklist = environment.ApiRoots + 'api/checklist';

@Injectable()
export class UpdateByKeyService {
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	UpdateByKey(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_ROOT_URL + '/Update-by-key', item, { headers: httpHeaders });
	}
	Update_CheckList(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_checklist + '/Update', item, { headers: httpHeaders });
	}
	Insert_CheckList(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_checklist + '/Insert', item, { headers: httpHeaders });
	}
	Insert_CheckList_Item(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_checklist + '/Insert-item', item, { headers: httpHeaders });
	}
}
