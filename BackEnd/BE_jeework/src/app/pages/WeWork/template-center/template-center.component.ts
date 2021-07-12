import { ProjectsTeamService } from "./../projects-team/Services/department-and-project.service";
import { ProjectTeamUserModel } from "./../projects-team/Model/department-and-project.model";
import { ProjectTeamModel } from "src/app/pages/WeWork/projects-team/Model/department-and-project.model";
import { ListDepartmentService } from "./../List-department/Services/List-department.service";
import {
  DepartmentModel,
  DepartmentOwnerModel,
  DepartmentViewModel,
} from "./../List-department/Model/List-department.model";
import { TranslateService } from "@ngx-translate/core";
import { MatMenuTrigger } from "@angular/material/menu";
import { filter } from "rxjs/operators";
import { QueryParamsModelNew } from "./../../../_metronic/jeework_old/core/models/query-models/query-params.model";
import { LayoutUtilsService } from "./../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { TemplateCenterService } from "./template-center.service";
import { MatAccordion } from "@angular/material/expansion";
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
import {
  Component,
  OnInit,
  Inject,
  ViewChild,
  ChangeDetectorRef,
} from "@angular/core";
import { type } from "node:os";
import {
  ListFieldModel,
  TemplateCenterModel,
} from "./template-model/template.model";

@Component({
  selector: "app-template-center",
  templateUrl: "./template-center.component.html",
  styleUrls: ["./template-center.component.scss"],
})
export class TemplateCenterComponent implements OnInit {
  @ViewChild(MatAccordion) accordion: MatAccordion;
  ItemParentID: any = {};
  ParentName = "Chọn vị trí lưu";
  buocthuchien = 1;
  isAddTask = true;
  AllView = true;
  importall = true;
  ProjectDatesDefault = true;
  chontacvu = 1;
  DanhSachTC: any = [];
  ListField: any = [];
  TemplateDetail: any = [];
  TemplateTypes: any = [];
  TemplateKeyWorks: any = "";
  ListDepartmentFolder: any = [];
  infoStep3: any = {};
  start_date = "";
  end_date = "";
  constructor(
    public dialogRef: MatDialogRef<TemplateCenterComponent>,
    private layoutUtilsService: LayoutUtilsService,
    private projectsTeamService: ProjectsTeamService,
    private templatecenterService: TemplateCenterService,
    private translateService: TranslateService,
    private departmentServices: ListDepartmentService,
    private changeDetectorRefs: ChangeDetectorRef,
    @Inject(MAT_DIALOG_DATA) public data: any
  ) {}

  ngOnInit(): void {
    //load type
    this.templatecenterService.getTemplateTypes().subscribe((res) => {
      if (res && res.status == 1) {
        this.TemplateTypes = res.data;
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
    //load Department Folder
    this.templatecenterService.LiteDepartmentFolder().subscribe((res) => {
      if (res && res.status == 1) {
        this.ListDepartmentFolder = res.data;
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
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
    this.LoadTC();
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
  getTitle() {
    // {{ 'GeneralKey.templatecenter' | translate }}
    if (this.buocthuchien == 1)
      return this.translateService.instant("GeneralKey.templatecenter");
    if (this.buocthuchien == 2) return "Mẫu giao diện";
    if (this.buocthuchien == 3)
      return this.translateService.instant("template.usetemp");
    return this.translateService.instant("GeneralKey.templatecenter");
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
      .getTemplateCenter(queryParams)
      .subscribe((res) => {
        if (res && res.status == 1) {
          this.DanhSachTC = res.data;
          this.changeDetectorRefs.detectChanges();
        } else {
          this.layoutUtilsService.showError(res.error.message);
        }
      });
  }
  getTooltipStatus(status) {
    var text = "Nhóm" + status.title + ": ";
    status.status_list.forEach((element) => {
      text += element.statusname + ", ";
    });
    return text.slice(0, -2);
  }
  selectedParent(item) {
    this.ItemParentID.id_row = item.id_row;
    this.ItemParentID.type = item.type;
    this.ParentName = item.title;
    item.expanded = !item.expanded;
  }
  // [checked]="IsChecked(space)"
  IsChecked(item) {
    if (
      item.id_row == this.ItemParentID.id_row &&
      item.type == this.ItemParentID.type
    ) {
      return true;
    } else {
      return false;
    }
  }
  filterConfiguration(): any {
    var listType = [];
    this.Types.forEach((element) => {
      if (element.checked) {
        listType.push(element.id);
      }
    });
    var listLevel = [];
    this.Levels.forEach((element) => {
      if (element.checked) {
        listLevel.push(element.id);
      }
    });
    var listTemplateTypes = [];
    this.TemplateTypes.forEach((element) => {
      if (element.isdefault) {
        listTemplateTypes.push(element.id_row);
      }
    });
    const filter: any = {};
    filter.keyword = this.TemplateKeyWorks;
    filter.template_typeid = listTemplateTypes.join(); //API: WeworkLiteController.lite_template_types
    filter.types = listType.join(); //1 - space, 2 - folder, 3 - list (Project)
    filter.levels = listLevel.join(); //1 - Beginner, 2 - Intermediate, 3 - Advanced
    filter.collect_by = ""; //Người tạo (Table: we_template_customer)
    return filter;
  }
  //
  SelectedTemplate(item) {
    this.NextStep();
    this.templatecenterService
      .getDetailTemplate(item.id_row)
      .subscribe((res) => {
        if (res && res.status == 1) {
          this.TemplateDetail = res.data;
        }
      });
  }
  NextStep() {
    this.buocthuchien += 1;
  }
  PrevStep() {
    if (this.buocthuchien == 1) {
      this.dialogRef.close();
    } else {
      this.buocthuchien -= 1;
    }
  }
  LoadInfoStep3() {
    if (this.TemplateDetail.types == 1) {
      // space
      this.infoStep3 = {
        name: this.translateService.instant("template.space"),
        inputname: this.translateService.instant("template.enterspacename"),
        selectFolder: this.translateService.instant("template.selectlocation"),
      };
    } else if (this.TemplateDetail.types == 2) {
      // folder
      this.infoStep3 = {
        name: this.translateService.instant("template.foldername"),
        inputname: this.translateService.instant("template.enterfoldername"),
        selectFolder: this.translateService.instant("template.selectlocation"),
      };
    } else if (this.TemplateDetail.types == 3) {
      // list
      this.infoStep3 = {
        name: this.translateService.instant("template.list"),
        inputname: this.translateService.instant("template.enterlistname"),
        selectFolder: this.translateService.instant("template.selectlocation"),
      };
    }
  }

  Onsubmit() {
    const TCinsert = new TemplateCenterModel();
    TCinsert.clear();
    // insert data
    TCinsert.types = this.TemplateDetail.types;
    TCinsert.customerid = this.TemplateDetail.customerid;
    TCinsert.template_typeid = this.TemplateDetail.template_typeid;
    if(this.TemplateDetail.img_temp)
      TCinsert.img_temp = this.TemplateDetail.img_temp;
    TCinsert.id_row = this.TemplateDetail.id_row;
    if(this.TemplateDetail.templateid) 
      TCinsert.templateid = this.TemplateDetail.templateid;
    TCinsert.levels = this.TemplateDetail.levels;
    TCinsert.is_task = this.isAddTask;
    TCinsert.is_views = this.AllView;
    TCinsert.group_statusid = this.TemplateDetail.group_statusid;
    TCinsert.viewid = this.TemplateDetail.viewid;
    if(this.TemplateDetail.sample_id > 0){
      TCinsert.sample_id = ''+this.TemplateDetail.sample_id;
    }
    // kiểm tra title
    let titleTemplate = (<HTMLInputElement>(
      document.getElementById("titleTemplate")
    )).value;
    if (titleTemplate) {
      TCinsert.title = titleTemplate;
    } else {
      this.layoutUtilsService.showError("Tên Template Center là bắt buộc");
      return;
    }
    // kiểm tra chọn parent
    if (this.TemplateDetail.types > 1) {
      if (this.ItemParentID.type > 0) {
        if(this.TemplateDetail.types == 2){ // folder
          if (this.ItemParentID.type != 1) {
            this.layoutUtilsService.showError("Vị trí lưu trữ không hợp lệ.");
            return;
          } else {
            TCinsert.ParentID = this.ItemParentID.id_row > 0 ? this.ItemParentID.id_row : 0;
          }
        }
        if(this.TemplateDetail.types == 3){ // list
          TCinsert.ParentID = this.ItemParentID.id_row > 0 ? this.ItemParentID.id_row : 0;
        }
      } else {
        this.layoutUtilsService.showError(
          "Bắt buộc chọn Folder hoặc Space để nhận dữ liệu parent"
        );
        return;
      }
    }
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

    if (
      this.TemplateDetail.data_fields &&
      this.TemplateDetail.data_fields.length > 0
    ) {
      this.TemplateDetail.data_fields.forEach((element) => {
        const cus_it = new ListFieldModel();
        cus_it.clear();
        cus_it.fieldname = element.fieldname;
        cus_it.id_field = element.id_field;
        cus_it.isnewfield = true;
        cus_it.isdefault = element.isdefault;
        cus_it.position = element.position;
        cus_it.title = element.title;
        if (element.isvisible) cus_it.isvisible = element.isvisible;
        if (element.note) cus_it.note = element.note;
        if (element.type) cus_it.type = element.type;
        if (element.typeid) cus_it.typeid = element.typeid;
        TCinsert.list_field_name.push(cus_it);
      });
    }

    //ListField new
    // kiểm tra project date
    TCinsert.is_projectdates = this.ProjectDatesDefault;
    if (!this.ProjectDatesDefault) {
      if (this.start_date) {
        TCinsert.start_date = this.f_convertDate(this.start_date);
      }
      if (this.end_date) {
        TCinsert.end_date = this.f_convertDate(this.end_date);
      }
    }

    if (TCinsert.types == 3) {
      const _item = new ProjectTeamModel();
      _item.templatecenter = TCinsert;
      _item.id_department = "" + TCinsert.ParentID;
      _item.title = TCinsert.title;
      _item.description = this.TemplateDetail.description?this.TemplateDetail.description:'';
      _item.loai = "1";
      _item.is_project = true;
      const ct = new ProjectTeamUserModel();
      ct.clear();
      ct.id_user = +localStorage.getItem("idUser");
      ct.admin = true;
      _item.Users.push(ct);
      // if ( this.TemplateDetail.data_views && this.TemplateDetail.data_views.length > 0 ) {
      //   this.TemplateDetail.data_views.map((item, index) => {
      //     const dv = new DepartmentViewModel();
      //     dv.clear();
      //     dv.viewid = item.id_row;
      //     dv.is_default = item.is_default;
      //     _item.DefaultView.push(dv);
      //   });
      // }

      this.CreateProject(_item, false);
    } else {
      const _item = new DepartmentModel();
      _item.clear();
      _item.templatecenter = TCinsert;
      _item.ParentID = TCinsert.ParentID;
      _item.title = TCinsert.title;
      _item.Owners = [];
      const ct = new DepartmentOwnerModel();
      ct.clear();
      ct.id_user = +localStorage.getItem("idUser");
      _item.Owners.push(ct);
      _item.DefaultView = [];
      if (
        this.TemplateDetail.data_views &&
        this.TemplateDetail.data_views.length > 0
      ) {
        this.TemplateDetail.data_views.map((item, index) => {
          const dv = new DepartmentViewModel();
          dv.clear();
          dv.viewid = item.id_row;
          dv.is_default = item.is_default;
          _item.DefaultView.push(dv);
        });
      }
      _item.TemplateID = TCinsert.templateid;
      this.Create(_item, false);
    }
    // this.Create(_item, false);
  }
  disabledBtn = false;
  Create(_item: DepartmentModel, withBack: boolean) {
    this.disabledBtn = true;
    this.departmentServices.InsertDept(_item).subscribe((res) => {
      this.disabledBtn = false;
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.layoutUtilsService.showActionNotification("Thêm Mẫu thành công");
        setTimeout(() => {
          window.location.reload();
        }, 10);
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }
  CreateProject(_item: ProjectTeamModel, withBack: boolean) {
    this.disabledBtn = true;
    this.projectsTeamService.InsertProjectTeam(_item).subscribe((res) => {
      this.disabledBtn = false;
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.layoutUtilsService.showActionNotification("Thêm Mẫu thành công");
        setTimeout(() => {
          window.location.reload();
        }, 10);
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
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

  getBackground(text) {
    return "#1DB954";
  }
  getTemplateCenterTemplate(item) {
    if (item.types == 1) {
      // space
      return "cu-template-center-template__space";
    } else if (item.types == 2) {
      // folder
      return "cu-template-center-template__folder";
    } else if (item.types == 3) {
      // list
      return "cu-template-center-template__list";
    }
    return "cu-template-center-template__space";
  }
  getTypesName(item) {
    if (item.types == 1) {
      // space
      return "Phòng ban";
    } else if (item.types == 2) {
      // folder
      return "Thư mục";
    } else if (item.types == 3) {
      // list
      return "Danh sách";
    }
    return "Phòng ban";
  }
  getIconTemplate(item) {
    if (item.types == 1) {
      // space
      return "https://cdn1.iconfinder.com/data/icons/space-exploration-and-next-big-things-5/512/676_Astrology_Planet_Space-512.png";
    } else if (item.types == 2) {
      // folder
      return "https://png.pngtree.com/png-vector/20190215/ourlarge/pngtree-vector-folder-icon-png-image_554064.jpg";
    } else if (item.types == 3) {
      // list
      return "https://img.pngio.com/list-icons-free-download-png-and-svg-list-icon-png-256_256.png";
    }
    return "https://cdn1.iconfinder.com/data/icons/space-exploration-and-next-big-things-5/512/676_Astrology_Planet_Space-512.png";
  }
  countID(str) {
    if (str == "") return 0;
    var lst = str.split(",");
    return lst.length;
  }
  CheckedType(item) {
    if (item.countitem > 0) {
      item.checked = !item.checked;
    }
    this.LoadTC();
  }
  Types = [
    {
      checked: false,
      name: "Phòng ban",
      id: "1",
      countitem: 5,
    },
    {
      checked: false,
      name: "Thư mục",
      id: "2",
      countitem: 68,
    },
    {
      checked: false,
      name: "Danh sách",
      id: "3",
      countitem: 15,
    },
    // {
    //   checked:false,
    //   name:'Task',
    //   countitem: 0,
    // },
    // {
    //   checked:false,
    //   name:'Doc',
    //   countitem: 40,
    // },
    // {
    //   checked:false,
    //   name:'View',
    //   countitem: 0,
    // },
  ];
  Levels = [
    //1 - Beginner, 2 - Intermediate, 3 - Advanced
    {
      checked: false,
      name: "Cơ bản",
      id: "1",
      countitem: 79,
    },
    {
      checked: false,
      name: "Trung bình",
      id: "2",
      countitem: 50,
    },
    {
      checked: false,
      name: "Nâng cao",
      id: "3",
      countitem: 17,
    },
  ];
}
