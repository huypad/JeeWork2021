import { Component, OnInit, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-danhsachthumuc',
  templateUrl: './danhsachthumuc.component.html',
  styleUrls: ['./danhsachthumuc.component.scss']
})
export class DanhsachthumucComponent implements OnInit {

  @Input() dataSource : any = [];
  @Output() ChangeFolder = new EventEmitter<any>();
  displayedColumns: string[] = ['Tenthumuc', 'Ngaytao']
  selected: any = {};
  constructor() { }

  ngOnInit(): void {
    if(this.dataSource.length >0){
      this.view(this.dataSource[0]);
    }
  }

  view(row){
    this.selected = row;
    this.ChangeFolder.emit(row.id_row);
  }
}
