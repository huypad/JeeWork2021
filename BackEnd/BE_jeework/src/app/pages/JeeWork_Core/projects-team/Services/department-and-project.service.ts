import { HttpUtilsService } from './../../../../_metronic/jeework_old/core/utils/http-utils.service';
import { environment } from 'src/environments/environment';
import { QueryResultsModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { QueryParamsModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, BehaviorSubject, of } from 'rxjs';
import { map, retry } from 'rxjs/operators';
import { Injectable } from '@angular/core';

const API_Project_Team = environment.APIROOTS + '/api/project-team';
const API_My_Work = environment.APIROOTS + '/api/personal';
const API_work = environment.APIROOTS + '/api/work';
const API_topic = environment.APIROOTS + '/api/topic';
const API_tag = environment.APIROOTS + '/api/tag';
const API_work_CU = environment.APIROOTS + '/api/work-click-up';
const API_Status = environment.APIROOTS + '/api/status-dynamic';
const api_department = environment.APIROOTS + '/api/department';





@Injectable()
export class ProjectsTeamService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	ReadOnlyControl: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }


	findDataProject(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_Project_Team + '/List';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	findDataProjectByDepartment(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_Project_Team + '/List-by-department';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	listView(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work + '/List-by-project';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	PeriodView(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work + '/period-view';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	findListActivities(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_Project_Team + '/List-activities';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	findListTopic(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_topic + '/list';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	findStreamView(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work + '/stream-view';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	findGantt(queryParams: QueryParamsModelNew): Observable<any> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work_CU + '/gantt-view';
		return this.http.get<any>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	find_gantt_editor(queryParams: QueryParamsModelNew): Observable<any> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work_CU + '/gantt-editor';
		return this.http.get<any>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	findUsers(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_Project_Team + '/lite_account';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	InsertProjectTeam(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Project_Team + '/Insert', item, { headers: httpHeaders });
	}
	InsertFasttProjectTeam(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Project_Team + '/Insert_Quick', item, { headers: httpHeaders });
	}
	UpdateProjectTeam(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Project_Team + '/update', item, { headers: httpHeaders });
	}
	UpdateStage(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Project_Team + '/Update-stage', item, { headers: httpHeaders });
	}
	UpdateByKey(id, key, val): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Project_Team + `/update-by-key?id=${id}&key=${key}&value=${val}`, { headers: httpHeaders });
	}
	ClosedProject(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Project_Team + '/Close', item, { headers: httpHeaders });
	}
	OpenProject(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Project_Team + '/Open', item, { headers: httpHeaders });
	}
	Duplicate(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Project_Team + '/Duplicate', item, { headers: httpHeaders });
	}
	get_config_email(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/get-config-email?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#region quyền
	ListRole(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/list-role?id_project_team=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	UpdateRole(id, key, role): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Project_Team + `/Update-role?id=${id}&key=${key}&role=${role}`, { headers: httpHeaders });
	}
	//#endregion
	DeptDetail(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/Detail?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	MyWork(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_My_Work}/my-work`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	OverView(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/overview?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DeleteProject(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	ChangeType(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/Change-type?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	LogDetail(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_work}/log-detail?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	TopicDetail(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_topic}/detail?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Add_Followers(topic: number, user: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_topic}/add-follower?topic=${topic}&&user=${user}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Delete_Followers(topic: number, user: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_topic}/Remove-follower?topic=${topic}&&user=${user}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#region member
	List_user(id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/List-user?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Add_user(data: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/Add-user`;
		return this.http.post<any>(url, data, { headers: httpHeaders });
	}
	Delete_user(id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/Delete-user?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	update_user(id, admin): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/Update-user?id=${id}&admin=${admin}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}

	favourireproject(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_My_Work}/favourite-project?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	UpdateTags(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_tag + '/Update', item, { headers: httpHeaders });
	}
	InsertTags(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_tag + '/Insert', item, { headers: httpHeaders });
	}
	DeleteTag(id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_tag}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//#endregion


	//tìm id department từ id project team
	FindDepartmentFromProjectteam(id) {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_Project_Team}/get-department?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}

	// get data từ work click up
	GetDataWorkCU(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work_CU + '/list-work';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	// drap-drop-item
	DragDropItemWork(item: any): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_work_CU + '/drap-drop-item';
		return this.http.post<QueryResultsModel>(url, item, {
			headers: httpHeaders,
		});
	}
	UpdateColumnWork(item: any): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_work_CU + '/update-column-work';
		return this.http.post<QueryResultsModel>(url, item, {
			headers: httpHeaders,
		});
	}
	UpdateColumnNewField(item: any): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_work_CU + '/update-column-new-field';
		return this.http.post<QueryResultsModel>(url, item, {
			headers: httpHeaders,
		});
	}
	UpdateNewField(item: any): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_work_CU + '/update_new-field';
		return this.http.post<QueryResultsModel>(url, item, {
			headers: httpHeaders,
		});
	}
	InsertTask(item: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_work_CU + '/Insert';
		return this.http.post<QueryResultsModel>(url, item, {
			headers: httpHeaders,
		});
	}
	UpdateTask(item: any): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_work_CU + '/Update';
		return this.http.post<QueryResultsModel>(url, item, {
			headers: httpHeaders,
		});
	}
	DeleteTask(id: any): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_work_CU + '/Delete?id=' + id;
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
		});
	}
	_UpdateByKey(item: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_work_CU + '/Update-by-key';
		return this.http.post<QueryResultsModel>(url, item, {
			headers: httpHeaders,
		});
	}
	// get log-detail
	LogDetailCU(id: any): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_work_CU + '/log-detail-by-work?id=' + id;
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
		});
	}
	WorkDetail(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_work_CU}/Detail?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	//detail column update
	Detail_column_new_field(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_work_CU}/detail-column-new-field?field=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}


	DuplicateCU(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_work_CU + '/Duplicate', item, { headers: httpHeaders });
	}

	ListByUserCU(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work_CU + '/my-list';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	ListByFilter(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work_CU + '/list-work-by-filter';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	ListByManageCU(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work_CU + '/list-work-user-by-manager';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}

	update_hidden(query): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = API_work_CU + '/update-hidden?' + query;
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
		});
	}

	//
	InsertStatus(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Status + '/Insert', item, { headers: httpHeaders });
	}
	UpdateStatus(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Status + '/Update', item, { headers: httpHeaders });
	}
	DeleteStatus(id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Status + '/Delete?id=' + id, { headers: httpHeaders });
	}

	Different_Statuses(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Status + '/different-statuses', item, { headers: httpHeaders });
	}
	// view update 
	Add_View(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Project_Team + '/add-view', item, { headers: httpHeaders });
	}
	update_view(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_Project_Team + '/update-view', item, { headers: httpHeaders });
	}
	Delete_View(id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Project_Team + '/delete-view?id=' + id, { headers: httpHeaders });
	}
	department_detail(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${api_department}/Detail?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
}
