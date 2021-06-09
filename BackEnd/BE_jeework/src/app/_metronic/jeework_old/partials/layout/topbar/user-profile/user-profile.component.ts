import { AuthService } from "./../../../../../../modules/auth/_services/auth.service";
import { LayoutService } from "./../../../../../core/services/layout.service";
import { TokenStorage } from "./../../../../core/auth/_services/token-storage.service";
import { UserProfileService } from "./../../../../core/auth/_services/user-profile.service";
import { MenuConfigService } from "./../../../../core/_base/layout/services/menu-config.service";
import { AuthenticationService } from "./../../../../core/auth/_services/auth.service";
import { LayoutConfigService } from "./../../../../core/_base/layout/services/layout-config.service";
import { GlobalVariable } from "./../../../../../../pages/global";
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../core/utils/layout-utils.service";
import { environment } from "./../../../../../../../environments/environment";
import { User } from "./../../../../core/auth/_models/user.model";
import { AppState } from "./../../../../core/reducers/index";

// Angular
import {
  Component,
  Input,
  OnInit,
  Inject,
  ViewChild,
  ElementRef,
  ChangeDetectorRef,
  HostBinding,
  HostListener,
} from "@angular/core";
// RxJS
import { Observable } from "rxjs";
// NGRX
// State
import { Router, ActivatedRoute } from "@angular/router";
import { DOCUMENT } from "@angular/common";
import { TranslateService } from "@ngx-translate/core";
import { CookieService } from "ngx-cookie-service";
import objectPath from "object-path";
import {
  MatDialog,
  MatDialogRef,
  MAT_DIALOG_DATA,
} from "@angular/material/dialog";
const Module = "" + environment.MODULE;
var swRegistration: any = null;

@Component({
  selector: "kt-user-profile",
  templateUrl: "./user-profile.component.html",
})
export class UserProfileComponent implements OnInit {
  @HostBinding("class")
  // tslint:disable-next-line:max-line-length
  classes =
    "kt-nav__item m-topbar__user-profile kt-topbar__user-profile--img kt-dropdown m-dropdown--medium kt-dropdown--arrow kt-dropdown--header-bg-fill kt-dropdown--align-right kt-dropdown--mobile-full-width kt-dropdown--skin-light";
  @ViewChild("ktProfileDropdown", { static: true })
  mProfileDropdown: ElementRef;
  @HostBinding("attr.kt-dropdown-toggle") attrDropdownToggle = "click";
  // Public properties
  user$: Observable<User>;
  user2: User = new User();

  @Input() avatar: boolean = true;
  @Input() greeting: boolean = true;
  @Input() badge: boolean;
  @Input() icon: boolean;

  listModule: any[] = [];
  desktopHeaderDisplay: boolean;
  TIMEOUTAPI: number = 3000;

  dataDialog: any = { newPass: "", oldPass: "", name: "", confirm: "" };

  /**
   * Component constructor
   *
   * @param store: Store<AppState>
   */
  private _sessionId: string;
  constructor(
    private layout: LayoutService,
    private tokenStore: TokenStorage,
    private authService: AuthenticationService,
    private auth: AuthService,
    private userProfileService: UserProfileService,
    @Inject(DOCUMENT) private document: Document,
    private changeDetectorRefs: ChangeDetectorRef,
    public dialog: MatDialog,
    private cookieService: CookieService,
    private menuConfigService: MenuConfigService // private dungchungservice: DungChungServices,
  ) {}

  /**
   * @ Lifecycle sequences => https://angular.io/guide/lifecycle-hooks
   */

  /**
   * On init
   */
  ngOnInit(): void {
    const config = this.layout.getConfig();
    this.desktopHeaderDisplay = objectPath.get(
      config,
      "header.self.fixed.desktop"
    );
    if (!this.desktopHeaderDisplay) {
    }
    this.permissionNof();
    this.checkSession();
    this.loadLoGoKhachHang();
    this.GetAllRoles();
    this.userProfileService.getDictionary().subscribe((res) => {
      if (res && res.status == 1) {
        res.data.emotions.map((x) => {
          GlobalVariable.emotions[x.key] = x.value;
        });
        res.data.accounts.map((x) => {
          GlobalVariable.accounts[x.key] = x.value;
        });
        GlobalVariable.icons = res.data.icons;
      }
    });
  }

  GetAllRoles() {
    this.menuConfigService.GetRole_WeWork(localStorage.getItem("Username"));
    console.log(
      "Username",
      this.menuConfigService.GetRole_WeWork(localStorage.getItem("Username"))
    );
  }
  checkSession() {
    try {
      this._sessionId = this.cookieService.get("_sessionId");
      if (this._sessionId != "") {
        this.tokenStore
          .setAccessToken(this._sessionId)
          .setRefreshToken(this._sessionId);
      } else {
        var _token = "";
        this.tokenStore.getAccessToken().subscribe((t) => {
          _token = t;
        });
      }
    } catch (ex) {
      // this.logout();
    }
  }

  // public logout(url?: string) {
  //   this.authService.logout(true, url);
  // }

  loadLoGoKhachHang() {}

  item: any;
  @Input() avatarr: string = "./assets/app/media/img/users/user4.jpg";
  @ViewChild("avatar1", { static: true }) avatar1: ElementRef;
  @ViewChild("avatar2", { static: true }) avatar2: ElementRef;
  Ten: string = "";
  ChucVu: string = "";
  loadThongTinUser() {
    let id: any;
    this.tokenStore.getIDUser().subscribe((res) => {
      id = +res;
    });
  }
  logoutJWT() {
    this.auth.logoutToSSO().subscribe((res) => {
      localStorage.clear();
      this.auth.logout();
    });
  }
  permissionNof() {
    if (Notification && Notification.permission !== "granted") {
      Notification.requestPermission(function (status) {
        if (Notification.permission !== status) {
          //Notification.permission = status;
        }
      });
    }
    // if ("serviceWorker" in navigator && "PushManager" in window) {
    //   var auth = this.authService;
    //   navigator.serviceWorker
    //     .register("sw.js")
    //     .then(function (swReg) {
    //       swRegistration = swReg;
    //       auth.CreateFCM().subscribe((res) => {
    //         if (res && res.status == 1) {
    //           var token = localStorage.getItem("PublicKey");
    //           if (token != res.data) {
    //             //token mới
    //             localStorage.setItem("PublicKey", res.data);
    //             swReg.active.postMessage(
    //               JSON.stringify({
    //                 Token: localStorage.getItem("accessToken"),
    //                 ApiUrl: environment.ApiRoot,
    //                 applicationServerPublicKey: res.data,
    //               })
    //             );
    //           }
    //         }
    //       });
    //     })
    //     .catch(function (error) {
    //       console.error("Service Worker Error", error);
    //     });
    // } else {
    //   console.warn("Service Worker and Push is not supported");
    // }
  }
  getRandomColor() {
    var color = Math.floor(0x1000000 * Math.random()).toString(16);
    return "#" + ("000000" + color).slice(-6);
  }

  openDialog() {
    const dialogRef = this.dialog.open(DialogOverviewExampleDialog, {
      width: "30%",
      data: this.dataDialog,
    });
    dialogRef.afterClosed().subscribe((result) => {
      this.dataDialog.oldPass = "";
      this.dataDialog.newPass = "";
      this.dataDialog.confirm = "";
    });
  }
}

@Component({
  selector: "dialog-overview-example-dialog",
  templateUrl: "dialog-overview-example-dialog.html",
})
export class DialogOverviewExampleDialog {
  public dis: boolean = false;
  yeucaudomanhmatkhau: string;
  id_user: string = "";
  oldPass: string = "";
  newPass: string = "";
  confirm: string = "";
  constructor(
    public dialogRef: MatDialogRef<DialogOverviewExampleDialog>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private layoutService: LayoutConfigService,
    private changeDetectorRefs: ChangeDetectorRef,
    private tokenStore: TokenStorage,
    private layoutUtilsService: LayoutUtilsService
  ) {}
  ngOnInit(): void {
    this.checkPass();
    this.tokenStore.getIDUser().subscribe((res) => {
      this.id_user = res;
    });
    this.layoutService.findYeuCauDoManhPassWord().subscribe((res) => {
      if (res && res.status === 1) {
        let rs = res;
        this.yeucaudomanhmatkhau = rs.data;
        this.changeDetectorRefs.detectChanges();
      }
    });
  }

  checkPass() {
    this.dis = this.data.confirm != this.data.newPass ? true : false;
    if (this.data.oldPass == "" || this.data.oldPass == undefined)
      this.dis = true;
  }
  goBack() {
    this.dialogRef.close();
  }

  DoiMatKhau() {
    let model = {
      Id: this.id_user,
      OldPassword: this.data.oldPass,
      NewPassword: this.data.newPass,
      RePassword: this.data.confirm,
    };
    this.layoutService.changePass(model).subscribe((res) => {
      if (res != undefined && res.status == 1) {
        this.layoutUtilsService.showActionNotification(
          "Đổi mật khẩu thành công",
          MessageType.Update,
          10000,
          true,
          false
        );
        this.dialogRef.close();
      } else {
        this.layoutUtilsService.showActionNotification(
          res.error.message,
          MessageType.Read,
          999999999,
          true,
          false,
          0,
          "top",
          0
        );
      }
    });
  }

  @HostListener("document:keydown", ["$event"])
  onKeydownHandler(event: KeyboardEvent) {
    if (event.keyCode == 13) {
      //phím Enter
      if (!this.dis) {
        this.DoiMatKhau();
      }
    }
  }
}
