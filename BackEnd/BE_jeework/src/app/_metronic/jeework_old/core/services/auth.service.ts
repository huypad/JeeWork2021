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
const API_USERS = environment.ApiRoot +'/user';
const API_PERMISSION_URL = 'api/permissions';
const API_ROLES_URL = 'api/roles';
const API_LOGIN_URL = environment.ApiRoot + '/user/Login';
const API_LOGOUT_URL = environment.ApiRoot + '/user/Logout';

@Injectable()
export class AuthService {
	constructor(private http: HttpClient,
		private tokenStorage: TokenStorage) { }
	// Authentication/Authorization
	login(username: string, password: string, checkReCaptCha: boolean, GReCaptCha: string): Observable<any> {
		let data = {
			username: username,
			password: password,
			checkReCaptCha: checkReCaptCha,
			GReCaptCha: GReCaptCha
		}
		return this.http.post<any>(API_LOGIN_URL, data)
			.pipe(
				map((result: any) => {
					return result;
				}),
				tap(this.saveAccessData.bind(this)),
				catchError(this.handleError('login', []))
			);
	}
	private saveAccessData(response: any) {
		if (response && response.status === 1) {
			this.tokenStorage.updateStorage(response.data);
		}
		else {
			throwError({ msg: 'error' });
		}
	}

	getUserByToken(): Observable<User> {
		
		const userToken = localStorage.getItem(environment.authTokenKey);
		const httpHeaders = new HttpHeaders();
		httpHeaders.set('Authorization', 'Bearer ' + userToken);
		return this.http.get<User>(API_USERS_URL, { headers: httpHeaders });
	}

	register(user: User): Observable<any> {
		
		const httpHeaders = new HttpHeaders();
		httpHeaders.set('Content-Type', 'application/json');
		return this.http.post<User>(API_USERS_URL, user, { headers: httpHeaders })
			.pipe(
				map((res: User) => {
					return res;
				}),
				catchError(err => {
					return null;
				})
			);
	}

    /*
     * Submit forgot password request
     *
     * @param {string} email
     * @returns {Observable<any>}
     */
	public requestPassword(email: string): Observable<any> {
		
		return this.http.get(API_USERS + '/ForgotPassword?username=' + email)
			.pipe(catchError(this.handleError('forgot-password', []))
			);
	}


	getAllUsers(): Observable<User[]> {
		
		return this.http.get<User[]>(API_USERS_URL);
	}

	getUserById(userId: number): Observable<User> {
		
		return this.http.get<User>(API_USERS_URL + `/${userId}`);
	}


	// DELETE => delete the user from the server
	deleteUser(userId: number) {
		
		const url = `${API_USERS_URL}/${userId}`;
		return this.http.delete(url);
	}

	// UPDATE => PUT: update the user on the server
	updateUser(_user: User): Observable<any> {
		
		const httpHeaders = new HttpHeaders();
		httpHeaders.set('Content-Type', 'application/json');
		return this.http.put(API_USERS_URL, _user, { headers: httpHeaders });
	}

	// CREATE =>  POST: add a new user to the server
	createUser(user: User): Observable<User> {
		
		const httpHeaders = new HttpHeaders();
		httpHeaders.set('Content-Type', 'application/json');
		return this.http.post<User>(API_USERS_URL, user, { headers: httpHeaders });
	}

	// Method from server should return QueryResultsModel(items: any[], totalsCount: number)
	// items => filtered/sorted result
	findUsers(queryParams: QueryParamsModel): Observable<QueryResultsModel> {
		
		const httpHeaders = new HttpHeaders();
		httpHeaders.set('Content-Type', 'application/json');
		return this.http.post<QueryResultsModel>(API_USERS_URL + '/findUsers', queryParams, { headers: httpHeaders });
	}

	// Permission
	getAllPermissions(): Observable<Permission[]> {
		return this.http.get<Permission[]>(API_PERMISSION_URL);
	}

	getRolePermissions(roleId: number): Observable<Permission[]> {
		return this.http.get<Permission[]>(API_PERMISSION_URL + '/getRolePermission?=' + roleId);
	}

	// Roles
	getAllRoles(): Observable<Role[]> {
		return this.http.get<Role[]>(API_ROLES_URL);
	}

	getRoleById(roleId: number): Observable<Role> {
		return this.http.get<Role>(API_ROLES_URL + `/${roleId}`);
	}

	// CREATE =>  POST: add a new role to the server
	createRole(role: Role): Observable<Role> {
		// Note: Add headers if needed (tokens/bearer)
		const httpHeaders = new HttpHeaders();
		httpHeaders.set('Content-Type', 'application/json');
		return this.http.post<Role>(API_ROLES_URL, role, { headers: httpHeaders });
	}

	// UPDATE => PUT: update the role on the server
	updateRole(role: Role): Observable<any> {
		const httpHeaders = new HttpHeaders();
		httpHeaders.set('Content-Type', 'application/json');
		return this.http.put(API_ROLES_URL, role, { headers: httpHeaders });
	}

	// DELETE => delete the role from the server
	deleteRole(roleId: number): Observable<Role> {
		const url = `${API_ROLES_URL}/${roleId}`;
		return this.http.delete<Role>(url);
	}

	// Check Role Before deletion
	isRoleAssignedToUsers(roleId: number): Observable<boolean> {
		return this.http.get<boolean>(API_ROLES_URL + '/checkIsRollAssignedToUser?roleId=' + roleId);
	}

	findRoles(queryParams: QueryParamsModel): Observable<QueryResultsModel> {
		// This code imitates server calls
		const httpHeaders = new HttpHeaders();
		httpHeaders.set('Content-Type', 'application/json');
		return this.http.post<QueryResultsModel>(API_ROLES_URL + '/findRoles', queryParams, { headers: httpHeaders });
	}

	/*
	 * Handle Http operation that failed.
	 * Let the app continue.
   *
   * @param operation - name of the operation that failed
	 * @param result - optional value to return as the observable result
	 */
	private handleError<T>(operation = 'operation', result?: any) {
		return (error: any): Observable<any> => {
			// TODO: send the error to remote logging infrastructure
			console.error(error); // log to console instead

			// Let the app keep running by returning an empty result.
			return of(result);
		};
	}

	resetSession(): Observable<any> {
		const userToken = localStorage.getItem(environment.authTokenKey);
		const httpHeaders = new HttpHeaders({
			'Authorization': 'Bearer ' + userToken,
		});
		httpHeaders.append("Content-Type", "application/json");
		return this.http.post<any>(environment.ApiRoot + '/user/ResetSession', null, { headers: httpHeaders })
			.pipe(
				map((res: any) => {
					return res;
				}),
				catchError(err => {
					return throwError(err);
				})
			);
	}
	public logout(refresh?: boolean): void {
		this.logout_new().subscribe(
			res => { },
			err => { },
			() => {
				this.tokenStorage.clear();
				if (refresh) {
					location.reload(true);
				}
			});
	}
	logout_new(): Observable<any> {
		const userToken = localStorage.getItem(environment.authTokenKey);
		const httpHeaders = new HttpHeaders({
			'Authorization': 'Bearer ' + userToken,
		});
		httpHeaders.append("Content-Type", "application/json");
		return this.http.get<any>(API_LOGOUT_URL, { headers: httpHeaders })
			.pipe(
				map((res: any) => {
					return res;
				}),
				catchError(err => {
					return throwError(err);
				})
			);
	}
	//#region service worker
	CreateFCM(): Observable<any> {
		const userToken = localStorage.getItem(environment.authTokenKey);
		const httpHeaders = new HttpHeaders({
			'Authorization': 'Bearer ' + userToken,
		});
		return this.http.get<any>(environment.ApiRoot + `/fcm/CreateFCM`, { headers: httpHeaders });
	}
	DeleteFCM(data): Observable<any> {
		const userToken = localStorage.getItem(environment.authTokenKey);
		const httpHeaders = new HttpHeaders({
			'Authorization': 'Bearer ' + userToken,
		});
		httpHeaders.append("Content-Type", "application/json");
		return this.http.post<any>(environment.ApiRoot + `/fcm/DeleteFCM`, data, { headers: httpHeaders });
	}
	//#endregion

	//#region vai trò
	getVaiTro(): Observable<any> {
		const userToken = localStorage.getItem(environment.authTokenKey);
		const httpHeaders = new HttpHeaders({
			'Authorization': 'Bearer ' + userToken,
		});
		httpHeaders.append("Content-Type", "application/json");
		return this.http.get<any>(environment.ApiRoot + '/user/ds-vai-tro', { headers: httpHeaders });
	}

	doiVaiTro(id): Observable<any> {
		const userToken = localStorage.getItem(environment.authTokenKey);
		const httpHeaders = new HttpHeaders({
			'Authorization': 'Bearer ' + userToken,
		});
		httpHeaders.append("Content-Type", "application/json");
		return this.http.get<any>(environment.ApiRoot + '/user/doi-vai-tro?VaiTro=' + id, { headers: httpHeaders });
	}
	//#endregion
}
