import { TokenStorage } from './../../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { LayoutUtilsService } from './../../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { TranslateService } from '@ngx-translate/core';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Inject, HostListener, Input, SimpleChange } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatPaginator,PageEvent } from '@angular/material/paginator';
import { MatSort } from '@angular/material/sort';
import { MatDialog,MatDialogRef,MAT_DIALOG_DATA } from '@angular/material/dialog';
import { SelectionModel } from '@angular/cdk/collections';
// Services
// Models 
import { WeWorkService } from '../../../services/wework.services';
import { PopoverContentComponent } from 'ngx-smart-popover';
@Component({
	selector: 'kt-add-users-dialog',
	templateUrl: './add-users-dialog.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush
})
export class AddUsersDialogComponent {
	ComponentTitle: string = '';
	listUser: any[] = [];
	options: any = {};
	@ViewChild('myPopover', { static: true }) myPopover: PopoverContentComponent;
	input: string = '';
	@ViewChild('hiddenText', { static: true }) textEl: ElementRef;
	@ViewChild('matInput', { static: true }) matInput: ElementRef;
	tempusername: string = '';
	selected: any[] = [];
	viewLoading:boolean=false;
	constructor(
		public dialogRef: MatDialogRef<AddUsersDialogComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		public weworkService: WeWorkService,
		public dialog: MatDialog,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private translate:TranslateService,
		private tokenStorage: TokenStorage,) {
	}

	ngOnInit() {
		this.ComponentTitle = this.data.title;
		this.listUser = [];
		this.options = this.getOptions();
		this.weworkService.list_account(this.data.filter).subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.listUser = res.data;
				// mảng idnv exclude
				if (this.data.excludes && this.data.excludes.length > 0) {
					var arr = this.data.excludes;
					this.listUser = this.listUser.filter(x => !arr.includes(x.id_nv));
				}
			}
			this.options = this.getOptions();
			this.changeDetectorRefs.detectChanges();
		});
	}
	getOptions() {
		var options: any = {
			showSearch: false,
			keyword: this.getKeyword(),
			data: this.listUser.filter(x => this.selected.findIndex(y => x.id_nv == y.id_nv) < 0),
		};
		return options;
	}
	getKeyword() {
		let i = this.input.lastIndexOf('@');
		if (i >= 0) {
			let temp = this.input.slice(i);
			if (temp.includes(' '))
				return '';
			return this.input.slice(i);
		}
		return '';
	}
	onSubmit() {
		if (this.selected.length == 0) {
			this.layoutUtilsService.showError(this.translate.instant('notify.vuilongchonthanhvien'));
			return;
		}
		this.dialogRef.close(this.selected.map(x => x.id_nv));
	}

	close() {
		this.dialogRef.close();
	}
	ItemSelected(data) {
		this.selected.push(data);
		let i = this.input.lastIndexOf('@');
		this.input = this.input.substr(0, i) + '@' + data.username + ' ';
		this.myPopover.hide();
		(<HTMLInputElement>this.matInput.nativeElement).focus();
		this.changeDetectorRefs.detectChanges();
	}
	click($event) {
		this.myPopover.hide();
	}
	onSearchChange($event) {
		if (this.selected.length > 0) {
			var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm
			var match = this.input.match(reg);
			if (match != null && match.length > 0) {
				let arr = match.map(x => x);
				this.selected = this.selected.filter(x => arr.includes('@' + x.username));
			} else {
				this.selected = [];
			}
		}
		this.options = this.getOptions();
		if (this.options.keyword) {
			let el = $event.currentTarget.offsetParent;
			var w = this.textEl.nativeElement.offsetWidth + 10;
			var h = -140
			this.myPopover.show();
			this.myPopover.top = el.offsetTop + h;
			this.myPopover.left = el.offsetLeft + w;
			this.changeDetectorRefs.detectChanges();
		}
	}

}
