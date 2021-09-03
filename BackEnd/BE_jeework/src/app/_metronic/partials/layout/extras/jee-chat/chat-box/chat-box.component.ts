import { AuthService } from 'src/app/modules/auth';
import { AfterViewInit, ChangeDetectionStrategy, ChangeDetectorRef, Component, ElementRef, EventEmitter, HostListener, Inject, Input, NgZone, OnDestroy, OnInit, Output, TemplateRef, ViewChild } from '@angular/core';
import { NgForm } from '@angular/forms';
import { take } from 'rxjs/operators';
import { DOCUMENT } from '@angular/common';
import { CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { MessageService } from '../my-chat/services/message.service';
import { Message } from '../my-chat/models/message';
import { environment } from 'src/environments/environment';
import { MatDialog } from '@angular/material/dialog';
import { EditGroupNameComponent } from '../edit-group-name/edit-group-name.component';
import { ChatService } from '../my-chat/services/chat.service';
import { BehaviorSubject } from 'rxjs';
import { InsertThanhvienComponent } from '../insert-thanhvien/insert-thanhvien.component';
import { ThanhVienGroupComponent } from '../thanh-vien-group/thanh-vien-group.component';
import { Gallery, GalleryItem, ImageSize, ThumbnailsPosition } from 'ng-gallery';
import {LayoutUtilsService} from '../../../../../jeework_old/core/utils/layout-utils.service';
const HOST_JEEChat = environment.HOST_JEECHAT;
@Component({
  selector: 'app-chat-box',
  templateUrl: './chat-box.component.html',
  styleUrls: ['./chat-box.component.scss'],

  providers: [MessageService]//separate services independently for every component
})
export class ChatBoxComponent implements AfterViewInit, OnInit, OnDestroy {
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
  //==================================cập nhật jeechat 30/08/2021====================
  listInfor: any[] = [];
  AttachFileChat: any[] = [];
  listFileChat: any[] = [];
  show: boolean = false;
  list_image: any[] = [];
  list_file: any[] = [];
  UserId: number;
  private _isLoading$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  composing: boolean = false
  composingname: string;
  hostjeechat: string = HOST_JEEChat;
  constructor(public messageService: MessageService,
    private auth: AuthService,
    @Inject(DOCUMENT) private document: Document,
    private ref: ChangeDetectorRef,
    private _ngZone: NgZone,
    public dialog: MatDialog,
    private chatService: ChatService,
    private layoutUtilsService: LayoutUtilsService,
    public gallery: Gallery,
  ) {
    const dt = this.auth.getAuthFromLocalStorage();
    this.userCurrent = dt.user.username;
    this.UserId = dt['user']['customData']['jee-account']['userID'];
    console.log("USERRRR", this.user)
    // this.accountService.currentUser$.pipe(take(1)).subscribe(user => this.userCurrent = user);
  }
  ngOnDestroy(): void {
    this.messageService.stopHubConnection();
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
  ngOnInit(): void {

    // console.log(this.messageService.messageThread$.le)
    this.getClass();
    // this.scroll(99)
    try {

    } catch (err) {
      console.log(err)
    }
    setTimeout(() => {
      this.messageService.connectToken(this.user.user.IdGroup);
    }, 1000);
    this.GetInforUserChatwith(this.user.user.IdGroup);
    this.subscribeToEventsComposing();
    this.subscribeToEventsHidenmes();

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

  }


  ItemMessenger(): Message {
    const item = new Message();
    item.Content_mess = this.messageContent;
    item.UserName = this.userCurrent;
    item.IdGroup = this.user.user.IdGroup;

    return item
  }


  sendMessage() {
    if (this.messageContent !== '' && this.messageContent || this.AttachFileChat.length > 0) {
      const data = this.auth.getAuthFromLocalStorage();
      var _token = data.access_token;
      let item = this.ItemMessenger();
      this.messageService.sendMessage(_token, item, this.user.user.IdGroup).then(() => {
        this.AttachFileChat = [];
        this.list_image = [];
        this.listFileChat = [];
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
  //==================================cập nhật jeechat 30/08/2021====================
  getClassActive(item) {
    if (item == 1) {
      return 'online';
    }
    else {
      return '';
    }
  }

  EditNameGroup(item: any) {
    const dialogRef = this.dialog.open(EditGroupNameComponent, {
      width: '400px',
      data: item
    });
    dialogRef.afterClosed().subscribe(res => {
      if (res) {
        this.GetInforUserChatwith(this.user.user.IdGroup)
        this.ref.detectChanges();
      }
    })
  }

  GetInforUserChatwith(IdGroup: number) {
    this.chatService.GetInforUserChatWith(IdGroup).subscribe(res => {
      this.listInfor = res.data;
      this.ref.detectChanges();
    })
  }

  showPT() {
    if (this.show) {
      this.show = false;
    }
    else {
      this.show = true;
    }
  }

  saverange(value) {
    if (value) {
      const data = this.auth.getAuthFromLocalStorage();
      var _token = data.access_token;
      this.messageService.Composing(_token, this.user.user.IdGroup);
    }
    else {
      return;
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
          if (cat === '.png' || cat === '.jpg') {
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
        reader.readAsDataURL(event.target.files[i]);
      }
    }

  }

  InsertThanhVienGroup() {
    const dialogRef = this.dialog.open(InsertThanhvienComponent, {
      width: '500px',
      data: this.user.user.IdGroup
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

  sendInsertMessage(note: string) {
    this._isLoading$.next(false);
    const data = this.auth.getAuthFromLocalStorage();
    var _token = data.access_token;
    let item = this.ItemInsertMessenger(note);
    this.messageService.sendMessage(_token, item, this.user.user.IdGroup).then(() => {
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

  ItemLeaveMessenger(content: string, note: string): Message {
    const item = new Message();
    item.Content_mess = content;
    item.UserName = this.userCurrent;
    item.IdGroup = this.user.user.IdGroup;
    item.IsDelAll = true;
    item.Note = note

    return item
  }

  RouterLink(item) {
    window.open(item, "_blank")
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

  RemoveChoseFile(index) {
    this.listFileChat.splice(index, 1);
    this.ref.detectChanges();
  }
  RemoveChoseImage(index) {
    this.list_image.splice(index, 1);
    this.ref.detectChanges();
  }
  HidenMess(IdChat: number, IdGroup: number) {
    const data = this.auth.getAuthFromLocalStorage();

    var _token = data.access_token;
    this.messageService.HidenMessage(_token, IdChat, IdGroup)
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
}
