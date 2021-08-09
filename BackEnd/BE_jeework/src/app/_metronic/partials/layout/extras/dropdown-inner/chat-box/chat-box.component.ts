import { environment } from 'src/environments/environment';
import { MessageService } from './../../../../../../modules/my-chat/services/message.service';
import { ChatService } from './../../../../../../modules/my-chat/services/chat.service';

import { AuthService } from 'src/app/modules/auth';
import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Inject, Input, NgZone, OnDestroy, OnInit, Output, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { take } from 'rxjs/operators';
import { Message } from 'src/app/modules/my-chat/models/message';
import { DOCUMENT } from '@angular/common';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
const   HOST_JEEChat=environment.HOST_JEECHAT;

@Component({
  selector: 'app-chat-box',
  templateUrl: './chat-box.component.html',
  styleUrls: ['./chat-box.component.scss'],

  providers: [MessageService]//separate services independently for every component
})
export class ChatBoxComponent implements AfterViewInit, OnInit, OnDestroy {
  hostjeechat:string=HOST_JEEChat;
  @Input() user: any;
  messageContent: string;
  //@ViewChild('ChatBox', { static: true }) element: ElementRef;
  userCurrent: string;
  @Input() right: number;
  @Output() removeChatBox = new EventEmitter();
  @Output() activedChatBoxEvent = new EventEmitter();
  isCollapsed = false;
  @ViewChild(CdkVirtualScrollViewport) viewPort: CdkVirtualScrollViewport;
  @ViewChild('messageForm') messageForm: NgForm;
  @ViewChild('scrollMe') private myScrollContainer: ElementRef;
  @ViewChild('scrollMeChat', { static: false }) scrollMeChat: ElementRef;
  constructor( public messageService: MessageService,
    private auth:AuthService,
    @Inject(DOCUMENT) private document: Document,
    private chatService:ChatService,
    private ref: ChangeDetectorRef,
    private _ngZone:NgZone,
    ) {
const dt=this.auth.getAuthFromLocalStorage();
this.userCurrent=dt.user.username
    // this.accountService.currentUser$.pipe(take(1)).subscribe(user => this.userCurrent = user);
  }
  ngOnDestroy(): void {
    this.messageService.stopHubConnection();
  }

  @HostListener('scroll', ['$event'])
  scrollHandler(event,item) {

    if (event.srcElement.scrollTop == 0) {
      // alert(item)
      // this.getIdTopChat();
      // if (!this.IsMinIdChat) {
      //  /// console.debug("Scroll to Top with page", this.idChatFrom);
      //   this.loadMoreMessChatFromId(this.idChatFrom);
      // }

    }

  }
  
  getClass()
  {  let url = window.location.href;
    if(url.includes('Messages'))
    {
      return 'disable';
    }
    else

    {
      return 'activechatbox';
    }

  }
  scroll(item) {
   
    this.viewPort.scrollToIndex(item, 'smooth');
  }
  ngOnInit(): void {
 
    // console.log(this.messageService.messageThread$.le)
  this.getClass();
    // this.scroll(99)
    try{

    }catch(err)
    {
    }
    setTimeout(() => {
      this.messageService.connectToken(this.user.user.IdGroup);
    }, 1000);
    
  }

  ngAfterViewInit() {
    setTimeout(() => {
      var chatBox = document.getElementById(this.user.user.IdGroup);
      chatBox.style.right = this.right + "px";
      this.ref.detectChanges();
    }, 10);
 
  }

  ngAfterViewChecked() {
    this.ref.detectChanges();
  }

  @HostListener("scroll", ["$event"])
  onScroll(event) {
   
    /* let pos = event.target.scrollTop + event.target.offsetHeight;
    let max = event.target.scrollHeight;
    pos/max will give you the distance between scroll bottom and and bottom of screen in percentage.
    if (pos == max) {
      this.messageService.seenMessage(this.user.userName);
    } */
  }

  
  ItemMessenger(): Message {
		const item = new Message();
    item.Content_mess=this.messageContent;
    item.UserName=this.userCurrent;
    item.IdGroup=this.user.user.IdGroup;

    return item
  }


  sendMessage() {
    if(this.messageContent.length>=0)
    {

    
    const data=this.auth.getAuthFromLocalStorage();

       var _token =data.access_token;
    let  item =this.ItemMessenger();
    this.messageService.sendMessage(_token,item,this.user.user.IdGroup).then(() => {
     
      this.messageForm.reset();
    })
}
  }

  closeBoxChat() {
    this.removeChatBox.emit(this.user.user.IdGroup);
  }

  onFocusEvent(event: any) {
  }
  
  activedChatBox() {
  
    this.activedChatBoxEvent.emit(this.user.user.IdGroup)
  }
}
