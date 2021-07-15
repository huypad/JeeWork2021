import { catchError, takeUntil, tap } from 'rxjs/operators';
import { of, Subject, BehaviorSubject } from 'rxjs';
import { ChangeDetectionStrategy, Component, Input, OnInit, ViewEncapsulation, ChangeDetectorRef, ElementRef, ViewChild } from '@angular/core';
import { JeeCommentService } from '../jee-comment.service';
import { PostCommentModel } from '../jee-comment.model';


@Component({
  selector: 'jeecomment-enter-comment-content',
  templateUrl: 'enter-comment-content.component.html',
  styleUrls: ['enter-comment-content.scss', '../jee-comment.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None
})

export class JeeCommentEnterCommentContentComponent implements OnInit {
  private readonly onDestroy = new Subject<void>();
  constructor(public service: JeeCommentService, public cd: ChangeDetectorRef) { }

  @Input() objectID: string = '';
  @Input() commentID: string = '';
  @Input() replyCommentID: string = '';
  @Input() isEdit?: boolean = false;
  @Input() isFocus$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);

  showPopupEmoji: boolean;
  isClickIconEmoji: boolean;
  showSpanCancelFocus: boolean;
  showSpanCancelNoFocus: boolean;
  imagesUrl: string[];
  imagesUrlArray: any[];
  inputTextArea: string;

  private _isLoading$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  get isLoading$() { return this._isLoading$.asObservable(); }

  ngOnInit() {
    this.isFocus$
      .pipe(
        tap((res) => {
          if (res) {
            this.FocusTextarea();
          }
        }),
        takeUntil(this.onDestroy),
      ).subscribe();

    this.imagesUrl = [];
    this.imagesUrlArray = [];
    this.inputTextArea = '';
    this.showPopupEmoji = false;
    this.isClickIconEmoji = false;
    this.showSpanCancelFocus = false;
    this.showSpanCancelNoFocus = false;
  }

  @ViewChild('txtarea') element: ElementRef;
  FocusTextarea() {
    this.element.nativeElement.focus();
  }

  validateCommentAndPost() {
    if (!this._isLoading$.value) {
      const model = this.prepareComment();
      if (this.checkCommentIsEqualEmpty(model)) {
        return;
      }
      if (this.isEdit) {
        this.updateComment(model);
      } else {
        this.postComment(model);
      }
    }
  }

  prepareComment(): PostCommentModel {
    const model = new PostCommentModel();
    model.TopicCommentID = this.objectID ? this.objectID : '';
    model.CommentID = this.commentID ? this.commentID : '';
    model.ReplyCommentID = this.replyCommentID ? this.replyCommentID : '';
    model.Text = this.inputTextArea;
    model.Attachs.Images = [];
    this.imagesUrl.forEach((imageUrl) => {
      const base64 = imageUrl.split(',')[1];
      model.Attachs.Images.push(base64);
    })
    return model;
  }

  checkCommentIsEqualEmpty(model: PostCommentModel): boolean {
    const empty = new PostCommentModel();
    return this.isEqual(model, empty);
  }

  isEqual(object: PostCommentModel, otherObject: PostCommentModel): boolean {
    let checkValue = object.Text === otherObject.Text;
    let checkList = false;
    if (object.Attachs.Files.length === otherObject.Attachs.Files.length &&
      object.Attachs.Images.length === otherObject.Attachs.Images.length &&
      object.Attachs.Videos.length === otherObject.Attachs.Videos.length)
      checkList = true;

    if (checkValue && checkList) return true;
    return false;
  }

  updateComment(model: PostCommentModel) {
    this.service.postCommentModel(model)
      .subscribe();
  }

  postComment(model: PostCommentModel) {
    this._isLoading$.next(true);
    this.service.postCommentModel(model).
      pipe(
        tap(
          (res) => { },
          catchError((err) => { console.log(err); return of() }),
          () => {
            this.ngOnInit();
            this.cd.detectChanges();
            setTimeout(() => {
              this._isLoading$.next(false);
            }, 750);
          }),
        takeUntil(this.onDestroy),
      ).subscribe();
  }

  onKeydown($event) {
    if (($event.ctrlKey && $event.keyCode == 13) || ($event.altKey && $event.keyCode == 13)) {
      this.inputTextArea = this.inputTextArea + '\n';
    } else if ($event.keyCode == 13) {
      $event.preventDefault();
    }
    this.focusFunction();
  }

  toggleEmojiPicker() {
    this.showPopupEmoji = true;
    this.isClickIconEmoji = true;
  }

  addEmoji(event) {
    const data = this.inputTextArea + `${event.emoji.native}`;
    this.inputTextArea = data;
    this.showPopupEmoji = false;
  }

  previewFileInput(files: FileList) {
    const filesAmount = files.length;
    if (filesAmount > 0) this.showSpanCancelNoFocus = true;
    for (let i = 0; i < filesAmount; i++) {
      let reader = new FileReader();
      reader.readAsDataURL(files.item(i));
      reader.onload = () => {
        this.imagesUrl.push(reader.result as string);
        this.cd.detectChanges();
      }
    }
  }

  deletePreviewImage(index) {
    this.imagesUrl.splice(index, 1);
    this.cd.detectChanges();
  }

  cancleComment() {
    this.inputTextArea = '';
    this.imagesUrl = [];
    this.showSpanCancelFocus = false;
    this.showSpanCancelNoFocus = false;
    this.cd.detectChanges();
  }

  focusFunction() {
    if (this.checkValueExistCommentModel()) {
      this.showSpanCancelFocus = true;
      this.showSpanCancelNoFocus = false;
    } else {
      this.showSpanCancelFocus = false;
    }
  }

  focusOutFunction() {
    if (this.checkValueExistCommentModel()) {
      this.showSpanCancelFocus = false;
      this.showSpanCancelNoFocus = true;
    } else {
      this.showSpanCancelNoFocus = false;
    }
  }

  checkValueExistCommentModel(): boolean {
    if (this.inputTextArea.length > 0 || this.imagesUrl.length > 1) {
      return true;
    }
    return false;
  }

  clickOutSideEmoji() {
    if (this.showPopupEmoji && this.isClickIconEmoji) {
      this.showPopupEmoji = true;
      this.isClickIconEmoji = false;
    } else {
      this.showPopupEmoji = false;
      this.isClickIconEmoji = false;
    }
  }


}