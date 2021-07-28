import { filter } from "rxjs/operators";
import { WeWorkService } from "./../../services/wework.services";
import { ListDepartmentService } from "./../../List-department/Services/List-department.service";
import { TranslateService } from "@ngx-translate/core";
import { TemplateCenterService } from "./../../template-center/template-center.service";
import { ProjectsTeamService } from "./../../projects-team/Services/department-and-project.service";
import { LayoutUtilsService } from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  Input,
  OnChanges,
  SimpleChanges,
  EventEmitter,
  Output,
} from "@angular/core";
import { AutomationService } from "../automation.service";

@Component({
  selector: "app-automation-action-field",
  templateUrl: "./automation-action-field.component.html",
  styleUrls: ["./automation-action-field.component.scss"],
})
export class AutomationActionFieldComponent implements OnInit, OnChanges {
  @Input() ID_projectteam: number = 0;
  @Input() ID_department: number = 0;
  @Input() Actionid: number = 1;
  @Input() dataAction: any = {};
  @Output() valueout = new EventEmitter<any>();
  showfullassign = false;
  isMapdata = true;
  listStatus: any = [];
  ListTaskParent: any = [];
  listUser: any = [];
  listPriority: any = [];
  ListDepartmentFolder: any = [];
  value: any = {};
  data_actions: any = {};
  OPTIONDATE: any = [
    {
      value: "1",
      name: "Sau 1 số ngày xảy ra điều kiện",
    },
    {
      value: "2",
      name: "Ngay ngày xảy ra điều kiện",
    },
    {
      value: "3",
      name: "Chọn ngày cụ thể",
    },
  ];
  constructor(
    private layoutUtilsService: LayoutUtilsService,
    private projectsTeamService: ProjectsTeamService,
    private templatecenterService: TemplateCenterService,
    private translateService: TranslateService,
    private departmentServices: ListDepartmentService,
    private automationService: AutomationService,
    private changeDetectorRefs: ChangeDetectorRef,
    private weWorkService: WeWorkService
  ) {
    this.listPriority = weWorkService.list_priority;
  }

  ngOnInit(): void {
    
    // list Status all
    this.LoadDataStatus();
    // list account user
    this.weWorkService.list_account({}).subscribe((res) => {
      if (res && res.status == 1) {
        this.listUser = res.data;
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
    // list lựa chọn dự án ( cây ở bên template center có sẵn )
    this.templatecenterService.LiteDepartmentFolder().subscribe((res) => {
      if (res && res.status == 1) {
        this.ListDepartmentFolder = res.data;
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
    // list Task (để lựa chọn cv con)

    //list priority

    // lưu list tags theo department để insert tags
  }
  LoadDataStatus() {
    if (this.ID_projectteam > 0) {
      this.weWorkService
        .ListStatusDynamic(this.ID_projectteam)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.listStatus = res.data;
            if (this.Actionid == 9) this.LoadDataAction9();
            if (this.Actionid == 2 || this.Actionid == 3) this.LoadStatusTask();
            this.changeDetectorRefs.detectChanges();
          } else {
            this.layoutUtilsService.showError(res.error.message);
          }
        });
      // load task parent
      this.automationService
        .ListTaskParent(this.ID_projectteam, false)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.ListTaskParent = res.data;            
            console.log('parent: ',this.ListTaskParent);
            this.changeDetectorRefs.detectChanges();
          } else {
            this.layoutUtilsService.showError(res.error.message);
          }
        });
    } else if (this.ID_department > 0) {
      this.weWorkService
        .ListStatusDynamicByDepartment(this.ID_department)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.listStatus = res.data;
            if (this.Actionid == 9) this.LoadDataAction9();
            if (this.Actionid == 2 || this.Actionid == 3) this.LoadStatusTask();
            this.changeDetectorRefs.detectChanges();
          } else {
            this.layoutUtilsService.showError(res.error.message);
          }
        });
      // load task parent
      this.automationService
        .ListTaskParent(this.ID_department, true)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.ListTaskParent = res.data;
            console.log('parent: ',this.ListTaskParent);
            this.changeDetectorRefs.detectChanges();
          } else {
            this.layoutUtilsService.showError(res.error.message);
          }
        });
    }
  }

  selectedParent(value) {
    console.log(value);
  }

  ngOnChanges(changes: SimpleChanges): void {
    this.checked = 0;
    this.value = {};
    if(this.dataAction){
      if(this.dataAction.data_actions && this.dataAction.data_actions[0]){
        this.data_actions = this.dataAction.data_actions[0];
        this.MapvalueEdit();
      }
    }
    this.LoadDataStatus();
    this.valueout.emit(this.value);
  }
  MapvalueEdit() {

    // Action ID
    switch (this.Actionid) {
      case 1: // assign
        {
          this.MapUser();
        }
        break;
      case 2: // 2 = task ; 3 = subtask ;
      case 3:
        {
          if (this.data_actions && this.data_actions.data_task && this.isMapdata) {
            this.isMapdata = false;
            var dataTask = this.data_actions.data_task;
            this.value.taskid = dataTask.rowid;
            this.value.taskname = dataTask.title;
            this.value.id_project_team = dataTask.id_project_team;
            this.projectsTeamService
              .DeptDetail(this.value.id_project_team)
              .subscribe((res) => {
                if (res && res.status == 1) {
                  this.value.title_project_team = res.data.title;
                } else {
                  this.layoutUtilsService.showError(res.error.message);
                }
              });
            this.value.status = {};
            this.value.status.id_row = dataTask.status;
            this.LoadStatusTask();
            //priority
            this.value.to = dataTask.priority;
            if(+dataTask.deadline_type > 0){
              this.value.startdatetype = +dataTask.deadline_type;
              this.value.startdateValue = dataTask.deadline;
            }
            if(+dataTask.startdate_type > 0){
              this.value.startdatetype = +dataTask.startdate_type;
              this.value.startdateValue = dataTask.start_date;
            }
            if(dataTask.users && dataTask.users.length > 0){
              dataTask.users.forEach(element => {
                element.id_nv = element.id_user;
                this.ItemSelectedAssign(element,1)
              });
            }
          }
        }
        break;
      case 6: //
      case 13:
      case 14:
        {
          this.value.id_project_team = +this.data_actions.value;
          this.projectsTeamService
            .DeptDetail(this.value.id_project_team)
            .subscribe((res) => {
              if (res && res.status == 1) {
                this.value.title_project_team = res.data.title;
                this.changeDetectorRefs.detectChanges();
              } else {
                this.layoutUtilsService.showError(res.error.message);
              }
            });
        }
        break;
      case 4: //comment
        {
          this.value.data = this.data_actions.value;
        }
        break;
      case 9: // status
        {
          this.LoadDataAction9();
        }
        break;
      case 11:
      case 12:
        {
          console.log(this.data_actions.value);
          var list = this.data_actions.value.split(';');
          this.value.datetype=list[0];
          this.value.dateValue=list[1];
        }
        break;
    }
  }

  MapUser(){
    if(this.listUser && this.listUser.length > 0){
      this.dataAction.data_actions.forEach(element => {
        var listUser = element.value.split(',');
        listUser.forEach(UserID => {
          var x = this.listUser.find(x=>x.id_nv == UserID);
          if(x){
            this.ItemSelectedAssign(x,element.actionid);
          }
        });
      });
      
    }else{
      setTimeout(() => {
        this.MapUser();
      }, 500);
    }
  }
  LoadStatusTask() {
    if(!this.value.status) return;
    if (!(this.value.status.id_row > 0) || this.listStatus.length == 0) return;

    var item = this.listStatus.find(
      (x) => x.id_row == this.value.status.id_row
    );
    if (item) {
      this.UpdateStatus(item);
    }
  }

  LoadDataAction9() {
    if (this.listStatus.length == 0) return;
    if (this.Actionid != this.dataAction.actionid || this.Actionid != 9) return;
    var x = this.listStatus.find((x) => +x.id_row == +this.dataAction.data);
    if (x) {
      this.SelectedStatus(x);
    }
  }

  Ischecked(stt) {
    if (this.value && this.value.checked && stt == this.value.checked)
      return true;
    return false;
  }

  getPriority(id) {
    var item = this.listPriority.find((x) => x.value == id);
    if (item) return item;
    return id;
  }
  updatePriority(value, isFrom = true) {
    if (isFrom) {
      this.value.from = value;
    } else {
      this.value.to = value;
    }
  }
  updateOPTIONDATE(value) {
    this.value.datetype = value;
    this.value.dateValue = "";
  }
  UpdateDateTask(value, isStart = true) {
    if (isStart) {
      this.value.startdatetype = value;
      this.value.startdateValue = "";
    } else {
      this.value.duedatetype = value;
      this.value.duedateValue = "";
    }
  }
  TitleOptionDate(value) {
    var x = this.OPTIONDATE.find((x) => x.value == value);
    if (x) {
      return x.name;
    }
    return "Chọn";
  }
  SelectedProject(project) {
    this.value.id_project_team = project.id_row;
    this.value.title_project_team = project.title;
  }
  SelectedTask(task) {
    this.value.id_task = task.id_row;
    this.value.title_task = task.title;
  }
  getOptions() {
    var options: any = {
      showSearch: true,
      keyword: "",
      data: this.listUser,
    };
    return options;
  }
  ItemSelectedAssign(item, Actionid) {
    if (Actionid == 1) { // giao
      if (!this.value.ItemSelectedAssign) {
        this.value.ItemSelectedAssign = [];
      }
      var index = this.value.ItemSelectedAssign.findIndex(
        (x) => x.id_nv == item.id_nv
      );
      if (index >= 0) {
        this.value.ItemSelectedAssign.splice(index, 1);
      } else {
        this.value.ItemSelectedAssign.push(item);
      }
    } else if (Actionid == 2) { // bỏ 
      if (!this.value.ItemRemoveAssign) {
        this.value.ItemRemoveAssign = [];
      }
      var index = this.value.ItemRemoveAssign.findIndex(
        (x) => x.id_nv == item.id_nv
      );
      if (index >= 0) {
        this.value.ItemRemoveAssign.splice(index, 1);
      } else {
        this.value.ItemRemoveAssign.push(item);
      }
    } else if (Actionid == 4) { // chỉ định lại
      if (!this.value.ItemReassign) {
        this.value.ItemReassign = [];
      }
      var index = this.value.ItemReassign.findIndex(
        (x) => x.id_nv == item.id_nv
      );
      if (index >= 0) {
        this.value.ItemReassign.splice(index, 1);
      } else {
        this.value.ItemReassign.push(item);
      }
    }
  }
  getItemSelectedAssign() {
    if (!this.value.ItemSelectedAssign) {
      return [];
    }
    return this.value.ItemSelectedAssign;
  }
  getItemRemoveAssign() {
    if (!this.value.ItemRemoveAssign) {
      return [];
    }
    return this.value.ItemRemoveAssign;
  }
  getItemReassign() {
    if (!this.value.ItemReassign) {
      return [];
    }
    return this.value.ItemReassign;
  }

  UpdateStatus(status) {
    this.value.status = status;
    console.log(this.value.status);
  }
  checked = 0;
  SelectedStatus(status) {
    this.value.checked = status;
    this.checked = status.value;
  }
}
