import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject, throwError } from 'rxjs';
import { Injectable } from '@angular/core';

import { map, catchError } from 'rxjs/operators';
import { QueryParamsModel } from '../models/query-models/query-params.model';
import { HttpUtilsService } from '../utils/http-utils.service';

@Injectable()
export class UserProfileService {
	lastFilter$: BehaviorSubject<QueryParamsModel> = new BehaviorSubject(new QueryParamsModel({}, 'asc', '', 0, 10));
	ReadOnlyControl: boolean;
	constructor(private http: HttpClient,
		private httpUtils: HttpUtilsService) { }
}
