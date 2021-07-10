import { QueryParamsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { Observable } from 'rxjs';
import { environment } from 'src/environments/environment';
import { HttpUtilsService } from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';

const API_Template = environment.APIROOTS + '/api/template';
const API_Lite = environment.APIROOTS + '/api/wework-lite';
const API_Auto = environment.APIROOTS + '/api/automation';

@Injectable({
  providedIn: 'root'
})
export class AutomationService {

  constructor(private http: HttpClient,
    private httpUtils: HttpUtilsService) { }

    getAutomationEventlist(): Observable<any> {
      const httpHeaders = this.httpUtils.getHTTPHeaders();
      return this.http.get<any>(API_Lite + '/lite_automation_eventlist',
        { headers: httpHeaders });
    }
    getAutomationActionList(): Observable<any> {
      const httpHeaders = this.httpUtils.getHTTPHeaders();
      return this.http.get<any>(API_Lite + '/lite_automation_actionlist',
        { headers: httpHeaders });
    }

    getAutomationList(queryParams): Observable<any> {
      const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
      const httpHeaders = this.httpUtils.getHTTPHeaders();
      return this.http.get<any>(API_Auto + '/get-automation-list',
        { headers: httpHeaders , params: httpParams});
    }
    InsertAutomation(data): Observable<any> {
      const httpHeaders = this.httpUtils.getHTTPHeaders();
      return this.http.post<any>(API_Auto + '/add-automation',data,
        { headers: httpHeaders });
    }
    UpdateAutomation(data): Observable<any> {
      const httpHeaders = this.httpUtils.getHTTPHeaders();
      return this.http.post<any>(API_Auto + '/update-automation',data,
        { headers: httpHeaders });
    }
    UpdateStatusAutomation(rowid): Observable<any> {
      const httpHeaders = this.httpUtils.getHTTPHeaders();
      return this.http.get<any>(API_Auto + '/updatestatus-automation?rowid='+rowid,
        { headers: httpHeaders });
    }
    ListTaskParent(id,isDeparment): Observable<any> {
      const httpHeaders = this.httpUtils.getHTTPHeaders();
      return this.http.get<any>(API_Auto + `/list-task-parent?id=${id}&isDeparment=${isDeparment}`,
        { headers: httpHeaders });
    }

    // getTemplateCenter(queryParams: QueryParamsModel): Observable<any> {
    //   const httpHeaders = this.httpUtils.getHTTPHeaders();
    //   const httpParms = this.httpUtils.getFindHTTPParams(queryParams)
  
    //   return this.http.get<any>(API_Template + '/get-list-template-center',
    //     { headers: httpHeaders, params: httpParms });
  
    // }
}
