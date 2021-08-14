import { MenuAsideService } from './../../../../_metronic/jeework_old/core/_base/layout/services/menu-aside.service';
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { DanhMucChungService } from "./../../../../_metronic/jeework_old/core/services/danhmuc.service";
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
import { ProjectTeamDuplicateModel } from "../Model/department-and-project.model";
import { ProjectsTeamService } from "../Services/department-and-project.service";

@Component({
  selector: "kt-duplicate-project",
  templateUrl: "./duplicate-project.component.html",
})
export class DuplicateProjectComponent implements OnInit {
  item1: ProjectTeamDuplicateModel;
  oldItem: ProjectTeamDuplicateModel;
  item: ProjectTeamDuplicateModel;
  itemForm: FormGroup;
  hasFormErrors: boolean = false;
  viewLoading: boolean = false;
  loadingAfterSubmit: boolean = false;
  // @ViewChild("focusInput", { static: true }) focusInput: ElementRef;
  disabledBtn: boolean = false;
  IsEdit: boolean;
  IsProject: boolean;
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
  listUser: any[] = [];
  listChecked: any[] = [];
  filter: any = {};
  optionsModel: number[];
  checked: any[] = [];
  checkedAdmin: any[] = [];
  ShowDrop: boolean = false;
  colorCtr: AbstractControl = new FormControl(null);
  tendapb: string = "";
  mota: string = "";
  constructor(
    public dialogRef: MatDialogRef<DuplicateProjectComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private changeDetectorRefs: ChangeDetectorRef,
    private _service: ProjectsTeamService,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    private menuAsideService: MenuAsideService,
    public weworkService: WeWorkService,
  ) { }
  /** LOAD DATA */
  ngOnInit() {
    this.title =
      this.translate.instant("work.bansaocua") + " " + this.data._item.title;
    this.item1 = this.data._item;

    this.IsEdit = this.data._IsEdit;
    this.IsProject = this.data.is_project;
    if (this.IsProject) {
      this.tendapb = this.translate.instant("projects.tenduan") + "";
      this.mota = this.translate.instant("projects.motangangonveduan") + "";
    } else {
      this.tendapb = this.translate.instant("projects.phongban") + "";
      this.mota = this.translate.instant("projects.motangangonvephongban") + "";
    }
    if (this.item1.id > 0) {
      this.viewLoading = true;
    } else {
      this.viewLoading = false;
    }
    this.createForm();
    this.itemForm.controls["title"].setValue(this.title);
  }
  onSearchChange(searchValue: string): void { }
  createForm() {
    this.itemForm = this.fb.group({
      title: [this.item1.title, Validators.required],
      type: [this.item1.type],
      keep_creater: [this.item1.keep_creater],
      keep_checker: [this.item1.keep_checker],
      keep_follower: [this.item1.keep_follower],
      keep_deadline: [this.item1.keep_deadline],
      hour_adjusted: [this.item1.hour_adjusted, Validators.required],
      keep_checklist: [this.item1.keep_checklist],
      keep_child: [this.item1.keep_child],
      keep_tag: [this.item1.keep_tag],
      keep_admin: [this.item1.keep_admin],
      keep_member: [this.item1.keep_member],
      //keep_milestone: [this.item1.keep_milestone],
      keep_role: [this.item1.keep_role],
    });
    //this.itemForm.controls["title"].markAsTouched();
    //this.itemForm.controls["type"].markAsTouched();
    //this.itemForm.controls["keep_creater"].markAsTouched();
    //this.itemForm.controls["keep_checker"].markAsTouched();
    //this.itemForm.controls["keep_follower"].markAsTouched();
    //this.itemForm.controls["keep_deadline"].markAsTouched();
    //this.itemForm.controls["hour_adjusted"].markAsTouched();
    //this.itemForm.controls["keep_checklist"].markAsTouched();
    //this.itemForm.controls["keep_child"].markAsTouched();
    //this.itemForm.controls["keep_tag"].markAsTouched();
  }
  Keep_deadlineChange(val: any) {
    if (val > 1) {
      this.ShowDrop = true;
      this.itemForm.controls["hour_adjusted"].setValue("");
    } else {
      this.ShowDrop = false;
      this.itemForm.controls["hour_adjusted"].setValue(1);
    }
  }
  /** UI */
  getTitle(): string {
    let result = this.translate.instant("GeneralKey.themmoi");
    if (!this.item1 || !this.item1.id) {
      return result;
    }
    result = this.translate.instant("projects.nhanbanduan");
    return result;
  }
  /** ACTIONS */
  prepare(): ProjectTeamDuplicateModel {
    const controls = this.itemForm.controls;
    const item = new ProjectTeamDuplicateModel();
    item.id = this.item1.id;
    item.title = controls["title"].value;
    item.type = controls["type"].value;
    item.keep_deadline = controls["keep_deadline"].value;
    item.hour_adjusted = controls["hour_adjusted"].value;
    item.keep_creater = controls["keep_creater"].value;
    item.keep_checker = controls["keep_checker"].value;
    item.keep_follower = controls["keep_follower"].value;
    item.keep_checklist = controls["keep_checklist"].value;
    item.keep_child = controls["keep_child"].value;
    item.keep_tag = controls["keep_tag"].value;
    item.keep_admin = controls["keep_admin"].value;
    item.keep_member = controls["keep_member"].value;
    //item.keep_milestone = controls['keep_milestone'].value;
    item.keep_role = controls["keep_role"].value;

    return item;
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
    const updatedegree = this.prepare();

    if (updatedegree.id > 0) {
      this.Update(updatedegree, withBack);
    } else {
      this.Create(updatedegree, withBack);
    }
  }
  filterConfiguration(): any {
    const filter: any = {};
    return filter;
  }
  Update(_item: ProjectTeamDuplicateModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.viewLoading = true;
    this.disabledBtn = true;
    this._service.Duplicate(_item).subscribe((res) => {
      this.disabledBtn = false;
      this.menuAsideService.loadMenu()
      if (res && res.status === 1) {
        window.location.reload();
        if (withBack == true) {
          this.dialogRef.close({
            res,
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
            .subscribe((tt) => { });
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
  Create(_item: ProjectTeamDuplicateModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.disabledBtn = true;
    this._service.Duplicate(_item).subscribe((res) => {
      this.disabledBtn = false;
      this.menuAsideService.loadMenu()
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
    this.dialogRef.close();
  }
  reset() {
    this.item1 = Object.assign({}, this.item1);
    this.createForm();
    this.hasFormErrors = false;
    this.itemForm.markAsPristine();
    this.itemForm.markAsUntouched();
    this.itemForm.updateValueAndValidity();
  }

  @HostListener("document:keydown", ["$event"])
  onKeydownHandler(event: KeyboardEvent) {
    if (event.ctrlKey && event.keyCode == 13) {
      //phím Enter
      this.item1 = this.data._item;
      if (this.viewLoading == true) {
        this.onSubmit(true);
      } else {
        this.onSubmit(false);
      }
    }
  }
  text(e: any) {
    if (
      !(
        (e.keyCode > 95 && e.keyCode < 106) ||
        (e.keyCode > 45 && e.keyCode < 58) ||
        e.keyCode == 8
      )
    ) {
      e.preventDefault();
    }
  }
}
