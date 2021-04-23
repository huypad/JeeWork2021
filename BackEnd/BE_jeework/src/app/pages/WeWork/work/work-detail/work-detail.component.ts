import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';
import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import {
  Component,
  OnInit,
  ElementRef,
  ViewChild,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  OnChanges,
} from "@angular/core";
import { ActivatedRoute } from "@angular/router";
// Material
import { MatDialog } from "@angular/material/dialog";
import { SelectionModel } from "@angular/cdk/collections";
// RXJS
import { tap } from "rxjs/operators";
import { fromEvent, merge, BehaviorSubject, ReplaySubject } from "rxjs";
import { TranslateService } from "@ngx-translate/core";
// Services
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import {
  FormGroup,
  FormBuilder,
  Validators,
  FormControl,
} from "@angular/forms";
import { isFulfilled } from "q";
import { DatePipe } from "@angular/common";
import { WeWorkService } from "../../services/wework.services";
import { WorkService } from "../work.service";
import { WorkModel, UserInfoModel, UpdateWorkModel } from "../work.model";
import {
  UpdateByKeyModel,
  ChecklistModel,
  ChecklistItemModel,
} from "../../update-by-keys/update-by-keys.model";
import { UpdateByKeysComponent } from "../../update-by-keys/update-by-keys-edit/update-by-keys-edit.component";
import { UpdateByKeyService } from "../../update-by-keys/update-by-keys.service";
// import { CheckListEditComponent } from '../check-list-edit/check-list-edit.component';
import { AttachmentService } from "../../services/attachment.service";
import { FileUploadModel } from "../../discussions/Model/Topic.model";
import { PopoverContentComponent } from "ngx-smart-popover";
import { AttachmentModel } from "../../projects-team/Model/department-and-project.model";

@Component({
  selector: "kt-work-detail",
  templateUrl: "./work-detail.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class WorkDetailComponent implements OnInit, OnChanges {
  selectedItem: any = undefined;
  itemForm: FormGroup;
  loadingSubject = new BehaviorSubject<boolean>(false);
  loadingControl = new BehaviorSubject<boolean>(false);
  loading1$ = this.loadingSubject.asObservable();
  hasFormErrors: boolean = false;
  item: any = {};
  oldItem: WorkModel;
  item_User: UserInfoModel;
  item_file: UserInfoModel;
  // itemHD: HopDongModel;
  // oldItemHD: HopDongModel;
  selectedTab: number = 0;
  //========================================================
  Visible: boolean = false;
  viewLoading: boolean = false;
  loadingAfterSubmit: boolean = false;
  listUser: any[] = [];
  FormControls: FormGroup;
  disBtnSubmit: boolean = false;
  isChange: boolean = false;
  UserInfo: any = {};
  data: any;
  SubTask: string = "";
  deadline: any;
  listColumn: any[] = [];
  IsShow_MoTaCV: boolean = false;
  IsShow_CheckList: boolean = false;
  IsShow_Result: boolean = false;
  MoTaCongViec: string = "";
  checklist: string = "";
  Result: string = "";
  CheckList: any[] = [];
  Value: string = "";
  Key: string = "";
  description: string = "";
  checklist_item: string = "";
  Id_project_team: number = 0;
  public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public bankFilterCtrl: FormControl = new FormControl();
  listType: any[] = [];
  DataID: number = 0;
  Comment:any = "";
  options_assign: any = {};
  @ViewChild("Assign", { static: true })
  myPopover_Assign: PopoverContentComponent;
  selected_Assign: any[] = [];
  @ViewChild("hiddenText_Assign", { static: true }) text_Assign: ElementRef;
  _Assign: string = "";
  list_Assign: any[] = [];
  constructor(
    private _service: WorkService,
    private danhMucService: DanhMucChungService,
    public dialog: MatDialog,
    private itemFB: FormBuilder,
    public subheaderService: SubheaderService,
    private layoutUtilsService: LayoutUtilsService,
    private changeDetectorRefs: ChangeDetectorRef,
    private translate: TranslateService,
    public datepipe: DatePipe,
    private activatedRoute: ActivatedRoute,
    public weworkService: WeWorkService,
    private updatebykeyService: UpdateByKeyService,
    private _attservice: AttachmentService
  ) {}

  /** LOAD DATA */
  ngOnInit() {
    this.options_assign = this.getOptions_Assign();

    this.UserInfo = JSON.parse(localStorage.getItem("UserInfo"));
    // this.activatedRoute.params.subscribe(params => {
    // 	this.loadingSubject.next(false);
    // 	this.DataID = params['id'];
    // });
  }

  ngOnChanges() {
    this.DataID = this.data.DATA.id_row;
    this.Id_project_team = this.data.DATA.id_project_team;
    this.item = this.data.DATA;
    this.description = this.data.DATA.description;
    this.options_assign = this.getOptions_Assign();

    this._service.WorkDetail(this.DataID).subscribe((res) => {
      if (res && res.status == 1) {
        this.item = res.data;
        this.changeDetectorRefs.detectChanges();
      }
    });

    this.weworkService.lite_milestone(this.Id_project_team).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listType = res.data;
        this.changeDetectorRefs.detectChanges();
      }
    });
    const filter: any = {};
    filter.key = "id_project_team";
    // filter.value = this.item.id_project_team;
    filter.id_project_team = this.item.id_project_team;
    this.weworkService.list_account(filter).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.listUser = res.data;
        this.setUpDropSearchNhanVien();
        this.changeDetectorRefs.detectChanges();
      }
      this.options_assign = this.getOptions_Assign();
    });
    // }
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      "",
      "",
      0,
      50,
      true
    );
    this._service.CheckList(queryParams).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.CheckList = res.data;
        this.changeDetectorRefs.detectChanges();
      }
    });
    // this.weworkService.list_account({}).subscribe(res => {
    // 	this.changeDetectorRefs.detectChanges();
    // 	if (res && res.status === 1) {
    // 		this.listUser = res.data;
    // 	}
    // 	this.changeDetectorRefs.detectChanges();
    // });
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
      showSearch: true,
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
    this.options_assign = this.getOptions_Assign();
    if (this.options_assign.keyword) {
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

  filterConfiguration(): any {
    const filter: any = {};
    filter.id_work = this.item.id_row;
    return filter;
  }
  //type=1: comment, type=2: reply
  CommentInsert(e: any, Parent: number, ind: number, type: number) {
    // var objSave: any = {};
    // objSave.comment = e;
    // objSave.id_parent = Parent;
    // objSave.object_type = this.Loai;
    // objSave.object_id = this.Id;
    // if (type == 1) { objSave.Attachment = this.AttachFileComment; }
    // else { objSave.Attachments = this.ListAttachFile[ind]; }
    // this.service.getDSYKienInsert(objSave).subscribe(res => {
    // 	if (type == 1) { this.Comment = ''; this.AttachFileComment = []; }
    // 	else {
    // 		// var el = document.getElementById("CommentRep" + ind) as HTMLElement;
    // 		// el.setAttribute('value', '');
    // 		(<HTMLInputElement>document.getElementById("CommentRep" + ind)).value = "";
    // 		this.ListAttachFile[ind] = [];
    // 	}
    // 	this.changeDetectorRefs.detectChanges();
    // 	//this.getDSYKien();
    // });
  }
  formatLabel(value: number) {
    if (value >= 1000) {
      return Math.round(value / 1000) + "%";
    }
    return value;
  }
  Update_MotaCongViec() {
    this.IsShow_MoTaCV = !this.IsShow_MoTaCV;
    this.MoTaCongViec = this.description;
  }
  Update_CheckList() {
    this.IsShow_CheckList = !this.IsShow_CheckList;
    this.checklist = this.checklist;
  }
  Update_Result() {
    this.IsShow_Result = !this.IsShow_Result;
    this.Result = this.Result;
  }
  download(path: any) {
    window.open(path);
  }
  Assign(val: any) {
    var model = new UpdateWorkModel();
    model.id_row = this.item.id_row;
    model.key = "assign";
    model.value = val.id_nv;
    this.layoutUtilsService.showWaitingDiv();
    this._service.UpdateByKey(model).subscribe((res) => {
      this.layoutUtilsService.OffWaitingDiv();
      this.changeDetectorRefs.detectChanges();
      if (res && res.status == 1) {
        this.ngOnChanges();
        this.layoutUtilsService.showActionNotification(
          this.translate.instant("GeneralKey.capnhatthanhcong"),
          MessageType.Read,
          4000,
          true,
          false,
          3000,
          "top",
          1
        );
      } else
        this.layoutUtilsService.showActionNotification(
          res.error.message,
          MessageType.Read,
          999999999,
          true,
          false,
          3000,
          "top",
          0
        );
    });
  }
  Delete_Followers(val: any) {
    const _title = this.translate.instant("GeneralKey.xoa");
    const _description = this.translate.instant(
      "GeneralKey.bancochacchanmuonxoakhong"
    );
    const _waitDesciption = this.translate.instant(
      "GeneralKey.dulieudangduocxoa"
    );
    const _deleteMessage = this.translate.instant("GeneralKey.xoathanhcong");
    const dialogRef = this.layoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      }
      this._service.Delete_Followers(this.item.Id, val).subscribe((res) => {
        if (res && res.status === 1) {
          this.layoutUtilsService.showActionNotification(
            _deleteMessage,
            MessageType.Delete,
            4000,
            true,
            false
          );
        } else {
          this.layoutUtilsService.showActionNotification(
            res.error.message,
            MessageType.Read,
            999999999,
            true,
            false,
            3000,
            "top",
            0
          );
        }
        this.ngOnChanges();
      });
    });
  }
  setUpDropSearchNhanVien() {
    this.bankFilterCtrl.setValue("");
    this.filterBanks();
    this.bankFilterCtrl.valueChanges.pipe().subscribe(() => {
      this.filterBanks();
    });
  }
  Update_Status(val: any) {
    var model = new UpdateWorkModel();
    model.id_row = this.item.id_row;
    model.key = "status";
    model.value = val;
    this.layoutUtilsService.showWaitingDiv();
    this._service.UpdateByKey(model).subscribe((res) => {
      this.layoutUtilsService.OffWaitingDiv();
      this.changeDetectorRefs.detectChanges();
      if (res && res.status == 1) {
        this.item.status = val;
        this.ngOnChanges();
        this.layoutUtilsService.showActionNotification(
          this.translate.instant("GeneralKey.capnhatthanhcong"),
          MessageType.Read,
          4000,
          true,
          false,
          3000,
          "top",
          1
        );
      } else
        this.layoutUtilsService.showActionNotification(
          res.error.message,
          MessageType.Read,
          999999999,
          true,
          false,
          3000,
          "top",
          0
        );
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

  initProduct() {
    this.createForm();
    this.loadingSubject.next(false);
    this.loadingControl.next(true);
  }
  createForm() {
    this.itemForm = this.itemFB.group({
      // maNV: [this.item.MaNV, [Validators.required]],
      // maChamCong: [this.item.MaChamCong],
      // ho: [this.item.HoLot, [Validators.required]],
      // ten: [this.item.Ten, [Validators.required]],
      // tenThuongGoi: [this.item.TenThuongGoi],
      // gioiTinh: [this.item.GioiTinh, [Validators.required]],
      // ngaySinh: [this.item.NgaySinh, [Validators.pattern(/^((((0[1-9])|([1-2][0-9])|(3[0])|([1-9]))(\/)(4|04|6|06|9|09|11)(\/)([1-2]\d{3})))|((((0[1-9])|([1-2][0-9])|(3[0-1])|([1-9]))(\/)(1|3|5|7|8|10|12|01|03|05|07|08)(\/)([1-2]\d{3})))|((((0[1-9])|([1-2][0-9])|([1-9]))(\/)(2|02)(\/)([1-2]\d{3})))$|^[0-9]{4}$/), Validators.required]],
      // noiSinh: [this.item.ID_NoiSinh, [Validators.required]],
      // nguyenQuan: [this.item.NguyenQuan],
      // cmnd: [this.item.CMND, [Validators.required, Validators.pattern(/^[0-9]{9}$|^[0-9]{12}$/)]],
      // ngayCapCMND: [this.f_convertDate(this.item.NgayCapCMND), [Validators.required]],
      // noiCapCMND: [this.item.ID_NoiCapCMND, [Validators.required]],
      // soHoChieu: [this.item.SoHoChieu],
      // ngayCapHoChieu: [this.f_convertDate(this.item.NgayCapSoHoChieu)],
      // ngayHetHan: [this.f_convertDate(this.item.NgayHetHan)],
      // danToc: [this.item.ID_DanToc, [Validators.required]],
      // tonGiao: [this.item.ID_TonGiao, [Validators.required]],
      // noiCapHoChieu: [this.item.NoiCapHoChieu],
      // diDong: [this.item.DiDong, [Validators.pattern(/^[0-9]{10}$|^[0-9]{11}$/)]],
      // emailCongTy: [this.item.EmailCongTy, [Validators.pattern(/^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/)]],
      // emailCaNhan: [this.item.EmailCaNhan, [Validators.pattern(/^(([^<>()\[\]\\.,;:\s@"]+(\.[^<>()\[\]\\.,;:\s@"]+)*)|(".+"))@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\])|(([a-zA-Z\-0-9]+\.)+[a-zA-Z]{2,}))$/)]],
      // Province: ['' + this.item.ID_Phuong, [Validators.required]],
      // diaChiThuongtru: [this.item.DiaChiThuongTru, [Validators.required]],
      // quocTich: [this.item.QuocTich],
      // diaChiTamTru: [this.item.DiaChiTamTru],
      // tinhTrangHonNhan: [this.item.ID_TinhTrangHonNhan, [Validators.required]],
      // mst: [this.item.MaSoThue],
      // ngayCapMST: [this.f_convertDate(this.item.NgayCapMST)],
      // chuTK: [this.item.ChuTaiKhoan],
      // soTK: [this.item.SoTaiKhoan],
      // nganHang: [this.item.ID_NganHang],
      // nguoiLienHe: [this.item.NguoiLienHe],
      // quanHeNguoiLienHe: [this.item.QuanHeNguoiLienHe],
      // soDienThoaiNguoiLienHe: [this.item.SoDienThoaiNguoiLienHe, [Validators.pattern(/^[0-9]{10}$|^[0-9]{11}$/)]],
    });
  }
  reset() {
    // this.item = Object.assign({}, this.oldItem);
    // this.itemHD = Object.assign({}, this.oldItemHD);
    this.createForm();
    this.hasFormErrors = false;
    this.itemForm.markAsPristine();
    this.itemForm.markAsUntouched();
  }
  onAlertClose($event) {
    this.hasFormErrors = false;
  }
  //---------------------------------------------------------

  f_number(value: any) {
    return Number((value + "").replace(/,/g, ""));
  }

  f_currency(value: any, args?: any): any {
    let nbr = Number((value + "").replace(/,|-/g, ""));
    return (nbr + "").replace(/(\d)(?=(\d{3})+(?!\d))/g, "$1,");
  }
  textPres(e: any, vi: any) {
    if (
      isNaN(e.key) &&
      //&& e.keyCode != 8 // backspace
      //&& e.keyCode != 46 // delete
      e.keyCode != 32 && // space
      e.keyCode != 189 &&
      e.keyCode != 45
    ) {
      // -
      e.preventDefault();
    }
  }
  checkDate(e: any, vi: any) {
    if (
      !(
        (e.keyCode > 95 && e.keyCode < 106) ||
        (e.keyCode > 46 && e.keyCode < 58) ||
        e.keyCode == 8
      )
    ) {
      e.preventDefault();
    }
  }
  checkValue(e: any) {
    if (
      !(
        (e.keyCode > 95 && e.keyCode < 106) ||
        (e.keyCode > 47 && e.keyCode < 58) ||
        e.keyCode == 8
      )
    ) {
      e.preventDefault();
    }
  }
  f_convertDate(v: any) {
    if (v != "" && v != null) {
      let a = new Date(v);
      return (
        a.getFullYear() +
        "-" +
        ("0" + (a.getMonth() + 1)).slice(-2) +
        "-" +
        ("0" + a.getDate()).slice(-2) +
        "T00:00:00.0000000"
      );
    }
  }

  f_date(value: any): any {
    if (value != "" && value != null && value != undefined) {
      let latest_date = this.datepipe.transform(value, "dd/MM/yyyy");
      return latest_date;
    }
    return "";
  }
  refreshData() {
    this._service.WorkDetail(this.item.id_row).subscribe((res) => {
      if (res && res.status == 1) {
        this.ngOnInit();
        this.changeDetectorRefs.detectChanges();
      }
    });
  }
  ThemCot() {
    let item = {
      RowID: 0,
      FieldName: "",
      Title: "",
      KeyValue: "",
      TypeID: "",
      Formula: "",
      ShowFomula: false,
      Priority: -1,
      IsEdit: true,
      Width: "",
      FormatID: "1",
      IsColumnHide: false,
    };
    this.listColumn.splice(0, 0, item);
    this.changeDetectorRefs.detectChanges();
  }
  Get_ValueByKey(key: string): string {
    switch (key) {
      case "16":
        return (this.Value = this.MoTaCongViec);
      case "0":
        return (this.Value = this.checklist);
    }
    return "";
  }
  UpdateByKey(id_log_action: string, key: string, IsQuick: boolean) {
    this.Get_ValueByKey(id_log_action);
    let model = new UpdateByKeyModel();
    if (IsQuick) {
      model.id_row = this.item.id_row;
      model.id_log_action = id_log_action;
      model.value = this.Value;
      model.key = key;
      if (id_log_action == "0") {
        let checklist_model = new ChecklistModel();
        checklist_model.id_work = this.item.id_row;
        checklist_model.title = this.Value;

        this.updatebykeyService
          .Insert_CheckList(checklist_model)
          .subscribe((res) => {
            this.changeDetectorRefs.detectChanges();
            if (res && res.status === 1) {
              this.IsShow_CheckList = !this.IsShow_CheckList;
              this.refreshData();
              this.changeDetectorRefs.detectChanges();
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
      } else {
        this.updatebykeyService.UpdateByKey(model).subscribe((res) => {
          this.changeDetectorRefs.detectChanges();
          if (res && res.status === 1) {
            this.IsShow_MoTaCV = !this.IsShow_MoTaCV;
            this._service.WorkDetail(model.id_row).subscribe((res) => {
              if (res && res.status == 1) {
                this.description = res.data.description;
                this.refreshData();
                // this.checklist = res.data.description;
                this.ngOnInit();
                this.changeDetectorRefs.detectChanges();
              }
            });
            this.changeDetectorRefs.detectChanges();
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
    } else {
      model.clear(); // Set all defaults fields
      this.UpdateKey(model);
    }
  }

  UpdateKey(_item: UpdateByKeyModel) {
    let saveMessageTranslateParam = "";
    saveMessageTranslateParam +=
      _item.id_row > 0
        ? "GeneralKey.capnhatthanhcong"
        : "GeneralKey.themthanhcong";
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(UpdateByKeysComponent, {
      data: { _item },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        this.refreshData();
        this.ngOnInit();
      } else {
        this.layoutUtilsService.showActionNotification(
          _saveMessage,
          _messageType,
          4000,
          true,
          false
        );
        this.ngOnInit();
      }
    });
    this.refreshData();
  }
  UpdateCheckList() {
    let model = new UpdateByKeyModel();
    model.clear(); // Set all defaults fields
    this.UpdateKey(model);
  }
  _UpdateCheckList(_item: ChecklistModel) {
    // let saveMessageTranslateParam = '';
    // saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
    // const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    // const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    // const dialogRef = this.dialog.open(CheckListEditComponent, { data: { _item, IsCheckList: true } });
    // dialogRef.afterClosed().subscribe(res => {
    // 	if (!res) {
    // 		this.refreshData();
    // 		this.ngOnInit();
    // 		this.changeDetectorRefs.detectChanges();
    // 		return;
    // 	}
    // 	else {
    // 		this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
    // 	}
    // });
    // this.refreshData();
  }
  UpdateCheckList_Item(_item: ChecklistModel) {
    // let saveMessageTranslateParam = '';
    // saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
    // const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    // const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    // const dialogRef = this.dialog.open(CheckListEditComponent, { data: { _item, IsCheckList: false } });
    // dialogRef.afterClosed().subscribe(res => {
    // 	if (!res) {
    // 		this.refreshData();
    // 		return;
    // 	}
    // 	else {
    // 		this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
    // 	}
    // });
  }
  InsertCheckListItem(id_checkList: number) {
    let model = new ChecklistItemModel();
    model.id_checklist = id_checkList;
    model.title = this.checklist_item;

    this.updatebykeyService.Insert_CheckList_Item(model).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this._service.WorkDetail(model.id_row).subscribe((res) => {
          if (res && res.status == 1) {
            this.refreshData();
            this.ngOnInit();
            this.changeDetectorRefs.detectChanges();
          }
        });
        this.checklist_item = "";
        this.changeDetectorRefs.detectChanges();
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
  Delete_CheckList(_item: ChecklistModel) {
    const _title = this.translate.instant("GeneralKey.xoa");
    const _description = this.translate.instant(
      "GeneralKey.bancochacchanmuonxoakhong"
    );
    const _waitDesciption = this.translate.instant(
      "GeneralKey.dulieudangduocxoa"
    );
    const _deleteMessage = this.translate.instant("GeneralKey.xoathanhcong");
    const dialogRef = this.layoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        this.refreshData();
        return;
      }

      this._service.Delete_CheckList(_item.id_row).subscribe((res) => {
        if (res && res.status === 1) {
          this.layoutUtilsService.showActionNotification(
            _deleteMessage,
            MessageType.Delete,
            4000,
            true,
            false,
            3000,
            "top",
            1
          );
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
        this.ngOnInit();
      });
    });
    this.refreshData();
  }
  DeleteItem(_item: ChecklistItemModel) {
    const _title = this.translate.instant("GeneralKey.xoa");
    const _description = this.translate.instant(
      "GeneralKey.bancochacchanmuonxoakhong"
    );
    const _waitDesciption = this.translate.instant(
      "GeneralKey.dulieudangduocxoa"
    );
    const _deleteMessage = this.translate.instant("GeneralKey.xoathanhcong");
    const dialogRef = this.layoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        this.refreshData();
        return;
      }
      this._service.DeleteItem(_item.id_row).subscribe((res) => {
        if (res && res.status === 1) {
          this.layoutUtilsService.showActionNotification(
            _deleteMessage,
            MessageType.Delete,
            4000,
            true,
            false,
            3000,
            "top",
            1
          );
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
        this.ngOnInit();
      });
    });
    this.refreshData();
  }

  TenFile: string = "";
  File: string = "";
  filemodel: any;
  @ViewChild("csvInput", { static: true }) myInputVariable: ElementRef;
  @ViewChild("resultInput", { static: true }) result: ElementRef;

  save_file_Direct(evt: any, type: string) {
    if (evt.target.files && evt.target.files.length) {
      //Nếu có file
      var size = evt.target.files[0].size;
      if (size / 1024 / 1024 > 3) {
        this.layoutUtilsService.showActionNotification(
          "File upload không được vượt quá 3 MB",
          MessageType.Read,
          9999999999,
          true,
          false,
          3000,
          "top",
          0
        );
        return;
      }
      let file = evt.target.files[0]; // Ví dụ chỉ lấy file đầu tiên
      this.TenFile = file.name;
      let reader = new FileReader();
      reader.readAsDataURL(evt.target.files[0]);
      let base64Str;
      setTimeout(() => {
        base64Str = reader.result as String;
        var metaIdx = base64Str.indexOf(";base64,");
        base64Str = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64
        this.File = base64Str;
        var _model = new AttachmentModel();
        _model.object_type = parseInt(type);
        _model.object_id = this.item.id_row;
        const ct = new FileUploadModel();
        ct.strBase64 = this.File;
        ct.filename = this.TenFile;
        ct.IsAdd = true;
        // _model.item = ct;
        this.loadingAfterSubmit = true;
        this.viewLoading = true;
        this._attservice.Upload_attachment(_model).subscribe((res) => {
          this.changeDetectorRefs.detectChanges();
          if (res && res.status === 1) {
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
      }, 2000);
    } else {
      this.File = "";
    }
  }
  Delete_File(val: any) {
    const _title = this.translate.instant("GeneralKey.xoa");
    const _description = this.translate.instant(
      "GeneralKey.bancochacchanmuonxoakhong"
    );
    const _waitDesciption = this.translate.instant(
      "GeneralKey.dulieudangduocxoa"
    );
    const _deleteMessage = this.translate.instant("GeneralKey.xoathanhcong");
    const dialogRef = this.layoutUtilsService.deleteElement(
      _title,
      _description,
      _waitDesciption
    );
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      }
      this.layoutUtilsService.showWaitingDiv();
      this._attservice.delete_attachment(val).subscribe((res) => {
        this.layoutUtilsService.OffWaitingDiv();
        if (res && res.status === 1) {
          this.ngOnInit();
          this.changeDetectorRefs.detectChanges();
          this.layoutUtilsService.showActionNotification(
            _deleteMessage,
            MessageType.Delete,
            4000,
            true,
            false
          );
        } else {
          this.layoutUtilsService.showActionNotification(
            res.error.message,
            MessageType.Read,
            999999999,
            true,
            false,
            3000,
            "top",
            0
          );
        }
      });
    });
  }

  list_User: any[] = [];
  Insert_SubTask() {
    let model = new WorkModel();
    model.title = this.SubTask;
    model.deadline = this.deadline;
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
    model.Users = this.list_User;
    model.id_parent = this.item.id_row;
    model.id_project_team = this.item.id_project_team;
    this.changeDetectorRefs.detectChanges();
    this.layoutUtilsService.showWaitingDiv();
    this._service.InsertWork(model).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();
      this.layoutUtilsService.OffWaitingDiv();
      if (res && res.status === 1) {
        model;
        this.ngOnChanges();
        // this.changeDetectorRefs.detectChanges();
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

  stopPropagation(event) {
    event.stopPropagation();
  }

  ItemSelected(val: any) {
    this.data.DATA.assign = val;
    var model = new UpdateWorkModel();
    model.id_row = this.item.id_row;
    model.key = "assign";
    model.value = val.id_nv;
    this.layoutUtilsService.showWaitingDiv();
    this._service.UpdateByKey(model).subscribe((res) => {
      this.layoutUtilsService.OffWaitingDiv();
      this.changeDetectorRefs.detectChanges();
      if (res && res.status == 1) {
        this.ngOnChanges();
        this.layoutUtilsService.showActionNotification(
          this.translate.instant("GeneralKey.capnhatthanhcong"),
          MessageType.Read,
          4000,
          true,
          false,
          3000,
          "top",
          1
        );
      } else
        this.layoutUtilsService.showActionNotification(
          res.error.message,
          MessageType.Read,
          999999999,
          true,
          false,
          3000,
          "top",
          0
        );
    });
  }

  ReloadData(event) {
    this.item.id_milestone = event.id_row;
    this.item.milestone = event.title;
  }
}
