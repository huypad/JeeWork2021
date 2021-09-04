import { QueryParamsModelNewLazy } from './../models/pagram';
import { QueryResultsModel } from './../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { AuthService } from 'src/app/modules/auth';
import { environment } from 'src/environments/environment';
import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';
import { ReplaySubject } from 'rxjs';
import { HttpClient, HttpHeaders, HttpParams } from '@angular/common/http';
import { PresenceService } from './presence.service';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  public search$ = new BehaviorSubject<string>("");
  public reload$ = new BehaviorSubject<boolean>(false);
  public InforUserChatWith$ = new BehaviorSubject<any>([]);

  public OpenMiniChat$ = new BehaviorSubject<any>(null);


  private unreadmessageSource = new ReplaySubject<number>(1);
  countUnreadmessage$ = this.unreadmessageSource.asObservable();

  baseUrl = environment.HOST_JEECHAT_API+'/api';
  private currentUserSource = new ReplaySubject<any>(1);
  currentUser$ = this.currentUserSource.asObservable();
  public authLocalStorageToken = `${environment.appVersion}-${environment.USERDATA_KEY}`;
  constructor(private http: HttpClient, private presence: PresenceService) { }
private auth:AuthService
  // login(model: any){
  //   return this.http.post(this.baseUrl+'Account/login', model).pipe(
  //     map((res:User)=>{
  //       const user = res;
  //       if(user){
  //         // this.setCurrentUser(user);
  //         this.presence.createHubConnection(user);
  //       }
  //     })
  //   )
  // }

  setCurrentUser(user: any){
    if(user){
     
      // const roles = this.getDecodedToken(user.token).role;//copy token to jwt.io see .role   
      // Array.isArray(roles) ? user.roles = roles : user.roles.push(roles);
      localStorage.setItem(this.authLocalStorageToken, JSON.stringify(user));
      this.currentUserSource.next(user.user.username); 
    }      
  }


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
  GetContactChatUser()
  {
    const url =this.baseUrl+'/chat/Get_Contact_Chat'
    const httpHeader = this.getHttpHeaders();
    return this.http.get<any>(url,{ headers: httpHeader});
  }

  GetInforUserChatWith(IdGroup:number)
  {
    const url =this.baseUrl+`/chat/GetInforUserChatWith?IdGroup=${IdGroup}`;
    const httpHeader = this.getHttpHeaders();
    return this.http.get<any>(url,{ headers: httpHeader});
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
		Object.keys(queryParams.filter).forEach(function (key) {
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
   GetListMess(queryParams:QueryParamsModelNewLazy , routeFind: string = ''): Observable<QueryResultsModel> {
    const url = this.baseUrl+routeFind;
    const httpHeader = this.getHttpHeaders();
    const httpParams = this.getFindHTTPParams(queryParams);
		return this.http.get<any>(url,{ headers: httpHeader,params:  httpParams });
		
	}


  UpdateUnRead(IdGroup:number,key:string)
  {
    const url =this.baseUrl+`/chat/UpdateDataUnread?IdGroup=${IdGroup}&key=${key}`
    const httpHeader = this.getHttpHeaders();
    return this.http.post<any>(url,null,{ headers: httpHeader});
  }

  set countUnreadMessage(value: number) {
    this.unreadmessageSource.next(value);
  }
}
