import { AuthService } from "src/app/modules/auth";
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  EventEmitter,
  HostListener,
  Inject,
  Input,
  NgZone,
  OnDestroy,
  OnInit,
  Output,
  TemplateRef,
  ViewChild,
} from "@angular/core";
import { NgForm } from "@angular/forms";
import { take } from "rxjs/operators";
import { DOCUMENT } from "@angular/common";
import { CdkVirtualScrollViewport } from "@angular/cdk/scrolling";
import { MessageService } from "../my-chat/services/message.service";
import { Message } from "../my-chat/models/message";
import { environment } from "src/environments/environment";
import { MatDialog } from "@angular/material/dialog";
import { EditGroupNameComponent } from "../edit-group-name/edit-group-name.component";
import { ChatService } from "../my-chat/services/chat.service";
import { BehaviorSubject } from "rxjs";
import { InsertThanhvienComponent } from "../insert-thanhvien/insert-thanhvien.component";
import { ThanhVienGroupComponent } from "../thanh-vien-group/thanh-vien-group.component";
import {
  Gallery,
  GalleryItem,
  ImageSize,
  ThumbnailsPosition,
} from "ng-gallery";
import { LayoutUtilsService } from "../../../../../jeework_old/core/utils/layout-utils.service";
import * as moment from "moment-timezone";
import { NotifyMessage } from "../my-chat/models/NotifyMess";
import { QuillEditorComponent } from "ngx-quill";
import { ShareMessageComponent } from "../../share-message/share-message.component";
import { UserChatBox } from "../my-chat/models/user-chatbox";
import { PerfectScrollbarConfigInterface } from "ngx-perfect-scrollbar";
import { PopoverContentComponent } from "ngx-smart-popover";
const HOST_JEEChat = environment.HOST_JEECHAT;
import "quill-mention";
@Component({
  selector: "app-chat-box",
  templateUrl: "./chat-box.component.html",
  styleUrls: ["./chat-box.component.scss"],

  providers: [MessageService], //separate services independently for every component
})
export class ChatBoxComponent implements AfterViewInit, OnInit, OnDestroy {
  hostjeechat: string = HOST_JEEChat;
  @ViewChild('myPopoverC', { static: true }) myPopover: PopoverContentComponent;
  public config: PerfectScrollbarConfigInterface = {};
  private _isLoading$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  get isLoading$() { return this._isLoading$.asObservable(); }
  @Input() user: any;
  listInfor: any[] = [];
  txttam: string;
  tam: any[] = [];
  chatBoxUsers: UserChatBox[] = [];
  lisTagGroup: any[] = [];
  lstTagName: any[] = [];
  listTagGroupAll: any[] = [];
  messageContent: string;
  composing: boolean = false
  composingname: string;
  show: boolean = false;
  //@ViewChild('ChatBox', { static: true }) element: ElementRef;
  userCurrent: string;
  @Input() right: number;
  @Output() removeChatBox = new EventEmitter();
  @Output() activedChatBoxEvent = new EventEmitter();
  isCollapsed = false;
  UserId: number;
  list_image: any[] = [];
  list_file: any[] = [];
  AttachFileChat: any[] = [];
  listFileChat: any[] = [];
  listChoseTagGroup: any[] = [];
  Avataruser: string;
  @ViewChild(CdkVirtualScrollViewport) viewPort: CdkVirtualScrollViewport;
  @ViewChild('messageForm') messageForm: NgForm;
  @ViewChild('scrollMe') private myScrollContainer: ElementRef;
  @ViewChild('scrollMeChat', { static: false }) scrollMeChat: ElementRef;
  constructor(public messageService: MessageService,
    private auth: AuthService,

    // @Inject(DOCUMENT) private document: Document,
    private chatService: ChatService,
    private ref: ChangeDetectorRef,
    public dialog: MatDialog,
    private layoutUtilsService: LayoutUtilsService,
    private _ngZone: NgZone,
    public gallery: Gallery,
  ) {
    const dt = this.auth.getAuthFromLocalStorage();
    this.userCurrent = dt.user.username;
    this.UserId = dt['user']['customData']['jee-account']['userID'];
    this.Avataruser = dt['user']['customData']['personalInfo']['Avatar'];

  }
  ngOnDestroy(): void {
    // this.messageService.stopHubConnection();
  }

  @HostListener('scroll', ['$event'])
  scrollHandler(event, item) {

    if (event.srcElement.scrollTop == 0) {
      // alert(item)
      // this.getIdTopChat();
      // if (!this.IsMinIdChat) {
      //  /// console.debug("Scroll to Top with page", this.idChatFrom);
      //   this.loadMoreMessChatFromId(this.idChatFrom);
      // }

    }
  }

  RemoveChoseFile(index) {
    this.listFileChat.splice(index, 1);
    this.ref.detectChanges();
  }
  RemoveChoseImage(index) {
    this.list_image.splice(index, 1);
    this.ref.detectChanges();
  }
  getClass() {
    let url = window.location.href;
    if (url.includes('Messages')) {
      return 'disable';
    }
    else {
      return 'activechatbox';
    }

  }
  scroll(item) {
    this.viewPort.scrollToIndex(item, 'smooth');
  }
  private subscribeToEventsComposing(): void {
    this._ngZone.run(() => {
      this.messageService.ComposingMess.subscribe(res => {
        if (res) {
          if (this.UserId != res.UserId && this.user.user.IdGroup == res.IdGroup) {
            this.composing = true
            this.composingname = res.Name;
          }
        }
        setTimeout(() => {
          this.composing = false;
          this.ref.detectChanges();
        }, 10000);
        this.ref.detectChanges();
      })

    })
  }
  ItemInsertMessenger(note: string): Message {
    const item = new Message();
    item.Content_mess = 'đã thêm';
    item.UserName = this.userCurrent;
    item.IdGroup = this.user.user.IdGroup;
    item.IsDelAll = false;
    item.Note = note;
    item.isInsertMember = true
    return item
  }
  sendInsertMessage(note: string) {
    this._isLoading$.next(false);
    const data = this.auth.getAuthFromLocalStorage();
    var _token = data.access_token;
    let item = this.ItemInsertMessenger(note);
    this.messageService.sendMessage(_token, item, this.user.user.IdGroup).then(() => {
    })
  }
  ItemLeaveMessenger(content: string, note: string): Message {
    const item = new Message();
    item.Content_mess = content;
    item.UserName = this.userCurrent;
    item.IdGroup = this.user.user.IdGroup;
    item.IsDelAll = true;
    item.Note = note

    return item
  }
  sendLeaveMessage(mess: string, note: string) {

    this._isLoading$.next(false);
    const data = this.auth.getAuthFromLocalStorage();

    var _token = data.access_token;
    let item = this.ItemLeaveMessenger(mess, note);
    this.messageService.sendMessage(_token, item, this.user.user.IdGroup).then(() => {
    }).catch(err => {
      console.log(err);
    });
  }

  GetInforUserChatwith(IdGroup: number) {
    this.chatService.GetInforUserChatWith(IdGroup).subscribe(res => {
      this.listInfor = res.data;
      this.ref.detectChanges();
    })
  }

  EditNameGroup(item: any) {
    const dialogRef = this.dialog.open(EditGroupNameComponent, {
      width: '400px',
      data: item
      // panelClass:'no-padding'
    });
    dialogRef.afterClosed().subscribe(res => {
      if (res) {
        this.GetInforUserChatwith(this.user.user.IdGroup)
        this.ref.detectChanges();
      }
    })
  }

  OpenThanhVien() {
    let noidung;
    let note;
    const dialogRef = this.dialog.open(ThanhVienGroupComponent, {
      width: '500px',
      data: this.user.user.IdGroup,

      // panelClass:'no-padding'

    });
    dialogRef.afterClosed().subscribe(res => {

      if (res) {
        this.GetInforUserChatwith(this.user.user.IdGroup)
        if (this.UserId == res.data) {
          noidung = 'đã rời'

          this.sendLeaveMessage(noidung, '');
          setTimeout(() => {

            this.closeBoxChat();
          }, 500);
        }
        else {
          this.chatService.GetUserById(res.data).subscribe(notedata => {
            if (notedata) {
              note = notedata.data[0].Fullname
              this.sendLeaveMessage(noidung, note);
            }
          })
          noidung = 'đã xóa '
        }

        this.ref.detectChanges();
      }
    })

  }

  InsertThanhVienGroup() {
    const dialogRef = this.dialog.open(InsertThanhvienComponent, {
      width: '500px',
      data: this.user.user.IdGroup

      // panelClass:'no-padding'

    });
    dialogRef.afterClosed().subscribe(res => {
      if (res) {
        let chuoi = "";
        res.data.forEach(element => {
          chuoi = chuoi + ',' + element.FullName
        });


        this.sendInsertMessage(chuoi.substring(1));
        this.GetInforUserChatwith(this.user.user.IdGroup)
        this.ref.detectChanges();
      }
    })

  }
  list_reaction: any[] = [];
  GetListReaction() {
    this.chatService.getlist_Reaction().subscribe(res => {
      this.list_reaction = res.data;
      this.ref.detectChanges();

    })
  }


  ngOnInit(): void {
    this.getClass();
    // this.scroll(99)
    try {
      setTimeout(() => {
        this.messageService.connectToken(this.user.user.IdGroup);
      }, 1000);
    } catch (err) {
      console.log(err)
    }

    this.GetInforUserChatwith(this.user.user.IdGroup)
    this.subscribeToEventsComposing();
    this.subscribeToEventsHidenmes();
    this.subscribeToEventsSendReaction();
    this.GetListReaction();
    this.subscribeToEventsNewMess();
    this.GetTagNameisGroup(this.user.user.isGroup)
  }
  private subscribeToEventsNewMess(): void {
    this.messageService.Newmessage.subscribe(res => {
      if (this.listChoseTagGroup.length > 0) {
        let notify = this.ItemNotifyMessenger(res[0].Content_mess, res[0].IdChat);
        this.chatService.publishNotifi(notify).subscribe(res => {

        })
      }
    })

  }
  private subscribeToEventsHidenmes(): void {


    const sb = this.messageService.hidenmess.subscribe(res => {
      if (res) {

        this.messageService.messageThread$.forEach(element => {
          let index = element.findIndex(x => x.IdChat == res)
          if (index > 0) {
            element[index].IsHidenAll = true;
            this.ref.detectChanges();
          }
        })


      }
    })



  }
  getClassActive(item) {
    if (item == 1) {
      return 'online';
    }
    else {
      return '';
    }
  }

  getClassRepy(item) {
    if (item == this.userCurrent) {
      return 'reply';
    }
    else {
      return 'reply-user';
    }
  }
  ngAfterViewInit() {
    setTimeout(() => {
      //  nhớ sửa
      var chatBox = document.getElementById(this.user.user.IdGroup);
      if (chatBox) {
        chatBox.style.right = this.right + "px";
      }

      this.ref.detectChanges();
    }, 10);

  }
  HidenMess(IdChat: number, IdGroup: number) {
    const data = this.auth.getAuthFromLocalStorage();

    var _token = data.access_token;
    this.messageService.HidenMessage(_token, IdChat, IdGroup)
  }

  getShowMoreChat(item) {
    if (item !== this.userCurrent) {
      return ' chat right';
    }
    else {
      return ' chat';
    }

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

  saverange(value) {
    if (value) {

      if (value.match(/<img/)) {
        value = value.replace(/<img(.*?)>/g, "");
      }
      value = value.replace("<p><br></p>", "");
      console.log("messageContent", value)

      this.messageContent = value;



      const data = this.auth.getAuthFromLocalStorage();

      var _token = data.access_token;
      this.messageService.Composing(_token, this.user.user.IdGroup);
    }
    else {

      return;

    }
  }


  ItemMessenger(): Message {
    const item = new Message();
    item.Content_mess = this.messageContent;
    item.UserName = this.userCurrent;
    item.IdGroup = this.user.user.IdGroup;
    if (this.listReply.length > 0) {
      item.Note = this.listReply[0].InfoUser[0].Fullname + ":" + this.listReply[0].Content_mess;
    }
    else {
      item.Note = "";
    }
    item.IsDelAll = false;
    item.IsVideoFile = this.url ? true : false;
    item.Attachment = this.AttachFileChat.slice();


    return item
  }


  RemoveVideos(index) {

    this.myFilesVideo.splice(index, 1);
    this.AttachFileChat.splice(index, 1);
    console.log("AttachFileChat Xoas", this.AttachFileChat)
    this.url = "";
    this.ref.detectChanges();

  }

  myFilesVideo: any[] = [];

  url;
  onSelectVideo(event) {

    let base64Str: any;
    const file = event.target.files && event.target.files;
    if (file) {
      var reader = new FileReader();



      // if(file.type.indexOf('video')> -1){
      //     this.format = 'video';
      //   }


      reader.onload = (event) => {
        this.myFilesVideo.push(event.target.result);
        var metaIdx = this.myFilesVideo[0].indexOf(';base64,');
        base64Str = this.myFilesVideo[0].substr(metaIdx + 8);

        this.AttachFileChat.push({ filename: file[0].name, type: file[0].type, size: file[0].size, strBase64: base64Str });
        this.url = (<FileReader>event.target).result;
      }
      reader.readAsDataURL(file[0]);
    }
  }
  onSelectFile_PDF(event) {
    this.show = false;

    if (event.target.files && event.target.files[0]) {

      var filesAmountcheck = event.target.files[0];


      var file_name = event.target.files;
      var filesAmount = event.target.files.length;

      for (var i = 0; i < this.AttachFileChat.length; i++) {
        if (filesAmountcheck.name == this.AttachFileChat[i].filename) {
          this.layoutUtilsService.showInfo("File đã tồn tại");
          return;
        }
      }
      for (let i = 0; i < filesAmount; i++) {
        var reader = new FileReader();
        //this.FileAttachName = filesAmount.name;
        let base64Str: any;
        let cat: any;
        reader.onload = (event) => {
          cat = file_name[i].name.substr(file_name[i].name.indexOf('.'));
          if (cat.toLowerCase() === '.png' || cat.toLowerCase() === '.jpg') {
            this.list_image.push(event.target.result);

            var metaIdx = this.list_image[i].indexOf(';base64,');
            base64Str = this.list_image[i].substr(metaIdx + 8);
            this.AttachFileChat.push({ filename: file_name[i].name, type: file_name[i].type, size: file_name[i].size, strBase64: base64Str });
            // console.log('list imgage',this.list_image)
          }
          else {
            this.list_file.push(event.target.result);

            if (this.list_file[i] != undefined) {
              var metaIdx = this.list_file[i].indexOf(';base64,');
            }

            if (this.list_file[i] != undefined) {
              base64Str = this.list_file[i].substr(metaIdx + 8);
            }

            this.AttachFileChat.push({ filename: file_name[i].name, type: file_name[i].type, size: file_name[i].size, strBase64: base64Str });
            //  this.AttachFileBaiDang.push({ filename: file_name[i].name,type:file_name[i].type,size:file_name[i].size,strBase64: base64Str });
            this.listFileChat.push({ filename: file_name[i].name, type: file_name[i].type, size: file_name[i].size });
          }


          this.ref.detectChanges();

        }


        //  console.log('this.list_image_Edit',this.list_image_Edit)
        reader.readAsDataURL(event.target.files[i]);
      }
    }

  }


  showPT() {
    if (this.show) {
      this.show = false;
    }
    else {
      this.show = true;
    }

  }

  NotifyTagName(content: string) {
    for (let i = 0; i < this.lstTagName.length; i++) {
      if (this.lstTagName[i] == "All") {
        this.listTagGroupAll.forEach(element => {
          this.listChoseTagGroup.push(element.id);
        });

      }
      else {

        let giatri = content.replace('/', "").indexOf(`data-id="${this.lstTagName[i]}`);

        if (giatri > -1) {
          this.listChoseTagGroup.push(this.lstTagName[i]);
        }
      }
    }
    console.log("listChoseTagGroup", this.listChoseTagGroup)
  }

  sendMessage() {
    this.messageContent = this.messageContent.replace("<p></p>", "");

    this.NotifyTagName(this.messageContent);
    if ((this.messageContent && this.messageContent != "" && this.messageContent != "<p><br></p>" && this.messageContent.length > 0) || this.AttachFileChat.length > 0) {


      const data = this.auth.getAuthFromLocalStorage();

      var _token = data.access_token;

      let item = this.ItemMessenger();
      this.messageService.sendMessage(_token, item, this.user.user.IdGroup).then(() => {
        this.listChoseTagGroup = []
        this.AttachFileChat = [];
        this.list_image = [];
        this.listFileChat = [];
        this.listReply = [];
        this.myFilesVideo = [];
        this.url = "";
        this.messageForm.reset();
      })
    }
  }

  closeBoxChat() {
    this.removeChatBox.emit(this.user.user.IdGroup);
    this.messageService.CloseMessage(this.user.user.IdGroup, this.userCurrent);

  }

  onFocusEvent($event) {

    this.chatService.OneMessage$.subscribe(res => {
      if (res > 0) {
        this.chatService.UpdateUnRead(this.user.user.IdGroup, this.user.user.UserId, "read").subscribe(res => {
          if (!res) {
            console.log("Error Update read Message")

          }
        })
        this.chatService.OneMessage$.next(0);
      }
    })




  }
  RouterLink(item) {
    window.open(item, "_blank")
  }

  activedChatBox() {

    this.activedChatBoxEvent.emit(this.user.user.IdGroup)
  }

  items: GalleryItem[] = [];
  @ViewChild('itemTemplate', { static: true }) itemTemplate: TemplateRef<any> | undefined;
  loadlightbox(id: any) {
    this.messageService.messageThread$.forEach(element => {
      let index = element.findIndex(x => x.IdChat == id)
      this.items = element[index].Attachment.map((item) => {
        return {
          type: 'imageViewer',
          data: {
            src: item.hinhanh,
            thumb: item.hinhanh,
          },
        };
      })
    });

    /** Lightbox Example */

    // Get a lightbox gallery ref
    const lightboxRef = this.gallery.ref('lightbox');

    // Add custom gallery config to the lightbox (optional)
    lightboxRef.setConfig({
      imageSize: ImageSize.Cover,
      thumbPosition: ThumbnailsPosition.Bottom,
      itemTemplate: this.itemTemplate,
      gestures: false,
    });

    // Load items into the lightbox gallery ref
    let ob = this.items;
    lightboxRef.load(this.items);
    this.ref.detectChanges();
  }

  @ViewChild('imgRenderer') imgRenderer: ElementRef;



  onPaste(event: any) {
    const items = (event.clipboardData || event.originalEvent.clipboardData).items;
    let blob = null;

    for (const item of items) {
      if (item.type.indexOf('image') === 0) {
        blob = item.getAsFile();
      }
    }

    // load image if there is a pasted image
    if (blob !== null) {
      let base64Str: any;
      var file_name = blob;
      const reader = new FileReader();
      reader.onload = (evt: any) => {
        console.log(evt.target.result); // data url!
        this.list_image.push(evt.target.result);
        var metaIdx = this.list_image[0].indexOf(';base64,');
        base64Str = this.list_image[0].substr(metaIdx + 8);
        this.AttachFileChat.push({ filename: file_name.name, type: file_name.type, size: file_name.size, strBase64: base64Str });
        this.ref.detectChanges();
      };
      reader.readAsDataURL(blob);
    }
  }

  DisplayTime(item) {

    if (item) {
      if (item.length > 0) {


        let d = item + 'Z'

        let date = new Date(d);

        var tz = moment.tz.guess()

        var dec = moment(d);
        return dec.tz(tz).format(' HH:mm DD/MM/YYYY');
      }
    }
  }
  getClassHidenTime(item) {
    if (item) {
      return 'message-body hidentime';
    }
    else {
      return 'message-body ';
    }

  }
  listReply: any[] = [];
  ReplyMess(item) {
    if (this.listReply.length == 0) {
      this.listReply.push(item);
      this.ref.detectChanges();
    }

  }
  DeleteReply() {
    this.listReply = [];
  }

  InsertRectionChat(idchat: number, type: number) {

    this.SendReaction(idchat, type);

  }
  SendReaction(idchat: number, type: number) {
    const dt = this.auth.getAuthFromLocalStorage();
    this.messageService.ReactionMessage(dt.access_token, this.user.user.IdGroup, idchat, type);
  }

  listreaction: any[] = [];
  toggleWithGreeting(idChat: number, type: number) {


    this.chatService.GetUserReaction(idChat, type).subscribe
      (res => {
        this.listreaction = res.data;
        this.ref.detectChanges();
      })

  }

  private subscribeToEventsSendReaction(): void {


    this._ngZone.run(() => {

      this.messageService.reaction.subscribe(res => {
        console.log("REACTION", res)
        if (res) {
          this.messageService.messageThread$.forEach(element => {
            let index = element.findIndex(x => x.IdChat == res.data[0].IdChat)
            if (index >= 0) {
              // element[index].IsHidenAll=true;
              element[index].ReactionChat = res.data[0].ReactionChat.slice();
              if (res.data[0].ReactionUser.CreateBy == this.UserId) {
                element[index].ReactionUser = Object.assign(res.data[0].ReactionUser);
              }

              this.ref.detectChanges();
            }
          })
        }
      })

    })



  }
  urlify(item) {
    var urlRegex = /(https?:\/\/[^\s]+)/g;
    return item.replace(urlRegex, function (url) {
      return '<pre><a target="_blank" href="' + url.replace("</p>", '') + '">' + url.replace("</p>", '') + '</a></pre>';
    })
  }
  getClassAtt(item) {
    if ((item.Attachment.length > 0 || item.Attachment_File.length > 0) && (item.Content_mess == "" || item.Content_mess == null)) {
      return 'styatt';
    }
    else {
      return '';
    }

  }

  ChuyenTiepMess(item) {
    // this.dcmt.body.classList.add('header-fixed');
    const dialogRef = this.dialog.open(ShareMessageComponent, {
      width: '600px',
      data: { item },


    });
    dialogRef.afterClosed().subscribe(res => {


      if (res) {
        //   const data = this.auth.getAuthFromLocalStorage();
        // this.presence.NewGroup(data.access_token,res[0],res[0])

        this.ref.detectChanges();
      }
    })

  }

  @ViewChild(QuillEditorComponent, { static: true })
  editor: QuillEditorComponent;
  modules = {
    toolbar: false,

    mention: {
      mentionListClass: "ql-mention-list mat-elevation-z8",
      allowedChars: /^[A-Za-z\sÅÄÖåäö]*$/,
      showDenotationChar: false,
      // mentionDenotationChars: ["@", "#"],
      spaceAfterInsert: false,
      onSelect: (item, insertItem) => {
        let index = this.lstTagName.findIndex(x => x == item.id);
        if (index < 0) {
          this.lstTagName.push(item.id)
        }
        console.log("IIIIIIIIII", this.lstTagName)
        const editor = this.editor.quillEditor;
        insertItem(item);
        // necessary because quill-mention triggers changes as 'api' instead of 'user'
        editor.insertText(editor.getLength() - 1, "", "user");
      },
      renderItem: function (item, searchTerm) {

        if (item.Avatar) {
          return `
        <div style="display:flex" >
       
        <img  style="    width: 30px;
        height: 30px;
        border-radius: 50px;" src="${item.Avatar}">
        <span>  ${item.value}</span>
      
        
      
       
        </div>`;
        }
        else if (item.id !== "All") {
          return `
        <div style="    display: flex;
        align-items: center;" >
       
          <div  style="     height: 30px;
          border-radius: 50px;    width: 30px; ;background-color:${item.BgColor}">
          </div>
          <span style=" position: absolute;     left: 20px;  color: white;">${item.value.slice(0, 1)}</span>
          <span style=" padding-left: 5px;">  ${item.value}</span>
  
        </div>`;
        }
        else {
          return `
        <div style="    display: flex;
        align-items: center;" >
       
          <div  style="     height: 30px;
          border-radius: 50px;    width: 30px; ;background-color:#F3D79F">
          </div>
          <span style=" position: absolute;     left: 20px;  color: white;">@</span>
          <span style=" padding-left: 5px;">${item.note}</span>
          <span style=" padding-left: 5px;">  ${item.value}</span>
  
        </div>`;
        }
      },
      source: (searchTerm, renderItem) => {
        const values = this.lisTagGroup;



        if (searchTerm.length === 0) {
          renderItem(values, searchTerm);

        } else {
          const matches = [];

          values.forEach(entry => {
            if (
              entry.value.toLowerCase().replace(/\s/g, '').indexOf(searchTerm.toLowerCase()) !== -1
            ) {
              matches.push(entry);
            }
          });

          renderItem(matches, searchTerm)

        }
      }
    }
  };

  GetTagNameisGroup(isGroup) {
    console.log("AAA", isGroup)
    if (isGroup) {
      this.tam = [
        {
          id: "All", note: "Nhắc cả nhóm", value: "@All"
        }
      ]
    }
    else {
      this.tam = [];
    }
    this.chatService.GetTagNameGroup(this.user.user.IdGroup).subscribe(res => {
      this.lisTagGroup = this.tam.concat(res.data);
      this.listTagGroupAll = res.data;
      console.log("GetTagNameGroup", this.lisTagGroup)
      this.ref.detectChanges();
    })
  }

  ItemNotifyMessenger(content: string, idchat: number): NotifyMessage {

    const item = new NotifyMessage();
    item.TenGroup = this.user.user.GroupName;
    item.Avatar = this.Avataruser;
    item.IdChat = idchat;
    item.IdGroup = this.user.user.IdGroup;
    item.Content = content;
    item.ListTagname = this.listChoseTagGroup;
    return item
  }
}

