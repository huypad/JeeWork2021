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
  @Input() ID_projectteam : number = 0;
  @Input() ID_department : number = 0;
  @Input() type: number = 1;
  @Output() valueout = new EventEmitter<any>();
  showfullassign = false;
  listStatus: any = [];
  ListTaskParent: any = [];
  listUser: any = [];
  listPriority: any = [];
  ListDepartmentFolder: any = [];
  value: any = {};
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
    console.log("type:", this.type);
    console.log("team:", this.ID_projectteam);
    console.log("dept:", this.ID_department);
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
  LoadDataStatus(){
    if(this.ID_projectteam > 0){
      this.weWorkService.ListStatusDynamic(this.ID_projectteam).subscribe((res) => {
        if (res && res.status == 1) {
          this.listStatus = res.data;
          this.changeDetectorRefs.detectChanges();
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
      // load task parent
      this.automationService.ListTaskParent(this.ID_projectteam,false).subscribe((res) => {
        if (res && res.status == 1) {
          this.ListTaskParent = res.data;
          console.log(this.ListTaskParent);
          this.changeDetectorRefs.detectChanges();
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
    }else if(this.ID_department > 0 ){
      this.weWorkService.ListStatusDynamicByDepartment(this.ID_department).subscribe((res) => {
        if (res && res.status == 1) {
          this.listStatus = res.data;
          this.changeDetectorRefs.detectChanges();
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
      // load task parent
      this.automationService.ListTaskParent(this.ID_department,true).subscribe((res) => {
        if (res && res.status == 1) {
          this.ListTaskParent = res.data;
          console.log(this.ListTaskParent);
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
    this.LoadDataStatus();
    this.valueout.emit(this.value);
  }

  Ischecked(stt) {
    if(this.value && this.value.checked && stt ==this.value.checked )
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
  UpdateDateTask(value,isStart = true) {
    if(isStart){
      this.value.startdatetype = value;
      this.value.startdateValue = "";
    }else{
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
  ItemSelectedAssign(item, type) {
    if (type == 1) {
      if (!this.value.ItemSelectedAssign) {
        this.value.ItemSelectedAssign = [];
      }
      var index = this.value.ItemSelectedAssign.findIndex((x) => x.id_nv == item.id_nv);
      if (index >= 0) {
        this.value.ItemSelectedAssign.splice(index, 1);
      } else {
        this.value.ItemSelectedAssign.push(item);
      }
    } else if (type == 2) {
      if (!this.value.ItemRemoveAssign) {
        this.value.ItemRemoveAssign = [];
      }
      var index = this.value.ItemRemoveAssign.findIndex((x) => x.id_nv == item.id_nv);
      if (index >= 0) {
        this.value.ItemRemoveAssign.splice(index, 1);
      } else {
        this.value.ItemRemoveAssign.push(item);
      }
    } else if (type == 3) {
      if (!this.value.ItemReassign) {
        this.value.ItemReassign = [];
      }
      var index = this.value.ItemReassign.findIndex((x) => x.id_nv == item.id_nv);
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
  
  UpdateStatus(status){
    this.value.status = status;
    console.log(this.value.status)
  }
  checked = 0;
  SelectedStatus(status) {
    this.value.checked = status;
    this.checked = status.value;
  }
}
