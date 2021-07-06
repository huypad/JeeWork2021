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
export class AutomationTriggerStateConditionComponent implements OnInit, OnChanges {
  @Input() ID_projectteam : number = 0;
  @Input() ID_department : number = 0;
  @Input() type: number = 1;
  @Output() valueout = new EventEmitter<any>();
  value: any = [];
  listPriority: any = [];
  listStatus: any = [];
  listUser: any = [];
  constructor(
    private layoutUtilsService: LayoutUtilsService,
    private projectsTeamService: ProjectsTeamService,
    private templatecenterService: TemplateCenterService,
    private translateService: TranslateService,
    private departmentServices: ListDepartmentService,
    private changeDetectorRefs: ChangeDetectorRef,
    private weWorkService: WeWorkService
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
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });

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
    }else if(this.ID_department > 0 ){
      this.weWorkService.ListStatusDynamicByDepartment(this.ID_department).subscribe((res) => {
        if (res && res.status == 1) {
          this.listStatus = res.data;
          this.changeDetectorRefs.detectChanges();
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
    }
  }

  ngOnChanges(changes: SimpleChanges): void {
    this.LoadDataStatus();
    this.value = {};

    this.valueout.emit(this.value);

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
