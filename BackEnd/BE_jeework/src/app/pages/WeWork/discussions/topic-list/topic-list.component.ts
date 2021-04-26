import { DynamicFormService } from "./../../../dynamic-form/dynamic-form.service";
import {
  Component,
  OnInit,
  ElementRef,
  ViewChild,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  Inject,
  HostListener,
  Input,
  SimpleChange,
} from "@angular/core";
import { ActivatedRoute, Router } from "@angular/router";
// Material
import { MatPaginator, PageEvent } from "@angular/material/paginator";
import { MatSort } from "@angular/material/sort";
import {
  MatDialog,
  MatDialogRef,
  MAT_DIALOG_DATA,
} from "@angular/material/dialog";
// RXJS
import { fromEvent, merge, ReplaySubject, BehaviorSubject } from "rxjs";
import { TranslateService } from "@ngx-translate/core";
// Services
import {
  LayoutUtilsService,
  MessageType,
} from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
// Models
import { QueryParamsModelNew } from "./../../../../_metronic/jeework_old/core/models/query-models/query-params.model";
import { TokenStorage } from "./../../../../_metronic/jeework_old/core/auth/_services/token-storage.service";
import { ViewTopicDetailComponent } from "../topic-view-detail/topic-view-detail.component";
import { DiscussionsService } from "../discussions.service";
import { PlatformLocation } from "@angular/common";
import { TopicEditComponent } from "../topic-edit/topic-edit.component";
import { TopicModel } from "../../projects-team/Model/department-and-project.model";
@Component({
  selector: "kt-topic-list",
  templateUrl: "./topic-list.component.html",
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopicListComponent {
  @Input() ID_QuyTrinh: any;
  data: any[] = [];
  loadingSubject = new BehaviorSubject<boolean>(false);
  loadingControl = new BehaviorSubject<boolean>(false);
  loading1$ = this.loadingSubject.asObservable();
  //=================PageSize Table=====================
  pageEvent: PageEvent;
  pageSize: number;
  pageLength: number;
  item: any;
  sortfield: any = [];
  ChildComponentInstance: any;
  selectedItem: any = undefined;
  childComponentType: any = ViewTopicDetailComponent;
  childComponentData: any = {};
  @ViewChild(MatPaginator, { static: true }) paginator: MatPaginator;
  @ViewChild("keyword", { static: true }) keyword: ElementRef;
  filterTinhTrang: string;
  constructor(
    public _services: DiscussionsService,
    public dialog: MatDialog,
    private route: ActivatedRoute,
    private layoutUtilsService: LayoutUtilsService,
    private activatedRoute: ActivatedRoute,
    private changeDetectorRefs: ChangeDetectorRef,
    private router: Router,
    private translate: TranslateService,
    public dynamicFormService: DynamicFormService,
    private tokenStorage: TokenStorage,
    location: PlatformLocation
  ) {
    this.sortfield = this.listSort[0];
    location.onPopState(() => {
      this.close_detail();
    });
  }

  ngOnInit() {
    this.activatedRoute.params.subscribe((params) => {
      this.ID_QuyTrinh = +params.id;
    });

    var arr = this.router.url.split("/");
    console.log(arr);
    if (arr[1] == "project") this.selectedItem = arr[4];
    if (arr[1] == "wework") this.selectedItem = arr[3];
    this.loadDataList();
  }

  loadDataList(page: boolean = false) {
    const queryParams = new QueryParamsModelNew(
      this.filterConfiguration(),
      "",
      "",
      0,
      50,
      true
    );
    queryParams.sortField = this.sortfield.value;
    queryParams.sortOrder = this.sortfield.sortOder;
    this.data = [];
    this._services.findListTopic(queryParams).subscribe((res) => {
      if (res && res.status === 1) {
        console.log(this.data);
        this.data = res.data;
      }
      this.changeDetectorRefs.detectChanges();
    });
  }

  filterConfiguration(): any {
    const filter: any = {};
    filter.keyword = this.keyword.nativeElement.value;
    return filter;
  }

  selectedField(item) {
    this.sortfield = item;
    this.loadDataList();
  }

  listSort = [
    {
      //CreatedDate
      title: this.translate.instant("day.theongaytao"),
      value: "CreatedDate",
      sortOder: "asc",
    },
    {
      //title
      title: this.translate.instant("GeneralKey.tieude"),
      value: "title",
      sortOder: "asc",
    },
    {
      //UpdatedBy
      title: this.translate.instant("day.theongaycapnhat"),
      value: "UpdatedDate",
      sortOder: "asc",
    },
    {
      //CreatedDate
      title: this.translate.instant("topic.thaoluancunhat"),
      value: "CreatedDate",
      sortOder: "desc",
    },
  ];

  goBack() {
    //let _backUrl = `ListDepartment/Tab/` + this.ID_QuyTrinh;
    //this.router.navigateByUrl(_backUrl);
    window.history.back();
  }
  getHeight(): any {
    let tmp_height = 0;
    tmp_height = window.innerHeight - 63 - this.tokenStorage.getHeightHeader(); //320
    return tmp_height + "px";
  }
  selected($event) {
    this.selectedItem = $event;
    let temp: any = {};
    temp.Id = this.selectedItem.id_row;
    let _backUrl = `/wework/discussions/` + this.selectedItem.id_row;
    var arr = this.router.url.split("/");
    if (arr[1] == "project")
      _backUrl =
        arr[0] +
        "/" +
        arr[1] +
        "/" +
        arr[2] +
        "/" +
        arr[3] +
        "/" +
        this.selectedItem.id_row;
    this.router.navigateByUrl(_backUrl);
    //this.childComponentData.DATA = temp
    //if (this.ChildComponentInstance != undefined)
    //	this.ChildComponentInstance.ngOnChanges();
  }
  close_detail() {
    this.selectedItem = undefined;
    if (!this.changeDetectorRefs["destroyed"])
      this.changeDetectorRefs.detectChanges();
  }
  getInstance($event) {
    this.ChildComponentInstance = $event;
  }
  applyFilter() {
    this.loadDataList();
  }

  AddTopic() {
    const models = new TopicModel();
    models.clear();
    var arr = this.router.url.split("/");
    var id_project_team = arr[2];
    models.id_project_team = id_project_team;
    this.UpdateTopic(models);
  }

  UpdateTopic(_item: TopicModel) {
    let saveMessageTranslateParam = "";
    saveMessageTranslateParam +=
      _item.id_row > 0
        ? "GeneralKey.capnhatthanhcong"
        : "GeneralKey.themthanhcong";
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType =
      _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(TopicEditComponent, { data: { _item } });
    dialogRef.afterClosed().subscribe((res) => {
      if (!res) {
        this.ngOnInit();
        this.changeDetectorRefs.detectChanges();
      } else {
        this.layoutUtilsService.showActionNotification(
          _saveMessage,
          _messageType,
          4000,
          true,
          false
        );
        this.ngOnInit();
        this.changeDetectorRefs.detectChanges();
      }
    });
  }
}
