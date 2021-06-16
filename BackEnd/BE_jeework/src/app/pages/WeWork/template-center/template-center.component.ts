import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Component, OnInit, Inject } from '@angular/core';

@Component({
  selector: 'app-template-center',
  templateUrl: './template-center.component.html',
  styleUrls: ['./template-center.component.scss']
})
export class TemplateCenterComponent implements OnInit {

  constructor(
		public dialogRef: MatDialogRef<TemplateCenterComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
  ) { }

  ngOnInit(): void {
  }

}
