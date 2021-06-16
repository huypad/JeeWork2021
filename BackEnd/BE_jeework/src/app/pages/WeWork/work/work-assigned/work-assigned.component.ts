import { LayoutUtilsService } from "./../../../../_metronic/jeework_old/core/utils/layout-utils.service";
import { Router } from "@angular/router";
import { WeWorkService } from "./../../services/wework.services";
import {
  MatDialog,
  MatDialogRef,
  MAT_DIALOG_DATA,
} from "@angular/material/dialog";
import {
  Component,
  OnInit,
  Input,
  Output,
  EventEmitter,
  ChangeDetectorRef,
  Inject,
} from "@angular/core";
import { FormBuilder, FormControl } from "@angular/forms";
import { ReplaySubject } from "rxjs";

@Component({
  selector: "kt-work-assigned",
  templateUrl: "./work-assigned.component.html",
})
export class WorkAssignedComponent implements OnInit {
  // Public properties
  options: any = {
    showSearch: true, //hiển thị search input hoặc truyền keyword
    keyword: "",
    data: [],
  };

  listUser: any[] = [];
  ID_Project = 0;
  public filteredUsers: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
  public userFilterCtrl: FormControl = new FormControl();
  constructor(
    public dialogRef: MatDialogRef<WorkAssignedComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any,
    private FormControlFB: FormBuilder,
    public dialog: MatDialog,
    public router: Router,
    private layoutUtilsService: LayoutUtilsService,
    public weworkService: WeWorkService,
    private changeDetectorRefs: ChangeDetectorRef
  ) {}

  /**
   * On init
   */
  ngOnInit() {
    this.options = this.data.item;
    this.ID_Project = this.data.ID_Project;
    console.log(this.ID_Project);
    this.userFilterCtrl.valueChanges.pipe().subscribe(() => {
      this.filterUsers();
    });
    this.ngOnChanges();
  }
  ngOnChanges() {
    this.userFilterCtrl.setValue("");
    this.listUser = [];
    if (this.options.showSearch == undefined) this.options.showSearch = true;
    if (this.options != undefined) {
      if (this.options.data) {
        this.listUser = this.options.data;
        this.filterUsers();
        this.changeDetectorRefs.detectChanges();
      } 
	  if(this.ID_Project > 0){
		this.weworkService
		.list_account({ id_project_team: this.ID_Project })
		.subscribe((res) => {
		  if (res && res.status === 1) {
			this.listUser = res.data;
			// mảng idnv exclude
			if (this.options.excludes && this.options.excludes.length > 0) {
			  var arr = this.options.excludes;
			  this.listUser = this.listUser.filter(
				(x) => !arr.includes(x.id_nv)
			  );
			}
			this.filterUsers();
			this.changeDetectorRefs.detectChanges();
		  }
		});
	  }
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

  themThanhvien() {
    var url = "project/" + this.ID_Project + "/settings/members";
    this.router.navigateByUrl(url);
  }
}
