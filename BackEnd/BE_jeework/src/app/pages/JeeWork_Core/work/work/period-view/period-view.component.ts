import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';
import { MatTreeFlattener, MatTreeFlatDataSource } from '@angular/material/tree';
import { MatDialog } from '@angular/material/dialog';
import { ChangeReivewerComponent } from './../change-reivewer/change-reivewer.component';
import { WorkGroupEditComponent } from './../work-group-edit/work-group-edit.component';
import { WorkService } from './../work.service';
import { UserInfoModel, WorkModel, WorkGroupModel } from './../work.model';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Injectable, Input, OnChanges, EventEmitter, Output, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
// Material
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { BehaviorSubject, Observable, of as observableOf, ReplaySubject } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { FormGroup, FormBuilder, FormControl } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';

import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { DepartmentModel } from '../../List-department/Model/List-department.model';
import { FlatTreeControl } from '@angular/cdk/tree';
import { WeWorkService } from '../../services/wework.services';
import { ParseError } from '@angular/compiler';
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
	selector: 'kt-period-view',
	templateUrl: './period-view.component.html',
	styleUrls: ['./period-view.component.scss'],
	changeDetection: ChangeDetectionStrategy.OnPush,
	providers: [ChecklistDatabase],
	encapsulation: ViewEncapsulation.None
})
export class PeriodViewComponent implements OnChanges {
	@Input() data: any[];
	@Input() listUser: any[] = [];
	@Input() selectedItem: any = undefined;
	@Output() ItemSelected = new EventEmitter<any>();
	@Output() Reload = new EventEmitter<any>();
	selectedId: number = 0;
	addItem = false;
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
	dragNodeExpandOverWaitTimeMs = 300;
	dragNodeExpandOverNode: any;
	dragNodeExpandOverTime: number;
	dragNodeExpandOverArea: string;
	public bankFilterCtrl: FormControl = new FormControl();
	public filteredBanks: ReplaySubject<any[]> = new ReplaySubject<any[]>(1);
	id_choose: number = 0;
	ID_PROJECT_TEAM: string;
	List_mileston: any[] = [];
	List_tag: any[] = [];
	constructor(
		private activatedRoute: ActivatedRoute,
		public dialog: MatDialog,
		private route: ActivatedRoute,
		private itemFB: FormBuilder,
		public subheaderService: SubheaderService,
		private layoutUtilsService: LayoutUtilsService,
		private changeDetectorRefs: ChangeDetectorRef,
		private translate: TranslateService,
		public datepipe: DatePipe,
		private tokenStorage: TokenStorage,
		private danhMucChungService: DanhMucChungService,
		private database: ChecklistDatabase,		
		public router: Router,
		public weworkService: WeWorkService,
		public workServices: WorkService,
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

		this.dataSource.data = this.data;
		this.treeControl.expandAll();
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
	}

	createNode:any = {
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

	AddMileston(item) {
		this.createNode.milestone = item;
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

	submitData(id_parent) {
		let a = this.createNode.deadline === "" ? new Date() : new Date(this.createNode.deadline);
		this.createNode.deadline = ("0" + (a.getMonth() + 1)).slice(-2) + "/" + ("0" + (a.getDate())).slice(-2) + "/" + a.getFullYear();
		this.createNode.tasklist = id_parent;
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
		dialogRef.afterClosed().subscribe(res => {this.layoutUtilsService.showWaitingDiv();
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
				this.layoutUtilsService.OffWaitingDiv();
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
	isSelectedMileston(item){
		if(this.createNode.milestone== item){
			return true;
		}
		return false
	}
}
