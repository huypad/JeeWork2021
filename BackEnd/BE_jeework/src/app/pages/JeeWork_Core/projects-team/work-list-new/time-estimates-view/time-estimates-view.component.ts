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

  //Hàm kiêm tra===================================
	text(e: any) {
		if (!((e.keyCode > 95 && e.keyCode < 106)
			|| (e.keyCode > 47 && e.keyCode < 58)
			|| e.keyCode == 8)) {
			e.preventDefault();
		}
	}

}
