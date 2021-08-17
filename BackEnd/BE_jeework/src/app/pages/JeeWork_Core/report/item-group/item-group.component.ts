import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'kt-item-group',
  templateUrl: './item-group.component.html'
})
export class ItemGroupComponent implements OnInit {

  @Input() value: any = undefined;
  constructor() { }

  ngOnInit() {
  }

}
