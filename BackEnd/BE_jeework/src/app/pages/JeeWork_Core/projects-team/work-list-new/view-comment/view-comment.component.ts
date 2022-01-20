import { LayoutUtilsService } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { TokenStorage } from './../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { CommentEditDialogComponent } from './../../../comment/comment-edit-dialog/comment-edit-dialog.component';
import { EmotionDialogComponent } from './../../../emotion-dialog/emotion-dialog.component';
import { DomSanitizer } from '@angular/platform-browser';
import { JeeWorkLiteService } from './../../../services/wework.services';
import { TranslateService } from '@ngx-translate/core';
import { CommentService } from './../../../comment/comment.service';
import { PopoverContentComponent } from 'ngx-smart-popover';
import { MatDialog } from '@angular/material/dialog';
import { FormGroup, FormBuilder, FormControl } from '@angular/forms';
import { Subject, BehaviorSubject, Observable, Subscription } from 'rxjs';
import { Component, OnInit, Input, Output, EventEmitter, ViewChild, ChangeDetectorRef, ElementRef, SimpleChange } from '@angular/core';
import { GlobalVariable } from 'src/app/pages/global';


@Component({
	selector: 'kt-view-comment',
	templateUrl: './view-comment.component.html',
	styleUrls: ['./view-comment.component.scss']
  })
  export class ViewCommentComponent implements OnInit {
  	@Output() refesh: EventEmitter<any> = new EventEmitter<any>();//event for component
	@Input() Id?: number = 0;//Id của đối tượng
	@Input() View?: boolean = false;//view cho xem hay ko
	//@Input() ListChecked?: any[] = [];//tên node roof có chứa các con của node, default = children
	@Input() Loai?: number = 2;//1: work,2 topic,
	@Input() id_comment: number = 0;//1: work,2 topic,
	// nếu không cả Mã và Tên đều Emty thì nút xuất file word sẽ không xuất hiện

	listResult = new Subject();

	// Public properties
	ItemData: any = {};
	FormControls: FormGroup;
	hasFormErrors: boolean = false;
	disBtnSubmit: boolean = false;
	loadingSubject = new BehaviorSubject<boolean>(true);
	loading$: Observable<boolean>;
	viewLoading: boolean = false;
	isChange: boolean = false;
	isZoomSize: boolean = false;
	LstDanhMucKhac: any[] = [];
	public datatreeDonVi: BehaviorSubject<any[]> = new BehaviorSubject([]);
	private componentSubscriptions: Subscription;

	ListDonViCon: any[] = [];
	ListVanBan: any[] = [];
	datasource: any;

	ListAttachFile: any[] = [];
	ListYKien: any[] = [];
	AcceptInterval: boolean = true;
	NguoiNhan: string = '';
	//NguoiNhans:any[]=[{FullName:'người 1'},{FullName:'người 2'}];

	Comment: string = '';
	AttachFileComment: any[] = [];
	fileControl: FormControl;
	setting: any = {
		ACCEPT_DINHKEM: '',
		MAX_SIZE: 0
	};
	files: any = {};
	//reload: boolean = true;
	UserData: any = {};
	emotions: any = {};
	accounts: any = {};
	icons: any[] = [];
	item_choose: number;

	public anchors;
	//tag username
	@ViewChild('myPopoverC', { static: true }) myPopover: PopoverContentComponent;
	selected: any[] = [];
	selectedChild: any[] = [];
	listUser: any[] = [];
	options: any = {};
	@ViewChild('matInput', { static: true }) matInput: ElementRef;
	@ViewChild('hiddenText', { static: true }) textEl: ElementRef;
	CommentTemp: string = '';
	indexxxxx: number = -1;
	it: any = {};
	constructor(
		private FormControlFB: FormBuilder,
		public dialog: MatDialog,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private tokenStorage: TokenStorage,
		private service: CommentService,
		private translate: TranslateService,
		public weworkService: JeeWorkLiteService,
		private elementRef: ElementRef,
		private sanitized: DomSanitizer) { }
	transform(value) {
		return this.sanitized.bypassSecurityTrustHtml(value);
	}

	/**
	 * On init
	 */

	ngOnChanges(changes: SimpleChange) {
		if (changes['Id']) {
			this.ngOnInit();
		}
	}

	async ngOnInit() {

		this.emotions = GlobalVariable.emotions;
		this.accounts = GlobalVariable.accounts;
		this.icons = GlobalVariable.icons;
		this.options = this.getOptions();
		this.weworkService.list_account({}).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listUser = res.data;
			}
			this.options = this.getOptions();
			this.changeDetectorRefs.detectChanges();
		});
		this.tokenStorage.getUserData().subscribe(res => {
			this.UserData = res;
		})
		this.AcceptInterval = true;
		this.viewLoading = true;
		if (this.Id > 0) {
			this.getDSYKien();
			//setInterval(() => {
			//	if (this.AcceptInterval)
			//		this.getDSYKien_Interval();
			//}, 500);
		}
	}
	ngAfterViewInit() {
		this.anchors = this.elementRef.nativeElement.querySelectorAll('.inline-tag');
		// this.anchors.forEach((anchor: HTMLAnchorElement) => {
		// 	anchor.addEventListener('click', this.clickOnUser)
		// })
	}

	getDSYKien() {
		this.service.getDSYKien(this.Id, this.Loai).subscribe(res => {
			if (res && res.status == 1) {
        
				this.ListYKien = res.data.filter(x=>x.id_row == this.id_comment);
				this.createForm();
				this.changeDetectorRefs.detectChanges();
			}
		});
	}

	


	CheckedChange(p: any, e: any) {
		p.check = e;
	}


	DateChanged(value: any, ind: number) {
		if (ind == 1) {
			let batdau = value.targetElement.value.replace(/-/g, '/').split('T')[0].split('/');
			if (+batdau[0] < 10 && batdau[0].length < 2)
				batdau[0] = '0' + batdau[0];
			if (+batdau[1] < 10 && batdau[1].length < 2)
				batdau[1] = '0' + batdau[1];

			this.FormControls.controls['bDNghi'].setValue(batdau[2] + '-' + batdau[1] + '-' + batdau[0]);
		}
		if (ind == 2) {
			let ketthuc = value.targetElement.value.replace(/-/g, '/').split('T')[0].split('/');
			if (+ketthuc[0] < 10 && ketthuc[0].length < 2)
				ketthuc[0] = '0' + ketthuc[0];
			if (+ketthuc[1] < 10 && ketthuc[1].length < 2)
				ketthuc[1] = '0' + ketthuc[1];

			this.FormControls.controls['kTNghi'].setValue(ketthuc[2] + '-' + ketthuc[1] + '-' + ketthuc[0]);
		}
	}

	/**
	 * On destroy
	 */
	ngOnDestroy() {
		if (this.componentSubscriptions) {
			this.componentSubscriptions.unsubscribe();
		}

		// if (this.interval) {
		// 	clearInterval(this.interval);
		// }

		this.AcceptInterval = false;
	}

	/**
	 * Create form
	 */
	createForm() {
		// this.FormControls = this.FormControlFB.group({
		// });

		// for (var i = 0; i < this.ListYKien.length; i++) {
		// 	//this.itemForm.addControl(this.Listbiuldview[i].BuildEditor, new FormControl('', Validators.required));
		// 	this.FormControls.addControl("FctAttachFile" + i, new FormControl([{filename:'Khoatest.docx',StrBase64:'1234553543534'}]));
		// }


		for (var i = 0; i < this.ListYKien.length; i++) {
			this.ListAttachFile.push([])
			//{filename:'Khoatest.docx',StrBase64:'1234553543534'}
			//this.ListAttachFile['FctAttachFile'+i]={filename:'Khoatest.docx',StrBase64:'1234553543534'};
		}
	}

	GetListAttach(ind: number): any {
		return this.ListAttachFile[ind];
	}

	/**
	 * Check control is invalid
	 * @param controlName: string
	 */
	isControlInvalid(controlName: string): boolean {
		const control = this.FormControls.controls[controlName];
		const result = control.invalid && control.touched;
		return result;
	}

	/**
	 * Save data
	 *
	 * @param withBack: boolean
	 */
	onSubmit(type: boolean) {
		let ArrDVC: any[] = [];
		for (var i = 0; i < this.ListDonViCon.length; i++) {
			if (this.ListDonViCon[i].check) {
				ArrDVC.push(this.ListDonViCon[i]);
			}
		}
		if (type) {
			//this.dialogRef.close(ArrDVC);
		}
		else {
			//this.dialogRef.close();
		}
	}

	ShowOrHideComment(ind: number) {
    
		var x = document.getElementById("ykchild" + ind);
		//var a = document.getElementById("btnHideyk" + ind);
		//var b = document.getElementById("btnShowyk" + ind);
		if (!x.style.display || x.style.display === "none") {
			x.style.display = "block";
			//a.style.display = "block";
			//b.style.display = "none";
		} else {
			x.style.display = "none";
			//a.style.display = "none";
			//b.style.display = "block";
		}
	}


	//type=1: comment, type=2: reply
	CommentInsert(e: any, Parent: number, ind: number, type: number) {
		var objSave: any = {};
		objSave.comment = e;
		objSave.id_parent = Parent;
		objSave.object_type = this.Loai;
		objSave.object_id = this.Id;
		if (type == 1) {
			objSave.Attachments = this.AttachFileComment;
			objSave.Users = this.getListTagUser(e,this.selected);
		}
		else {
			objSave.Attachments = this.ListAttachFile[ind];
			objSave.Users = this.getListTagUser(e,this.selectedChild);
		}
		this.service.getDSYKienInsert(objSave).subscribe(res => {
			if (type == 1) { this.Comment = ''; this.AttachFileComment = []; }
			else {
				(<HTMLInputElement>document.getElementById("CommentRep" + ind)).value = "";
				this.ListAttachFile[ind] = [];
			}
			this.ngOnInit();
			this.refesh.emit(true);
			// if (Parent == 0) {
			// 	this.ListYKien.unshift(res.data);
			// } else {
			// 	this.ListYKien[ind].Children.unshift(res.data);
			// }
			this.changeDetectorRefs.detectChanges();
		});
	}

	getListTagUser(chuoi, array) {
		var arr = [];
		var user = []
		chuoi.split(' ').forEach(element => {
			if (element[0] == '@') {
				user.push(element);
			}
		});;
		user = Array.from(new Set(user));
		user.forEach(element => {
			var x = array.find(x => x.username == element.substr(1));
			if (x) {
				arr.push(x)
			}
		})
		return arr;

	}

	selectFile_PDF(ind) {
		if (ind == -1) {
			let f = document.getElementById("PDFInpdd");
			f.click();
		}
		else {
			let f = document.getElementById("PDFInpdd" + ind);
			f.click();
		}

	}
	onSelectFile_PDF(event, ind) {
		// event.target.type='text';
		// event.target.type='file';
		if (event.target.files && event.target.files[0]) {
			var filesAmount = event.target.files[0];
			var Strfilename = filesAmount.name.split('.');
			// if (Strfilename[Strfilename.length - 1] != 'docx' && Strfilename[Strfilename.length - 1] != 'doc') {
			// 	this.layoutUtilsService.showInfo("File không đúng định dạng");
			// 	return;
			// }
			if (ind == -1) {
				for (var i = 0; i < this.AttachFileComment.length; i++) {
					if (filesAmount.name == this.AttachFileComment[i].filename) {
						this.layoutUtilsService.showInfo(this.translate.instant('notify.filedatontai'));
						return;
					}
				}
			}
			else {
				for (var i = 0; i < this.ListAttachFile[ind].length; i++) {
					if (filesAmount.name == this.ListAttachFile[ind][i].filename) {
						this.layoutUtilsService.showInfo(this.translate.instant('notify.filedatontai'));
						return;
					}
				}
			}

			event.target.type = 'text';
			event.target.type = 'file';
			var reader = new FileReader();
			//this.FileAttachName = filesAmount.name;
			let base64Str: any;
			reader.onload = (event) => {
				base64Str = event.target["result"]
				var metaIdx = base64Str.indexOf(';base64,');
				base64Str = base64Str.substr(metaIdx + 8); // Cắt meta data khỏi chuỗi base64

				//this.FileAttachStrBase64 = base64Str;
				if (ind == -1) {
					this.AttachFileComment.push({ filename: filesAmount.name, strBase64: base64Str });
					this.changeDetectorRefs.detectChanges();
				}
				else {
					this.ListAttachFile[ind].push({ filename: filesAmount.name, strBase64: base64Str });
					this.changeDetectorRefs.detectChanges();
				}
			}

			reader.readAsDataURL(filesAmount);

		}
	}
	DeleteFile_PDF(ind, ind1) {
		//this.ListAttachFile[ind].push({filename:filesAmount.name,StrBase64:base64Str});
		if (ind == -1) {
			this.AttachFileComment.splice(ind1, 1);
		}
		else {
			this.ListAttachFile[ind].splice(ind1, 1);
		}
	}


	DownloadFile(link) {
		window.open(link);
	}
	preview(link) {
		this.layoutUtilsService.showInfo("preview:" + link);
	}
	reply(item, index) {
		var ele = (<HTMLInputElement>document.getElementById("CommentRep" + index));
		ele.value = "@" + item.NguoiTao.username + " ";
		ele.focus();
	}
	openEmotionDialog(ind, id_p) {
		const dialogRef = this.dialog.open(EmotionDialogComponent, { data: {}, width: '500px' });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.CommentInsert(res, id_p, ind, 2);
			}
		});
	}

	parseHtml(str) {
		var html = str;
		var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm
		var reg1 = /\:[A-Za-z]\w*\:/gm
		var match = html.match(reg);
		if (match != null) {
			for (var i = 0; i < match.length; i++) {
				var key = match[i] + '';
				var username = key.slice(1);
				if (this.accounts[key]) {
					var re = `<span class="url inline-tag" data-username="${username}">${this.accounts[key]}</span>`;
					html = html.replace(key, re);
				}
			}
		}
		match = html.match(reg1);
		if (match != null) {
			for (var i = 0; i < match.length; i++) {
				var key = match[i] + '';
				if (this.emotions[key]) {
					var re = `<img src="${this.emotions[key]}" />`;
					html = html.replace(key, re);
				}
			}
		}
		setTimeout(() => {
			this.ngAfterViewInit();
		}, 10)
		//return html;
		return this.sanitized.bypassSecurityTrustHtml(html)
	}
	like(item, icon) {
		this.service.like(item.id_row, icon).subscribe(res => {
			if (res && res.status == 1) {
				item.Like = res.data.Like;
				item.Likes = res.data.Likes;
				this.changeDetectorRefs.detectChanges();
			}
		})
	}
	remove(item, index, indexc = -1) {
		this.service.remove(item.id_row).subscribe(res => {
			if (res && res.status == 1) {
				if (indexc >= 0)//xoa con
					this.ListYKien[index].Children.splice(indexc, 1);
				else
					this.ListYKien.splice(index, 1);
				this.changeDetectorRefs.detectChanges();
			}
		})
	}

	initUpdate(item, index, indexc = -1) {
		var data = Object.assign({}, item);
		const dialogRef = this.dialog.open(CommentEditDialogComponent, { data: data, width: '500px' });
		dialogRef.afterClosed().subscribe(res => {
			if (res) {
				item.comment = res.comment
				this.changeDetectorRefs.detectChanges();
			}
		});
	}
	//#region tag username
	getOptions() {
		var options: any = {
			showSearch: false,
			keyword: this.getKeyword(),
			data: this.listUser.filter(x => this.selected.findIndex(y => x.id_nv == y.id_nv) < 0),
		};
		return options;
	}
	getKeyword() {
		let i = this.CommentTemp.lastIndexOf('@');
		if (i >= 0) {
			let temp = this.CommentTemp.slice(i);
			if (temp.includes(' '))
				return '';
			return this.CommentTemp.slice(i);
		}
		return '';
	}
	ItemSelected(data) {
		this.selected.push(data);
		let i = this.CommentTemp.lastIndexOf('@');
		this.CommentTemp = this.CommentTemp.substr(0, i) + '@' + data.username + ' ';
		this.myPopover.hide();
		let ele = (<HTMLInputElement>this.matInput.nativeElement);
		if (this.indexxxxx >= 0)
			ele = (<HTMLInputElement>document.getElementById("CommentRep" + this.indexxxxx));
		ele.value = this.CommentTemp;
		ele.focus();
		this.changeDetectorRefs.detectChanges();
	}



	click($event, vi = -1) {
		this.myPopover.hide();
	}
	onSearchChange($event, vi = -1) {
		if (vi >= 0)
			this.CommentTemp = (<HTMLInputElement>document.getElementById("CommentRep" + vi)).value;
		else
			this.CommentTemp = this.Comment;

		if (this.selected.length > 0) {
			var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm
			var match = this.CommentTemp.match(reg);
			if (match != null && match.length > 0) {
				let arr = match.map(x => x);
				this.selected = this.selected.filter(x => arr.includes('@' + x.username));
			} else {
				this.selected = [];
			}
		}
		this.options = this.getOptions();
		if (this.options.keyword) {
			let el = $event.currentTarget;
			let rect = el.getBoundingClientRect();
			var w = this.textEl.nativeElement.offsetWidth + 25;
			var h = 0;
			this.myPopover.show();
			this.myPopover.top = el.offsetTop + h;
			this.myPopover.left = el.offsetLeft + w;
			//this.myPopover.top = rect.y + h;
			//this.myPopover.left = w ;
			this.changeDetectorRefs.detectChanges();
		}else{
			this.myPopover.hide();
		}
	}
	//#endregion
}
