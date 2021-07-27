import { DepartmentModel } from './../Model/List-department.model';
import { LayoutUtilsService } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { ListDepartmentService } from './../Services/List-department.service';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Component, OnInit, Inject } from '@angular/core';

@Component({
  selector: 'app-creat-quick-folder',
  templateUrl: './creat-quick-folder.component.html',
  styleUrls: ['./creat-quick-folder.component.scss']
})
export class CreatQuickFolderComponent implements OnInit {
	viewLoading: boolean = false;
  folderName :string = "";
  constructor(
    public dialogRef: MatDialogRef<CreatQuickFolderComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
		private _Services: ListDepartmentService,
		private layoutUtilsService: LayoutUtilsService,

  ) { }

  ngOnInit(): void {
    console.log('data folder quick:' , this.data);  
  }
  close() {
		this.dialogRef.close();
	}
  onSubmit() {
		if(this.folderName){
      const folder = new DepartmentModel();
      folder.clear();
      folder.title = this.folderName;
      folder.ParentID = this.data.item.id;
      this.Create(folder);
    }else{
      this.layoutUtilsService.showError('Nhập tên thư mục');
    }
	}
  Create(_item) { 
    this.viewLoading = true;
		this._Services.InsertQuickFolder(_item).subscribe((res) => {
			if (res && res.status === 1) {
				this.layoutUtilsService.showInfo('Thêm thành công');
        setTimeout(() => {
          window.location.reload();
        }, 50);
			} else {
				this.viewLoading = false;
				this.layoutUtilsService.showError(res.error.message);
			}
		});
	}
}
