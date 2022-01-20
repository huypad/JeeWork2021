import {ChangeDetectorRef, Component, OnInit} from '@angular/core';
import {FilterModel, LuuYModel, MyWorkModel, UserInfoModel} from '../work.model';
import {Router} from '@angular/router';
import {ProjectsTeamService} from '../../projects-team/Services/department-and-project.service';
import {WorkService} from '../work.service';
import {MatDialog} from '@angular/material/dialog';
import {TranslateService} from '@ngx-translate/core';
import { JeeWorkLiteService } from '../../services/wework.services';
import {TokenStorage, UserProfileService} from '../../../../_metronic/jeework_old/core/auth/_services';
import {filterService} from '../../filter/filter.service';
import {LayoutUtilsService, MessageType} from '../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import {filterEditComponent} from '../../filter/filter-edit/filter-edit.component';
import {MilestoneModel} from '../../projects-team/Model/department-and-project.model';
import {milestoneDetailEditComponent} from '../../department/milestone-detail-edit/milestone-detail-edit.component';

@Component({
  selector: 'app-summary',
  templateUrl: './summary.component.html',
  styleUrls: ['./summary.component.scss']
})
export class SummaryComponent implements OnInit {

  activeLink = 'home';
  milestone: any = [];
  listfilter: any = [];
  mystaff: UserInfoModel[] = [];
  item: MyWorkModel;
  count: any = [];
  moigiao: any = [];
  giaoquahan: any = [];
  note: LuuYModel;
  show: boolean = false;
  // load menu right
  listUser: any = [];
  listProject: any = [];
  profile: any = [];
  Image: string = "";
  Ten: string = "";
  ChucVu: string = "";
  constructor(
      private router: Router,
      public myworkSer: ProjectsTeamService,
      private _service: WorkService,
      public dialog: MatDialog,
      private changeDetect: ChangeDetectorRef,
      private translate: TranslateService,
      private weworkService: JeeWorkLiteService,
      private tokenStore: TokenStorage,
      private userProfileService: UserProfileService,
      private _filterService: filterService,
      private layoutUtilsService: LayoutUtilsService,
  ) { }

  loadThongTinUser() {
    let id: any;
    this.tokenStore.getUserData().subscribe(res => {
      if(res){
        this.Image = res.Image;
        this.Ten = res.HoTen;
        this.ChucVu = res.ChucVu;
      }
      else{
        setTimeout(() => {
          this.loadThongTinUser();
        }, 1000);
      }
    });
  }
  ngOnInit() {
    var arr = this.router.url.split('/');
    if (arr.length > 2)
      this.activeLink = arr[2];

    // load menu right
    this.weworkService.list_account({}).subscribe(res => {
      if (res && res.status == 1)
        this.listUser = res.data;
    });
    this.loadThongTinUser();
    // this._service.mymilestone().subscribe(res => {
    // 	if (res && res.status === 1) {
    // 		this.milestone = res.data;
    // 		this.changeDetect.detectChanges();
    // 	}
    // });
    this.LoadFilter();
    this.myworkSer.MyWork().subscribe(res => {
      if (res && res.status == 1) {
        this.item = res.data;
        this.count = res.data.Count;
        this.giaoquahan = res.data.GiaoQuaHan;
        this.moigiao = res.data.MoiDuocGiao;
        this.note = res.data.LuuY;
      }
    });

    // this._service.myStaff().subscribe(res => {
    // 	if (res && res.status === 1) {
    // 		this.mystaff = res.data;
    // 	}
    // 	this.changeDetect.detectChanges();
    // });
    //load ds dự án và chèn tất cả dự án vào đầu
    this.weworkService.lite_project_team_byuser("").subscribe(res => {
      if (res && res.status === 1) {
        this.listProject = res.data;
        this.listProject.unshift(
            {
              title: this.translate.instant('filter.tatcaduan'),
              id_row: ''
            }
        )
      };
    });
  }
  LoadFilter() {
    this._service.Filter().subscribe(res => {
      if (res && res.status === 1) {
        this.listfilter = res.data;
        this.changeDetect.detectChanges();
      }
    });
  }
  click(activeLink, id = '0') {
    this.activeLink = activeLink;
    var url = './';
    if (activeLink != "home") {
      url += activeLink;
    }

    if (+id != 0) {
      this.router.navigate([url,id]).then(() => {
        this.ngOnInit();
      });
    }
    else {
      this.router.navigate([url]).then(() => {
        this.ngOnInit();
      });
    }
  }

  addFilter() {
    const model = new FilterModel();
    model.clear(); // Set all defaults fields
    this.Update(model);
  }
  Update(_item: FilterModel) {
    let saveMessageTranslateParam = '';
    saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(filterEditComponent, { data: { _item } });
    dialogRef.afterClosed().subscribe(res => {
      if (!res) {
        return;
      }
      else {
        this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
        this.LoadFilter();
        // this.changeDetect.detectChanges();
      }
    });
  }

  getItemCssClassByurgent(status: boolean): string {

    switch (status) {
      case true:
        return 'metal';
    }
  }
  getItemurgent(condition: boolean): string {
    switch (condition) {
      case true:
        return 'Urgent';
    }
  }
  DeleteFilter(_item: FilterModel) {
    const _title = this.translate.instant('GeneralKey.xoa');
    // const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
    // const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
    const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
    // const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
    // dialogRef.afterClosed().subscribe(res => {
    // 	if (!res) {
    // 		return;
    // 	}

    // });
    this.layoutUtilsService.showWaitingDiv();
    this._filterService.Delete_filter(_item.id_row).subscribe(res => {
      this.layoutUtilsService.OffWaitingDiv();
      if (res && res.status === 1) {
        this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
        let _backUrl = `tasks`;
        this.router.navigateByUrl(_backUrl);
        this.LoadFilter();
        // this.changeDetect.detectChanges();
      }
      else {
        // this.LoadFilter();
        this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
      }

    });
  }

  addMileston(){
    let saveMessageTranslateParam = '';
    var _item = new MilestoneModel;
    _item.clear();
    _item.id_project_team = 0;
    saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
    const _saveMessage = this.translate.instant(saveMessageTranslateParam);
    const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
    const dialogRef = this.dialog.open(milestoneDetailEditComponent, { data: { _item } });
    dialogRef.afterClosed().subscribe(res => {
      if (!res) {
        // this.ngOnInit();
        return;
      }
      else {
        this.ngOnInit();
        this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
        // this.changeDetectorRefs.detectChanges();
      }
    });
  }

  selectedMileston(_item){
    this.myworkSer.FindDepartmentFromProjectteam(_item.id_project_team).subscribe(res => {
      if (res && res.status == 1) {
        const url = 'depts/' + res.data + '/milestones/' + _item.id_row;
        this.router.navigateByUrl(url);
      }
    })
  }

  getHeight() {
    let tmp_height = 0;
    tmp_height = window.innerHeight - 50 - this.tokenStore.getHeightHeader();
    return tmp_height + 'px';
  }
}
