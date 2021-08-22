import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-task-datetime',
  templateUrl: './task-datetime.component.html',
  styleUrls: ['./task-datetime.component.scss']
})
export class TaskDatetimeComponent implements OnInit {

  @Input() fieldname = "";
  @Input() value = "";
  @Output() valueChange = new EventEmitter<any>();
  @Input() role = "";
  constructor() { }

  ngOnInit(): void {
  }
  getDeadline(field,value){
    console.log('deadline:',field,value);

  }
  updateDate(field,value){
    console.log('update date:',field,value);
    this.valueChange.emit(value);
  }

  RemoveKey(field,value){
    console.log('remove key:',field,value);
    this.valueChange.emit(value);

  }
}
