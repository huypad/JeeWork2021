import { MatAccordion } from '@angular/material/expansion';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { Component, OnInit, Inject, ViewChild } from '@angular/core';

@Component({
  selector: 'app-template-center',
  templateUrl: './template-center.component.html',
  styleUrls: ['./template-center.component.scss']
})
export class TemplateCenterComponent implements OnInit {
  @ViewChild(MatAccordion) accordion: MatAccordion;
  buocthuchien = 1;
  importall = true;
  ProjectDatesDefault = true;
  chontacvu = 1;
  constructor(
		public dialogRef: MatDialogRef<TemplateCenterComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
  ) { }

  ngOnInit(): void {
  }

  getBackground(text){
    return 'rgb(27, 94, 32)';
  }

  CheckedType(item){
    console.log(item);
    if(item.countitem >0 ){
      item.checked = !item.checked;
    }
  }
  Types = [
    {
      checked:true,
      name:'Space',
      countitem: 5,
    },
    {
      checked:false,
      name:'Folder',
      countitem: 68,
    },
    {
      checked:false,
      name:'List',
      countitem: 15,
    },
    {
      checked:false,
      name:'Task',
      countitem: 0,
    },
    {
      checked:false,
      name:'Doc',
      countitem: 40,
    },
    {
      checked:false,
      name:'View',
      countitem: 0,
    },
  ]
}
