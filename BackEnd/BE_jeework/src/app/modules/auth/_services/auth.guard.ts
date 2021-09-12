import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { environment } from 'src/environments/environment';
import { AuthService } from './auth.service';
import { TokenStorage } from 'src/app/_metronic/jeework_old/core/auth/_services';
import { MenuConfigService } from 'src/app/_metronic/jeework_old/core/_base/layout';
import {LayoutUtilsService} from '../../../_metronic/jeework_old/core/utils/layout-utils.service';
@Injectable({ providedIn: 'root' })
export class AuthGuard implements CanActivate {
  constructor(
      private authService: AuthService,
      private router: Router,
      private TokenStorage: TokenStorage,
      private layoutUtilsService: LayoutUtilsService,
      private MenuConfigService: MenuConfigService
  ) {
  }
  appCode = environment.APPCODE;
  HOST_JEELANDINGPAGE = environment.HOST_JEELANDINGPAGE;

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean> {
    return new Promise<boolean>((resolve, reject) => {
      if (!this.authService.isAuthenticated()) {
        if (this.authService.getParamsSSO()) {
          this.authService.saveToken_cookie(this.authService.getParamsSSO());
        }
        resolve(this.canPassGuard());
      } else {
        resolve(this.canPassGuard());
      }
    });
  }

  canPassGuard(): Promise<boolean> {
    return new Promise<boolean>((resolve, reject) => {
      this.authService.getUserMeFromSSO().subscribe(
          (data) => {
            resolve(this.canPassGuardAccessToken(data));
          },
          (error) => {
            this.authService.refreshToken().subscribe(
                (data) => {
                  resolve(this.canPassGuardAccessToken(data));
                },
                (error) => {
                  resolve(this.unauthorizedGuard());
                }
            );
          }
      );
    });
  }

  canPassGuardAccessToken(data) {
    return new Promise<boolean>((resolve, reject) => {
      if (data && data.access_token) {
        this.authService.saveNewUserMe(data);
        const lstAppCode: string[] = data['user']['customData']['jee-account']['appCode'];
        if (lstAppCode) {
          if (lstAppCode.indexOf(this.appCode) === -1) {
            return resolve(this.unAppCodeAuthorizedGuard());
          } else {
            this.LoadStorage(data);
            return resolve(true);
          }
        } else {
          return resolve(this.unAppCodeAuthorizedGuard());
        }
      }
    });
  }

  unauthorizedGuard() {
    return new Promise<boolean>((resolve, reject) => {
      this.authService.logout();
      return resolve(false);
    });
  }

  unAppCodeAuthorizedGuard() {
    return new Promise<boolean>((resolve, reject) => {
      const popup = this.layoutUtilsService.showError(
        'Bạn không có quyền truy cập trang này'
      );
      popup.afterDismissed().subscribe((res) => {
        window.open(this.HOST_JEELANDINGPAGE);
        return resolve(false);
      });
      return resolve(false);
    });
  }

  LoadStorage(data) {
    if (data.user.customData['jee-account'].userID) {
      this.TokenStorage.setIDUser(data.user.customData['jee-account'].userID);
    }
    if (data.user.customData.personalInfo) {
      var i4 = data.user.customData.personalInfo;
      var info = {
        ChucVu: i4.Jobtitle,
        HoTen: i4.Fullname,
        Image: i4.Avatar,
        Username: data.user.username,
      };
      this.TokenStorage.setUserData(info);
    }
    if (data.user.username) {
      this.TokenStorage.setUserCustomer(data.user.username);
      this.MenuConfigService.GetRole_WeWork(data.user.username);
    }
  }
}
