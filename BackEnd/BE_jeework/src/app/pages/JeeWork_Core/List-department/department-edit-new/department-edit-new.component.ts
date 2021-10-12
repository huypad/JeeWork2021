import { CdkDragDrop, moveItemInArray } from "@angular/cdk/drag-drop";
import { CommonService } from "./../../../../_metronic/jeework_old/core/services/common.service";
import { StatusDynamicModel } from "./../../projects-team/Model/status-dynamic.model";
import { TemplateCenterComponent } from "./../../template-center/template-center.component";
import { values } from "lodash";
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { TokenStorage } from "./../../../../_metronic/jeework_old/core/auth/_services/token-storage.service";
import { DanhMucChungService } from "./../../../../_metronic/jeework_old/core/services/danhmuc.service";
import {
  Component,
  OnInit,
  Inject,
  ChangeDetectionStrategy,
  HostListener,
  ViewChild,
  Directive,
  ElementRef,
  ChangeDetectorRef,
} from "@angular/core";

import {
  MatDialog,
  MatDialogRef,
  MAT_DIALOG_DATA,
} from "@angular/material/dialog";
import {
  FormBuilder,
  FormGroup,
  Validators,
  FormControl,
} from "@angular/forms";
import { TranslateService } from "@ngx-translate/core";
import { ReplaySubject, BehaviorSubject } from "rxjs";
import { Router } from "@angular/router";
import { ListDepartmentService } from "../Services/List-department.service";
import {
  DepartmentModel,
  DepartmentOwnerModel,
  DepartmentViewModel,
  UpdateQuickModel,
} from "../Model/List-department.model";
import { PopoverContentComponent } from "ngx-smart-popover";
import { WeWorkService } from "../../services/wework.services";

@Component({
  selector: "kt-department-edit-new",
  templateUrl: "./department-edit-new.component.html",
  styleUrls: ["./department-edit-new.component.scss"],
})
export class DepartmentEditNewComponent implements OnInit {
  step = 1;
  item: DepartmentModel;
  dataParent: DepartmentModel;
  oldItem: DepartmentModel;
  itemFormGroup: FormGroup = new FormGroup({});
  hasFormErrors: boolean = false;
  viewLoading: boolean = false;
  loadingAfterSubmit: boolean = false;
  // @ViewChild("focusInput", { static: true }) focusInput: ElementRef;
  disabledBtn: boolean = false;
  IsEdit: boolean;
  //====================Người Áp dụng====================
  public bankFilterCtrlAD: FormControl = new FormControl();
  public filteredBanksAD: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);

  //====================Người theo dõi===================
  public bankFilterCtrlTD: FormControl = new FormControl();
  public filteredBanksTD: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public datatree: BehaviorSubject<any[]> = new BehaviorSubject([]);
  title: string = "";
  selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
  ID_Struct: string = "";
  Id_parent: string = "";
  options: any = {};
  id_project_team: number;
  admins: any[] = [];
  members: any[] = [];
  IsAdmin: boolean = false;
  myPopover: PopoverContentComponent;
  selected: any[] = [];
  listUser: any[] = [];
  CommentTemp: string = "";
  listChiTiet: any[] = [];
  list_Owners: any[] = [];
  list_Assign: any[] = [];
  litsTemplateDemo: any = [];
  listSpaceStatus: any = [];
  IsDataStaff_HR = false;
  ReUpdated = false;
  UserId: any = 0;
  listSTT: any = [];
  listDefaultView: any = [];
  IsUpdate: any;
  formData: any;
  UseParentTemplate = false;
  isSpaceStatus = false;
  public defaultColors: string[] = [
    "rgb(187, 181, 181)",
    "rgb(29, 126, 236)",
    "rgb(250, 162, 140)",
    "rgb(14, 201, 204)",
    "rgb(11, 165, 11)",
    "rgb(123, 58, 245)",
    "rgb(238, 177, 8)",
    "rgb(236, 100, 27)",
    "rgb(124, 212, 8)",
    "rgb(240, 56, 102)",
    "rgb(255, 0, 0)",
    "rgb(0, 0, 0)",
    "rgb(255, 0, 255)",
  ];

  isComplete = false;

  constructor(
    public dialogRef: MatDialogRef<DepartmentEditNewComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private changeDetectorRefs: ChangeDetectorRef,
    private _Services: ListDepartmentService,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    private danhMucChungService: DanhMucChungService,
    public weworkService: WeWorkService,
    public TokenStorage: TokenStorage,
    public commonService: CommonService,
    public dialog: MatDialog,
    private router: Router
  ) {
    this.itemFormGroup = this.fb.group({
      title: [""],
      dept_name: [""],
    });
  }

  /** LOAD DATA */
  ngOnInit() {
    this.IsUpdate = this.data.IsUpdate;
    if (this.IsUpdate) {
      this.step = 5;
      this.isComplete = true;
    }
    //get các giá trị khởi tạo
    this.UserId = localStorage.getItem("idUser");
    //------------------
    this.title = this.translate.instant("GeneralKey.choncocautochuc") + "";
    this.item = this.data._item;
    if (this.item.RowID > 0) {
      this.isSpaceStatus = true;
    }
    this.options = this.getOptions();
    this.weworkService.list_account({}).subscribe((res) => {
      if (res && res.status === 1) {
        this.listUser = res.data;
        if (!this.IsUpdate) {
          var index = this.listUser.find((x) => x.id_nv == this.UserId);
          if (index) {
            index.type = 1;
            this.list_Owners.push(index);
          }
        }
      } else {
      }
      this.options = this.getOptions();
    });

    this.weworkService.list_default_view({}).subscribe((res) => {
      if (res && res.status === 1) {
        this.listDefaultView = res.data;
        if (this.item.RowID > 0) {
          this.LoadDeparment();
          this.viewLoading = true;
          this.changeDetectorRefs.detectChanges();
        } else {
          if (+this.item.ParentID > 0) {
            this.LoadDataParent();
          }
          this.viewLoading = false;
          this.createForm();

          this.listDefaultView.forEach((x) => {
            if (x.is_default) {
              x.isCheck = true;
            }
          });
        }
      }
    });

    this.LoadDataTemp();
    this.ListStatusDynamic();
    this.IsEdit = this.data._IsEdit;
    this.getTreeValue();
    // this.focusInput.nativeElement.focus();
  }

  LoadDeparment() {
    this._Services.DeptDetail(this.item.RowID).subscribe((res) => {
      if (res && res.status == 1) {
        this.item = res.data;
        this.item.RowID = res.data.id_row;
        this.list_Owners = res.data.Owners;
        this.LoadDetail(res.data);
        if (+this.item.ParentID > 0) {
          this.LoadDataParent();
        }
        this.createForm();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  LoadDataParent() {
    // this._Services.DeptDetail(this.item.ParentID).subscribe((res) => {
    //   if (res && res.status == 1) {
    //     this.dataParent = res.data;
    //     if (res.data.Template[0] && res.data.Template[0].TemplateID) {
    //       this.dataParent.TemplateID = res.data.Template[0].TemplateID;
    //       if (this.TempSelected > 0 && this.item.RowID > 0) {
    //         if (this.TempSelected != res.data.Template[0].TemplateID) {
    //           this.UseParentTemplate = false;
    //         } else {
    //           this.UseParentTemplate = true;
    //         }
    //       } else {
    //         if(this.IsInListCustom(res.data.Template[0].TemplateID)){
    //           this.TempSelected = res.data.Template[0].TemplateID;
    //         }
    //         this.UseParentTemplate = true;
    //         // this.LoadListSTT();
    //         if (this.item.RowID == 0) {
    //           this.LoadListSTT();
    //         }
    //       }
    //     }
    //   } else {
    //     this.layoutUtilsService.showError(res.error.message);
    //   }
    // });
  }

  LoadParentStatus() {
    var x = this.litsTemplateDemo.find(
      (x) => x.id_row == this.dataParent.TemplateID
    );
    if (x) {
      this.listSTT = x.status;
    }
  }

  LoadListSTT() {
    this.listSTT = [];
    var x = this.litsTemplateDemo.find((x) => x.id_row == this.TempSelected);
    if (x) {
      this.listSTT = x.status;
    }
  }

  LoadDetail(item) {
    if (
      item.Template[0] &&
      item.Template[0].TemplateID &&
      this.IsInListCustom(item.Template[0].TemplateID)
    ) {
      this.TempSelected = item.Template[0].TemplateID;
    }
    // this.LoadListSTT();
    if (this.item.RowID == 0) {
      this.LoadListSTT();
    }
    this.listDefaultView.forEach((x) => {
      var isCheck = item.DefaultView.find((view) => view.viewid == x.id_row);
      if (isCheck || x.is_default) {
        x.isCheck = true;
      } else {
        x.isCheck = false;
      }
    });
  }

  LoadDataTemp() {
    //load lại
    this.weworkService.ListTemplateByCustomer().subscribe((res) => {
      if (res && res.status === 1) {
        this.litsTemplateDemo = res.data;
        if (this.TempSelected == 0 || !this.IsInListCustom(this.TempSelected)) {
          this.TempSelected = this.litsTemplateDemo[0].id_row;
        }
        if (this.item.RowID == 0) {
          this.LoadListSTT();
        }
      }
      this.changeDetectorRefs.detectChanges();
    });
  }

  IsInListCustom(idtemplate) {
    if (this.litsTemplateDemo?.length > 0) {
      const index = this.litsTemplateDemo.findIndex(
        (x) => x.id_row == idtemplate
      );
      if (index >= 0) {
        return true;
      }
    }
    return false;
  }

  clickOnUser = (event: Event) => {
    // Prevent opening anchors the default way
    event.preventDefault();
    const anchor = event.target as HTMLAnchorElement;

    this.layoutUtilsService.showInfo("user clicked");
  };

  isCompleteStep1() {
    if (this.itemFormGroup) {
      const controls = this.itemFormGroup.controls;
      if (this.itemFormGroup.invalid) {
        Object.keys(controls).forEach((controlName) =>
          controls[controlName].markAsTouched()
        );
        this.hasFormErrors = true;
        return true;
      }
      if (this.itemFormGroup.controls["title"].value.trim() == "") {
        return true;
      }
    }

    return false;
  }

  getTreeValue() {
    // this.danhMucChungService.Get_MaCoCauToChuc_HR().subscribe((res) => {
    // 	if (res.data && res.data.length > 0) {
    // 		this.datatree.next(res.data);
    // 		this.changeDetectorRefs.detectChanges();
    // 	}
    // });
  }

  GetValueNode(val: any) {
    if (!val) {
      return;
    }
    this.ID_Struct = val.RowID;
    this.danhMucChungService
      .GetListPositionbyStructure(this.ID_Struct)
      .subscribe((res) => {
        if (res.data.length > 0) {
        } else {
          // this.itemFormGroup.controls['chucDanh'].setValue('');
          // this.itemFormGroup.controls['chucVu'].setValue('');
        }
      });
  }

  createForm() {
    this.itemFormGroup = this.fb.group({
      title: [this.item.title, Validators.required],
      dept_name: [this.item.id_cocau],
    });
    this.formData = this.fb.group({
      title: [this.item.title, Validators.required],
      dept_name: [this.item.id_cocau],
    });
    // this.itemFormGroup.controls["title"].markAsTouched();
    // this.itemFormGroup.controls["dept_name"].markAsTouched();
  }

  /** UI */
  getTitle(): string {
    let result = this.translate.instant("department.allgood");
    if (!this.item || !this.item.id_row) {
      return result;
    }
    result = this.translate.instant("department.chinhsua");
    if (this.item.ParentID > 0) {
      result = this.translate.instant("department.chinhsuafolder");
    }
    return result;
  }

  /** ACTIONS */
  prepare(): DepartmentModel {
    const controls = this.itemFormGroup.controls;
    const _item = new DepartmentModel();
    _item.clear();
    _item.id_row = this.item.id_row;
    _item.ParentID = this.item.ParentID ? this.item.ParentID : 0;
    _item.id_cocau = controls["dept_name"].value
      ? controls["dept_name"].value
      : 0;
    _item.title = controls["title"].value.trim();
    _item.Owners = [];

    this.list_Owners.map((item, index) => {
      const ct = new DepartmentOwnerModel();
      if (item.id_row == undefined) {
        item.id_row = 0;
      }
      ct.id_row = item.id_row;
      ct.id_department = this.item.id_row ? this.item.id_row : 0;
      ct.id_user = item.id_nv;
      ct.type = item.type;
      _item.Owners.push(ct);
    });

    _item.IsDataStaff_HR = this.IsDataStaff_HR;
    _item.ReUpdated = this.ReUpdated;
    _item.DefaultView = [];
    this.listDefaultView
      .filter((x) => x.isCheck == true)
      .map((item, index) => {
        const ct = new DepartmentViewModel();
        if (item.id_row == undefined) {
          item.id_row = 0;
        }
        ct.id_row = item.id_row;
        ct.id_department = this.item.id_row ? this.item.id_row : 0;
        ct.viewid = item.view_id ? item.view_id : 0;
        ct.is_default = item.is_default;
        _item.DefaultView.push(ct);
      });
    _item.TemplateID = this.TempSelected;
    if (this.UseParentTemplate && +this.item.ParentID > 0) {
      _item.TemplateID = this.dataParent.TemplateID;
    }

    this.listSTT.forEach((element) => {
      const item = new StatusDynamicModel();
      item.clear();
      item.Id_row = element.id_row ? element.id_row : 0;
      item.StatusName = element.StatusName;
      item.Color = element.color ? element.color : "";

      item.Description = element.Description ? element.Description : "";
      item.Id_project_team = element.id_project_team
        ? element.id_project_team
        : 0;
      item.id_department = element.id_department
        ? element.id_department
        : this.item.RowID;
      item.IsDeadline = element.IsDeadline ? element.IsDeadline : false;
      item.IsFinal = element.IsFinal ? element.IsFinal : false;
      item.IsToDo = element.IsTodo ? element.IsTodo : false;
      item.IsDefault = element.IsDefault ? element.IsDefault : false;
      //   item.Follower = status.Follower;
      item.Type = element.Type ? element.Type : "2";
      _item.status.push(item);
    });

    return _item;
  }

  onSubmit(withBack: boolean = false) {
    this.hasFormErrors = false;
    this.loadingAfterSubmit = false;
    const controls = this.itemFormGroup.controls;
    /* check form */
    if (this.itemFormGroup.invalid) {
      Object.keys(controls).forEach((controlName) =>
        controls[controlName].markAsTouched()
      );

      this.hasFormErrors = true;
      return;
    }
    const updatedegree = this.prepare();
    if (updatedegree.Owners.length == 0) {
      this.layoutUtilsService.showError("Người sở hữu là bắt buộc");
      return;
    }
    if (updatedegree.id_row > 0) {
      this.Update(updatedegree, withBack);
    } else {
      this.Create(updatedegree, withBack);
    }
  }

  AddTemplate() {
    const item = "Danh sách giao diện template";
    const dialogRef = this.dialog.open(TemplateCenterComponent, {
      data: { item },
      width: "50vw",
      height: "95vh",
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      } else {
        this.changeDetectorRefs.detectChanges();
      }
    });
  }

  Update(_item: DepartmentModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.viewLoading = true;
    this.disabledBtn = true;
    this.layoutUtilsService.showWaitingDiv();
    this._Services.UpdateDept(_item).subscribe((res) => {
      /* Server loading imitation. Remove this on real code */
      this.layoutUtilsService.OffWaitingDiv();
      this.disabledBtn = false;
      if (res && res.status === 1) {
        if (withBack == true) {
          this.dialogRef.close({
            _item,
          });
        } else {
          this.ngOnInit();
          const _messageType = this.translate.instant(
            "GeneralKey.capnhatthanhcong"
          );
          this.layoutUtilsService
            .showActionNotification(
              _messageType,
              MessageType.Update,
              4000,
              true,
              false
            )
            .afterDismissed()
            .subscribe((tt) => {});
          // this.focusInput.nativeElement.focus();
        }
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
      this.changeDetectorRefs.detectChanges();
    });
  }

  Create(_item: DepartmentModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.disabledBtn = true;
    this.layoutUtilsService.showWaitingDiv();
    this._Services.InsertDept(_item).subscribe((res) => {
      this.layoutUtilsService.OffWaitingDiv();
      this.disabledBtn = false;
      if (res && res.status === 1) {
        if (withBack == true) {
          this.dialogRef.close({
            _item,
          });
        } else {
          this.dialogRef.close();
        }
      } else {
        this.viewLoading = false;
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
      this.changeDetectorRefs.detectChanges();
    });
  }

  onAlertClose($event) {
    this.hasFormErrors = false;
  }

  close() {
    const _title = this.translate.instant("GeneralKey.xacnhanthoat");
    const _description = this.translate.instant("GeneralKey.bancomuonthoat");
    const _waitDesciption = this.translate.instant("GeneralKey.dangdong");
    const _deleteMessage = this.translate.instant(
      "GeneralKey.thaydoithanhcong"
    );
    if (this.isChangeData()) {
      const dialogRef = this.layoutUtilsService.deleteElement(
        _title,
        _description,
        _waitDesciption
      );
      dialogRef.afterClosed().subscribe((res) => {
        if (!res) {
          return;
        }
        this.dialogRef.close();
      });
    } else {
      this.dialogRef.close();
    }
  }

  isChangeData() {
    const val1 = this.prepare();
    if (val1.title != this.item.title) {
      return true;
    }
    if (this.item.RowID > 0 && val1.Owners != this.item.Owners) {
      return true;
    }
    return false;
  }

  reset() {
    this.item = Object.assign({}, this.item);
    this.createForm();
    this.hasFormErrors = false;
    this.itemFormGroup.markAsPristine();
    this.itemFormGroup.markAsUntouched();
    this.itemFormGroup.updateValueAndValidity();
  }

  getKeyword() {
    let i = this.CommentTemp.lastIndexOf("@");
    if (i >= 0) {
      let temp = this.CommentTemp.slice(i);
      if (temp.includes(" ")) {
        return "";
      }
      return this.CommentTemp.slice(i);
    }
    return "";
  }

  getOptions() {
    var options: any = {
      showSearch: true,
      keyword: this.getKeyword(),
      data: this.listUser.filter(
        (x) => this.selected.findIndex((y) => x.id_nv == y.id_nv) < 0
      ),
    };
    return options;
  }

  onSearchChange($event) {
    this.CommentTemp = (<HTMLInputElement>(
      document.getElementById("InputUser")
    )).value;

    if (this.selected.length > 0) {
      var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm;
      var match = this.CommentTemp.match(reg);
      if (match != null && match.length > 0) {
        let arr = match.map((x) => x);
        this.selected = this.selected.filter((x) =>
          arr.includes("@" + x.username)
        );
      } else {
        this.selected = [];
      }
    }
    this.options = this.getOptions();
    if (this.options.keyword) {
      let el = $event.currentTarget;
      let rect = el.getBoundingClientRect();
      this.myPopover.show();
      this.changeDetectorRefs.detectChanges();
    }
  }

  click($event, vi = -1) {
    this.myPopover.hide();
  }

  ItemSelected(data, type = 1) {
    if (data.id_nv == this.UserId) {
      return;
    }
    var index = this.list_Owners.findIndex((x) => x.id_nv == data.id_nv);

    if (index >= 0) {
      if (type == this.list_Owners[index].type) {
        this.list_Owners.splice(index, 1);
      } else {
        this.list_Owners[index].type = type;
      }
    } else {
      data.type = type;
      this.list_Owners.push(data);
    }

    // if (type==1) {
    // 	if (data.id_nv == this.UserId) {
    // 		return;
    // 	}
    // 	var index = this.list_Owners.findIndex(
    // 		(x) => x.id_nv == data.id_nv
    // 	);
    // 	if (index >= 0) {
    // 		this.list_Owners.splice(index, 1);
    // 	} else {
    // 		this.list_Owners.push(data);
    // 	}
    // } else {
    // 	var index = this.list_Assign.findIndex(
    // 		(x) => x.id_nv == data.id_nv
    // 	);
    // 	if (index >= 0) {
    // 		this.list_Assign.splice(index, 1);
    // 	} else {
    // 		this.list_Assign.push(data);
    // 	}
    // }
  }

  getlist_Assign() {
    var x = this.list_Owners.filter((x) => x.type == 2);
    if (x) {
      return x;
    }
    return x;
  }

  getlist_Owners() {
    var x = this.list_Owners.filter((x) => x.type == 1);
    if (x) {
      return x;
    }
    return x;
  }

  stopPropagation(event) {
    event.stopPropagation();
  }

  Next() {
    if (this.isComplete) {
      this.step = 5;
    } else {
      this.step += 1;
    }
  }

  Pre() {
    if (this.isComplete) {
      this.step = 5;
    } else {
      this.step -= 1;
    }
  }

  viewDetail(val) {
    this.step = val;
  }

  idfocus = 0;

  sttFocus(value) {
    this.idfocus = value;
  }

  sttFocusout(value, status) {
    this.idfocus = 0;
    const index = this.listSTT.findIndex(
      (x) => x.StatusName.trim() === value.trim()
    );
    if (index >= 0 && value.trim() != status.StatusName.trim()) {
      this.layoutUtilsService.showError("Trạng thái đã tồn tại");
      status.StatusName = status.StatusName + " ";
      return;
    }
    if (!value) {
      status.StatusName = status.StatusName + " ";
      // this.LoadListSTT();
      return;
    }
    status.StatusName = value;
    // const _item = new UpdateQuickModel();
    // _item.clear();
    // _item.id_row = status.id_row;
    // _item.columname = 'StatusName';
    // _item.values = value;
    // _item.id_template = this.TempSelected;
    // this.UpdateQuick(_item);
  }

  ChangeColor(value, status) {
    status.color = value;
    // const _item = new UpdateQuickModel();
    // _item.clear();
    // _item.id_row = status.id_row;
    // _item.columname = 'color';
    // _item.values = value;
    // _item.id_template = this.TempSelected;
    // this.UpdateQuick(_item);
  }

  ChangeTemplate(id) {
    this.TempSelected = id;
    this.LoadListSTT();
  }

  isAddTemplate = false;
  updateTemp = 0;
  isAddStatus = false;
  TempSelected = 0;

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
  Delete_Status(stt) {
    const index = this.listSTT.findIndex(
      (x) => x.StatusName === stt.StatusName
    );
    if (index >= 0) {
      this.listSTT.splice(index, 1);
    }
  }

  focusOutSTT(value) {
    this.isAddStatus = false;
    if (!value) {
      return;
    }
    const index = this.listSTT.findIndex((x) => x.StatusName === value);
    if (index >= 0) {
      this.layoutUtilsService.showError("Trạng thái đã tồn tại");
      return;
    }

    let _item = {
      IsTodo: false,
      // Position: ,
      StatusName: value,
      color: "rgb(255, 0, 0)",
      id_project_team: 0,
      id_department: this.item.RowID,
      Type: 2,
    };
    this.listSTT.push(_item);
    // const _item = new UpdateQuickModel();
    // _item.clear();
    // _item.id_row = 0;
    // _item.columname = 'StatusName';
    // _item.values = value;
    // _item.istemplate = false;
    // _item.id_template = this.TempSelected;
    // this.UpdateQuick(_item);
  }

  ListStatusDynamic() {
    if (this.item.RowID > 0) {
      this.layoutUtilsService.showWaitingDiv();
      this.weworkService
        .ListStatusDynamic(this.item.RowID, true)
        .subscribe((res) => {
          this.layoutUtilsService.OffWaitingDiv();
          if (res && res.status == 1) {
            this.listSpaceStatus = res.data;
            this.listSpaceStatus.forEach((element) => {
              element.newvalue = 0;
            });
            this.LoadSpaceStatus();
            this.changeDetectorRefs.detectChanges();
          }
        });
    }
  }

  LoadSpaceStatus() {
    if (this.listSpaceStatus && this.isSpaceStatus) {
      // this.listSTT = this.listStatus;
      const listTemp = [];
      this.listSpaceStatus.forEach((element) => {
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
      this.changeDetectorRefs.detectChanges();
    }
  }

  listSTTDeadline() {
    return this.listSTT.filter((x) => x.Type === 3);
  }
  listSTTHoatdong() {
    return this.listSTT.filter((x) => x.Type === 2);
  }

  listSTTMoitao() {
    return this.listSTT.filter((x) => x.Type === 1);
  }

  listSTTFinal() {
    return this.listSTT.filter((x) => x.Type === 4);
  }

  SubmitData() {
    const controls = this.itemFormGroup.controls;
    if (this.itemFormGroup.invalid) {
      Object.keys(controls).forEach((controlName) =>
        controls[controlName].markAsTouched()
      );

      this.hasFormErrors = true;
      return;
    }

    const updatedegree = this.prepare();

    console.log(updatedegree);
    // return;
    if (updatedegree.id_row > 0) {
      this.Update(updatedegree, true);
    } else {
      this.Create(updatedegree, true);
    }
  }

  CheckRole() {
    if (this.isSpaceStatus) {
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

  /*
    Đổi vị trí
  */
  drop(event: CdkDragDrop<string[]>) {
    moveItemInArray(
      this.listSTTHoatdong(),
      event.previousIndex,
      event.currentIndex
    );
    const previous = this.listSTTHoatdong()[event.previousIndex];
    const curent = this.listSTTHoatdong()[event.currentIndex];
    // this.listSTTHoatdong()[event.previousIndex] = curent;
    // this.listSTTHoatdong()[event.currentIndex] = previous;
    // const positon = new PositionModel();
    // positon.id_row_from = previous.id_row;
    // positon.position_from = previous.Position;
    // positon.id_row_to = curent.id_row;
    // positon.position_to = curent.Position;
    // positon.columnname = this.data.columnname;
    // positon.id_columnname = this.data.id_row;
    // console.log(positon);
    // this.dropPosition(positon);
    this.MoveItem(curent, previous);
    // if (event.previousContainer === event.container) {
    //     moveItemInArray(event.container.data, event.previousIndex, event.currentIndex);
    //   } else {
    //     transferArrayItem(event.previousContainer.data,
    //       event.container.data,
    //       event.previousIndex,
    //       event.currentIndex);
    //   }
  }

  MoveItem(curent, previous) {
    
    const position_from = previous.Position;
    const position_to = curent.Position;
    previous.Position = position_to;
    curent.Position = position_from;
    const indexcurent = this.listSTT.findIndex(
      (x) => x.StatusName.trim() == curent.StatusName.trim()
    );
    const indexprevious = this.listSTT.findIndex(
      (x) => x.StatusName.trim() == previous.StatusName.trim()
    );
    if (indexcurent >= 0 && indexprevious >= 0) {
      this.listSTT[indexcurent] = previous;
      this.listSTT[indexprevious] = curent;
    }
  }
  /*
    End đổi vị trí
  */

  @HostListener("document:keydown", ["$event"])
  onKeydownHandler1(event: KeyboardEvent) {
    if (event.keyCode == 27) {
      //phím ESC
    }
  }
}
