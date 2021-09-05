import {Component, OnInit, Input, Output, EventEmitter, OnChanges, SimpleChanges, ChangeDetectorRef} from '@angular/core';
import * as moment from 'moment';
import {TimezonePipe} from '../../../../pipe/timezone.pipe';

@Component({
  selector: 'app-task-datetime',
  templateUrl: './task-datetime.component.html',
  styleUrls: ['./task-datetime.component.scss'],
  providers:[TimezonePipe]
})
export class TaskDatetimeComponent implements OnInit,OnChanges {

  @Input() fieldname = '';
  @Input() value = '';
  @Output() valueChange = new EventEmitter<any>();
  @Input() role = '';
  @Input() showall = true;
  isToday = false;
  date :any = '';
  constructor(
      private pipetimezone:TimezonePipe,
      private cd: ChangeDetectorRef
  ) {
  }

  ngOnInit(): void {
  }

  getIconCalendar(){
    if(this.fieldname == 'deadline')
      return 'flaticon-calendar-3';
    else if(this.fieldname == 'start_date')
      return 'flaticon-calendar-2';
    else
      return 'flaticon-calendar-1';
  }

  ngOnChanges(changes: SimpleChanges) {
    if (this.value){
      this.isToday = moment(this.value).format('MM/DD/YYYY') === moment(new Date()).utc().format('MM/DD/YYYY');
    }
    this.date = this.pipetimezone.transform(this.value,'YYYY-MM-DD')+'T'+this.pipetimezone.transform(this.value,'HH:mm:ss');
    this.cd.detectChanges()
  }

  getClassdate(){
    if(this.fieldname == 'deadline' && moment(this.value).format('MM/DD/YYYY HH:mm:ss') < moment(new Date()).utc().format('MM/DD/YYYY HH:mm:ss')){
      return 'trehan';
    }
    if(this.fieldname == 'deadline' && this.isToday)
      return 'homnay';
    return '';
  }
  updateDate(value){
    this.valueChange.emit(moment(value).utc().format("MM/DD/YYYY HH:mm"));
    // this.valueChange.emit(moment(value).format("MM/DD/YYYY HH:mm"));
  }

  RemoveKey(){
    this.valueChange.emit(null);

  }

  GetDatetime(value){
    if (value){
      if (moment(value).format('MM/DD/YYYY') === moment(new Date()).format('MM/DD/YYYY') && this.fieldname == 'deadline'){
        return 'Hôm nay';
      }else{
        if (moment(value).format('MM/DD/YYYY') !== 'Invalid date') {
          return moment(value).format('DD/MM/YYYY');
        }
        return value;
      }
    }
    return value;
  }
}
