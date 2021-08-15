import { SocketioService } from './../../../../../../modules/auth/_services/socketio.service';
import { Component, EventEmitter, OnInit, Output } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { environment } from 'src/environments/environment';
import { LayoutService } from '../../../../../core';
import * as moment from 'moment';
import { Router } from '@angular/router';
@Component({
  selector: 'app-notifications-offcanvas',
  templateUrl: './notifications-offcanvas.component.html',
  styleUrls: ['./notifications-offcanvas.component.scss'],
})
export class NotificationsOffcanvasComponent implements OnInit {
  extrasNotificationsOffcanvasDirectionCSSClass = 'offcanvas-right';
  listNoti: any = []
  @Output() loadUnreadList = new EventEmitter();
  constructor(private layout: LayoutService,
    public translate: TranslateService,
    private socketService: SocketioService,
    private router: Router,) { }

  ngOnInit(): void {
    this.extrasNotificationsOffcanvasDirectionCSSClass = `offcanvas-${this.layout.getProp(
      'extras.notifications.offcanvas.direction'
    )}`;

    this.getListNoti();
    this.socketService.connect();
    this.socketService.listen().subscribe((res: any) => {
      res.createdDate = moment(res.createdDate).format("hh:mm A - DD/MM/YYYY")
      if (this.listNoti.indexOf(res._id) < 0) {
        this.listNoti.unshift(res) //thông báo mới nhất thêm vào phía trước
        this.loadUnreadList.emit(true)
      }
    })
  }

  getListNoti() {
    this.socketService.getNotificationList('').subscribe(res => {
      res.forEach(x => {
        x.createdDate = moment(x.createdDate).format("hh:mm A - DD/MM/YYYY")
        if ((x.message_json == null || x.message_json.Content == null) && x.message_text == null) {
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
        if (x.id == noti.id) {
          x.read = true;
        }
      });
      this.getListNoti();
      if (noti.message_json.Link != null && noti.message_json.Link != "") {
        let domain = ""
        if (noti.message_json.AppCode != environment.APPCODE) {
          domain = noti.message_json.Domain
          window.open(domain + noti.message_json.Link, '_blank');
        }else{
          // this.router.navigate([noti.message_json.Link]);
          this.router.navigateByUrl(noti.message_json.Link) //có link thì chuyển link
        }
      }
      this.loadUnreadList.emit(true) 
    });
  }

  DanhDauDaXem(){
    this.socketService.ReadAll().subscribe(res => {
      this.getListNoti();
      this.loadUnreadList.emit(true)
    });
  }
}
