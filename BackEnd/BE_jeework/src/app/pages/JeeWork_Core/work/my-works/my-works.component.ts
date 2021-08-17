import { WorkService } from "./../work.service";
import { ActivatedRoute } from "@angular/router";
import { ChangeDetectorRef, Component, OnInit } from "@angular/core";

@Component({
  selector: "kt-my-works",
  templateUrl: "./my-works.component.html",
  styleUrls: ["./my-works.component.scss"],
})
export class MyWorksComponent implements OnInit {
  selectedTab: number = 0;
  idFilter: number = 0;
  UserID: number = 0;
  detailWork: number = 0;
  data: any = [];
  constructor(
    private activatedRoute: ActivatedRoute,
    private _service: WorkService,
    private route: ActivatedRoute,
    private changeDetect: ChangeDetectorRef
  ) {
    this.UserID = +localStorage.getItem("idUser");
  }

  ngOnInit() {
    this.activatedRoute.params.subscribe((res) => {
      if (res && res.id) this.idFilter = res.id;
    });
    this.activatedRoute.data.subscribe((res) => {
      if (res && res.selectedTab) this.selectedTab = res.selectedTab;
    });
    this.route.queryParamMap.subscribe((params) => {
      const pr = params["params"];
      if (pr && pr.detail) {
        if (+pr.detail > 0) {
          this.detailWork = pr.detail;
        }
      }

    });
    this.LoadFilter();
  }

  LoadFilter() {
    this._service.Filter().subscribe((res) => {
      if (res && res.status === 1) {
        this.changeDetect.detectChanges();
      }
    });
  }
}
