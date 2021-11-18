import {
  TemplateCenterModel,
  ListFieldModel,
  TempalteUserModel,
  StatusListModel,
} from "./../template-model/template.model";
import { WeWorkService } from "./../../services/wework.services";
import { QueryParamsModelNew } from "./../../../../_metronic/jeework_old/core/models/query-models/query-params.model";
import { LayoutUtilsService } from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { ListDepartmentService } from "./../../department/Services/List-department.service";
import { TranslateService } from "@ngx-translate/core";
import { TemplateCenterService } from "./../template-center.service";
import { ProjectsTeamService } from "./../../projects-team/Services/department-and-project.service";
import { TemplateCenterComponent } from "./../template-center.component";
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
import { Component, OnInit, ChangeDetectorRef, Inject } from "@angular/core";

@Component({
  selector: "app-template-center-update",
  templateUrl: "./template-center-update.component.html",
  styleUrls: ["./template-center-update.component.scss"],
})
export class TemplateCenterUpdateComponent implements OnInit {
  buocthuchien = 1;
  ListTemplateUser: any = [];
  ListDepartmentFolder: any = [];
  ListField: any = [];
  listUserSelected: any = [];
  listUser: any = [];
  listStatus: any = [];
  listDefaultView: any = [];
  importall = true;
  isAddTask = true;
  AllView = true;
  adddescription = false;
  options: any = {};
  ItemNewSaveAs: any = {};
  ItemSelect: any = {};
  TemplateDetail: any = [];
  share_with = 1;
  id_save_as = 0;
  keyword = "";
  IsSaveAs = true;
  UserID = localStorage.getItem("idUser");
  constructor(
    public dialogRef: MatDialogRef<TemplateCenterComponent>,
    private layoutUtilsService: LayoutUtilsService,
    private projectsTeamService: ProjectsTeamService,
    private templatecenterService: TemplateCenterService,
    private translateService: TranslateService,
    private departmentServices: ListDepartmentService,
    public weWorkService: WeWorkService,
    private changeDetectorRefs: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) { }

  ngOnInit(): void {
    this.buocthuchien = this.data.buocthuchien;
    this.LoadDetailChoose();
    //load Department Folder
    this.templatecenterService.Lite_WorkSpace_tree_By_User().subscribe((res) => {
      if (res && res.status == 1) {
        this.ListDepartmentFolder = res.data;
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
    //load DS user
    this.weWorkService.list_account({}).subscribe((res) => {
      if (res && res.status == 1) {
        this.listUser = res.data;
        this.options = this.getOptions();
        this.changeDetectorRefs.detectChanges();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
    //load list field Template
    this.templatecenterService.LiteListField().subscribe((res) => {
      if (res && res.status == 1) {
        this.ListField = res.data;
        this.ListField.forEach((element) => {
          element.checked = true;
        });
        this.changeDetectorRefs.detectChanges();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
    this.LoadTC();
  }
  LoadDetailChoose() {
    this.id_save_as = this.data.item.id;
    this.TemplateDetail.share_with = 1;
    this.TemplateDetail.types = this.data.item.type;
    if (this.data.item.type == 1 || this.data.item.type == 2) {
      // department or folder
      this.departmentServices.DeptDetail(this.data.item.id).subscribe((res) => {
        if (res && res.status == 1) {
          this.ItemNewSaveAs = res.data;
          var listTemplate = this.ItemNewSaveAs.Template;
          if (listTemplate && listTemplate.length > 0) {
            listTemplate.forEach((element) => {
              if (element.Status) {
                Array.prototype.push.apply(this.listStatus, element.Status);
              }
            });
          }

          this.listDefaultView = this.ItemNewSaveAs.DefaultView;
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
      // this.ItemNewSaveAs
    } else if (this.data.item.type == 3) {
      // project
      this.projectsTeamService
        .Detail(this.data.item.id)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.ItemNewSaveAs = res.data;
          } else {
            this.layoutUtilsService.showError(res.error.message);
          }
        });
      // list view by project
      this.weWorkService
        .ListViewByProject(this.data.item.id)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.listDefaultView = res.data;
          } else {
            this.layoutUtilsService.showError(res.error.message);
          }
        });
      // list Status
      this.weWorkService
        .ListStatusDynamic(this.data.item.id)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.listStatus = res.data;
          } else {
            this.layoutUtilsService.showError(res.error.message);
          }
        });
    }
  }
  filterConfiguration(): any {
    const filter: any = {};
    if (this.CreatedBy.id_nv > 0) filter.collect_by = this.CreatedBy.id_nv;
    filter.keyword = this.keyword;
    return filter;
  }
  close() {
    this.dialogRef.close();
  }
  LoadTC() {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      "",
      "",
      1,
      50,
      true
    );
    this.templatecenterService
      .getListTemplateUser(queryParams)
      .subscribe((res) => {
        if (res && res.status == 1) {
          this.ListTemplateUser = res.data;
          this.changeDetectorRefs.detectChanges();
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
  }
  ItemSelected(data) {
    var index = this.listUserSelected.findIndex((x) => x.id_nv == data.id_nv);
    if (index >= 0) {
      this.listUserSelected.splice(index, 1);
    } else {
      this.listUserSelected.push(data);
    }
  }
  CreatedBy: any = {};
  ItemSelectedCreatedBy(data) {
    this.CreatedBy = data;
    this.LoadTC();
  }
  getTitle() {
    return "Lưu mẫu";
  }

  LoadFieldName() {
    var text = "";
    this.ListField.forEach((element) => {
      // {{ 'filter.'+item.title | translate }}
      text +=
        this.translateService.instant("filter." + element.fieldname) + " ,";
    });
    return text.slice(0, -1);
  }
  getTypesName(types) {
    if (types == 1) {
      // space
      return "Phòng ban";
    } else if (types == 2) {
      // folder
      return "Thư mục";
    } else if (types == 3) {
      // list
      return "Danh sách";
    }
    return "Phòng ban";
  }
  Share(value) {
    if (value == 2) return;
    this.share_with = value;
  }
  UpdateTemplate(item) {
    this.ItemSelect = item;
  }
  NextStep() {
    this.IsSaveAs = false;
    if (!(this.ItemSelect.id_row > 0)) return;
    this.templatecenterService
      .getDetailTemplate(this.ItemSelect.id_row)
      .subscribe((res) => {
        if (res && res.status == 1) {
          this.TemplateDetail = res.data;
          this.share_with = this.TemplateDetail.share_with;
          this.listUserSelected = this.TemplateDetail.list_share;
          this.buocthuchien = 1;
          this.LoadDatafield();
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
  }
  getIconTemplate(types) {
    if (types == 1) {
      // space
      return "https://cdn1.iconfinder.com/data/icons/space-exploration-and-next-big-things-5/512/676_Astrology_Planet_Space-512.png";
    } else if (types == 2) {
      // folder
      return "https://png.pngtree.com/png-vector/20190215/ourlarge/pngtree-vector-folder-icon-png-image_554064.jpg";
    } else if (types == 3) {
      // list
      return "https://img.pngio.com/list-icons-free-download-png-and-svg-list-icon-png-256_256.png";
    }
    return "https://cdn1.iconfinder.com/data/icons/space-exploration-and-next-big-things-5/512/676_Astrology_Planet_Space-512.png";
  }
  getOptions() {
    var options: any = {
      showSearch: false,
      keyword: "",
      data: this.listUser,
    };
    return options;
  }
  SaveAs() {
    if (this.IsSaveAs) this.buocthuchien = 1;
    return;
  }
  Onsubmit() {
    if (this.IsSaveAs) {
      // save as templateID
      this.DataSaveAs();
    } else {
      // Update template
      this.DataUpdate();
    }
  }

  DataUpdate() {
    const TCinsert = new TemplateCenterModel();
    TCinsert.clear();
    // insert data
    TCinsert.types = this.TemplateDetail.types;
    TCinsert.customerid = this.TemplateDetail.customerid;
    TCinsert.template_typeid = this.TemplateDetail.template_typeid;
    TCinsert.img_temp = this.TemplateDetail.img_temp;
    TCinsert.id_row = this.TemplateDetail.id_row;
    TCinsert.templateid = this.TemplateDetail.templateid
      ? this.TemplateDetail.templateid
      : 0;
    TCinsert.levels = this.TemplateDetail.levels
      ? this.TemplateDetail.levels
      : 1;
    TCinsert.is_task = this.isAddTask;
    TCinsert.is_views = this.AllView;
    TCinsert.group_statusid = this.TemplateDetail.group_statusid;
    TCinsert.viewid = this.TemplateDetail.viewid;
    TCinsert.title = this.TemplateDetail.title;
    TCinsert.description = this.TemplateDetail.description;
    TCinsert.field_id = this.TemplateDetail.field_id;
    // kiểm tra custom item
    TCinsert.is_customitems = this.importall;
    TCinsert.list_field_name = [];
    if (!this.importall) {
      const listcustomitems = this.ListField.filter((item) => item.checked);
      listcustomitems.forEach((element) => {
        const cus_it = new ListFieldModel();
        cus_it.clear();
        cus_it.fieldname = element.fieldname;
        cus_it.id_field = element.id_field;
        cus_it.isnewfield = element.isnewfield;
        cus_it.isdefault = element.isdefault;
        cus_it.isvisible = element.isvisible;
        cus_it.position = element.position;
        cus_it.title = element.title;
        if (element.note) cus_it.note = element.note;
        if (element.type) cus_it.type = element.type;
        if (element.typeid) cus_it.typeid = element.typeid;
        TCinsert.list_field_name.push(cus_it);
      });
    } else {
      this.ListField.forEach((element) => {
        const cus_it = new ListFieldModel();
        cus_it.clear();
        cus_it.fieldname = element.fieldname;
        cus_it.id_field = element.id_field;
        cus_it.isnewfield = element.isnewfield;
        cus_it.isdefault = element.isdefault;
        cus_it.isvisible = element.isvisible;
        cus_it.position = element.position;
        cus_it.title = element.title;
        if (element.note) cus_it.note = element.note;
        if (element.type) cus_it.type = element.type;
        if (element.typeid) cus_it.typeid = element.typeid;
        TCinsert.list_field_name.push(cus_it);
      });
    } //ListField

    TCinsert.share_with = this.share_with;
    const listShare = new Array<TempalteUserModel>();
    if (this.share_with == 1) {
      const tempU = new TempalteUserModel();
      tempU.clear();
      tempU.id_user = +this.UserID;
      tempU.id_template = TCinsert.templateid;
      // tempU.id_row
      listShare.push(tempU);
    } else if (this.share_with == 3) {
      this.listUser.forEach((element) => {
        const tempU = new TempalteUserModel();
        tempU.clear();
        tempU.id_user = element.id_nv;
        tempU.id_template = TCinsert.templateid;
        // tempU.id_row
        listShare.push(tempU);
      });
    } else if (this.share_with == 4) {
      if (this.listUserSelected.length == 0) {
        this.layoutUtilsService.showError("Danh sách chia sẻ không được trống");
        return;
      }
      this.listUserSelected.forEach((element) => {
        const tempU = new TempalteUserModel();
        tempU.clear();
        tempU.id_user = element.id_nv;
        tempU.id_template = TCinsert.templateid;
        // tempU.id_row
        listShare.push(tempU);
      });
    }
    TCinsert.list_share = listShare;
    TCinsert.save_as_id = "" + this.id_save_as;

    this.UpdateTemplateCenter(TCinsert);
  }
  DataSaveAs() {
    const TCinsert = new TemplateCenterModel();
    TCinsert.clear();
    // insert data
    TCinsert.types = this.TemplateDetail.types;
    TCinsert.customerid = this.TemplateDetail.customerid;
    TCinsert.template_typeid = this.TemplateDetail.template_typeid;
    TCinsert.img_temp = this.TemplateDetail.img_temp;
    TCinsert.id_row = this.TemplateDetail.id_row;
    TCinsert.templateid = this.TemplateDetail.templateid
      ? this.TemplateDetail.templateid
      : 0;
    TCinsert.levels = this.TemplateDetail.levels
      ? this.TemplateDetail.levels
      : 1;
    TCinsert.is_task = this.isAddTask;
    TCinsert.is_views = this.AllView;
    TCinsert.group_statusid = this.TemplateDetail.group_statusid;
    TCinsert.viewid = this.TemplateDetail.viewid;
    TCinsert.title = this.TemplateDetail.title;
    TCinsert.description = this.TemplateDetail.description;
    TCinsert.field_id = this.TemplateDetail.field_id;
    // kiểm tra custom item
    TCinsert.is_customitems = this.importall;
    TCinsert.list_field_name = [];
    if (!this.importall) {
      const listcustomitems = this.ListField.filter((item) => item.checked);
      listcustomitems.forEach((element) => {
        const cus_it = new ListFieldModel();
        cus_it.clear();
        cus_it.fieldname = element.fieldname;
        cus_it.id_field = element.id_field;
        cus_it.isnewfield = element.isnewfield;
        cus_it.isdefault = element.isdefault;
        cus_it.isvisible = element.isvisible;
        cus_it.position = element.position;
        cus_it.title = element.title;
        if (element.note) cus_it.note = element.note;
        if (element.type) cus_it.type = element.type;
        if (element.typeid) cus_it.typeid = element.typeid;
        TCinsert.list_field_name.push(cus_it);
      });
    } else {
      this.ListField.forEach((element) => {
        const cus_it = new ListFieldModel();
        cus_it.clear();
        cus_it.fieldname = element.fieldname;
        cus_it.id_field = element.id_field;
        cus_it.isnewfield = element.isnewfield;
        cus_it.isdefault = element.isdefault;
        cus_it.isvisible = element.isvisible;
        cus_it.position = element.position;
        cus_it.title = element.title;
        if (element.note) cus_it.note = element.note;
        if (element.type) cus_it.type = element.type;
        if (element.typeid) cus_it.typeid = element.typeid;
        TCinsert.list_field_name.push(cus_it);
      });
    } //ListField

    TCinsert.share_with = this.share_with;
    const listShare = new Array<TempalteUserModel>();
    if (this.share_with == 1) {
      const tempU = new TempalteUserModel();
      tempU.clear();
      tempU.id_user = +this.UserID;
      tempU.id_template = TCinsert.templateid;
      // tempU.id_row
      listShare.push(tempU);
    } else if (this.share_with == 3) {
      this.listUser.forEach((element) => {
        const tempU = new TempalteUserModel();
        tempU.clear();
        tempU.id_user = element.id_nv;
        tempU.id_template = TCinsert.templateid;
        // tempU.id_row
        listShare.push(tempU);
      });
    } else if (this.share_with == 4) {
      if (this.listUserSelected.length == 0) {
        this.layoutUtilsService.showError("Danh sách chia sẻ không được trống");
        return;
      }
      this.listUserSelected.forEach((element) => {
        const tempU = new TempalteUserModel();
        tempU.clear();
        tempU.id_user = element.id_nv;
        tempU.id_template = TCinsert.templateid;
        // tempU.id_row
        listShare.push(tempU);
      });
    }
    TCinsert.list_share = listShare;
    TCinsert.save_as_id = "" + this.id_save_as;

    if (this.listDefaultView && this.listDefaultView.length > 0) {
      var listviewID = [];
      this.listDefaultView.forEach((element) => {
        listviewID.push(element.viewid);
      });
      TCinsert.viewid = listviewID.join();
    }


    if (this.listStatus && this.listStatus.length > 0) {
      if (this.TemplateDetail.types == 1 || this.TemplateDetail.types == 2) {
        this.listStatus.forEach(element => {
          const status = new StatusListModel();
          status.clear();
          status.id_row = element.Id_row;
          status.StatusName = element.StatusName;
          status.color = element.color ? element.color : '';
          status.IsDefault = element.IsDefault ? element.IsDefault : false;
          status.IsFinal = element.IsFinal;
          status.IsDeadline = element.IsDeadline;
          status.IsToDo = element.IsTodo;
          status.Type = 1;
          TCinsert.list_status.push(status);
        });
      } else {
        this.listStatus.forEach(element => {
          const status = new StatusListModel();
          status.clear();
          status.id_row = element.id_row;
          status.StatusName = element.statusname;
          status.Description = element.Description ? element.Description : '';
          status.color = element.color ? element.color : '';
          status.IsDefault = element.IsDefault ? element.IsDefault : false;
          status.IsFinal = element.IsFinal;
          status.IsDeadline = element.IsDeadline;
          status.IsToDo = element.IsToDo;
          status.Type = 1;
          TCinsert.list_status.push(status);
        });
      }
    }


    this.SaveAsTemplateCenter(TCinsert);
  }

  LoadDatafield() {
    // this.ListField.filter((item) => item.checked)
    if (this.ListField.length == this.TemplateDetail.data_fields.length) {
      this.importall = true;
    } else {
      this.importall = false;
      var i = 0;
      this.ListField.forEach((element) => {
        var x = this.TemplateDetail.data_fields.find(x => x.id_field == element.id_field);
        i++;
        if (x) {
          element.checked = true;
        } else {
          element.checked = false;
        }
      });
    }
  }

  UpdateTemplateCenter(TCinsert) {
    this.departmentServices.UpdateTemplateCenter(TCinsert).subscribe((res) => {
      if (res && res.status === 1) {
        this.layoutUtilsService.showActionNotification("Cập nhập thành công");
        this.dialogRef.close();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }
  SaveAsTemplateCenter(TCinsert) {
    this.departmentServices.SaveAsTemplateCenter(TCinsert).subscribe((res) => {
      if (res && res.status === 1) {
        this.layoutUtilsService.showActionNotification("Thêm mới thành công");
        this.dialogRef.close();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }
}
