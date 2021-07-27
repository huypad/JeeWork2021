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
// const connection = new signalR.HubConnectionBuilder()
//   .withUrl(environment.hubUrl+'message', {
//     skipNegotiation: true,
//     transport: signalR.HttpTransportType.WebSockets
//   })
//   .build();

@Injectable({
  providedIn: 'root'
})
export class MessageService {
  baseUrl = environment.HOST_JEECHAT_API+'/api';
  hubUrl = environment.HOST_JEECHAT_API+'/hubs';
  private hubConnection: HubConnection;
  private messageThreadSource = new BehaviorSubject<Message[]>([]);
  messageThread$ = this.messageThreadSource.asObservable();

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

  constructor(
    private auth:AuthService
    
    ) { 
     
    //   connection.onclose(()=>{
    //     setTimeout(r=>{
    //       this.reconnectToken();          
    //     },5000);
    //  }) 
    }
  

  connectToken(idGroup:number){
    this.hubConnection = new HubConnectionBuilder()
    .withUrl(this.hubUrl+'/message', {
      skipNegotiation: true,
      transport: signalR.HttpTransportType.WebSockets
    }).withAutomaticReconnect()
        .build()
        try
        {
        
        this.hubConnection .start().then(()=>{
        
      const data=this.auth.getAuthFromLocalStorage();

         var _token =`Bearer ${data.access_token}`
     
         try
         {
          this.hubConnection .invoke("onConnectedTokenAsync", _token,idGroup);  
                     
         }catch(err)
         {
          console.log(err)
         }
       
        

          // load mess khi
         this.hubConnection.on('ReceiveMessageThread', messages => {
              console.log('ReceiveMessageThread',messages)
              const reversed = messages.reverse();
              this.messageThreadSource.next(reversed);
            })
        
            this.hubConnection.on('SeenMessageReceived', username => {
              this.seenMessageSource.next(username);
            })
        
            this.hubConnection.on('NewMessage', message => {
              // console.log('mesenger',message)
              // this.messageReceived.emit(message)
              this.messageThread$.pipe(take(1)).subscribe(messages => {
                this.messageThreadSource.next([...messages, message[0]])   
                 this.Newmessage.next(message);     
              })
            })
      }).catch(err => {
        // document.write(err);
        console.log("error",err);
      });

    }
    catch(err)
    {
      console.log(err)
    }

  
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
  this.hubConnection .stop().catch(error => console.log(error));
}


reconnectToken(): void {
  var _token = '',_idUser="0";
  const data=this.auth.getAuthFromLocalStorage();
  let infoTokenCon = { "Token": _token,"UserID":_idUser};
  this.hubConnection .start().then((data: any) => {
      console.log('Connect with ID',data);
      this.hubConnection .invoke("ReconnectToken", JSON.stringify(infoTokenCon)).then(()=>{
      });
    }).catch((error: any) => {
     console.log('Could not ReconnectToken! ',error); 
    });       
 ///  console.log('Connect with ID',this.proxy.id);
  }
  
  async sendMessage(token:string,item:Message,IdGroup:number){    
    return  this.hubConnection .invoke('SendMessage',token,item,IdGroup)
      .catch(error => console.log(error));
  }

  

  // async seenMessage(recipientUsername: string){    
  //   return this.hubConnection.invoke('SeenMessage', recipientUsername)
  //     .catch(error => console.log(error));
  // }
}
