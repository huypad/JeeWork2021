import { WorkGroupEditComponent } from "./../../../../work/work-group-edit/work-group-edit.component";
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { SubheaderService } from "./../../../../../../_metronic/partials/layout/subheader/_services/subheader.service";
import { TokenStorage } from "./../../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service";
import { QueryParamsModelNew } from "./../../../../../../_metronic/jeework_old/core/models/query-models/query-params.model";
import { MenuPhanQuyenServices } from "./../../../../../../_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service";
import { AttachmentService } from "./../../../../services/attachment.service";
import {
  AttachmentModel,
  FileUploadModel,
} from "./../../../Model/department-and-project.model";
import { AddNewFieldsComponent } from "./../../add-new-fields/add-new-fields.component";
import { StatusDynamicDialogComponent } from "./../../../../status-dynamic/status-dynamic-dialog/status-dynamic-dialog.component";
import { WorkService } from "./../../../../work/work.service";
import { DuplicateTaskNewComponent } from "./../../duplicate-task-new/duplicate-task-new.component";
import { WorkListNewDetailComponent } from "./../../work-list-new-detail/work-list-new-detail.component";
import { DialogSelectdayComponent } from "./../../../../report/dialog-selectday/dialog-selectday.component";
import {
  WorkModel,
  UpdateWorkModel,
  UserInfoModel,
  WorkDuplicateModel,
  WorkGroupModel,
} from "./../../../../work/work.model";
import { ColumnWorkModel, DrapDropItem } from "./../../drap-drop-item.model";
import {
  filter,
  tap,
  catchError,
  finalize,
  share,
  takeUntil,
  debounceTime,
  startWith,
} from "rxjs/operators";
import { element } from "protractor";
import { WeWorkService } from "./../../../../services/wework.services";
import { DatePipe, DOCUMENT } from "@angular/common";
import { TranslateService } from "@ngx-translate/core";
import { FormBuilder, FormControl } from "@angular/forms";
import { Router, ActivatedRoute } from "@angular/router";
import { ProjectsTeamService } from "./../../../Services/department-and-project.service";
import {
  CdkDropList,
  CdkDragDrop,
  moveItemInArray,
  transferArrayItem,
  CdkDragStart,
} from "@angular/cdk/drag-drop";
import {
  Component,
  OnInit,
  Input,
  ViewChild,
  ChangeDetectorRef,
  Inject,
  OnChanges,
  OnDestroy,
  SimpleChange,
  SimpleChanges,
} from "@angular/core";
import { MatTable } from "@angular/material/table";
import { MatDialog } from "@angular/material/dialog";
import { MatSort } from "@angular/material/sort";
import { cloneDeep, find, values } from "lodash";
import * as moment from "moment";
import { SelectionModel } from "@angular/cdk/collections";
import { workAddFollowersComponent } from "../../../../work/work-add-followers/work-add-followers.component";
// import { WorkEditDialogComponent } from "../../../work/work-edit-dialog/work-edit-dialog.component";
import { WorkAssignedComponent } from "../../../../work/work-assigned/work-assigned.component";
import { DuplicateWorkComponent } from "../../../../work/work-duplicate/work-duplicate.component";
import { OverlayContainer } from "@angular/cdk/overlay";
import { BehaviorSubject, of } from "rxjs";

@Component({
  selector: 'app-work-task-list',
  templateUrl: './work-task-list.component.html',
  styleUrls: ['./work-task-list.component.scss']
})
export class WorkTaskListComponent implements OnInit, OnChanges {
  @Input() ID_Project: number = 0;
  @Input() ValueStatus: any = {};
  @Input() filter: any = {};
  @Input() groupby: string = "";
  @Input() ListColumns: any = [];

  ListtopicObjectID$: BehaviorSubject<any> = new BehaviorSubject<any>([]);
  data: any = [];
  listFilter: any = [];
  ListTasks: any = [];
  ListTags: any = [];
  ListUsers: any = [];
  editmail = 0;
  isAssignforme = false;
  loadding = true;
  // col
  displayedColumnsCol: string[] = [];
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  previousIndex: number;
  ListAction: any = [];
  addNodeitem = 0;
  newtask = -1;
  options_assign: any = {};
  filter_subtask: any = [];
  list_milestone: any = [];
  Assign_me = -1;
  // view setting
  tasklocation = false;
  showsubtask = true;
  showclosedtask = true;
  showclosedsubtask = true;
  showtaskmutiple = true;
  showemptystatus = false;
  status_dynamic: any = [];
  list_priority: any[];
  UserID = 0;
  isEdittitle = -1;
  startDatelist: Date = new Date();
  selection = new SelectionModel<WorkModel>(true, []);
  list_role: any = [];
  ItemFinal = 0;
  ProjectTeam: any = {};
  listNewField: any = [];
  DataNewField: any = [];
  listType: any = [];
  textArea: string = "";
  searchCtrl: FormControl = new FormControl();
  private readonly componentName: string = "kt-task_";
  Emtytask = false;
  public column_sort: any = [];

  IsAdminGroup = false;
  constructor(
    @Inject(DOCUMENT) private document: Document, // multi level
    private _service: ProjectsTeamService,
    private WorkService: WorkService,
    private router: Router,
    public dialog: MatDialog,
    private route: ActivatedRoute,
    private itemFB: FormBuilder,
    public subheaderService: SubheaderService,
    private layoutUtilsService: LayoutUtilsService,
    private changeDetectorRefs: ChangeDetectorRef,
    private translate: TranslateService,
    public datepipe: DatePipe,
    private tokenStorage: TokenStorage,
    private WeWorkService: WeWorkService,
    private menuServices: MenuPhanQuyenServices,
    private overlayContainer: OverlayContainer,
    private _attservice: AttachmentService
  ) {
    this.taskinsert.clear();
    this.filter_subtask = this.listFilter_Subtask[0];
    this.list_priority = this.WeWorkService.list_priority;
    this.UserID = +localStorage.getItem("idUser");
  }

  ngOnInit() {

    // this.selection = new SelectionModel<WorkModel>(true, []);
    this.menuServices.GetRoleWeWork("" + this.UserID).subscribe((res) => {
      if (res && res.status == 1) {
        this.list_role = res.data.dataRole;
        this.IsAdminGroup = res.data.IsAdminGroup;
      }
      if (!this.CheckRoles(3)) {
        this.isAssignforme = true;
      }
    });
    // this.LoadData();
    this.GetField();
    this.mark_tag();
    this.LoadListAccount();
    this.LoadDetailProject();

    this.WeWorkService.lite_milestone(this.ID_Project).subscribe((res) => {
      this.changeDetectorRefs.detectChanges();
      if (res && res.status === 1) {
        this.list_milestone = res.data;
        this.changeDetectorRefs.detectChanges();
      }
    });

    this.WeWorkService.ListStatusDynamic(this.ID_Project).subscribe((res) => {
      if (res && res.status === 1) {
        this.status_dynamic = res.data;
        //load ItemFinal
        var x = this.status_dynamic.find((val) => val.IsFinal == true);
        if (x) {
          this.ItemFinal = x.id_row;
        } else {
        }
        // this.changeDetectorRefs.detectChanges();
      }
    });

    //Load data work group
    this.WeWorkService.lite_workgroup(this.ID_Project).pipe(
      tap(res => {
        if (res && res.status === 1) {
          this.listType = res.data;
          this.changeDetectorRefs.detectChanges();
        };
      })
    ).subscribe();
  }

  ngOnChanges(changes : SimpleChanges) {
    // this.LoadData();
    
    this.GetField();
    this.mark_tag();
    this.LoadListAccount();
    this.LoadDetailProject();
  }

  LoadDetailProject() {
    this._service.DeptDetail(this.ID_Project).subscribe((res) => {
      if (res && res.status == 1) {
        this.ProjectTeam = res.data;
      }
    });
  }

  CheckRoles(roleID: number) {
    if (this.IsAdminGroup) return true;
    if (this.list_role) {
      var x = this.list_role.find((x) => x.id_row == this.ID_Project);
      if (x) {
        if (x.admin == true) {
          return true;
        } else {
          if (roleID == 3 || roleID == 4) {
             if (x.isuyquyen && x.isuyquyen != "0") return true;
          }
          if (
            roleID == 7 ||
            roleID == 9 ||
            roleID == 11 ||
            roleID == 12 ||
            roleID == 13
          ) {
            if (x.Roles.find((r) => r.id_role == 15)) return false;
          }
          if (roleID == 10) {
            if (x.Roles.find((r) => r.id_role == 16)) return false;
          }
          if (roleID == 4 || roleID == 14) {
            if (x.Roles.find((r) => r.id_role == 17)) return false;
          }
          var r = x.Roles.find((r) => r.id_role == roleID);
          if (r) {
            return true;
          } else {
            return false;
          }
        }
      }
    }
    return false;
  }
  CheckRoleskeypermit(key) {
    if (this.IsAdminGroup) return true;
    if (this.list_role) {
      var x = this.list_role.find((x) => x.id_row == this.ID_Project);
      if (x) {
        if (x.admin == true) {
          return true;
        } else {
          if (key == "id_nv") {
             if (x.isuyquyen && x.isuyquyen != "0") return true;
          }
          if (
            key == "title" ||
            key == "description" ||
            key == "status" ||
            key == "checklist" ||
            key == "delete"
          ) {
            if (x.Roles.find((r) => r.id_role == 15)) return false;
          }
          if (key == "deadline") {
            if (x.Roles.find((r) => r.id_role == 16)) return false;
          }
          if (key == "id_nv" || key == "assign") {
            if (x.Roles.find((r) => r.id_role == 17)) return false;
          }
          var r = x.Roles.find((r) => r.keypermit == key);
          if (r) {
            return true;
          } else {
            return false;
          }
        }
      } else {
        return false;
      }
    }
    return false;
  }
  /** SELECTION */
  CheckedNode(check: any, arr_model: any) {
    let checked = this.selection.selected.find(
      (x) => x.id_row === arr_model.id_row
    );
    const index = this.selection.selected.indexOf(arr_model, 0);
    if (!checked && check.checked) this.selection.selected.push(arr_model);
    else this.selection.selected.splice(index, 1);
  }
  /** Selects all rows if they are not all selected; otherwise clear selection. */
  masterToggle() {}
  LoadData() {
    this.clearList();
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      "",
      "",
      0,
      50,
      true
    );
    this.data = [];
    this.layoutUtilsService.showWaitingDiv();
    //get option new field
    this.GetOptions_NewField();
    //get list new field
    this.WeWorkService.GetNewField().subscribe((res) => {
      if (res && res.status == 1) {
        this.listNewField = res.data;
      }
    });
    //get data work binding data
    this._service.GetDataWorkCU(queryParams).subscribe((res) => {
      this.layoutUtilsService.OffWaitingDiv();
      this.loadding = false;
      if (res && res.status === 1) {
        this.data = res.data;
        this.listFilter = this.data.Filter;
        this.ListColumns = this.data.TenCot;
        //xóa title khỏi cột
        var colDelete = ["title", "id_row", "id_project_team", "id_parent"];
        colDelete.forEach((element) => {
          var indextt = this.ListColumns.findIndex(
            (x) => x.fieldname == element
          );
          if (indextt >= 0) this.ListColumns.splice(indextt, 1);
        });

        this.ListColumns.sort((a, b) =>
          a.title > b.title ? 1 : b.title > a.title ? -1 : 0
        ); // xếp theo anphabet
        this.ListColumns.sort((a, b) =>
          a.id_project_team > b.id_project_team
            ? -1
            : b.id_project_team > a.id_project_team
            ? 1
            : 0
        ); // nào chọn xếp trước
        this.ListColumns.sort((a, b) =>
          a.isbatbuoc > b.isbatbuoc ? -1 : b.isbatbuoc > a.isbatbuoc ? 1 : 0
        ); // nào bắt buộc xếp trước
        this.ListTasks = this.data.datawork;

        this.prepareDragDrop(this.ListTasks);
        this.ListTags = this.data.Tag;
        this.ListUsers = this.data.User;
        this.DataNewField = this.data.DataWork_NewField;
        this.LoadListStatus();
        this.changeDetectorRefs.detectChanges();
      }
    }); 
  }

  Statusdefault() {
    var x = this.status_dynamic.find(
      (x) =>
        x.isdefault == true &&
        x.IsToDo == false &&
        x.IsFinal == false &&
        x.IsDeadline == false
    );
    if (x) {
      return x.id_row;
    }
    return 0;
  }

  GetDataNewField(id_work, id_field, isDropdown = false, getColor = false) {
    var x = this.DataNewField.find(
      (x) => x.WorkID == id_work && x.FieldID == id_field
    );
    if (x) {
      if (isDropdown) {
        var list = this.listNewfield.find(
          (element) => element.FieldID == id_field && element.RowID == x.Value
        );
        if (list) {
          if (getColor) {
            return list.Color;
          }
          return list.Value;
        }
        return "--";
      }
      return x.Value;
    }
    return "-";
    // if()
  }

  UpdateValueField(value, idWork, field) {
    this.editmail = 0;
    if (value == "" || value == null || value == undefined) {
      if (field.fieldname != "checkbox") return;
    }
    const _item = new UpdateWorkModel();
    _item.clear();
    _item.FieldID = field.id_row;
    _item.Value = value;
    _item.WorkID = idWork;
    _item.TypeID = field.TypeID;
    this._service.UpdateNewField(_item).subscribe((res) => {
      if (res && res.status == 1) {
        // this.LoadData();
      }
    });
  }

  LoadNhomCongViec(id){
    var x = this.listType.find(x=>x.id_row == id);
    if(x){
      return x.title;
    }
    return "Chưa phân loại"
  }

  UpdateValue() {}

  filterConfiguration(): any {
    const filter: any = this.filter;
    filter.id_project_team = this.ID_Project;
    return filter;
  }

  getColorStatus(val) {
    var index = this.status_dynamic.find((x) => x.id_row == val);
    if (index) {
      return index.color;
    } else return "gray";
  }

  listField: any = [];
  GetField() { 
    this.WeWorkService.GetListField(this.ID_Project,3,false).subscribe((res) => {
      if (res && res.status === 1) {
        this.listField = res.data;
        // this.changeDetectorRefs.detectChanges();
      }
    });
  }

  isItemFinal(id) {
    if (id == this.ItemFinal) {
      return true;
    }
    return false;
  }

  listStatus: any = [];
  LoadListStatus() {
    this.listFilter.forEach((val) => {
      val.data = [];
    });

    this.ListTasks.forEach((element) => {
      element.isExpanded =
        this.filter_subtask.value == "show" ||
        this.addNodeitem == element.id_row
          ? true
          : false;
      this.listFilter.forEach((val) => {  
        
          if ( this.CheckDataStatus(val,element) ) {
            val.data.push(element);
          } 
          else if ( this.CheckDataAssigne(val,element) ) {
            if ( element.User?.length == 1 || (this.UserNull(element.User) && val.id_row == "") || (element.User?.length > 1 && val.id_row == "0")) 
            {
              val.data.push(element);
            }
          } else if ( this.CheckDataWorkGroup(val,element) ) {
            val.data.push(element);
          }
      });
    });
    this.listStatus = this.listFilter;
  }

  CheckDataStatus(valuefilter,elementTask){
    if(this.groupby == "status" && valuefilter.id_row == +elementTask.status){
      if(this.isAssignforme){
        // kiểm tra có phải người được giao hay người tạo hay không
        if( this.isAssignForme(elementTask)  || elementTask.createdby == this.UserID ){
          return true;
        } 
      }else{
        return true;
      } 
    }
    return false;
  }
  isAssignForme(elementTask){
    if(this.FindUser(elementTask.User,this.UserID) || this.FindUser(elementTask.Follower, this.UserID) || this.FindUser(elementTask.UserSubtask, this.UserID) ){
      return true;
    } 
    return false;
  }
  CheckDataAssigne(valuefilter,elementTask){  
    if(this.groupby == "assignee"){
      if(this.isAssignforme){
        if( ( this.FindUser(elementTask.User,valuefilter.id_row) && this.isAssignForme(elementTask)) || (elementTask.createdby == this.UserID && this.UserNull(elementTask.User))  ){
            return true
          }
      }else{
        if( this.FindUser(elementTask.User,valuefilter.id_row)  ||
          ( this.UserNull(elementTask.User) && valuefilter.id_row == "") ||
          (elementTask.User?.length > 1 && valuefilter.id_row == "0") ){
            return true
          }
      }
    }
    return false;
  }
  FindUser(listUser,iduser){
    if(listUser){
      var x = listUser.find((x) => x.id_user == iduser);
      if(x) return true;
    }
    return false;
  }
  UserNull(listUser){
    if(listUser){
      if(listUser.length > 0) return false;
    }
    return true;
  }
  CheckDataWorkGroup(valuefilter,elementTask){ 
    /**
     * this.groupby == "groupwork" &&
        //     element.id_group == val.id_row &&
        //     (element.User.find((x) => x.id_user == this.UserID) ||
        //       element.createdby == this.UserID)
     */
    if(this.groupby == "groupwork" && elementTask.id_group == valuefilter.id_row){
      if(this.isAssignforme){
        // kiểm tra có phải người được giao hay người tạo hay không
        if( this.isAssignForme(elementTask)  || elementTask.createdby == this.UserID ){
          return true;
        } 
      }else{
        return true;
      } 
    }
    return false;
  }

  drop1(event: CdkDragDrop<string[]>) {
    moveItemInArray(this.ListTasks, event.previousIndex, event.currentIndex);
  }

  drop2(event: CdkDragDrop<string[]>) {
    moveItemInArray(this.ListColumns, event.previousIndex, event.currentIndex);
    var item = this.ListColumns[event.currentIndex];
    const itemDrop = new DrapDropItem();
    itemDrop.id_row = 0;
    itemDrop.typedrop = 5;
    itemDrop.id_from = event.previousIndex;
    itemDrop.id_to = event.currentIndex;
    itemDrop.IsAbove = event.previousIndex > event.currentIndex ? true : false;
    itemDrop.fieldname = item.fieldname;
    itemDrop.status = 0;
    itemDrop.status_from = 0;
    itemDrop.status_to = 0;
    itemDrop.priority_from = 0; // neeus bang 2 thi xet
    itemDrop.id_parent = 0;
    itemDrop.id_project_team = +item.id_project_team;
    this.DragDropItemWork(itemDrop);
  }

  dragStarted(event: CdkDragStart, index: number) {
    this.previousIndex = index;
  }

  // list da nhiệm
  // nodes: any[] = demoData;

  // ids for connected drop lists
  dropTargetIds = [];
  nodeLookup = {};
  dropActionTodo: DropInfo = null;

  prepareDragDrop(nodes: any[]) {
    nodes.forEach((node) => {
      this.dropTargetIds.push(node.id_row);
      this.nodeLookup[node.id_row] = node;
      this.prepareDragDrop(node.DataChildren);
    });
  }

  DragDropItemWork(item) {
    const dropItem = new DrapDropItem();
    this._service.DragDropItemWork(item).subscribe((res) => {});
  }

  UpdateCol(fieldname) {
    // var item:any=[];
    // item.id_project_team = this.ID_Project;
    // item.columnname = fieldname;
    const item = new ColumnWorkModel();
    item.id_project_team = +this.ID_Project;
    item.columnname = fieldname;
    this.layoutUtilsService.showWaitingDiv();
    this._service.UpdateColumnWork(item).subscribe((res) => {
      if (res && res.status == 1) {
        this.LoadUpdateCol();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  LoadUpdateCol() {
    this.layoutUtilsService.showWaitingDiv();
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      "",
      "",
      0,
      50,
      true
    );
    this._service.GetDataWorkCU(queryParams).subscribe((res) => {
      if (res && res.status === 1) {
        this.ListColumns = res.data["TenCot"];
        //xóa title khỏi cột
        var colDelete = ["title", "id_row"];
        colDelete.forEach((element) => {
          var indextt = this.ListColumns.findIndex(
            (x) => x.fieldname == element
          );
          if (indextt >= 0) this.ListColumns.splice(indextt, 1);
        });

        this.ListColumns.sort((a, b) =>
          a.title > b.title ? 1 : b.title > a.title ? -1 : 0
        ); // xếp theo anphabet
        this.ListColumns.sort((a, b) =>
          a.id_project_team > b.id_project_team
            ? -1
            : b.id_project_team > a.id_project_team
            ? 1
            : 0
        ); // nào chọn xếp trước
        this.ListColumns.sort((a, b) =>
          a.isbatbuoc > b.isbatbuoc ? -1 : b.isbatbuoc > a.isbatbuoc ? 1 : 0
        ); // nào bắt buộc xếp trước

        this.layoutUtilsService.OffWaitingDiv();
        this.changeDetectorRefs.detectChanges();
      }
    });
  }

  listNewfield: any = [];
  GetOptions_NewField() {
    // this.WeWorkService.GetOptions_NewField(this.ID_Project, 0).subscribe(
    //   (res) => {
    //     if (res && res.status == 1) {
    //       this.listNewfield = res.data;
    //     }
    //   }
    // );
  }

  getDropdownField(idField) {
    var list = this.listNewfield.filter((x) => x.FieldID == idField);
    if (list) {
      return list;
    }
    return [];
  }

  focusInput(id_task, id_field) {
    var idname = "mail" + id_task + "a" + id_field;
    // let ele = (<HTMLInputElement>document.getElementById(idname));
    // setTimeout(() => {
    //   ele.focus();
    // }, 50);
    // setTimeout(()=>{ // this will make the execution after the above boolean has changed
    //   let ele = (<HTMLInputElement>document.getElementById(idname));
    //   ele.focus();
    // },10);
  }

  setCheckField(event) {}

  onSelectFile(event) {
    if (event.target.files && event.target.files[0]) {
      let reader = new FileReader();
      reader.readAsDataURL(event.target.files[0]);
      var base64Str: any = "";
      reader.onload = (event) => {
        base64Str = event.target["result"];
        var metaIdx = base64Str.indexOf(";base64,");
        let strBase64 = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64
        // var icon = { filename: filesAmount.name, strBase64: strBase64, base64Str: base64Str };
        // this.changeDetectorRefs.detectChanges();
      };
    }
  }

  // @debounce(50)
  dragMoved(event) {
    let e = this.document.elementFromPoint(
      event.pointerPosition.x,
      event.pointerPosition.y
    );

    if (!e) {
      this.clearDragInfo();
      return;
    }
    let container = e.classList.contains("node-item")
      ? e
      : e.closest(".node-item");
    if (!container) {
      this.clearDragInfo();
      return;
    }
    this.dropActionTodo = {
      targetId: container.getAttribute("data-id"),
    };
    const targetRect = container.getBoundingClientRect();
    const oneThird = targetRect.height / 3;

    if (event.pointerPosition.y - targetRect.top < oneThird) {
      // before
      this.dropActionTodo["action"] = "before";
    } else if (event.pointerPosition.y - targetRect.top > 2 * oneThird) {
      // after
      this.dropActionTodo["action"] = "after";
    } else {
      // inside
      this.dropActionTodo["action"] = "inside";
    }
    this.showDragInfo();
  }

  drop(event) {
    if (this.CheckRoles(15)) {
      this.isAssignforme = true;
    }
    const itemDrop = new DrapDropItem();
    const draggedItemId = event.item.data; // get data -- id
    const parentItemId = event.previousContainer.id; // từ thằng cha hiện tại
    var status = 0;
    const listdata = this.ListTasks;
    if (!this.dropActionTodo) return;
    // load list new data
    const draggedItem = this.nodeLookup[draggedItemId]; // lấy item từ node

    var listDatanew = this.ListTasks;
    // cách này chỉ dùng gọi data trong 1 bảng
    var stt = draggedItem.status;
    var newArr = this.listStatus.find((x) => x.id_row == stt);
    if (newArr) {
      listDatanew = newArr.data;
    }
    //get list data từ list bỏ đi
    // if(parentItemId=='main'){
    //   //get list bỏ đi
    //   //draggedItem.id_parent = '';
    //   // draggedItem.status = id_row?

    // }

    const targetListId = this.getParentNodeId(
      this.dropActionTodo.targetId,
      listDatanew,
      "main"
    ); // thằng cha mới nếu ngoài cùng thì = main

    //get list data nơi muốn đến
    // if(targetListId=='main' && parentItemId!='main' ){
    //   //get list muốn đến
    //   // draggedItem.status = id_row?
    //   var stt = draggedItem.status;
    //   var newArr = this.listStatus.find(x => x.id_row == stt);
    //   if(newArr){
    //     listDatanew = newArr.data;
    //   }
    // }
    var text =
      "\nmoving\n[" +
      draggedItemId +
      "] from list [" +
      parentItemId +
      "]" +
      ",,," +
      "\n[" +
      this.dropActionTodo.action +
      "]\n[" +
      this.dropActionTodo.targetId +
      "] from list [" +
      targetListId +
      "]";
    itemDrop.id_from = draggedItemId;
    itemDrop.id_to = +this.dropActionTodo.targetId;
    if (this.dropActionTodo.action == "before") {
      itemDrop.IsAbove = true;
    } else {
      itemDrop.IsAbove = false;
    }
    // Set công việc con 2 cấp
    // set parent cho node
    if (this.dropActionTodo.action == "inside") {
      // nếu inside thì nhận node inside làm cha
      const nodeParent = this.nodeLookup[this.dropActionTodo.targetId]; // lấy item từ node
      if (nodeParent.id_parent == null) {
        nodeParent.id_parent = "";
      }
      if (nodeParent.id_parent != "") {
        return;
      } else {
        if (draggedItem.DataChildren.length)
          // nếu có node con thì out không cho phép ghép các node
          return;
        draggedItem.id_parent = this.dropActionTodo.targetId;
      }
    } else {
      // khác thì lấy thằng cha của node mới
      if (targetListId == "main") {
        // node ngoài cùng
        draggedItem.id_parent = "";
      } else {
        // node con
        if (draggedItem.DataChildren.length)
          // nếu có node con thì out không cho phép ghép các node
          return;
        draggedItem.id_parent = targetListId;
      }
    }
    // list data phai la list tất cả thằng cha trong bảng
    const oldItemContainer =
      parentItemId != "main"
        ? this.nodeLookup[parentItemId].DataChildren
        : listDatanew; // lấy con từ thằng cha nế thằng cha main thì lấy nguyên cây
    const newContainer =
      targetListId != "main"
        ? this.nodeLookup[targetListId].DataChildren
        : listDatanew; // lấy list muốn đưa tới chuẩn bị map vào list này

    let i = oldItemContainer.findIndex((c) => c.id_row == draggedItemId); // lấy index từ list cũ
    oldItemContainer.splice(i, 1); // cắt item từ list bỏ đi
    // set parent
    switch (this.dropActionTodo.action) {
      case "before":
      case "after":
        const targetIndex = newContainer.findIndex(
          (c) => c.id_row == this.dropActionTodo.targetId
        ); // tìm id thằng được map - so sánh với thằng được làm mốc
        if (this.dropActionTodo.action == "before") {
          newContainer.splice(targetIndex, 0, draggedItem); // nếu trước nó thì index của thằng mới kéo thay vào index thằng làm mốc
        } else {
          newContainer.splice(targetIndex + 1, 0, draggedItem); // nếu đứng sau thì index thằng làm mốc + 1
        }
        break;

      case "inside": // đưa vào trong làm con của thằng node được chọn
        this.nodeLookup[this.dropActionTodo.targetId].DataChildren.push(
          draggedItem
        ); // get ID node được chọn push item mới vào đó
        this.nodeLookup[this.dropActionTodo.targetId].isExpanded = true; // trạng thái đang mở node
        break;
    }
    itemDrop.typedrop = 1;
    itemDrop.status = +draggedItem.status;
    itemDrop.status_from = +draggedItem.status;
    itemDrop.status_to = +draggedItem.status;
    itemDrop.priority_from = 0; // neeus bang 2 thi xet
    itemDrop.id_row = +draggedItem.id_row;
    itemDrop.id_project_team = +draggedItem.id_project_team;
    itemDrop.id_parent = +draggedItem.id_parent;
    this.DragDropItemWork(itemDrop);
    this.clearDragInfo(true); // xóa thằng this.dropActionTodo  set nó về null
  }
  getParentNodeId(id: string, nodesToSearch: any[], parentId: string): string {
    var findNode = nodesToSearch.find((x) => x.id_row == id);
    if (findNode) {
      return parentId;
    } else {
      for (let node of nodesToSearch) {
        // if (node.id_row == id) return parentId;
        let ret = this.getParentNodeId(id, node.DataChildren, node.id_row);
        if (ret) return ret;
      }
      return null;
    }
  }
  showDragInfo() {
    this.clearDragInfo();
    if (this.dropActionTodo) {
      this.document
        .getElementById("node-" + this.dropActionTodo.targetId)
        .classList.add("drop-" + this.dropActionTodo.action);
    }
  }
  clearDragInfo(dropped = false) {
    if (dropped) {
      this.dropActionTodo = null;
    }
    this.document
      .querySelectorAll(".drop-before")
      .forEach((element) => element.classList.remove("drop-before"));
    this.document
      .querySelectorAll(".drop-after")
      .forEach((element) => element.classList.remove("drop-after"));
    this.document
      .querySelectorAll(".drop-inside")
      .forEach((element) => element.classList.remove("drop-inside"));
  }

  AddnewTask(val, task = false) {
    if (task) {
      this.newtask = val;
      this.addNodeitem = 0;
    } else {
      this.addNodeitem = val;
      this.newtask = -1;
    }
  }
  editTitle(val) {
    this.isEdittitle = val;
    var ele = <HTMLInputElement>document.getElementById("task" + val);
    setTimeout(() => {
      ele.focus();
    }, 50);
  }
  focusOutFunction(node) {
    this.isEdittitle = -1;
    this.UpdateByKey(node, "title", node.title);
  }
  focusFunction(val) {}
  CloseAddnewTask(val) {
    if (val) {
      this.addNodeitem = 0;
      this.newtask = -1;
    }
  }

  taskinsert = new WorkModel();
  AddTask(item) {
    // WorkModel

    var task = new WorkModel();
    task.status = item.id_row;
    task.title = this.taskinsert.title;
    task.id_project_team = this.ID_Project;
    task.Users = [];
    if (this.Assign.id_nv > 0) {
      var assign = this.AssignInsert(this.Assign);
      task.Users.push(assign);
    }
    const start = moment();
    if (
      moment(this.selectedDate.startDate).format("MM/DD/YYYY") != "Invalid date"
    )
      task.start_date = moment(this.selectedDate.startDate).format(
        "MM/DD/YYYY"
      );
    if (
      moment(this.selectedDate.endDate).format("MM/DD/YYYY") != "Invalid date"
    ) {
      task.end_date = moment(this.selectedDate.endDate).format("MM/DD/YYYY");
      task.deadline = moment(this.selectedDate.endDate).format("MM/DD/YYYY");
    }

    this._service.InsertTask(task).subscribe((res) => {
      if (res && res.status == 1) {
        this.CloseAddnewTask(true);
        // this.LoadData();
      }
    });
  }

  AssignInsert(assign) {
    var NV = new UserInfoModel();
    NV = assign;
    NV.id_user = assign.id_nv;
    NV.loai = 1;
    return NV;
  }

  bindStatus(val) {
    var stt = this.status_dynamic.find((x) => +x.id_row == +val);
    if (stt) {
      return stt.statusname;
    }
    return this.translate.instant("GeneralKey.chuagantinhtrang");
  }

  clickOutside() {
    if (this.addNodeitem > 0) {
      this.addNodeitem = 0;
    }
  }
  cot = 1;
  Themcot() {
    // this.ListColumns.push({
    //   fieldname: "cot" + this.cot,
    //   isbatbuoc: true,
    //   isnewfield: false,
    //   isvisible: false,
    //   position: this.ListColumns.length,
    //   title: "Cột" + this.cot,
    //   type: null
    // })
    // this.cot++;
  }
  Assign: any = [];
  // Assign
  ItemSelected(val: any, task, remove = false) {
    // chọn item
    if (remove) {
      val.id_nv = val.id_user;
    }
    this.UpdateByKey(task, "assign", val.id_nv,false);
  }

  listUser: any[];
  LoadListAccount() {
    const filter: any = {};
    // filter.key = 'id_project_team';
    // filter.value = this.ID_Project;
    filter.id_project_team = this.ID_Project;
    this.WeWorkService.list_account(filter).subscribe((res) => {
      if (res && res.status === 1) {
        this.listUser = res.data;
        // this.setUpDropSearchNhanVien();
      }
      this.options_assign = this.getOptions_Assign();
      // this.changeDetectorRefs.detectChanges();
    });
  }
  stopPropagation(event) {
    event.stopPropagation();
  }
  getOptions_Assign() {
    var options_assign: any = {
      showSearch: true,
      keyword: "",
      data: this.listUser,
    };
    return options_assign;
  }

  selectedDate: any = {
    startDate: "",
    endDate: "",
  };
  Selectdate() {
    const dialogRef = this.dialog.open(DialogSelectdayComponent, {
      width: "500px",
      data: this.selectedDate,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result != undefined) {
        this.selectedDate.startDate = new Date(result.startDate);
        this.selectedDate.endDate = new Date(result.endDate);
      }
    });
  }
  ViewDetai(item) {
    this.router.navigate(['', { outlets: { auxName: 'aux/detail/'+item.id_row }, }]);
    // const dialogRef = this.dialog.open(WorkListNewDetailComponent, {
    //   width: "90vw",
    //   height: "90vh",
    //   data: item,
    // });

    // dialogRef.afterClosed().subscribe((result) => {
    //   // this.LoadData();
    //   if (result != undefined) {
    //     // this.selectedDate.startDate = new Date(result.startDate)
    //     // this.selectedDate.endDate = new Date(result.endDate)
    //   }
    // });
  }

  f_convertDate(v: any) {
    if (v != "" && v != undefined) {
      let a = new Date(v);
      return (
        ("0" + a.getDate()).slice(-2) +
        "/" +
        ("0" + (a.getMonth() + 1)).slice(-2) +
        "/" +
        a.getFullYear()
      );
    }
  }
  viewdate() {
    if (this.selectedDate.startDate == "" && this.selectedDate.endDate == "") {
      return "Set due date";
    } else {
      var start = this.f_convertDate(this.selectedDate.startDate);
      var end = this.f_convertDate(this.selectedDate.endDate);
      return start + " - " + end;
    }
  }

  listFilter_Groupby = [
    {
      title: "Status",
      value: "status",
    },
    {
      title: "Assignee",
      value: "assignee",
    },
    {
      title: "groupwork",
      value: "groupwork",
    },
  ];


  listFilter_Subtask = [
    {
      title: "showtask",
      showvalue: "showtask",
      value: "hide",
    },
    {
      title: "expandall",
      showvalue: "expandall",
      value: "show",
    },
  ];

  ExpandNode(node) {
    if (this.filter_subtask.value == "show") return;
    else {
      node.isExpanded = !node.isExpanded;
    }
  }

  ShowCloseTask() {
    this.showclosedtask = !this.showclosedtask;
  }

  LoadClosedTask(val) {
    if (val == this.ItemFinal) {
      return this.showclosedtask;
    }
    return true;
  }

  loadSubtask() {
    var isExpanded = this.filter_subtask.value == "show" ? true : false;
    for (let i of this.listStatus) {
      i.data.forEach((element) => {
        element.isExpanded = isExpanded;
      });
    }
  }
  Subtask(item) {
    if (item.value == this.filter_subtask.value) {
      return;
    }
    this.filter_subtask = item;
    this.loadSubtask();
  }

  CreateTask(val) {
    var x = this.newtask;
    this.CloseAddnewTask(true);
    setTimeout(() => {
      this.newtask = x;
    }, 1000);
    this._service.InsertTask(val).subscribe((res) => {
      if (res && res.status == 1) {
        // this.LoadData();
      } else {
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }

  DeleteTask(task) {
    this._service.DeleteTask(task.id_row).subscribe((res) => {
      if (res && res.status == 1) {
        // this.LoadData();
      }
    });
  }

  clearList() {
    this.selection = new SelectionModel<WorkModel>(true, []);
  }

  IsAdmin() {
    if (this.IsAdminGroup) return true;
    if (this.list_role) {
      var x = this.list_role.find((x) => x.id_row == this.ID_Project);
      if (x) {
        if (x.admin == true) {
          return true;
        }
      }
    }
    return false;
  }

  UpdateStatus(task, status) {
    if (+task.status == +status.id_row) return;
    
    this.UpdateByKey(task, "status", status.id_row);
  }

  UpdateByKey(task, key, value,isReloadData = true) {
    const item = new UpdateWorkModel();
    item.id_row = task.id_row;
    item.key = key;
    item.value = value;
    if (task.id_nv > 0) {
      item.IsStaff = true;
    }
    this._service._UpdateByKey(item).subscribe((res) => {
      if (res && res.status == 1) {
        // this.LoadData();
      } else {
        if(isReloadData){ 
          setTimeout(() => {
            // this.LoadData();
          }, 500);
        }
        this.layoutUtilsService.showError(res.error.message);
      }
    });
  }
  colorName: string = "";
  GetColorName(val) {
    // name
    this.WeWorkService.getColorName(val).subscribe((res) => {
      this.colorName = res.data.Color;
      return this.colorName;
    });
  }

  getTenAssign(val) {
    var list = val.split(" ");
    return list[list.length - 1];
  }
  Updateestimates(task,event){
    this.UpdateByKey(task, "estimates", event);
  }
  UpdateGroup(node,id_row){
    if(id_row == 0){
      this.UpdateByKey(node, 'id_group', null);
    }else
      this.UpdateByKey(node, 'id_group', id_row);
  }
  updateDate(task, date, field) {
    this.UpdateByKey(task, field, moment(date).format("MM/DD/YYYY HH:mm"));
  }
  updatePriority(task, field, value) {
    this.UpdateByKey(task, field, value);
  }

  UpdateTask(task) {
    this._service.UpdateTask(task.id_row).subscribe((res) => {
      // this.LoadData();
      if (res && res.status == 1) {
        // this.LoadData();
      }
    });
  }

  DeleteByKey(task, field) {
    this.UpdateByKey(task, field, null);
  }
  getAssignee(id_nv) {
    if (+id_nv > 0 && this.listUser) {
      var assign = this.listUser.find((x) => x.id_nv == id_nv);
      if (assign) {
        return assign;
      }
      return false;
    }
    return false;
  }

  getPriority(id) {
    var item = this.list_priority.find((x) => x.value == id);
    if (item) return item;
    return id;
  }
  duplicateNew(node, type) {
    this.Update_duplicateNew(node, type);
  }
  Update_duplicateNew(_item: WorkDuplicateModel, type) {
    const dialogRef = this.dialog.open(DuplicateTaskNewComponent, {
      width: "40vw",
      minHeight: "60vh",
      data: { _item, type },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
      } else {
        // this.LoadData();
      }
    });
  }

  duplicate(type: number) {
    var model = new WorkDuplicateModel();
    model.clear();

    model.type = type;
    model.type = type;
    this.Update_duplicate(model);
  }
  Update_duplicate(_item: WorkDuplicateModel) {
    let saveMessageTranslateParam = "";
    saveMessageTranslateParam += "GeneralKey.themthanhcong";
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType = MessageType.Create;
    const dialogRef = this.dialog.open(DuplicateWorkComponent, {
      data: { _item },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
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
  }
  work() {
    const model = new WorkModel();
    model.clear();
  }
  assign(node) {
    var item = this.getOptions_Assign();
    const dialogRef = this.dialog.open(WorkAssignedComponent, {
      width: "500px",
      height: "500px",
      data: { item },
    });
    dialogRef.afterClosed().subscribe((res) => {
      this.UpdateByKey(node, "assign", res.id_nv);
    });
  }
  Add_followers() {
    let saveMessageTranslateParam = "";
    var _item = new WorkModel();
    // _item = this.detail;
    saveMessageTranslateParam +=
      _item.id_row > 0
        ? "GeneralKey.capnhatthanhcong"
        : "GeneralKey.themthanhcong";
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(workAddFollowersComponent, {
      data: { _item },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
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
  }
  Delete() {
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
      
    });
  }

  // nhóm công việc
  Assignmore() {
    var item = this.getOptions_Assign();
    const dialogRef = this.dialog.open(WorkAssignedComponent, {
      width: "500px",
      height: "500px",
      data: { item, ID_Project: this.ID_Project },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        this.selection.selected.forEach((element) => {
          this.UpdateByKey(element, "assign", res.id_nv);
        });
      }
    });
  }
  // nhóm status
  UpdateStatuslist(status) {
    if (this.IsAdmin()) {
    } else {
      if (status.Follower) {
        if (status.Follower != this.UserID) {
          this.layoutUtilsService.showError(
            "Không có quyền thay đổi trạng thái"
          );
        }
      }
    }

    this.selection.selected.forEach((element) => {
      this.UpdateByKey(element, "status", status.id_row);
      // this.UpdateByKey(task, 'status', status.id_row);
    });
  }
  // nhóm start date
  updateStartDateList() {
    var date = moment(this.startDatelist).format("MM/DD/YYYY HH:mm");
    this.selection.selected.forEach((element) => {
      this.UpdateByKey(
        element,
        "start_date",
        moment(this.startDatelist).format("MM/DD/YYYY HH:mm")
      );
      // this.UpdateByKey(task, 'status', status.id_row);
    });
  }
  // nhóm độ ưu tiên
  updatePrioritylist(value) {
    this.selection.selected.forEach((element) => {
      this.UpdateByKey(element, "clickup_prioritize", value);
    });
  }
  // nhóm xóa cv
  XoaCVList() {
    this.selection.selected.forEach((element) => {
      this.DeleteTask(element);
    });
  }

  getViewCheck(node) {
    let checked = this.selection.selected.find((x) => x.id_row == node);
    if (checked) return 1;
    return "";
  }
  Chontatca(node) {
    var list = node.data;
    list.forEach((element) => {
      let checked = this.selection.selected.find(
        (x) => x.id_row == element.id_row
      );
      if (!checked) {
        this.selection.selected.push(element);
      }
    });
  }

  // lisst dupliacte
  UpdateListDuplicate(type) {
    var time = this.selection.selected.length * 500;
    if (type == 1) {
      this.selection.selected.forEach((element) => {
        setTimeout(() => {
          this.DuplicateTask(element, type, this.ID_Project);
        }, 100);
      });
    } else if (type == 2) {
      let saveMessageTranslateParam = "GeneralKey.themthanhcong";
      const _saveMessage = this.translate.instant(saveMessageTranslateParam);
      const _messageType = MessageType.Create;
      const dialogRef = this.dialog.open(DuplicateTaskNewComponent, {
        data: { getOnlyIDproject: true },
      });
      dialogRef.afterClosed().subscribe((res) => {
        if (res) {
          this.selection.selected.forEach((element) => {
            this.DuplicateTask(element, type, res);
          });
        }
      });
    }

    setTimeout(() => {
      this.clearList();
      // this.LoadData();
    }, time);
  }

  DuplicateTask(task, type, id_project_team = 0) {
    const duplicate = new WorkDuplicateModel();
    duplicate.clear();
    duplicate.title = task.title;
    duplicate.type = type;
    duplicate.id = task.id_row;
    duplicate.description = task.description ? task.description : "";
    duplicate.id_parent = task.id_parent ? task.id_parent : 0;
    duplicate.id_project_team = id_project_team;
    if (task.deadline) {
      duplicate.deadline = task.deadline;
    }
    if (task.start_date) {
      duplicate.start_date = task.start_date;
    }
    if (task.id_nv) {
      duplicate.assign = task.id_nv;
    }
    duplicate.id_group = 0;
    duplicate.followers = [];
    duplicate.urgent = "true";
    duplicate.required_result = "true";
    duplicate.require = "true";
    duplicate.Users = [];
    duplicate.duplicate_child = "true";
    this.Create(duplicate);
  }

  Create(_item: WorkDuplicateModel) {
    this._service.DuplicateCU(_item).subscribe((res) => {
      if (res && res.status === 1) {
        this.layoutUtilsService.showActionNotification(
          "Nhân bản thành công",
          MessageType.Read,
          3000,
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
    });
  }

  list_Tag: any = [];
  project_team: any = "";
  mark_tag() {
    this.WeWorkService.lite_tag(this.ID_Project).subscribe((res) => {
      if (res && res.status === 1) {
        this.list_Tag = res.data;
        // this.changeDetectorRefs.detectChanges();
      }
    });
  }

  ReloadData(event) {
    this.ngOnInit();
  }

  RemoveTag(tag, item) {
    const model = new UpdateWorkModel();
    model.id_row = item.id_row;
    model.key = "Tags";
    model.value = tag.id_row;
    this.WorkService.UpdateByKey(model).subscribe((res) => {
      if (res && res.status == 1) {
        // this.LoadData();
        // this.layoutUtilsService.showActionNotification(this.translate.instant('work.dachon'), MessageType.Read, 1000, false, false, 3000, 'top', 1);
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
  chinhsuanoidung(item) {
    if (this.groupby == "status") {
      this.chinhsuastt(item);
    }
    if (this.groupby == "groupwork") {
      this.chinhsuaNhomCV(item);
    }
  }
  chinhsuastt(item) {
    item.id_project_team = this.ID_Project;
    const dialogRef = this.dialog.open(StatusDynamicDialogComponent, {
      width: "40vw",
      minHeight: "200px",
      data: item,
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (res) {
        // this.LoadData();
      }
    });
  }
  chinhsuaNhomCV(item) {
    let saveMessageTranslateParam = "";
    var _item = new WorkGroupModel();
    _item.clear();
    _item.id_project_team = "" + this.ID_Project;
    if (item && item.id_row) {
      _item.id_row = item.id_row;
      _item.title = item.statusname;
      _item.description = item.description;
    }
    saveMessageTranslateParam +=
      _item.id_row > 0
        ? "GeneralKey.capnhatthanhcong"
        : "GeneralKey.themthanhcong";
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(WorkGroupEditComponent, {
      data: { _item },
    });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        return;
      } else {
        this.layoutUtilsService.showActionNotification(
          _saveMessage,
          _messageType,
          4000,
          true,
          false
        );
        // this.LoadData();
      }
    });
  }

  getNhom() {
    return this.groupby;
  }

  getDeadline(fieldname, date) {
    if (fieldname == "deadline") {
      if (new Date(date) < new Date())
        // return 'text-danger'
        return "red-color";
    }
    return "";
  }

  preventCloseOnClickOutTextArea(value) {
    this.textArea = value == "-" ? "" : value;
  }
  allowCloseOnClickOutTextArea(idWork, item, textVal) {
    if (textVal == this.textArea) {
      return;
    }
    this.UpdateValueField(this.textArea, idWork, item);
    // // this.LoadData();
    // đóng matmenu Textarea
  }

  selectedCol(item) {
    const _item = new ColumnWorkModel();
    _item.id_project_team = this.ID_Project;
    _item.title = "";
    _item.columnname = item.fieldname;
    _item.isnewfield = true;
    const dialogRef = this.dialog.open(AddNewFieldsComponent, {
      width: "600px",
      data: _item,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result != undefined) {
        // this.LoadData();
      }
    });
  }

  UpdateField(item) {
    const _item = new ColumnWorkModel();
    _item.id_row = item.id_row;
    _item.id_project_team = this.ID_Project;
    _item.title = item.Title_NewField;
    _item.columnname = item.fieldname;
    _item.isnewfield = true;
    const dialogRef = this.dialog.open(AddNewFieldsComponent, {
      width: "600px",
      data: _item,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result != undefined) {
        // this.LoadData();
      }
    });
  }
  Nguoitaocv(id) {
    if (this.listUser) {
      var x = this.listUser.find((x) => x.id_nv == id);
      if (x) {
        return x;
      }
    }
    return {};
  }

  themThanhvien() {
    var url = "project/" + this.ID_Project + "/settings/members";
    this.router.navigateByUrl(url);
  }

  trackByFn(index, item) {
    return item.id_row;
  }

  update_hidden(item, isDelete = false) {
    var query = "";
    var hidden = item.IsHidden ? 1 : 0;
    if (isDelete) {
      query = `id=${item.id_row}&&hidden=${hidden}&&isdeleted=true`;
    } else {
      hidden = item.IsHidden ? 0 : 1;
      query = `id=${item.id_row}&&hidden=${hidden}`;
    }

    // this._service.update_hidden(query).subscribe((res) => {
    //   if (res && res.status == 1) {
    //     this.LoadUpdateCol();
    //   } else {
    //     this.layoutUtilsService.showError(res.error.message);
    //   }
    // });
  }

  isUpdateStatusname(id = 1) {
    if (!(id > 0)) return false;
    if (
      this.groupby == "status" ||
      this.groupby == "groupwork"
    ) {
      return true;
    }
    return false;
  }

  getNhomCV(node) {
    if (node.id_group > 0) {
      return "- " + node.work_group;
    }
    return "";
  }

  HasColunmHidden(list) {
    var x = list.filter((item) => item.IsHidden && item.isnewfield);
    if (x.length > 0) {
      return true;
    }
    return false;
  }


  getComponentName(id_row) {
    if (id_row) {
      return this.componentName + id_row;
    } else {
      return "";
    }
  }
}

export interface DropInfo {
  targetId: string;
  action?: string;
}

