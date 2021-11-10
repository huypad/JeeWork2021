import { Permission } from './../auth/_models/permission.model';
import { Role } from './../auth/_models/role.model';
import { User } from './../../../../modules/material/formcontrols/autocomplete/autocomplete.component';
import { environment } from 'src/environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable, of, Subject, throwError } from 'rxjs';

import { catchError, map, tap } from 'rxjs/operators';
import { TokenStorage } from './token-storage.service';
import { QueryResultsModel } from '../models/query-models/query-results.model';
import { QueryParamsModel } from '../models/query-models/query-params.model';

const API_USERS_URL = 'api/users';//'api/users'
const API_USERS = environment.APIROOTS +'/user';
const API_PERMISSION_URL = 'api/permissions';
const API_ROLES_URL = 'api/roles';
const API_LOGIN_URL = environment.APIROOTS + '/user/Login';
const API_LOGOUT_URL = environment.APIROOTS + '/user/Logout';

@Injectable()
export class AuthService {
	constructor(private http: HttpClient,
		private tokenStorage: TokenStorage) { }
	
}
