import { Component, EventEmitter, Input, OnInit, Output } from '@angular/core';

@Component({
  selector: 'app-automation-tab-browse',
  templateUrl: './automation-tab-browse.component.html',
  styleUrls: ['./automation-tab-browse.component.scss']
})
export class AutomationTabBrowseComponent implements OnInit {
  @Output() eventClose = new EventEmitter<any>();

  @Input() ID_projectteam : number = 0;
  @Input() ID_department : number = 0;
  constructor() { }

  ngOnInit(): void {
  }
 
  Close(event){
    this.eventClose.emit(event);
  }
}
