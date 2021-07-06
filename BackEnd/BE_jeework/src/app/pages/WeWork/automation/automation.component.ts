import { AutomationService } from './automation.service';
import { PopoverContentComponent } from 'ngx-smart-popover';
import { ListDepartmentService } from './../List-department/Services/List-department.service';
import { TranslateService } from '@ngx-translate/core';
import { TemplateCenterService } from './../template-center/template-center.service';
import { ProjectsTeamService } from './../projects-team/Services/department-and-project.service';
import { LayoutUtilsService } from './../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Component, OnInit, ChangeDetectorRef, Inject, ViewChild } from '@angular/core';

@Component({
  selector: 'app-automation',
  templateUrl: './automation.component.html',
  styleUrls: ['./automation.component.scss']
})
export class AutomationComponent implements OnInit {
  // @ViewChild('popoverStatus', { static: true }) popoverStatus: PopoverContentComponent;
  // @ViewChild('popoverAction', { static: true }) popoverAction: PopoverContentComponent;
  // Triggerselect : any = {};
  // Actionselect : any = {};
  // ListEvent:any = [];
  // ListAction:any = [];
  // valueAction:any = [];
  ID_department = 0;
  ID_projectteam = 0;
  isEditAuto = true;
  tab :any = "brower";
  constructor(
    public dialogRef: MatDialogRef<AutomationComponent>,
    private layoutUtilsService: LayoutUtilsService,
    private projectsTeamService: ProjectsTeamService,
    private templatecenterService: TemplateCenterService,
    private translateService: TranslateService,
    private departmentServices: ListDepartmentService,
    private changeDetectorRefs: ChangeDetectorRef,
    private automationService: AutomationService,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) { }

  ngOnInit(): void {
    // this.LoadListAutomation();
    this.LoadTeam();
  }
  LoadTeam(){
    console.log(this.data.item)
    if (this.data.item.type == 1 || this.data.item.type == 2) {
      // department or folder
      this.departmentServices.DeptDetail(this.data.item.id).subscribe((res) => {
        if (res && res.status == 1) {
          this.ID_department = res.data.id_row;
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
      // this.ItemNewSaveAs
    } else if (this.data.item.type == 3) {
      // project
      this.projectsTeamService
        .DeptDetail(this.data.item.id)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.ID_department = res.data.id_department;
            this.ID_projectteam = res.data.id_row;
          } else {
            this.layoutUtilsService.showError(res.error.message);
          }
        });
    }
  }
  // LoadListAutomation(){
  //   this.automationService.getAutomationEventlist().subscribe(
  //     res => {
  //       if(res && res.status == 1){
  //         this.ListEvent = res.data;
  //         this.Triggerselect = this.ListEvent[0];
  //       }else{
  //         this.layoutUtilsService.showError(res.error.message);
  //       }
  //     }
  //   );
  //   this.automationService.getAutomationActionList().subscribe(
  //     res => {
  //       if(res && res.status == 1){
  //         this.ListAction = res.data;
  //         this.Actionselect = this.ListAction[9];
  //       }else{
  //         this.layoutUtilsService.showError(res.error.message);
  //       }
  //     }
  //   );
  // }
  // ChangeTypeTrigger(item){
  //   this.Triggerselect = item;
  //   this.popoverStatus.hide();
  // }
  // ChangeTypeAction(item){
  //   this.Actionselect = item;
  //   this.popoverAction.hide();
  // }

  // OnSubmit(){
  //   console.log(this.valueAction);
  // }

  // GetvalueAction($event){
  //   this.valueAction = $event;
  //   console.log(this.valueAction);
  // }
}
