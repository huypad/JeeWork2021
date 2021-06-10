import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { AuthService } from 'src/app/modules/auth';
import { environment } from '../../../../environments/environment';
import * as io from 'socket.io-client';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';

@Injectable()
export class SocketioService {
  socket: any
  constructor(private auth:AuthService, private http: HttpClient) { }

  connect(){
    const auth = this.auth.getAuthFromLocalStorage();
    this.socket = io(environment.WEBSOCKET + '/notification',{
      transportOptions: {
        polling: {
          extraHeaders: {
            "x-auth-token": `${auth!=null ? auth.access_token : ''}`
          }
        }
      }
    });
  }
  
  listen(){
    return new Observable((subscriber) => { 
      this.socket.on('notification', (data) => {
        subscriber.next(data)
      })
    })
  }
  
  getNotificationList(isRead: any): Observable<any> {
    const auth = this.auth.getAuthFromLocalStorage();
    const httpHeader = new HttpHeaders({
      Authorization: `${auth!=null ? auth.access_token : ''}`,
    });
    const httpParam = new HttpParams().set('status', isRead)
    return this.http.get<any>(environment.APINOTIFICATION+'/notification/pull', {
      headers: httpHeader,
      params: httpParam
    });
  }

  readNotification(id: string): Observable<any> {
    const auth = this.auth.getAuthFromLocalStorage();
    const httpHeader = new HttpHeaders({
      Authorization: `${auth!=null ? auth.access_token : ''}`,
    });
    let item = {"id":  id}
    return this.http.post<any>(environment.APINOTIFICATION+'/notification/read', item, { headers: httpHeader });
  }

  getListApp(): Observable<any> {
    const auth = this.auth.getAuthFromLocalStorage();
    const httpHeader = new HttpHeaders({
      Authorization: `Bearer ${auth!=null ? auth.access_token : ''}`,
    });
    var UserID = localStorage.getItem("idUser");
    const httpParam = new HttpParams().set('userID', UserID)
    return this.http.get<any>(environment.JEEACCOUNTAPI+'/api/accountmanagement/GetListAppByUserID', {
        headers: httpHeader,
        params: httpParam
      });
  }
}