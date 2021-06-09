import { environment } from 'src/environments/environment';
import { Inject, Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject, Observable, of, Subject, throwError } from 'rxjs';
import { User } from '../_models/user.model';
import { catchError, map, tap } from 'rxjs/operators';
import { HttpUtilsService, QueryParamsModel, QueryResultsModel } from '../../_base/crud';
import { ErrorModel, ApiResponseModel } from '../_models/api-response-model';
import { TokenStorage } from './token-storage.service';
import { AccessData } from '../../_base/crud/models/interfaces/access-data';
import { DOCUMENT } from '@angular/common';
import { BaseModel } from 'src/app/_metronic/shared/crud-table';
const redirectUrl = environment.REDIRECTURL;
@Injectable()
export class AuthenticationService {
    API_IDENTITY = `${environment.APIIDENTITY}`;
    public onCredentialUpdated$: Subject<AccessData>;
    public ldp_loadDataUser: string = '/user/me';
    public ldp_logOutUser: string = '/user/logout';
    public ldp_refresh: string = '/user/refresh';
    private _errorMessage = new BehaviorSubject<string>('');
    public authLocalStorageToken = `${environment.APPVERSION}-${environment.USERDATA_KEY}`;
    constructor(
        private http: HttpClient,
        private httpUtils: HttpUtilsService,
        private tokenStorage: TokenStorage,
        @Inject(DOCUMENT) private document: Document,
    ) {
        ;
        this.onCredentialUpdated$ = new Subject();
    }

    /**
     * Save access data in the storage
     * @private
     * @param {response} data
     */
    private saveAccessData(response: ApiResponseModel) {
        ;
        if (typeof response !== 'undefined' && response.status === 1) {
            var accessData = {
                accessToken: response.token,
                refreshToken: response.token,
                //roles: response.data.Rules,
                roles: []
            };
            accessData.roles.push('ADMIN');
            this.tokenStorage
                .setAccessToken(accessData.accessToken)
                .setRefreshToken(accessData.refreshToken)
                .setUserRoles(accessData.roles);
            this.onCredentialUpdated$.next(accessData);
        }
        else {
            throwError({ msg: 'error' });
        }
    }

    // getUserByToken(): Observable<User> {
    //     ;
    //     const userToken = localStorage.getItem(environment.AUTHTOKENKEY);
    //     const httpHeaders = new HttpHeaders();
    //     httpHeaders.append('Authorization', 'Bearer ' + userToken);
    //     return this.http.get<User>(API_URL + API_LOGIN_URL, { headers: httpHeaders });
    // }


    /*
     * Submit forgot password request
     *
     * @param {string} email
     * @returns {Observable<any>}
     */

    private handleError<T>(operation = 'operation', result?: any) {
        return (error: any): Observable<any> => {
            // TODO: send the error to remote logging infrastructure
            console.error(error); // log to console instead

            // Let the app keep running by returning an empty result.
            return of(result);
        };
    }

    public getUserRoles(): Observable<any> {
        return this.tokenStorage.getUserRoles();
    }

    public getAccessToken(): Observable<string> {
        return this.tokenStorage.getAccessToken();
    }

    // public logout(refresh?: boolean, url?: string): void {
    //     this.tokenStorage.clear();
    //     if (refresh) {
    //         window.location.href = url;
    //     }
    // }
    logout() {
        this.logOutUser(this.ldp_logOutUser).subscribe(
            (resData: any) => {
                //bên kia trả về null
                localStorage.removeItem(this.authLocalStorageToken);
                // Chuyển hướng người dùng đến Single Sign On
                this.document.location.href = redirectUrl
                    + document.location.protocol + '//'
                    + document.location.hostname + ':'
                    + document.location.port;

            }
        );

    }
    logOutUser(routeFind: string = ''): Observable<BaseModel> {
        const url = this.API_IDENTITY + routeFind;
        const httpHeader = this.httpUtils.getHTTPHeaders();
        return this.http.post<any>(url, null, { headers: httpHeader })
            .pipe(
                tap((res) => { }),
                catchError(err => {
                    this._errorMessage.next(err);
                    console.error('lỗi logout', err);
                    // Chuyển hướng người dùng đến Single Sign On
                    this.chuyenHuongSSO();
                    return of({ id: undefined });
                })

            );
    }
    getDataUser_LandingPage(routeFind: string = '', sso_token: string = ''): Observable<BaseModel> {
        const url = this.API_IDENTITY + routeFind;
        const httpHeader = new HttpHeaders({
            'Content-Type': 'application/json',
            "Authorization": sso_token
        });

        return this.http.get<BaseModel>(url, { headers: httpHeader })
            .pipe(
                tap((res) => {
                }),
                catchError(err => {
                    this._errorMessage.next(err);
                    console.error('lỗi lấy data', err);
                    return of({ id: undefined });
                })

            );
    }
    chuyenHuongSSO() {
        document.location.href = redirectUrl
            + document.location.protocol + '//'
            + document.location.hostname + ':'
            + document.location.port;
    }
    refreshToken(userMe: string = '', userRefresh: string = ''): Observable<BaseModel> {
        const urlUserMe = this.API_IDENTITY + userMe;
        const urlUserRefresh = this.API_IDENTITY + userRefresh;
        const httpHeader = this.httpUtils.getHTTPHeaders();
        return this.http.get<BaseModel>(urlUserMe, { headers: httpHeader })
            .pipe(
                tap((res) => {
                }),
                catchError(err => {
                    const httpHeaderRefresh = this.httpUtils.getHttpHeadersRefresh();
                    return this.http.post<BaseModel>(urlUserRefresh, null, { headers: httpHeaderRefresh }).pipe(
                        tap((res) => {
                        }),
                        catchError(err => {
                            console.error('Lỗi', err);
                            localStorage.removeItem(this.authLocalStorageToken);
                            return of({ id: undefined });
                        })

                    );
                })
            );
    }
}
