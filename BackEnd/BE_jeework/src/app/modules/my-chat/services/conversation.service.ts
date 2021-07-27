import { environment } from 'src/environments/environment';
import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';

@Injectable({
  providedIn: 'root'
})
export class ConversationService {
  baseUrl = environment.HOST_JEECHAT_API+'/api/conversation';
  public authLocalStorageToken = `${environment.appVersion}-${environment.USERDATA_KEY}`;
  constructor(private http: HttpClient) { }


  public getAuthFromLocalStorage(): any {
    try {
      const authData = JSON.parse(localStorage.getItem(this.authLocalStorageToken));
      return authData;
    } catch (error) {
      console.error(error);
      return undefined;
    }
  }
  getHttpHeaders() {
    
    const data = this.getAuthFromLocalStorage();
    
    // console.log('auth.token',auth.access_token)
    let result = new HttpHeaders({
      'Content-Type': 'application/json',
      'Authorization':'Bearer '+data.access_token,
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Headers': 'Content-Type'
    });
    return result;
  }
  GetDanhBaNotConversation()
  {
    const url =this.baseUrl+'/Get_DanhBa_NotConversation'
    const httpHeader = this.getHttpHeaders();
    return this.http.get<any>(url,{ headers: httpHeader});
  }
  CreateConversation(item:any)
  {
    const url =this.baseUrl+'/CreateConversation'
    const httpHeader = this.getHttpHeaders();
    return this.http.post<any>(url,item,{ headers: httpHeader});
  }
  getAllUsers():any {
    const url =this.baseUrl+'/GetAllUser'
    const httpHeaders = this.getHttpHeaders();
    return this.http.get<any>(url,{ headers: httpHeaders});
}
  

}
