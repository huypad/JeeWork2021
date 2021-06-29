import { environment } from 'src/environments/environment';
import { QueryResultsModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { HttpUtilsService } from './../../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { QueryParamsModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, BehaviorSubject, of } from 'rxjs';
import { map, retry } from 'rxjs/operators';
import { Injectable } from '@angular/core'; 

const API_department = environment.APIROOTS + '/api/department';
const API_Project_Team = environment.APIROOTS + '/api/project-team';
const API_milestone = environment.APIROOTS + '/api/milestone';
const API_Process = environment.APIROOTS + '/api/workprocess';
const API_Template = environment.APIROOTS + '/api/template';


@Injectable()
export class ListDepartmentService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	ReadOnlyControl: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	// CREATE =>  POST: add a new oduct to the server
	UpdateDept(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_department + '/Update', item, { headers: httpHeaders });
	}
	InsertDept(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_department + '/Insert', item, { headers: httpHeaders });
	}
	UpdateTemplateCenter(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Template + '/update-template-center', item, { headers: httpHeaders });
	}
	SaveAsTemplateCenter(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Template + '/save-as-template', item, { headers: httpHeaders });
	}
	DeptDetail(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_department}/Detail?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	findDataDept(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_department + '/List';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	findDataProject(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_Project_Team + '/List-by-department';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	findDataMilestone(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_milestone + '/List';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	Delete_Dept(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_department}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//=====================Tab hoạt động==============================
	getActivityLog(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_Process + '/getActivityLog';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	Get_MilestoneDetail(itemId: number): Observable<any> {//Lấy chi tiết quy trình
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_milestone}/Detail?id=${itemId}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	UpdatMilestone(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_milestone + '/Update', item, { headers: httpHeaders });
	}
	InsertMilestone(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_milestone + '/Insert', item, { headers: httpHeaders });
	}

	// api template
	
	Update_Quick_Template(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Template + '/Update_Quick', item, { headers: httpHeaders });
	}
	Delete_Templete(id,isDelStatus): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Template + `/Delete?id=${id}&&isDelStatus=${isDelStatus}`, { headers: httpHeaders });
	}
}
