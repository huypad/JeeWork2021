import { Auto_Task_Model } from "./../automation-model/automation.model";
import { AutomationService } from "./../automation.service";
import { PopoverContentComponent } from "ngx-smart-popover";
import { ListDepartmentService } from "./../../List-department/Services/List-department.service";
import { TranslateService } from "@ngx-translate/core";
import { TemplateCenterService } from "./../../template-center/template-center.service";
import { ProjectsTeamService } from "./../../projects-team/Services/department-and-project.service";
import { LayoutUtilsService } from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  Inject,
  ViewChild,
  Input,
  OnChanges,
  Output,
  EventEmitter,
} from "@angular/core";
import {
  AutomationListModel,
  Automation_SubAction_Model,
} from "../automation-model/automation.model";
import { WorkUserModel } from "../../work/work.model";

@Component({
  selector: "app-edit-automation",
  templateUrl: "./edit-automation.component.html",
  styleUrls: ["./edit-automation.component.scss"],
})
export class EditAutomationComponent implements OnInit, OnChanges {
  @Input() ID_projectteam: number = 0;
  @Input() ID_department: number = 0;
  @Input() dataEdit: any = {};
  @Input() isBack: boolean = false;
  @Output() eventClose = new EventEmitter<any>();
  @ViewChild("popoverStatus", { static: true })
  popoverStatus: PopoverContentComponent;
  @ViewChild("popoverAction", { static: true })
  popoverAction: PopoverContentComponent;
  Eventid: any = {};
  Actionid: any = {};
  ListEvent: any = [];
  ListAction: any = [];
  valueAction: any = [];
  valueEvent: any = [];
  listActions: any = [];
  idAction = 0;
  constructor(
    private layoutUtilsService: LayoutUtilsService,
    private changeDetectorRefs: ChangeDetectorRef,
    private automationService: AutomationService,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) { }

  ngOnInit(): void {
    this.LoadListAutomation();
    this.loadAction();
  }

  ngOnChanges(): void {
    if (this.dataEdit.rowid > 0) {
      this.Eventid = this.dataEdit.eventid;
      this.Actionid = this.dataEdit.actionid;
      setTimeout(() => {
        if (this.listActions[0]) {
          this.listActions[0].Actionid = this.Actionid;
          this.listActions[0].dataEdit = this.dataEdit;
        }
      }, 100);
    }
    this.LoadListAutomation();
  }
  LoadListAutomation() {
    this.automationService.getAutomationEventlist().subscribe((res) => {
      if (res && res.status == 1) {
        this.ListEvent = res.data;
        if (!this.dataEdit.eventid) this.Eventid = this.ListEvent[0].rowid;
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
    this.automationService.getAutomationActionList().subscribe((res) => {
      if (res && res.status == 1) {
        this.ListAction = res.data;
        if (!this.dataEdit.actionid) this.Actionid = this.ListAction[9].rowid;
        if (this.ID_projectteam == 0) {
          this.ListAction.forEach(element => {
            if ( element.rowid == 7 ) element.disabled = true;
          })
        }
        // disabled webhook
        this.ListAction.forEach(element => {
          if ( element.rowid == 15 ) element.disabled = true;
        })
        this.changeDetectorRefs.detectChanges();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }
  getTitleTrigger() {
    if (this.Eventid > 0) {
      var x = this.ListEvent.find((x) => x.rowid == this.Eventid);
      if (x) {
        return x.title;
      }
    }
    return "";
  }
  getTitleAction(Actionid) {
    if (Actionid > 0) {
      var x = this.ListAction.find((x) => x.rowid == Actionid);
      if (x) {
        return x.actionname;
      }
    }
    return "";
  }
  getListTitleAction() {
    var lst = [];
    this.listActions.forEach(element => {
      lst.push(this.getTitleAction(element.Actionid));
    });
    if (lst.length > 0) {
      return lst.join('\r\n');
    }
    return "";
  }
  OnSubmit(isclose = false) {
    const ListAuto = new Array<AutomationListModel>();
    this.listActions.forEach(element => {
      const _item = new AutomationListModel();
      _item.clear();
      if (this.dataEdit.rowid > 0) {
        _item.rowid = this.dataEdit.rowid;
      }
      _item.title =
        "Khi " + this.getTitleTrigger() + " thì " + this.getTitleAction(element.Actionid);
      _item.departmentid = "" + this.ID_department;
      _item.eventid = this.Eventid;
      _item.actionid = element.Actionid;
      _item.status = "1";
      // Event ID
      if (this.Eventid == 6 || this.Eventid == 5) {
        // assign // 56609,17198
        var listID = [];
        if (this.valueEvent.ItemSelected) {
          this.valueEvent.ItemSelected.forEach((element) => {
            listID.push(element.id_nv);
          });
          _item.condition = listID.join();
        } else {
          _item.condition = 'any';
        }
      } else if (this.Eventid == 1) {
        // status // Lưu Condition theo định dang From:x,y;To:z,k. Trong đó x,y,z,k là statusid (để trống là any)
        var listfrom = [];
        if (this.valueEvent.from && this.valueEvent.from.length > 0) {
          this.valueEvent.from.forEach((element) => {
            listfrom.push(element.id_row);
          });
        } else {
          listfrom.push('any');
        }
        var listto = [];
        if (this.valueEvent.to && this.valueEvent.to.length > 0) {
          this.valueEvent.to.forEach((element) => {
            listto.push(element.id_row);
          });
        } else {
          listto.push('any');
        }
        _item.condition = "From:" + listfrom.join() + ";To:" + listto.join();
      } else if (this.Eventid == 2) {
        // priority // Lưu Condition theo định dang From:x,y;To:z,k. Trong đó x,y,z,k là statusid (để trống là any)
        if (!this.valueEvent.from) this.valueEvent.from = 'any';
        if (!this.valueEvent.to) this.valueEvent.to = 'any';
        _item.condition =
          "From:" + this.valueEvent.from + ";To:" + this.valueEvent.to;
      } else {
        // các trường hợp còn lại
      }
      // Action ID
      switch (element.Actionid) {
        case 1: // assign
          {
            const subaction = new Array<Automation_SubAction_Model>();
            if (
              element.data.ItemSelectedAssign &&
              element.data.ItemSelectedAssign.length > 0
            ) {
              // Assign subaction 1
              var listID = [];
              element.data.ItemSelectedAssign.forEach((element) => {
                listID.push(element.id_nv);
              });
              const itemsub = new Automation_SubAction_Model();
              itemsub.clear();
              // itemsub.autoid =
              itemsub.subactionid = "1";
              itemsub.value = listID.join();
              subaction.push(itemsub);
            }
            if (
              element.data.ItemRemoveAssign &&
              element.data.ItemRemoveAssign.length > 0
            ) {
              // RemoveAssign subaction 2
              var listID = [];
              element.data.ItemRemoveAssign.forEach((element) => {
                listID.push(element.id_nv);
              });
              const itemsub = new Automation_SubAction_Model();
              itemsub.clear();
              // itemsub.autoid =
              itemsub.subactionid = "2";
              itemsub.value = listID.join();
              subaction.push(itemsub);
            }
            if (
              element.data.ItemReassign &&
              element.data.ItemReassign.length > 0
            ) {
              // Reassign subaction 4
              var listID = [];
              element.data.ItemReassign.forEach((element) => {
                listID.push(element.id_nv);
              });
              const itemsub = new Automation_SubAction_Model();
              itemsub.clear();
              itemsub.subactionid = "4";
              itemsub.value = listID.join();
              subaction.push(itemsub);
            }
            if (element.data.removeall) {
              // Remove all subaction 3
              const itemsub = new Automation_SubAction_Model();
              itemsub.clear();
              // itemsub.autoid =
              itemsub.subactionid = "3";
              itemsub.value = "true";
              subaction.push(itemsub);
            }
            _item.subaction = subaction;
          }
          break;
        case 7: // Change tags
          {
            const subaction = new Array<Automation_SubAction_Model>();
            if (
              element.data.Tags &&
              element.data.Tags.length > 0
            ) {
              // Assign subaction 1
              var listID = [];
              element.data.Tags.forEach((element) => {
                listID.push(element.id_row);
              });
              const itemsub = new Automation_SubAction_Model();
              itemsub.clear();
              // itemsub.autoid =
              itemsub.subactionid = "5";
              itemsub.value = listID.join();
              subaction.push(itemsub);
            }
            if (
              element.data.Tags2 &&
              element.data.Tags2.length > 0
            ) {
              // RemoveAssign subaction 2
              var listID = [];
              element.data.Tags2.forEach((element) => {
                listID.push(element.id_row);
              });
              const itemsub = new Automation_SubAction_Model();
              itemsub.clear();
              // itemsub.autoid =
              itemsub.subactionid = "6";
              itemsub.value = listID.join();
              subaction.push(itemsub);
            }
            _item.subaction = subaction;
          }
          break;
        case 2: // 2 = task ; 3 = subtask ;
        case 3:
          {
            const task = new Auto_Task_Model();
            task.clear();
            if (element.data.taskid > 0) {
              task.id_row = element.data.taskid;
            }
            if (element.data.taskname) {
              task.title = element.data.taskname;
            } else {
              this.layoutUtilsService.showError(
                "Tên công việc không được để trống."
              );
              return;
            }
            if (this.Actionid == 3) {
              if (element.data.id_task) {
                task.id_parent = element.data.id_task;
              } else {
                this.layoutUtilsService.showError(
                  "Bắt buộc nhập công việc thiết lập công việc con."
                );
                return;
              }
            }
            task.id_project_team = element.data.id_project_team
              ? element.data.id_project_team
              : 0;
            task.priority = element.data.to ? element.data.to : 0;
            if (element.data.startdatetype) {
              task.startdate_type = element.data.startdatetype;
              if (+element.data.startdatetype == 3) {
                task.start_date = this.f_convertDate(
                  element.data.startdateValue
                );
              } else {
                task.start_date = element.data.startdateValue
                  ? element.data.startdateValue
                  : 0;
              }
            }
            if (element.data.duedatetype) {
              task.deadline_type = element.data.duedatetype;
              if (+element.data.duedatetype == 3) {
                task.deadline = this.f_convertDate(element.data.duedateValue);
              } else {
                task.deadline = element.data.duedateValue
                  ? element.data.duedateValue
                  : 0;
              }
            }
            if (element.data.status)
              task.status = element.data.status.id_row;
            const listUser = new Array<WorkUserModel>();
            if (
              element.data.ItemSelectedAssign &&
              element.data.ItemSelectedAssign.length > 0
            ) {
              element.data.ItemSelectedAssign.forEach((element) => {
                const workuser = new WorkUserModel();
                workuser.clear();
                workuser.loai = 1;
                workuser.id_user = element.id_nv;
                // if(task.id_row > 0)
                // workuser.id_work = task.id_row;
                listUser.push(workuser);
              });
              task.users = listUser;
            }

            _item.task = new Array<Auto_Task_Model>();
            _item.task.push(task);
          }
          break;
        case 6: //
        case 13:
        case 14:
          {
            _item.data = "" + element.data.id_project_team;
          }
          break;
        case 4: //comment
        case 8: //estimate
          {
            _item.data = element.data.data;
          }
          break;
        case 9:
          {
            if (element.data.checked) {
              _item.data = element.data.checked.id_row;
            }
          }
          break;
        case 10:
          {
            _item.data = "" + element.data.to;
          }
          break;
        case 11:
        case 12:
          {
            if (+element.data.datetype == 1) {
              _item.data = element.data.datetype + ";" + element.data.dateValue;
            } else if (+element.data.datetype == 2) {
              _item.data = element.data.datetype + ";0";
            } else {
              _item.data = element.data.datetype + ";" + this.f_convertDate(element.data.dateValue);
            }
          }
          break;
      }
      ListAuto.push(_item);
    });

    if (ListAuto[0].rowid > 0) {
      this.UpdateAuto(ListAuto[0], isclose);
    } else {
      this.CreateAuto(ListAuto, isclose);
    };

  }

  CreateAuto(_item, isclose) {
    this.automationService.InsertAutomation(_item).subscribe((res) => {
      if (res && res.status == 1) {
        this.layoutUtilsService.showActionNotification("Thêm mới thành công");
        this.eventClose.emit(isclose)
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }
  UpdateAuto(_item, isclose) {
    this.automationService.UpdateAutomation(_item).subscribe((res) => {
      if (res && res.status == 1) {
        this.layoutUtilsService.showActionNotification("Thêm mới thành công");
        this.eventClose.emit(isclose)
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  GetvalueAction($event, index) {
    this.valueAction = $event;
    this.listActions[index].data = this.valueAction;
  }

  GetvalueEvent($event) {
    this.valueEvent = $event;
  }

  f_convertDate(v: any = "") {
    let a = v === "" ? new Date() : new Date(v);
    return (
      a.getFullYear() +
      "-" +
      ("0" + (a.getMonth() + 1)).slice(-2) +
      "-" +
      ("0" + a.getDate()).slice(-2)
    );
  }

  close(value = false) {
    this.eventClose.emit(value);
  }
  loadAction() {
    this.listActions.push({ id: this.idAction });
    this.idAction++;
  }
}
