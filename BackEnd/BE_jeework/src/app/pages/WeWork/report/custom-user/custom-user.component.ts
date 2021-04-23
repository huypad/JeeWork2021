import { WeWorkService } from './../../services/wework.services';
import { Component, OnInit,Input } from '@angular/core';

@Component({
  selector: 'kt-custom-user',
  templateUrl: './custom-user.component.html',
})
export class CustomUserComponent implements OnInit {
  @Input() image:string = '';
  @Input() name:string = '';
  @Input() info:string = '';
  @Input() textcolor:any = undefined;
  constructor(
    public WeWorkService:WeWorkService,
  ) { }

  ngOnInit() {
  }

}
