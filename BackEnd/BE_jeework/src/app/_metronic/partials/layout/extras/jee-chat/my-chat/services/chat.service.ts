import {UserChatBox} from './../models/user-chatbox';
import {AuthService} from 'src/app/modules/auth';
import {environment} from 'src/environments/environment';
import {Injectable} from '@angular/core';
import {BehaviorSubject, Observable} from 'rxjs';
import {ReplaySubject} from 'rxjs';
import {HttpClient, HttpHeaders, HttpParams} from '@angular/common/http';
import {PresenceService} from './presence.service';
import {QueryParamsModelNewLazy, QueryResultsModel} from '../models/pagram';
import { NotifyMessage } from '../models/NotifyMess';

@Injectable({
    providedIn: 'root'
})
export class ChatService {
    public search$ = new BehaviorSubject<string>('');
    public reload$ = new BehaviorSubject<boolean>(false);
    public InforUserChatWith$ = new BehaviorSubject<any>([]);

    public UnreadMess$ = new BehaviorSubject<number>(0);
    public OpenMiniChat$ = new BehaviorSubject<any>(null);
    public OneMessage$ = new BehaviorSubject<number>(0);
    public CloseMiniChat$ = new BehaviorSubject<any>(null);

    ChangeDatachat(data) {
        console.log('data service:', data);
        this.OpenMiniChat$.next(data);
    }


    private unreadmessageSource = new ReplaySubject<number>(1);
    countUnreadmessage$ = this.unreadmessageSource.asObservable();

    baseUrl = environment.HOST_JEECHAT_API + '/api';
    private currentUserSource = new ReplaySubject<any>(1);
    currentUser$ = this.currentUserSource.asObservable();
    public authLocalStorageToken = `${environment.appVersion}-${environment.USERDATA_KEY}`;

    constructor(private http: HttpClient, private presence: PresenceService,private auth: AuthService ) {
    }

    UpdateUnRead(IdGroup: number, UserId: number, key: string) {
        const url = this.baseUrl + `/chat/UpdateDataUnread?IdGroup=${IdGroup}&UserID=${UserId}&key=${key}`;
        const httpHeader = this.getHttpHeaders();
        return this.http.post<any>(url, null, {headers: httpHeader});
    }
    publishMessNotifi(token:string,IdGroup:number,mesage:string,fullname:string,avatar:string)
    {
      const url =this.baseUrl+`/notifi/publishMessNotifiTwoUser?token=${token}&IdGroup=${IdGroup}
      &mesage=${mesage}&fullname=${fullname}&avatar=${avatar}`;
      const httpHeader = this.getHttpHeaders();
      return this.http.get<any>(url,{ headers: httpHeader});
    }
    publishMessNotifiGroup(token:string,IdGroup:number,mesage:string,fullname:string)
    {
      const url =this.baseUrl+`/notifi/publishMessNotifiGroup?token=${token}&IdGroup=${IdGroup}
      &mesage=${mesage}&fullname=${fullname}`;
      const httpHeader = this.getHttpHeaders();
      return this.http.get<any>(url,{ headers: httpHeader});
    }
    GetTagNameGroup(IdGroup:number)
    {
      const url =this.baseUrl+`/chat/GetTagNameisGroup?IdGroup=${IdGroup}`
      const httpHeader = this.getHttpHeaders();
      return this.http.get<any>(url,{ headers: httpHeader});
    }
  GetUserReaction(idchat:number,type:number)
  {
    const url =this.baseUrl+`/chat/GetUserReaction?idchat=${idchat}&type=${type}`
    const httpHeader = this.getHttpHeaders();
    return this.http.get<any>(url,{ headers: httpHeader});
  }
  UpdateUnReadGroup(IdGroup:number,userUpdateRead:any,key:string)
  {
    const url =this.baseUrl+`/chat/UpdateDataUnreadInGroup?IdGroup=${IdGroup}&userUpdateRead=${userUpdateRead}&key=${key}`
    const httpHeader = this.getHttpHeaders();
    return this.http.post<any>(url,null,{ headers: httpHeader});
  }
    GetUserById(IdUser: number) {
        const url = this.baseUrl + `/chat/GetnforUserById?IdUser=${IdUser}`;
        const httpHeader = this.getHttpHeaders();
        return this.http.get<any>(url, {headers: httpHeader});
    }

    setCurrentUser(user: any) {
        if (user) {

            // const roles = this.getDecodedToken(user.token).role;//copy token to jwt.io see .role
            // Array.isArray(roles) ? user.roles = roles : user.roles.push(roles);
            localStorage.setItem(this.authLocalStorageToken, JSON.stringify(user));
            this.currentUserSource.next(user.user.username);
        }
    }

    GetChatWithFriend(username){
        const url = this.baseUrl + `/chat/GetChatWithFriend?usernamefriend=${username}`;
        const httpHeader = this.getHttpHeaders();
        return this.http.get<any>(url, {headers: httpHeader});
    }

    public getAuthFromLocalStorage(): any {
        return this.auth.getAuthFromLocalStorage();
    }

    publishNotifi(item:NotifyMessage): Observable<any> 
    {
      const url =this.baseUrl+`/notifi/PushNotifiTagName`;
      const httpHeader = this.getHttpHeaders();
      return this.http.post<any>(url,item,{ headers: httpHeader});
    }
    getHttpHeaders() {

        const data = this.getAuthFromLocalStorage();

        // console.log('auth.token',auth.access_token)
        let result = new HttpHeaders({
            'Content-Type': 'application/json',
            'Authorization': 'Bearer ' + data.access_token,
            'Access-Control-Allow-Origin': '*',
            'Access-Control-Allow-Headers': 'Content-Type'
        });
        return result;
    }
    getlist_Reaction()
    {
      const url =this.baseUrl+'/chat/GetDSReaction'
      const httpHeader = this.getHttpHeaders();
      return this.http.get<any>(url,{ headers: httpHeader});
    }
    GetContactChatUser() {

        const url = this.baseUrl + '/chat/Get_Contact_Chat';
        const httpHeader = this.getHttpHeaders();
        return this.http.get<any>(url, {headers: httpHeader});

    }

    GetInforUserChatWith(IdGroup: number) {
        const url = this.baseUrl + `/chat/GetInforUserChatWith?IdGroup=${IdGroup}`;
        const httpHeader = this.getHttpHeaders();
        return this.http.get<any>(url, {headers: httpHeader});
    }


    getFindHTTPParams(queryParams): HttpParams {
        let params = new HttpParams()
            //.set('filter',  queryParams.filter )
            .set('sortOrder', queryParams.sortOrder)
            .set('sortField', queryParams.sortField)
            .set('page', (queryParams.pageNumber + 1).toString())
            .set('record', queryParams.pageSize.toString());
        let keys = [], values = [];
        if (queryParams.more) {
            params = params.append('more', 'true');
        }
        Object.keys(queryParams.filter).forEach(function(key) {
            if (typeof queryParams.filter[key] !== 'string' || queryParams.filter[key] !== '') {
                keys.push(key);
                values.push(queryParams.filter[key]);
            }
        });
        if (keys.length > 0) {
            params = params.append('filter.keys', keys.join('|'))
                .append('filter.vals', values.join('|'));
        }
        return params;
    }

    //begin load page-home
    GetListMess(queryParams: QueryParamsModelNewLazy, routeFind: string = ''): Observable<QueryResultsModel> {
        const url = this.baseUrl + routeFind;
        const httpHeader = this.getHttpHeaders();
        const httpParams = this.getFindHTTPParams(queryParams);
        return this.http.get<any>(url, {headers: httpHeader, params: httpParams});

    }

    set countUnreadMessage(value: number) {
        this.unreadmessageSource.next(value);
    }
}
