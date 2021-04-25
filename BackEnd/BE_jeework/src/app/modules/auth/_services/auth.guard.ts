import { TokenStorage } from './../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { HttpUtilsService } from './../../../_metronic/jeework_old/core/_base/crud/utils/http-utils.service';
import { DOCUMENT } from '@angular/common';
import { HttpParams } from '@angular/common/http';
import { Inject, Injectable } from '@angular/core';
import jwt_decode from "jwt-decode";
import {
  Router,
  CanActivate,
  ActivatedRouteSnapshot,
  RouterStateSnapshot,
} from '@angular/router';
import { BehaviorSubject, Observable, Subscription } from 'rxjs';
import { AuthenticationService } from 'src/app/_metronic/jeework_old/core/auth/_services/auth.service';
import { environment } from 'src/environments/environment';
import { MenuConfigService } from 'src/app/_metronic/jeework_old/core/_base/layout';
// import { AuthService } from './auth.service';

@Injectable({ providedIn: "root" })
export class AuthGuard implements CanActivate {
  sso_token: any;
  private subscriptions: Subscription[] = [];
  currentUserSubject: BehaviorSubject<any>;
  decoded: any;

  constructor(
    public authService: AuthenticationService,
    private httpUtils: HttpUtilsService,
    private TokenStorage: TokenStorage,
    private MenuConfigService: MenuConfigService,
    @Inject(DOCUMENT) private document: Document,
  ) {
    this.sso_token = this.getParamValueQueryString("sso_token");
  }

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Observable<boolean> | Promise<any> {
    setInterval(() => {
      this.refreshToken()
    }, 60000);
    return this.kiemTra()

  }
  async refreshToken() {
    this.authService.refreshToken(this.authService.ldp_loadDataUser, this.authService.ldp_refresh)
      .subscribe(
        (resData: any) => {
          if (resData && resData.access_token) {
            localStorage.setItem(this.authService.authLocalStorageToken, JSON.stringify(resData));
          } else {
            this.authService.logout();
          }
        }
      );
  }
  async kiemTra() {

    const currentUser = this.httpUtils.getAuthFromLocalStorage();
    if (currentUser && this.isTokenExpiredRefresh()) {
      if (!this.isTokenExpired()) {
        this.refreshToken()
        return new Promise(res => { { res(true); } })
      }
      return true;
    }
    else {
      if (!this.sso_token) {
        this.authService.logout();
        return false;
      } else {
        return new Promise(res => {
          this.authService.getDataUser_LandingPage(this.authService.ldp_loadDataUser, this.sso_token)
            .subscribe(
              (resData: any) => {
                if (resData && resData.access_token) {
                  localStorage.setItem(this.authService.authLocalStorageToken, JSON.stringify(resData));
                  if(resData.user.customData["jee-account"].userID){
                    this.TokenStorage.setIDUser(resData.user.customData["jee-account"].userID);
                  }
                  if(resData.user.customData.personalInfo){
                    var i4 = resData.user.customData.personalInfo;
                    var info = {
                      ChucVu: i4.Jobtitle,
                      HoTen: i4.Fullname,
                      Image: i4.Avatar,
                      Username: resData.user.username
                    }
                    this.TokenStorage.setUserData(info);
                  }
                  if(resData.user.username){
                    this.TokenStorage.setUserCustomer(resData.user.username);
                    this.MenuConfigService.GetRole_WeWork(resData.user.username);
                  }
                  res(true);
                } else {
                  this.authService.logout();
                  res(false);
                }
              }
            );
        })
      }
    }

  }
  isTokenExpired(token?: string): boolean {
    const auth = this.httpUtils.getAuthFromLocalStorage();
    if (auth === null) return false;

    if (!token) token = auth.access_token;
    if (!token) return false;

    const date = this.getTokenExpirationDate(token);
    if (date === undefined) return false;
    return date.valueOf() > new Date().valueOf();
  }
  isTokenExpiredRefresh(token?: string): boolean {
    const auth = this.httpUtils.getAuthFromLocalStorage();
    if (auth === null) return false;

    if (!token) token = auth.refresh_token;
    if (!token) return false;

    const date = this.getTokenExpirationDate(token);
    if (date === undefined) return false;
    return date.valueOf() > new Date().valueOf();
  }

  getTokenExpirationDate(token: string): Date {
    // token = atob(token);
    this.decoded = jwt_decode(token);
    if (this.decoded.exp === undefined) return null;
    const date = new Date(0);
    date.setUTCSeconds(this.decoded.exp);
    return date;
  }

  getParamValueQueryString(paramName) {
    const url = window.location.href;
    let paramValue;
    if (url.includes("?")) {
      const httpParams = new HttpParams({ fromString: url.split("?")[1] });
      paramValue = httpParams.get(paramName);
    }
    return paramValue;
  }
}


