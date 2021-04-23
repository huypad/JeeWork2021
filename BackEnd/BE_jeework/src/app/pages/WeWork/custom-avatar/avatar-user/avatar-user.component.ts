import { WeWorkService } from './../../services/wework.services';
import { Component, OnInit, Input } from '@angular/core';

@Component({
  selector: 'kt-avatar-user',
  templateUrl: './avatar-user.component.html',
  styleUrls: ['./avatar-user.component.scss']
})
export class AvatarUserComponent implements OnInit {

  @Input() image:string = '';
  @Input() name:string = '';
  @Input() info:string = '';
  @Input() size:number = 25;
  @Input() textcolor:any = 'black';
  @Input() showFull:boolean = false;
  constructor(
    public WeWorkService:WeWorkService,
  ) { }

  ngOnInit() {
  }

}
