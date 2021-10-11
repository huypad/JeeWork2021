import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { StatusDynamicModel } from "./../Model/status-dynamic.model";
import {
  Different_StatusesModel,
  MapModel,
  PositionModel,
} from "./../../List-department/Model/List-department.model";
import { ListDepartmentService } from "./../../List-department/Services/List-department.service";
import {
  Component,
  OnInit,
  Inject,
  ChangeDetectionStrategy,
  HostListener,
  ViewChild,
  ElementRef,
  ChangeDetectorRef,
} from "@angular/core";
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
import { FormBuilder } from "@angular/forms";
import { TranslateService } from "@ngx-translate/core";
import { ReplaySubject, BehaviorSubject, Observable } from "rxjs";
import { Router } from "@angular/router";

import { WeWorkService } from "../../services/wework.services";
import { ProjectsTeamService } from "../Services/department-and-project.service";
import { PopoverContentComponent } from "ngx-smart-popover";
// import { LayoutUtilsService } from 'app/core/_base/crud';

import { UpdateQuickModel } from "../../List-department/Model/List-department.model";
import {
  CdkDrag,
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
} from "@angular/cdk/drag-drop";
import { CommonService } from "../../../../_metronic/jeework_old/core/services/common.service";
import { ChangeTemplateModel } from "../Model/department-and-project.model";

@Component({
  selector: "kt-project-team-edit-status",
  templateUrl: "./project-team-edit-status.component.html",
  styleUrls: ["./project-team-edit-status.component.scss"],
})
export class ProjectTeamEditStatusComponent implements OnInit {
  constructor(
    public dialogRef: MatDialogRef<ProjectTeamEditStatusComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private changeDetectorRefs: ChangeDetectorRef,
    private _service: ProjectsTeamService,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    public weworkService: WeWorkService,
    public _Services: ListDepartmentService,
    public commonService: CommonService
  ) {}
  litsTemplateDemo: any = [];
  listSTT: any = [];
  listStatus: any = [];
  TempSelected = 0;
  isChose = false;
  defaultColors = this.weworkService.defaultColors;
  isDoinguoi = false;
  isStatusNow = true;
  itemde = new ChangeTemplateModel();
  idfocus = 0;
  numbers = [1, 2, 3, 4, 5, 6, 7, 8];
  isAddTemplate = false;
  updateTemp = 0;
  isAddStatus = false;
  IsSave = false;
  visible = true;
  movies = [
    {
      name: "Episode I - The Phantom Menace",
      isDisable: false,
    },
    {
      name: "Episode II - Attack of the Clones",
      isDisable: false,
    },
    {
      name: "Episode III - Revenge of the Sith",
      isDisable: false,
    },
    {
      name: "Episode IV - A New Hope",
      isDisable: false,
    },
    {
      name: "Episode V - The Empire Strikes Back",
      isDisable: false,
    },
    {
      name: "Episode VI - Return of the Jedi",
      isDisable: false,
    },
  ];
  // Update người quản lý quá trình cv
  listUser: any[];

  ngOnInit() {
    console.log(this.data);
    this.TempSelected = this.data.id_template > 0 ? this.data.id_template : 0;
    this.LoadDataTemp();
    this.ListStatusDynamic();
    this.LoadListAccount();
  }
  drop(event: CdkDragDrop<string[]>) {
    console.log(event);
    moveItemInArray(
      this.listSTTHoatdong(),
      event.previousIndex,
      event.currentIndex
    );
    const previous = this.listSTTHoatdong()[event.previousIndex];
    const curent = this.listSTTHoatdong()[event.currentIndex];
    console.log(previous, curent);
    const positon = new PositionModel();
    positon.id_row_from = previous.id_row;
    positon.position_from = previous.Position;
    positon.id_row_to = curent.id_row;
    positon.position_to = curent.Position;
    positon.columnname = this.data.columnname;
    positon.id_columnname = this.data.id_row;
    console.log(positon);
    this.dropPosition(positon);
    // debugger
    // if (event.previousContainer === event.container) {
    //     moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    //   } else {
    //     transferArrayItem(event.previousContainer.data,
    //       event.container.data,
    //       event.previousIndex,
    //       event.currentIndex);
    //   }
  }
  /**
   * Predicate function that only allows even numbers to be
   * sorted into even indices and odd numbers at odd indices.
   */
  dropPosition(_item) {
    if (this.isStatusNow) {
      this._service.dropPosition(_item).subscribe((res) => {
        if (res && res.status == 1) { 
          this.ListStatusDynamic();
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
    } else {
      this._service.dropPositionTemplate(_item).subscribe((res) => {
        if (res && res.status == 1) { 
          this.LoadDataTemp();
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
    }
  }

  sortPredicate(index: number, item: CdkDrag<number>) {
    return (index + 1) % 2 === item.data % 2;
  }
  LoadDataTemp() {
    // load lại
    this.layoutUtilsService.showWaitingDiv();
    this.weworkService.ListTemplateByCustomer().subscribe((res) => {
      this.layoutUtilsService.OffWaitingDiv();
      if (res && res.status === 1) {
        this.litsTemplateDemo = res.data;
        // this.litsTemplateDemo.sort((a, b) => (a.IsTodo === b.IsTodo ? -1 : 1)); // isTodo true lên trước
        if (
          this.TempSelected == 0 ||
          !this.litsTemplateDemo.find((x) => x.id_row == this.TempSelected)
        ) {
          this.TempSelected = this.litsTemplateDemo[0].id_row;
        }
        if (!this.isStatusNow) {
          this.LoadListSTT();
        }
        this.changeDetectorRefs.detectChanges();
      }
    });
  }

  ListStatusDynamic() {
    this.layoutUtilsService.showWaitingDiv();
    this.weworkService
      .ListStatusDynamic(
        this.data.id_row,
        this.data.columnname !== "id_project_team"
      )
      .subscribe((res) => {
        this.layoutUtilsService.OffWaitingDiv();
        if (res && res.status == 1) {
          this.listStatus = res.data;
          this.listStatus.forEach((element) => {
            element.newvalue = 0;
          });
          this.LoadStatusDuan();
          this.changeDetectorRefs.detectChanges();
        }
      });
  }

  LoadListSTT() {
    if (!this.isStatusNow) {
      let x = this.litsTemplateDemo.find((x) => x.id_row == this.TempSelected);
      if (x) {
        this.listSTT = x.status;
      }
    }
  }

  listSTTHoatdong() {
    return this.listSTT.filter((x) => x.Type === 2);
  }

  listSTTMoitao() {
    return this.listSTT.filter((x) => x.Type === 1);
  }

  listSTTDeadline() {
    return this.listSTT.filter((x) => x.Type === 3);
  }

  listSTTFinal() {
    return this.listSTT.filter((x) => x.Type === 4);
  }

  LoadStatusDuan() {
    if (this.listStatus && this.isStatusNow) {
      // this.listSTT = this.listStatus;
      const listTemp = [];
      this.listStatus.forEach((element) => {
        let item = {
          IsDeadline: element.IsDeadline,
          IsDefault: element.isdefault,
          IsFinal: element.IsFinal,
          IsTodo: element.IsToDo,
          Type: element.Type,
          Position: element.position,
          StatusName: element.statusname,
          color: element.color,
          description: element.Description,
          id_project_team: element.id_project_team,
          id_department: element.id_department,
          id_row: element.id_row,
          SL_Tasks: element.SL_Tasks,
        };
        listTemp.push(item);
      });
      this.listSTT = listTemp;
      console.log(this.listSTT);
      this.changeDetectorRefs.detectChanges();
    }
  }

  LoadNewvalue(viewid) {
    let x = this.listSTT.find((x) => x.StatusID == viewid);
    if (x) {
      return x;
    } else {
      return {
        color: "pink",
        StatusName: "Chọn trạng thái mới",
      };
    }
  }

  LuuThayDoi() {
    if (this.data.columnname === "id_project_team") {
      this.IsSave = true;
    } else {
    //   debugger;
      this.itemde.id = this.data.id_row;
      this.itemde.templateid_new = this.TempSelected;
      this.itemde.templateid_old = this.data.id_template;
      this.itemde.isproject = false;
      this._service.ChangeTemplate(this.itemde).subscribe((res) => {
        if (res && res.status == 1) {
          const _messageType = this.translate.instant(
            "GeneralKey.capnhatthanhcong"
          );
          this.layoutUtilsService.showActionNotification(
            _messageType,
            MessageType.Create,
            4000,
            true,
            false
          );
          this.dialogRef.close(true);
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
    }
  }

  Doistt(item, stt) {
    item.colornew = stt.color;
    item.newtitle = stt.StatusName;
  }

  HoanthanhUpdate() {
    const _item = new Different_StatusesModel();
    _item.clear();
    _item.id_project_team = this.data.id_row;
    _item.IsMapAll = !this.isChose;
    _item.TemplateID_New = this.TempSelected;
    let error = false;
    if (this.isChose) {
      this.listStatus.forEach((element) => {
        if (element.SL_Tasks > 0) {
          if (element.newvalue == 0) {
            error = true;
            return;
          } else {
            const ct = new MapModel();
            ct.new_status = element.newvalue;
            ct.old_status = element.id_row;
            _item.Map_Detail.push(ct);
          }
        }
      });
      if (!error) {
        this.Created(_item);
      } else {
        this.layoutUtilsService.showError(
          "Bắt buộc phải chọn trạng thái công việc"
        );
      }
    } else {
      _item.Map_Detail = [];
      this.Created(_item);
    }
  }

  Created(_item) {
    this._service.Different_Statuses(_item).subscribe((res) => {
      if (res && res.status == 1) {
        const _messageType = this.translate.instant(
          "GeneralKey.capnhatthanhcong"
        );
        this.layoutUtilsService.showActionNotification(
          _messageType,
          MessageType.Read,
          4000,
          true,
          false
        );
        this.dialogRef.close(true);
        window.location.reload();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  close() {
    this.dialogRef.close();
  }

  sttFocus(value) {
    this.idfocus = value;
  }

  sttFocusout(value, status) {
    this.idfocus = 0;
    if (!value) {
      return;
    }
    if (this.TempSelected > 0 && !this.isStatusNow) {
      const _item = new UpdateQuickModel();
      _item.clear();
      _item.id_row = status.id_row;
      _item.columname = "StatusName";
      _item.values = value;
      _item.id_template = this.TempSelected;
      this.UpdateQuick(_item);
    } else {
      const item = new StatusDynamicModel();
      item.clear();
      item.Id_row = status.id_row;
      item.StatusName = status.StatusName;
      item.Color = status.color;

      item.Description = status.Description;
      item.Id_project_team = status.id_project_team;
      item.id_department = status.id_department;
      //   item.Follower = status.Follower;
      item.Type = status.Type ? status.Type : "2";
      this.UpdateStatus(item);
    }
  }

  ChangeColor(value, status) {
    debugger
    if (this.TempSelected > 0 && !this.isStatusNow) {
      const _item = new UpdateQuickModel();
      _item.clear();
      _item.id_row = status.id_row;
      _item.columname = "color";
      _item.values = value;
      _item.id_template = this.TempSelected;
      this.UpdateQuick(_item);
    } else {
      const item = new StatusDynamicModel();
      item.clear();
      item.Id_row = status.id_row;
      item.StatusName = status.StatusName;
      item.Color = value;

      item.Description = status.Description;

      if( status.Id_project_team || status.Id_project_team == undefined){
        if (status.id_project_team == null)
        item.Id_project_team =  0;
        else
        item.Id_project_team =  status.id_project_team;
      }
      if(status.id_department){
        item.id_department = status.id_department;
      }
     
      //   item.Follower = status.Follower;
      item.Type = status.Type ? status.Type : "2";
      this.UpdateStatus(item);
    }
  }

  ChangeTemplate(id) {
    this.TempSelected = id;
    this.LoadListSTT();
  }

  addTemplate() {
    this.isAddTemplate = true;
  }

  focusOutTemp(value, temp, isUpdate = false) {
    if (isUpdate) {
      this.updateTemp = 0;
      if (!value) {
        return;
      }
      temp.title = value;
      const _item = new UpdateQuickModel();
      _item.clear();
      _item.id_row = temp.id_row;
      _item.columname = "title";
      _item.values = value;
      _item.istemplate = true;
      this.UpdateQuick(_item);
    } else {
      this.isAddTemplate = false;
      if (!value) {
        return;
      }
      const _item = new UpdateQuickModel();
      _item.clear();
      _item.id_row = 0;
      _item.columname = "title";
      _item.values = value;
      _item.istemplate = true;
      this.UpdateQuick(_item);
    }
  }

  UpdateQuick(item) {
    this._Services.Update_Quick_Template(item).subscribe((res) => {
      if (res && res.status == 1) {
        this.LoadDataTemp();
      }
    });
  }

  Delete_Templete(status, isDelStatus) {
    if (!this.isStatusNow) {
      this._Services
        .Delete_Templete(status.id_row, isDelStatus)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.LoadDataTemp();
          }
        });
    } else {
      if (+status.SL_Tasks > 0) {
        this.layoutUtilsService.showError(
          `Trạng thái đang được sử dụng cho ${status.SL_Tasks} công việc`
        );
        return;
      } else {
        this._service.DeleteStatus(status.id_row).subscribe((res) => {
          if (res && res.status == 1) {
            this.ListStatusDynamic();
          } else {
            this.layoutUtilsService.showActionNotification(
              res.error.message,
              MessageType.Read,
              9999999999,
              true,
              false,
              3000,
              "top",
              0
            );
          }
        });
      }
    }
  } 
  focusOutSTT(value) {
    this.isAddStatus = false;
    if (!value) {
      return;
    } 
    if (!this.isStatusNow) {
      const _item = new UpdateQuickModel();
      _item.clear();
      _item.id_row = 0;
      _item.columname = "StatusName";
      _item.values = value;
      _item.istemplate = false;
      _item.id_template = this.TempSelected;
      this.UpdateQuick(_item);
    } else {
      const item = new StatusDynamicModel();
      item.clear();
      item.StatusName = value;
      item.Color = "rgb(29, 126, 236)";
      if (this.data.columnname=='id_department')
      {
        item.id_department = this.data.id_row;
        item.Id_project_team = 0;
      }
      else
      {
        item.Id_project_team = this.data.id_row;
        item.id_department = 0;
      }
      item.Type = "2";
      this._service.InsertStatus(item).subscribe((res) => {
        if (res && res.status == 1) {
          this.ListStatusDynamic();
        } else {
          this.layoutUtilsService.showActionNotification(
            res.error.message,
            MessageType.Read,
            9999999999,
            true,
            false,
            3000,
            "top",
            0
          );
        }
      });
    }
  }

  LoadListAccount() {
    const filter: any = {};
    filter.id_project_team = this.data.id_row;
    this.weworkService.list_account(filter).subscribe((res) => {
      if (res && res.status === 1) {
        this.listUser = res.data;
        this.changeDetectorRefs.detectChanges();
        // this.setUpDropSearchNhanVien();
      }
    });
  }

  onSubmit(_item) {
    const item = new StatusDynamicModel();
    item.clear();
    item.Id_row = _item.id_row;
    item.StatusName = _item.statusname;
    item.Color = _item.color;

    item.Description = _item.Description;
    item.Id_project_team = _item.id_project_team;
    item.Follower = _item.Follower;
    item.Type = "1";
    // if (item.Id_row > 0) {
    this.UpdateStatus(item);
    // } else {
    // 	this.InsertStatus(item);
    // }
  }

  InsertStatus(item) {
    this._service.InsertStatus(item).subscribe((res) => {
      if (res && res.status == 1) {
        this.layoutUtilsService.showActionNotification(
          this.translate.instant("GeneralKey.themthanhcong"),
          MessageType.Read,
          3000,
          true,
          false,
          3000,
          "top",
          1
        );
        this.dialogRef.close(true);
      } else {
        this.layoutUtilsService.showActionNotification(
          res.error.message,
          MessageType.Read,
          9999999999,
          true,
          false,
          3000,
          "top",
          0
        );
      }
    });
  }

  CheckRole() {
    if (this.isStatusNow) {
      return true;
    } else {
      return this.RoleUpdate();
    }
  }

  RoleUpdate() {
    if (this.commonService.CheckRole_WeWork(3901).length > 0) {
      if (this.litsTemplateDemo && this.litsTemplateDemo[0]) {
        if (this.litsTemplateDemo[0].visible) {
          return true;
        } else {
          return false;
        }
      }
    }
    return false;
  }

  UpdateStatus(item) {
    // console.log(item);
    // return;
    this._service.UpdateStatus(item).subscribe((res) => {
        if (res && res.status == 1) {
            this.ListStatusDynamic();
        } else {
            this.layoutUtilsService.showError(
                res.error.message
            );
        }
    });
  }

  @HostListener("document:keydown", ["$event"])
  onKeydownHandler1(event: KeyboardEvent) {
    if (event.keyCode == 27) {
      // phím ESC
    }
  }
}
