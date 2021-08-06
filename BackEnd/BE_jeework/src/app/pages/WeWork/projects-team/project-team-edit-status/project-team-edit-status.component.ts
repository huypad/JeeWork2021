import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { StatusDynamicModel } from "./../Model/status-dynamic.model";
import {
  Different_StatusesModel,
  MapModel,
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
import {
  FormBuilder,
  FormGroup,
  Validators,
  FormControl,
  AbstractControl,
} from "@angular/forms";
import { TranslateService } from "@ngx-translate/core";
import { ReplaySubject, BehaviorSubject, Observable } from "rxjs";
import { Router } from "@angular/router";

import { WeWorkService } from "../../services/wework.services";
import {
  ProjectTeamModel,
  ProjectTeamUserModel,
} from "../Model/department-and-project.model";
import { ProjectsTeamService } from "../Services/department-and-project.service";
import { PopoverContentComponent } from "ngx-smart-popover";
// import { LayoutUtilsService } from 'app/core/_base/crud';

import { UpdateQuickModel } from "../../List-department/Model/List-department.model";
@Component({
  selector: "kt-project-team-edit-status",
  templateUrl: "./project-team-edit-status.component.html",
  styleUrls: ["./project-team-edit-status.component.scss"],
})
export class ProjectTeamEditStatusComponent implements OnInit {
  litsTemplateDemo: any = [];
  listSTT: any = [];
  listStatus: any = [];
  TempSelected = 0;
  isChose = false;
  defaultColors = this.weworkService.defaultColors;
  isDoinguoi = false;
  isStatusNow = true;
  constructor(
    public dialogRef: MatDialogRef<ProjectTeamEditStatusComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private changeDetectorRefs: ChangeDetectorRef,
    private _service: ProjectsTeamService,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    public weworkService: WeWorkService,
    public _Services: ListDepartmentService,
    private router: Router
  ) {}

  ngOnInit() {
    this.TempSelected = this.data.id_template > 0 ? this.data.id_template : 0;
    this.LoadDataTemp();
    this.ListStatusDynamic();
    this.LoadListAccount();
  }

  LoadDataTemp() {
    //load lại
    this.weworkService.ListTemplateByCustomer().subscribe((res) => {
      if (res && res.status === 1) {
        this.litsTemplateDemo = res.data;
        this.litsTemplateDemo.sort((a, b) => (a.IsTodo === b.IsTodo ? -1 : 1)); // isTodo true lên trước
		console.log(this.TempSelected);
		console.log(this.litsTemplateDemo);
        if (this.TempSelected == 0 || !this.litsTemplateDemo.find(x=>x.id_row == this.TempSelected))
          {
			this.TempSelected = this.litsTemplateDemo[0].id_row;
		  }
        this.LoadListSTT();
        this.changeDetectorRefs.detectChanges();
      }
    });
  }

  ListStatusDynamic() {
    this.weworkService.ListStatusDynamic(this.data.id_row).subscribe((res) => {
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
    var x = this.litsTemplateDemo.find((x) => x.id_row == this.TempSelected);
    if (x) {
      this.listSTT = x.status;
    }
  }

  LoadStatusDuan(){
	if (this.listStatus) {
        // this.listSTT = this.listStatus;
        var listTemp = [];
        this.listStatus.forEach((element) => {
          var item = {
            IsDeadline: element.IsDeadline,
            IsDefault: element.isdefault,
            IsFinal: element.IsFinal,
            IsTodo: element.IsTodo,
            Position: element.position,
            StatusName: element.statusname,
            color: element.color,
            description: element.Description,
            id_row: element.id_row,
          };
          listTemp.push(item);
        });
        this.listSTT = listTemp;
        this.changeDetectorRefs.detectChanges();
      }
  }

  LoadNewvalue(viewid) {
    var x = this.listSTT.find((x) => x.StatusID == viewid);
    if (x) {
      return x;
    } else {
      return {
        color: "pink",
        StatusName: "Chọn trạng thái mới",
      };
    }
  }

  HoanthanhUpdate() {
    const _item = new Different_StatusesModel();
    _item.clear();
    _item.id_project_team = this.data.id_row;
    _item.IsMapAll = !this.isChose;
    _item.TemplateID_New = this.TempSelected;
    if (this.isChose) {
      this.listStatus.forEach((element) => {
        if (element.newvalue == 0) {
          this.layoutUtilsService.showError(
            "Bắt buộc phải chọn trạng thái công việc"
          );
          return;
        } else {
          const ct = new MapModel();
          ct.new_status = element.newvalue;
          ct.old_status = element.id_row;
          _item.Map_Detail.push(ct);
        }
      });
    } else {
      _item.Map_Detail = [];
    }

    this._service.Different_Statuses(_item).subscribe((res) => {
      if (res && res.status == 1) {
        this.layoutUtilsService.showInfo("Update thành công");
        this.dialogRef.close(true);
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  close() {
    this.dialogRef.close();
  }

  idfocus = 0;
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
      item.StatusName = status.statusname;
      item.Color = status.color;

      item.Description = status.Description;
      item.Id_project_team = status.id_project_team;
    //   item.Follower = status.Follower;
      item.Type = status.Type?status.Type:"2";
      console.log(status);

    //   this.UpdateStatus(item);
    }
  }
  ChangeColor(value, status) {
    const _item = new UpdateQuickModel();
    _item.clear();
    _item.id_row = status.id_row;
    _item.columname = "color";
    _item.values = value;
    _item.id_template = this.TempSelected;
    this.UpdateQuick(_item);
  }

  ChangeTemplate(id) {
    this.TempSelected = id;
    this.LoadListSTT();
  }

  isAddTemplate = false;
  updateTemp = 0;
  isAddStatus = false;
  IsSave = false;
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

  Delete_Templete(id, isDelStatus) {
    this._Services.Delete_Templete(id, isDelStatus).subscribe((res) => {
      if (res && res.status == 1) {
        this.LoadDataTemp();
      }
    });
  }

  focusOutSTT(value) {
    this.isAddStatus = false;
    if (!value) {
      return;
    }
    const _item = new UpdateQuickModel();
    _item.clear();
    _item.id_row = 0;
    _item.columname = "StatusName";
    _item.values = value;
    _item.istemplate = false;
    _item.id_template = this.TempSelected;
    this.UpdateQuick(_item);
  }

  // Update người quản lý quá trình cv
  listUser: any[];
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
  UpdateStatus(item) {
    this._service.UpdateStatus(item).subscribe((res) => {
      if (res && res.status == 1) {
        this.layoutUtilsService.showActionNotification(
          this.translate.instant("GeneralKey.capnhatthanhcong"),
          MessageType.Read,
          3000,
          true,
          false,
          3000,
          "top",
          1
        );
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

  @HostListener("document:keydown", ["$event"])
  onKeydownHandler1(event: KeyboardEvent) {
    if (event.keyCode == 27) {
      //phím ESC
    }
  }
}
