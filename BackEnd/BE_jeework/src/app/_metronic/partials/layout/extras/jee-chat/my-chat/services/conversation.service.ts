import { environment } from 'src/environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { BehaviorSubject } from 'rxjs';
import {AuthService} from '../../../../../../../modules/auth';

@Injectable({
  providedIn: 'root'
})
export class ConversationService {
  baseUrl = environment.HOST_JEECHAT_API + '/api/conversation';
  public authLocalStorageToken = `${environment.appVersion}-${environment.USERDATA_KEY}`;

  public refreshConversation = new BehaviorSubject<any>(null);
  RefreshConversation$ = this.refreshConversation.asObservable();
  constructor(private http: HttpClient, private auth: AuthService) { }
  getDSThanhVienNotInGroup(IdGroup: number): any {
    const url = this.baseUrl + `/GetDSThanhVienNotInGroup?IdGroup=${IdGroup}`
    const httpHeaders = this.getHttpHeaders();
    return this.http.get<any>(url, { headers: httpHeaders });
  }
  EditNameGroup(IdGroup: number, grname: string) {
    const url = this.baseUrl + `/UpdateGroupName?IdGroup=${IdGroup}&nameGroup=${grname}`
    const httpHeader = this.getHttpHeaders();
    return this.http.post<any>(url, null, { headers: httpHeader });
  }
  GetThanhVienGroup(IdGroup: number) {
    const url = this.baseUrl + `/GetThanhVienGroup?IdGroup=${IdGroup}`
    const httpHeader = this.getHttpHeaders();
    return this.http.get<any>(url, { headers: httpHeader });
  }
  InsertThanhVien(IdGroup: number, item: any) {
    const url = this.baseUrl + `/InsertThanhVienInGroup?IdGroup=${IdGroup}`
    const httpHeader = this.getHttpHeaders();
    return this.http.post<any>(url, item, { headers: httpHeader });
  }

  DeleteConversation(item: any) {
    const url = this.baseUrl + `/DeleteConverSation?IdGroup=${item}`
    const httpHeader = this.getHttpHeaders();
    return this.http.post<any>(url, item, { headers: httpHeader });
  }
  getHttpHeaders() {

    const auth = this.auth.getAuthFromLocalStorage();

    // console.log('auth.token',auth.access_token)
    let result = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${auth != null ? auth.access_token : ''}`,
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Headers': 'Content-Type'
    });
    return result;
  }

  DeleteThanhVienInGroup(IdGroup: number, IdUser: number) {
    const url = this.baseUrl + `/DeleteThanhVienInGroup?IdGroup=${IdGroup}&UserId=${IdUser}`
    const httpHeader = this.getHttpHeaders();
    return this.http.post<any>(url, null, { headers: httpHeader });
  }

  UpdateAdmin(IdGroup: number, IdUser: number, key: number) {
    const url = this.baseUrl + `/UpdateAdminGroup?IdGroup=${IdGroup}&UserId=${IdUser}&key=${key}`
    const httpHeader = this.getHttpHeaders();
    return this.http.post<any>(url, null, { headers: httpHeader });
  }
  GetDanhBaNotConversation() {
    const url = this.baseUrl + '/Get_DanhBa_NotConversation'
    const httpHeader = this.getHttpHeaders();
    return this.http.get<any>(url, { headers: httpHeader });
  }
  CreateConversation(item: any) {
    const url = this.baseUrl + '/CreateConversation'
    const httpHeader = this.getHttpHeaders();
    return this.http.post<any>(url, item, { headers: httpHeader });
  }
  getAllUsers(): any {
    const url = this.baseUrl + '/GetAllUser'
    const httpHeaders = this.getHttpHeaders();
    return this.http.get<any>(url, { headers: httpHeaders });
  }
}
