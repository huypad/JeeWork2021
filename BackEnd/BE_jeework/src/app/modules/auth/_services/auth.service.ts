import { CookieService } from 'ngx-cookie-service';
import { Injectable, OnDestroy, OnInit } from '@angular/core';
import { Observable, BehaviorSubject, of, Subscription } from 'rxjs';
import { map, finalize } from 'rxjs/operators';
import { UserModel } from '../_models/user.model';
import { AuthHTTPService } from './auth-http';
import { environment } from 'src/environments/environment';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import jwt_decode from 'jwt-decode';
import { AuthSSO } from '../_models/authSSO.model';

const redirectUrl = environment.REDIRECTURL + '/?redirectUrl=';
const API_IDENTITY = `${environment.APIIDENTITY}`;
const API_IDENTITY_LOGOUT = `${API_IDENTITY}/user/logout`;
const API_IDENTITY_USER = `${API_IDENTITY}/user/me`;
const API_IDENTITY_REFESHTOKEN = `${API_IDENTITY}/user/refresh`;
const KEY_SSO_TOKEN = 'sso_token';
const KEY_RESRESH_TOKEN = 'sso_token_refresh';
const DOMAIN = environment.DOMAIN_COOKIES;
@Injectable({
    providedIn: 'root',
})
export class AuthService implements OnDestroy {
    // private fields
    private unsubscribe: Subscription[] = [];

    // public fields
    currentUser$: Observable<UserModel>;

    currentUserSubject: BehaviorSubject<UserModel> = new BehaviorSubject<UserModel>(undefined);
    authSSOModelSubject$: BehaviorSubject<AuthSSO> = new BehaviorSubject<AuthSSO>(undefined);
    // Private fields
    isLoading$ = new BehaviorSubject<boolean>(false);
    isFirstLoading$ = new BehaviorSubject<boolean>(true);
    errorMessage = new BehaviorSubject<string>(undefined);
    subscriptions: Subscription[] = [];

    private userSubject = new BehaviorSubject<any | null>(null);

    User$: Observable<any> = this.userSubject.asObservable();

    constructor(private http: HttpClient, private authHttpService: AuthHTTPService, private cookieService: CookieService) {
        this.isLoading$ = new BehaviorSubject<boolean>(false);
        if (this.getAccessToken_cookie()) {

            this.getUserMeFromSSO().subscribe(
                (data) => {
                    if (data && data.access_token) {
                        this.userSubject.next(data);
                        this.saveToken_cookie(data.access_token, data.refresh_token);
                    }
                },
                (error) => {
                    this.refreshToken().subscribe(
                        (data: AuthSSO) => {
                            if (data && data.access_token) {
                                this.userSubject.next(data);
                                this.saveToken_cookie(data.access_token, data.refresh_token);
                            }
                        },
                        (error) => {
                            this.logout();
                        }
                    );
                },
                () => {
                    setInterval(() => {
                        if (!this.getAccessToken_cookie() && !this.getRefreshToken_cookie()) this.prepareLogout();
                    }, 3000);
                }
            );
        }
        setInterval(() => this.autoGetUserFromSSO(), 60000);
    }

    get currentUserValue(): UserModel {
        return this.currentUserSubject.value;
    }

    set currentUserValue(user: UserModel) {
        this.currentUserSubject.next(user);
    }

    getUserId() {
        var auth = this.getAuthFromLocalStorage();
        return auth.user.customData['jee-account'].userID;
    }
    getAccessToken_cookie() {
        const access_token = this.cookieService.get(KEY_SSO_TOKEN);
        return access_token;
    }

    saveToken_cookie(access_token?: string, refresh_token?: string) {
        if (access_token) this.cookieService.set(KEY_SSO_TOKEN, access_token, 365, '/', DOMAIN);
        if (refresh_token) this.cookieService.set(KEY_RESRESH_TOKEN, refresh_token, 365, '/', DOMAIN);
    }

    getRefreshToken_cookie() {
        const sso_token = this.cookieService.get(KEY_RESRESH_TOKEN);
        return sso_token;
    }

    deleteAccessRefreshToken_cookie() {
        this.cookieService.delete(KEY_SSO_TOKEN, '/', DOMAIN);
        this.cookieService.delete(KEY_RESRESH_TOKEN, '/', DOMAIN);
    }

    autoGetUserFromSSO() {
        const auth = this.getAuthFromLocalStorage();
        if (auth) {
            this.saveNewUserMe();
        }
    }

    saveNewUserMe(data?: any) {
        if (data) {
            this.userSubject.next(data);
            this.saveToken_cookie(data.access_token, data.refresh_token);
        }
        this.getUserMeFromSSO().subscribe(
            (data) => {
                if (data && data.access_token) {
                    this.userSubject.next(data);
                    this.saveToken_cookie(data.access_token, data.refresh_token);
                }
            },
            (error) => {
                this.refreshToken().subscribe(
                    (data: AuthSSO) => {
                        if (data && data.access_token) {
                            this.userSubject.next(data);
                            this.saveToken_cookie(data.access_token, data.refresh_token);
                        }
                    },
                    (error) => {
                        this.logout();
                    }
                );
            }
        );
    }

    isAuthenticated(): boolean {
        const access_token = this.getAccessToken_cookie();
        const refresh_token = this.getRefreshToken_cookie();
        if (access_token) {
            if (this.isTokenExpired(access_token)) {
                this.saveToken_cookie(access_token);
                return true;
            }
        }
        if (refresh_token) {
            if (this.isTokenExpired(refresh_token)) {
                this.saveToken_cookie(undefined, refresh_token);
                return true;
            }
        }
        return false;
    }

    isTokenExpired(token: string): boolean {
        const date = this.getTokenExpirationDate(token);
        if (!date) return false;
        return date.valueOf() > new Date().valueOf();
    }

    getTokenExpirationDate(auth: string): Date {
        let decoded: any = jwt_decode(auth);
        if (!decoded.exp) return null;
        const date = new Date(0);
        date.setUTCSeconds(decoded.exp);
        return date;
    }

    logout() {
        this.ClearChatBox();
        localStorage.clear();
        const access_token = this.getAccessToken_cookie();
        if (access_token) {
            this.logoutToSSO().subscribe(
                (res) => {
                    this.prepareLogout();
                },
                (err) => {
                    this.prepareLogout();
                }
            );
        } else {
            this.prepareLogout();
        }
    }
    ClearChatBox()
    {
      localStorage.removeItem('chatboxusers');
      localStorage.removeItem('chatGroup');
    }
    prepareLogout() {
        this.deleteAccessRefreshToken_cookie();
        let url = '';
        if (document.location.port) {
            url = redirectUrl + document.location.protocol + '//' + document.location.hostname + ':' + document.location.port;
        } else {
            url = redirectUrl + document.location.protocol + '//' + document.location.hostname;
        }
        window.location.href = url;
    }

    getParamsSSO() {
        const url = window.location.href;
        let paramValue = undefined;
        if (url.includes('?')) {
            const httpParams = new HttpParams({ fromString: url.split('?')[1] });
            paramValue = httpParams.get('sso_token');
        }
        return paramValue;
    }

    getAuthFromLocalStorage() {
        return this.userSubject.value;
    }

    ngOnDestroy() {
        this.unsubscribe.forEach((sb) => sb.unsubscribe());
    }

    // call api identity server
    getUserMeFromSSO(): Observable<any> {
        const access_token = this.getAccessToken_cookie();
        const url = API_IDENTITY_USER;
        const httpHeader = new HttpHeaders({
            'Content-Type': 'application/json',
            Authorization: `Bearer ${access_token}`,
        });
        return this.http.get<any>(url, { headers: httpHeader });
    }

    refreshToken(): Observable<any> {
        const refresh_token = this.getRefreshToken_cookie();
        const url = API_IDENTITY_REFESHTOKEN;
        const httpHeader = new HttpHeaders({
            'Content-Type': 'application/json',
            Authorization: `Bearer ${refresh_token}`,
        });
        return this.http.post<any>(url, null, { headers: httpHeader });
    }

    logoutToSSO(): Observable<any> {
        const access_token = this.getAccessToken_cookie();
        const url = API_IDENTITY_LOGOUT;
        const httpHeader = new HttpHeaders({
            'Content-Type': 'application/json',
            Authorization: `Bearer ${access_token}`,
        });
        return this.http.post<any>(url, null, { headers: httpHeader });
    }

    // end call api identity server

    // method metronic call
    getUserByToken(): Observable<UserModel> {
        const auth = this.getAuthFromLocalStorage();
        if (!auth || !auth.accessToken) {
            return of(undefined);
        }
        this.isLoading$.next(true);
        return this.authHttpService.getUserByToken(auth.accessToken).pipe(
            map((user: UserModel) => {
                if (user) {
                    this.currentUserSubject = new BehaviorSubject<UserModel>(user);
                } else {
                    this.logout();
                }
                return user;
            }),
            finalize(() => this.isLoading$.next(false))
        );
    }

    forgotPassword(value: any): Observable<any> {
        throw new Error('Method not implemented.');
    }
    registration(newUser: UserModel): Observable<any> {
        throw new Error('Method not implemented.');
    }

    getStaffId() {
        var auth = this.getAuthFromLocalStorage();
        return auth.user.customData['jee-account'].staffID;
    }
    getAppCodeId() {
        var auth = this.getAuthFromLocalStorage();
        return auth.user.customData['jee-account'].appCode;
    }
}
