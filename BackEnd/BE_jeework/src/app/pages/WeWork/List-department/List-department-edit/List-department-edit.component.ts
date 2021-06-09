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
} from "@angular/forms";
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";

// import { AngularEditorConfig } from '@kolkov/angular-editor';
import { TranslateService } from "@ngx-translate/core";
import { ReplaySubject, BehaviorSubject } from "rxjs";
import { Router } from "@angular/router";
import { ListDepartmentService } from "../Services/List-department.service";
import { DanhMucChungService } from "./../../../../_metronic/jeework_old/core/services/danhmuc.service";
import {
  DepartmentModel,
  DepartmentOwnerModel,
} from "../Model/List-department.model";
import { PopoverContentComponent } from "ngx-smart-popover";
import { WeWorkService } from "../../services/wework.services";
@Component({
  selector: "kt-List-department-edit",
  templateUrl: "./List-department-edit.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DepartmentEditComponent implements OnInit {
  item: DepartmentModel;
  oldItem: DepartmentModel;
  itemForm: FormGroup;
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
  @ViewChild("myPopoverC", { static: true })
  myPopover: PopoverContentComponent;
  listUser: any[] = [];
  @ViewChild("hiddenText", { static: true }) textEl: ElementRef;
  listChiTiet: any[] = [];
  list_Owners: any[] = [];
  IsDataStaff_HR = false;
  selectedUser = [];
  IsChangeUser = false;
  constructor(
    public dialogRef: MatDialogRef<DepartmentEditComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private changeDetectorRefs: ChangeDetectorRef,
    private _Services: ListDepartmentService,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    private danhMucChungService: DanhMucChungService,
    public weworkService: WeWorkService,
    private router: Router
  ) {}
  /** LOAD DATA */
  ngOnInit() {
    this.title = this.translate.instant("GeneralKey.choncocautochuc") + "";
    this.item = this.data._item;
    this.options = this.getOptions();
    this.weworkService.list_account({}).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listUser = res.data;
      }
      this.options = this.getOptions();
      this.changeDetectorRefs.detectChanges();
    });
    this.IsEdit = this.data._IsEdit;
    if (this.item.RowID > 0) {
      this._Services.DeptDetail(this.item.RowID).subscribe((res) => {
        if (res && res.status == 1) {
          console.log(res.data);
          this.item = res.data;
          this.list_Owners = res.data.Owners;
          this.selectedUser = this.item.Owners;
          console.log(this.selectedUser);
          this.createForm();
          this.changeDetectorRefs.detectChanges();
        }
      });

      this.viewLoading = true;
    } else {
      this.viewLoading = false;
    }
    this.createForm();
    this.getTreeValue();
    // this.focusInput.nativeElement.focus();
  }
  clickOnUser = (event: Event) => {
    // Prevent opening anchors the default way
    event.preventDefault();
    const anchor = event.target as HTMLAnchorElement;

    this.layoutUtilsService.showInfo("user clicked");
  };

  getTreeValue() {
    this.danhMucChungService.Get_MaCoCauToChuc_HR().subscribe((res) => {
      if (res.data && res.data.length > 0) {
        this.datatree.next(res.data);
        this.changeDetectorRefs.detectChanges();
        // this.selectedNode.next({
        // 	RowID: "" + this.item.id_cocau,
        // });
        // if ("" + this.item.id_cocau != undefined)
        // 	this.ID_Struct = '' + this.item.id_cocau;
        // else
        // 	this.ID_Struct = '';
        // this.loadListChucVu();
      }
    });
  }
  GetValueNode(val: any) {
    this.ID_Struct = val.RowID;
    this.danhMucChungService
      .GetListPositionbyStructure(this.ID_Struct)
      .subscribe((res) => {
        // this.listChucDanh = res.data;
        if (res.data.length > 0) {
        } else {
          // this.itemForm.controls['chucDanh'].setValue('');
          // this.itemForm.controls['chucVu'].setValue('');
        }
      });
  }
  createForm() {
    this.itemForm = this.fb.group({
      title: [this.item.title, Validators.required],
      dept_name: [this.item.id_cocau],
      id_user: [""],
    });
    // this.itemForm.controls["title"].markAsTouched();
    // this.itemForm.controls["dept_name"].markAsTouched();
  }

  /** UI */
  getTitle(): string {
    let result = this.translate.instant("department.taomoi");
    if (!this.item || !this.item.id_row) {
      return result;
    }

    result = this.translate.instant("department.chinhsua");
    return result;
  }
  /** ACTIONS */
  prepare(): DepartmentModel {
    const controls = this.itemForm.controls;
    const _item = new DepartmentModel();
    _item.id_row = this.item.id_row;
    _item.id_cocau = controls["dept_name"].value
      ? controls["dept_name"].value
      : 0;
    _item.title = controls["title"].value;

    if (this.selectedUser.length > 0) {
      this.list_Owners.map((item, index) => {
        let _true = this.selectedUser.find((x) => x.id_user === item.id_nv);
        if (_true) {
          const ct = new DepartmentOwnerModel();
          if (item.id_row == undefined) item.id_row = 0;
          ct.id_row = item.id_row;
          ct.id_department = this.item.id_row;
          ct.id_user = item.id_nv;
          this.listChiTiet.push(ct);
        } else {
          const ct = new DepartmentOwnerModel();
          if (item.id_row == undefined) item.id_row = 0;
          ct.id_row = item.id_row;
          ct.id_department = this.item.id_row;
          ct.id_user = item.id_nv;
          this.listChiTiet.push(ct);
        }
      });
    }
    _item.Owners = this.listChiTiet;

    return _item;
  }
  onSubmit(withBack: boolean = false) {
    this.hasFormErrors = false;
    this.loadingAfterSubmit = false;
    const controls = this.itemForm.controls;
    /* check form */
    if (this.itemForm.invalid) {
      Object.keys(controls).forEach((controlName) =>
        controls[controlName].markAsTouched()
      );

      this.hasFormErrors = true;
      return;
    }
    if (
      this.IsDataStaff_HR &&
      this.itemForm.controls["dept_name"].value == ""
    ) {
      this.itemForm.controls["dept_name"].markAsTouched();
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

  Update(_item: DepartmentModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.viewLoading = true;
    this.disabledBtn = true;
    this._Services.UpdateDept(_item).subscribe((res) => {
      /* Server loading imitation. Remove this on real code */
      this.disabledBtn = false;
      this.changeDetectorRefs.detectChanges();
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
    });
  }

  Create(_item: DepartmentModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.disabledBtn = true;
    this._Services.InsertDept(_item).subscribe((res) => {
      this.disabledBtn = false;
      this.changeDetectorRefs.detectChanges();
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
    } else this.dialogRef.close();
  }
  isChangeData() {
    const val1 = this.prepare();
    if (val1.title != this.item.title) return true;
    if (this.IsChangeUser) return true;
    return false;
  }
  reset() {
    this.item = Object.assign({}, this.item);
    this.createForm();
    this.hasFormErrors = false;
    this.itemForm.markAsPristine();
    this.itemForm.markAsUntouched();
    this.itemForm.updateValueAndValidity();
  }

  getOptions() {
    var options: any = {
      showSearch: true,
      keyword: "",
      data: this.listUser,
    };
    return options;
  }
  click($event, vi = -1) {
    this.myPopover.hide();
  }
  ItemselectedUser(data) {
    this.IsChangeUser = true;
    console.log(this.selectedUser);

    var index = this.selectedUser.findIndex((x) => x.id_nv == data.id_nv);

    if (index >= 0) {
      this.selectedUser.splice(index, 1);
    } else {
      this.selectedUser.push(data);
    }
  }
  stopPropagation(event) {
    event.stopPropagation();
  }
}
