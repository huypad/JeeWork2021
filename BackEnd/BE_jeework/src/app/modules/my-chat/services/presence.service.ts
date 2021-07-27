import { AuthService } from 'src/app/modules/auth';
import { EventEmitter, Injectable } from '@angular/core';
import { Router } from '@angular/router';
import * as signalR from '@microsoft/signalr';

import { HubConnection, HubConnectionBuilder } from '@microsoft/signalr';

import { BehaviorSubject, ReplaySubject } from 'rxjs';
import { take } from 'rxjs/operators';
import { environment } from 'src/environments/environment';
import { ConversationModel } from '../models/conversation';
// const connection = new signalR.HubConnectionBuilder()
//   .withUrl(environment.hubUrl+'presence', {
//     skipNegotiation: true,
//     transport: signalR.HttpTransportType.WebSockets
//   })
//       .build()

@Injectable({
  providedIn: 'root'
})
export class PresenceService {
  hubUrl = environment.HOST_JEECHAT_API+'/hubs';
   private hubConnection: HubConnection;
  private onlineUsersSource = new BehaviorSubject<any[]>([]);  
  onlineUsers$ = this.onlineUsersSource.asObservable();

  private NewGroupSource = new BehaviorSubject<any>(null);  
  NewGroupSource$ = this.NewGroupSource.asObservable();

  private OpenmessageUsernameSource = new ReplaySubject<any>(1);
  OpenmessageUsername$ = this.OpenmessageUsernameSource.asObservable();

  constructor( private router: Router,
    private auth:AuthService
    
    ) { 
     
    //   this.connectToken();  
    //   connection.onclose(()=>{
    //     setTimeout(r=>{
    //       this.reconnectToken();          
    //     },5000);
    //  }) 
    }
  

  connectToken(){
    this.hubConnection = new HubConnectionBuilder()
  .withUrl(this.hubUrl+'/presence', {
    skipNegotiation: true,
    transport: signalR.HttpTransportType.WebSockets
  }).withAutomaticReconnect()
      .build()
try
{


      this.hubConnection.start().then(()=>{
    
      const data=this.auth.getAuthFromLocalStorage();

         var _token =`Bearer ${data.access_token}`
       
         try
         {
           setTimeout(() => {
            this.hubConnection.invoke("onConnectedTokenAsync",_token);  
           }, 2000);
       
         }catch(err)
         {
          console.log(err)
         }
   
        
        this.hubConnection.on('UserIsOnline', (username: any) => {
          
      this.onlineUsers$.pipe(take(1)).subscribe(usernames => {
        this.onlineUsersSource.next([...usernames, username])
        // console.log('UserIsOnline',this.onlineUsers$)
     
      })
      // this.toastr.info(username.FullName+' has connect')
      // this.toastr.info(username.displayName+ ' has connect')
    })

    this.hubConnection.on('UserIsOffline', (User: any) => {
      this.onlineUsers$.pipe(take(1)).subscribe(usernames => {
        
        this.onlineUsersSource.next([...usernames.filter(x => x.Username !== User.Username),User])
        // this.onlineUsersSource.next([...usernames, User])
      
        // console.log('UserIsOffline',this.onlineUsers$)
      })
    })
    this.hubConnection.on('GetOnlineUsers', (usernames: any[]) => {
      this.onlineUsersSource.next(usernames);
    })

    this.hubConnection.on('NewGroupChatReceived', data => {
      // console.log('NewGroupChatReceived',data)
      this.NewGroupSource.next(data);
    })
    this.hubConnection.on('NewMessageReceived', (IdGroup: any) => {
      // console.log('NewMessageReceived',IdGroup)
      this.OpenmessageUsernameSource.next(IdGroup)
    })

    
      }).catch(err => {
         document.write(err);
        console.log("error",err);
      });
    }
    catch(err)
    {
      console.log(err)
    }


  
}

async NewGroup(token:string,item:ConversationModel,dl:any){   
  return  this.hubConnection .invoke('NewGroupChat',token,item,dl)
    .catch(error => console.log(error));
}


disconnectToken(){
  var _token = '';
  var _userID = -1;
  const data=this.auth.getAuthFromLocalStorage();
      console.log("data",data); 

         var _token =`Bearer ${data.access_token}`

         this.hubConnection.invoke("onDisconnectToken",_token);  
}


stopHubConnection() {
  this.hubConnection.stop().catch(error => console.log(error));
}


reconnectToken(): void {
  const data=this.auth.getAuthFromLocalStorage();
  console.log("data",data); 

     var _token =`Bearer ${data.access_token}`
     this.hubConnection.start().then((data: any) => {
      console.log('Connect with ID',data);
      this.hubConnection.invoke("ReconnectToken", _token).then(()=>{
      });
    }).catch((error: any) => {
     console.log('Could not ReconnectToken! ',error); 
    });       
 ///  console.log('Connect with ID',this.proxy.id);
  }
  // //endpoints.MapHub<PresenceHub>("hubs/presence") at startup file of backend
  // createHubConnection(user: User) {
  //   debugger
  //   // đây là nơi nó call đến BE
  //   this.hubConnection = new HubConnectionBuilder()
  //     .withUrl(this.hubUrl + 'presence', {
  //       accessTokenFactory: () => user.access_token
  //     })
  //     .withAutomaticReconnect()
  //     .build()

  //   this.hubConnection
  //     .start()
  //     .catch(error => console.log(error));

  //   this.hubConnection.on('UserIsOnline', (username: Member) => {
  //     this.onlineUsers$.pipe(take(1)).subscribe(usernames => {
  //       this.onlineUsersSource.next([...usernames, username])
  //     })
  //     this.toastr.info(' has connect')
  //     // this.toastr.info(username.displayName+ ' has connect')
  //   })

  //   this.hubConnection.on('UserIsOffline', (username: Member) => {
  //     this.onlineUsers$.pipe(take(1)).subscribe(usernames => {
  //       this.onlineUsersSource.next([...usernames.filter(x => x.userName !== username.userName)])
  //     })
  //     this.toastr.warning( ' disconnect')
  //   })

  //   this.hubConnection.on('GetOnlineUsers', (usernames: Member[]) => {
  //     this.onlineUsersSource.next(usernames);
  //   })

  //   // this.hubConnection.on('NewMessageReceived', ({username, diplayName}) => {
  //   //   this.toastr.info(diplayName + ' has sent you a new message!')
  //   // })

  //   this.hubConnection.on('NewMessageReceived', (username: Member) => {
  //     this.messageUsernameSource.next(username)
  //   })
  // }

 
}
