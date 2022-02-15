import { UserCommentInfo } from './../jee-comment.model';
import { JeeCommentService } from './../jee-comment.service';
import { Observable, Subscription } from 'rxjs';
import { ChangeDetectionStrategy, ChangeDetectorRef, Component, EventEmitter, Input, OnDestroy, OnInit, Output } from '@angular/core';

@Component({
  selector: 'tag-comment-show',
  templateUrl: 'tag-comment-show.component.html',
  styleUrls: ['tag-comment-show.component.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TagCommentShowComponent implements OnInit, OnDestroy {
  private subs: Subscription[] = [];

  @Input('sreach') sreach$: Observable<string>;

  @Output() ItemSelected = new EventEmitter<any>();

  listUser: UserCommentInfo[];

  constructor(public cd: ChangeDetectorRef, private service: JeeCommentService) {}

  ngOnDestroy(): void {
    this.subs.forEach((element) => {
      element.unsubscribe();
    });
  }

  ngOnInit() {
    this.listUser = this.service.lstUser;
    const sb = this.sreach$.subscribe((res) => {
      let input = res.toLowerCase();
      if (input) {
        const data = this.listUser.slice().filter((val) => val.FullName.toLowerCase().indexOf(res) >= 0);
        if (data.length > 0) {
          this.listUser = data;
        } else {
          this.listUser = this.service.lstUser.slice();
        }
      } else {
        this.listUser = this.service.lstUser.slice();
      }
      this.cd.detectChanges();
    });
    this.subs.push(sb);
  }

  select(user) {
    this.ItemSelected.emit(user);
  }
}
