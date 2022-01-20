import { CommentService } from './../../comment/comment.service';
import { SubheaderService } from './../../../../_metronic/jeework_old/core/_base/layout/services/subheader.service';
import {
    Component,
    OnInit,
    ElementRef,
    ViewChild,
    ChangeDetectionStrategy,
    ChangeDetectorRef,
    OnChanges, OnDestroy
} from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { tap, catchError, finalize, share, takeUntil } from 'rxjs/operators';
import {
    merge,
    BehaviorSubject,
    ReplaySubject,
    Subject,
    of,
} from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
// Services
import {
    LayoutUtilsService,
    MessageType,
} from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import {
    FormGroup,
    FormBuilder,
    Validators,
    FormControl,
} from '@angular/forms';
import { isFulfilled } from 'q';
import { DatePipe } from '@angular/common';
import {
    TopicModel,
    TopicUserModel,
} from '../Model/Topic.model';
import { JeeWorkLiteService } from '../../services/wework.services';
import { DiscussionsService } from '../discussions.service';
import { TopicEditComponent } from '../topic-edit/topic-edit.component';
import { AttachmentService } from '../../services/attachment.service';
import { AttachmentModel, FileUploadModel } from '../../projects-team/Model/department-and-project.model';

@Component({
    selector: 'kt-topic-view-detail',
    templateUrl: './topic-view-detail.component.html',
    changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ViewTopicDetailComponent implements OnInit, OnDestroy {
    // Table fields
    listNam: any[] = [];
    // Filter fields
    //Form
    customStyle: any = {};
    selectedItem: any = undefined;
    itemForm: FormGroup;
    loadingSubject = new BehaviorSubject<boolean>(false);
    loadingControl = new BehaviorSubject<boolean>(false);
    loading1$ = this.loadingSubject.asObservable();
    hasFormErrors: boolean = false;
    item: any;
    oldItem: TopicModel;
    item_User: TopicUserModel;
    item_file: FileUploadModel;
    selectedTab: number = 0;
    ID_NV: string = '';
    ListDSHopDong: any[] = [];
    //===============Khai báo value chi tiêt==================
    listNoiCapCMND: any[] = [];
    //========================================================
    Visible: boolean = false;
    viewLoading: boolean = false;
    loadingAfterSubmit: boolean = false;
    listUser: any[] = [];
    FormControls: FormGroup;
    disBtnSubmit: boolean = false;
    isChange: boolean = false;
    UserInfo: any = {};
    ItemData: any = {};
    Comment: string = '';
    MaxSize = 15;
    public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
    public bankFilterCtrl: FormControl = new FormControl();
    ListAttachFile: any[] = [];
    AttachFileComment: any[] = [];
    //===============Khai báo JEEcomment chi tiêt==================
    topicObjectID$: BehaviorSubject<string> = new BehaviorSubject<string>('');
    private readonly componentName = 'kt-topic_';
    private readonly onDestroy = new Subject<void>();

    //========================================================
    constructor(
        private _service: DiscussionsService,
        public dialog: MatDialog,
        public commentService: CommentService,
        private _attservice: AttachmentService,
        public subheaderService: SubheaderService,
        private layoutUtilsService: LayoutUtilsService,
        private changeDetectorRefs: ChangeDetectorRef,
        private translate: TranslateService,
        public datepipe: DatePipe,
        private activatedRoute: ActivatedRoute,
        public WeWorkService: JeeWorkLiteService,
        private router: Router
    ) {
    }

    /** LOAD DATA */
    ngOnInit() {
        this.loadingSubject.next(true);
        this.UserInfo = JSON.parse(localStorage.getItem('UserInfo'));
        const perModel = new TopicModel();
        perModel.clear();
        this.item = perModel;
        this.activatedRoute.params.subscribe((params) => {
            this.loadingSubject.next(false);
            this.ItemData.Id = params['id'];
            this.LoadData();
        });

    }

    LoadData() {
        const componentName = this.componentName + this.ItemData.Id;
        this._service.TopicDetail(this.ItemData.Id).subscribe((res) => {
            if (res && res.status == 1) {
                this.item = res.data;
                const filter: any = {};
                // filter.key = 'id_project_team';
                // filter.value = this.item.id_project_team;
                filter.id_project_team = this.item.id_project_team;
                this.WeWorkService.list_account(filter).subscribe((res) => {
                    this.changeDetectorRefs.detectChanges();
                    if (res && res.status === 1) {
                        this.listUser = res.data;
                        this.setUpDropSearchNhanVien();
                        this.changeDetectorRefs.detectChanges();
                    }
                });
                this.changeDetectorRefs.detectChanges();
            } else {
                this.item = Object.assign({}, this.oldItem);
                this.initProduct();
                this.layoutUtilsService.showActionNotification(
                    res.error.message,
                    MessageType.Read,
                    999999999,
                    true,
                    false,
                    3000,
                    'top',
                    0
                );
            }
        });
        this.WeWorkService.getTopicObjectIDByComponentName(componentName)
            .pipe(
                tap((res) => {
                    this.topicObjectID$.next('');
                    setTimeout(() => {
                        this.topicObjectID$.next(res);
                    }, 10);
                }),
                catchError((err) => {
                    return of();
                }),
                finalize(() => {
                }),
                share(),
                takeUntil(this.onDestroy)
            )
            .subscribe();
    }

    ngOnDestroy(): void {
        this.onDestroy.next();
    }

    DownloadFile(link) {
        window.open(link);
    }

    preview(link) {
        this.layoutUtilsService.ViewDoc(link);
    }

    download(path: any) {
        window.open(path);
    }

    Add_Followers(val: any) {
        this.layoutUtilsService.showWaitingDiv();
        this._service.Add_Followers(this.ItemData.Id, val).subscribe((res) => {
            this.layoutUtilsService.OffWaitingDiv();
            if (res && res.status == 1) {
                // this.ngOnInit();
                this.LoadData();
                this.changeDetectorRefs.detectChanges();
                this.layoutUtilsService.showActionNotification(
                    this.translate.instant('GeneralKey.capnhatthanhcong'),
                    MessageType.Read,
                    4000,
                    true,
                    false,
                    3000,
                    'top',
                    1
                );
            } else {
                this.layoutUtilsService.showActionNotification(
                    res.error.message,
                    MessageType.Read,
                    999999999,
                    true,
                    false,
                    3000,
                    'top',
                    0
                );
            }
        });
    }

    Delete_Followers(val: any) {
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant(
            'GeneralKey.bancochacchanmuonxoakhong'
        );
        const _waitDesciption = this.translate.instant(
            'GeneralKey.dulieudangduocxoa'
        );
        const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
        const dialogRef = this.layoutUtilsService.deleteElement(
            _title,
            _description,
            _waitDesciption
        );
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                return;
            }
            this.layoutUtilsService.showWaitingDiv();
            this._service.Delete_Followers(this.ItemData.Id, val).subscribe((res) => {
                this.layoutUtilsService.OffWaitingDiv();
                if (res && res.status === 1) {
                    // this.ngOnInit();
                    this.LoadData();
                    this.changeDetectorRefs.detectChanges();
                    this.layoutUtilsService.showActionNotification(
                        _deleteMessage,
                        MessageType.Delete,
                        4000,
                        true,
                        false
                    );
                } else {
                    this.layoutUtilsService.showActionNotification(
                        res.error.message,
                        MessageType.Read,
                        999999999,
                        true,
                        false,
                        3000,
                        'top',
                        0
                    );
                }
                //this.ngOnChanges();
            });
        });
    }

    favourite() {
        this.layoutUtilsService.showWaitingDiv();

        this._service.favouriteTopic(this.ItemData.Id).subscribe((res) => {
            this.layoutUtilsService.OffWaitingDiv();

            if (res && res.status == 1) {
                // this.ngOnInit();
                this.LoadData();
                this.changeDetectorRefs.detectChanges();
                this.layoutUtilsService.showActionNotification(
                    this.translate.instant('GeneralKey.capnhatthanhcong'),
                    MessageType.Read,
                    4000,
                    true,
                    false,
                    3000,
                    'top',
                    1
                );
            } else {
                this.layoutUtilsService.showActionNotification(
                    res.error.message,
                    MessageType.Read,
                    999999999,
                    true,
                    false,
                    3000,
                    'top',
                    0
                );
            }
        });
    }

    Update() {
        var _item = new TopicModel();
        let saveMessageTranslateParam = '';
        _item = this.item;
        saveMessageTranslateParam +=
            _item.id_row > 0
                ? 'GeneralKey.capnhatthanhcong'
                : 'GeneralKey.themthanhcong';
        const _saveMessage = this.translate.instant(saveMessageTranslateParam);
        const _messageType =
            _item.id_row > 0 ? MessageType.Update : MessageType.Create;
        const dialogRef = this.dialog.open(TopicEditComponent, { data: { _item } });
        dialogRef.afterClosed().subscribe((res) => {
            // this.ngOnInit();
            this.LoadData();
            if (res) {
                this.layoutUtilsService.showActionNotification(
                    _saveMessage,
                    _messageType,
                    4000,
                    true,
                    false
                );
                this.changeDetectorRefs.detectChanges();
            }
        });
    }

    setUpDropSearchNhanVien() {
        this.bankFilterCtrl.setValue('');
        this.filterBanks();
        this.bankFilterCtrl.valueChanges.pipe().subscribe(() => {
            this.filterBanks();
        });
    }

    protected filterBanks() {
        if (!this.listUser) {
            return;
        }
        let search = this.bankFilterCtrl.value;
        if (!search) {
            this.filteredBanks.next(this.listUser.slice());
            return;
        } else {
            search = search.toLowerCase();
        }
        // filter the banks
        this.filteredBanks.next(
            this.listUser.filter(
                (bank) => bank.hoten.toLowerCase().indexOf(search) > -1
            )
        );
    }

    initProduct() {
        this.loadingSubject.next(false);
        this.loadingControl.next(true);
    }

    onAlertClose($event) {
        this.hasFormErrors = false;
    }

    //---------------------------------------------------------

    f_number(value: any) {
        return Number((value + '').replace(/,/g, ''));
    }

    f_currency(value: any, args?: any): any {
        let nbr = Number((value + '').replace(/,|-/g, ''));
        return (nbr + '').replace(/(\d)(?=(\d{3})+(?!\d))/g, '$1,');
    }

    textPres(e: any, vi: any) {
        if (
            isNaN(e.key) &&
            //&& e.keyCode != 8 // backspace
            //&& e.keyCode != 46 // delete
            e.keyCode != 32 && // space
            e.keyCode != 189 &&
            e.keyCode != 45
        ) {
            // -
            e.preventDefault();
        }
    }

    checkDate(e: any, vi: any) {
        if (
            !(
                (e.keyCode > 95 && e.keyCode < 106) ||
                (e.keyCode > 46 && e.keyCode < 58) ||
                e.keyCode == 8
            )
        ) {
            e.preventDefault();
        }
    }

    checkValue(e: any) {
        if (
            !(
                (e.keyCode > 95 && e.keyCode < 106) ||
                (e.keyCode > 47 && e.keyCode < 58) ||
                e.keyCode == 8
            )
        ) {
            e.preventDefault();
        }
    }

    f_convertDate(v: any) {
        if (v != '' && v != null) {
            let a = new Date(v);
            return (
                a.getFullYear() +
                '-' +
                ('0' + (a.getMonth() + 1)).slice(-2) +
                '-' +
                ('0' + a.getDate()).slice(-2) +
                'T00:00:00.0000000'
            );
        }
    }

    f_date(value: any): any {
        if (value != '' && value != null && value != undefined) {
            let latest_date = this.datepipe.transform(value, 'dd/MM/yyyy');
            return latest_date;
        }
        return '';
    }

    DeleteFile_PDF(ind, ind1) {
        //this.ListAttachFile[ind].push({filename:filesAmount.name,StrBase64:base64Str});
        if (ind == -1) {
            this.AttachFileComment.splice(ind1, 1);
        } else {
            this.ListAttachFile[ind].splice(ind1, 1);
        }
    }

    Delete_File(val: any) {
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant(
            'GeneralKey.bancochacchanmuonxoakhong'
        );
        const _waitDesciption = this.translate.instant(
            'GeneralKey.dulieudangduocxoa'
        );
        const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
        const dialogRef = this.layoutUtilsService.deleteElement(
            _title,
            _description,
            _waitDesciption
        );
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                return;
            }
            this.layoutUtilsService.showWaitingDiv();
            this._attservice.delete_attachment(val).subscribe((res) => {
                this.layoutUtilsService.OffWaitingDiv();
                if (res && res.status === 1) {
                    // this.ngOnInit();
                    this.LoadData();
                    this.changeDetectorRefs.detectChanges();
                    this.layoutUtilsService.showActionNotification(
                        _deleteMessage,
                        MessageType.Delete,
                        4000,
                        true,
                        false
                    );
                } else {
                    this.layoutUtilsService.showActionNotification(
                        res.error.message,
                        MessageType.Read,
                        999999999,
                        true,
                        false,
                        3000,
                        'top',
                        0
                    );
                }
            });
        });
    }

    TenFile: string = '';
    File: string = '';
    filemodel: any;
    @ViewChild('csvInput', { static: true }) myInputVariable: ElementRef;

    save_file_Direct(evt: any) {
        if (evt.target.files && evt.target.files.length) {
            //Nếu có file 
            let file = evt.target.files[0]; // Ví dụ chỉ lấy file đầu tiên
            this.TenFile = file.name;
            let reader = new FileReader();
            reader.readAsDataURL(evt.target.files[0]);
            let base64Str;
            setTimeout(() => {
                base64Str = reader.result as String;
                var metaIdx = base64Str.indexOf(';base64,');
                base64Str = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64
                this.File = base64Str;
                const _model = new AttachmentModel();
                _model.object_type = 2;
                _model.object_id = this.item.id_row;
                const ct = new FileUploadModel();
                ct.strBase64 = this.File;
                ct.filename = this.TenFile;
                ct.IsAdd = true;
                // this.filemodel.push(ct);
                _model.item = ct;
                this.loadingAfterSubmit = true;
                this.viewLoading = true;
                this._attservice.Upload_attachment(_model).subscribe((res) => {
                    this.changeDetectorRefs.detectChanges();
                    if (res && res.status === 1) {
                        // this.ngOnInit();
                        this.LoadData();
                        const _messageType = this.translate.instant(
                            'GeneralKey.capnhatthanhcong'
                        );
                        this.layoutUtilsService
                            .showActionNotification(
                                _messageType,
                                MessageType.Update,
                                4000,
                                true,
                                false
                            )
                            .afterDismissed()
                            .subscribe((tt) => {
                            });
                    } else {
                        this.layoutUtilsService.showActionNotification(
                            res.error.message,
                            MessageType.Read,
                            9999999999,
                            true,
                            false,
                            3000,
                            'top',
                            0
                        );
                    }
                });
            }, 2000);
        } else {
            this.File = '';
        }
    }

    goToProject(item) {
        let _backUrl = ``;
        if (item == 'project') {
            _backUrl = `project/` + this.item.id_project_team;
        } else {
            _backUrl = `project/` + this.item.id_project_team + `/discussions`;
        }

        this.router.navigateByUrl(_backUrl);
    }

    back() {
        const res = this.router.url.split('/');
        res.splice(res.length - 1, 1);
        const backUrl = res.join('/');
        this.router.navigateByUrl(backUrl).then(() => {
            this._service.changeMessage(true);
        });
        // this.layoutUtilsService.showActionNotification("back");
        // window.history.back();
    }

    DeleteTopic() {
        const _title = this.translate.instant('GeneralKey.xoa');
        const _description = this.translate.instant(
            'GeneralKey.bancochacchanmuonxoakhong'
        );
        const _waitDesciption = this.translate.instant(
            'GeneralKey.dulieudangduocxoa'
        );
        const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');

        const dialogRef = this.layoutUtilsService.deleteElement(
            _title,
            _description,
            _waitDesciption
        );
        dialogRef.afterClosed().subscribe((res) => {
            if (!res) {
                return;
            }
            this.layoutUtilsService.showWaitingDiv();

            this._service.Delete_Topic(this.ItemData.Id).subscribe((res) => {
                this.layoutUtilsService.OffWaitingDiv();
                if (res && res.status === 1) {
                    this.layoutUtilsService.showActionNotification(
                        _deleteMessage,
                        MessageType.Delete,
                        4000,
                        true,
                        false,
                        3000,
                        'top',
                        1
                    );
                    this.back();
                } else {
                    this.layoutUtilsService.showActionNotification(
                        res.error.message,
                        MessageType.Read,
                        9999999999,
                        true,
                        false,
                        3000,
                        'top',
                        0
                    );
                }
                let _backUrl = `wework/discussions`;
                this.router.navigateByUrl(_backUrl);
                // window.location.reload();
            });
        });
    }
}
