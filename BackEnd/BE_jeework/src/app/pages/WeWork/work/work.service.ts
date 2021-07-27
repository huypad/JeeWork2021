import { QueryParamsModelNew } from './../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { QueryParamsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { environment } from 'src/environments/environment';
import { HttpUtilsService } from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { QueryResultsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { Injectable } from '@angular/core';
import { map } from 'rxjs/operators';
const API_work = environment.APIROOTS + '/api/work';
const API_My_Work = environment.APIROOTS + '/api/personal';
const API_filter = environment.APIROOTS + '/api/filter';
const API_checklist = environment.APIROOTS + '/api/checklist';
const API_repeated = environment.APIROOTS + '/api/repeated';
const API_personal = environment.APIROOTS + '/api/personal';
const API_work_group = environment.APIROOTS + '/api/work-group';
const API_work_CU = environment.APIROOTS + '/api/work-click-up';


@Injectable()
export class WorkService {
	lastFilter$: BehaviorSubject<QueryParamsModelNew> = new BehaviorSubject(new QueryParamsModelNew({}, 'asc', '', 0, 10));

	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }

	getEvents(queryParams: QueryParamsModel): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParms = this.httpUtils.getFindHTTPParams(queryParams)

		return this.http.get<any>(API_work + '/list-event',
			{ headers: httpHeaders, params: httpParms });

	}
	myList(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work + '/my-list';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	listFollowing(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work + '/list-following';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	listByFilter(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work + '/list-by-filter';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	myStaffList(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_work + '/my-staff-list';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}

	myStaff(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_My_Work}/my-staff`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	mymilestone(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_My_Work}/my-milestone`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Filter(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_filter}/List`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	// list_tag(lite_workgroup: any): Observable<any> {
	// 	const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	// let params = this.httpUtils.parseFilter(filter);
	// 	return this.http.get<any>(environment.ApiRoot + `/wework-lite/lite_account`, { headers: httpHeaders, params: params });
	// }
	list_tag(id_project_team: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.APIROOTS + `/api/wework-lite/lite_tag?id_project_team=${id_project_team}`, { headers: httpHeaders });
	}
	InsertWork(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_work + '/Insert', item, { headers: httpHeaders });
	}
	UpdateWork(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_work + '/Update', item, { headers: httpHeaders });
	}
	Duplicate(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_work + '/Duplicate', item, { headers: httpHeaders });
	}
	WorkDetail(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_work}/Detail?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	WorkDelete(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_work}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Add_Followers(topic: number, user: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_work}/add-follower?topic=${topic}&&user=${user}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Delete_Followers(topic: number, user: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_work}/Remove-follower?topic=${topic}&&user=${user}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	CheckList(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_checklist + '/List';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
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
	Update_CheckList_Item(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_checklist + '/Update-item', item, { headers: httpHeaders });
	}
	CheckedItem(id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_checklist + '/Checked/'+id, { headers: httpHeaders });
	}
	Delete_CheckList(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_checklist}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	DeleteItem(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_checklist}/Delete-item?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	UpdateByKey(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_work_CU + '/Update-by-key', item, { headers: httpHeaders });
	}
	// UpdateByKeyNew(item): Observable<any> {
	// 	const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	return this.http.post<any>(API_work_CU + '/Update-by-key', item, { headers: httpHeaders });
	// }
	//#region import/export
	// ImportData(item): Observable<any> {
	// 	const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	return this.http.post<any>(API_work + '/ImportData', item, { headers: httpHeaders });
	// }
	ImportData(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_work_CU + '/ImportData', item, { headers: httpHeaders });
	}
	ExportExcel(params): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		let httpparams = this.httpUtils.getFindHTTPParams(params)
		return this.http.get(API_work_CU + '/ExportExcel', { //API_work
			headers: httpHeaders,
			params: httpparams,
			responseType: 'blob',
			observe: 'response'
		});
		//.pipe(
		//	map((res: any) => {
		//		var headers = res.headers;
		//		filename = headers.get('x-filename');
		//		let blob = new Blob([res.body], { type: 'application/vnd.ms-excel' });
		//		return blob;
		//	})
		//);
	}
	DownloadFileImport(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_work}/DownloadFileImport`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}

	findData(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_My_Work + '/my-staff-overview';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}
	findDataRepeated(queryParams: QueryParamsModelNew): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const httpParams = this.httpUtils.getFindHTTPParams(queryParams);
		const url = API_repeated + '/List';
		return this.http.get<QueryResultsModel>(url, {
			headers: httpHeaders,
			params: httpParams
		});
	}

	favouritework(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_personal}/favourite-work?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Add_followers(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_work + '/Add_followers', item, { headers: httpHeaders });
	}
	DeleteWork(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_work}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	InsertWorkGroup(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_work_group + '/Insert', item, { headers: httpHeaders });
	}
	UpdateWorkGroup(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_work_group + '/Update', item, { headers: httpHeaders });
	}
	DeleteWorkGroup(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_work_group}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	assign(id: number, user: number): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<QueryResultsModel>(API_repeated + `/assign?id=${id}&&user=${user}`, { headers: httpHeaders });
	}
	Locked(id: number, locked: boolean): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<QueryResultsModel>(API_repeated + `/Lock?id=${id}&&locked=${locked}`, { headers: httpHeaders });
	}
	updateProject(id: number, id_project_team: string): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<QueryResultsModel>(API_repeated + `/project_team?id=${id}&&id_project_team=${id_project_team}`, { headers: httpHeaders });
	}
	DeleteRepeatedTask(id: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_repeated}/Delete?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Forcerun(id_repeated: number): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_repeated}/forcerun?id_repeated=${id_repeated}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Detail_repeated(id: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = `${API_repeated}/Detail?id=${id}`;
		return this.http.get<any>(url, { headers: httpHeaders });
	}
	Update_RepeatedTask(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_repeated + '/Update', item, { headers: httpHeaders });
	}
	Insert_RepeatedTask(item): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.post<any>(API_repeated + '/Insert', item, { headers: httpHeaders });
	}

}
