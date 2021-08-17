import { Component, Inject, OnInit } from '@angular/core';
import {MatDialog, MatDialogRef, MAT_DIALOG_DATA} from '@angular/material/dialog';
import { DialogData } from '../report-tab-dashboard/report-tab-dashboard.component';

@Component({
  selector: 'kt-dialog-selectday',
  templateUrl: './dialog-selectday.component.html',
})
export class DialogSelectdayComponent implements OnInit {
start_temp: any;
end_temp: any;
  constructor(
    public dialogRef: MatDialogRef<DialogSelectdayComponent>,
    @Inject(MAT_DIALOG_DATA) public data: DialogData
  ) { }

  ngOnInit() {
    this.start_temp = this.data.startDate;
    this.end_temp = this.data.endDate;
  }

  onNoClick(): void {
     this.data.startDate = this.start_temp;
     this.data.endDate = this.end_temp;
    this.dialogRef.close();
  }

}
