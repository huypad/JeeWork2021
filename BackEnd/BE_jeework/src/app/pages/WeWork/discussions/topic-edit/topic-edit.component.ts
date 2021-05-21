import { TokenStorage } from "./../../../../_metronic/jeework_old/core/auth/_services/token-storage.service";
import { DanhMucChungService } from "./../../../../_metronic/jeework_old/core/services/danhmuc.service";
import { tinyMCE } from "src/app/_metronic/jeework_old/components/tinyMCE";
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
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
import { DiscussionsService } from "../discussions.service";
import { TopicModel, TopicUserModel } from "../Model/Topic.model";
import { PopoverContentComponent } from "ngx-smart-popover";
import { UserInfoModel } from "../../work/work.model";

@Component({
  selector: "kt-topic-edit",
  templateUrl: "./topic-edit.component.html",
})
export class TopicEditComponent implements OnInit {
  item: TopicModel;
  oldItem: TopicModel;
  itemForm: FormGroup;
  hasFormErrors: boolean = false;
  viewLoading: boolean = false;
  loadingAfterSubmit: boolean = false;
  disabledBtn: boolean = false;
  IsEdit: boolean;
  IsProject: boolean;
  tinyMCE = {};
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
  listProject: any[] = [];
  filter: any = {};
  optionsModel: number[];
  autocompleteItems = ["item", "item2", "item3"];
  public touchUi = false;
  Model: any[] = ["@item"];
  checked: any[] = [];
  checkedFollower: any;
  listID_Admin: any[] = [];
  colorCtr: AbstractControl = new FormControl(null);
  tendapb: string = "";
  mota: string = "";
  editor_description: string = "";
  NoiDung: string;
  UserInfo: any = {};
  selectedUser: any = [];
  ItemData: any;
  public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public bankFilterCtrl: FormControl = new FormControl();
  public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public projectFilterCtrl: FormControl = new FormControl();
  _Follower: string = "";
  list_follower: any[] = [];
  DisableTeam = false;
  list_User: any[] = [];
  User: any[] = [];
  options: any = {};
  UserId = localStorage.getItem("idUser");
  constructor(
    public dialogRef: MatDialogRef<TopicEditComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private fb: FormBuilder,
    private changeDetectorRefs: ChangeDetectorRef,
    private _service: DiscussionsService,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    private tokenStorage: TokenStorage,
    private danhMucChungService: DanhMucChungService,
    public weworkService: WeWorkService,
    private router: Router
  ) {}
  /** LOAD DATA */
  ngOnInit() {
    this.title = this.translate.instant("GeneralKey.choncocautochuc") + "";
    this.item = this.data._item;
    this.tinyMCE = tinyMCE;
    if (+this.item.id_project_team > 0) {
      this.DisableTeam = true;
      this.changeproject(this.item.id_project_team);
    }
    const filter: any = {};
    this.weworkService.lite_project_team_byuser("").subscribe((res) => {
      this.disabledBtn = false;
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listProject = res.data;
        this.setUpDropSearchProject();
        this.changeDetectorRefs.detectChanges();
      }
    });
    this.User = this.item.Follower;
    this.selectedUser = this.User;

    if (this.item.id_row > 0) {
      this.viewLoading = true;
    } else {
      this.viewLoading = false;
    }
    this.createForm();
  }
  ItemSelectedUser(data) {
    var index = this.selectedUser.findIndex((x) => x.id_nv == data.id_nv);

    if (index >= 0) {
      this.selectedUser.splice(index, 1);
    } else {
      this.selectedUser.push(data);
    }
  }
  changeproject(event) {
    var id_project = event;
    const filter: any = {};
    filter.id_project_team = id_project;
    this.weworkService.list_account(filter).subscribe((res) => {
      this.disabledBtn = false;
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listUser = res.data;
        this.options = this.getOptions();
        this.changeDetectorRefs.detectChanges();
      }
    });
  }
  getKeyword() {
    let i = this._Follower.lastIndexOf("@");
    if (i >= 0) {
      let temp = this._Follower.slice(i);
      if (temp.includes(" ")) return "";
      return this._Follower.slice(i);
    }
    return "";
  }
  getOptions() {
    var options: any = {
      showSearch: true,
      keyword: "",
      data: this.listUser,
    };
    return options;
  }

  createForm() {
    this.itemForm = this.fb.group({
      title: ["" + this.item.title, Validators.required],
      id_project_team: ["" + this.item.id_project_team, Validators.required],
      email: ["" + this.item.email, Validators.required],
      NoiDung: ["" + this.item.description],
    });
  }
  getHeight(): any {
    let tmp_height = 0;
    tmp_height = window.innerHeight - 320 - this.tokenStorage.getHeightHeader();
    return tmp_height + "px";
  }
  /** UI */
  getTitle(): string {
    let result = this.translate.instant("GeneralKey.themmoi");
    if (!this.item || !this.item.id_row) {
      return result;
    }
    result = this.translate.instant("GeneralKey.chinhsua");
    return result;
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

  /** ACTIONS */
  prepare(): TopicModel {
    const controls = this.itemForm.controls;
    const _item = new TopicModel();
    _item.id_row = this.item.id_row;
    _item.id_project_team = controls["id_project_team"].value;
    _item.title = controls["title"].value;
    _item.description = controls["NoiDung"].value;
    _item.email = controls["email"].value;
    if (this.selectedUser.length > 0) {
      if (this.User.length > 0) {
        this.selectedUser.map((item, index) => {
          let _true = this.User.find((x) => x.id_nv === item.id_nv);
          if (_true) {
            const ct = new UserInfoModel();
            ct.id_row = item.id_row;
            ct.id_user = item.id_nv;
            this.list_User.push(ct);
          } else {
            const ct = new UserInfoModel();
            if (ct.id_row == undefined) ct.id_row = 0;
            ct.id_user = item.id_nv;
            this.list_User.push(ct);
          }
        });
      } else {
        this.selectedUser.map((item, index) => {
          let _true = this.listUser.find((x) => x.id_nv === item.id_nv);
          const ct = new UserInfoModel();
          if (item.id_row == undefined) item.id_row = 0;
          ct.id_user = item.id_nv;
          this.list_User.push(ct);
        });
      }
    }

    _item.Users = this.list_User;
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
    const updatedegree = this.prepare();
    if (updatedegree.Users.length < 1) {
      this.layoutUtilsService.showError(
        this.translate.instant("topic.addfollower")
      );
      return;
    }
    if (updatedegree.id_row > 0) {
      this.Update(updatedegree, withBack);
    } else {
      this.Create(updatedegree, withBack);
    }
  }

  Update(_item: TopicModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.viewLoading = true;
    this.disabledBtn = true;
    this._service.UpdateTopic(_item).subscribe((res) => {
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

  Create(_item: TopicModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.disabledBtn = true;
    this._service.InsertTopic(_item).subscribe((res) => {
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

  stopPropagation(event) {
	  event.stopPropagation();
  }
}
