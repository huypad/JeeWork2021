import { tinyMCE } from "src/app/_metronic/jeework_old/components/tinyMCE";
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import {
  Component,
  OnInit,
  ViewChild,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Inject,
  Input,
  OnChanges,
} from "@angular/core";
import { Router } from "@angular/router";
// Material
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
// RXJS
import { ReplaySubject, BehaviorSubject } from "rxjs";
import { TranslateService } from "@ngx-translate/core";
// Services
// Models
import {
  FormBuilder,
  FormControl,
  FormGroup,
  Validators,
} from "@angular/forms";
import { JeeWorkLiteService } from "../../services/wework.services";
import { RepeatedModel, RepeatedTaskModel, UserInfoModel } from "../work.model";
import { WorkService } from "../work.service";
import { PopoverContentComponent } from "ngx-smart-popover";

@Component({
  selector: "kt-repeated-edit",
  templateUrl: "./repeated-edit.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class RepeatedEditComponent implements OnInit, OnChanges {
  item: RepeatedModel;
  itemForm: FormGroup;
  hasFormErrors: boolean = false;
  viewLoading: boolean = false;
  loadingAfterSubmit: boolean = false;
  disabledBtn: boolean = false;
  showtask = false;
  _Follower: string = "";
  _Assign: string = "";
  listGroup: any[] = [];
  tinyMCE: any = {};
  listUser: any[] = [];
  listProject: any[] = [];
  list_weekdays: any[] = [];
  show_frequency: boolean = false;
  txt_repeat_day = "";
  User: any[] = [];
  public filteredUsers: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public userFilterCtrl: FormControl = new FormControl();
  public filtereproject: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public projectFilterCtrl: FormControl = new FormControl();
  showOption: boolean = false;
  @Input() options: any = {
    showSearch: true, //hiển thị search input hoặc truyền keyword
    keyword: "",
    data: [],
  };
  id_project = 0;
  list_follower: any[] = [];
  check_weekdays: any[] = [];
  list_User: any[] = [];
  id_project_team: number = 0;
  isUpdate = false;
  public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public bankFilterCtrl: FormControl = new FormControl();
  isUpdatesubtask: boolean = false;
  isUpdatetodolist: boolean = false;
  listColumn: any[] = [];
  showColumn: boolean = true;
  public filter_member: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public filter_ctrl: FormControl = new FormControl();
  dayofmonth: any = [];
  listCol_Todo: any[] = [];
  showCol_Todo: boolean = true;
  public filter_member_todo: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public filter_ctrl_todo: FormControl = new FormControl();
  Is_duplicate: boolean = false;
  listInsert: any[] = [];
  selectedUser: any = [];
  constructor(
    public dialogRef: MatDialogRef<RepeatedEditComponent>,
    private fb: FormBuilder,
    @Inject(MAT_DIALOG_DATA) public _data: any,
    private changeDetectorRefs: ChangeDetectorRef,
    public weworkService: JeeWorkLiteService,
    private _service: WorkService,
    private layoutUtilsService: LayoutUtilsService,
    private translate: TranslateService,
    private router: Router
  ) {
    this.list_weekdays = [
      {
        Code: "T2",
        Title: this.translate.instant("day.thu2"),
      },
      {
        Code: "T3",
        Title: this.translate.instant("day.thu3"),
      },
      {
        Code: "T4",
        Title: this.translate.instant("day.thu4"),
      },
      {
        Code: "T5",
        Title: this.translate.instant("day.thu5"),
      },
      {
        Code: "T6",
        Title: this.translate.instant("day.thu6"),
      },
      {
        Code: "T7",
        Title: this.translate.instant("day.thu7"),
      },
      {
        Code: "CN",
        Title: this.translate.instant("day.chunhat"),
      },
    ];
    let x = (this.router.url).split('/');
    if (+x[2] > 0) {
      this.id_project = + x[2];
    } else {
    }
  }
  /** LOAD DATA */
  ngOnInit() {
    for (let i = 1; i < 32; i++) {
      this.dayofmonth.push(i.toString());
    }
    this.item = new RepeatedModel();
    this.item.clear();
    this.item = this._data._item;
    this.id_project_team = +this.item.id_project_team;
    if (this.id_project_team > 0) {
      this.isUpdate = true;
      this.LoadUser(this.id_project_team);
    } else if (this.id_project > 0) {
      this.id_project_team = this.id_project;
      this.LoadUser(this.id_project_team);
    }
    this.txt_repeat_day = this.item.repeated_day;
    this.Is_duplicate = this.item.IsCopy;
    this.createForm();
    var newArr = this.item.repeated_day.split(",");
    this.itemForm.controls.repeated_day.setValue(newArr);
    if (this.item.id_row > 0) {
      this._service
        .Detail_repeated(this._data._item.id_row)
        .subscribe((res) => {
          if (res && res.status == 1) {
            this.item = res.data;
            // this.itemForm.controls["id_group"].setValue('' + this.item.id_group);
            this.User = this.item.Users;
            // this.selected = this.User;
            for (let i = 0; i < this.User.length; i++) {
              this._Follower += " @" + this.User[i]["username"];
            }
            this.selectedUser = this.item.Users;
            this._Follower = this._Follower.substring(1);
            this.changeDetectorRefs.detectChanges();
          }
        });

      for (var i = 0; i < this.list_weekdays.length; i++) {
        var bool = false;
        for (
          var j = 0;
          j < this.item.repeated_day.substring(0).split(",").length;
          j++
        ) {
          if (
            this.list_weekdays[i].Code ==
            this.item.repeated_day.substring(0).split(",")[j]
          ) {
            bool = true;
          }
        }
        this.check_weekdays.push({
          Code: this.list_weekdays[i],
          Title: this.list_weekdays[i].Title,
          Checked: bool,
        });
      }

      if (this.item.Tasks) {
        this.listColumn = this.item.Tasks.filter((x) => x.IsTodo == false);
        this.listCol_Todo = this.item.Tasks.filter((x) => x.IsTodo == true);
        this.changeDetectorRefs.detectChanges();
      }
    } else {
      for (var i = 0; i < this.list_weekdays.length; i++) {
        var bool = false;
        this.check_weekdays.push({
          Code: this.list_weekdays[i],
          Title: this.list_weekdays[i].Title,
          Checked: bool,
        });
      }
    }
    this.BinddingData();
    this.userFilterCtrl.valueChanges.pipe().subscribe(() => {
      this.filterUsers();
    });
    this.tinyMCE = Object.assign({}, tinyMCE);
    this.tinyMCE.height = 100;

    if (this.item.IsCopy) {
      var newtext = "" + this.item.title + " (Copy)";
      this.itemForm.controls["title"].setValue(newtext);
    }

    if (this.item["UpdateSubtask"] !== "") {
      this.showtask = true;
      if (this.item["UpdateSubtask"] == "subtask") {
        this.isUpdatesubtask = true;
      } else {
        this.isUpdatetodolist = true;
      }
    }

    this.LoadTask();
  }
  LoadTask() {
    if (this.listColumn.length > 0) {
      var list = [];
      var index = 0;

      for (let i of this.listColumn) {
        list.push({
          RowID: i.id_row,
          SubTask: i.Title,
          id_nv: i.UserID.id_nv,
          Deadline: i.Deadline,
        });
        index++;
      }
      this.listColumn = list;
    }
    if (this.listCol_Todo.length > 0) {
      var list = [];
      var index = 0;
      for (let i of this.listCol_Todo) {
        list.push({
          RowID: i.id_row,
          todo: i.Title,
          id_nv: i.UserID.id_nv,
        });
        index++;
      }
      this.listCol_Todo = list;
    }
    let item = {
      RowID: 0,
      SubTask: "",
      id_nv: "",
      Deadline: "",
    };
    let item_todo = {
      RowID: 0,
      todo: "",
      id_nv: "",
    };
    this.listColumn.splice(this.listColumn.length, 0, item);
    this.listCol_Todo.splice(this.listCol_Todo.length, 0, item_todo);
    // this.updateChanges();
    // this.updateChanges_Todo();
  }
  BinddingData() {
    this.weworkService.lite_project_team_byuser("").subscribe((res) => {
      this.disabledBtn = false;
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listProject = res.data;
        this.setUpDropSearchProject();
        this.changeDetectorRefs.detectChanges();
      }
    });
    this.weworkService.lite_workgroup(this.id_project_team).subscribe((res) => {
      if (res && res.status === 1) {
        this.listGroup = res.data;
        this.changeDetectorRefs.detectChanges();
      }
    });
    this.Change_frequency(this._data._item.frequency);
  }
  LoadUser(id_projectteam) {
    const filter: any = {};
    filter.id_project_team = id_projectteam;
    this.weworkService.list_account(filter).subscribe((res) => {
      this.disabledBtn = false;
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listUser = res.data;
        this.setUpDropSearchNhanVien();
        this.setUpDropSearch_Member();
        this.setUpDrop_Member_Todo();
        if (this.options.excludes && this.options.excludes.length > 0) {
          var arr = this.options.excludes;
          this.listUser = this.listUser.filter((x) => !arr.includes(x.id_nv));
        }
        this.filterUsers();
        this.changeDetectorRefs.detectChanges();
        this.options = this.getOptions();
      }
    });
  }
  createForm() {
    this.itemForm = this.fb.group({
      title: [
        this.item.title == null ? "" : this.item.title,
        Validators.required,
      ],
      description: [this.item.description == null ? "" : this.item.description],
      id_project_team: [
        this.id_project_team == null ? "" : "" + this.id_project_team,
        Validators.required,
      ],
      frequency: [
        this.item.frequency == null ? "" : "" + this.item.frequency,
        Validators.required,
      ],
      // id_group: [this.item.id_group == null ? '' : '' + this.item.id_group, Validators.required],
      repeated_day: [
        this.item.repeated_day == null ? "" : "" + this.item.repeated_day,
        Validators.required,
      ],
      start_date: [
        this.item.start_date == null
          ? ""
          : this.f_convertDateTime(this.item.start_date),
        Validators.required,
      ],
      end_date: [
        this.item.end_date == null
          ? ""
          : this.f_convertDateTime(this.item.end_date),
        Validators.required,
      ],
      // assign: [this.item.Assign == null ? '' : '' + this.item.Assign.id_nv, Validators.required],
      Locked: [this.item.Locked == null ? "" : this.item.Locked],
      deadline: [
        this.item.deadline == null ? "" : this.item.deadline,
      ],
    });
    // this.itemForm.controls["id_project_team"].markAsTouched();
    // this.itemForm.controls["id_group"].markAsTouched();
    // this.itemForm.controls["frequency"].markAsTouched();
    // this.itemForm.controls["title"].markAsTouched();
    // this.itemForm.controls["repeated_day"].markAsTouched();
    // this.itemForm.controls["start_date"].markAsTouched();
    // this.itemForm.controls["assign"].markAsTouched();
    // this.itemForm.controls["end_date"].markAsTouched();
    // this.itemForm.controls["deadline"].markAsTouched();
  }
  setUpDropSearchNhanVien() {
    this.bankFilterCtrl.setValue("");
    this.filterBanks();
    this.bankFilterCtrl.valueChanges.pipe().subscribe(() => {
      this.filterBanks();
    });
  }
  protected filterBanks() {
    if (!this.listUser) {
      return;
    }
    let search = this.bankFilterCtrl.value;
    if (!search) {
      this.filteredBanks.next(this.listUser.slice());
      return;
    } else {
      search = search.toLowerCase();
    }
    // filter the banks
    this.filteredBanks.next(
      this.listUser.filter(
        (bank) => bank.hoten.toLowerCase().indexOf(search) > -1
      )
    );
  }
  setUpDropSearch_Member() {
    this.filter_ctrl.setValue("");
    this.filterMember();
    this.filter_ctrl.valueChanges.pipe().subscribe(() => {
      this.filterMember();
    });
  }
  protected filterMember() {
    if (!this.listUser) {
      return;
    }
    let search = this.filter_ctrl.value;
    if (!search) {
      this.filter_member.next(this.listUser.slice());
      return;
    } else {
      search = search.toLowerCase();
    }
    // filter the banks
    this.filter_member.next(
      this.listUser.filter(
        (bank) => bank.hoten.toLowerCase().indexOf(search) > -1
      )
    );
  }
  setUpDrop_Member_Todo() {
    this.filter_ctrl_todo.setValue("");
    this.filterMember_Todo();
    this.filter_ctrl_todo.valueChanges.pipe().subscribe(() => {
      this.filterMember_Todo();
    });
  }
  protected filterMember_Todo() {
    if (!this.listUser) {
      return;
    }
    let search = this.filter_ctrl_todo.value;
    if (!search) {
      this.filter_member_todo.next(this.listUser.slice());
      return;
    } else {
      search = search.toLowerCase();
    }
    // filter the banks
    this.filter_member_todo.next(
      this.listUser.filter(
        (bank) => bank.hoten.toLowerCase().indexOf(search) > -1
      )
    );
  }
  Change_frequency(val: any) {
    if (val > 1) {
      this.show_frequency = true;
    } else {
      this.show_frequency = false;
    }
  }
  ShowOption() {
    this.showOption = true;
  }
  getTitle(): string {
    let result = this.translate.instant("GeneralKey.themmoi");
    if (!this.item || !this.item.id_row) {
      return result;
    }
    result = this.translate.instant("GeneralKey.chinhsua");
    return result;
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
      keyword: this.getKeyword(),
      data: this.listUser,
    };
    return options;
  }

  // end _Assign
  f_convertDateTime(date: string) {
    var componentsDateTime = date.split("/");
    var date = componentsDateTime[0];
    var month = componentsDateTime[1];
    var year = componentsDateTime[2];
    var formatConvert = year + "-" + month + "-" + date + "T00:00:00.0000000";
    return new Date(formatConvert);
  }
  f_convertDate(p_Val: any) {
    let a = p_Val === "" ? new Date() : new Date(p_Val);
    return (
      a.getFullYear() +
      "/" +
      ("0" + (a.getMonth() + 1)).slice(-2) +
      "/" +
      ("0" + a.getDate()).slice(-2)
    );
  }
  ItemSelectedUser(data) {
    var index = this.selectedUser.findIndex((x) => x.id_nv == data.id_nv);

    if (index >= 0) {
      this.selectedUser.splice(index, 1);
    } else {
      // this.selectedUser[0] = data;
      this.selectedUser.push(data);
    }
  }

  ngOnChanges() {
    this.userFilterCtrl.setValue("");
    this.listUser = [];
    if (this.options.showSearch == undefined) this.options.showSearch = true;
    if (this.options.data) {
      this.listUser = this.options.data;
      this.filterUsers();
      this.changeDetectorRefs.detectChanges();
    } else {
      this.weworkService.list_account(this.options.filter).subscribe((res) => {
        if (res && res.status === 1) {
          this.listUser = res.data;
          // mảng idnv exclude
          if (this.options.excludes && this.options.excludes.length > 0) {
            var arr = this.options.excludes;
            this.listUser = this.listUser.filter((x) => !arr.includes(x.id_nv));
          }
          this.filterUsers();
          this.changeDetectorRefs.detectChanges();
        }
      });
    }
    if (!this.options.showSearch) this.filterUsers();
  }
  protected filterUsers() {
    if (!this.listUser) {
      return;
    }
    let search = !this.options.showSearch
      ? this.options.keyword
      : this.userFilterCtrl.value;
    if (!search) {
      this.filteredUsers.next(this.listUser.slice());
      return;
    } else {
      search = search.toLowerCase();
    }
    // filter the banks
    if (search[0] == "@") {
      this.filteredUsers.next(
        this.listUser.filter(
          (bank) => ("@" + bank.username.toLowerCase()).indexOf(search) > -1
        )
      );
    } else {
      this.filteredUsers.next(
        this.listUser.filter(
          (bank) => bank.hoten.toLowerCase().indexOf(search) > -1
        )
      );
    }
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
      this.listProject.filter(
        (bank) => bank.title.toLowerCase().indexOf(search) > -1
      )
    );
  }
  BindList(id_project: any) {
    this.weworkService.lite_workgroup(id_project).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();

      if (res && res.status === 1) {
        this.listGroup = res.data;
        this.changeDetectorRefs.detectChanges();
      }
    });
  }
  Checked(id: string, check: any) {
    for (var i = 0; i < this.check_weekdays.length; i++) {
      if (id == this.check_weekdays[i].Code) {
        this.check_weekdays[i].Checked = check;
      }
    }
  }
  GetListCheck(): any {
    var chuoicheck = "";
    for (var i = 0; i < this.check_weekdays.length; i++) {
      if (this.check_weekdays[i].Checked) {
        chuoicheck += "," + this.list_weekdays[i].Code;
      }
    }
    if (chuoicheck.length > 0) chuoicheck = chuoicheck.substring(1);
    return chuoicheck;
  }
  close() {
    this.dialogRef.close();
  }
  onSubmit(withBack: boolean = false) {
    this.hasFormErrors = false;
    this.loadingAfterSubmit = false;
    // if (this.itemForm.controls.id_group.value == "") {
    // 	this.itemForm.controls.id_group.setValue("NULL");
    // }
    if (!this.show_frequency) {
      this.itemForm.controls.repeated_day.setValue(this.GetListCheck());
    }
    // else {
    // 	this.itemForm.controls.repeated_day.setValue(this.txt_repeat_day);
    // }
    const controls = this.itemForm.controls;
    /* check form */
    if (this.itemForm.invalid) {
      Object.keys(controls).forEach((controlName) =>
        controls[controlName].markAsTouched()
      );
      this.hasFormErrors = true;
      this.layoutUtilsService.showError("Vui lòng nhập đầy đủ thông tin bắt buộc");
      // this.layoutUtilsService.showActionNotification("Error Valid")
      return;
    }
    const updatedegree = this.prepare();
    if (updatedegree) {
      if (updatedegree.id_row > 0 && !this.Is_duplicate) {
        this.Update(updatedegree, withBack);
      } else {
        this.Create(updatedegree, withBack);
      }
    }
  }
  prepare(): any {
    const controls = this.itemForm.controls;
    const _item = new RepeatedModel();
    var isErro = "";
    this.listInsert = [];
    _item.id_row = this.item.id_row;
    _item.title = controls["title"].value;
    _item.description = controls["description"].value;
    _item.id_project_team = controls["id_project_team"].value;
    // _item.id_group = controls['id_group'].value == "null" ? "0" : controls['id_group'].value;
    // _item.assign = controls['assign'].value;// chỉ lưu id
    _item.frequency = controls["frequency"].value;
    _item.deadline = +controls["deadline"].value > 0 ? controls["deadline"].value : "0";

    if (!this.show_frequency)
      _item.repeated_day = controls["repeated_day"].value;
    else _item.repeated_day = this.txt_repeat_day;
    const LIST = Array<UserInfoModel>();
    if (this.selectedUser.length > 0) {
      this.selectedUser.map((item, index) => {
        let _true = this.User.find((x) => x.id_nv === item.id_nv);
        if (_true) {
          const ct = new UserInfoModel();
          ct.id_row = item.id_row;
          ct.id_user = item.id_nv;
          ct.loai = 1;
          LIST.push(ct);
        } else {
          const ct = new UserInfoModel();
          if (ct.id_row == undefined) ct.id_row = 0;
          ct.id_user = item.id_nv;
          ct.loai = 1;
          LIST.push(ct);
        }
      });
    }

    _item.Users = LIST;
    _item.start_date = this.f_convertDate(controls["start_date"].value);
    _item.end_date = this.f_convertDate(controls["end_date"].value);
    _item.Locked = controls["Locked"].value;

    if (this.listColumn.length > 0) {
      this.listColumn.map((item, index) => {
        if (item.SubTask == "" && !(item.id_nv > 0) && !(item.Deadline > 0)) {
        } else {
          if (item.SubTask == "") {
            isErro = "Tên công việc con không được để trống";
          } else {
            const _task = new RepeatedTaskModel();
            _task.id_row = item.RowID;
            _task.id_repeated = _item.id_row;
            _task.Title = item.SubTask;
            _task.IsTodo = false;
            _task.UserID = item.id_nv > 0 ? item.id_nv : 0;
            _task.Deadline = item.Deadline > 0 ? item.Deadline : 0;
            this.listInsert.push(_task);
          }
        }
      });
    }
    if (isErro != "") {
      this.layoutUtilsService.showError(isErro);
      return false;
    }
    if (this.listCol_Todo.length > 0) {
      this.listCol_Todo.map((item, index) => {
        if (item.todo != "") {
          const _todo = new RepeatedTaskModel();
          _todo.id_row = item.RowID;
          _todo.id_repeated = _item.id_row;
          _todo.Title = item.todo;
          _todo.IsTodo = true;
          _todo.UserID = item.id_nv > 0 ? item.id_nv : 0;
          this.listInsert.push(_todo);
        } else if (item.id_nv > 0) {
          isErro = "Tên công việc cụ thể không được để trống";
        }
      });
    }
    _item.Tasks = this.listInsert;
    if (isErro != "") {
      this.layoutUtilsService.showError(isErro);
      return false;
    }
    return _item;
  }
  Update(_item: RepeatedModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.viewLoading = true;
    this.disabledBtn = true;
    this._service.Update_RepeatedTask(_item).subscribe((res) => {
      this.disabledBtn = false;
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
          this.layoutUtilsService.showActionNotification(_messageType);
          // this.layoutUtilsService.showActionNotification(_messageType, MessageType.Update, 4000, true, false).afterDismissed().subscribe(tt => {
          // });
        }
      } else {
        this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Update, 9999999999, true, false, 3000, 'top', 0);
      }
      this.changeDetectorRefs.detectChanges();
    });
  }
  Create(_item: RepeatedModel, withBack: boolean) {
    this.loadingAfterSubmit = true;
    this.disabledBtn = true;
    this._service.Insert_RepeatedTask(_item).subscribe((res) => {
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
  updateChanges() {
    this.onChange(this.listColumn);
  }
  updateChanges_Todo() {
    this.onChange(this.listCol_Todo);
  }
  onChange: (_: any) => void = (_: any) => { };
  checkShow($event, arr) {
    if ($event.target.value == "" || arr[arr.length - 1].SubTask == "") return;
    let item = {
      RowID: 0,
      SubTask: "",
      id_nv: "",
      Deadline: "",
    };
    this.listColumn.splice(this.listColumn.length, 0, item);
    this.updateChanges();
    this.changeDetectorRefs.detectChanges();
  }
  checkShow_Todo($event, arr) {
    if ($event.target.value == "" || arr[arr.length - 1].todo == "") return;
    let item = {
      RowID: 0,
      todo: "",
      id_nv: "",
    };
    this.listCol_Todo.splice(this.listCol_Todo.length, 0, item);
    this.updateChanges_Todo();
    this.changeDetectorRefs.detectChanges();
  }

  checkDays(event) {
    this.txt_repeat_day = "";
    event.forEach((element) => {
      this.txt_repeat_day += element + ",";
    });
  }
  stopPropagation(event) {
    event.stopPropagation();
  }
}
