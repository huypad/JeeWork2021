import { WeWorkService } from './../../../../../../pages/WeWork/services/wework.services';
import { Router } from '@angular/router';
import { TokenStorage } from './../../../../../jeework_old/core/auth/_services/token-storage.service';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { LayoutService } from '../../../../../core';
import { Observable } from 'rxjs';
import { UserModel } from '../../../../../../modules/auth/_models/user.model';
import { AuthService } from '../../../../../../modules/auth/_services/auth.service';

@Component({
  selector: 'app-user-offcanvas',
  templateUrl: './user-offcanvas.component.html',
  styleUrls: ['./user-offcanvas.component.scss'],
})
export class UserOffcanvasComponent implements OnInit {
  extrasUserOffcanvasDirection = 'offcanvas-right';
  user$: Observable<UserModel>;
  _user:any = {};
  listNhacNho: any[] = [];
  AppCode: string = '';
  constructor(
    private layout: LayoutService, 
    private tokenStorage: TokenStorage, 
    private changeDetectorRefs: ChangeDetectorRef,
    private auth: AuthService,
    private weWorkService: WeWorkService,
    private router: Router,) {}

  ngOnInit(): void {
    this.extrasUserOffcanvasDirection = `offcanvas-${this.layout.getProp(
      'extras.user.offcanvas.direction'
    )}`;
    // this.user$ = this.auth.currentUserSubject.asObservable();
    // console.log(this.user$,'user')
    this.LoadData();
    
    this.weWorkService.Get_DSNhacNho().subscribe(res => {
      console.log('nhac nho',res);
      if (res && res.status == 1) {
        this.listNhacNho = res.data
        this.changeDetectorRefs.detectChanges();
      }
    })
  }

  UserData:any = {};
  LoadData(){
    // this.tokenStorage.getUserData().subscribe(res => {
		// 	this.UserData = res;
		// })
    this.user$ = this.auth.getAuthFromLocalStorage();

  }

  logout() {
    this.auth.logoutToSSO().subscribe((res) => {
      localStorage.removeItem(this.auth.authLocalStorageToken);  
      localStorage.clear(); 
      this.auth.logout();
    });
  }

  quanlytaikhoan(){
    window.open("https://jeeaccount.jee.vn/Management/AccountManagement","_blank")
  }
  ChangeLink(item){
    if(this.AppCode == item.AppCode){
      if (item.Target == "_blank") {
        window.open(item.WebAppLink, '_blank')
      } else {
        this.router.navigate([item.WebAppLink]);
      }
    }
    else{
      if (item.Target == "_blank") {
        window.open(item.Link, '_blank')
      } else {
        window.location.href = item.Link;
      }
    }
  }
}

const html = `
<!-- begin::User Panel-->
<div id="kt_quick_user" class="offcanvas p-10" [ngClass]="extrasUserOffcanvasDirection">
  <ng-container *ngIf="user$ | async as _user">
    <!--begin::Header-->
    <div class="offcanvas-header d-flex align-items-center justify-content-between pb-5">
      <h3 class="font-weight-bold m-0">
        User Profile
        <small class="text-muted font-size-sm ml-2">12 messages</small>
      </h3>
      <a href="#" class="btn btn-xs btn-icon btn-light btn-hover-primary" id="kt_quick_user_close">
        <i class="ki ki-close icon-xs text-muted"></i>
      </a>
    </div>
    <!--end::Header-->

    <!--begin::Content-->
    <div class="offcanvas-content pr-5 mr-n5">
      <!--begin::Header-->
      <div class="d-flex align-items-center mt-5">
        <div class="symbol symbol-100 mr-5">
          <div class="symbol-label" style="background-image: url('./assets/media/users/300_21.jpg');"></div>
          <i class="symbol-badge bg-success"></i>
        </div>
        <div class="d-flex flex-column">
          <a href="#" class="font-weight-bold font-size-h5 text-dark-75 text-hover-primary">
            {{ _user.firstname }} {{ _user.lastname }}
          </a>
          <div class="text-muted mt-1">
            Application Developer
          </div>
          <div class="navi mt-2">
            <a href="#" class="navi-item">
              <span class="navi-link p-0 pb-2">
                <span class="navi-icon mr-1">
                  <span [inlineSVG]="
                      './assets/media/svg/icons/Communication/Mail-notification.svg'
                    " cacheSVG="true" class="svg-icon svg-icon-lg svg-icon-primary"></span>
                </span>
                <span class="navi-text text-muted text-hover-primary">{{
                  _user.email
                }}</span>
              </span>
            </a>

            <a class="btn btn-sm btn-light-primary font-weight-bolder py-2 px-5 cursor-pointer" (click)="logout()">Sign
              Out</a>
          </div>
        </div>
      </div>
      <!--end::Header-->

      <!--begin::Separator-->
      <div class="separator separator-dashed mt-8 mb-5"></div>
      <!--end::Separator-->

      <!--begin::Nav-->
      <div class="navi navi-spacer-x-0 p-0">
        <!--begin::Item-->
        <a class="navi-item cursor-pointer" routerLink="/user-profile">
          <div class="navi-link">
            <div class="symbol symbol-40 bg-light mr-3">
              <div class="symbol-label">
                <span [inlineSVG]="
                    './assets/media/svg/icons/General/Notification2.svg'
                  " cacheSVG="true" class="svg-icon svg-icon-md svg-icon-success"></span>
              </div>
            </div>
            <div class="navi-text">
              <div class="font-weight-bold">
                My Profile
              </div>
              <div class="text-muted">
                Account settings and more
                <span class="label label-light-danger label-inline font-weight-bold">update</span>
              </div>
            </div>
          </div>
        </a>
        <!--end:Item-->

        <!--begin::Item-->
        <a class="navi-item cursor-pointer">
          <div class="navi-link">
            <div class="symbol symbol-40 bg-light mr-3">
              <div class="symbol-label">
                <span [inlineSVG]="
                    './assets/media/svg/icons/Shopping/Chart-bar1.svg'
                  " cacheSVG="true" class="svg-icon svg-icon-md svg-icon-warning"></span>
              </div>
            </div>
            <div class="navi-text">
              <div class="font-weight-bold">
                My Messages
              </div>
              <div class="text-muted">
                Inbox and tasks
              </div>
            </div>
          </div>
        </a>
        <!--end:Item-->

        <!--begin::Item-->
        <a class="navi-item cursor-pointer">
          <div class="navi-link">
            <div class="symbol symbol-40 bg-light mr-3">
              <div class="symbol-label">
                <span [inlineSVG]="
                    './assets/media/svg/icons/Files/Selected-file.svg'
                  " cacheSVG="true" class="svg-icon svg-icon-md svg-icon-danger"></span>
              </div>
            </div>
            <div class="navi-text">
              <div class="font-weight-bold">
                My Activities
              </div>
              <div class="text-muted">
                Logs and notifications
              </div>
            </div>
          </div>
        </a>
        <!--end:Item-->

        <!--begin::Item-->
        <a class="navi-item cursor-pointer">
          <div class="navi-link">
            <div class="symbol symbol-40 bg-light mr-3">
              <div class="symbol-label">
                <span [inlineSVG]="
                    './assets/media/svg/icons/Communication/Mail-opened.svg'
                  " cacheSVG="true" class="svg-icon svg-icon-md svg-icon-primary"></span>
              </div>
            </div>
            <div class="navi-text">
              <div class="font-weight-bold">
                My Tasks
              </div>
              <div class="text-muted">
                latest tasks and projects
              </div>
            </div>
          </div>
        </a>
        <!--end:Item-->
      </div>
      <!--end::Nav-->

      <!--begin::Separator-->
      <div class="separator separator-dashed my-7"></div>
      <!--end::Separator-->

      <!--begin::Notifications-->
      <div>
        <!--begin:Heading-->
        <h5 class="mb-5">
          Recent Notifications
        </h5>
        <!--end:Heading-->

        <!--begin::Item-->
        <div class="d-flex align-items-center bg-light-warning rounded p-5 gutter-b">
          <span class="svg-icon svg-icon-warning mr-5">
            <span [inlineSVG]="'./assets/media/svg/icons/Home/Library.svg'" cacheSVG="true"
              class="svg-icon svg-icon-lg"></span>
          </span>

          <div class="d-flex flex-column flex-grow-1 mr-2">
            <a href="#" class="font-weight-normal text-dark-75 text-hover-primary font-size-lg mb-1">Another purpose
              persuade</a>
            <span class="text-muted font-size-sm">Due in 2 Days</span>
          </div>

          <span class="font-weight-bolder text-warning py-1 font-size-lg">+28%</span>
        </div>
        <!--end::Item-->

        <!--begin::Item-->
        <div class="d-flex align-items-center bg-light-success rounded p-5 gutter-b">
          <span class="svg-icon svg-icon-success mr-5">
            <span [inlineSVG]="'./assets/media/svg/icons/Communication/Write.svg'" cacheSVG="true"
              class="svg-icon svg-icon-lg"></span>
          </span>
          <div class="d-flex flex-column flex-grow-1 mr-2">
            <a href="#" class="font-weight-normal text-dark-75 text-hover-primary font-size-lg mb-1">Would be to
              people</a>
            <span class="text-muted font-size-sm">Due in 2 Days</span>
          </div>

          <span class="font-weight-bolder text-success py-1 font-size-lg">+50%</span>
        </div>
        <!--end::Item-->

        <!--begin::Item-->
        <div class="d-flex align-items-center bg-light-danger rounded p-5 gutter-b">
          <span class="svg-icon svg-icon-danger mr-5">
            <span [inlineSVG]="
                './assets/media/svg/icons/Communication/Group-chat.svg'
              " cacheSVG="true" class="svg-icon svg-icon-lg"></span>
          </span>
          <div class="d-flex flex-column flex-grow-1 mr-2">
            <a href="#" class="font-weight-normel text-dark-75 text-hover-primary font-size-lg mb-1">Purpose would be to
              persuade</a>
            <span class="text-muted font-size-sm">Due in 2 Days</span>
          </div>

          <span class="font-weight-bolder text-danger py-1 font-size-lg">-27%</span>
        </div>
        <!--end::Item-->

        <!--begin::Item-->
        <div class="d-flex align-items-center bg-light-info rounded p-5">
          <span class="svg-icon svg-icon-info mr-5">
            <span [inlineSVG]="'./assets/media/svg/icons/General/Attachment2.svg'" cacheSVG="true"
              class="svg-icon svg-icon-lg"></span>
          </span>

          <div class="d-flex flex-column flex-grow-1 mr-2">
            <a href="#" class="font-weight-normel text-dark-75 text-hover-primary font-size-lg mb-1">The best
              product</a>
            <span class="text-muted font-size-sm">Due in 2 Days</span>
          </div>

          <span class="font-weight-bolder text-info py-1 font-size-lg">+8%</span>
        </div>
        <!--end::Item-->
      </div>
      <!--end::Notifications-->
    </div>
    <!--end::Content-->
  </ng-container>
</div>
<!-- end::User Panel-->
`;