import { WorkGroupEditComponent } from "./../../../work/work-group-edit/work-group-edit.component";
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { SubheaderService } from "./../../../../../_metronic/partials/layout/subheader/_services/subheader.service";
import { TokenStorage } from "./../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service";
import { QueryParamsModelNew } from "./../../../../../_metronic/jeework_old/core/models/query-models/query-params.model";
import { MenuPhanQuyenServices } from "./../../../../../_metronic/jeework_old/core/_base/layout/services/menu-phan-quyen.service";
import { AttachmentService } from "./../../../services/attachment.service";
import {
  AttachmentModel,
  FileUploadModel,
} from "./../../Model/department-and-project.model";
import { AddNewFieldsComponent } from "./../add-new-fields/add-new-fields.component";
import { StatusDynamicDialogComponent } from "./../../../status-dynamic/status-dynamic-dialog/status-dynamic-dialog.component";
import { WorkService } from "./../../../work/work.service";
import { DuplicateTaskNewComponent } from "./../duplicate-task-new/duplicate-task-new.component";
import { WorkListNewDetailComponent } from "./../work-list-new-detail/work-list-new-detail.component";
import { DialogSelectdayComponent } from "./../../../report/dialog-selectday/dialog-selectday.component";
import {
  WorkModel,
  UpdateWorkModel,
  UserInfoModel,
  WorkDuplicateModel,
  WorkGroupModel,
} from "./../../../work/work.model";
import { ColumnWorkModel, DrapDropItem } from "./../drap-drop-item.model";
import {
  filter,
  tap,
  catchError,
  finalize,
  share,
  takeUntil,
  debounceTime,
  startWith,
  switchMap,
} from "rxjs/operators";
import { element } from "protractor";
import { WeWorkService } from "./../../../services/wework.services";
import { DatePipe, DOCUMENT } from "@angular/common";
import { TranslateService } from "@ngx-translate/core";
import { FormBuilder, FormControl } from "@angular/forms";
import { Router, ActivatedRoute } from "@angular/router";
import { ProjectsTeamService } from "./../../Services/department-and-project.service";
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
  SimpleChanges,
} from "@angular/core";
import { MatTable } from "@angular/material/table";
import { MatDialog } from "@angular/material/dialog";
import { MatSort } from "@angular/material/sort";
import { cloneDeep, find, values } from "lodash";
import * as moment from "moment";
import { SelectionModel } from "@angular/cdk/collections";
import { workAddFollowersComponent } from "../../../work/work-add-followers/work-add-followers.component";
// import { WorkEditDialogComponent } from "../../work/work-edit-dialog/work-edit-dialog.component";
import { WorkAssignedComponent } from "../../../work/work-assigned/work-assigned.component";
import { DuplicateWorkComponent } from "../../../work/work-duplicate/work-duplicate.component";
import { OverlayContainer } from "@angular/cdk/overlay";
import { BehaviorSubject, of, Subject, SubscriptionLike, throwError } from "rxjs";
import { CommunicateService } from "../work-list-new-service/communicate.service";


@Component({
  selector: 'app-works-dash-board',
  templateUrl: './works-dash-board.component.html',
  styleUrls: ['./works-dash-board.component.scss']
})
export class WorksDashBoardComponent implements OnInit, OnChanges {
  @Input() ListProject: any = [];
  @Input() Id_Department: any = 0;
  @Input() isFolder: boolean = false;
  subscription: SubscriptionLike;

  ListtopicObjectID$: BehaviorSubject<any> = new BehaviorSubject<any>([]);
  data: any = [];
  listFilter: any = [];
  ListTags: any = [];
  ListUsers: any = [];
  editmail = 0;
  isAssignforme = false;
  // col
  displayedColumnsCol: string[] = [];
  @ViewChild(MatSort, { static: true }) sort: MatSort;
  previousIndex: number;
  ListAction: any = [];
  addNodeitem = 0;
  newtask = -1;
  options_assign: any = {};
  filter_groupby: any = [];
  filter_subtask: any = [];
  list_milestone: any = [];
  Assign_me = -1;
  keyword: string = "";
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
  filter: any = [];
  listType: any = [];
  textArea: string = "";
  searchCtrl: FormControl = new FormControl();
  private readonly componentName: string = "kt-task_";
  Emtytask = false;
  filterDay = {
    startDate: new Date("09/01/2020"),
    endDate: new Date("09/30/2020"),
  };
  type = 1;
  IsAdminGroup = false;
  public column_sort: any = [];
  onChanges = new Subject<SimpleChanges>();
  constructor(
    @Inject(DOCUMENT) private document: Document, // multi level
    private _service: ProjectsTeamService,
    private workService: WorkService,
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
    private CommunicateService: CommunicateService,
    private _attservice: AttachmentService
  ) {
    this.filter_groupby = this.listFilter_Groupby[0];
    this.filter_subtask = this.listFilter_Subtask[0];
    this.list_priority = this.WeWorkService.list_priority;
    this.UserID = +localStorage.getItem("idUser");
    
    var today = new Date();
    var start_date = new Date();
    this.filterDay = {
      endDate: new Date(today.setMonth(today.getMonth() + 1)),
      startDate: new Date(start_date.setMonth(start_date.getMonth() - 1)),
    };

    this.column_sort = this.sortField[0];
  }

  ngOnInit() {

    if(this.isFolder){
      this.type = 2;
    }

     // giao tiếp service
     this.subscription = this.CommunicateService.currentMessage.subscribe(message => {
      if(message){
        console.log('LoadData');
        this.LoadData();
      }
    });
    //end giao tiếp service
    
    this.searchCtrl.valueChanges
    .pipe(
      debounceTime(1000),
      startWith("")
    )
    .subscribe(res => {
      this.keyword = res;
      this.LoadData();
    });
    // this.selection = new SelectionModel<WorkModel>(true, []);
    this.menuServices.GetRoleWeWork("" + this.UserID).subscribe((res) => {
      if (res && res.status == 1) {
        this.list_role = res.data.dataRole;
        this.IsAdminGroup = res.data.IsAdminGroup;
      }
      
    });
    // this.LoadData();

    // this.LoadNewList();
    this.onChanges.subscribe((data:SimpleChanges)=>{
      if(data.Id_Department){
        this.LoadData();
      }
    });
  }
  listField$ = new BehaviorSubject<any>("");
  listField : any = [];

  DataSpace = new BehaviorSubject<any[]>([]);
  LoadNewList(){
    if(this.Id_Department <= 0)
      return;
    console.log(this.filterConfiguration());
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration()
    );
    this.layoutUtilsService.showWaitingDiv();
    this.workService.WorkFilter(queryParams).pipe(
      switchMap(resultFromServer => of(resultFromServer).pipe(
        tap(resultFromServer => {
          this.DataSpace.next(resultFromServer.data);
        })
      )),
      catchError(err => throwError(err)),
      finalize(() => console.log)
    ).subscribe( () => {
      this.layoutUtilsService.OffWaitingDiv()  
    });
  }



  ngOnChanges(changes: SimpleChanges) {
    console.log(changes,'2')
    this.onChanges.next(changes);
  }

  loadSubtask() {
    var isExpanded = this.filter_subtask.value == "show" ? true : false;
  }
  Subtask(item) {
    if (item.value == this.filter_subtask.value) {
      return;
    }
    this.filter_subtask = item;
    this.loadSubtask();
  }

  LoadData() {

    this.filter = this.filterConfiguration();
    this.data = [];
    // this.layoutUtilsService.showWaitingDiv();
    this.WeWorkService.GetNewField().subscribe((res) => {
      if (res && res.status == 1) {
        this.listNewField = res.data;
      }
    });
    this.getListField();
    this.LoadNewList(); 
  }

  getListField(Loading = false){
    if(Loading){
      this.layoutUtilsService.showWaitingDiv();
    }
    this.GetOptions_NewField();
    this.WeWorkService.GetListField(this.Id_Department,this.isFolder?2:1,false).pipe(
      finalize( () => { if(Loading) this.layoutUtilsService.OffWaitingDiv() } ),
    ).subscribe(res => {
      if (res && res.status === 1) {
         var listField = res.data;
        //xóa title khỏi cột

        var colDelete = ['title','id_row','id_parent'];
        colDelete.forEach(element => {
          var indextt = listField.findIndex(x => x.fieldname == element);
          if (indextt >= 0)
            listField.splice(indextt, 1)
        });
        this.listField$.next(listField);
        this.listField= listField;
        console.log(listField);
        this.changeDetectorRefs.detectChanges();
      }
      else{
        this.layoutUtilsService.showInfo(res.error.message);
      }
    });
  }

  ngOnDestroy(): void {
    //Called once, before the instance is destroyed.
    //Add 'implements OnDestroy' to the class.
    if(this.subscription){
      this.subscription.unsubscribe();
    }
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
    const filter: any = {};
    filter.groupby = this.filter_groupby.value; //assignee
    filter.keyword = this.keyword;
    filter.TuNgay = this.f_convertDate(this.filterDay.startDate).toString();
    filter.DenNgay = this.f_convertDate(this.filterDay.endDate).toString();
    filter.collect_by = this.column_sort.value?this.column_sort.value:'CreatedDate';
    if(!this.isFolder){
      filter.spaceid = this.Id_Department;
    }else{
      filter.folderid = this.Id_Department;
    }
      return filter;
  }

  SelectedField(item) {
    this.column_sort = item;
    this.LoadData();
  }

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

  LoadUpdateCol() {
    // this.layoutUtilsService.showWaitingDiv();
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      "",
      "",
      0,
      50,
      true
    );
  }
 
  setCheckField(event) {}

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
  GroupBy(item) {
    if (item == this.filter_groupby) {
      return;
    }
    this.filter_groupby = item;
    this.LoadData();
  }

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


  ShowCloseTask() {
    this.showclosedtask = !this.showclosedtask;
  }



  SelectFilterDate() {
    const dialogRef = this.dialog.open(DialogSelectdayComponent, {
      width: "500px",
      data: this.filterDay,
    });

    dialogRef.afterClosed().subscribe((result) => {
      if (result != undefined) {
        this.filterDay.startDate = new Date(result.startDate);
        this.filterDay.endDate = new Date(result.endDate);
        this.LoadData();
      }
    });
  }
 

  sortField = [
    {
      title: this.translate.instant("day.theongaytao"),
      value: "CreatedDate",
    },
    {
      title: this.translate.instant("day.theothoihan"),
      value: "Deadline",
    },
    {
      title: this.translate.instant("day.theongaybatdau"),
      value: "StartDate",
    },
  ];

  getHeight() {
    var height = window.innerHeight - 175 - this.tokenStorage.getHeightHeader();
    return height;
  }

  listNewfield:any = [];
  GetOptions_NewField() {
    this.WeWorkService.GetOptions_NewField(this.Id_Department, 0, this.isFolder?2:1).subscribe(
      (res) => {
        if (res && res.status == 1) {
          this.listNewfield = res.data;
          console.log(this.listNewfield)
        }
      }
    );
  }
}

export interface DropInfo {
  targetId: string;
  action?: string;
}

