import { environment } from 'src/environments/environment';
import {
    Component,
    OnInit,
    ViewChild,
    ElementRef,
    AfterViewInit,
    HostListener,
    NgZone,
    ChangeDetectorRef,
} from '@angular/core';
import { LayoutService, LayoutInitService } from '../../_metronic/core';
import KTLayoutContent from '../../../assets/js/layout/base/content';
import { CallVideoComponent, ChatService, PresenceService } from 'lib-chat-box-dps';
import { MatDialog } from '@angular/material/dialog';
import { T } from '@angular/cdk/keycodes';

const LAYOUT_CONFIG_LOCAL_STORAGE_KEY = `${environment.appVersion}-layoutConfig`;

@Component({
    selector: 'app-layout',
    templateUrl: './layout.component.html',
    styleUrls: ['./layout.component.scss'],
})
export class LayoutComponent implements OnInit, AfterViewInit {
    // Public variables
    selfLayout = 'default';
    asideSelfDisplay: true;
    asideMenuStatic: true;
    contentClasses = '';
    contentContainerClasses = '';
    subheaderDisplay = true;
    contentExtended: false;
    asideCSSClasses: string;
    asideHTMLAttributes: any = {};
    headerMobileClasses = '';
    headerMobileAttributes = {};
    footerDisplay: boolean;
    footerCSSClasses: string;
    headerCSSClasses: string;
    headerHTMLAttributes: any = {};
    lstContact:any[];
    // offcanvases
    extrasSearchOffcanvasDisplay = false;
    extrasNotificationsOffcanvasDisplay = false;
    extrasQuickActionsOffcanvasDisplay = false;
    extrasCartOffcanvasDisplay = false;
    extrasUserOffcanvasDisplay = false;
    extrasQuickPanelDisplay = false;
    extrasScrollTopDisplay = false;
    @ViewChild('ktAside', { static: true }) ktAside: ElementRef;
    @ViewChild('ktHeaderMobile', { static: true }) ktHeaderMobile: ElementRef;
    @ViewChild('ktHeader', { static: true }) ktHeader: ElementRef;

    constructor(
        private initService: LayoutInitService,
        private layout: LayoutService,
        private _ngZone: NgZone,
        public dialog: MatDialog,
        private presence: PresenceService,
        private chatService: ChatService,
        private changeDetectorRefs: ChangeDetectorRef,
    ) {
        this.initService.init();
    }

    ngOnInit(): void {
        this.GetContact();
        setTimeout(() => {
            const userChatBox: UserChatBox[] = JSON.parse(localStorage.getItem('chatboxusers'));
            if (userChatBox) {
                this.chatBoxUsers = userChatBox;
            }
        }, 500);

        this.layout.setConfig(this.layout.getConfig());
        // build view by layout config settings
        this.selfLayout = this.layout.getProp('self.layout');
        this.asideSelfDisplay = this.layout.getProp('aside.self.display');
        this.asideMenuStatic = this.layout.getProp('aside.menu.static');
        this.subheaderDisplay = this.layout.getProp('subheader.display');
        this.contentClasses = this.layout.getStringCSSClasses('content');
        this.contentContainerClasses = this.layout.getStringCSSClasses(
            'content_container'
        );
        this.contentExtended = this.layout.getProp('content.extended');
        this.asideHTMLAttributes = this.layout.getHTMLAttributes('aside');
        this.asideCSSClasses = this.layout.getStringCSSClasses('aside');
        this.headerMobileClasses = this.layout.getStringCSSClasses('header_mobile');
        this.headerMobileAttributes = this.layout.getHTMLAttributes(
            'header_mobile'
        );
        this.footerDisplay = this.layout.getProp('footer.display');
        this.footerCSSClasses = this.layout.getStringCSSClasses('footer');
        this.headerCSSClasses = this.layout.getStringCSSClasses('header');
        this.headerHTMLAttributes = this.layout.getHTMLAttributes('header');
        // offcanvases
        if (this.layout.getProp('extras.search.display')) {
            this.extrasSearchOffcanvasDisplay =
                this.layout.getProp('extras.search.layout') === 'offcanvas';
        }

        if (this.layout.getProp('extras.notifications.display')) {
            this.extrasNotificationsOffcanvasDisplay =
                this.layout.getProp('extras.notifications.layout') === 'offcanvas';
        }

        if (this.layout.getProp('extras.quickActions.display')) {
            this.extrasQuickActionsOffcanvasDisplay =
                this.layout.getProp('extras.quickActions.layout') === 'offcanvas';
        }

        if (this.layout.getProp('extras.cart.display')) {
            this.extrasCartOffcanvasDisplay =
                this.layout.getProp('extras.cart.layout') === 'offcanvas';
        }

        if (this.layout.getProp('extras.user.display')) {
            this.extrasUserOffcanvasDisplay =
                this.layout.getProp('extras.user.layout') === 'offcanvas';
        }

        this.extrasQuickPanelDisplay = this.layout.getProp(
            'extras.quickPanel.display'
        );

        this.extrasScrollTopDisplay = this.layout.getProp(
            'extras.scrolltop.display'
        );
        try {
            this.presence.connectToken();
        } catch (err) {
        }
        this.subscribeToEvents();
        this.EventCloseChatboxAll();

        this.EventSubcibeCallVideo()
    

}
GetContact()
{
    
    
        this.chatService.GetContactChatUser().subscribe(res=>{
          this.lstContact=res.data;
     
          this.changeDetectorRefs.detectChanges();
         
        })
    
 
}
CheckCall(idGroup)
  {
 
    let index=this.lstContact.findIndex(x=>x.IdGroup==idGroup);
    if(index>=0)
    {
      return true
    }
    else{
      return false
    }
}


private EventSubcibeCallVideo(): void {

    this.presence.CallvideoMess$.subscribe(res=>
      { 
        if(res&&this.CheckCall(res.IdGroup)&&res.UserName!==this.userCurrent)
        {

          this.CallVideoDialogEvent(res.isGroup,res.UserName,res.Status,res.keyid,res.IdGroup,res.FullName,res.Avatar,res.BGcolor);

        }

      })

  }

  CallVideoDialogEvent(isGroup,username,code,key,idgroup,fullname,img,bg) {


    var dl={isGroup:isGroup,UserName:username,BG:bg,Avatar:img,PeopleNameCall:fullname,status:code,idGroup:idgroup,keyid:key,Name:fullname};
    const dialogRef = this.dialog.open(CallVideoComponent, {
  //  width:'800px',
  // height:'800px',
  data: {dl },
  disableClose: true

    });
  dialogRef.afterClosed().subscribe(res => {

          if(res)
    {
      this.presence.ClosevideoMess.next(undefined)

      this.changeDetectorRefs.detectChanges();
    }
          })

  }
    userCurrent: string;
    EventCloseChatboxAll() {
        this._ngZone.run(() => {
            this.chatService.CloseMiniChat$.subscribe(res => {

                if (res && res.UserName === this.userCurrent) {
                    this.removeChatBox(res.IdGroup);
                    this.changeDetectorRefs.detectChanges();
                }
            })
        })
    }
    ngAfterViewInit(): void {
        if (this.ktAside) {
            for (const key in this.asideHTMLAttributes) {
                if (this.asideHTMLAttributes.hasOwnProperty(key)) {
                    this.ktAside.nativeElement.attributes[key] = this.asideHTMLAttributes[
                        key
                    ];
                }
            }
        }

        if (this.ktHeaderMobile) {
            for (const key in this.headerMobileAttributes) {
                if (this.headerMobileAttributes.hasOwnProperty(key)) {
                    this.ktHeaderMobile.nativeElement.attributes[
                        key
                    ] = this.headerMobileAttributes[key];
                }
            }
        }

        if (this.ktHeader) {
            for (const key in this.headerHTMLAttributes) {
                if (this.headerHTMLAttributes.hasOwnProperty(key)) {
                    this.ktHeader.nativeElement.attributes[
                        key
                    ] = this.headerHTMLAttributes[key];
                }
            }
        }
        // Init Content
        KTLayoutContent.init('kt_content');

        var username = '';// sau khi x??c th???c c?? username
        let data = JSON.parse(localStorage.getItem('User'));
        if (data && data.Username) {
            username = data.Username;
        } else {
            setTimeout(() => {
                data = JSON.parse(localStorage.getItem('User'));
                if (data && data.Username) {
                    username = data.Username;
                }
            }, 1000);
        }
        // setup onesignal
        //const username = "huypad";// sau khi x??c th???c c?? username
        const host = {
            portal: 'https://portal.jee.vn',
        };

        // Thi???t l???p iframe ?????n trang ????ng k??
        const iframeSource = `${host.portal}/?getstatus=true`;
        const iframe = document.createElement('iframe');
        iframe.setAttribute('src', iframeSource);
        iframe.style.display = 'none';
        document.body.appendChild(iframe);

        // Thi???t l???p Event Listener ????? x??c nh???n ng?????i d??ng ????ng k?? ch??a
        window.addEventListener(
            'message',
            (event) => {
                if (event.origin !== host.portal) {
                    return;
                } // Quan tr???ng, b???o m???t, n???u kh??ng ph???i message t??? portal th?? ko l??m g?? c???, tr??nh XSS attack
                // event.data = false l?? user ch??a ????ng k?? nh???n th??ng b??o, n???u ????ng k?? r???i th?? l?? true
                if (event.data === false) {
                    // ??o???n setTimeout n??y ch??? l?? 1 v?? d??? -> N???u ng?????i d??ng v??o trang m?? ch??a ????ng k?? th?? 2s sau s??? hi???n popup cho ng?????i d??ng ????ng k??
                    // C?? th??? t??y ch???nh ??o???n n??y, th??m v??o cookie, popup, button,... ????? t??? ch??? ?????ng trong vi???c ????ng k??
                    setTimeout(() => {
                        // L???nh window.open n??y ch??nh l?? l???nh g???i m??? popup ?????n trang ????ng k??
                        // Trang n??y v???a c?? th??? ????ng k??, v???a c?? th??? h???y ????ng k??
                        // C?? th??? s??? d???ng l???nh n??y g??n v??o 1 n??t n??o ???? tr??n trang cho ng?????i d??ng ch??? ?????ng trong vi???c ????ng k?? ho???c h???y ????ng k??
                        window.open(
                            `${host.portal}/notificationsubscribe?username=${username}`, // username ??i???n v??o ????y
                            'childWin',
                            'width=400,height=400'
                        );
                    }, 2000);
                }
            },
            false
        );
    }

    @HostListener('document:message', ['$event'])
    onMessage(event) {
        // ...
    }

    // chat box
    chatBoxUsers: UserChatBox[] = [];
    usernameActived: number;


    removeChatBox(event: number) {
        let index = this.chatBoxUsers.findIndex(x => x.user.IdGroup);
        this.chatBoxUsers = this.chatBoxUsers.filter(x => x.user.IdGroup !== event);

        if (this.chatBoxUsers.length === 1 && index == 0) {
            this.chatBoxUsers[index].right = 300;
            // this.chatService.reload$.next(true);
        }
        localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
    }

    activedChatBox(event: number) {
        this.usernameActived = event;
        var u = this.chatBoxUsers.find(x => x.user.IdGroup === event);
        if (u) {
            this.chatBoxUsers = this.chatBoxUsers.filter(x => x.user.IdGroup !== event);//remove
            this.chatBoxUsers.push(u);// add to end of array
            // localStorage.setItem('chatboxusers', JSON.stringify(this.chatBoxUsers));
        }
    }

    private subscribeToEvents(): void {


        this.chatService.OpenMiniChat$.subscribe(res => {
            if (res) {
                this.chatBoxUsers.push(res);
                this.changeDetectorRefs.detectChanges();
            } else {
                this.chatBoxUsers = [];
            }
        });


    }
}


export class UserChatBox {
    user: any;
    right: number;//position

    constructor(_user, _right) {
        this.user = _user;
        this.right = _right;
    }
}
