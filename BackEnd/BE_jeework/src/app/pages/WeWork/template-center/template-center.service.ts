import { Observable } from 'rxjs';
import { QueryParamsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { HttpUtilsService } from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { HttpClient } from '@angular/common/http';
import { environment } from './../../../../environments/environment';
import { Injectable } from '@angular/core';

const API_Template = environment.APIROOTS + '/api/template';
const API_Lite = environment.APIROOTS + '/api/wework-lite';

@Injectable({
  providedIn: 'root'
})
export class TemplateCenterService {

  constructor(private http: HttpClient,
    private httpUtils: HttpUtilsService) { }

  getTemplateCenter(queryParams: QueryParamsModel): Observable<any> {
    const httpHeaders = this.httpUtils.getHTTPHeaders();
    const httpParms = this.httpUtils.getFindHTTPParams(queryParams)

    return this.http.get<any>(API_Template + '/get-list-template-center',
      { headers: httpHeaders, params: httpParms });

  }
  getDetailTemplate(id,istemplatelist = false): Observable<any> {
    const httpHeaders = this.httpUtils.getHTTPHeaders();
    return this.http.get<any>(API_Template + `/detail?id=${id}&istemplatelist=${istemplatelist}`,
      { headers: httpHeaders });
  }
  getListTemplateUser(queryParams: QueryParamsModel): Observable<any> {
    const httpHeaders = this.httpUtils.getHTTPHeaders();
    const httpParms = this.httpUtils.getFindHTTPParams(queryParams)
    return this.http.get<any>(API_Template + '/get-template-by-user', //get-template-by-user update-template-center
    { headers: httpHeaders, params: httpParms });
  }
  //lite_template_types
  getTemplateTypes(): Observable<any> {
    const httpHeaders = this.httpUtils.getHTTPHeaders();
    return this.http.get<any>(API_Lite + '/lite_template_types',
      { headers: httpHeaders });
  }
  LiteDepartmentFolder(): Observable<any> {
    const httpHeaders = this.httpUtils.getHTTPHeaders();
    return this.http.get<any>(API_Lite + '/lite_department_folder_user',
      { headers: httpHeaders });
  }
  LiteListField(): Observable<any> {
    const httpHeaders = this.httpUtils.getHTTPHeaders();
    return this.http.get<any>(API_Lite + '/list-field-template',
      { headers: httpHeaders });
  }
}
