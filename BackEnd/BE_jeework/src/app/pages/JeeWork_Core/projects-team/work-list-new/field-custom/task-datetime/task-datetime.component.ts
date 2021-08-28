import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';
import * as moment from 'moment';

@Component({
  selector: 'app-task-datetime',
  templateUrl: './task-datetime.component.html',
  styleUrls: ['./task-datetime.component.scss']
})
export class TaskDatetimeComponent implements OnInit {

  @Input() fieldname = '';
  @Input() value = '';
  @Output() valueChange = new EventEmitter<any>();
  @Input() role = '';
  isToday = false;
  constructor() { }

  ngOnInit(): void {
    if (this.value){

      this.isToday = moment(this.value).format('MM/DD/YYYY') === moment(new Date()).format('MM/DD/YYYY');
    }
  }
  getDeadline(field, value){

  }
  updateDate(value){
    this.valueChange.emit(value);
  }

  RemoveKey(){
    this.valueChange.emit(null);

  }

  GetDatetime(value){
    if (value){
      if (moment(value).format('MM/DD/YYYY') === moment(new Date()).format('MM/DD/YYYY')){
        return 'Hôm nay';
      }else{
        if (moment(value).format('MM/DD/YYYY') !== 'Invalid date') {
          return moment(value).format('DD/MM/YYYY');
        }
        return value;
      }
    }
  }
}
