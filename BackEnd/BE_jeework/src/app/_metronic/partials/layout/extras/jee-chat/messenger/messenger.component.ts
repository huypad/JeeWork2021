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
import { UserChatBox } from "../my-chat/models/user-chatbox";
import { PresenceService } from "../my-chat/services/presence.service";
import { MessageService } from "../my-chat/services/message.service";
import { SoundService } from "../my-chat/services/sound.service";
import { ChatService } from "../my-chat/services/chat.service";
import { ConversationService } from "../my-chat/services/conversation.service";
import { CreateConvesationGroupComponent } from "../create-convesation-group/create-convesation-group.component";
import { CreateConversationUserComponent } from "../create-conversation-user/create-conversation-user.component";
import { ConversationModel } from "../my-chat/models/conversation";
import { TranslateService } from "@ngx-translate/core";
import {
  LayoutUtilsService,
  MessageType,
} from "../../../../../jeework_old/core/utils/layout-utils.service";
import { Title } from "@angular/platform-browser";

@Component({
  selector: "kt-messenger",
  templateUrl: "./messenger.component.html",
  styleUrls: ["./messenger.component.scss"],
})
export class MessengerComponent implements OnInit, OnDestroy, OnChanges {
  private _subscriptions: Subscription[] = [];
  chatBoxUsers: UserChatBox[] = [];
  usernameActived: number;
  userCurrent: string;
  searchText: string;
  lstContact: any[] = [];
  listDanhBa: any[] = [];
  listmember: any[] = [];
  UserId: number;
  contentnotfy: string;
  list_userchat: any[] = [];
  dem: number = 0;
  @Input() PData: number;
  propChanges: any;
  public searchControl: FormControl = new FormControl();
  public filteredGroups: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  constructor(
    private changeDetectorRefs: ChangeDetectorRef,
    public presence: PresenceService,
    public messageService: MessageService,
    private soundService: SoundService,
    private chatService: ChatService,
    private _ngZone: NgZone,
    public dialog: MatDialog,
    private auth: AuthService,
    private conversation_sevices: ConversationService,
    private titleService: Title,
    private translate: TranslateService,
    private layoutUtilsService: LayoutUtilsService
  ) {
    const sb = this.presence.OpenmessageUsername$.subscribe((IdGroup) => {
      this.unReadMessageFromSenderUsername(IdGroup);
    });
    this._subscriptions.push(sb);

    const dt = this.auth.getAuthFromLocalStorage();
    this.userCurrent = dt?.user.username;
    this.UserId = dt["user"]["customData"]["jee-account"]["userID"];
  }
  ngOnChanges(changes: SimpleChanges) {
    this.propChanges = changes;
    this.searchText = this.propChanges.PData.currentValue;
  }
  ngOnDestroy(): void {
    if (this._subscriptions) {
      this._subscriptions.forEach((sb) => sb.unsubscribe());
    }
  }
  // mở khung chat tự động
  // mở khung chat tự động
  unReadMessageFromSenderUsername(datamess: any) {
    const userChatBox: any[] = JSON.parse(localStorage.getItem("chatboxusers"));
    if (userChatBox) {
      this.chatBoxUsers = userChatBox;
    } else {
      this.chatBoxUsers = [];
    }
    let index = this.lstContact.findIndex((x) => x.IdGroup == datamess.IdGroup);
    let check = this.chatBoxUsers.findIndex(
      (x) => x.user.IdGroup == datamess.IdGroup
    );

    if (index >= 0) {
      if (index >= 0 && check < 0) {
        this.soundService.playAudioMessage();

        this.chatService.OneMessage$.next(1);

        //  this.dem+=1;
        this.selectUser(this.lstContact[index]);
        //  this.chatService.UnreadMess$.next(this.dem);

        if (this.lstContact[index].isGroup == false) {
          if (
            datamess.Attachment.length > 0 ||
            datamess.Attachment_File.length > 0
          ) {
            this.contentnotfy = "Gửi một file đính kèm";
          } else {
            this.contentnotfy = datamess.Content_mess;
          }
          const data = this.auth.getAuthFromLocalStorage();
          this.chatService
            .publishMessNotifi(
              data.access_token,
              datamess.IdGroup,
              this.contentnotfy,
              datamess.InfoUser[0].Fullname,
              datamess.InfoUser[0].Avatar
            )
            .subscribe((res) => {});
          // this.chatService.UpdateUnRead(datamess.IdGroup,this.UserId,"unread").subscribe(res=>{
          //     if(res.status===1)
          //     {

          // 	this.GetContact();

          //     }
          //     else
          //     {
          //       return;
          //     }

          // })
        } else {
          const sb = this.chatService
            .UpdateUnReadGroup(datamess.IdGroup, this.userCurrent, "unread")
            .subscribe((res) => {
              if (res.status === 1) {
              } else {
                return;
              }
            });
        }
        //display chat-box

        // this.chatService.UpdateUnRead(IdGroup,"read").subscribe(res=>{
        //   if(res.status===1)
        //   {

        //   }
        //   else
        //   {
        // 	return;
        //   }
        // })
      } else {
        if (this.lstContact[index].isGroup == false) {
          this.chatService
            .UpdateUnRead(datamess.IdGroup, this.UserId, "unread")
            .subscribe((res) => {
              if (res.status === 1) {
                this.dem = 0;
                this.lstContact[index].UnreadMess =
                  this.lstContact[index].UnreadMess + 1;
                this.getSoLuongMessUnread();
              } else {
                return;
              }
            });
        } else {
          const sb = this.chatService
            .UpdateUnReadGroup(datamess.IdGroup, this.userCurrent, "unread")
            .subscribe((res) => {
              if (res.status === 1) {
                this.dem = 0;
                this.lstContact[index].UnreadMess =
                  this.lstContact[index].UnreadMess + 1;
                this.getSoLuongMessUnread();
              } else {
                return;
              }
            });
        }
      }
    }
    if (index >= 0 && this.lstContact[index].isGroup == true && check < 0) {
      // dành cho group
      this.soundService.playAudioMessage();

      this.chatService.OneMessage$.next(1);
      // this.dem+=1

      this.selectUser(this.lstContact[index]); //display chat-box

      // this.chatService.UnreadMess$.next(this.dem);

      if (
        datamess.Attachment.length > 0 ||
        datamess.Attachment_File.length > 0
      ) {
        this.contentnotfy = "Gửi một file đính kèm";
      } else {
        this.contentnotfy = datamess.Content_mess;
      }
      const data = this.auth.getAuthFromLocalStorage();
      this.chatService
        .publishMessNotifiGroup(
          data.access_token,
          datamess.IdGroup,
          this.contentnotfy,
          datamess.InfoUser[0].Fullname
        )
        .subscribe((res) => {});
    }
    this.changeDetectorRefs.detectChanges();
  }

  CreaterGroupChat() {
    // this.dcmt.body.classList.add('header-fixed');
    const dialogRef = this.dialog.open(CreateConvesationGroupComponent, {
      // width: '500px',
      panelClass: "mat-dialog-popup",
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        const data = this.auth.getAuthFromLocalStorage();
        this.presence.NewGroup(data.access_token, res[0], res[0]);
        // this.GetContact();
        // this.subscribeToEvents();
        this.selectUser(res[0]);
        this.GetContact();
        this.changeDetectorRefs.detectChanges();
      }
    });
  }

  CreaterUserChat() {
    // this.dcmt.body.classList.add('header-fixed');
    const dialogRef = this.dialog.open(CreateConversationUserComponent, {
      // width: '500px',
      panelClass: "mat-dialog-popup",
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        this.selectUser(res[0]);
        this.GetContact();
        this.changeDetectorRefs.detectChanges();
      }
    });
  }
  UpdateNewGroup() {
    this._ngZone.run(() => {
      this.presence.NewGroupSource$.subscribe((res) => {
        if (res) {
          let index = this.lstContact.findIndex(
            (x) => x.IdGroup === res.IdGroup
          );
          if (index < 0) {
            this.lstContact.push(res);
            this.filteredGroups.next(this.lstContact);
            this.changeDetectorRefs.detectChanges();
          }
        }
      });
    });
  }

  SetActive(item: any, active = true) {
    let index = this.lstContact.findIndex((x) => x.UserId === item);

    if (index >= 0) {
      this.lstContact[index].Active = active ? 1 : 0;
    }

    this.changeDetectorRefs.detectChanges();
  }
  private subscribeToEvents(): void {
    this._ngZone.run(() => {
      const sb = this.presence.onlineUsers$.subscribe((res) => {
        for (let i = 0; i < res.length; i++) {
          if (res[i].JoinGroup === "changeActive") {
            this.SetActive(res[i].UserId, true);
          } else {
            this.SetActive(res[i].UserId, false);
          }
        }
      });
      this._subscriptions.push(sb);
    });
  }

  // begin  phần get danh bạ và tạo conversation

  ItemConversation(): ConversationModel {
    const item = new ConversationModel();
    item.ListMember = this.listmember.slice();
    return item;
  }

  CreateConverSation(item) {
    this.listmember.push(item);
    let data = this.ItemConversation();
    this.conversation_sevices.CreateConversation(data).subscribe((res) => {
      if (res && res.status === 1) {
        this.selectUser(res.data[0]);
        this.GetContact();
      }
    });
  }

  //end phần get danh bạ và tạo conversation
  getSoLuongMessUnread() {
    this.lstContact.forEach((element) => {
      if (element.UnreadMess > 0) {
        this.dem += 1;
      }
    });
    this.chatService.UnreadMess$.next(this.dem);
    this.titleService.setTitle(
      "(" + this.dem + ")" + " Quản lý công việc | Tasks management"
    );
  }

  setIntrvl() {
    setInterval(() => {
      if (this.dem > 0) {
        this.titleService.setTitle("Bạn có (" + this.dem + ")" + " tin nhắn chưa đọc");
      }
    }, 1000);
  }

  setIntrvl1() {
    setInterval(() => {
      if (this.dem > 0) {
        this.titleService.setTitle(
          "(" + this.dem + ")" + " Quản lý công việc | Tasks management"
        );
      } else if (this.dem === 0) {
        this.titleService.setTitle(" Quản lý công việc | Tasks management");
      }
    }, 2000);
  }
  EventsetTitleNull() {
    this.chatService.OneMessage$.subscribe((res) => {
      if (res == 0) {
        this.dem = 0;
      }
      // else if(res==1)
      // {
      // 	this.dem=this.dem+res;
      // }
      else if (res == -1) {
        this.dem = this.dem - 1;
      }
    });
  }
  ngOnInit(): void {
    this.GetContact();
    this.subscribeToEvents();
    this.UpdateNewGroup();
    this.setIntrvl();
    this.setIntrvl1();
    this.EventsetTitleNull();

    const userChatBox: UserChatBox[] = JSON.parse(
      localStorage.getItem("chatboxusers")
    );
    if (userChatBox) {
      this.chatBoxUsers = userChatBox;
    } else {
      this.chatBoxUsers = [];
    }
    //   this.searchControl.valueChanges
    //   .pipe()
    //   .subscribe(() => {
    // 	// this.filterBankGroups();
    //   });
  }
  GetContact() {
    this.listmember = [];
    this.chatService.GetContactChatUser().subscribe((res) => {
      this.lstContact = res.data;
      this.listTam = res.data;
      this.getSoLuongMessUnread();
      this.filteredGroups.next(this.lstContact.slice());

      this.changeDetectorRefs.detectChanges();
    });
  }
  selectUser(user: any) {
    const userChatBox: UserChatBox[] = JSON.parse(
      localStorage.getItem("chatboxusers")
    );
    if (userChatBox) {
      this.chatBoxUsers = userChatBox;
    } else {
      this.chatBoxUsers = [];
    }

    if (user.UnreadMess > 0) {
      this.dem = this.dem - 1;
      this.chatService.UnreadMess$.next(this.dem);
      this.UpdateUnreadMess(user.IdGroup, user.UserId, user.UnreadMess);
    }
    this.usernameActived = user.IdGroup;

    switch (this.chatBoxUsers.length) {
      case 2: {
        var u = this.chatBoxUsers.find((x) => x.user.IdGroup === user.IdGroup);
        if (u != undefined) {
          this.chatBoxUsers = this.chatBoxUsers.filter(
            (x) => x.user.IdGroup !== user.IdGroup
          );
          this.chatBoxUsers.push(u);
        } else {
          this.chatBoxUsers.push(new UserChatBox(user, 625 + 325));
          this.chatService.OpenMiniChat$.next(new UserChatBox(user, 625 + 325));
        }
        localStorage.setItem("chatboxusers", JSON.stringify(this.chatBoxUsers));
        break;
      }
      case 1: {
        var u = this.chatBoxUsers.find((x) => x.user.IdGroup === user.IdGroup);
        if (u != undefined) {
          this.chatBoxUsers = this.chatBoxUsers.filter(
            (x) => x.user.IdGroup !== user.IdGroup
          );
          this.chatBoxUsers.push(u);
        } else {
          this.chatBoxUsers.push(new UserChatBox(user, 300 + 325));
          this.chatService.OpenMiniChat$.next(new UserChatBox(user, 300 + 325));
        }
        localStorage.setItem("chatboxusers", JSON.stringify(this.chatBoxUsers));
        break;
      }
      default: {
        //0 nó vào đây trc nếu có 1 hội thoại để xem thử
        var u = this.chatBoxUsers.find((x) => x.user.IdGroup === user.IdGroup);
        if (u != undefined) {
          this.chatBoxUsers = this.chatBoxUsers.filter(
            (x) => x.user.IdGroup !== user.IdGroup
          );
          this.chatBoxUsers.push(u);
        } else {
          this.chatBoxUsers.push(new UserChatBox(user, 300));
          let item = new UserChatBox(user, 300);
          //this.chatService.ChangeDatachat(new UserChatBox(user, 625 + 325));
          this.chatService.OpenMiniChat$.next(item);
        }
        localStorage.setItem("chatboxusers", JSON.stringify(this.chatBoxUsers));
        break;
      }
    }
    this.searchText = "";
    this.chatService.search$.next("activechat");
    this.changeDetectorRefs.detectChanges();
  }

  removeChatBox(event: number) {
    let index = this.chatBoxUsers.findIndex((x) => x.user.IdGroup);
    this.chatBoxUsers = this.chatBoxUsers.filter(
      (x) => x.user.IdGroup !== event
    );

    if (this.chatBoxUsers.length === 1 && index == 0) {
      this.chatBoxUsers[index].right = 25;
      // this.chatService.reload$.next(true);
    }
    localStorage.setItem("chatboxusers", JSON.stringify(this.chatBoxUsers));
  }

  activedChatBox(event: number) {
    this.usernameActived = event;
    var u = this.chatBoxUsers.find((x) => x.user.IdGroup === event);
    if (u) {
      this.chatBoxUsers = this.chatBoxUsers.filter(
        (x) => x.user.IdGroup !== event
      ); //remove
      this.chatBoxUsers.push(u); // add to end of array
      // localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
    }
  }

  getNameUser(item: any) {
    let name = item.Fullname.split(" ")[item.Fullname.split(" ").length - 1];
    return name.substring(0, 1).toUpperCase();
  }

  getColorNameUser(item: any) {
    let value = item.Fullname.split(" ")[item.Fullname.split(" ").length - 1];
    let result = "";
    switch (value) {
      case "A":
        return (result = "rgb(51 152 219)");
      case "Ă":
        return (result = "rgb(241, 196, 15)");
      case "Â":
        return (result = "rgb(142, 68, 173)");
      case "B":
        return (result = "#0cb929");
      case "C":
        return (result = "rgb(91, 101, 243)");
      case "D":
        return (result = "rgb(44, 62, 80)");
      case "Đ":
        return (result = "rgb(127, 140, 141)");
      case "E":
        return (result = "rgb(26, 188, 156)");
      case "Ê":
        return (result = "rgb(51 152 219)");
      case "G":
        return (result = "rgb(241, 196, 15)");
      case "H":
        return (result = "rgb(248, 48, 109)");
      case "I":
        return (result = "rgb(142, 68, 173)");
      case "K":
        return (result = "#2209b7");
      case "L":
        return (result = "rgb(44, 62, 80)");
      case "M":
        return (result = "rgb(127, 140, 141)");
      case "N":
        return (result = "rgb(197, 90, 240)");
      case "O":
        return (result = "rgb(51 152 219)");
      case "Ô":
        return (result = "rgb(241, 196, 15)");
      case "Ơ":
        return (result = "rgb(142, 68, 173)");
      case "P":
        return (result = "#02c7ad");
      case "Q":
        return (result = "rgb(211, 84, 0)");
      case "R":
        return (result = "rgb(44, 62, 80)");
      case "S":
        return (result = "rgb(127, 140, 141)");
      case "T":
        return (result = "#bd3d0a");
      case "U":
        return (result = "rgb(51 152 219)");
      case "Ư":
        return (result = "rgb(241, 196, 15)");
      case "V":
        return (result = "#759e13");
      case "X":
        return (result = "rgb(142, 68, 173)");
      case "W":
        return (result = "rgb(211, 84, 0)");
    }
    return result;
  }

  getClass(item) {
    return item > 0 ? "unread " : "lastmess";
  }

  UpdateUnreadMess(IdGroup: number, UserId: number, count: number) {
    localStorage.setItem("chatGroup", JSON.stringify(IdGroup));
    if (count > 0) {
      let index = this.lstContact.findIndex((x) => x.IdGroup == IdGroup);
      this.lstContact[index].UnreadMess = 0;

      this.chatService
        .UpdateUnRead(IdGroup, UserId, "read")
        .subscribe((res) => {
          if (res) {
            this.dem = 0;
            this.getSoLuongMessUnread();
          }
        });
      this.changeDetectorRefs.detectChanges();
    }
  }

  DeleteGroupChat(user: any) {
    const _title = this.translate.instant("landingpagekey.xoa");
    const _description = this.translate.instant(
      "landingpagekey.bancochacchanmuonxoakhong"
    );
    const _waitDesciption = this.translate.instant(
      "landingpagekey.dulieudangduocxoa"
    );
    const _deleteMessage = this.translate.instant(
      "landingpagekey.xoathanhcong"
    );

    const dialogRef = this.layoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      }
      this.conversation_sevices
        .DeleteConversation(user.IdGroup)
        .subscribe((res) => {
          if (res && res.status === 1) {
            this.layoutUtilsService.showActionNotification(
              _deleteMessage,
              MessageType.Delete,
              4000
            );
          } else {
            this.layoutUtilsService.showActionNotification(
              res.error.message,
              MessageType.Read,
              999999999,
              true,
              false,
              3000,
              "top",
              0
            );
          }
          this.ngOnInit();
        });
    });
  }

  listunread: any[] = [];
  listTam: any[] = [];
  changed(item) {
    this.listunread = [];
    if (item == 1) {
      this.GetContact();
    } else if (item == 2) {
      this.listTam.forEach((item) => {
        if (item.UnreadMess > 0) {
          this.listunread.push(item);
        }
      });
      this.lstContact = this.listunread.slice();
      this.changeDetectorRefs.detectChanges();
    } else if (item == 3) {
      this.listTam.forEach((item) => {
        if (item.UnreadMess == 0) {
          this.listunread.push(item);
        }
      });
      this.lstContact = this.listunread.slice();
      this.changeDetectorRefs.detectChanges();
    }
  }
  listAllread: any[] = [];
  AllRead() {
    this.listAllread = [];
    this.listTam.forEach((item) => {
      if (item.UnreadMess > 0) {
        this.listAllread.push(item);
      }
    });
    this.listAllread.forEach((item) => {
      //user bt

      let index = this.lstContact.findIndex((x) => x.IdGroup == item.IdGroup);
      if (index >= 0) {
        this.lstContact[index].UnreadMess = 0;
      }
      if (item.isGroup) {
        this.chatService
          .UpdateUnRead(item.IdGroup, item.UserId, "read")
          .subscribe((res) => {
            if (res) {
            } else {
              console.log("Eror");
            }
          });
      } else {
        //  group

        this.chatService
          .UpdateUnReadGroup(item.IdGroup, item.Username, "read")
          .subscribe((res) => {
            if (res.status === 1) {
            } else {
              console.log("Eror");
            }
          });
      }
    });
    this.dem = 0;
    this.changeDetectorRefs.detectChanges();
  }
}
