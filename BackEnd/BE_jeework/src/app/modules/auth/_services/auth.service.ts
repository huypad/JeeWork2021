import { HttpHeaders, HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, OnDestroy } from "@angular/core";
import { Observable, BehaviorSubject, of, Subscription } from "rxjs";
import { map, catchError, switchMap, finalize } from "rxjs/operators";
import { UserModel } from "../_models/user.model";
import { AuthModel } from "../_models/auth.model";
import { AuthHTTPService } from "./auth-http";
import { environment } from "src/environments/environment";
import { Router } from "@angular/router";
import jwt_decode from 'jwt-decode';

const redirectUrl = environment.REDIRECTURL;
const API_IDENTITY = `${environment.APIIDENTITY}`;
const API_IDENTITY_LOGOUT = `${environment.APIIDENTITY_LOGOUT}`;
const API_IDENTITY_USER = `${environment.APIIDENTITY_GETUSER}`;
const API_IDENTITY_REFESHTOKEN = `${environment.APIIDENTITY_REFRESH}`;
@Injectable({
  providedIn: "root",
})
export class AuthService implements OnDestroy {
  // private fields
  private unsubscribe: Subscription[] = []; // Read more: => https://brianflove.com/2016/12/11/anguar-2-unsubscribe-observables/
  authLocalStorageToken = `${environment.APPVERSION}-${environment.USERDATA_KEY}`;

  // public fields
  currentUser$: Observable<UserModel>;
  currentUserSubject: BehaviorSubject<UserModel>;
  isLoadingSubject: BehaviorSubject<boolean>;
  authSSOModel$: Observable<AuthSSO>;
  authSSOModelSubject$: BehaviorSubject<AuthSSO> = new BehaviorSubject<AuthSSO>(
    undefined
  );
  // Private fields
  isLoading$ = new BehaviorSubject<boolean>(false);
  isFirstLoading$ = new BehaviorSubject<boolean>(true);
  errorMessage = new BehaviorSubject<string>(undefined);
  subscriptions: Subscription[] = [];
  ssoToken$: BehaviorSubject<string> = new BehaviorSubject<string>(undefined);
  accessToken$ = new BehaviorSubject<string>(undefined);
  refreshToken$ = new BehaviorSubject<string>(undefined);

  get currentUserValue(): UserModel {
    return this.currentUserSubject.value;
  }

  set currentUserValue(user: UserModel) {
    this.currentUserSubject.next(user);
  }

  constructor(
    private authHttpService: AuthHTTPService,
    private router: Router,
    private http: HttpClient,
  ) {
    this.isLoadingSubject = new BehaviorSubject<boolean>(false);
    this.currentUserSubject = new BehaviorSubject<UserModel>(undefined);
    this.currentUser$ = this.currentUserSubject.asObservable();
    this.isLoading$ = this.isLoadingSubject;
    const subscr = this.getUserByToken().subscribe();
    this.unsubscribe.push(subscr);
    this.authSSOModel$ = this.authSSOModelSubject$.asObservable();
    this.ssoToken$.next(this.getParamsSSO());
    setInterval(() => this.autoGetUserFromSSO(), 60000);
  }

  // public methods
  login(email: string, password: string): Observable<UserModel> {
    this.isLoadingSubject.next(true);
    return this.authHttpService.login(email, password).pipe(
      map((auth: AuthModel) => {
        const result = this.setAuthFromLocalStorage(auth);
        return result;
      }),
      switchMap(() => this.getUserByToken()),
      catchError((err) => {
        console.error("err", err);
        return of(undefined);
      }),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  // logout() {
  //   localStorage.removeItem(this.authLocalStorageToken);
  //   this.router.navigate(["/auth/login"], {
  //     queryParams: {},
  //   });
  // }

  autoGetUserFromSSO() {
    const auth = this.getAuthFromLocalStorage();
    if (auth) {
      this.saveNewUserMe();
    }
  }

  saveNewUserMe(access_token?: string, refresh_token?: string) {
    if (access_token) this.accessToken$.next(access_token);
    if (refresh_token) this.refreshToken$.next(refresh_token);
    this.getUserMeFromSSO().subscribe(
      (data) => {
        if (data && data.access_token) {
          this.saveLocalStorageToken(this.authLocalStorageToken, data);
        }
      },
      (error) => {
        this.refreshToken().subscribe(
          (data: AuthSSO) => {
            if (data && data.access_token) {
              this.saveLocalStorageToken(this.authLocalStorageToken, data);
            }
          },
          (error) => {
            // localStorage.removeItem(this.authLocalStorageToken);
            // this.logout();
          }
        );
      }
    );
  }

  isAuthenticated(): boolean {
    const auth = this.getAuthFromLocalStorage();
    if (auth) {
      if (this.isTokenExpired(auth.access_token)) {
        this.saveAuthSSOModelAccessTokenRefreshToken(auth);
        return true;
      }
      if (this.isTokenExpired(auth.refresh_token)) {
        this.saveAuthSSOModelAccessTokenRefreshToken(auth);
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

  saveAuthSSOModelAccessTokenRefreshToken(auth) {
    if (!this.authSSOModelSubject$.getValue()) {
      const authSSOModel = new AuthSSO();
      authSSOModel.setAuthSSO(auth);
      this.authSSOModelSubject$.next(authSSOModel);
      this.authSSOModel$ = this.authSSOModelSubject$.asObservable();
    }
    if (!this.accessToken$.getValue()) this.accessToken$.next(auth.access_token);
    if (!this.refreshToken$.getValue()) this.refreshToken$.next(auth.refresh_token);
  }

  getTokenExpirationDate(auth: string): Date {
    let decoded: any = jwt_decode(auth);
    if (!decoded.exp) return null;
    const date = new Date(0);
    date.setUTCSeconds(decoded.exp);
    return date;
  }

  logout() {
    // localStorage.removeItem(this.authLocalStorageToken);
    let url = redirectUrl + document.location.protocol + '//' + document.location.hostname + ':' + document.location.port;
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
  // call api identity server
  getUserMeFromSSO(): Observable<any> {
    const accessToken = this.accessToken$.getValue();
    const auth = this.getAuthFromLocalStorage();
    const url = API_IDENTITY_USER;
    const httpHeader = new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${accessToken!=null?accessToken:(auth!=null?auth.access_token:'')}`,
    });
    return this.http.get<any>(url, { headers: httpHeader });
  }

  refreshToken(): Observable<any> {
    const url = API_IDENTITY_REFESHTOKEN;
    const httpHeader = new HttpHeaders({
      'Content-Type': 'application/json',
      Authorization: `Bearer ${this.refreshToken$.getValue()}`,
    });
    return this.http.post<any>(url, null, { headers: httpHeader });
  }

  logoutToSSO(): Observable<any> {
    const url = API_IDENTITY_LOGOUT;
    const accessToken = this.accessToken$.getValue();
    const auth = this.getAuthFromLocalStorage();
		const httpHeader = new HttpHeaders({
			'Content-Type': 'application/json', 
			'Authorization': `Bearer ${auth!=null?auth.access_token:''}`,
		});
    // const httpHeader = new HttpHeaders({
    //   'Content-Type': 'application/json',
    //   Authorization: `Bearer ${accessToken}`,
    // });
    return this.http.post<any>(url, null, { headers: httpHeader });
  }

  // end call api identity server
  saveLocalStorageToken(key: string, value: any) {
    localStorage.setItem(key, JSON.stringify(value));
    this.authSSOModelSubject$.next(value);
    this.authSSOModel$ = this.authSSOModelSubject$.asObservable();
    this.accessToken$.next(value.access_token);
    this.refreshToken$.next(value.refresh_token);
  }


  // old 

  getUserByToken(): Observable<UserModel> {
    const auth = this.getAuthFromLocalStorage();
    if (!auth || !auth.accessToken) {
      return of(undefined);
    }

    this.isLoadingSubject.next(true);
    return this.authHttpService.getUserByToken(auth.accessToken).pipe(
      map((user: UserModel) => {
        if (user) {
          this.currentUserSubject = new BehaviorSubject<UserModel>(user);
        } else {
          this.logout();
        }
        return user;
      }),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  // need create new user then login
  registration(user: UserModel): Observable<any> {
    this.isLoadingSubject.next(true);
    return this.authHttpService.createUser(user).pipe(
      map(() => {
        this.isLoadingSubject.next(false);
      }),
      switchMap(() => this.login(user.email, user.password)),
      catchError((err) => {
        console.error("err", err);
        return of(undefined);
      }),
      finalize(() => this.isLoadingSubject.next(false))
    );
  }

  forgotPassword(email: string): Observable<boolean> {
    this.isLoadingSubject.next(true);
    return this.authHttpService
      .forgotPassword(email)
      .pipe(finalize(() => this.isLoadingSubject.next(false)));
  }

  // private methods
  private setAuthFromLocalStorage(auth: AuthModel): boolean {
    // store auth accessToken/refreshToken/epiresIn in local storage to keep user logged in between page refreshes
    if (auth && auth.accessToken) {
      localStorage.setItem(this.authLocalStorageToken, JSON.stringify(auth));
      return true;
    }
    return false;
  }

  public getAuthFromLocalStorage(): any {
    try {
      const authData = JSON.parse(
        localStorage.getItem(this.authLocalStorageToken)
      );
      return authData;
    } catch (error) {
      console.error(error);
      return undefined;
    }
  }
  ngOnDestroy() {
    this.unsubscribe.forEach((sb) => sb.unsubscribe());
  }
}

export class AuthSSO {
  user: any;
  access_token: string;
  refresh_token: string;

  setAuthSSO(auth: any) {
    this.user = auth.user;
    this.access_token = auth.access_token;
    this.refresh_token = auth.refresh_token;
  }
}
