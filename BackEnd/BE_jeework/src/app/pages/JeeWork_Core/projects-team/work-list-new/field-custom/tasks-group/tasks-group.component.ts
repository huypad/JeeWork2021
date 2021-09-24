import { Component, OnInit, Input,Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-tasks-group',
  templateUrl: './tasks-group.component.html',
  styleUrls: ['./tasks-group.component.scss']
})
export class TasksGroupComponent implements OnInit {

  @Input() ListGroup : any = [];
  @Input() viewdetail= false;
  @Input() value : any = [];
  @Output() valueChange = new EventEmitter<any>();
  @Input() role = false;
  constructor() { }

  ngOnInit(): void {

  }

  UpdateGroup(id){
    this.valueChange.emit(id);
  }

  LoadNhomCongViec(id) {
    if(!this.ListGroup) return;
    var x = this.ListGroup.find((x) => x.id_row == id);
    if (x) {
      return x.title;
    }
    return "Chưa phân loại";
  }
}
