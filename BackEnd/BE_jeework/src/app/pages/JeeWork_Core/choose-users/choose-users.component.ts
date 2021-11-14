import { LayoutUtilsService } from './../../../_metronic/jeework_old/core/utils/layout-utils.service';
// Angular
import { Component, OnInit, ChangeDetectionStrategy, OnDestroy, ChangeDetectorRef, Inject, Input, Output, EventEmitter, ViewEncapsulation, OnChanges } from '@angular/core';
import { FormBuilder, FormGroup, Validators, FormControl } from '@angular/forms';
// Material
import { MatDialog } from '@angular/material/dialog';
// RxJS
import { ReplaySubject } from 'rxjs';
// NGRX
// Service
//Models

import { WeWorkService } from '../services/wework.services';

@Component({
	selector: 'kt-choose-users',
	templateUrl: './choose-users.component.html',
	changeDetection: ChangeDetectionStrategy.OnPush,
	encapsulation: ViewEncapsulation.None
})

export class ChooseUsersComponent implements OnInit, OnChanges {
	// Public properties
	@Input() options: any = {
		showSearch: true,//hiển thị search input hoặc truyền keyword
		keyword: '',
		data: []
	};
	@Input() isNewView = false;
	@Output() ItemSelected = new EventEmitter<any>();
	@Output() IsSearch = new EventEmitter<any>();

	listUser: any[] = [];
	customStyle: any = [];
	public filteredUsers: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public userFilterCtrl: FormControl = new FormControl();
	constructor(
		private FormControlFB: FormBuilder,
		public dialog: MatDialog,
		private layoutUtilsService: LayoutUtilsService,
		public weworkService: WeWorkService,
		private changeDetectorRefs: ChangeDetectorRef) { }

	/**
	 * On init
	 */
	ngOnInit() {
		this.userFilterCtrl.valueChanges
			.pipe()
			.subscribe(() => {
				this.filterUsers();
			});
	}
	ngOnChanges() {
		this.userFilterCtrl.setValue('');
		this.listUser = [];

		if (this.options.showSearch == undefined)
			this.options.showSearch = true;
		if (this.options != undefined) {
			if (this.options.data) {
				this.listUser = this.options.data;
				this.filterUsers();
				this.changeDetectorRefs.detectChanges();
			} else {
				// this.weworkService.list_account({}).subscribe(res => {
				// 	if (res && res.status === 1) {
				// 		this.listUser = res.data;
				// 		// mảng idnv exclude
				// 		if (this.options.excludes && this.options.excludes.length > 0) {
				// 			var arr = this.options.excludes;
				// 			this.listUser = this.listUser.filter(x => !arr.includes(x.id_nv));
				// 		}
				// 		this.filterUsers();
				// 		// this.changeDetectorRefs.detectChanges();
				// 	};
				// });
			}
		}
		if (!this.options.showSearch)
			this.filterUsers();

	}
	protected filterUsers() {
		if (!this.listUser) {
			return;
		}

		let search = !this.options.showSearch ? this.options.keyword : this.userFilterCtrl.value;
		if (!search) {
			this.filteredUsers.next(this.listUser.slice());
			return;
		} else {
			search = search.toLowerCase();
		}
		// filter the banks
		if (search[0] == '@') {
			this.filteredUsers.next(
				this.listUser.filter(bank => (bank.hoten.toLowerCase()).indexOf(search.replace('@', '')) > -1)
			);
		}
		else {
			this.filteredUsers.next(
				this.listUser.filter(bank => bank.hoten.toLowerCase().indexOf(search) > -1)
			);
		}
	}
	select(user) {
		this.ItemSelected.emit(user)
	}
	stopPropagation(event) {
		this.IsSearch.emit(event)
	}
}
