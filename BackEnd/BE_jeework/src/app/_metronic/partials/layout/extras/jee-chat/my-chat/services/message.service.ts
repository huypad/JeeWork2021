import { ChatService } from './chat.service';
import { AuthService } from 'src/app/modules/auth';
import { HttpClient } from '@angular/common/http';
import { EventEmitter, Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

import { BehaviorSubject, Observable, ReplaySubject } from 'rxjs';
import { take } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { Message } from '../models/message';
import { Member } from '../models/member';

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  baseUrl = environment.HOST_JEECHAT_API + '/api';
  hubUrl = environment.HOST_JEECHAT_API + '/hubs';
  private hubConnection: HubConnection;
  private messageThreadSource = new BehaviorSubject<any[]>([]);
  messageThread$ = this.messageThreadSource.asObservable();
  public reaction = new BehaviorSubject<any>(undefined);
  reaction$ = this.reaction.asObservable();

  private seenMessageSource = new ReplaySubject<string>(1);
  seenMessage$ = this.seenMessageSource.asObservable();
  messageReceived: EventEmitter<Message[]> = new EventEmitter<Message[]>();
  // public messageReceived: EventEmitter<any>;///tin nhan ca nhan

  public Newmessage = new BehaviorSubject<Message[]>([]);
  Newmessage$ = this.Newmessage.asObservable();
  // constructor(private http: HttpClient) { }



  // private hubConnection: HubConnection;
  private onlineUsersSource = new BehaviorSubject<Member[]>([]);
  onlineUsers$ = this.onlineUsersSource.asObservable();

  private messageUsernameSource = new ReplaySubject<Member>(1);
  messageUsername$ = this.messageUsernameSource.asObservable();

  public hidenmess = new BehaviorSubject<any>(undefined);
  hidenmess$ = this.hidenmess.asObservable();

  public ComposingMess = new BehaviorSubject<any>(undefined);
  ComposingMess$ = this.ComposingMess.asObservable();

  constructor(
    private auth: AuthService,
    private chatservices: ChatService
  ) {

  }


  connectToken(idGroup: number) {
    this.hubConnection = new HubConnectionBuilder()
      .withUrl(this.hubUrl + "/message", {
        skipNegotiation: true,
        transport: signalR.HttpTransportType.WebSockets,
      })
      .withAutomaticReconnect()
      .build();
    try {
      this.hubConnection
        .start()
        .then(() => {
          const data = this.auth.getAuthFromLocalStorage();

          var _token = `Bearer ${data.access_token}`;

          try {
            this.hubConnection.invoke("onConnectedTokenAsync", _token, idGroup);
          } catch (err) {
            console.log(err);
          }

          // load mess khi
          this.hubConnection.on("ReceiveMessageThread", (messages) => {
            console.log("ReceiveMessageThread", messages);
            const reversed = messages.reverse();
            this.messageThreadSource.next(reversed);
          });

          this.hubConnection.on("SeenMessageReceived", (username) => {
            this.seenMessageSource.next(username);
          });
          this.hubConnection.on("ReactionMessage", (data) => {
            this.reaction.next(data);
          });
          this.hubConnection.on("HidenMessage", (data) => {
            this.hidenmess.next(data);
          });
          this.hubConnection.on("CloseMessage", (data) => {
            this.chatservices.CloseMiniChat$.next(data);
          });
          this.hubConnection.on("Composing", (data) => {
            this.ComposingMess.next(data);
          });
          this.hubConnection.on("NewMessage", (message) => {
            // this.messageReceived.emit(message)
            this.messageThread$.pipe(take(1)).subscribe((messages) => {
              this.messageThreadSource.next([...messages, message[0]]);
              this.Newmessage.next(message);
            });
          });
        })
        .catch((err) => {
          console.log("error", err);
        });
    } catch (err) {
      console.log(err);
    }
  }

  async HidenMessage(token: string, IdChat: number, IdGroup: number) {
    return this.hubConnection.invoke('DeleteMessage', token, IdChat, IdGroup)
      .catch(error => console.log(error));
  }
  async Composing(token: string, IdGroup: number) {
    return this.hubConnection.invoke('ComposingMessage', token, IdGroup)
      .catch(error => console.log(error));
  } 
  async  ReactionMessage(token:string,IdGroup:number,idchat:number,type){    
    return  this.hubConnection.invoke('ReactionMessage',token,IdGroup,idchat,type)
      .catch(error => console.log(error));
  }
  // NhanMess()
  // {
  //   this.hubConnection.on('NewMessage', message => {
  //     console.log('mesengeraaa',message)
  //     this.Newmessage$.next(message);    
  //     // this.messageReceived.emit(message)

  //   })
  // }


  stopHubConnection() {
    this.hubConnection.stop().catch(error => console.log(error));
  }


  reconnectToken(): void {
    var _token = '', _idUser = "0";
    const data = this.auth.getAuthFromLocalStorage();
    let infoTokenCon = { "Token": _token, "UserID": _idUser };
    this.hubConnection.start().then((data: any) => {
      console.log('Connect with ID', data);
      this.hubConnection.invoke("ReconnectToken", JSON.stringify(infoTokenCon)).then(() => {
      });
    }).catch((error: any) => {
      console.log('Could not ReconnectToken! ', error);
    });
    ///  console.log('Connect with ID',this.proxy.id);
  }

  async sendMessage(token: string, item: Message, IdGroup: number) {
    return this.hubConnection.invoke('SendMessage', token, item, IdGroup)
      .catch(error => console.log(error));
  }

  async CloseMessage(IdGroup:number,UserCurrent:string){    
    return  this.hubConnection .invoke('CloseMessage',IdGroup,UserCurrent)
      .catch(error => console.log(error));
  }
}
