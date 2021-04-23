import { environment } from "src/environments/environment";
import { Injectable } from "@angular/core";
import { Observable, of } from "rxjs";
const LAYOUT_CONFIG_LOCAL_STORAGE_KEY = `${environment.appVersion}-layoutConfig`;

@Injectable()
export class TokenStorage {
  /**
   * Get access token
   * @returns {Observable<string>}
   */
  public getAccessToken(): Observable<string> {
    const token: string = <string>localStorage.getItem("accessToken");
    return of(token);
  }

  /**
   * Set access token
   * @returns {TokenStorage}
   */
  public setAccessToken(token: string): TokenStorage {
    localStorage.setItem("accessToken", token);
    return this;
  }
  public getAccessTokenNew(): Observable<string> {
    const token: string = <string>(
      localStorage.getItem(environment.authTokenKey)
    );
    return of(token);
  }
  /**
   * Set refresh token
   * @returns {TokenStorage}
   */
  public setRefreshToken(token: string): TokenStorage {
    localStorage.setItem("refreshToken", token);
    return this;
  }

  /**
   * Get refresh token
   * @returns {Observable<string>}
   */
  public getRefreshToken(): Observable<string> {
    const token: string = <string>localStorage.getItem("refreshToken");
    return of(token);
  }

  /**
   * Get pageSize
   * @returns {Observable<string>}
   */
  public getPageSize(): Observable<string> {
    const size: string = "50";
    return of(size);
  }

  public setIDUser(iduser: string): TokenStorage {
    localStorage.setItem("idUser", iduser);
    return this;
  }

  public getIDUser(): Observable<string> {
    const Id: string = <string>localStorage.getItem("idUser");
    return of(Id);
  }
  /**
   * Get user roles in JSON string
   * @returns {Observable<any>}
   */
  public getUserRoles(): Observable<any> {
    const roles: any = localStorage.getItem("WeWorkRoles");
    try {
      return of(JSON.parse(roles));
    } catch (e) {}
  }

  /**
   * Set user roles
   * @param roles
   * @returns {TokenStorage}
   */
  public setUserRoles(roles: any): any {
    if (roles != null) {
      localStorage.setItem("userRoles", JSON.stringify(roles));
    }
    return this;
  }

  /**
   * Remove tokens
   */
  public clear() {
    localStorage.removeItem("accessToken");
    localStorage.removeItem("refreshToken");
    localStorage.removeItem("userRoles");
    //localStorage.removeItem('idUser');
  }

  public setLogoCus(iduser: string): TokenStorage {
    localStorage.setItem("LogoCus", iduser);
    return this;
  }

  public getLogoCus(): Observable<string> {
    const Id: string = <string>localStorage.getItem("LogoCus");
    return of(Id);
  }

  /**
   * Get user roles in JSON string
   * @returns {Observable<any>}
   */
  public getMenuRoles(): Observable<any> {
    const roles: any = localStorage.getItem("menuRoles");
    try {
      return of(JSON.parse(roles));
    } catch (e) {}
  }

  /**
   * Set user roles
   * @param roles
   * @returns {TokenStorage}
   */
  public setMenuRoles(roles: any): any {
    if (roles != null) {
      localStorage.setItem("menuRoles", JSON.stringify(roles));
    }
    return this;
  }

  /**
   * Set Url of cookie
   * @param roles
   * @returns {TokenStorage}
   */
  public setUrl(roles: any): any {
    if (roles != null) {
      localStorage.setItem("url", JSON.stringify(roles));
    }
    return this;
  }

  /**
   * Get Url of cookie
   * @returns {Observable<any>}
   */
  public getUrl(): Observable<any> {
    const roles: any = localStorage.getItem("url");
    try {
      return of(JSON.parse(roles));
    } catch (e) {}
  }

  public setUserCustomer(iduser: string): TokenStorage {
    localStorage.setItem("Username", iduser);
    return this;
  }

  public getUserCustomer(): Observable<string> {
    const Id: string = <string>localStorage.getItem("Username");
    return of(Id);
  }

  public setUserData(data: any): TokenStorage {
    localStorage.setItem("User", JSON.stringify(data));
    return this;
  }

  public getUserData(): Observable<any> {
    const user: string = <string>localStorage.getItem("User");
    return of(JSON.parse(user));
  }

  public getHeightHeader() {
    //header 65px
    const configFromLocalStorage = localStorage.getItem(
      LAYOUT_CONFIG_LOCAL_STORAGE_KEY
    );
    if (configFromLocalStorage) {
      try {
		var height = 0;
        var config = JSON.parse(configFromLocalStorage);
        return (config.header.self.display ? 65 : 0) + (config.subheader.display ? 104 : 0);
      } catch (error) {
        console.error("config parse from local storage", error);
        return 0;
      }
    }
    return 0;
  }
}
