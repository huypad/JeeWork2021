import { PopoverContentComponent } from 'ngx-smart-popover';
import { Component, OnInit, ViewChild, ElementRef, ChangeDetectorRef, Input } from '@angular/core';

@Component({
  selector: 'kt-input-custom',
  templateUrl: './input-custom.component.html',
  styleUrls: ['./input-custom.component.scss']
})
export class InputCustomComponent implements OnInit {

  @Input() listUser: any[] = [];
  
	listdepartment: any[] = [];
	@ViewChild('Assign', { static: true }) myPopover_Assign: PopoverContentComponent;
	selected_Assign: any[] = [];
	@ViewChild('hiddenText_Assign', { static: true }) text_Assign: ElementRef;
	list_Assign: any[] = [];
	options_assign: any = {};
	_Assign: string = '';
  constructor(
    private changeDetectorRefs: ChangeDetectorRef,
    ) { }

  ngOnInit() {
    this.options_assign = this.getOptions_Assign();
  }

  ItemSelected_Assign(data) {
		this.selected_Assign = this.list_Assign;
		this.selected_Assign.push(data);
		let i = this._Assign.lastIndexOf('@');
		this._Assign = this._Assign.substr(0, i) + '@' + data.username + ' ';
		this.myPopover_Assign.hide();
		let ele = (<HTMLInputElement>document.getElementById("InputAssign"));
		ele.value = this._Assign;
		ele.focus();
    this.changeDetectorRefs.detectChanges();
    
  }
  getKeyword_Assign() {
		let i = this._Assign.lastIndexOf('@');
		if (i >= 0) {
			let temp = this._Assign.slice(i);
			if (temp.includes(' '))
				return '';
			return this._Assign.slice(i);
		}
		return '';
	}
  getOptions_Assign() {
		var options_assign: any = {
			showSearch: false,
			keyword: this.getKeyword_Assign(),
			data: this.listUser.filter(x => this.selected_Assign.findIndex(y => x.id_nv == y.id_nv) < 0),
		};
		return options_assign;
  }
  
  click_Assign($event, vi = -1) {
		// this.myPopover_Assign.hide();
	}
  
  onSearchChange_Assign($event) {
		this._Assign = (<HTMLInputElement>document.getElementById("InputAssign")).value;

		if (this.selected_Assign.length > 0) {
			var reg = /@\w*(\.[A-Za-z]\w*)|\@[A-Za-z]\w*/gm
			var match = this._Assign.match(reg);
			if (match != null && match.length > 0) {
				let arr = match.map(x => x);
				this.selected_Assign = this.selected_Assign.filter(x => arr.includes('@' + x.username));
			} else {
				this.selected_Assign = [];
			}
		}
		this.options_assign = this.getOptions_Assign();
		if (this.options_assign.keyword) {
			let el = $event.currentTarget;
			let rect = el.getBoundingClientRect();
			var ele = (<HTMLInputElement>document.getElementById("InputAssign"));
			var h = ele.offsetTop + 280;
			this.myPopover_Assign.show();
			this.myPopover_Assign.top = el.offsetTop + h;
			this.myPopover_Assign.left = el.offsetLeft + 1000;
			this.changeDetectorRefs.detectChanges();
		}
	}

}
