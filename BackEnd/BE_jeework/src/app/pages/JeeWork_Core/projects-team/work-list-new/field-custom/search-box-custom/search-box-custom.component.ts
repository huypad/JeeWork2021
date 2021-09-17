import {Component, EventEmitter, Input, OnInit, Output} from '@angular/core';

@Component({
  selector: 'app-search-box-custom',
  templateUrl: './search-box-custom.component.html',
  styleUrls: ['./search-box-custom.component.scss']
})
export class SearchBoxCustomComponent implements OnInit {

  @Input() keyword = '';
  @Input() placeholder = '';
  @Output() submitSearch = new EventEmitter<any>();
  constructor() { }

  ngOnInit(): void {
  }

  Submit(val){
    this.keyword = val;
    this.submitSearch.emit(val);
  }

}
