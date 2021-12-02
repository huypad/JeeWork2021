import { PresenceService } from './../../_metronic/partials/layout/extras/jee-chat/my-chat/services/presence.service';
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
import { ChatService } from '../../_metronic/partials/layout/extras/jee-chat/my-chat/services/chat.service';

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
        private presence: PresenceService,
        private chatService: ChatService,
        private changeDetectorRefs: ChangeDetectorRef,
    ) {
        this.initService.init();
    }

    ngOnInit(): void {
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
        // try {
        //     this.presence.connectToken();
        // } catch (err) {
        // }
        this.subscribeToEvents();
        this.EventCloseChatboxAll();

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

        var username = '';// sau khi xác thực có username
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
        //const username = "huypad";// sau khi xác thực có username
        const host = {
            portal: 'https://portal.jee.vn',
        };

        // Thiết lập iframe đến trang đăng ký
        const iframeSource = `${host.portal}/?getstatus=true`;
        const iframe = document.createElement('iframe');
        iframe.setAttribute('src', iframeSource);
        iframe.style.display = 'none';
        document.body.appendChild(iframe);

        // Thiết lập Event Listener để xác nhận người dùng đăng ký chưa
        window.addEventListener(
            'message',
            (event) => {
                if (event.origin !== host.portal) {
                    return;
                } // Quan trọng, bảo mật, nếu không phải message từ portal thì ko làm gì cả, tránh XSS attack
                // event.data = false là user chưa đăng ký nhận thông báo, nếu đăng ký rồi thì là true
                if (event.data === false) {
                    // Đoạn setTimeout này chỉ là 1 ví dụ -> Nếu người dùng vào trang mà chưa đăng ký thì 2s sau sẽ hiện popup cho người dùng đăng ký
                    // Có thể tùy chỉnh đoạn này, thêm vào cookie, popup, button,... để tự chủ động trong việc đăng ký
                    setTimeout(() => {
                        // Lệnh window.open này chính là lệnh gọi mở popup đến trang đăng ký
                        // Trang này vừa có thể đăng ký, vừa có thể hủy đăng ký
                        // Có thể sử dụng lệnh này gán vào 1 nút nào đó trên trang cho người dùng chủ động trong việc đăng ký hoặc hủy đăng ký
                        window.open(
                            `${host.portal}/notificationsubscribe?username=${username}`, // username điền vào đây
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
