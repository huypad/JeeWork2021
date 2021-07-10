import { AutomationService } from "./automation.service";
import { PopoverContentComponent } from "ngx-smart-popover";
import { ListDepartmentService } from "./../List-department/Services/List-department.service";
import { TranslateService } from "@ngx-translate/core";
import { TemplateCenterService } from "./../template-center/template-center.service";
import { ProjectsTeamService } from "./../projects-team/Services/department-and-project.service";
import { LayoutUtilsService } from "./../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
import {
  Component,
  OnInit,
  ChangeDetectorRef,
  Inject,
  ViewChild,
} from "@angular/core";

@Component({
  selector: "app-automation",
  templateUrl: "./automation.component.html",
  styleUrls: ["./automation.component.scss"],
})
export class AutomationComponent implements OnInit {
  
  ID_department = 0;
  ID_projectteam = 0;
  isEditAuto = false;
  tab: any = "browse";
  dataEdit: any = {};
  locationtitle: any = {};
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
  ) {}

  ngOnInit(): void {
    // this.LoadListAutomation();
    this.LoadTeam();
  }
  LoadTeam() {
    console.log(this.data.item);
    if (this.data.item.type == 1 || this.data.item.type == 2) {
      // department or folder
      this.departmentServices.DeptDetail(this.data.item.id).subscribe((res) => {
        if (res && res.status == 1) {
          this.ID_department = res.data.id_row;
          this.locationtitle.title = res.data.title;
          if(res.data.ParentID > 0){
            this.locationtitle.folder = res.data.title;
            this.departmentServices.DeptDetail(res.data.ParentID).subscribe((res) => {
              if (res && res.status == 1) {
                this.locationtitle.department = res.data.title;
              } else {
                this.layoutUtilsService.showError(res.error.message);
              }
            });
          }else{
            this.locationtitle.department = res.data.title;
          }
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
    } else if (this.data.item.type == 3) {
      // project
      this.projectsTeamService
        .DeptDetail(this.data.item.id)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.ID_department = res.data.id_department;
            this.ID_projectteam = res.data.id_row;
            this.locationtitle.title = res.data.title;
            this.locationtitle.project = res.data.title;
            this.departmentServices.DeptDetail(this.ID_department).subscribe((res) => {
              if (res && res.status == 1) {
                if(res.data.ParentID > 0){
                  this.locationtitle.folder = res.data.title;
                  this.departmentServices.DeptDetail(res.data.ParentID).subscribe((res) => {
                    if (res && res.status == 1) {
                      this.locationtitle.department = res.data.title;
                    } else {
                      this.layoutUtilsService.showError(res.error.message);
                    }
                  });
                }else{
                  this.locationtitle.department = res.data.title;
                }
              } else {
                this.layoutUtilsService.showError(res.error.message);
              }
            });
        } else {
            this.layoutUtilsService.showError(res.error.message);
          }
        });
    }

    setTimeout(() => {
      console.log(this.locationtitle);
    }, 3000);
  }

  getTitleEdit(){
    var x = [];
    if(this.locationtitle.department){
      x.push(this.locationtitle.department);
    }
    if(this.locationtitle.folder){
      x.push(this.locationtitle.folder);
    }
    if(this.locationtitle.project){
      x.push(this.locationtitle.project);
    }

    if(x.length == 0){
      return 'Loading . . .';
    }else{
      return x.join(' &gt; ')
    }
  }

  EditItem(value){
    this.isEditAuto = true;
    this.dataEdit = value;
  }
  Close(event){ 
    console.log('ev:',event);
    this.isEditAuto = false;
    if(event){
      this.dialogRef.close();
    }
  }
}
