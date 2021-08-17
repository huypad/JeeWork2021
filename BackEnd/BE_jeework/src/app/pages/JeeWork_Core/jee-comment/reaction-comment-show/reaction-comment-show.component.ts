import { switchMap } from 'rxjs/operators';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, Input, OnInit, ViewEncapsulation } from '@angular/core';
import { async, BehaviorSubject } from 'rxjs';
import { CommentDTO } from '../jee-comment.model';

@Component({
  selector: 'jeecomment-reaction-comment-show',
  templateUrl: 'reaction-comment-show.component.html',
  styleUrls: ['reaction-comment-show.scss'],
  encapsulation: ViewEncapsulation.None,
  changeDetection: ChangeDetectionStrategy.OnPush,
})

export class JeeCommentReactionShowComponent implements OnInit {

  showAngry: string = 'icon-reaction-show-angry';
  showSad: string = 'icon-reaction-show-sad';
  showWow: string = 'icon-reaction-show-wow';
  showHaha: string = 'icon-reaction-show-haha';
  showCare: string = 'icon-reaction-show-care';
  showLove: string = 'icon-reaction-show-love';
  showLike: string = 'icon-reaction-show-like';
  maxShow: number = 3;
  lstShowReaction: string[] = [];

  @Input() comment?: CommentDTO;
  @Input() isDeteachChange$?: BehaviorSubject<boolean>;

  constructor(private cd: ChangeDetectorRef) {
  }

  ngOnInit() {
    if (this.isDeteachChange$) {
      this.isDeteachChange$
        .pipe(
          switchMap(async (res) => {
            if (res) {
              this.cd.detectChanges();
              if (this.comment) this.showTypeReaction();
            }
          }))
        .subscribe();
    }
    if (this.comment) this.showTypeReaction();
  }

  showTypeReaction() {
    this.lstShowReaction = [];
    let index = 0;
    const mostTypeReaction = this.comment.MostTypeReaction;
    if (mostTypeReaction) {
      mostTypeReaction.forEach(element => {
        if (index === this.maxShow) return;
        const showReact = this.getShowReact(element);
        this.lstShowReaction.push(showReact);
        index++;
      });
    }
    this.lstShowReaction.sort();
    this.cd.detectChanges();
  }

  getShowReact(react: string): string {
    switch (react) {
      case 'Like':
        return this.showLike;
      case 'Love':
        return this.showLove;
      case 'Care':
        return this.showCare;
      case 'Haha':
        return this.showHaha;
      case 'Wow':
        return this.showWow;
      case 'Sad':
        return this.showSad;
      case 'Angry':
        return this.showAngry;
    }
  }
}