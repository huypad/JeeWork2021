import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { WorkGroupModel, WorkTagModel } from './../work.model';
import { WorkService } from './../work.service';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Injectable, Input, OnChanges, EventEmitter, Output, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { MatDialog } from '@angular/material/dialog';
import { MatTreeFlattener, MatTreeFlatDataSource } from '@angular/material/tree';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { BehaviorSubject, Observable, of as observableOf, ReplaySubject } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { FormGroup, FormBuilder, FormControl } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { FlatTreeControl } from '@angular/cdk/tree';
import { PopoverContentComponent } from 'ngx-smart-popover';
import { WeWorkService } from '../../services/wework.services';
import { UserInfoModel, WorkModel } from '../work.model';
import { WorkGroupEditComponent } from '../work-group-edit/work-group-edit.component';
import { ChangeReivewerComponent } from '../change-reivewer/change-reivewer.component';
// Services
// Models

export class TodoItemNode {
	Children: any;
	title: string;
}
export class TodoItemFlatNode {
	item: any;
	level: number;
	expandable: boolean;
	title: string;
}

@Injectable()
export class ChecklistDatabase {
	dataChange = new BehaviorSubject<TodoItemNode[]>([]);

	get data(): TodoItemNode[] { return this.dataChange.value; }

	constructor() {

	}

	init(data) {
		// Build the tree nodes from Json object. The result is a list of `TodoItemNode` with nested
		//     file node as children.
		// Notify the change.
		this.dataChange.next(data);
	}
}
@Component({
	selector: 'kt-list-view',
	templateUrl: './list-view.component.html',
	styleUrls: ['./list-view.component.scss'],
	changeDetection: ChangeDetectionStrategy.OnPush,
	providers: [ChecklistDatabase],
	encapsulation: ViewEncapsulation.None
})
export class ListViewComponent implements OnChanges {
	@Input() data: any[];
	@Input() listUser: any[] = [];
	@Input() selectedItem: any = undefined;
	@Output() ItemSelected = new EventEmitter<any>();
	@Output() Reload = new EventEmitter<any>();
	selectedId: number = 0;
	dataSource: MatTreeFlatDataSource<TodoItemNode, TodoItemFlatNode>;
	flatNodeMap = new Map<TodoItemFlatNode, TodoItemNode>();
	/** Map from nested node to flattened node. This helps us to keep the same object for selection */
	nestedNodeMap = new Map<TodoItemNode, TodoItemFlatNode>();
	/** A selected parent node to be inserted */
	selectedParent: TodoItemFlatNode | null = null;
	treeControl: FlatTreeControl<TodoItemFlatNode>;
	treeFlattener: MatTreeFlattener<TodoItemNode, TodoItemFlatNode>;
	/** The selection for checklist */
	checklistSelection = new SelectionModel<TodoItemFlatNode>(true /* multiple */);
	/* Drag and drop */
	dragNode: any;
	addItem = false;
	dragNodeExpandOverWaitTimeMs = 300;
	dragNodeExpandOverNode: any;
	dragNodeExpandOverTime: number;
	dragNodeExpandOverArea: string;
	id_choose: number = 0;
	options_assign: any = {};
	@ViewChild('Assign', { static: true }) myPopover_Assign: PopoverContentComponent;
	selected_Assign: any[] = [];
	@ViewChild('hiddenText_Assign', { static: true }) text_Assign: ElementRef;
	_Assign: string = '';
	public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	public bankFilterCtrl: FormControl = new FormControl();
	list_Assign: any[] = [];
	ID_PROJECT_TEAM: string;
	List_mileston: any[];
	List_tag: any[];
	milestone: any;
	constructor(
		public router: Router,
		private activatedRoute: ActivatedRoute,
		public dialog: MatDialog,
		private route: ActivatedRoute,
		private itemFB: FormBuilder,
		public subheaderService: SubheaderService,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private translate: TranslateService,
		public datepipe: DatePipe,
		public weworkService: WeWorkService,
		public workServices: WorkService,
		private tokenStorage: TokenStorage,
		private danhMucChungService: DanhMucChungService,
		private database: ChecklistDatabase
	) {
		this.treeFlattener = new MatTreeFlattener(this.transformer, this.getLevel, this.isExpandable, this.getChildren);
		this.treeControl = new FlatTreeControl<TodoItemFlatNode>(this.getLevel, this.isExpandable);
		this.dataSource = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);
		this.treeControl.expandAll();
	}

	/** The selection for checklist */
	getLevel = (node: TodoItemFlatNode) => node.level;
	isExpandable = (node: TodoItemFlatNode) => node.expandable;
	getChildren = (node: TodoItemNode): TodoItemNode[] => node.Children;
	// hasChild = (_: number, _nodeData: TodoItemFlatNode) => _nodeData.expandable;
	hasChild = (_: number, _nodeData: TodoItemFlatNode) => {
		var child = Object.keys(_nodeData.item);
		const found = child.find(element => element == 'Children');
		if (found) {
			return true;
		}
		else {
			return false
		}
	}
	// isNode

	hasNoContent = (_: number, _nodeData: TodoItemFlatNode) => _nodeData.item === '';
	transformer = (node: TodoItemNode, level: number) => {
		const existingNode = this.nestedNodeMap.get(node);

		const flatNode = existingNode && existingNode.item === node.Children ? existingNode : new TodoItemFlatNode();
		// const flatNode = new TodoItemFlatNode();
		flatNode.item = node;
		flatNode.level = level;
		flatNode.expandable = (node.Children && node.Children.length > 0);
		// flatNode.expandable = !!node.Children?.length;
		this.flatNodeMap.set(flatNode, node);
		this.nestedNodeMap.set(node, flatNode);
		return flatNode;
	}
	/** LOAD DATA */
	ngOnChanges() {
		var splitted = this.router.url.split("/");
		this.ID_PROJECT_TEAM = (splitted[2])
		if (this.selectedItem != undefined)
			this.selectedId = this.selectedItem.id_row;
		else
			this.selectedId = 0;
		this.dataSource.data = this.data;
		if (this.dataSource.data) {
			this.dataSource.data.forEach(element => {
				if (element.Children.length == 0)
					element.Children.push({})
			})
		}
		if (this.ID_PROJECT_TEAM) {
			this.weworkService.lite_milestone(this.ID_PROJECT_TEAM).subscribe(res => {
				this.changeDetectorRefs.detectChanges();
				if (res && res.status === 1) {
					this.List_mileston = res.data;
				};
			});
			this.weworkService.lite_tag(this.ID_PROJECT_TEAM).subscribe(res => {
				this.changeDetectorRefs.detectChanges();
				if (res && res.status === 1) {
					this.List_tag = res.data;
				};
			});
		}

		this.options_assign = this.getOptions_Assign();
		this.treeControl.expandAll();
	}
	ReloadNode(item){
		this.ngOnChanges();
	}
	setUpDropSearchNhanVien() {
		this.bankFilterCtrl.setValue('');
		this.filterBanks();
		this.bankFilterCtrl.valueChanges
			.pipe()
			.subscribe(() => {
				this.filterBanks();
			});
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
			this.listUser.filter(bank => bank.hoten.toLowerCase().indexOf(search) > -1)
		);
	}
	getOptions_Assign() {
		var options_assign: any = {
			showSearch: true,
			keyword: this.getKeyword_Assign(),
			data: this.listUser.filter(x => this.selected_Assign.findIndex(y => x.id_nv == y.id_nv) < 0),
		};
		return options_assign;
	}

	click_Assign($event, vi = -1) {
		this.myPopover_Assign.hide();
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
			this.myPopover_Assign.show();
			this.changeDetectorRefs.detectChanges();
		}
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
	descendantsAllSelected(node: TodoItemFlatNode): boolean {
		const descendants = this.treeControl.getDescendants(node);
		const descAllSelected = descendants.length > 0 && descendants.every(child => {
			return this.checklistSelection.isSelected(child);
		});
		return descAllSelected;
	}
	descendantsPartiallySelected(node: TodoItemFlatNode): boolean {
		const descendants = this.treeControl.getDescendants(node);
		const result = descendants.some(child => this.checklistSelection.isSelected(child));
		return result && !this.descendantsAllSelected(node);
	}
	/** Toggle the to-do item selection. Select/deselect all the descendants node */
	todoItemSelectionToggle(node: TodoItemFlatNode): void {
		this.checklistSelection.toggle(node);
		const descendants = this.treeControl.getDescendants(node);
		this.checklistSelection.isSelected(node)
			? this.checklistSelection.select(...descendants)
			: this.checklistSelection.deselect(...descendants);

		// Force update for the parent
		descendants.forEach(child => this.checklistSelection.isSelected(child));
		this.checkAllParentsSelection(node);
	}

	/** Toggle a leaf to-do item selection. Check all the parents to see if they changed */
	todoLeafItemSelectionToggle(node: TodoItemFlatNode): void {
		this.checklistSelection.toggle(node);
		this.checkAllParentsSelection(node);
	}

	/* Checks all the parents when a leaf node is selected/unselected */
	checkAllParentsSelection(node: TodoItemFlatNode): void {
		let parent: TodoItemFlatNode | null = this.getParentNode(node);
		while (parent) {
			this.checkRootNodeSelection(parent);
			parent = this.getParentNode(parent);
		}
	}
	getParentNode(node: TodoItemFlatNode): TodoItemFlatNode | null {
		const currentLevel = this.getLevel(node);

		if (currentLevel < 1) {
			return null;
		}
		const startIndex = this.treeControl.dataNodes.indexOf(node) - 1;

		for (let i = startIndex; i >= 0; i--) {
			const currentNode = this.treeControl.dataNodes[i];

			if (this.getLevel(currentNode) < currentLevel) {
				return currentNode;
			}
		}
		return null;
	}
	/** Check root node checked state and change it accordingly */
	checkRootNodeSelection(node: TodoItemFlatNode): void {
		const nodeSelected = this.checklistSelection.isSelected(node);
		const descendants = this.treeControl.getDescendants(node);
		const descAllSelected = descendants.length > 0 && descendants.every(child => {
			return this.checklistSelection.isSelected(child);
		});
		if (nodeSelected && !descAllSelected) {
			this.checklistSelection.deselect(node);
		} else if (!nodeSelected && descAllSelected) {
			this.checklistSelection.select(node);
		}
	}

	getHeight(): any {
		let tmp_height = 0;
		tmp_height = window.innerHeight - 67 -this.tokenStorage.getHeightHeader();//217
		return tmp_height + 'px';
	}
	selected(item) {
		this.ItemSelected.emit(item);
		this.selectedId = item.id_row;
		this.selectedItem = item;
	}

	addNewItem(node) {
		this.ClearNode();
		if (this.id_choose == node.item.id) {
			this.id_choose = 0;
		}
		else {
			this.id_choose = node.item.id;
		}
		// this.addItem = !this.addItem;
	}

	createNode = {
		title: '',
		giaocho: '',
		deadline: '',
		tasklist: '',
		milestone: {},
		tag: [],
	}

	ClearNode() {
		this.createNode = {
			title: '',
			giaocho: '',
			deadline: '',
			tasklist: '',
			milestone: {},
			tag: [],
		}
	}


	submitData(id_parent) {
		let a = this.createNode.deadline === "" ? new Date() : new Date(this.createNode.deadline);
		this.createNode.deadline = ("0" + (a.getMonth() + 1)).slice(-2) + "/" + ("0" + (a.getDate())).slice(-2) + "/" + a.getFullYear();
		this.createNode.tasklist = id_parent;
		// this.layoutUtilsService.showActionNotification("Thêm mới thành công" + JSON.stringify(this.createNode));
		for (let i of this.createNode.tag) {
			i.id_tag = i.id_row;
		}
		const _item = new WorkModel();
		var user = new UserInfoModel();
		user.id_user = + this.createNode.giaocho;
		user.loai = 1;
		_item.id_parent = id_parent;
		_item.deadline = this.createNode.deadline;
		_item.id_project_team = parseInt(this.ID_PROJECT_TEAM);
		_item.title = this.createNode.title;
		_item.id_milestone = this.createNode.milestone['id_row'];
		_item.Users.push(user);
		_item.Tags = this.createNode.tag;
		this.Create(_item);

	}

	// prepare(): WorkModel {

	// 	const _item = new WorkModel();
	// 	// _item.id_row = this.item.id_row;
	// 	_item.title =  this.createNode.title;
	// 	_item.id_project_team =this.ID_PROJECT_TEAM;
	// 	_item.id_milestone
	// 	if (_item.id_row > 0) {
	// 		_item.urgent = controls['urgent'].value;
	// 	}
	// 	else {
	// 		_item.id_milestone = controls['id_milestone'].value;
	// 		if (this.selected_Assign.length > 0) {
	// 			this.listUser.map((item, index) => {
	// 				let _true = this.selected_Assign.find(x => x.id_nv === item.id_nv);
	// 				if (_true) {
	// 					const _model = new UserInfoModel();
	// 					_model.id_user = item.id_nv;
	// 					_model.loai = 1;
	// 					this.list_User.push(_model);
	// 				}
	// 			});
	// 		}
	// 		if (this.selected.length > 0) {
	// 			this.listUser.map((item, index) => {
	// 				let _true = this.selected.find(x => x.id_nv === item.id_nv);
	// 				if (_true) {
	// 					const _model = new UserInfoModel();
	// 					_model.id_user = item.id_nv;
	// 					_model.loai = 2;
	// 					this.list_User.push(_model);
	// 				}
	// 			});
	// 		}
	// 		_item.Users = this.list_User;
	// 		_item.deadline = controls['deadline'].value;
	// 		this.check_tags.map((item, index) => {
	// 			let ktc = this.listTag.find(x => x.id_row == item);
	// 			if (ktc) {
	// 				let tag = new WorkTagModel;
	// 				tag.id_tag = ktc.id_row;
	// 				this.listTags.push(tag);
	// 			}
	// 		});
	// 		_item.Tags = this.listTags;
	// 		_item.Attachments = this.AttachFileComment;
	// 	}
	// 	return _item;
	// }


	Create(_item: WorkModel) {
		this.layoutUtilsService.showWaitingDiv();
		this.workServices.InsertWork(_item).subscribe(res => {
			if (res && res.status === 1) {
				this.id_choose = 0;
				this.ClearNode();
				this.Reload.emit(true);
			}
			else {
				this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
			}
			this.changeDetectorRefs.detectChanges();
			this.layoutUtilsService.OffWaitingDiv();
		});
	}

	AddMileston(item) {
		this.createNode.milestone = item;
		// 
	}
	isSelectedMileston(item){
		if(this.createNode.milestone== item){
			return true;
		}
		return false
	}
	AddTag(item) {
		var index = this.FindIndexinArr(this.createNode.tag, item);
		if (index == -1) {
			this.createNode.tag.push(item);
		}
		else {
			this.createNode.tag.splice(index, 1);
		}
		// 
	}
	FindIndexinArr(arr, item) {
		let i = 0;
		for (let a of arr) {
			if (a == item) {
				return i;
			}
			i++;
		}
		return -1;
	}
	isSelectedItem(item) {
		if (this.FindIndexinArr(this.createNode.tag, item) == -1)
			return false;
		return true;
	}
	UpdateWorkGroup(node) {
		let saveMessageTranslateParam = '';
		var _item = new WorkGroupModel();
		_item = node.item;
		// _item.id_project_team = '' + node.id_project_team;
		
		saveMessageTranslateParam += _item.id_row > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id_row > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(WorkGroupEditComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			else {
				this.ngOnChanges();
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.changeDetectorRefs.detectChanges();
			}
		});
	}
	ChangeReviewer(node) {
		let saveMessageTranslateParam = '';
		var _item = new WorkGroupModel();
		_item.clear();
		_item = node.item;
		
		_item.id_row = node.item.id;
		saveMessageTranslateParam += _item.id > 0 ? 'GeneralKey.capnhatthanhcong' : 'GeneralKey.themthanhcong';
		const _saveMessage = this.translate.instant(saveMessageTranslateParam);
		const _messageType = _item.id > 0 ? MessageType.Update : MessageType.Create;
		const dialogRef = this.dialog.open(ChangeReivewerComponent, { data: { _item } });
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				this.ngOnChanges();
			}
			else {
				this.layoutUtilsService.showActionNotification(_saveMessage, _messageType, 4000, true, false);
				this.ngOnChanges();
			}
		});
	}
	DeleteWorkGroup(node) {
		var _item = new WorkGroupModel();
		_item.clear();
		_item = node.item;
		const _title = this.translate.instant('GeneralKey.xoa');
		const _description = this.translate.instant('GeneralKey.bancochacchanmuonxoakhong');
		const _waitDesciption = this.translate.instant('GeneralKey.dulieudangduocxoa');
		const _deleteMessage = this.translate.instant('GeneralKey.xoathanhcong');

		const dialogRef = this.layoutUtilsService.deleteElement(_title, _description, _waitDesciption);
		dialogRef.afterClosed().subscribe(res => {
			if (!res) {
				return;
			}
			this.workServices.DeleteWorkGroup(_item.id).subscribe(res => {
				if (res && res.status === 1) {
					this.layoutUtilsService.showActionNotification(_deleteMessage, MessageType.Delete, 4000, true, false, 3000, 'top', 1);
				}
				else {
					this.layoutUtilsService.showActionNotification(res.error.message, MessageType.Read, 9999999999, true, false, 3000, 'top', 0);
				}
				this.ngOnChanges();
				this.Reload.emit(true);
			});
		});
	}

	isSelectedTag(item){
		if(this.createNode.tag.find(x => x == item)){
			return true;
		}
		return false
	}
}
