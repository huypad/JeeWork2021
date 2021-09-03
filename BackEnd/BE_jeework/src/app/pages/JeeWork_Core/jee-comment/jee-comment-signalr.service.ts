import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';
import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable, BehaviorSubject, of } from 'rxjs';
import { finalize, share, tap } from 'rxjs/operators';
import { AuthService } from 'src/app/modules/auth';
import { ReturnFilterComment } from './jee-comment.model';
import * as signalR from '@microsoft/signalr';
import {environment} from '../../../../environments/environment';
import {HttpUtilsService} from '../../../_metronic/jeework_old/core/utils/http-utils.service';

const HUB_JEECOMMENT_URL = environment.HOST_JEECOMMENT_API + '/hub/comment';

@Injectable()
export class JeeCommentSignalrService {
  private hubConnection: HubConnection;
  public _showChange$: BehaviorSubject<any> = new BehaviorSubject<any>('');

  get showChange$() {
    return this._showChange$.asObservable();
  }
  constructor(private http: HttpClient, private httpUtils: HttpUtilsService, private _authService: AuthService) {}

  connectToken(topicObjectID: string) {
    this.hubConnection = new HubConnectionBuilder()
        .withUrl(HUB_JEECOMMENT_URL, {
          skipNegotiation: true,
          transport: signalR.HttpTransportType.WebSockets,
        })
        .withAutomaticReconnect()
        .build();

    this.hubConnection
        .start()
        .then(() => {
          const data = this._authService.getAuthFromLocalStorage();
          const token = `${data.access_token}`;
          this.hubConnection.invoke('JoinGroup', topicObjectID, token);
          this.hubConnection.on('changeComment', (data: any) => {
            const result = JSON.parse(data);
            this._showChange$.next(result);
          });
        })
        .catch((err) => {
          this.hubConnection.stop();
        });
  }

  disconnectToken(topicObjectID: string) {
    const data = this._authService.getAuthFromLocalStorage();
    const token = `${data.access_token}`;
    this.hubConnection.invoke('LeaveGroup', topicObjectID, token);
  }
}
