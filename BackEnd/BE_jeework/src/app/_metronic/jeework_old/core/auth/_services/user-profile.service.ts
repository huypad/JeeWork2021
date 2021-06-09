import { HttpClient } from '@angular/common/http';
import { Observable,  BehaviorSubject } from 'rxjs';
import { Injectable } from '@angular/core';
import { environment } from 'src/environments/environment';
import { HttpUtilsService } from '../../_base/crud/utils/http-utils.service';
import { QueryParamsModel, QueryResultsModel } from '../../_base/crud';
import { MasterPageNhanVienModel } from '../_models/user.model';
import { map } from 'rxjs/operators';
@Injectable()
export class UserProfileService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	ReadOnlyControl: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }
	getDictionary(): Observable<any> {
		const httpHeaders = this.httpUtils.getHTTPHeaders();
		return this.http.get<any>(environment.APIROOTS + `api/wework-lite/get-dicionary`, { headers: httpHeaders });
	}
}
