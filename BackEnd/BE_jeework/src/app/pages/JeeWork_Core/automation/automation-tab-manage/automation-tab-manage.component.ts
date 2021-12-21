import { QueryParamsModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-params.model';
import { QueryResultsModel } from './../../../../_metronic/jeework_old/core/_base/crud/models/query-models/query-results.model';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { ProjectsTeamService } from './../../projects-team/Services/department-and-project.service';
import { TemplateCenterService } from './../../template-center/template-center.service';
import { TranslateService } from '@ngx-translate/core';
import { ListDepartmentService } from './../../department/Services/List-department.service';
import { AutomationService } from './../automation.service';
import { Component, OnInit, ChangeDetectorRef, Output, EventEmitter, Input } from '@angular/core';
import { AutomationListModel } from '../automation-model/automation.model';

@Component({
  selector: 'app-automation-tab-manage',
  templateUrl: './automation-tab-manage.component.html',
  styleUrls: ['./automation-tab-manage.component.scss']
})
export class AutomationTabManageComponent implements OnInit {
  @Input() vitri: any = "";
  listAutomation: any = [];
  @Input() ID_projectteam: number = 0;
  @Input() ID_department: number = 0;
  @Output() selectedItem = new EventEmitter<any>();

  constructor(
    private automationService: AutomationService,
    private layoutUtilsService: LayoutUtilsService,
    private changeDetectorRefs: ChangeDetectorRef,
  ) { }

  ngOnInit(): void {
    const query = new QueryParamsModel(this.filterConfiguration());
    this.automationService.getAutomationList(query).subscribe(res => {
      if (res && res.status == 1) {
        this.listAutomation = res.data;
        this.changeDetectorRefs.detectChanges();
      } else {
        this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
      }
    })
  }

  filterConfiguration(): any {
    const filter: any = {};
    if (this.ID_projectteam > 0) filter.listid = this.ID_projectteam;//assignee
    if (this.ID_department > 0) filter.departmentid = this.ID_department;
    return filter;
  }
  UpdateAutomation(item = null) {
    this.selectedItem.emit(item);
  }
  Delete(item) {
    this.automationService.DeleteAutomation(item.rowid).subscribe(res => {
      if (res && res.status == 1) {
        this.ngOnInit();
      } else {
        this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
      }
    })
  }
  Update(item) {
    if (item.status == 1) {
      item.status = 0;
    } else {
      item.status = 1;
    }
    this.automationService.UpdateStatusAutomation(item.rowid).subscribe((res) => {
      if (res && res.status == 1) {
        this.ngOnInit();
        // this.layoutUtilsService.showActionNotification("cập nhật thành công");
      } else {
        this.ngOnInit();
        this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
      }
    });
  }

  getAutomationActive() {
    var x = this.listAutomation.filter(x => x.status == 1);
    return x.length;
  }
}
