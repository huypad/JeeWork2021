import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'kt-my-staff-new',
  templateUrl: './my-staff-new.component.html',
  styleUrls: ['./my-staff-new.component.scss']
})
export class MyStaffNewComponent implements OnInit {

  UserID : number = 0
  constructor() {
    this.UserID = +localStorage.getItem('idUser');
   }

  ngOnInit() {
  }

}
