import { TokenStorage } from './../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { Router } from '@angular/router';
import { ListDepartmentService } from './../../Services/List-department.service';
import { ProjectsTeamService } from './../../../projects-team/Services/department-and-project.service';
import { Component, OnInit, ChangeDetectorRef } from '@angular/core';

@Component({
  selector: 'app-report-tab',
  templateUrl: './report-tab.component.html',
  styleUrls: ['./report-tab.component.scss']
})
export class ReportTabComponent implements OnInit {
  loadListfolder = false;
  Id_Department = 0;
  constructor(
		public _deptServices: ListDepartmentService,
    private router: Router,
    private changeDetectorRefs: ChangeDetectorRef,
    private tokenStorage: TokenStorage,
  ) { }

  ngOnInit(): void {
    var path = this.router.url;
    if (path) {
      var arr = path.split("/");
      if (arr.length > 2) this.Id_Department = +arr[2];
    }
    this.LoadDataFolder();
  }

  dataFolder:any = [];
	LoadDataFolder(){
	  this._deptServices.DeptDetail(this.Id_Department).subscribe(res => {
		if (res && res.status == 1) {
		  if(!res.data.ParentID){
			this.dataFolder = res.data.data_folder; 
			var itemhientai = {
			  CreatedBy: res.data.CreatedBy,
			  CreatedDate: res.data.CreatedDate,
			  id_row: res.data.id_row,
			  parentid: res.data.ParentID,
			  templateid: res.data.templateid,
			  title: 'Dự án trực tiếp của phòng ban',
			}
			this.dataFolder.unshift(itemhientai)
			this.loadListfolder = true;
			this.changeDetectorRefs.detectChanges();
		  }
		  
		}
	  })
	}
  getHeight(){ 
		return window.innerHeight - 114 - this.tokenStorage.getHeightHeader();
  }
	ReloadList(event){
    this.router.navigate([`/depts/${this.Id_Department}/report/${event}`]);
	}

}
