import { values } from 'lodash';
import { Component, EventEmitter, Input, OnInit, Output, ViewChild, ElementRef } from '@angular/core';

@Component({
  selector: 'time-estimates-view',
  templateUrl: './time-estimates-view.component.html',
  styleUrls: ['./time-estimates-view.component.scss']
})
export class TimeEstimatesViewComponent implements OnInit {
  @ViewChild("times", { static: true }) times: ElementRef;
  @Output() submit = new EventEmitter<any>();
  @Input() time = 0;
  @Input() role = false;
  constructor() { }

  ngOnInit(): void {
  }

  submitOut(){
    if(this.times.nativeElement.value){ 
      this.submit.emit(this.times.nativeElement.value)
      // this.time = this.times.nativeElement.value;
    }
  }

}
