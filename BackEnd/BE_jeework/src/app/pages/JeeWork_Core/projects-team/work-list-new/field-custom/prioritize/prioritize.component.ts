import { Component, OnInit, Output, Input, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-prioritize',
  templateUrl: './prioritize.component.html',
  styleUrls: ['./prioritize.component.scss']
})
export class PrioritizeComponent implements OnInit {
  @Output() submit = new EventEmitter<any>();
  @Input() value = 0;
  @Input() role = false;

  list_priority = [
    {
      name: 'Urgent',
      value: 1,
      icon: 'fab fa-font-awesome-flag text-danger',
    },
    {
      name: 'High',
      value: 2,
      icon: 'fab fa-font-awesome-flag text-warning',
    },
    {
      name: 'Normal',
      value: 3,
      icon: 'fab fa-font-awesome-flag text-info',
    },
    {
      name: 'Low',
      value: 4,
      icon: 'fab fa-font-awesome-flag text-muted',
    },
    {
      name: 'Clear',
      value: 0,
      icon: 'fas fa-times text-danger',
    },
  ];

  constructor() { }

  ngOnInit(): void {
  }

  getPriority(id) {
    var item = this.list_priority.find((x) => x.value == id);
    if (item) return item;
    return id;
  }

  updatePriority(value){
    this.submit.emit(value);
  }
}
