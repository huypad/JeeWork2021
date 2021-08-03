import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { Component, OnInit, Inject, ChangeDetectionStrategy, HostListener, ViewChild, ElementRef, ChangeDetectorRef } from '@angular/core';
import { MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { FormBuilder, FormGroup, Validators, FormControl, AbstractControl } from '@angular/forms';
import { TranslateService } from '@ngx-translate/core';
import { ReplaySubject, BehaviorSubject, Observable } from 'rxjs';
import { Router } from '@angular/router';
import { WeWorkService } from '../../services/wework.services';
import { useAnimation } from '@angular/animations';
import { FilterModel, FilterDetailModel } from '../filter.model';
import { filterService } from '../filter.service';
import { DatePipe } from '@angular/common';

@Component({
	selector: 'kt-filter-edit',
	templateUrl: './filter-edit.component.html',
})
export class filterEditComponent implements OnInit {
	oldItem: FilterModel;
	item: FilterModel;
	itemForm: FormGroup;
	hasFormErrors: boolean = false;
	viewLoading: boolean = false;
	loadingAfterSubmit: boolean = false;
	disabledBtn: boolean = false;
	show_operators: boolean = false;
	show_option_1: boolean = false;
	show_option_2: boolean = false;
	show_option_3: boolean = false;
	list_filter_key: any[] = [];
	list_options: any[] = [];
	list_operators: any[] = [];
	inputdata: string = '';
	listColumn: any[] = [];
	listCaLamViec: any[] = [];
	filter_key: string = '';
	filter_operators: string = '';
	filter_options: string = '';
	showColumn: boolean = false;
	listChiTiet: any[] = [];
	datadetail: any[] = [];
	list_data_old: any[] = [];
	constructor(public dialogRef: MatDialogRef<filterEditComponent>,
		@Inject(MAT_DIALOG_DATA) public data: any,
		private fb: FormBuilder,
		private changeDetectorRefs: ChangeDetectorRef,
		private _service: filterService,
		private layoutUtilsService: LayoutUtilsService,
		private translate: TranslateService,
		public weworkService: WeWorkService,
		public datepipe: DatePipe,
		private router: Router,) { }
	/** LOAD DATA */
	ngOnInit() {
		this.item = new FilterModel();
		this.item.clear();
		this.item = this.data._item;
		this._service.Get_list_filterkey().subscribe(res => {
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				this.list_filter_key = res.data;
				this.changeDetectorRefs.detectChanges();
			};
		});
		if (this.item.id_row > 0) {
			this._service.Detail(this.item.id_row).subscribe(res => {
				if (res && res.status === 1) {
					this.item = res.data;
					this.datadetail = res.data.details;
					this.listColumn = [];
					if (this.datadetail.length > 0) {
						setTimeout(() => {
							this.datadetail.map((item, index) => {
								this.showColumn = false;
								let title = this.list_filter_key.find(x => x.id_row == item.id_key);
								if (title == null || title == undefined) {
									return '';
								}
								else {
									// this.listColumn[index].StrTitle = title.title + ' ' + item.operator + ' ' + item.value;
									this.listColumn.push({
										StrTitle: title.title + ' ' + item.operator + ' ' + item.value,
										getTitleCol: title.title + ' ' + this.codeReplace(item.operator) + ' ' + this.getNamevalue(title.options,item.value),
										title: title.title,
										operator: item.operator,
										value: item.value,
										id_row: item.id_row
									});
									this.list_data_old.push({
										title: title.title,
										operator: item.operator,
										value: item.value,
										id_row: item.id_row
									});
									this.changeDetectorRefs.detectChanges();
								}
							});
						}, 100);
					}
					this.createForm();
				};
			});
			this.viewLoading = true;
		}
		else {
			// this.themcot();
			this.viewLoading = false;
		}
		this.createForm();
	}
 
	getNamevalue(item,id){
		if(item){
			var x = item.find(x => x.id == id);
			if(x){
				return x.title;
			}
		}
		return id;
	}
	createForm() {
		this.itemForm = this.fb.group({
			title: ['' + this.item.title, Validators.required],
			loai: ['', Validators.required],
			operators: ['', Validators.required],
			options: ['', Validators.required],
			title_input: ['', Validators.required],
			time: ['', Validators.required],
		});
		// this.itemForm.controls["title"].markAsTouched();
		// this.itemForm.controls["loai"].markAsTouched();
		// this.itemForm.controls["operators"].markAsTouched();
		// this.itemForm.controls["options"].markAsTouched();
		// this.itemForm.controls["title_input"].markAsTouched();
		// this.itemForm.controls["time"].markAsTouched();
	}
	getTitle(): string {
		let result = this.translate.instant('filter.customfilter');
		if (!this.item || !this.item.id_row) {
			return result;
		}
		result = this.translate.instant('filter.edit');
		return result;
	}
	/** ACTIONS */
	prepare(): FilterModel {
		const controls = this.itemForm.controls;
		const item = new FilterModel();
		item.id_row = this.data._item.id_row;
		item.title = controls['title'].value;
		
		if (this.listColumn.length > 0) {
			this.listColumn.map((item, index) => {
				let _true = this.list_data_old.find(x => x.id_row === item.id_row);
				if (_true) {
					const ct = new FilterDetailModel();
					ct.id_row = item.id_row;
					ct.id_key = item.id_key;
					ct.operator = item.operator;
					
					ct.value = item.value;
					this.listChiTiet.push(ct);
				}
				else {
					const ct = new FilterDetailModel();
					if (ct.id_row == undefined)
						ct.id_row = 0;
					ct.id_key = item.id_key;
					ct.operator = item.operator;
					
					ct.value = item.value;
					this.listChiTiet.push(ct);
				}
			});
		}
		item.details = this.listChiTiet;
		return item;
	}
	FilterChange(e: any) {
		let filter_key = this.list_filter_key.find(x => x.id_row == e);
		if (filter_key == null || filter_key == undefined) {
			return '';
		};
		
		this.show_operators = true;
		this.list_operators = filter_key.operators;
		this.list_options = filter_key.options;
		//filter_key.loai: 1: options, 2 text-string, 3: text_date
		if (filter_key.loai == 1) {
			this.show_option_2 = false;
			this.show_option_3 = false;
			this.show_option_1 = true;
		}
		else {
			if (filter_key.loai == 2) {
				this.show_option_1 = false;
				this.show_option_3 = false;
				this.show_option_2 = true;
			}
			else {
				this.show_option_1 = false;
				this.show_option_2 = false;
				this.show_option_3 = true;
			}
		}
		this.itemForm.controls['operators'].setValue('');
		this.itemForm.controls['options'].setValue('');
		this.itemForm.controls['title_input'].setValue('');
		this.itemForm.controls['time'].setValue('');
		return filter_key.loai;
	}
	Filter_Options(e: any) {
		
		let filter_option = this.list_options.find(x => x.id == e);
		if (filter_option == null || filter_option == undefined) {
			return '';
		};
		this.showColumn = false;
		return filter_option.id;
	}

	Filter_operators(e: any) {
		let filter_operators = this.list_operators.find(x => x.id == e);
		if (filter_operators == null || filter_operators == undefined) {
			return '';
		};
		return filter_operators.id;

	}
	onSubmit(withBack: boolean = false) {
		this.hasFormErrors = false;
		this.loadingAfterSubmit = false;
		const controls = this.itemForm.controls;
		this.itemForm.controls['loai'].setValue(' ');
		this.itemForm.controls['operators'].setValue(' ');
		this.itemForm.controls['options'].setValue(' ');
		this.itemForm.controls['title_input'].setValue(' ');
		this.itemForm.controls['time'].setValue(' ');
		/* check form */
		// if (this.itemForm.invalid) {
		// 	Object.keys(controls).forEach(controlName =>
		// 		controls[controlName].markAsTouched()
		// 	);
		// 	this.hasFormErrors = true;
		// 	return;
		// }
		if(this.listColumn.length == 0){
			var text = 'Trường dữ liệu không thể trống';
			this.layoutUtilsService.showActionNotification(text, MessageType.Read, 3000, true, false, 3000, 'top', 0);
			return ;
		}

		const updatedegree = this.prepare();
		if (updatedegree.id_row > 0) {
			this.Update(updatedegree, withBack);
		} else {
			this.Create(updatedegree, withBack);
		}
	}
	filterConfiguration(): any {
		const filter: any = {};
		return filter;
	}
	Update(_item: FilterModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.viewLoading = true;
		this.disabledBtn = true;
		this._service.Update_filter(_item).subscribe(res => {
			this.disabledBtn = false;
			this.changeDetectorRefs.detectChanges();
			if (res && res.status === 1) {
				if (withBack == true) {
					this.dialogRef.close({
						_item
					});
				}
				else {
					this.ngOnInit();
					const _messageType = this.translate.instant('GeneralKey.capnhatthanhcong');
					this.layoutUtilsService.showActionNotification(_messageType, MessageType.Update, 4000, true, false).afterDismissed().subscribe(tt => {
					});
					// this.focusInput.nativeElement.focus();
				}
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
		});
	}
	Create(_item: FilterModel, withBack: boolean) {
		this.loadingAfterSubmit = true;
		this.disabledBtn = true;
		this._service.Insert_filter(_item).subscribe(res => {
			this.disabledBtn = false;
			if (res && res.status === 1) {
				if (withBack == true) {
					this.dialogRef.close({
						_item
					});
				}
				else {
					this.dialogRef.close();
				}
			}
			else {
				this.viewLoading = false;
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
			this.changeDetectorRefs.detectChanges();
		});
	}
	onAlertClose($event) {
		this.hasFormErrors = false;
	}
	close() {
		this.dialogRef.close();
	}
	reset() {
		this.item = Object.assign({}, this.item);
		this.createForm();
		this.hasFormErrors = false;
		this.itemForm.markAsPristine();
		this.itemForm.markAsUntouched();
		this.itemForm.updateValueAndValidity();
	}

	@HostListener('document:keydown', ['$event'])
	onKeydownHandler(event: KeyboardEvent) {
		if (event.ctrlKey && event.keyCode == 13)//phím Enter
		{
			this.item = this.data._item;
			if (this.viewLoading == true) {
				this.onSubmit(true);
			}
			else {
				this.onSubmit(false);
			}
		}
	}
	text(e: any, vi: any) {
		if (!((e.keyCode > 95 && e.keyCode < 106)
			|| (e.keyCode > 45 && e.keyCode < 58)
			|| e.keyCode == 8)) {
			e.preventDefault();
		}
	}
	themcot() {
		this.showColumn = true;
		this.changeDetectorRefs.detectChanges();
	}
	addcot() {
		let key = this.list_filter_key.find(x => x.id_row == this.filter_key);
		
		if (key) {
			let _filter = new FilterDetailModel;
			_filter.id_key = key.id_row;
			_filter.title = key.title;
			let operators = this.list_operators.find(x => x.id == this.filter_operators);
			if (operators || operators != undefined) {
				_filter.operator = operators.id;
			}
			else{
				this.layoutUtilsService.showError('Chưa chọn điều kiện');
				return;
			}
			if (key.loai == 1) {
				let options = this.list_options.find(x => x.id == this.filter_options);
				if (options || operators != undefined) {
					_filter.value = options.id; 
				}
			}
			else {
				if (key.loai == 2) {
					const controls = this.itemForm.controls;
					_filter.value = controls['title_input'].value;
				}
				else {
					const controls = this.itemForm.controls;
 					_filter.value = this.datepipe.transform(controls['time'].value, 'yyyy-MM-dd');
				}
			}
			if(!_filter.value){
				this.layoutUtilsService.showError('Chưa chọn giá trị');
				return;
			}
			_filter.StrTitle = key.title + ' ' + _filter.operator + ' ' + _filter.value;
			_filter.getTitleCol = key.title + ' ' + this.codeReplace(_filter.operator) + ' ' + this.getNamevalue(key.options,_filter.value);
			var check = this.listColumn.find(x=>x.StrTitle == _filter.StrTitle);
			if(check){
				this.layoutUtilsService.showActionNotification('Dữ liệu bị trùng');
				return ;  
			}
			this.listColumn.push(_filter);
		}
		this.showColumn = true;
		this.FilterChange('');
		this.changeDetectorRefs.detectChanges();
	}
	updateChanges() {
		this.onChange(this.listColumn);
	}

	codeReplace(code){
		if(code){
			var txt = code.replace("like", ":");
			return txt;
		}
		return code;
	}

	onChange: (_: any) => void = (_: any) => { };
	remove(item) {
		this.listColumn.splice(item, 1);
		this.showColumn = false;
		this.changeDetectorRefs.detectChanges();
	}
	checkShow(index: number) {
		try {
			let r = this.listCaLamViec.filter((item, vi) => {
				let t1 = this.listColumn.findIndex(x => +x.ID_Row === +item.ID_Row);
				return t1 !== -1 ? t1 == index : t1 == -1;
			});
			return r;
		} catch (error) {
			return [];
		}
	}
	DeleteFilter() {
		const _title = this.translate.instant('GeneralKey.xoa');
		const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
		const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');
		const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			this._service.Delete_filter(this.item.id_row).subscribe(res => {
				if (res && res.status === 1) {
					this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
					let _backUrl = `tasks`;
					this.router.navigateByUrl(_backUrl);
					this.dialogRef.close();
					this.changeDetectorRefs.detectChanges();
				}
				else {
					this.ngOnInit();
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				}
			});
		});
	}
}
