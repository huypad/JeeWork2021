import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'kt-avatar-list-users',
  templateUrl: './avatar-list-users.component.html',
  styleUrls: ['./avatar-list-users.component.scss']
})
export class AvatarListUsersComponent implements OnInit {

  @Input() ListUser:any = [];
  @Input() MaxSize:number = 3;
  @Input() iconSize:number = 35;
  constructor() { }

  ngOnInit() {
  }

}
