import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'kt-report-list-tab',
  templateUrl: './report-list-tab.component.html',
})
export class ReportListTabComponent implements OnInit {

  activeLink='home';

  constructor() { }

  ngOnInit() {
  }

  click(activeLink) {
		this.activeLink = activeLink;
	}
  public Danhmuc =[
    {
      ten:'Dashboard',
      url:'/reports'
    },
    {
      ten:'Member',
      // url:'/reports'
      url:'/reports/member'
    }
  ];

}
