import { MatDialog } from "@angular/material/dialog";
import { TokenStorage } from "./../../../../_metronic/jeework_old/core/auth/_services/token-storage.service";
import { MenuPhanQuyenServices } from "./../../../../_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service";
import { StatusDynamicDialogComponent } from "./../../status-dynamic/status-dynamic-dialog/status-dynamic-dialog.component";
import { FormControl } from "@angular/forms";
import { WeWorkService } from "./../../services/wework.services";
import { WorkGroupEditComponent } from "./../../work/work-group-edit/work-group-edit.component";
import {
  WorkModel,
  WorkGroupModel,
  UpdateWorkModel,
} from "./../../work/work.model";
import {
  Component,
  OnInit,
  ElementRef,
  ViewChild,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Inject,
  HostListener,
  Input,
  SimpleChange,
} from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
// Material
import { SelectionModel } from "@angular/cdk/collections";
// RXJS
import { fromEvent, merge, ReplaySubject } from "rxjs";
import { TranslateService } from "@ngx-translate/core";
import * as moment from "moment";
// Services
import { DanhMucChungService } from "./../../../../_metronic/jeework_old/core/services/danhmuc.service";
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
// Models
import {
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from "@angular/cdk/drag-drop";
// import { DialogDecision } from '../process-work-details/process-work-details.component';
import { QueryParamsModelNew } from "./../../../../_metronic/jeework_old/core/models/query-models/query-params.model";
import { ProjectsTeamService } from "../Services/department-and-project.service";
@Component({
  selector: "kt-work-kanban",
  templateUrl: "./work-kanban.component.html",
  styleUrls: ["./work-kanban.component.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkKanBanComponent implements OnInit {
  @Input() WorkID: any;
  @Input() Values: any;
  Data_Header: any[] = [];
  Data_Body: any[] = [];
  ProjectTeam: any = [];
  weeks = [];
  filter_groupby: any = [];
  filter_subtask: any = [];
  connectedTo = [];
  GiaiDoanID: number = 0;
  Type: number = 0; //0: Bắt đầu, 1: Công việc, 2: Quyết định, 3: Kết quả quyết định, 4: Quy trình khác, 5: Đồng thời, 6: Kết thúc; 7: Gửi mail
  //==========Dropdown Search==============
  filter: any = {};
  options_assign: any = {};
  UserID = 0;
  isAssignforme = false;
  isEdittitle = -1;
  startDatelist: Date = new Date();
  selection = new SelectionModel<WorkModel>(true, []);
  list_role: any = [];
  keyword: string = "";
  showclosedsubtask = false;
  showclosedtask = false;
  showemptystatus = false;
  tasklocation = false;
  addNodeitem: number = -1;
  IsAdminGroup = false;
  @Input() ID_Project: number = 0;
  constructor(
    public _service: ProjectsTeamService,
    private danhMucService: DanhMucChungService,
    private WeWorkService: WeWorkService,
    private menuServices: MenuPhanQuyenServices,
    public dialog: MatDialog,
    private route: ActivatedRoute,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    private changeDetectorRefs: ChangeDetectorRef,
    private tokenStorage: TokenStorage,
    private router: Router
  ) {
    this.filter_groupby = this.listFilter_Groupby[0];
    this.filter_subtask = this.listFilter_Subtask[0];
    // this.list_priority = this.weworkService.list_priority;
    this.UserID = +localStorage.getItem("idUser");
  }

  ngOnInit() {
    this.Load();
    this.LoadData();
    this.LoadDetailProject();
    this.options_assign = this.getOptions_Assign();
    this.changeDetectorRefs.detectChanges();
  }
  /** LOAD DATA */
  Load() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      "",
      "",
      1,
      50,
      true
    );
    this.layoutUtilsService.showWaitingDiv();
    this._service.listView(queryParams).subscribe((res) => {
      this.layoutUtilsService.OffWaitingDiv();
      if (res && res.status == 1) {
        if (res.data.length > 0) {
          this.Data_Header = res.data;
          this.changeDetectorRefs.detectChanges();
        }
      } else {
        // this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
      }
    });
    const filter: any = {};
    // filter.key = 'id_project_team';
    // filter.value = this.ID_Project;
    filter.id_project_team = this.ID_Project;
    this.WeWorkService.list_account(filter).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listUser = res.data;
        this.setUpDropSearchNhanVien();
        this.changeDetectorRefs.detectChanges();
      }
      this.options_assign = this.getOptions_Assign();
    });

    // const filter: any = {};
    // filter.key = 'id_project_team';
    // filter.value = this.ID_Project;
    // this.weworkService.list_account(filter).subscribe(res => {
    // if (res && res.status === 1) {
    // 	this.listUser = res.data;
    // 	// this.setUpDropSearchNhanVien();
    // };
    // this.options_assign = this.getOptions_Assign();
    // // this.changeDetectorRefs.detectChanges();
    // });
  }

  data: any = [];
  listFilter: any = [];
  ListColumns: any = [];
  ListTasks: any = [];
  ListTags: any = [];
  status_dynamic: any = [];
  LoadData() {
    this.menuServices.GetRoleWeWork("" + this.UserID).subscribe((res) => {
      if (res && res.status == 1) {
        this.list_role = res.data.dataRole;
        this.IsAdminGroup = res.data.IsAdminGroup;
      }
      if (!this.CheckRoles(3)) {
        this.isAssignforme = true;
      }
    });
    this.tokenStorage.getUserData().subscribe((res) => {
      this.menuServices.WW_Roles(res.Username).subscribe((resl) => { });
    });
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      "",
      "",
      0,
      50,
      true
    );
    this.data = [];
    this.layoutUtilsService.showWaitingDiv();
    this._service.GetDataWorkCU(queryParams).subscribe((res) => {
      this.layoutUtilsService.OffWaitingDiv();
      if (res && res.status === 1) {
        this.data = res.data;
        this.listFilter = this.data.Filter;

        this.ListColumns = this.data.TenCot;
        //xóa title khỏi cột
        var indextt = this.ListColumns.findIndex((x) => x.fieldname == "title");
        if (indextt >= 0) this.ListColumns.splice(indextt, 1);
        this.ListColumns.sort((a, b) =>
          a.title > b.title ? 1 : b.title > a.title ? -1 : 0
        ); // xếp theo anphabet
        this.ListColumns.sort((a, b) =>
          a.id_project_team > b.id_project_team
            ? -1
            : b.id_project_team > a.id_project_team
              ? 1
              : 0
        ); // nào chọn xếp trước
        this.ListColumns.sort((a, b) =>
          a.isbatbuoc > b.isbatbuoc ? -1 : b.isbatbuoc > a.isbatbuoc ? 1 : 0
        ); // nào bắt buộc xếp trước
        this.ListTasks = this.data.datawork;
        //   this.prepareDragDrop(this.ListTasks);// load list kéo thả
        this.ListTags = this.data.Tag;
        this.LoadListStatus();
        this.changeDetectorRefs.detectChanges();
        // setTimeout(() => {
        // }, 2000);
        // this.changeDetectorRefs.detectChanges();
      }
    });

    this.WeWorkService.ListStatusDynamic(this.ID_Project).subscribe((res) => {
      if (res && res.status === 1) {
        this.status_dynamic = res.data;
        //load ItemFinal
        var x = this.status_dynamic.find((val) => val.IsFinal == true);
        //   if (x) {
        // 	this.ItemFinal = x.id_row;
        //   } else {
        //   }
        // this.changeDetectorRefs.detectChanges();
      }
    });
  }

  LoadDetailProject() {
    this._service.DeptDetail(this.ID_Project).subscribe((res) => {
      if (res && res.status == 1) {
        this.ProjectTeam = res.data;
      }
    });
  }

  listStatus: any = [];
  LoadListStatus() {
    this.ListTasks.forEach((element) => {
      element.isExpanded = false;
      this.listFilter.forEach((val) => {
        if (!val.data) {
          val.data = [];
        }
        if (this.isAssignforme) {
          if (
            +val.id_row == +element.status &&
            element.User.find((x) => x.id_user == this.UserID)
          ) {
            val.data.push(element);
          } else if (
            element.User.find((x) => x.id_user == val.id_row) &&
            element.User.find((x) => x.id_user == this.UserID)
          ) {
            if (element.User.length == 1) {
              val.data.push(element);
            }
          }
        } else {
          if (+val.id_row == +element.status) {
            val.data.push(element);
          } else if (
            element.User.find((x) => x.id_user == val.id_row) ||
            (element.User.length == 0 && val.id_row == "") ||
            (element.User.length > 1 && val.id_row == "0")
          ) {
            if (
              element.User.length == 1 ||
              (element.User.length == 0 && val.id_row == "") ||
              (element.User.length > 1 && val.id_row == "0")
            ) {
              val.data.push(element);
            }
          }
        }
      });
    });
    this.listStatus = this.listFilter;
  }

  CloseAddnewTask(val) {
    if (val) {
      this.addNodeitem = -1;
      //   this.newtask = -1;
    }
  }
  CreateTask(val) {
    this._service.InsertTask(val).subscribe((res) => {
      if (res && res.status == 1) {
        this.CloseAddnewTask(true);
        this.LoadData();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  getAssignee(id_nv) {
    if (+id_nv > 0 && this.listUser) {
      var assign = this.listUser.find((x) => x.id_nv == id_nv);
      if (assign) {
        return assign;
      }
      return false;
    }
    return false;
  }

  listFilter_Groupby = [
    {
      title: "Status",
      value: "status",
    },
    {
      title: "Assignee",
      value: "assignee",
    },
    // {
    // 	title: 'Priority',
    // 	value: 'priority'
    // },
  ];
  GroupBy(item) {
    this.filter_groupby = item;
    this.LoadData();
  }

  listFilter_Subtask = [
    {
      title: "showtask",
      showvalue: "showtask",
      value: "hide",
    },
    {
      title: "expandall",
      showvalue: "expandall",
      value: "show",
    },
  ];

  public bankFilterCtrl: FormControl = new FormControl();
  setUpDropSearchNhanVien() {
    this.bankFilterCtrl.setValue("");
    this.filterBanks();
    this.bankFilterCtrl.valueChanges.pipe().subscribe(() => {
      this.filterBanks();
    });
  }
  public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  protected filterBanks() {
    if (!this.listUser) {
      return;
    }
    let search = this.bankFilterCtrl.value;
    if (!search) {
      this.filteredBanks.next(this.listUser.slice());
      return;
    } else {
      search = search.toLowerCase();
    }
    // filter the banks
    this.filteredBanks.next(
      this.listUser.filter(
        (bank) => bank.hoten.toLowerCase().indexOf(search) > -1
      )
    );
  }
  _Assign: string = "";
  getKeyword_Assign() {
    let i = this._Assign.lastIndexOf("@");
    if (i >= 0) {
      let temp = this._Assign.slice(i);
      if (temp.includes(" ")) return "";
      return this._Assign.slice(i);
    }
    return "";
  }

  getName() {
    if (this.filter_groupby.value == "assignee") {
      return "Chưa được giao việc";
    } else if (this.filter_groupby.value == "status") {
      return "Chưa gắn trạng thái công việc";
    }
    return "";
  }

  ItemSelected(val: any, item) {
    if (val.id_user) {
      val.id_nv = val.id_user;
    }
    var model = new UpdateWorkModel();
    model.id_row = item.id_row;
    model.key = "assign";
    model.value = val.id_nv;
    this.layoutUtilsService.showWaitingDiv();
    this._service._UpdateByKey(model).subscribe((res) => {
      this.layoutUtilsService.OffWaitingDiv();
      if (res && res.status == 1) {
        // this.layoutUtilsService.showActionNotification(this.translate.instant('JeeHR.capnhatthanhcong'), MessageType.Read, 4000, true, false, 3000, 'top', 1);
        this.LoadData();
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

  UpdateByKey(idUpdate, key, value, IsStaff = false) {
    var model = new UpdateWorkModel();
    model.id_row = idUpdate;
    model.key = key;
    model.value = value;
    model.IsStaff = IsStaff;
    this.layoutUtilsService.showWaitingDiv();
    this._service._UpdateByKey(model).subscribe((res) => {
      this.layoutUtilsService.OffWaitingDiv();
      if (res && res.status == 1) {
      } else {
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
        this.LoadData();
      }
    });
  }

  listUser: any[] = [];
  selected_Assign: any[] = [];
  getOptions_Assign() {
    var options_assign: any = {
      showSearch: true,
      keyword: this.getKeyword_Assign(),
      data: this.listUser.filter(
        (x) => this.selected_Assign.findIndex((y) => x.id_nv == y.id_nv) < 0
      ),
    };
    return options_assign;
  }

  filterConfiguration(): any {
    // this.filter.ProcessID = this.ID_QuyTrinh;
    const filter: any = {};
    filter.id_project_team = this.ID_Project;
    filter.groupby = this.filter_groupby.value; //assignee
    filter.keyword = this.keyword;
    return filter;
  }
  getColorProgressbar(percentage: number = 0): string {
    if (percentage < 50) return "metal";
    else if (percentage < 100) return "brand";
    else return "success";
  }
  getHeight(): any {
    let tmp_height = 0;
    tmp_height = window.innerHeight - 110 - this.tokenStorage.getHeightHeader();
    return tmp_height + "px";
  }
  //===================================Chuyển về giai đoan trước===========================
  ChuyenGiaiDoanTruoc(item: any) {
    let _item = {
      NodeID: item.StageTasksID,
      InfoChuyenGiaiDoanData: [],
      IsNext: false,
      NodeListID: 0,
    };
  }
  //============================================Xét style CSS=================================
  height: number = 1200;
  onScroll($event) {
    let _scroll = 1200;
    let _height = _scroll + $event.currentTarget.scrollTop;
    this.height = _height;
  }

  ViewDetail(item) {
		this.router.navigate(['', { outlets: { auxName: 'aux/detail/'+item.id_row }, }]);
    // const dialogRef = this.dialog.open(WorkListNewDetailComponent, {
    //   width: "90vw",
    //   height: "90vh",
    //   data: item,
    // });

    // dialogRef.afterClosed().subscribe((result) => {
    //   this.LoadData();
    //   if (result != undefined) {
    //     // this.selectedDate.startDate = new Date(result.startDate)
    //     // this.selectedDate.endDate = new Date(result.endDate)
    //   }
    // });
  }

  ThemCongviec() {
    const ObjectModels = new WorkModel();
    ObjectModels.clear(); // Set all defaults fields
  }

  AddWorkGroup() {
    const ObjectModels = new WorkGroupModel();
    ObjectModels.clear(); // Set all defaults fields
    this.UpdateWorkGroup(ObjectModels);
  }
  UpdateWorkGroup(_item: WorkGroupModel) {
    let saveMessageTranslateParam = "";
    _item.id_project_team = "" + this.ID_Project;

    saveMessageTranslateParam +=
      _item.id_row > 0
        ? "GeneralKey.capnhatthanhcong"
        : "GeneralKey.themthanhcong";
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(WorkGroupEditComponent, {
      data: { _item },
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
  }

  stopPropagation(event) {
    event.stopPropagation();
  }

  CheckRoles(roleID: number) {
    if (this.IsAdminGroup) return true;
    var x = this.list_role.find((x) => x.id_row == this.ID_Project);
    if (x) {
      if (x.admin == true || x.admin ==1 || +x.owner ==1 || +x.parentowner ==1 ) {
        return true;
      } else {
        if (
          roleID == 7 ||
          roleID == 9 ||
          roleID == 11 ||
          roleID == 12 ||
          roleID == 13
        ) {
          if (x.Roles.find((r) => r.id_role == 15)) return false;
        }
        if (roleID == 10) {
          if (x.Roles.find((r) => r.id_role == 16)) return false;
        }
        if (roleID == 4 || roleID == 14) {
          if (x.Roles.find((r) => r.id_role == 17)) return false;
        }
        var r = x.Roles.find((r) => r.id_role == roleID);
        if (r) {
          return true;
        } else {
          return false;
        }
      }
    } else {
      return false;
    }
  }
  CheckRoleskeypermit(key) {
    if (this.IsAdminGroup) return true;
    if (this.list_role) {
      var x = this.list_role.find((x) => x.id_row == this.ID_Project);
      if (x) {
        if (x.admin == true || x.admin ==1 || +x.owner ==1 || +x.parentowner ==1 ) {
          return true;
        } else {
          if (
            key == "title" ||
            key == "description" ||
            key == "status" ||
            key == "checklist" ||
            key == "delete"
          ) {
            if (x.Roles.find((r) => r.id_role == 15)) return false;
          }
          if (key == "deadline") {
            if (x.Roles.find((r) => r.id_role == 16)) return false;
          }
          if (key == "id_nv" || key == "assign") {
            if (x.Roles.find((r) => r.id_role == 17)) return false;
          }
          var r = x.Roles.find((r) => r.keypermit == key);
          if (r) {
            return true;
          } else {
            return false;
          }
        }
      } else {
        return false;
      }
    }
    return false;
  }

  drop(event: CdkDragDrop<string[]>, val) {
    var item;
    var newplace = val;
    if (event.previousContainer !== event.container) {
      transferArrayItem(
        event.previousContainer.data,
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
      item = event.container.data[event.currentIndex];
      this.UpdateStaus(item, newplace);
    } else {
      moveItemInArray(
        event.container.data,
        event.previousIndex,
        event.currentIndex
      );
    }
  }
  UpdateStaus(item, newplace) {
    if (this.filter_groupby.value == "assignee") {
      item.id_nv = newplace.id_row;
      var isStaff = false;
      if (newplace.id_row > 0) isStaff = true;
      this.UpdateByKey(item.id_row, "assign", newplace.id_row, isStaff);
    } else {
      item.status = newplace.id_row;
      var isStaff = false;
      if (item.id_nv > 0) isStaff = true;
      this.UpdateByKey(item.id_row, "status", newplace.id_row, isStaff);
    }
  }

  updateDate(item) {
    var isStaff = false;
    if (item.id_nv > 0) isStaff = true;
    this.UpdateByKey(
      item.id_row,
      "deadline",
      moment(item.deadline).format("MM/DD/YYYY HH:mm"),
      isStaff
    );
  }

  DeleteByKey(item, key) {
    var isStaff = false;
    if (key != "assign") {
      if (item.id_nv > 0) isStaff = true;
    } else {
      item.id_nv = 0;
    }
    this.UpdateByKey(item.id_row, key, null, isStaff);
  }

  updateState(item) {
    //item.item.data.state = item.container.element.nativeElement.attributes.state.value;
  }

  chinhsuastt(item) {
    item.id_project_team = this.ID_Project;
    const dialogRef = this.dialog.open(StatusDynamicDialogComponent, {
      width: "40vw",
      minHeight: "200px",
      data: item,
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        this.LoadData();
      }
    });
  }

  getNhom() {
    if (this.filter_groupby.value == "assignee") {
      return "assign";
    }
    return "status";
  }

  trackByFn(index, item) {
    return item.id_row;
  }
}
