import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, forkJoin, BehaviorSubject, of } from 'rxjs';
import { mergeMap } from 'rxjs/operators';
import { HttpUtilsService } from '../utils/http-utils.service';
import { QueryParamsModel } from '../models/query-models/query-params.model';
import { QueryResultsModel } from '../models/query-models/query-results.model';
import { environment } from 'src/environments/environment';

const API_URL = environment.APIROOTS;
const API_URL_Landingpage = environment.APIROOTSLANDING + '/apild';

@Injectable()
export class DanhMucChungService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));

	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }
	GetListDepartmentbyBranch(id_dv: string): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<QueryResultsModel>(API_URL + `/controllergeneral/GetListDepartmentbyBranch?id_DV=${id_dv}`, { headers: httpHeaders });
	}
	GetListPositionbyDepartment(id_bp: string): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<QueryResultsModel>(API_URL + `/controllergeneral/GetListPositionbyDepartment?id_bp=${id_bp}`, { headers: httpHeaders });
	}
	GetListJobtitleByPosition(id_cv: string, id_bp: string): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<QueryResultsModel>(API_URL + `/controllergeneral/GetListJobtitleByPosition?id_cv=${id_cv}&&id_bp=${id_bp}`, { headers: httpHeaders });
	}
	GetListPositionbyStructure_All(structureid: string): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<QueryResultsModel>(API_URL + `/controllergeneral/GetListPositionbyStructure_All?structureid=${structureid}`, { headers: httpHeaders });
	}
	Get_CoCauToChuc(): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<QueryResultsModel>(API_URL + `/controllergeneral/Get_CoCauToChuc_HR`, { headers: httpHeaders });
	}
	Get_MaCoCauToChuc_HR(): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<QueryResultsModel>(API_URL + `/controllergeneral/Get_MaCoCauToChuc_HR`, { headers: httpHeaders });
	}
	GetListPositionbyStructure(structureid: string): Observable<QueryResultsModel> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<QueryResultsModel>(API_URL + `/controllergeneral/GetListPositionbyStructure?structureid=${structureid}`, { headers: httpHeaders });
	}

}
