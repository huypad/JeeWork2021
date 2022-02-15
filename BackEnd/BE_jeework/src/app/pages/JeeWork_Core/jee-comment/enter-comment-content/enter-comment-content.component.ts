import { CommentDTO, UserCommentInfo, TagComment } from './../jee-comment.model';
import { catchError, takeUntil, tap } from 'rxjs/operators';
import { of, Subject, BehaviorSubject, Observable } from 'rxjs';
import {
  ChangeDetectionStrategy,
  Component,
  Input,
  OnInit,
  ViewEncapsulation,
  ChangeDetectorRef,
  ElementRef,
  ViewChild,
  AfterViewInit,
  EventEmitter,
  Output,
} from '@angular/core';
import { JeeCommentService } from '../jee-comment.service';
import { PostCommentModel } from '../jee-comment.model';

@Component({
  selector: 'jeecomment-enter-comment-content',
  templateUrl: 'enter-comment-content.component.html',
  styleUrls: ['enter-comment-content.scss', '../jee-comment.scss'],
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class JeeCommentEnterCommentContentComponent implements OnInit, AfterViewInit {
  private readonly onDestroy = new Subject<void>();
  constructor(public service: JeeCommentService, public cd: ChangeDetectorRef) {}

  @Input('objectID') objectID: string = '';
  @Input('commentID') commentID: string = '';
  @Input('replyCommentID') replyCommentID: string = '';

  @Input('isEdit') set editing(isEdit: boolean) {
    this.isEdit$.next(isEdit);
  }
  private isEdit$ = new BehaviorSubject<boolean>(false);
  @Input('isFocus$') isFocus$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  @Input('editCommentModel') commentModelDto?: CommentDTO;
  @Output() valuecomment = new EventEmitter<string>();
  @Output('isEditEvent') isEditEvent = new EventEmitter<boolean>();

  showPopupEmoji: boolean;
  isClickIconEmoji: boolean;
  showSpanCancelFocus: boolean;
  showSpanCancelNoFocus: boolean;
  imagesUrl: string[];
  videosUrl: any[];
  filesUrl: any[];
  filesName: string[] = [];
  inputTextArea: string;

  subjectSreachTag = new BehaviorSubject<string>('');
  sreachTag$: Observable<string> = this.subjectSreachTag.asObservable();
  @ViewChild('txtarea') txtarea: ElementRef;

  //tag zone
  reg =
    /@\w*(\.[A-Za-záàạãảâẩấầẫậăặắằẳôốồộổỗõỏòóọủũụùủúỉìịỉíĩơởỡờớợđêểềểệễÁÀẠÃẢÂẨẤẪẬẨẦẪĂẶẲẴẶẮÔỐỒỘỔỖÕÒÓỌỎÚÙŨỦỤỦỈÌỊÍỈĨƠỞỠỢỚỜÊỂỀỆỄEẾĐ_ ]*$\w*)|\@[A-Za-záàạãảâẩấầẫậăặắằẳôốồộổỗõỏòóọủũụùủúỉìịỉíĩơởỡờớợđêểềểệễÁÀẠÃẢÂẨẤẪẬẨẦẪĂẶẲẴẶẮÔỐỒỘỔỖÕÒÓỌỎÚÙŨỦỤỦỈÌỊÍỈĨƠỞỠỢỚỜÊỂỀỆỄEẾĐ_ ]*$\w*/gm;

  @ViewChild('tagcommentshow') tagcommentshow: ElementRef;
  private _matchReg: string[] = [];
  private _posCursorInTextarea: number = 0;
  private _currentPosCursorInTextarea: number = 0;
  private _isLoading$: BehaviorSubject<boolean> = new BehaviorSubject<boolean>(false);
  private _lstTag: TagComment[] = [];

  get isLoading$() {
    return this._isLoading$.asObservable();
  }

  ngOnInit() {
    this.imagesUrl = [];
    this.videosUrl = [];
    this.filesUrl = [];
    this.inputTextArea = '';
    this.showPopupEmoji = false;
    this.isClickIconEmoji = false;
    this.showSpanCancelFocus = false;
    this.showSpanCancelNoFocus = false;
  }

  ngAfterViewInit(): void {
    if (this.commentModelDto) {
      this.initData();
    }
    this.isFocus$
      .pipe(
        tap((res) => {
          if (res) {
            this.FocusTextarea();
          }
        }),
        takeUntil(this.onDestroy)
      )
      .subscribe();

    this.hideCommentTag();
  }

  initData() {
    this.inputTextArea = this.commentModelDto.Text;
    this._currentPosCursorInTextarea = this.inputTextArea.length - 1;
    this._posCursorInTextarea = this.inputTextArea.length - 1;
    this.imagesUrl = this.commentModelDto.Attachs.Images;
    this.videosUrl = this.commentModelDto.Attachs.Videos;
    this.filesUrl = this.commentModelDto.Attachs.Files;
    this.commentModelDto.Attachs.Files.forEach((file) => {
      this.filesName.push(file.split('/')[file.split('/').length - 1]);
    });
    this.isEdit$
      .pipe(
        tap((res) => {
          this.isEditEvent.emit(res);
        }),
        takeUntil(this.onDestroy)
      )
      .subscribe();

    this.cd.detectChanges();
    this.isFocus$.next(true);
  }

  FocusTextarea() {
    this.txtarea.nativeElement.focus();
  }

  getContent($event) {
  }

  validateCommentAndPost() {
    if (!this._isLoading$.value) {
      const model = this.prepareComment();
      if (this.checkCommentIsEqualEmpty(model)) {
        return;
      }
      if (this.isEdit$.value) {
        this.updateComment(model);
        this.isEdit$.next(false);
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
    model.Tag = this.getTagFromInput(model.Text);
    model.Attachs.FileNames = this.filesName;
    this.imagesUrl.forEach((imageUrl) => {
      const base64 = imageUrl.split(',')[1];
      model.Attachs.Images.push(base64);
    });
    this.videosUrl.forEach((videoURL) => {
      const base64 = videoURL.split(',')[1];
      model.Attachs.Videos.push(base64);
    });
    this.filesUrl.forEach((fileUrl) => {
      const base64 = fileUrl.split(',')[1];
      model.Attachs.Files.push(base64);
    });
    return model;
  }

  getTagFromInput(input: string) {
    var lstTag = [];
    this._lstTag.forEach((element) => {
      if (input.search('@' + element.Display) >= 0) lstTag.push(element);
    });
    return lstTag;
  }

  checkCommentIsEqualEmpty(model: PostCommentModel): boolean {
    const empty = new PostCommentModel();
    return this.isEqual(model, empty);
  }

  isEqual(object: PostCommentModel, otherObject: PostCommentModel): boolean {
    let checkValue = object.Text === otherObject.Text;
    let checkList = false;
    if (
      object.Attachs.Files.length === otherObject.Attachs.Files.length &&
      object.Attachs.Images.length === otherObject.Attachs.Images.length &&
      object.Attachs.Videos.length === otherObject.Attachs.Videos.length
    )
      checkList = true;
    if (checkValue && checkList) return true;
    return false;
  }

  updateComment(model: PostCommentModel) {
    this._isLoading$.next(true);
    this.service
      .updateCommentModel(model)
      .pipe(
        tap(
          (res) => {},
          catchError((err) => {
            return of();
          }),
          () => {
            this.ngOnInit();
            this.cd.detectChanges();
            setTimeout(() => {
              this._isLoading$.next(false);
            }, 750);
          }
        ),
        takeUntil(this.onDestroy)
      )
      .subscribe();
  }
  Luulogcomment(model) {
    this.service.LuuLogcomment(model)
        .subscribe(() => {
            this.valuecomment.emit(model.id_topic);
        });
}

  postComment(model: PostCommentModel) {
    this._isLoading$.next(true);
    this.service
      .postCommentModel(model)
      .pipe(
        tap(
          (res) => {
            // TODO: viết api notify trong này
            const objSave: any = {};
            objSave.id_topic = res.Id;
            objSave.comment = model.Text ? model.Text : 'has comment';
            objSave.id_parent = 0;
            objSave.object_type = 0;
            objSave.object_id_new = model.TopicCommentID;
            // Đếm lại số lượng comment cho JeeWork
            this.Luulogcomment(objSave);
            this.service.notifyComment(model);
          },
          catchError((err) => {
            return of();
          }),
          () => {
            this.cancleComment();
            this.cd.detectChanges();
            setTimeout(() => {
              this._isLoading$.next(false);
            }, 750);
          }
        ),
        takeUntil(this.onDestroy)
      )
      .subscribe();
  }

  onKeydown($event) {
    // tag zone
    this._currentPosCursorInTextarea = $event.target.selectionStart;
    if (this._posCursorInTextarea > this._currentPosCursorInTextarea) this._posCursorInTextarea = this._currentPosCursorInTextarea;
    let input = this.inputTextArea.substr(this._posCursorInTextarea, this.inputTextArea.length - this._posCursorInTextarea);
    this._matchReg = input.match(this.reg);
    if (this._matchReg) {
      this.splitMatchAndSreachTagUser(this._matchReg);
      this.showCommentTag();
    } else {
      this.hideCommentTag();
    }
    //  check cancel keyword
    if (($event.ctrlKey && $event.keyCode == 13) || ($event.altKey && $event.keyCode == 13)) {
      this.inputTextArea = this.inputTextArea + '\n';
    } else if ($event.keyCode == 13) {
      $event.preventDefault();
    }
    this.focusFunction();
  }

  splitMatchAndSreachTagUser(match: string[]) {
    if (match != null && match.length > 0) {
      let tagValue = match[match.length - 1].split('@')[1];
      this.subjectSreachTag.next(tagValue);
    }
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
      const name = files[i].name;
      if (this.isImage(name)) {
        this.addImage(files.item(i));
      } else if (this.isVideo(name)) {
        this.addVideo(files.item(i));
      } else {
        this.filesName.push(name);
        this.addFile(files.item(i));
      }
    }
  }

  addFile(item) {
    let reader = new FileReader();
    reader.readAsDataURL(item);
    reader.onload = () => {
      this.filesUrl.push(reader.result as string);
      this.cd.detectChanges();
    };
  }

  addImage(item) {
    let reader = new FileReader();
    reader.readAsDataURL(item);
    reader.onload = () => {
      this.imagesUrl.push(reader.result as string);
      this.cd.detectChanges();
    };
  }

  addVideo(item) {
    let reader = new FileReader();
    reader.readAsDataURL(item);
    reader.onload = () => {
      this.videosUrl.push(reader.result);
      this.cd.detectChanges();
    };
  }

  getExtension(filename: string) {
    var parts = filename.split('.');
    return parts[parts.length - 1];
  }

  isImage(filename: string) {
    const ext = this.getExtension(filename);
    switch (ext.toLowerCase()) {
      case 'jpg':
      case 'gif':
      case 'bmp':
      case 'png':
      case 'heic':
      case 'heif':
        return true;
    }
    return false;
  }

  isVideo(filename: string) {
    var ext = this.getExtension(filename);
    switch (ext.toLowerCase()) {
      case 'm4v':
      case 'avi':
      case 'mpg':
      case 'mp4':
      case 'ts':
      case 'mkv':
      case 'webm':
      case 'wmv':
      case '3gpp':
      case 'mpeg':
      case 'ogv':
        return true;
    }
    return false;
  }

  deletePreviewImage(index) {
    this.imagesUrl.splice(index, 1);
    this.cd.detectChanges();
  }

  deletePreviewVideo(index) {
    this.videosUrl.splice(index, 1);
    this.cd.detectChanges();
  }
  deletePreviewFile(index) {
    this.filesUrl.splice(index, 1);
    this.filesName.splice(index, 1);
    this.cd.detectChanges();
  }

  cancleComment() {
    this.inputTextArea = '';
    this.imagesUrl = [];
    this.videosUrl = [];
    this.filesUrl = [];
    this.filesName = [];
    this.showSpanCancelFocus = false;
    this.showSpanCancelNoFocus = false;
    this.hideCommentTag();
    this.isEdit$.next(false);
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
    setTimeout(() => {
      this.hideCommentTag();
    }, 100);
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

  ItemSelected(user: UserCommentInfo) {
    this.hideCommentTag();
    let i = this.inputTextArea.lastIndexOf('@');
    this.inputTextArea = this.inputTextArea.substr(0, i) + '@' + user.FullName + ' ';
    this._posCursorInTextarea = this.inputTextArea.length;
    this.txtarea.nativeElement.focus();
    var tag = new TagComment();
    tag.Display = user.FullName;
    tag.Username = user.Username;
    this._lstTag.push(tag);
    this.cd.detectChanges();
  }

  showCommentTag() {
    this.tagcommentshow.nativeElement.style.display = 'block';
  }

  hideCommentTag() {
    this.tagcommentshow.nativeElement.style.display = 'none';
  }
}
