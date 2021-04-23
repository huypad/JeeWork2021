import { HttpUtilsService } from './../../_metronic/jeework_old/core/utils/http-utils.service';
import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
const API_URL = environment.ApiRoots + '/controllergeneral';
@Injectable()
export class DynamicFormService {
    title$: BehaviorSubject<string> = new BehaviorSubject('');
    controls$: BehaviorSubject<any[]> = new BehaviorSubject([]);

    constructor(private http: HttpClient,
        private httpUtils: HttpUtilsService, ) { }
    getInitData(api): Observable<any> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        const url = `${API_URL}${api}`;
        return this.http.get<any>(url, { headers: httpHeaders });
    }
}
