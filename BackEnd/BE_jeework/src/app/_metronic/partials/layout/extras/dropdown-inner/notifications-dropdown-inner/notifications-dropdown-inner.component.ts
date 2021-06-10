import { environment } from 'src/environments/environment';
import { Router } from '@angular/router';
import { TranslateService } from '@ngx-translate/core';
import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { LayoutService } from '../../../../../core';
import { SocketioService } from 'src/app/modules/auth/_services/socketio.service';
import * as moment from 'moment';

@Component({
  selector: 'app-notifications-dropdown-inner',
  templateUrl: './notifications-dropdown-inner.component.html',
  styleUrls: ['./notifications-dropdown-inner.component.scss'],
})
export class NotificationsDropdownInnerComponent implements OnInit {
  extrasNotificationsDropdownStyle: 'light' | 'dark' = 'dark';
  activeTabId:
    | 'topbar_notifications_notifications'
    | 'topbar_notifications_events'
    | 'topbar_notifications_logs' = 'topbar_notifications_notifications';
  constructor(
    private layout: LayoutService, 
    public translate: TranslateService,
    private socketService: SocketioService,
    private router: Router,
  ) {}

  @Output() loadUnreadList = new EventEmitter();

  listNoti: any=[]
  ngOnInit(): void {
    this.extrasNotificationsDropdownStyle = this.layout.getProp(
      'extras.notifications.dropdown.style'
    );

    this.getListNoti();
    this.socketService.connect();
    this.socketService.listen().subscribe( (res:any) => {
      res.createdDate = moment(res.createdDate).format("hh:mm A - DD/MM/YYYY")
      if(this.listNoti.indexOf(res._id) < 0) {
        this.listNoti.unshift(res) //thông báo mới nhất thêm vào phía trước
        this.loadUnreadList.emit(true)
      }
    })
  }

  setActiveTabId(tabId) {
    this.activeTabId = tabId;
  }

  getActiveCSSClasses(tabId) {
    if (tabId !== this.activeTabId) {
      return '';
    }
    return 'active show';
  }

  getListNoti() {
    this.socketService.getNotificationList('').subscribe( res => {
      res.forEach(x => {
          x.createdDate = moment(x.createdDate).format("hh:mm A - DD/MM/YYYY")
          if((x.message_json == null || x.message_json.Content == null) && x.message_text == null) {
              x.message_text = "Thông báo không có nội dung"
          }
      });
      this.listNoti = res;
      this.loadUnreadList.emit(true) //load thành công list load số thông báo chưa đọc
    })
  }

	clickRead(noti: any) {
    this.socketService.readNotification(noti._id).subscribe(res => {
			this.listNoti.forEach(x => {
        if(x.id == noti.id){
          x.read = true;
        }
      });
      this.getListNoti();
      if(noti.message_json.Link != null && noti.message_json.Link != "") {
        if(noti.message_json.AppCode == "WORK") {
          this.router.navigateByUrl(noti.message_json.Link) //có link thì chuyển link
        }
        else { //các link không nằm trong app
          let domain = ""
          // if(noti.message_json.AppCode == "REQ") {
          //   domain = environment.linkREQ
          // }
          if(noti.message_json.AppCode == "ACC") {
            domain = environment.LINKACCOUNT+'/'
          }
          window.open(domain + noti.message_json.Link, '_blank');
        }
      }
      this.loadUnreadList.emit(true)
		});
  }
}