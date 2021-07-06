import { LayoutUtilsService } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { ProjectsTeamService } from './../../projects-team/Services/department-and-project.service';
import { TemplateCenterService } from './../../template-center/template-center.service';
import { TranslateService } from '@ngx-translate/core';
import { ListDepartmentService } from './../../List-department/Services/List-department.service';
import { AutomationService } from './../automation.service';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';

@Component({
  selector: 'app-automation-tab-manage',
  templateUrl: './automation-tab-manage.component.html',
  styleUrls: ['./automation-tab-manage.component.scss']
})
export class AutomationTabManageComponent implements OnInit {

  listAutomation:any = [];

  constructor(
    private automationService:AutomationService,
    private layoutUtilsService: LayoutUtilsService,
    private projectsTeamService: ProjectsTeamService,
    private templatecenterService: TemplateCenterService,
    private translateService: TranslateService,
    private departmentServices: ListDepartmentService,
    private changeDetectorRefs: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    this.automationService.getAutomationList().subscribe( res => {
      if(res && res.status ==1){
        this.listAutomation = res.data;
        console.log(this.listAutomation);
      }else{
        this.layoutUtilsService.showError(res.error.message);
      }
    })
  }
  UpdateAutomation(item){
    console.log(item);
  }
}
