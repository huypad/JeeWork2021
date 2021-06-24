import { ActivatedRoute } from '@angular/router';
import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-user-work',
  templateUrl: './user-work.component.html',
  styleUrls: ['./user-work.component.scss']
})
export class UserWorkComponent implements OnInit {
	UserID: number;
  constructor(
		private activatedRoute: ActivatedRoute,
  ) { }

  ngOnInit(): void {
    this.activatedRoute.params.subscribe(res => {
			this.UserID = res.idu;
		});
  }

}
