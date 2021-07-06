import { Auto_Task_Model } from './../automation-model/automation.model';
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
} from "@angular/core";
import { AutomationListModel, Automation_SubAction_Model } from "../automation-model/automation.model";
import { WorkUserModel } from '../../work/work.model';

@Component({
  selector: "app-edit-automation",
  templateUrl: "./edit-automation.component.html",
  styleUrls: ["./edit-automation.component.scss"],
})
export class EditAutomationComponent implements OnInit {
  @Input() ID_projectteam : number = 0;
  @Input() ID_department : number = 0;
  @ViewChild("popoverStatus", { static: true })
  popoverStatus: PopoverContentComponent;
  @ViewChild("popoverAction", { static: true })
  popoverAction: PopoverContentComponent;
  Triggerselect: any = {};
  Actionselect: any = {};
  ListEvent: any = [];
  ListAction: any = [];
  valueAction: any = [];
  valueEvent: any = [];
  constructor(
    private layoutUtilsService: LayoutUtilsService,
    private projectsTeamService: ProjectsTeamService,
    private templatecenterService: TemplateCenterService,
    private translateService: TranslateService,
    private departmentServices: ListDepartmentService,
    private changeDetectorRefs: ChangeDetectorRef,
    private automationService: AutomationService,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {}

  ngOnInit(): void {
    this.LoadListAutomation();
  }
  LoadListAutomation() {
    this.automationService.getAutomationEventlist().subscribe((res) => {
      if (res && res.status == 1) {
        this.ListEvent = res.data;
        this.Triggerselect = this.ListEvent[0].rowid;
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
    this.automationService.getAutomationActionList().subscribe((res) => {
      if (res && res.status == 1) {
        this.ListAction = res.data;
        this.Actionselect = this.ListAction[9].rowid;
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }
  getTitleTrigger() {
    if(this.Triggerselect > 0){
      var x = this.ListEvent.find(x=>x.rowid == this.Triggerselect);
      if(x){
        return x.title;
      }
    }
    return "";
  }
  getTitleAction() {
    if(this.Actionselect > 0){
      var x = this.ListAction.find(x=>x.rowid == this.Actionselect);
      if(x){
        return x.actionname;
      }
    }
    return "";
  }

  OnSubmit() {
    console.log('Event: ',this.valueEvent);
    console.log('Action: ',this.valueAction);
    console.log('ID_projectteam: ',this.ID_projectteam);
    console.log('ID_department: ',this.ID_department);

    const _item = new AutomationListModel();
    _item.clear();
    _item.title = "Khi " + this.getTitleTrigger() + ' thì ' + this.getTitleAction();
    _item.departmentid = ''+this.ID_department;
    _item.eventid = this.Triggerselect;
    _item.actionid = this.Actionselect;
    _item.status = "1";
    // Event ID
    if(this.Triggerselect == 6 || this.Triggerselect== 5 ){ // assign // 56609,17198
      var listID = [];
      this.valueEvent.ItemSelected.forEach(element => {
        listID.push(element.id_nv);
      });
      _item.condition = listID.join();
    }else if(this.Triggerselect == 1){ // status // Lưu Condition theo định dang From:x,y;To:z,k. Trong đó x,y,z,k là statusid (để trống là any)
      var listfrom = [];
      this.valueEvent.from.forEach(element => {
        listfrom.push(element.id_row);
      });
      var listto = [];
      this.valueEvent.to.forEach(element => {
        listto.push(element.id_row);
      });
      _item.condition = "From:"+ listfrom.join() +";To:"+listto.join();
    }else if(this.Triggerselect == 2){ // priority // Lưu Condition theo định dang From:x,y;To:z,k. Trong đó x,y,z,k là statusid (để trống là any)
      _item.condition = "From:"+ this.valueEvent.from +";To:"+ this.valueEvent.to;
    }else{ // các trường hợp còn lại

    }
    // Action ID
    switch (this.Actionselect){
      case 1: // assign
        {
          const subaction = new Array<Automation_SubAction_Model>();
          if(this.valueAction.ItemSelectedAssign && this.valueAction.ItemSelectedAssign.length >0) { // Assign subaction 1
            var listID = [];
            this.valueAction.ItemSelectedAssign.forEach(element => {
              listID.push(element.id_nv);
            });
            const itemsub = new Automation_SubAction_Model();
            itemsub.clear();
            // itemsub.autoid =
            itemsub.subactionid = '1';
            itemsub.value = listID.join();
            subaction.push(itemsub);
          }
          if(this.valueAction.ItemRemoveAssign && this.valueAction.ItemRemoveAssign.length >0) { // RemoveAssign subaction 2
            var listID = [];
            this.valueAction.ItemRemoveAssign.forEach(element => {
              listID.push(element.id_nv);
            });
            const itemsub = new Automation_SubAction_Model();
            itemsub.clear();
            // itemsub.autoid =
            itemsub.subactionid = '2';
            itemsub.value = listID.join();
            subaction.push(itemsub);
          }
          if(this.valueAction.ItemReassign && this.valueAction.ItemReassign.length >0) { // Reassign subaction 3
            var listID = [];
            this.valueAction.ItemReassign.forEach(element => {
              listID.push(element.id_nv);
            });
            const itemsub = new Automation_SubAction_Model();
            itemsub.clear();
            // itemsub.autoid =
            itemsub.subactionid = '3';
            itemsub.value = listID.join();
            subaction.push(itemsub);
          }
          if(this.valueAction.removeall ) { // Remove all subaction 4
            const itemsub = new Automation_SubAction_Model();
            itemsub.clear();
            // itemsub.autoid =
            itemsub.subactionid = '4';
            itemsub.value = 'true';
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
          if(this.valueAction.taskname){
            task.title = this.valueAction.taskname;
          }else{
            this.layoutUtilsService.showError("Tên công việc không được để trống.");
            return;
          }
          if(this.Actionselect==3){
            if(this.valueAction.id_task)
            {
              task.id_parent = this.valueAction.id_task;
            }else {
              this.layoutUtilsService.showError("Bắt buộc nhập công việc thiết lập công việc con.");
              return;
            }
          }
          task.id_project_team = this.valueAction.id_project_team?this.valueAction.id_project_team:0;
          task.priority = this.valueAction.to?this.valueAction.to:0;
          if(this.valueAction.startdatetype)
          {
            task.startdate_type = this.valueAction.startdatetype;
            if(+this.valueAction.startdatetype==3){
              task.start_date = this.f_convertDate(this.valueAction.startdateValue);
            }else{
              task.start_date = this.valueAction.startdateValue?this.valueAction.startdateValue:0;
            }
            
          }
          if(this.valueAction.duedatetype)
          {
            task.deadline_type = this.valueAction.duedatetype;
            if(+this.valueAction.duedatetype==3){
              task.deadline = this.f_convertDate(this.valueAction.duedateValue);
            }else{
              task.deadline = this.valueAction.duedateValue?this.valueAction.duedateValue:0;
            }
            
          }
          if(this.valueAction.status)
            task.status = this.valueAction.status.id_row
          const listUser = new Array<WorkUserModel>();
          if(this.valueAction.ItemSelectedAssign && this.valueAction.ItemSelectedAssign.length > 0){
            this.valueAction.ItemSelectedAssign.forEach(element => {
              const workuser = new WorkUserModel();
              workuser.clear();
              workuser.loai = 1;
              workuser.id_user = element.id_nv;
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
          _item.data = ''+this.valueAction.id_project_team;
        }
        break;
      case 4: //comment
        {
          _item.data = this.valueAction.data;
        }
        break;
      case 9:
        {
          if(this.valueAction.checked){
            _item.data = this.valueAction.checked.id_row;
          }          
        }
        break;
      case 10:
        {
          _item.data = ''+this.valueAction.to;
        }
        break; 
    }
    // if(this.Actionselect == 6 || this.Actionselect== 5 ){ // assign

    // }else if(this.Actionselect == 1){ // status

    // }else if(this.Actionselect == 2){ // priority

    // }else{ // các trường hợp còn lại

    // }

    console.log('Automation: ', _item);

    this.automationService.InsertAutomation(_item).subscribe(
      res => {
        if(res && res.status == 1){
          console.log(res);
          this.layoutUtilsService.showError('Thêm mới thành công');
        }
        else{
          this.layoutUtilsService.showError(res.error.message);
        }
      }
    );

  }

  GetvalueAction($event) {
    this.valueAction = $event;
    console.log(this.valueAction);
  }

  GetvalueEvent($event) {
    this.valueEvent = $event;
    console.log(this.valueEvent);
  }

  f_convertDate(v: any = "") {
    let a = v === "" ? new Date() : new Date(v);
    return (
      a.getFullYear() +
      "-" +
      ("0" + (a.getMonth() + 1)).slice(-2) +
      "-" +
      ("0" + a.getDate()).slice(-2) +
      "T00:00:00.0000000"
    );
  }
}
