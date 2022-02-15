import { animate, style, transition, trigger } from '@angular/animations';
import { ChangeDetectionStrategy, Component, Input, OnInit } from '@angular/core';

@Component({
  selector: 'bottom-info-reaction-comment-show',
  templateUrl: 'bottom-info-reaction-comment-show.component.html',
  styleUrls: ['bottom-info-reaction-comment-show.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  animations: [
    trigger('tooltip', [
      transition(':enter', [style({ opacity: 0 }), animate(300, style({ opacity: 1 }))]),
      transition(':leave', [animate(300, style({ opacity: 0 }))]),
    ]),
  ],
})
export class BottomInfoReactionCommentShowComponent implements OnInit {
  @Input() text = '';

  constructor() {}

  ngOnInit() {}
}
