import { QueryResultsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { HttpUtilsService } from './../../../_metronic/jeework_old/core/utils/http-utils.service';
import { environment } from 'src/environments/environment';
import { QueryParamsModelNew } from './../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { QueryParamsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, BehaviorSubject, of } from 'rxjs';
import { map, retry } from 'rxjs/operators';
import { Injectable } from '@angular/core';

const API_Lite = environment.APIROOTS + '/api/wework-lite';
const APIROOTS = environment.APIROOTS;


@Injectable()
export class WeWorkService {
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }
	// list priority
	list_priority = [
		{
			name: 'Urgent',
			value: 1,
			icon: 'fab fa-font-awesome-flag text-danger',
		},
		{
			name: 'High',
			value: 2,
			icon: 'fab fa-font-awesome-flag text-warning',
		},
		{
			name: 'Normal',
			value: 3,
			icon: 'fab fa-font-awesome-flag text-info',
		},
		{
			name: 'Low',
			value: 4,
			icon: 'fab fa-font-awesome-flag text-muted',
		},
		{
			name: 'Clear',
			value: 0,
			icon: 'fas fa-times text-danger',
		},
	];

	public defaultColors: string[] = [
		'rgb(187, 181, 181)',
		'rgb(29, 126, 236)',
		'rgb(250, 162, 140)',
		'rgb(14, 201, 204)',
		'rgb(11, 165, 11)',
		'rgb(123, 58, 245)',
		'rgb(238, 177, 8)',
		'rgb(236, 100, 27)',
		'rgb(124, 212, 8)',
		'rgb(240, 56, 102)',
		'rgb(255, 0, 0)',
		'rgb(0, 0, 0)',
		'rgb(255, 0, 255)',
	];

	getName(val) {
		var x = val.split(' ');
		return x[x.length - 1];
	}

	list_account(filter: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		let params = this.httpUtils.parseFilter(filter);
		return this.http.get<any>(API_Lite + `/lite_account`, { headers: httpHeaders, params: params });
	}
	//get-list-default-view   
	list_default_view(filter: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		let params = this.httpUtils.parseFilter(filter);
		return this.http.get<any>(API_Lite + `/get-list-default-view`, { headers: httpHeaders, params: params });
	}
	ListViewByProject(id): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/list-view-project?id_project_team=${id}`, { headers: httpHeaders });
	}

	lite_workgroup(id_project_team: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/lite_workgroup?id_project_team=${id_project_team}`, { headers: httpHeaders });
	}

	lite_tag(id_project_team: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/lite_tag?id_project_team=${id_project_team}`, { headers: httpHeaders });
	}

	lite_milestone(id_project_team: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/lite_milestone?id_project_team=${id_project_team}`, { headers: httpHeaders });
	}
	lite_department(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/lite_department`, { headers: httpHeaders });
	}
	ListTemplateByCustomer(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/lite_template_by_customer`, { headers: httpHeaders });
	}
	lite_department_byuser(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/lite_department_byuser`, { headers: httpHeaders });
	}
	lite_project_team_byuser(keyword: string = ""): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/lite_project_team_byuser?keyword=${keyword}`, { headers: httpHeaders });
	}
	lite_project_team_bydepartment(keyword: string = ""): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/lite_project_team_bydepartment?id=${keyword}`, { headers: httpHeaders });
	}
	lite_emotion(id: number = 0): Observable<any> {

		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/lite_emotion?id=${id}`, { headers: httpHeaders });
	}
	getColorName(name): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/get-color-name?name=${name}`, { headers: httpHeaders });
	}
	getRolesByProjects(id_project_team): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/roles-by-project?id_project_team=${id_project_team}`, { headers: httpHeaders });
	}
	GetListField(filter: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/list-field${filter}`, { headers: httpHeaders });
	}
	GetNewField(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/list-new-field`, { headers: httpHeaders });
	}
	GetOptions_NewField(id_project_team, fieldID): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/get-options-new-field?id_project_team=${id_project_team}&&fieldID=${fieldID}`, { headers: httpHeaders });
	}
	//status
	ListStatusDynamic(id_project_team: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/list-status-dynamic?id_project_team=${id_project_team}`, { headers: httpHeaders });
	}
	ListStatusDynamicByDepartment(id_department: any): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/list-status-dynamic-bydepartment?id_department=${id_department}`, { headers: httpHeaders });
	}
	ListAllStatusDynamic(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(API_Lite + `/list-all-status-dynamic`, { headers: httpHeaders });
	}

	//#region nhắc nhở 
	Get_DSNhacNho(): Observable<QueryResultsModel> {
        const httpHeaders = this.httpUtils.getHTTPHeaders();
        return this.http.get<any>(environment.HOST_JEELANDINGPAGE_API + `/api/widgets/Get_DSNhacNho`, { headers: httpHeaders });
    }
	//#region nhắc nhở 
	// getTopicObjectIDByComponentName(componentName): Observable<any> {
    //     const httpHeaders = this.httpUtils.getHTTPHeaders();
	// 	const url = APIROOTS + `/api/comments/getByComponentName/${componentName}`;
    //     return this.http.get<any>(url, { headers: httpHeaders });
    // }
	//#endregion

	// setup jee-comment
	getTopicObjectIDByComponentName(componentName: string): Observable<string> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		const url = APIROOTS + `/api/comments/getByComponentName/${componentName}`;
		return this.http.get(url, {
		  headers: httpHeaders,
		  responseType: 'text'
		});
	  }


	// setup avatar 
	getNameUser(val) {
		if (val) {
			var list = val.split(' ');
			return list[list.length - 1];
		}
		return "";
	}

	getColorNameUser(fullname) {
		var name = this.getNameUser(fullname).substr(0, 1);
		var result = "#bd3d0a";
		switch (name) {
			case "A":
				result = "rgb(197, 90, 240)";
				break;
			case "Ă":
				result = "rgb(241, 196, 15)";
				break;
			case "Â":
				result = "rgb(142, 68, 173)";
				break;
			case "B":
				result = "#02c7ad";
				break;
			case "C":
				result = "#0cb929";
				break;
			case "D":
				result = "rgb(44, 62, 80)";
				break;
			case "Đ":
				result = "rgb(127, 140, 141)";
				break;
			case "E":
				result = "rgb(26, 188, 156)";
				break;
			case "Ê":
				result = "rgb(51 152 219)";
				break;
			case "G":
				result = "rgb(44, 62, 80)";
				break;
			case "H":
				result = "rgb(248, 48, 109)";
				break;
			case "I":
				result = "rgb(142, 68, 173)";
				break;
			case "K":
				result = "#2209b7";
				break;
			case "L":
				result = "#759e13";
				break;
			case "M":
				result = "rgb(236, 157, 92)";
				break;
			case "N":
				result = "#bd3d0a";
				break;
			case "O":
				result = "rgb(51 152 219)";
				break;
			case "Ô":
				result = "rgb(241, 196, 15)";
				break;
			case "Ơ":
				result = "rgb(142, 68, 173)";
				break;
			case "P":
				result = "rgb(142, 68, 173)";
				break;
			case "Q":
				result = "rgb(91, 101, 243)";
				break;
			case "R":
				result = "rgb(44, 62, 80)";
				break;
			case "S":
				result = "rgb(122, 8, 56)";
				break;
			case "T":
				result = "rgb(120, 76, 240)";
				break;
			case "U":
				result = "rgb(51 152 219)";
				break;
			case "Ư":
				result = "rgb(241, 196, 15)";
				break;
			case "V":
				result = "rgb(142, 68, 173)";
				break;
			case "X":
				result = "rgb(142, 68, 173)";
				break;
			case "W":
				result = "rgb(211, 84, 0)";
				break;
		}
		return result;
	}

}
