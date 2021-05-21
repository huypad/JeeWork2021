import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { LayoutConfigService } from './../../../../_metronic/jeework_old/core/_base/layout/services/layout-config.service';
import { Component, OnInit, AfterViewInit, ChangeDetectorRef } from '@angular/core';
import { Observable } from 'rxjs';
import { LayoutService } from '../../../../_metronic/core';
import { AuthService } from '../../../../modules/auth/_services/auth.service';
import { UserModel } from '../../../../modules/auth/_models/user.model';
import KTLayoutQuickSearch from '../../../../../assets/js/layout/extended/quick-search';
import KTLayoutQuickNotifications from '../../../../../assets/js/layout/extended/quick-notifications';
import KTLayoutQuickActions from '../../../../../assets/js/layout/extended/quick-actions';
import KTLayoutQuickCartPanel from '../../../../../assets/js/layout/extended/quick-cart';
import KTLayoutQuickPanel from '../../../../../assets/js/layout/extended/quick-panel';
import KTLayoutQuickUser from '../../../../../assets/js/layout/extended/quick-user';
import KTLayoutHeaderTopbar from '../../../../../assets/js/layout/base/header-topbar';
import { KTUtil } from '../../../../../assets/js/components/util';
import objectPath from 'object-path';
import { SocketioService } from 'src/app/modules/auth/_services/socketio.service';

@Component({
  selector: 'app-topbar',
  templateUrl: './topbar.component.html',
  styleUrls: ['./topbar.component.scss'],
})
export class TopbarComponent implements OnInit, AfterViewInit {
  user$: Observable<UserModel>;
  // tobbar extras
  extraSearchDisplay: boolean;
  extrasSearchLayout: 'offcanvas' | 'dropdown';
  extrasNotificationsDisplay: boolean;
  extrasNotificationsLayout: 'offcanvas' | 'dropdown';
  extrasQuickActionsDisplay: boolean;
  extrasQuickActionsLayout: 'offcanvas' | 'dropdown';
  extrasCartDisplay: boolean;
  extrasCartLayout: 'offcanvas' | 'dropdown';
  extrasQuickPanelDisplay: boolean;
  extrasLanguagesDisplay: boolean;
  extrasUserDisplay: boolean;
  extrasUserLayout: 'offcanvas' | 'dropdown';
  numberInfo: number;
  desktopHeaderDisplay: boolean;

  constructor(
    private layout: LayoutService, private auth: AuthService,
    private layoutConfigService: LayoutConfigService,
    private tokenStorage: TokenStorage,
    private changeDetectorRefs: ChangeDetectorRef,
    private socketService: SocketioService,
    ) {
    this.user$ = this.auth.currentUserSubject.asObservable();
  }

  ngOnInit(): void {
    // topbar extras
    this.extraSearchDisplay = this.layout.getProp('extras.search.display');
    this.extrasSearchLayout = this.layout.getProp('extras.search.layout');
    this.extrasNotificationsDisplay = this.layout.getProp(
      'extras.notifications.display'
    );
    this.extrasNotificationsLayout = this.layout.getProp(
      'extras.notifications.layout'
    );
    this.extrasQuickActionsDisplay = this.layout.getProp(
      'extras.quickActions.display'
    );
    this.extrasQuickActionsLayout = this.layout.getProp(
      'extras.quickActions.layout'
    );
    this.extrasCartDisplay = this.layout.getProp('extras.cart.display');
    this.extrasCartLayout = this.layout.getProp('extras.cart.layout');
    this.extrasLanguagesDisplay = this.layout.getProp(
      'extras.languages.display'
    );
    this.extrasUserDisplay = this.layout.getProp('extras.user.display');
    this.extrasUserLayout = this.layout.getProp('extras.user.layout');
    this.extrasQuickPanelDisplay = this.layout.getProp(
      'extras.quickPanel.display'
    );


    const config = this.layout.getConfig();
		this.desktopHeaderDisplay = objectPath.get(config, 'header.self.fixed.desktop');
		if (!this.desktopHeaderDisplay) {
		}

    this.LoadData();
  }

  UserData:any = {};
  LoadData(){
    this.tokenStorage.getUserData().subscribe(res => {
			this.UserData = res;
		})
  }
  updateNumberNoti(value) {
    if(value == true) {
      this.getNotiUnread() 
    }
  }

getNotiUnread() {
	   this.socketService.getNotificationList('unread').subscribe( res => {
      let dem = 0;
      res.forEach(x => dem++);
      this.numberInfo = dem;
      this.changeDetectorRefs.detectChanges();
    })
  }
  ngAfterViewInit(): void {
    KTUtil.ready(() => {
      // Called after ngAfterContentInit when the component's view has been initialized. Applies to components only.
      // Add 'implements AfterViewInit' to the class.
      if (this.extraSearchDisplay && this.extrasSearchLayout === 'offcanvas') {
        KTLayoutQuickSearch.init('kt_quick_search');
      }

      if (
        this.extrasNotificationsDisplay &&
        this.extrasNotificationsLayout === 'offcanvas'
      ) {
        // Init Quick Notifications Offcanvas Panel
        KTLayoutQuickNotifications.init('kt_quick_notifications');
      }

      if (
        this.extrasQuickActionsDisplay &&
        this.extrasQuickActionsLayout === 'offcanvas'
      ) {
        // Init Quick Actions Offcanvas Panel
        KTLayoutQuickActions.init('kt_quick_actions');
      }

      if (this.extrasCartDisplay && this.extrasCartLayout === 'offcanvas') {
        // Init Quick Cart Panel
        KTLayoutQuickCartPanel.init('kt_quick_cart');
      }

      if (this.extrasQuickPanelDisplay) {
        // Init Quick Offcanvas Panel
        KTLayoutQuickPanel.init('kt_quick_panel');
      }

      if (this.extrasUserDisplay && this.extrasUserLayout === 'offcanvas') {
        // Init Quick User Panel
        KTLayoutQuickUser.init('kt_quick_user');
      }

      // Init Header Topbar For Mobile Mode
      KTLayoutHeaderTopbar.init('kt_header_mobile_topbar_toggle');
    });
  }
}

//================================ Code html file demo chưa xóa để tạm ở dưới đây
const html = `
<!--begin::Topbar-->
<ng-container *ngIf="extraSearchDisplay">
  <ng-container *ngIf="extrasSearchLayout === 'offcanvas'">
    <!--begin::Search-->
    <div class="topbar-item">
      <div class="btn btn-icon btn-clean btn-lg mr-1" id="kt_quick_search_toggle">
        <span [inlineSVG]="'./assets/media/svg/icons/General/Search.svg'" cacheSVG="true"
          class="svg-icon svg-icon-xl svg-icon-primary"></span>
      </div>
    </div>
    <!--end::Search-->
  </ng-container>

  <ng-container *ngIf="extrasSearchLayout === 'dropdown'" id="kt_quick_search_toggle">
    <div class="dropdown" id="kt_quick_search_toggle" autoClose="outside" ngbDropdown>
      <!--begin::Toggle-->
      <div class="topbar-item" ngbDropdownToggle>
        <div class="btn btn-icon btn-clean btn-lg btn-dropdown mr-1">
          <span [inlineSVG]="'./assets/media/svg/icons/General/Search.svg'" cacheSVG="true"
            class="svg-icon svg-icon-xl svg-icon-primary"></span>
        </div>
      </div>
      <!--end::Toggle-->

      <!--begin::Dropdown-->
      <div class="dropdown-menu p-0 m-0 dropdown-menu-right dropdown-menu-anim-up dropdown-menu-lg" ngbDropdownMenu>
        <app-search-dropdown-inner></app-search-dropdown-inner>
      </div>
      <!--end::Dropdown-->
    </div>
  </ng-container>
</ng-container>

<ng-container *ngIf="extrasNotificationsDisplay">
  <ng-container *ngIf="extrasNotificationsLayout === 'offcanvas'">
    <!--begin::Notifications-->
    <div class="topbar-item">
      <div class="btn btn-icon btn-icon-mobile btn-clean btn-lg mr-1 pulse pulse-primary"
        id="kt_quick_notifications_toggle">
        <span [inlineSVG]="'./assets/media/svg/icons/Code/Compiling.svg'" cacheSVG="true"
          class="svg-icon svg-icon-xl svg-icon-primary"></span>
        <span class="pulse-ring"></span>
      </div>
    </div>
    <!--end::Notifications-->
  </ng-container>

  <ng-container *ngIf="extrasNotificationsLayout === 'dropdown'">
    <!--begin::Notifications-->
    <div class="dropdown" ngbDropdown placement="bottom">
      <!--begin::Toggle-->
      <div class="topbar-item" data-toggle="dropdown" data-offset="10px,0px" ngbDropdownToggle>
        <div class="btn btn-icon btn-clean btn-dropdown btn-lg mr-1 pulse pulse-primary">
          <span [inlineSVG]="'./assets/media/svg/icons/Code/Compiling.svg'" cacheSVG="true"
            class="svg-icon svg-icon-xl svg-icon-primary"></span>
          <span class="pulse-ring"></span>
        </div>
      </div>
      <!--end::Toggle-->

      <!--begin::Dropdown-->
      <div ngbDropdownMenu class="dropdown-menu p-0 m-0 dropdown-menu-anim-up dropdown-menu-lg">
        <form>
          <app-notifications-dropdown-inner></app-notifications-dropdown-inner>
        </form>
      </div>
      <!--end::Dropdown-->
    </div>
    <!--end::Notifications-->
  </ng-container>
</ng-container>

<ng-container *ngIf="extrasQuickActionsDisplay">
  <ng-container *ngIf="extrasQuickActionsLayout === 'offcanvas'">
    <!--begin::Quick Actions-->
    <div class="topbar-item">
      <div class="btn btn-icon btn-clean btn-dropdown btn-lg mr-1" id="kt_quick_actions_toggle">
        <span [inlineSVG]="'./assets/media/svg/icons/Media/Equalizer.svg'" cacheSVG="true"
          class="svg-icon svg-icon-xl svg-icon-primary"></span>
      </div>
    </div>
    <!--end::Quick Actions-->
  </ng-container>
  <ng-container *ngIf="extrasQuickActionsLayout === 'dropdown'">
    <!--begin::Quick Actions-->
    <div class="dropdown" ngbDropdown placement="bottom">
      <!--begin::Toggle-->
      <div class="topbar-item" data-toggle="dropdown" data-offset="10px,0px" ngbDropdownToggle>
        <div class="btn btn-icon btn-clean btn-dropdown btn-lg mr-1">
          <span [inlineSVG]="'./assets/media/svg/icons/Media/Equalizer.svg'" cacheSVG="true"
            class="svg-icon svg-icon-xl svg-icon-primary"></span>
        </div>
      </div>
      <!--end::Toggle-->
      <!--begin::Dropdown-->
      <div class="dropdown-menu p-0 m-0 dropdown-menu-anim-up dropdown-menu-lg" ngbDropdownMenu>
        <app-quick-actions-dropdown-inner></app-quick-actions-dropdown-inner>
      </div>
      <!--end::Dropdown-->
    </div>
    <!--end::Quick Actions-->
  </ng-container>
</ng-container>

<ng-container *ngIf="extrasCartDisplay">
  <ng-container *ngIf="extrasCartLayout === 'offcanvas'">
    <!--begin::Cart-->
    <div class="topbar-item">
      <div class="btn btn-icon btn-clean btn-dropdown btn-lg mr-1" id="kt_quick_cart_toggle">
        <span [inlineSVG]="'./assets/media/svg/icons/Shopping/Cart3.svg'" cacheSVG="true"
          class="svg-icon svg-icon-xl svg-icon-primary"></span>
      </div>
    </div>
    <!--end::Cart-->
  </ng-container>
  <ng-container *ngIf="extrasCartLayout === 'dropdown'">
    <!--begin::Cart-->
    <div class="dropdown" ngbDropdown placement="bottom">
      <!--begin::Toggle-->
      <div class="topbar-item" data-toggle="dropdown" data-offset="10px,0px" ngbDropdownToggle>
        <div class="btn btn-icon btn-clean btn-dropdown btn-lg mr-1">
          <span [inlineSVG]="'./assets/media/svg/icons/Shopping/Cart3.svg'" cacheSVG="true"
            class="svg-icon svg-icon-xl svg-icon-primary"></span>
        </div>
      </div>
      <!--end::Toggle-->
      <!--begin::Dropdown-->
      <div ngbDropdownMenu class="dropdown-menu p-0 m-0 dropdown-menu-xl dropdown-menu-anim-up">
        <form>
          <app-cart-dropdown-inner></app-cart-dropdown-inner>
        </form>
      </div>
      <!--end::Dropdown-->
    </div>
    <!--end::Cart-->
  </ng-container>
</ng-container>

<ng-container *ngIf="extrasQuickPanelDisplay">
  <!--begin::Quick panel-->
  <div class="topbar-item">
    <div class="btn btn-icon btn-clean btn-lg mr-1" id="kt_quick_panel_toggle">
      <span [inlineSVG]="'./assets/media/svg/icons/Layout/Layout-4-blocks.svg'" cacheSVG="true"
        class="svg-icon svg-icon-xl svg-icon-primary"></span>
    </div>
  </div>
  <!--end::Quick panel-->
</ng-container>

<ng-container *ngIf="extrasLanguagesDisplay">
  <app-language-selector style="margin-top: 10px;"></app-language-selector>
</ng-container>

<ng-container *ngIf="extrasUserDisplay">
  <ng-container *ngIf="extrasUserLayout === 'offcanvas'">
    <ng-container *ngIf="user$ | async as _user">
      <!--begin::User-->
      <div class="topbar-item">
        <div class="btn btn-icon btn-icon-mobile w-auto btn-clean d-flex align-items-center btn-lg px-2"
          id="kt_quick_user_toggle">
          <span class="text-muted font-weight-bold font-size-base d-none d-md-inline mr-1">Hi,</span>
          <span
            class="text-dark-50 font-weight-bolder font-size-base d-none d-md-inline mr-3">{{ _user.fullname }}</span>
          <span class="symbol symbol-lg-35 symbol-25 symbol-light-success">
            <span class="symbol-label font-size-h5 font-weight-bold">{{
              _user.fullname[0]
            }}</span>
          </span>
        </div>
      </div>
      <!--end::User-->
    </ng-container>
  </ng-container>

  <ng-container *ngIf="extrasUserLayout === 'dropdown'">
    <!--begin::User-->
    <ng-container *ngIf="user$ | async as _user">
      <div class="dropdown" ngbDropdown placement="bottom-right">
        <div class="topbar-item" data-toggle="dropdown" data-offset="0px,0px" ngbDropdownToggle>
          <div class="btn btn-icon w-auto btn-clean d-flex align-items-center btn-lg px-2">
            <span class="text-muted font-weight-bold font-size-base d-none d-md-inline mr-1">Hi,</span>
            <span
              class="text-dark-50 font-weight-bolder font-size-base d-none d-md-inline mr-3">{{ _user.fullname }}</span>
            <span class="symbol symbol-35 symbol-light-success">
              <span class="symbol-label font-size-h5 font-weight-bold">{{
                _user.fullname | firstLetter
              }}</span>
            </span>
          </div>
        </div>
        <!--end::Toggle-->
        <!--begin::Dropdown-->
        <div ngbDropdownMenu
          class="dropdown-menu p-0 m-0 dropdown-menu-right dropdown-menu-anim-up dropdown-menu-lg p-0">
          <app-user-dropdown-inner></app-user-dropdown-inner>
        </div>
      </div>
      <!--end::Dropdown-->
    </ng-container>
    <!--end::User-->
  </ng-container>
</ng-container>

<!--end::Topbar-->
`;
