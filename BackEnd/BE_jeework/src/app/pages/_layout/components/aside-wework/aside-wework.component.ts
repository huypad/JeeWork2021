import { WeWorkService } from './../../../WeWork/services/wework.services';
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { ProjectTeamEditStatusComponent } from "./../../../WeWork/projects-team/project-team-edit-status/project-team-edit-status.component";
import { AddStatusComponent } from "./../../../WeWork/projects-team/work-list-new/add-status/add-status.component";
import { DepartmentEditNewComponent } from "./../../../WeWork/List-department/department-edit-new/department-edit-new.component";
import { DepartmentModel } from "./../../../WeWork/List-department/Model/List-department.model";
import { UpdateStatusProjectComponent } from "./../../../WeWork/projects-team/update-status-project/update-status-project.component";
import { DuplicateProjectComponent } from "./../../../WeWork/projects-team/duplicate-project/duplicate-project.component";
import { ProjectTeamDuplicateModel } from "./../../../WeWork/projects-team/Model/department-and-project.model";
import { ClosedProjectComponent } from "./../../../WeWork/projects-team/closed-project/closed-project.component";
import { ProjectTeamEditComponent } from "./../../../WeWork/projects-team/project-team-edit/project-team-edit.component";
import { CommonService } from "./../../../../_metronic/jeework_old/core/services/common.service";
import { ProjectsTeamService } from "./../../../WeWork/projects-team/Services/department-and-project.service";
import { ListDepartmentService } from "./../../../WeWork/List-department/Services/List-department.service";
import { OffcanvasOptions } from "./../../../../_metronic/jeework_old/core/_base/layout/directives/offcanvas.directive";
import { MenuOptions } from "./../../../../_metronic/jeework_old/core/_base/layout/directives/menu.directive";
import { MenuAsideService } from "./../../../../_metronic/jeework_old/core/_base/layout/services/menu-aside.service";
import { LayoutConfigService } from "./../../../../_metronic/jeework_old/core/_base/layout/services/layout-config.service";
import { Location } from "@angular/common";
import { LayoutService } from "../../../../_metronic/core";
import {
  AfterViewInit,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Component,
  ElementRef,
  OnInit,
  Renderer2,
  ViewChild,
} from "@angular/core";
import { filter } from "rxjs/operators";
import { NavigationEnd, Router } from "@angular/router";
import * as objectPath from "object-path";
// Layout
// import { HtmlClassService } from '../html-class.service';
import { TranslateService } from "@ngx-translate/core";
import { MatDialog } from "@angular/material/dialog";
import { ProjectTeamModel } from "src/app/pages/WeWork/projects-team/Model/department-and-project.model";

@Component({
  selector: "app-aside-wework",
  templateUrl: "./aside-wework.component.html",
  styleUrls: ["./aside-wework.component.scss"],
})
export class AsideWeworkComponent implements OnInit, AfterViewInit {
  @ViewChild("asideMenu", { static: true }) asideMenu: ElementRef;

  currentRouteUrl: string = "";
  insideTm: any;
  outsideTm: any;
  QuickInsert = 0;
  menuCanvasOptions: OffcanvasOptions = {
    baseClass: "kt-aside",
    overlay: true,
    closeBy: "kt_aside_close_btn",
    toggleBy: {
      target: "kt_aside_mobile_toggler",
      state: "kt-header-mobile__toolbar-toggler--active",
    },
  };
  item: ProjectTeamModel;
  ID_Project: number;

  menuOptions: MenuOptions = {
    // vertical scroll
    scroll: null,

    // submenu setup
    submenu: {
      desktop: {
        // by default the menu mode set to accordion in desktop mode
        default: "dropdown",
      },
      tablet: "accordion", // menu set to accordion in tablet mode
      mobile: "accordion", // menu set to accordion in mobile mode
    },

    // accordion setup
    accordion: {
      expandAll: true, // allow having multiple expanded accordions in the menu
    },
  };

  /**
   * Component Conctructor
   *
   * @param htmlClassService: HtmlClassService
   * @param menuAsideService
   * @param layoutConfigService: LayouConfigService
   * @param router: Router
   * @param render: Renderer2
   * @param cdr: ChangeDetectorRef
   */

  disableAsideSelfDisplay: boolean;
  headerLogo: string;
  brandSkin: string;
  ulCSSClasses: string;
  location: Location;
  asideMenuHTMLAttributes: any = {};
  asideMenuCSSClasses: string;
  asideMenuDropdown;
  brandClasses: string;
  asideMenuScroll = 1;
  asideSelfMinimizeToggle = false;

  constructor(
    private layout: LayoutService,
    private loc: Location,
    public _deptServices: ListDepartmentService,
    // public htmlClassService: HtmlClassService,
    public menuAsideService: MenuAsideService,
    public layoutConfigService: LayoutConfigService,
    private router: Router,
    private render: Renderer2,
    private cdr: ChangeDetectorRef,
    private translate: TranslateService,
    private layoutUtilsService: LayoutUtilsService,
    public dialog: MatDialog,
    public _Services: ProjectsTeamService,
    private changeDetectorRefs: ChangeDetectorRef,
    public commonService: CommonService,
    public WeWorkService: WeWorkService
  ) {}

  ngAfterViewInit(): void {}

  ngOnInit(): void {
    // load view settings
    this.disableAsideSelfDisplay =
      this.layout.getProp("aside.self.display") === false;
    this.brandSkin = this.layout.getProp("brand.self.theme");
    this.headerLogo = this.getLogo();
    this.ulCSSClasses = this.layout.getProp("aside_menu_nav");
    this.asideMenuCSSClasses = this.layout.getStringCSSClasses("aside_menu");
    this.asideMenuHTMLAttributes = this.layout.getHTMLAttributes("aside_menu");
    this.asideMenuDropdown = this.layout.getProp("aside.menu.dropdown")
      ? "1"
      : "0";
    this.brandClasses = this.layout.getProp("brand");
    this.asideSelfMinimizeToggle = this.layout.getProp(
      "aside.self.minimize.toggle"
    );
    this.asideMenuScroll = this.layout.getProp("aside.menu.scroll") ? 1 : 0;
    // this.asideMenuCSSClasses = `${this.asideMenuCSSClasses} ${this.asideMenuScroll === 1 ? 'scroll my-4 ps ps--active-y' : ''}`;
    // Routing
    this.location = this.loc;

    this.currentRouteUrl = this.router.url.split(/[?#]/)[0];
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => {
        this.currentRouteUrl = this.router.url.split(/[?#]/)[0];
        this.cdr.markForCheck();
      });
    const config = this.layoutConfigService.getConfig();
    // if (objectPath.get(config, 'aside.menu.dropdown') !== true && objectPath.get(config, 'aside.self.fixed')) {
    // 	this.render.setAttribute(this.asideMenu.nativeElement, 'data-ktmenu-scroll', '1');
    // }

    // if (objectPath.get(config, 'aside.menu.dropdown')) {
    // 	this.render.setAttribute(this.asideMenu.nativeElement, 'data-ktmenu-dropdown', '1');
    // 	// tslint:disable-next-line:max-line-length
    // 	this.render.setAttribute(this.asideMenu.nativeElement, 'data-ktmenu-dropdown-timeout', objectPath.get(config, 'aside.menu.submenu.dropdown.hover-timeout'));
    // }
  }

  private getLogo() {
    // if (this.brandSkin === 'light') {
    //   return 'https://share-devgcs.basecdn.net/apps/wework.png';
    // } else {
    //   return 'https://share-devgcs.basecdn.net/apps/wework.png';
    // }
    return "./../../../../../assets/images/jeework_logo.png";
  }
  //   https://share-devgcs.basecdn.net/apps/wiki.png
  focusOutProject(value, item) {
    this.QuickInsert = 0;

    if (!value) {
      return;
    } else {
      const _item = new ProjectTeamModel();
      _item.title = value;
      _item.id_department = item.id;
      this.Create(_item);
    }
  }

  Create(_item: ProjectTeamModel) {
    this._Services.InsertFasttProjectTeam(_item).subscribe((res) => {
      if (res && res.status == 1) {
        location.reload();
      } else {
        this.layoutUtilsService.showActionNotification(
          res.error.message,
          MessageType.Read,
          9999999999,
          true,
          false,
          3000,
          "top",
          0
        );
      }
      this.changeDetectorRefs.detectChanges();
    });
  }

  /**
   * Check Menu is active
   * @param item: any
   */
  isMenuItemIsActive(item): boolean {
    if (item.submenu) {
      return this.isMenuRootItemIsActive(item);
    }

    if (!item.page) {
      return false;
    }

    return this.currentRouteUrl.indexOf(item.page) !== -1;
  }

  /**
   * Check Menu Root Item is active
   * @param item: any
   */
  isMenuRootItemIsActive(item): boolean {
    let result: boolean = false;

    for (const subItem of item.submenu) {
      result = this.isMenuItemIsActive(subItem);
      if (result) {
        return true;
      }
    }

    return false;
  }

  /**
   * Use for fixed left aside menu, to show menu on mouseenter event.
   * @param e Event
   */
  mouseEnter(e: Event) {
    // check if the left aside menu is fixed
    if (document.body.classList.contains("kt-aside--fixed")) {
      if (this.outsideTm) {
        clearTimeout(this.outsideTm);
        this.outsideTm = null;
      }

      this.insideTm = setTimeout(() => {
        // if the left aside menu is minimized
        if (
          document.body.classList.contains("kt-aside--minimize") &&
          KTUtil.isInResponsiveRange("desktop")
        ) {
          // show the left aside menu
          this.render.removeClass(document.body, "kt-aside--minimize");
          this.render.addClass(document.body, "kt-aside--minimize-hover");
        }
      }, 50);
    }
  }

  /**
   * Use for fixed left aside menu, to show menu on mouseenter event.
   * @param e Event
   */
  mouseLeave(e: Event) {
    if (document.body.classList.contains("kt-aside--fixed")) {
      if (this.insideTm) {
        clearTimeout(this.insideTm);
        this.insideTm = null;
      }

      this.outsideTm = setTimeout(() => {
        // if the left aside menu is expand
        if (
          document.body.classList.contains("kt-aside--minimize-hover") &&
          KTUtil.isInResponsiveRange("desktop")
        ) {
          // hide back the left aside menu
          this.render.removeClass(document.body, "kt-aside--minimize-hover");
          this.render.addClass(document.body, "kt-aside--minimize");
        }
      }, 100);
    }
  }

  /**
   * Returns Submenu CSS Class Name
   * @param item: any
   */
  getItemCssClasses(item) {
    let classes = "kt-menu__item";

    if (objectPath.get(item, "submenu")) {
      classes += " kt-menu__item--submenu";
    }

    if (!item.submenu && this.isMenuItemIsActive(item)) {
      classes += " kt-menu__item--active kt-menu__item--here";
    }

    if (item.submenu && this.isMenuItemIsActive(item)) {
      classes += " kt-menu__item--open kt-menu__item--here";
    }

    // custom class for menu item
    const customClass = objectPath.get(item, "custom-class");
    if (customClass) {
      classes += " " + customClass;
    }

    if (objectPath.get(item, "icon-only")) {
      classes += " kt-menu__item--icon-only";
    }

    return classes;
  }

  getItemAttrSubmenuToggle(item) {
    let toggle = "hover";
    if (objectPath.get(item, "toggle") === "click") {
      toggle = "click";
    } else if (objectPath.get(item, "submenu.type") === "tabs") {
      toggle = "tabs";
    } else {
      // submenu toggle default to 'hover'
    }

    return toggle;
  }
  AddProject(id, is_project: boolean) {
    var _item: any = [];
    var _project = new ProjectTeamModel();
    _project.clear(); // Set all defaults fields
    this._Services.DeptDetail(id.id).subscribe((res) => {
      if (res && res.status == 1) {
        _project = res.data;
        _project.is_project = is_project;
        this.UpdateProject(_project);
        this.changeDetectorRefs.detectChanges();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  ProjectTeamDetail(id) {
    this._Services.DeptDetail(id).subscribe((res) => {
      if (res && res.status == 1) {
        // return res.data.locked;
        // return res.data;
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  UpdateProject(_item: ProjectTeamModel) {
    const dialogRef = this.dialog.open(ProjectTeamEditComponent, {
      data: { _item, _IsEdit: _item.IsEdit, is_project: _item.is_project },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      } else {
      }
    });
  }
  ClosedProject(item) {
    var model = new ProjectTeamModel();
    model.clear(); // Set all defaults fields
    this._Services.DeptDetail(item.id).subscribe((res) => {
      if (res && res.status == 1) {
        model = res.data;
        this.Update_ClosedProject(model);
        this.changeDetectorRefs.detectChanges();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }
  Update_ClosedProject(_item) {
    let saveMessageTranslateParam = "";
    saveMessageTranslateParam +=
      _item.id_row > 0
        ? "GeneralKey.capnhatthanhcong"
        : "GeneralKey.themthanhcong";
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    var isReset = true;
    const dialogRef = this.dialog.open(ClosedProjectComponent, {
      data: { _item, isReset },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        this.ngOnInit();
      } else {
        this.layoutUtilsService.showActionNotification(
          _saveMessage,
          _messageType,
          4000,
          true,
          false
        );
        this.ngOnInit();
      }
    });
  }

  DuplicateProject(item) {
    const model = new ProjectTeamDuplicateModel();
    model.clear(); // Set all defaults fields
    this.Update_DuplicateProject(model, item.id);
  }
  Update_DuplicateProject(_item: ProjectTeamDuplicateModel, id_project_team) {
    var newitem: any = [];
    this._Services.DeptDetail(id_project_team).subscribe((res) => {
      if (res && res.status == 1) {
        newitem = res.data;

        let saveMessageTranslateParam = "";
        //_item = this.item;
        _item.id = id_project_team;
        _item.title = newitem.title;
        saveMessageTranslateParam +=
          _item.id > 0
            ? "GeneralKey.capnhatthanhcong"
            : "GeneralKey.themthanhcong";
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
          _item.id > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(DuplicateProjectComponent, {
          data: { _item },
        });
        dialogRef.afterClosed().subscribe((res) => {
          if (!res) {
            this.ngOnInit();
          } else {
            this.layoutUtilsService.showActionNotification(
              _saveMessage,
              _messageType,
              4000,
              true,
              false
            );
            this.gotoPageDuplicate(res.res.data.ItemArray);
          }
        });
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  gotoPageDuplicate(item) {
    var newurl = "/project/" + item[0];
    const _title = this.translate.instant("notify.thongbaochuyentrang");
    const _description =
      this.translate.instant("notify.bancomuonchuyendentrang") + item[2];
    const _waitDesciption =
      this.translate.instant("notify.dangchuyentrang") + item[2];
    const dialogRef = this.layoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        this.ngOnInit();
        return;
      } else {
        this.router.navigate(["/project", item[0]]).then(() => {
          this.ngOnInit();
        });
      }
    });
  }

  updateStage(_item) {
    this._Services.DeptDetail(_item.id).subscribe((res) => {
      if (res && res.status == 1) {
        _item = res.data;
        let saveMessageTranslateParam = "";
        saveMessageTranslateParam +=
          _item.id_row > 0
            ? "GeneralKey.capnhatthanhcong"
            : "GeneralKey.themthanhcong";
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
          _item.id_row > 0 ? MessageType.Update : MessageType.Create;

        const dialogRef = this.dialog.open(UpdateStatusProjectComponent, {
          data: { _item, _IsEdit: _item.IsEdit },
        });
        dialogRef.afterClosed().subscribe((res) => {
          if (!res) {
            return;
          } else {
            this.ngOnInit();
            this.layoutUtilsService.showActionNotification(
              _saveMessage,
              _messageType,
              4000,
              true,
              false
            );
            // this.changeDetectorRefs.detectChanges();
          }
        });
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }
  Deleted(item) {
    const ObjectModels = new DepartmentModel();
    ObjectModels.clear();
    this.Delete(ObjectModels, item.id);
  }
  Delete(_item: DepartmentModel, ID_Project) {
    _item.RowID = ID_Project;
    const _title = this.translate.instant("GeneralKey.xoa");
    const _description = this.translate.instant("projects.confirmxoa");
    const _waitDesciption = this.translate.instant(
      "GeneralKey.dulieudangduocxoa"
    );
    const _deleteMessage = this.translate.instant("GeneralKey.xoathanhcong");
    const dialogRef = this.layoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      }
      this._Services.DeleteProject(_item.RowID).subscribe((res) => {
        if (res && res.status === 1) {
          this.layoutUtilsService.showActionNotification(
            _deleteMessage,
            MessageType.Delete,
            4000,
            true,
            false,
            3000,
            "top",
            1
          );
          if (ID_Project == this.router.url.split("/")[2]) {
            this.router.navigate(["/wework/projects"]).then(() => {
              window.location.reload();
            });
          } else {
            location.reload();
          }
        } else {
          this.layoutUtilsService.showActionNotification(
            res.error.message,
            MessageType.Read,
            9999999999,
            true,
            false,
            3000,
            "top",
            0
          );
        }
      });
    });
  }

  ViewDetail(item) {
    this.router.navigate(["/depts", item.id, "projects"]); //depts/5/projects
  }

  AddDept(item, id = 0) {
    const ObjectModels = new DepartmentModel();
    ObjectModels.clear(); // Set all defaults fields
    this.UpdateDept(ObjectModels, item.id);
  }

  UpdateDept(_item: DepartmentModel, ID_Department) {
    var item: any = [];
    this._deptServices.DeptDetail(ID_Department).subscribe((res) => {
      if (res && res.status == 1) {
        item = res.data;
        _item.RowID = item.id_row;
        _item.title = item.title;
        _item.id_cocau = item.id_cocau;
        _item.ParentID = item.ParentID;
        let saveMessageTranslateParam = "";
        saveMessageTranslateParam +=
          _item.RowID > 0
            ? "GeneralKey.capnhatthanhcong"
            : "GeneralKey.themthanhcong";
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
          _item.RowID > 0 ? MessageType.Update : MessageType.Create;
        var IsUpdate = _item.RowID > 0 ? true : false;
        const dialogRef = this.dialog.open(DepartmentEditNewComponent, {
          data: { _item, _IsEdit: _item.IsEdit, IsUpdate },
        });
        dialogRef.afterClosed().subscribe((res) => {
          if (!res) {
            return;
          } else {
            this.ngOnInit();
            this.layoutUtilsService.showActionNotification(
              _saveMessage,
              _messageType,
              4000,
              true,
              false
            );
            this.changeDetectorRefs.detectChanges();
          }
        });
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  NavigateTo(item) {
    if (item) {
      // this.router.navigate([item]);
      var res = item.replace("//", "/");
      var res2 = res.replace("/home/clickup", "");

      // this.router.navigateByUrl(res2);
      this.router.navigate([res2]);
    }
  }

  DeleteDept(item) {
    var ObjectModels = new DepartmentModel();
    ObjectModels.clear();
    const _title = this.translate.instant("GeneralKey.xoa");
    const _description = this.translate.instant("department.confirmxoa");
    const _waitDesciption = this.translate.instant(
      "GeneralKey.dulieudangduocxoa"
    );
    const _deleteMessage = this.translate.instant("GeneralKey.xoathanhcong");
    const dialogRef = this.layoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      }
      this._deptServices.Delete_Dept(item.id).subscribe((res) => {
        if (res && res.status === 1) {
          this.layoutUtilsService.showActionNotification(
            _deleteMessage,
            MessageType.Delete,
            4000,
            true,
            false,
            3000,
            "top",
            1
          );
          location.reload();
        } else {
          this.layoutUtilsService.showActionNotification(
            res.error.message,
            MessageType.Read,
            9999999999,
            true,
            false,
            3000,
            "top",
            0
          );
        }
      });
    });
  }

  Change(item) {
    const ObjectModels = new DepartmentModel();
    ObjectModels.clear();
    this.ChangeType(ObjectModels, item.id);
  }
  ChangeType(_item: DepartmentModel, id_project_team: number) {
    _item.RowID = id_project_team;
    const _title = this.translate.instant("projects.chuyenloaiteam");
    const _description = this.translate.instant("projects.confirmchange");
    const _waitDesciption = this.translate.instant(
      "projects.dulieudangduocthaydoi"
    );
    const _deleteMessage = this.translate.instant("projects.thaydoithanhcong");
    const dialogRef = this.layoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      }
      this._Services.ChangeType(_item.RowID).subscribe((res) => {
        if (res && res.status === 1) {
          this.layoutUtilsService.showActionNotification(
            _deleteMessage,
            MessageType.Delete,
            4000,
            true,
            false,
            3000,
            "top",
            1
          );
          let _backUrl = `/wework/projects`;
          this.router.navigateByUrl(_backUrl);
        } else {
          this.layoutUtilsService.showActionNotification(
            res.error.message,
            MessageType.Read,
            9999999999,
            true,
            false,
            3000,
            "top",
            0
          );
          this.ngOnInit();
        }
      });
    });
  }
  isFavourite = false;
  favourite() {
    this._Services.favourireproject(this.ID_Project).subscribe((res) => {
      if (res && res.status == 1) {
        this.isFavourite = res.data;
        this.changeDetectorRefs.detectChanges();
        this.layoutUtilsService.showActionNotification(
          this.translate.instant("GeneralKey.capnhatthanhcong"),
          MessageType.Read,
          4000,
          true,
          false,
          3000,
          "top",
          1
        );
      } else
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
    });
  }

  // add department
  Add() {
    const ObjectModels = new DepartmentModel();
    ObjectModels.clear(); // Set all defaults fields
    this.Update(ObjectModels);
  }
  AddFolder(item) {
    const ObjectModels = new DepartmentModel();
    ObjectModels.clear(); // Set all defaults fields
	ObjectModels.ParentID = item.id;
    this.Update(ObjectModels);
  }

  Update(_item: DepartmentModel) {
    let saveMessageTranslateParam = "";
    saveMessageTranslateParam +=
      _item.RowID > 0
        ? "GeneralKey.capnhatthanhcong"
        : "GeneralKey.themthanhcong";
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.RowID > 0 ? MessageType.Update : MessageType.Create;

    // DepartmentEditComponent -- department cũ

    const dialogRef = this.dialog.open(DepartmentEditNewComponent, {
      // minHeight: '50vh',
      data: { _item, _IsEdit: _item.IsEdit },
      minWidth: "650px",
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      } else {
        // this.ngOnInit();
        this.layoutUtilsService.showActionNotification(
          _saveMessage,
          _messageType,
          4000,
          true,
          false
        );
        location.reload();
        // this.changeDetectorRefs.detectChanges();
      }
    });
  }

  // thêm mới project team
  AddNewProject(item, is_project = true) {
    const _project = new ProjectTeamModel();
    _project.clear(); // Set all defaults fields
    _project.is_project = is_project;
    _project.id_project_team = item.id;

    this.UpdateNewProject(_project);
  }
  UpdateNewProject(_item: ProjectTeamModel) {
    const dialogRef = this.dialog.open(AddStatusComponent, {
      width: "70vw",
      height: "70vh",
      data: {
        _item,
        _IsEdit: _item.IsEdit,
        is_project: _item.is_project,
        id_project_team: _item.id_project_team,
      },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        // location.reload();
        return;
      } else {
      }
    });
  }

  UpdateStatus(item) {
    this._Services.DeptDetail(item.id).subscribe((res) => {
      if (res && res.status == 1) {
        const dialogRef = this.dialog.open(ProjectTeamEditStatusComponent, {
          data: res.data,
          minWidth: "800px",
        });
        dialogRef.afterClosed().subscribe((res) => {
          if (!res) {
            return;
          } else {
            location.reload();
          }
        });
        this.changeDetectorRefs.detectChanges();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }
  currentUrl: string;
  isMenuItemActive(path) {
    if (!this.currentUrl || !path) {
      return false;
    }

    if (this.currentUrl === path) {
      return true;
    }

    if (this.currentUrl.indexOf(path) > -1) {
      return true;
    }

    return false;
  }

  getText(item){
	  if(item.title){
		  return item.title[0];
	  }
	  return 'N';
  }
  getColorText(item){
    var text = "";
	  if(item.title){
		  text = item.title[0];
	  }
    if(text != ""){
      return this.WeWorkService.getColorNameUser(text);
    }
    return "red";
  }
}
