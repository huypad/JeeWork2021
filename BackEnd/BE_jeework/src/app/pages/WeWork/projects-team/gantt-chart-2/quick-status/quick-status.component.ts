import { DialogData } from "./../../../report/report-tab-dashboard/report-tab-dashboard.component";
import { MatDialogRef, MAT_DIALOG_DATA } from "@angular/material/dialog";
import { Component, OnInit, Inject } from "@angular/core";

@Component({
  selector: "app-quick-status",
  templateUrl: "./quick-status.component.html",
  styleUrls: ["./quick-status.component.scss"],
})
export class QuickStatusComponent implements OnInit {
  constructor(
    public dialogRef: MatDialogRef<QuickStatusComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DialogData
  ) {}

  ngOnInit(): void {
    console.log(this.data,'data')
  }

  onNoClick(): void {
    this.dialogRef.close();
  }

  selectSTT(status){
    this.dialogRef.close(status);
  }
}
