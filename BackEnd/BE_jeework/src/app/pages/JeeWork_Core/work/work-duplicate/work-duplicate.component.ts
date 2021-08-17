import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
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
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { TranslateService } from "@ngx-translate/core";
import { ReplaySubject, BehaviorSubject, Observable } from "rxjs";
import { Router } from "@angular/router";


import { WeWorkService } from "../../services/wework.services";
import { useAnimation } from "@angular/animations";
import { UserInfoModel, WorkDuplicateModel } from "../work.model";
import { WorkService } from "../work.service";
import { tinyMCE } from 'src/app/_metronic/jeework_old/components/tinyMCE';
import { PopoverContentComponent } from "ngx-smart-popover";
import * as moment from 'moment';

@Component({
  selector: "kt-work-duplicate",
  templateUrl: "./work-duplicate.component.html",
})
export class DuplicateWorkComponent implements OnInit {
  oldItem: WorkDuplicateModel;
  item: WorkDuplicateModel;
  itemForm: FormGroup;
  hasFormErrors: boolean = false;
  viewLoading: boolean = false;
  loadingAfterSubmit: boolean = false;
  disabledBtn: boolean = false;
  title: string = "";
  selectedNode: BehaviorSubject<any> = new BehaviorSubject([]);
  listUser: any[] = [];
  ShowDrop: boolean = false;
  listType: any[] = [];
  isParent: boolean = false;
  type_Duplicate: boolean = false;
  listProject: any[] = [];
  tinyMCE = {};
  NoiDung: string;
  minDate: any = new Date();
  public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public projectFilterCtrl: FormControl = new FormControl();
  @ViewChild("follower", { static: true }) myPopover: PopoverContentComponent;
  @ViewChild("Assign", { static: true })
  myPopover_Assign: PopoverContentComponent;
  selected: any[] = [];
  selected_Assign: any[] = [];
  @ViewChild("hiddenText", { static: true }) textEl: ElementRef;
  @ViewChild("hiddenText_Assign", { static: true }) text_Assign: ElementRef;
  ListFollower: string = "";
  _Assign: string = "";
  list_follower: any[] = [];
  list_Assign: any[] = [];
  list_User: any[] = [];
  options: any = {};
  options_assign: any = {};
  constructor(
    public dialogRef: MatDialogRef<DuplicateWorkComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private changeDetectorRefs: ChangeDetectorRef,
    private _service: WorkService,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    public weworkService: WeWorkService,
    private router: Router
  ) { }
  /** LOAD DATA */
  ngOnInit() {
    this.item = this.data._item;
    this.tinyMCE = tinyMCE;
    this.weworkService.list_account({}).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listUser = res.data;
      }
      this.options = this.getOptions();
      // this.options_assign = this.getOptions_Assign();
      this.changeDetectorRefs.detectChanges();
    });
    if (this.item.id_row > 0) {
      this.viewLoading = true;
    } else {
      this.viewLoading = false;
    }
    this.weworkService.lite_project_team_byuser("").subscribe((res) => {
      this.disabledBtn = false;
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listProject = res.data;
        this.BindList(this.data._item.id_project_team);
        this.setUpDropSearchProject();
        this.changeDetectorRefs.detectChanges();
      }
    });

    this.createForm();
  }
  createForm() {
    if (this.item.type > 1) {
      this.type_Duplicate = false;
      this.itemForm = this.fb.group({
        id_project_team: ["", Validators.required],
        deadline: ["", Validators.required],
        duplicate_child: ["", Validators.required],
        id_group: ["", Validators.required],
      });
      this.itemForm.controls["id_project_team"].markAsTouched();
    } else {
      this.type_Duplicate = true;
      // if (this.data._item.id_parent == null) {
      // 	this.isParent = false;
      // 	this.itemForm.controls["id_parent"].setValue('0');
      // }
      // else {
      // 	this.isParent = true;
      // 	this.itemForm.controls["id_parent"].setValue('');
      // }
      this.itemForm = this.fb.group({
        deadline: ["", Validators.required],
        duplicate_child: ["", Validators.required],
        id_group: ["", Validators.required],
        title: ["", Validators.required],
        start_date: ["", Validators.required],
        NoiDung: [""],
        id_parent: ["", Validators.required],
        urgent: ["", Validators.required],
        required_result: ["", Validators.required],
        timeline: ["", Validators.required],
      });
      this.title = " Bảo sao của " + this.data._item.title;
      // this.itemForm.controls["title"].setValue(this.title);
      // this.itemForm.controls["title"].markAsTouched();
      // this.itemForm.controls["start_date"].markAsTouched();
      // this.itemForm.controls["id_parent"].markAsTouched();
      // this.itemForm.controls["urgent"].markAsTouched();
      // this.itemForm.controls["required_result"].markAsTouched();
      // this.itemForm.controls["timeline"].markAsTouched();
    }
    // this.itemForm.controls["deadline"].markAsTouched();
    // this.itemForm.controls["duplicate_child"].markAsTouched();
    // this.itemForm.controls["id_group"].markAsTouched();
  }
  getKeyword() {
    let i = this.ListFollower.lastIndexOf("@");
    if (i >= 0) {
      let temp = this.ListFollower.slice(i);
      if (temp.includes(" ")) return "";
      return this.ListFollower.slice(i);
    }
    return "";
  }
  getOptions() {
    var options: any = {
      showSearch: false,
      keyword: this.getKeyword(),
      data: this.listUser.filter(
        (x) => this.selected.findIndex((y) => x.id_nv == y.id_nv) < 0
      ),
    };
    return options;
  }
  onSearchChange($event) {
    this.ListFollower = (<HTMLInputElement>(
      document.getElementById("InputUser")
    )).value;
    if (this.selected.length > 0) {
      var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm;
      var match = this.ListFollower.match(reg);
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
  ItemSelected(data) {
    this.selected = this.list_follower;
    this.selected.push(data);
    let i = this.ListFollower.lastIndexOf("@");
    this.ListFollower =
      this.ListFollower.substr(0, i) + "@" + data.username + " ";
    this.myPopover.hide();
    let ele = <HTMLInputElement>document.getElementById("InputUser");
    ele.value = this.ListFollower;
    ele.focus();
    this.changeDetectorRefs.detectChanges();
  }
  getKeyword_Assign() {
    let i = this._Assign.lastIndexOf("@");
    if (i >= 0) {
      let temp = this._Assign.slice(i);
      if (temp.includes(" ")) return "";
      return this._Assign.slice(i);
    }
    return "";
  }
  getOptions_Assign() {
    var options_assign: any = {
      showSearch: false,
      keyword: this.getKeyword_Assign(),
      data: this.listUser.filter(
        (x) => this.selected_Assign.findIndex((y) => x.id_nv == y.id_nv) < 0
      ),
    };
    return options_assign;
  }

  click_Assign($event, vi = -1) {
    this.myPopover_Assign.hide();
  }
  onSearchChange_Assign($event) {
    this._Assign = (<HTMLInputElement>(
      document.getElementById("InputAssign")
    )).value;

    if (this.selected_Assign.length > 0) {
      var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm;
      var match = this._Assign.match(reg);
      if (match != null && match.length > 0) {
        let arr = match.map((x) => x);
        this.selected_Assign = this.selected_Assign.filter((x) =>
          arr.includes("@" + x.username)
        );
      } else {
        this.selected_Assign = [];
      }
    }
    this.options = this.getOptions_Assign();
    if (this.options.keyword) {
      let el = $event.currentTarget;
      let rect = el.getBoundingClientRect();
      this.myPopover_Assign.show();
      this.changeDetectorRefs.detectChanges();
    }
  }
  ItemSelected_Assign(data) {
    this.selected_Assign = this.list_Assign;
    this.selected_Assign.push(data);
    let i = this._Assign.lastIndexOf("@");
    this._Assign = this._Assign.substr(0, i) + "@" + data.username + " ";
    this.myPopover_Assign.hide();
    let ele = <HTMLInputElement>document.getElementById("InputAssign");
    ele.value = this._Assign;
    ele.focus();
    this.changeDetectorRefs.detectChanges();
  }
  setUpDropSearchProject() {
    this.projectFilterCtrl.setValue("");
    this.filterProject();
    this.projectFilterCtrl.valueChanges.pipe().subscribe(() => {
      this.filterProject();
    });
  }
  protected filterProject() {
    if (!this.listProject) {
      return;
    }
    let search = this.projectFilterCtrl.value;
    if (!search) {
      this.filtereproject.next(this.listProject.slice());
      return;
    } else {
      search = search.toLowerCase();
    }
    // filter the banks
    this.filtereproject.next(
      this.listUser.filter(
        (bank) => bank.hoten.toLowerCase().indexOf(search) > -1
      )
    );
  }
  DeadlineChange(val: any) {
    if (val > 0) {
      this.ShowDrop = true;
      this.itemForm.controls["deadline"].setValue("");
    } else {
      this.ShowDrop = false;
      this.itemForm.controls["deadline"].setValue(" ");
    }
  }
  /** UI */
  getTitle(): string {
    let result =
      this.translate.instant("work.nhanbancongviec") +
      " " +
      this.data._item.title;
    if (!this.item || this.item.type == 1) {
      return result;
    }
    result =
      this.translate.instant("work.nhanbancongviec") +
      " " +
      this.translate.instant("work.vaoduankhac");
    return result;
  }
  BindList(id_project: any) {
    this.weworkService.lite_workgroup(id_project).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listType = res.data;
        this.changeDetectorRefs.detectChanges();
      }
    });
  }
  /** ACTIONS */
  prepare(): WorkDuplicateModel {
    const controls = this.itemForm.controls;
    const item = new WorkDuplicateModel();
    item.id = this.data._item.id_row;
    item.type = this.item.type;
    item.deadline = controls["deadline"].value;
    item.id_group = controls["id_group"].value;
    item.duplicate_child =
      controls["duplicate_child"].value == "1" ? "true" : "false";
    if (item.type == 2) {
      item.id_project_team = controls["id_project_team"].value;
    } else {
      item.title = controls["title"].value;
      if (this.data._item.id_parent > 0) {
        item.id_parent = controls["id_parent"].value;
      }
      if (this.selected_Assign.length > 0) {
        this.listUser.map((item, index) => {
          let _true = this.selected_Assign.find((x) => x.id_nv === item.id_nv);
          if (_true) {
            const _model = new UserInfoModel();
            _model.id_user = item.id_nv;
            _model.loai = 1;
            this.list_User.push(_model);
          }
        });
      }
      if (this.selected.length > 0) {
        this.listUser.map((item, index) => {
          let _true = this.selected.find((x) => x.id_nv === item.id_nv);
          if (_true) {
            const _model = new UserInfoModel();
            _model.id_user = item.id_nv;
            _model.loai = 2;
            this.list_User.push(_model);
          }
        });
      }
      item.Users = this.list_User;
      item.start_date = controls["start_date"].value;
      item.urgent = controls["urgent"].value == "1" ? "true" : "false";
      item.required_result =
        controls["required_result"].value == "1" ? "true" : "false";
      item.description = controls["NoiDung"].value;
    }
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
  Update(_item: WorkDuplicateModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.viewLoading = true;
    this.disabledBtn = true;
    this._service.Duplicate(_item).subscribe((res) => {
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
            "GeneralKey.themthanhcong"
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
  Create(_item: WorkDuplicateModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.disabledBtn = true;
    this._service.Duplicate(_item).subscribe((res) => {
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
    this.dialogRef.close();
  }
  reset() {
    this.item = Object.assign({}, this.item);
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
      this.item = this.data._item;
      if (this.viewLoading == true) {
        this.onSubmit(true);
      } else {
        this.onSubmit(false);
      }
    }
  }
  text(e: any, vi: any) {
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
