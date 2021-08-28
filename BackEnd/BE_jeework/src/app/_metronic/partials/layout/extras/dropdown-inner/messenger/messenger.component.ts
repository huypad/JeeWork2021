import { PresenceService } from "./../../../../../../modules/my-chat/services/presence.service";
import { SoundService } from "./../../../../../../modules/my-chat/services/sound.service";
import { MessageService } from "./../../../../../../modules/my-chat/services/message.service";
import { ChatService } from "src/app/modules/my-chat/services/chat.service";
import { UserChatBox } from "./../../../../../../modules/my-chat/models/user-chatbox";
import { ConversationService } from "./../../../../../../modules/my-chat/services/conversation.service";
import { ConversationModel } from "./../../../../../../modules/my-chat/models/conversation";
import { CreateConvesationGroupComponent } from "./../../create-convesation-group/create-convesation-group.component";
import { AuthService } from "src/app/modules/auth";

import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  EventEmitter,
  Input,
  NgZone,
  OnChanges,
  OnDestroy,
  OnInit,
  Output,
  SimpleChanges,
} from "@angular/core";
import { ReplaySubject, Subscription } from "rxjs";
import { FormControl } from "@angular/forms";
import { MatDialog } from "@angular/material/dialog";
import { CreateConversationUserComponent } from "../../create-conversation-user/create-conversation-user.component";
// import { CreateConversationUserComponent } from '../../create-conversation-user/create-conversation-user.component';

@Component({
  selector: 'kt-messenger',
  templateUrl: './messenger.component.html',
  styleUrls: ['./messenger.component.scss'],
})
export class MessengerComponent implements OnInit,OnDestroy,OnChanges {
	private _subscriptions: Subscription[] = [];
	chatBoxUsers: UserChatBox[] = [];
	ListBBChat:any[]=[];
	usernameActived: number;
	userCurrent:string;
	searchText: string;
	lstContact:any[]=[];
	listDanhBa:any[]=[];
	listmember:any[]=[]
	list_userchat:any[]=[];
	@Input() PData: number;
	propChanges: any;
	public searchControl: FormControl = new FormControl();
	public filteredGroups: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	constructor(
		private changeDetectorRefs: ChangeDetectorRef,
		public presence: PresenceService, 
		public messageService: MessageService, 
		private soundService: SoundService,
		private chatService:ChatService,
		private _ngZone: NgZone,
		public dialog: MatDialog,
		private auth:AuthService,
		private chatservice:ChatService,
		private conversation_sevices:ConversationService,

	) {
		
		const sb= this.presence.OpenmessageUsername$.subscribe(IdGroup => {
			this.unReadMessageFromSenderUsername(IdGroup);
		  })
		  this._subscriptions.push(sb);
	  
		  const dt=this.auth.getAuthFromLocalStorage();
	  this.userCurrent=dt.user.username
	}
	ngOnChanges(changes: SimpleChanges) {
		this.propChanges = changes;
		this.searchText=this.propChanges.PData.currentValue
	  }
	ngOnDestroy(): void {
		if(this._subscriptions)
		  {
			  this._subscriptions.forEach((sb) => sb.unsubscribe());
		  }
	}
  // mở khung chat tự động
	unReadMessageFromSenderUsername(IdGroup: any) {
		// 
		const userChatBox: UserChatBox[] = JSON.parse(localStorage.getItem('chatboxusers'));
		if (userChatBox) {
		  this.chatBoxUsers = userChatBox;
		} else {
		  this.chatBoxUsers = [];
		}
	  let index =this.lstContact.findIndex(x=>x.IdGroup==IdGroup);
	  let check=this.chatBoxUsers.findIndex(x=>x.user.IdGroup==IdGroup)
	  if(index>=0&&this.lstContact[index].Active===1&&this.lstContact[index].isGroup==false
		&& check<0
		
		)
	  {
	   
		this.selectUser(this.lstContact[index]);//display chat-box  
		this.soundService.playAudioMessage();
		this.chatService.UpdateUnRead(IdGroup,"read").subscribe(res=>{
		  if(res.status===1)
		  {
  
		  }
		  else
		  {
			return;
		  }
		})
	  }
	  else{
		//Update unread
		this.chatService.UpdateUnRead(IdGroup,"unread").subscribe(res=>{
		  if(res.status===1)
		  {
  
		  }
		  else
		  {
			return;
		  }
		})
	  }        
  
	  if(index>=0&&this.lstContact[index].isGroup==true&&check<0)
	  {
	   
		this.selectUser(this.lstContact[index]);//display chat-box  
		this.soundService.playAudioMessage();
	  }
	
	}
  
	CreaterGroupChat() {
		  // this.dcmt.body.classList.add('header-fixed');
		  const dialogRef = this.dialog.open(CreateConvesationGroupComponent, {
		width:'500px',
		  
			  // panelClass:'no-padding'
  
		  });
	  dialogRef.afterClosed().subscribe(res => {
		
	
					  if(res)
			{
			  const data = this.auth.getAuthFromLocalStorage();
			this.presence.NewGroup(data.access_token,res[0],res[0])
			  // this.GetContact();
			  // this.subscribeToEvents();
			  this.selectUser(res[0]);
			  this.GetContact();
			  this.changeDetectorRefs.detectChanges();
			}
					  })
			
		  }
  
		   
	CreaterUserChat() {
		// this.dcmt.body.classList.add('header-fixed');
		const dialogRef = this.dialog.open(CreateConversationUserComponent, {
	  width:'500px',
		
			// panelClass:'no-padding'

		});
	dialogRef.afterClosed().subscribe(res => {
	
					if(res)
				
		  {
		
			this.selectUser(res[0]);
			this.GetContact();
			this.changeDetectorRefs.detectChanges();
		  }
					})
		  
		}
  UpdateNewGroup()
  {
	this._ngZone.run(() => {  
   
	  this.presence.NewGroupSource$.subscribe(res=>
		{
		  if(res)
		  {
			let index=this.lstContact.findIndex(x=>x.IdGroup===res.IdGroup)
			if(index<0)
			{
			  this.lstContact.push(res);
			  this.filteredGroups.next(this.lstContact)
			  this.changeDetectorRefs.detectChanges();
			}
		 
		
		  }
		
		})
	  })
  }
  
  
  
  SetActive(item:any,active=true)
  {
   
	  let index=this.lstContact.findIndex(x=>x.UserId===item);
	  
	  if(index>=0)
	  {
	   
		this.lstContact[index].Active = active ? 1 : 0;
	   
  
	  }
	 
		this.changeDetectorRefs.detectChanges();
  }
	private subscribeToEvents(): void {  
  
	this._ngZone.run(() => {  
   
	const sb=this.presence.onlineUsers$.subscribe(res =>
	  { 
	   for(let i=0;i<res.length;i++)
	   {
		if(res[i].JoinGroup==="changeActive" )
		{
			this.SetActive(res[i].UserId,true)
		}
		else
		{
		  this.SetActive(res[i].UserId,false)
		}
	   }
	  })
	  this._subscriptions.push(sb);
	})
  
  }
  
  
  // begin  phần get danh bạ và tạo conversation 
  
  
  
  ItemConversation(): ConversationModel
  {
	const item = new ConversationModel();
	  item.ListMember=this.listmember.slice();
	return item
  }
  
  
  CreateConverSation(item)
  {
	this.listmember.push(item);
	let  data=this.ItemConversation();
	this.conversation_sevices.CreateConversation(data).subscribe(res=>
	  {
		if (res && res.status === 1) {
		  this.selectUser(res.data[0]);
			this.GetContact();
		
		}
	  })
	
  }
  
  //end phần get danh bạ và tạo conversation 
	ngOnInit(): void {
		try
		{
			this.presence.connectToken();
		}catch(err)
		{
		}
	
	  this.GetContact();
	  this.subscribeToEvents();
	  this.UpdateNewGroup();
	
	  
	  const userChatBox: UserChatBox[] = JSON.parse(localStorage.getItem('chatboxusers'));
	  if (userChatBox) {
		this.chatBoxUsers = userChatBox;
	  } else {
		this.chatBoxUsers = [];
	  }
	
	}
  
	clearUnreadMessage(IdGroup: number,UserId:number){
	  this.chatService.UpdateUnRead(IdGroup,"read").subscribe(res=>{
		if(res.status==1)
		{
		  let index = this.lstContact.findIndex(x=>x.IdGroup==IdGroup&&x.UserId==UserId);
		  if(index>=0)
		  {
			this.lstContact[index].UnreadMess=0;
		  }
		  
		}
		else
  
		{
			return;
		}
  
	  })
	}
  
  
  
	GetContact()
	{
	  this.listmember = [];
		this.chatService.GetContactChatUser().subscribe(res=>{
		  this.lstContact=res.data;
		  this.filteredGroups.next(this.lstContact.slice());
		  console.log('lstContact',this.lstContact)
		  this.changeDetectorRefs.detectChanges();
		 
		})
	 
	}
	selectUser(user: any) {
		const userChatBox: UserChatBox[] = JSON.parse(localStorage.getItem('chatboxusers'));
		if (userChatBox) {
		  this.chatBoxUsers = userChatBox;
		} else {
		  this.chatBoxUsers = [];
		}
	
	  if(user.UnreadMess > 0)
	  {
		this.clearUnreadMessage(user.IdGroup, user.UserId);
	  }
	  this.usernameActived = user.IdGroup;
	
	  switch (this.chatBoxUsers.length % 2) {
		
		case 2: {
		  var u = this.chatBoxUsers.find(x => x.user.IdGroup === user.IdGroup);
		  if (u != undefined) {
			//   this.ListBBChat.push(u);
			//   console.log(' this.ListBBChat', this.ListBBChat)
			this.chatBoxUsers = this.chatBoxUsers.filter(x => x.user.IdGroup !== user.IdGroup);
			this.chatBoxUsers.push(u);
		  } else {
			this.chatBoxUsers.push(new UserChatBox(user, 300 + 325)); 
		  }
		  localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
		  break;
		}
		case 1: {
		  var u = this.chatBoxUsers.find(x => x.user.IdGroup === user.IdGroup);
		  if (u != undefined) {
			this.chatBoxUsers = this.chatBoxUsers.filter(x => x.user.IdGroup !== user.IdGroup);
			this.chatBoxUsers.push(u);

			  this.ListBBChat.push(u);
			  console.log(' this.ListBBChat', this.ListBBChat)
		  } else {
			this.chatBoxUsers.push(new UserChatBox(user, 300 + 325));
		  }
		  localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
		  break;
		}
		default: {//0
		  var u = this.chatBoxUsers.find(x => x.user.IdGroup === user.IdGroup);
		  if (u != undefined) {
			this.chatBoxUsers = this.chatBoxUsers.filter(x => x.user.IdGroup !== user.IdGroup);
			this.chatBoxUsers.push(u);
		  } else {
			this.chatBoxUsers.push(new UserChatBox(user, 300));
		  }
		  localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
		  break;
		}
	  }
	  this.searchText="";
	  this.chatService.search$.next('activechat')
	  this.chatService.OpenMiniChat$.next(true);
	  this.changeDetectorRefs.detectChanges();
	}
  
	removeChatBox(event: number) {
	  let index =this.chatBoxUsers.findIndex(x=>x.user.IdGroup);
	  this.chatBoxUsers = this.chatBoxUsers.filter(x => x.user.IdGroup !== event);
   
	  if(this.chatBoxUsers.length===1&& index==0)
	  {
		this.chatBoxUsers[index].right=300;
		// this.chatService.reload$.next(true);
	  }
	  localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
	}
  
	activedChatBox(event: number){
	  this.usernameActived = event;
	  var u = this.chatBoxUsers.find(x => x.user.IdGroup === event);
	  if(u){
		this.chatBoxUsers = this.chatBoxUsers.filter(x => x.user.IdGroup !== event);//remove
		this.chatBoxUsers.push(u);// add to end of array
		// localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
	  }    
	}
  
}
