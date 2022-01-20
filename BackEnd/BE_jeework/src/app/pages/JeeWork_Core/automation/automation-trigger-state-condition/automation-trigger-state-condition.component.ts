import { JeeWorkLiteService } from "./../../services/wework.services";
import { ListDepartmentService } from "./../../department/Services/List-department.service";
import { TranslateService } from "@ngx-translate/core";
import { TemplateCenterService } from "./../../template-center/template-center.service";
import { ProjectsTeamService } from "./../../projects-team/Services/department-and-project.service";
import { LayoutUtilsService, MessageType } from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  Input,
  SimpleChanges,
  OnChanges,
  Output,
  EventEmitter,
} from "@angular/core";

@Component({
  selector: "app-automation-trigger-state-condition",
  templateUrl: "./automation-trigger-state-condition.component.html",
  styleUrls: ["./automation-trigger-state-condition.component.scss"],
})
export class AutomationTriggerStateConditionComponent
  implements OnInit, OnChanges {
  @Input() ID_projectteam: number = 0;
  @Input() ID_department: number = 0;
  @Input() condition: any = {};
  @Input() Eventid: number = 1;
  @Output() valueout = new EventEmitter<any>();
  Remapdata = false;
  value: any = [];
  listPriority: any = [];
  listStatus: any = [];
  listUser: any = [];
  data_condition: any = [];
  constructor(
    private layoutUtilsService: LayoutUtilsService,
    private projectsTeamService: ProjectsTeamService,
    private templatecenterService: TemplateCenterService,
    private translateService: TranslateService,
    private departmentServices: ListDepartmentService,
    private changeDetectorRefs: ChangeDetectorRef,
    private weWorkService: JeeWorkLiteService
  ) {
    this.listPriority = weWorkService.list_priority;
  }

  ngOnInit(): void {
    // list Status
    this.LoadDataStatus();
    // list account user
    this.weWorkService.list_account({}).subscribe((res) => {
      if (res && res.status == 1) {
        this.listUser = res.data;
        if (this.Eventid == 5 || this.Eventid == 6) this.loadAssign_ev56();
      } else {
        this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
      }
    });
  }

  LoadDataStatus() {
    if (this.ID_projectteam > 0) {
      this.weWorkService
        .ListStatusDynamic(this.ID_projectteam)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.listStatus = res.data;
            this.LoadDataEvent1();
            this.changeDetectorRefs.detectChanges();
          } else {
            this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
          }
        });
    } else if (this.ID_department > 0) {
      this.weWorkService
        .ListStatusDynamicByDepartment(this.ID_department)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.listStatus = res.data;
            this.LoadDataEvent1();
            this.changeDetectorRefs.detectChanges();
          } else {
            this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
          }
        });
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    this.LoadDataStatus();
    this.value = {};
    if (this.condition.condition && this.condition.condition[0]) {
      this.data_condition = this.condition.condition[0];
      this.MapvalueEdit();
    }
    this.valueout.emit(this.value);
  }
  MapvalueEdit() {
    switch (this.Eventid) {
      case 1: // thay đổi trạng thái
        {
          this.LoadDataEvent1();
        }
        break;
      case 2: // priority
        {
          if (this.data_condition.from && this.data_condition.from != "any") {
            var x = this.data_condition.from.split(",");
            if (x && x.length > 0) {
              this.value.from = +x[0];
            }
          }
          if (this.data_condition.to && this.data_condition.to != "any") {
            var x = this.data_condition.to.split(",");
            if (x && x.length > 0) {
              this.value.to = +x[0];
            }
          }
        }
        break;
      case 5: // gắn người
      case 6:
        {
          this.loadAssign_ev56();
        }
        break;
    }
  }
  loadAssign_ev56() {
    if (!this.listUser || this.listUser.length == 0) return;
    if (this.Eventid != this.condition.eventid) return;
    if (!this.Remapdata && this.data_condition.list && this.data_condition.list != "any") {
      this.Remapdata = true;
      var listID = this.data_condition.list.split(",");
      listID.forEach((id) => {
        var user = this.listUser.find((x) => +x.id_nv == +id);
        if (user) this.SelectedUser(user);
      });
    }
  }
  LoadDataEvent1() {
    if (this.listStatus.length == 0) return;
    if (this.Eventid != this.condition.eventid || this.Eventid != 1) return;
    var condition = this.data_condition;
    var listIDfrom = condition.from.split(",");
    var listIDto = condition.to.split(",");
    this.value.from = [];
    this.value.to = [];
    if (listIDfrom) {
      listIDfrom.forEach((element) => {
        var item = this.listStatus.find((x) => x.id_row == element);
        if (item) this.value.from.push(item);
      });
    }
    if (listIDto) {
      listIDto.forEach((element) => {
        var item = this.listStatus.find((x) => x.id_row == element);
        if (item) this.value.to.push(item);
      });
    }
  }
  SelectedStatus(status, isfrom = true) {
    if (isfrom) {
      if (!this.value.from) {
        this.value.from = [];
      }
      var index = this.value.from.findIndex((x) => x.id_row == status.id_row);
      if (index >= 0) {
        this.value.from.splice(index, 1);
      } else {
        this.value.from.push(status);
      }
    } else {
      if (!this.value.to) {
        this.value.to = [];
      }
      var index = this.value.to.findIndex((x) => x.id_row == status.id_row);
      if (index >= 0) {
        this.value.to.splice(index, 1);
      } else {
        this.value.to.push(status);
      }
    }
  }
  getOptions() {
    var options: any = {
      showSearch: true,
      keyword: "",
      data: this.listUser,
    };
    return options;
  }
  getItemSelectedAssign() {
    if (!this.value.ItemSelected) {
      return [];
    }
    return this.value.ItemSelected;
  }
  SelectedUser(item) {
    if (!this.value.ItemSelected) {
      this.value.ItemSelected = [];
    }
    var index = this.value.ItemSelected.findIndex((x) => x.id_nv == item.id_nv);
    if (index >= 0) {
      this.value.ItemSelected.splice(index, 1);
    } else {
      this.value.ItemSelected.push(item);
    }
  }
}
