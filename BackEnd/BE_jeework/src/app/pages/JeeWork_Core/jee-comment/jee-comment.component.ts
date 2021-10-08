import { BehaviorSubject, of, Subject, interval } from "rxjs";
import {
  Component,
  Input,
  OnInit,
  ViewChild,
  ChangeDetectionStrategy,
  ChangeDetectorRef,
  ElementRef,
  OnChanges,
  Output,
  EventEmitter,
} from "@angular/core";
import { JeeCommentService } from "./jee-comment.service";
import { CdkTextareaAutosize } from "@angular/cdk/text-field";
import {
  catchError,
  finalize,
  takeUntil,
  tap,
  share,
  switchMap,
} from "rxjs/operators";
import {
  CommentDTO,
  QueryFilterComment,
  ReturnFilterComment,
  TopicCommentDTO,
  ChangeComment,
} from "./jee-comment.model";
import {JeeCommentSignalrService} from './jee-comment-signalr.service';

@Component({
  selector: "app-jee-comment",
  templateUrl: "./jee-comment.component.html",
  styleUrls: ["jee-comment.scss"],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class JeeCommentComponent implements OnInit {
  private readonly onDestroy = new Subject<void>();
  private _isLoading$ = new BehaviorSubject<boolean>(false);
  private _errorMessage$ = new BehaviorSubject<string>("");
  get isLoading$() {
    return this._isLoading$.asObservable();
  }
  get errorMessage$() {
    return this._errorMessage$.asObservable();
  }

  
  item: TopicCommentDTO;
  hiddenLike: boolean = true;
  hiddenShare: boolean = true;
  isFirstTime: boolean = true;
  ShowSpinner$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  ShowFilter$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  ShowSpinnerViewMore$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(
    false
  );

  //filter
  filterDate: Date = new Date();

  @Input() isDeteachChange$?: BehaviorSubject<boolean> =
    new BehaviorSubject<boolean>(false);
  @Input() objectID: string;
  @Input() showCommentDefault?: boolean;
  @Input() number: number;
  @Input() componentName: string;
  @Input() showonpopup: boolean = false;
  @Output() changeValue = new EventEmitter<any>();
  public lstObjectID: string[] = [];

  //demo
  @Input() img: any;
  @ViewChild("autosize") autosize: CdkTextareaAutosize;

  constructor(
    public service: JeeCommentService,
    public cd: ChangeDetectorRef,
    public signalrService: JeeCommentSignalrService,
    private elementRef: ElementRef
  ) {
    console.log('==================================================');
  }

  ngOnInit() {
    if (this.objectID) {
      this.lstObjectID.push(this.objectID);
      if (this.showCommentDefault) {
        setTimeout(() => {
          this.clickButtonComment();
        }, 500);
      }
    }
    else {
      this.LoadObjectID();
    }
  }
  hubConnectionShowChangeTopic() {
    if (this.objectID) {
      this.signalrService.connectToken(this.objectID);
    } else {
      this._errorMessage$.next('Topic comment là bắt buộc');
    }
  }
  clickButtonComment() {
    if (this.isFirstTime) {
      this.ShowSpinner$.next(true);
      if (this.objectID) {
        this.getShowTopic();
        this.hubConnectionShowChangeTopic();
        setTimeout(() => {
          this._isLoading$.next(true);
          this.signalrService.showChange$
              .pipe(
                  tap((result: ReturnFilterComment) => {
                    console.log(result,'result ================');
                    if (result) {
                      if (result.LstCreate.length > 0 || result.LstEdit.length > 0 || result.LstDelete.length > 0) {
                        if (result.LstCreate.length > 0) {
                          this.pushItemCommentInTopicComemnt(this.item, result.LstCreate);
                        }
                        if (result.LstEdit.length > 0) {
                          this.editItemCommentInTopicComemnt(this.item, result.LstEdit);
                        }
                        if (result.LstDelete.length > 0) {
                          this.deleteItemCommentInTopicComemnt(this.item, result.LstDelete);
                        }
                        this.filterDate = new Date();
                        this.isDeteachChange$.next(true);
                      }
                    }
                    this.cd.detectChanges();
                  }),
                  catchError((err) => {
                    this._isLoading$.next(false);
                    this.signalrService.disconnectToken(this.objectID);
                    this._errorMessage$.next(err);
                    return of();
                  }),
                  finalize(() => {
                    this._isLoading$.next(false);
                    this.cd.detectChanges();
                  }),
                  takeUntil(this.onDestroy)
              )
              .subscribe();
        }, 500);
      } else {
        this.ShowSpinner$.next(false);
        this.isFirstTime = false;
      }
    }
  }

  getShowTopic() {
    this._isLoading$.next(true);
    this.service
      .showTopicCommentByObjectID(this.objectID, this.filter())
      .pipe(
        tap((topic: TopicCommentDTO) => {
          console.log(topic,'topic',this.isFirstTime);
          if (this.isFirstTime) {
            this.item = topic;
          } else {
            this.pushItemIndex(this.item.Comments, topic.Comments);
            topic.TotalLengthComment = topic.TotalLengthComment;
          }
          if (topic.ViewLengthComment < topic.TotalLengthComment) {
            this.ShowFilter$.next(true);
          } else {
            this.ShowFilter$.next(false);
          }
        }),
        catchError((err) => {
          this._errorMessage$.next(err);
          return of();
        }),
        finalize(() => {
          if (this.isFirstTime) {
            this.ShowSpinner$.next(false);
            this.isFirstTime = false;
          }
          this._isLoading$.next(false);
          this.cd.detectChanges();
        }),
        takeUntil(this.onDestroy),
        share()
      )
      .subscribe();
  }

  getShowChangeTopic() {
    this.changeValue.emit(true);
    this._isLoading$.next(true);
    this.service
      .showChangeTopicCommentByObjectID(this.objectID, this.filter())
      .pipe(
        tap(async (result: ReturnFilterComment) => {
          if (
            result.LstCreate.length > 0 ||
            result.LstEdit.length > 0 ||
            result.LstDelete.length > 0
          ) {
            if (result.LstCreate.length > 0) {
              this.pushItemCommentInTopicComemnt(this.item, result.LstCreate);
            }
            if (result.LstEdit.length > 0) {
              this.editItemCommentInTopicComemnt(this.item, result.LstEdit);
            }
            if (result.LstDelete.length > 0) {
              this.deleteItemCommentInTopicComemnt(this.item, result.LstDelete);
            }
            this.filterDate = new Date();
            this.isDeteachChange$.next(true);
          }
        }),
        catchError((err) => {
          this._isLoading$.next(false);
          this._errorMessage$.next(err);
          return of();
        }),
        finalize(() => {
          this._isLoading$.next(false);
          this.cd.detectChanges();
        }),
        takeUntil(this.onDestroy),
        share()
      )
      .subscribe();
  }

  pushItemIndex(
    lstCommentDTO_current: CommentDTO[],
    lstCommentDTO_new: CommentDTO[]
  ) {
    lstCommentDTO_new.forEach((comment, pos) => {
      const index = lstCommentDTO_current.findIndex(
        (item) => item.Id === comment.Id
      );
      if (index === -1) {
        lstCommentDTO_current.splice(pos, 0, comment);
      }
    });
  }

  updateLengCreate(currentLength: number, lengthLstCreate: number) {
    currentLength = currentLength + lengthLstCreate;
  }

  pushItemCommentInTopicComemnt(
    topicComment: TopicCommentDTO,
    lstChange: ChangeComment[]
  ) {
    lstChange.forEach((element) => {
      this.pushItem(
        topicComment.Id,
        topicComment.Comments,
        element,
        topicComment.TotalLengthComment,
        topicComment.ViewLengthComment
      );
    });
  }

  pushItem(
    objectID_current: string,
    lstCommentDTO_current: CommentDTO[],
    changeComment: ChangeComment,
    totalLength: number,
    viewLength: number
  ) {
    if (objectID_current === changeComment.parentObjectID) {
      this.updateLengCreate(totalLength, changeComment.LstChange.length);
      this.updateLengCreate(viewLength, changeComment.LstChange.length);
      // changeComment.LstChange.forEach((comment) => {
      //   lstCommentDTO_current.push(comment);
      // });
      changeComment.LstChange.forEach((comment) => {
        const index = lstCommentDTO_current.findIndex((item) => item.Id === comment.Id);
        if (index === -1) {
          lstCommentDTO_current.push(comment);
        }
      });
    } else {
      lstCommentDTO_current.forEach((comment) => {
        this.pushItem(
          comment.Id,
          comment.Replies,
          changeComment,
          comment.TotalLengthComment,
          comment.ViewLengthComment
        );
      });
    }
  }

  editItemCommentInTopicComemnt(
    topicComment: TopicCommentDTO,
    lstChange: ChangeComment[]
  ) {
    lstChange.forEach((comment) => {
      this.editItem(topicComment.Id, topicComment.Comments, comment);
    });
  }

  editItem(
    objectID_current: string,
    lstCommentDTO_current: CommentDTO[],
    changeComment: ChangeComment
  ) {
    if (objectID_current === changeComment.parentObjectID) {
      changeComment.LstChange.forEach((comment) => {
        const index = lstCommentDTO_current.findIndex(
          (item) => item.Id === comment.Id
        );
        if (index !== -1) {
          this.copyComment(lstCommentDTO_current[index], comment);
        }
      });
    } else {
      lstCommentDTO_current.forEach((comment) => {
        this.editItem(comment.Id, comment.Replies, changeComment);
      });
    }
  }

  deleteItemCommentInTopicComemnt(
    topicComment: TopicCommentDTO,
    lstChange: ChangeComment[]
  ) {
    lstChange.forEach((comment) => {
      this.deleteItem(topicComment.Id, topicComment.Comments, comment);
    });
  }

  deleteItem(
    objectID_current: string,
    lstCommentDTO_current: CommentDTO[],
    changeComment: ChangeComment
  ) {
    if (objectID_current === changeComment.parentObjectID) {
      changeComment.LstChange.forEach((comment) => {
        const index = lstCommentDTO_current.findIndex(
          (item) => item.Id === comment.Id
        );
        if (index !== -1) {
          lstCommentDTO_current.splice(index, 1);
        }
      });
    } else {
      lstCommentDTO_current.forEach((comment) => {
        this.deleteItem(comment.Id, comment.Replies, changeComment);
      });
    }
  }

  copyComment(mainCommentDTO: CommentDTO, newCommentDTO: CommentDTO) {
    if (mainCommentDTO.Text !== newCommentDTO.Text)
      mainCommentDTO.Text = newCommentDTO.Text;
    if (mainCommentDTO.Attachs !== newCommentDTO.Attachs)
      mainCommentDTO.Attachs = newCommentDTO.Attachs;
    if (mainCommentDTO.IsEdit !== newCommentDTO.IsEdit)
      mainCommentDTO.IsEdit = newCommentDTO.IsEdit;
    if (mainCommentDTO.DateCreated !== newCommentDTO.DateCreated)
      mainCommentDTO.DateCreated = newCommentDTO.DateCreated;
    if (mainCommentDTO.IsUserReply !== newCommentDTO.IsUserReply)
      mainCommentDTO.IsUserReply = newCommentDTO.IsUserReply;
    if (mainCommentDTO.LengthReply !== newCommentDTO.LengthReply)
      mainCommentDTO.LengthReply = newCommentDTO.LengthReply;
    if (mainCommentDTO.MostLengthReaction !== newCommentDTO.MostLengthReaction)
      mainCommentDTO.MostLengthReaction = newCommentDTO.MostLengthReaction;
    if (mainCommentDTO.MostTypeReaction !== newCommentDTO.MostTypeReaction)
      mainCommentDTO.MostTypeReaction = newCommentDTO.MostTypeReaction;
    if (mainCommentDTO.TotalLengthComment !== newCommentDTO.TotalLengthComment)
      mainCommentDTO.TotalLengthComment = newCommentDTO.TotalLengthComment;
    if (
      mainCommentDTO.TotalLengthReaction !== newCommentDTO.TotalLengthReaction
    )
      mainCommentDTO.TotalLengthReaction = newCommentDTO.TotalLengthReaction;
    if (mainCommentDTO.UserReaction !== newCommentDTO.UserReaction)
      mainCommentDTO.UserReaction = newCommentDTO.UserReaction;
    if (mainCommentDTO.UserReactionColor !== newCommentDTO.UserReactionColor)
      mainCommentDTO.UserReactionColor = newCommentDTO.UserReactionColor;
    this.cd.detectChanges();
  }

  filter(): QueryFilterComment {
    let filter = new QueryFilterComment();
    filter.ViewLengthComment = this.item ? this.item.ViewLengthComment : 10;
    filter.Date = this.filterDate;
    return filter;
  }

  viewMoreComment() {
    this.item.ViewLengthComment += 10;
    this.getShowTopic();
    this.ShowSpinnerViewMore$.next(true);
    setTimeout(() => {
      this.ShowSpinnerViewMore$.next(false);
    }, 1000);
  }

  ngOnDestroy(): void {
    this.onDestroy.next();
    this.onDestroy.complete();
    this.signalrService.disconnectToken(this.objectID);
  }

  isScrolledViewElement() {
    const rect = this.elementRef.nativeElement.getBoundingClientRect();
    const isVisible = rect.top < window.innerHeight && rect.bottom >= 0;
    return isVisible;
  }

  LoadObjectID() {
    if(this.componentName){
      this.service
      .getTopicObjectIDByComponentName(this.componentName)
      .pipe(
        tap((res) => {
          this.objectID = res;
          this.ngOnInit();
        }),
        catchError((err) => {
          return of();
        }),
        finalize(() => {}),
        share()
      )
      .subscribe();
    }
    
  }

  GetValueComment(event){
    this.getShowChangeTopic();
    this.getShowTopic();
  }
}
