import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'app-automation-label',
  templateUrl: './automation-label.component.html',
  styleUrls: ['./automation-label.component.scss']
})
export class AutomationLabelComponent implements OnInit {
  @Input() row: number = 1; 
  @Input() value: any = []; 
  editdesc = false;
  constructor() { }

  ngOnInit(): void {
  }

}
