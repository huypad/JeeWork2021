import { element } from 'protractor';
import { Component, OnInit, ElementRef, ViewChild, ChangeDetectionStrategy, ChangeDetectorRef, Injectable, Input, OnChanges, EventEmitter, Output, ViewEncapsulation } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
// Material
import { MatSort } from '@angular/material/sort';
import { MatPaginator,MatPaginatorIntl } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
import { MatTreeFlattener, MatTreeFlatDataSource } from '@angular/material/tree';
import { SelectionModel } from '@angular/cdk/collections';
// RXJS
import { debounceTime, distinctUntilChanged, tap } from 'rxjs/operators';
import { BehaviorSubject, Observable, of as observableOf } from 'rxjs';
import { TranslateService } from '@ngx-translate/core';
import { LayoutUtilsService, MessageType } from './../../../../_metronic/jeework_old/core/utils/layout-utils.service';
import { FormGroup, FormBuilder } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { TokenStorage } from './../../../../_metronic/jeework_old/core/auth/_services/token-storage.service';
import { SubheaderService } from './../../../../_metronic/partials/layout/subheader/_services/subheader.service';

import { QueryParamsModelNew } from './../../../../_metronic/jeework_old/core/models/query-models/query-params.model';
import { FlatTreeControl } from '@angular/cdk/tree';
import { DanhMucChungService } from './../../../../_metronic/jeework_old/core/services/danhmuc.service';// Services
// Models

export class TodoItemNode {
	Children: any;
	title: string;
	Childs: any;
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
	selector: 'kt-stream-view',
	templateUrl: './stream-view.component.html',
	styleUrls: ['./stream-view.component.scss'],
	changeDetection: ChangeDetectionStrategy.OnPush,
	providers: [ChecklistDatabase],
	encapsulation: ViewEncapsulation.None
})
export class StreamViewComponent implements OnChanges {
	@Input() data: any[];
	@Input() listUser: any[] = [];
	@Input() selectedItem: any = undefined;
	@Output() ItemSelected = new EventEmitter<any>();
	// @Output() Reload = new EventEmitter<any>();
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
	dragNodeExpandOverWaitTimeMs = 300;
	dragNodeExpandOverNode: any;
	dragNodeExpandOverTime: number;
	dragNodeExpandOverArea: string;
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
	getChildren = (node: TodoItemNode): TodoItemNode[] => node.Childs;
	hasChild = (_: number, _nodeData: TodoItemFlatNode) => _nodeData.expandable;
	hasNoContent = (_: number, _nodeData: TodoItemFlatNode) => _nodeData.item === '';
	transformer = (node: TodoItemNode, level: number) => {
		const existingNode = this.nestedNodeMap.get(node);

		const flatNode = existingNode && existingNode.item === node.Childs ? existingNode : new TodoItemFlatNode();
		// const flatNode = new TodoItemFlatNode();
		flatNode.item = node;
		flatNode.level = level;
		flatNode.expandable = (node.Childs && node.Childs.length > 0);
		// flatNode.expandable = !!node.Children?.length;
		this.flatNodeMap.set(flatNode, node);
		this.nestedNodeMap.set(node, flatNode);
		return flatNode;
	}
	listNode : any=[];
	/** LOAD DATA */
	ngOnChanges() {
		this.layoutUtilsService.showWaitingDiv();
		if (this.selectedItem != undefined)
			this.selectedId = this.selectedItem.id_row;
		else
			this.selectedId = 0;
		this.listNode = [];
		this.dataSource.data = this.data;
		this.data.forEach(element => {
			var dts : MatTreeFlatDataSource<TodoItemNode, TodoItemFlatNode>;
			dts = new MatTreeFlatDataSource(this.treeControl, this.treeFlattener);
			this.treeControl.expandAll();
			dts.data = element.Children;
			this.listNode.push({
				end:element.end,
				start:element.start,
				id_row:element.id_row,
				DataSource:dts
			})
		}) 
		this.layoutUtilsService.OffWaitingDiv();
		this.treeControl.expandAll();
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
	Reload(value) {
		if (value) {
			this.ngOnChanges();
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
		tmp_height = window.innerHeight - 50 -this.tokenStorage.getHeightHeader();//217
		return tmp_height + 'px';
	}
	selected(item) {
		this.ItemSelected.emit(item);
		this.selectedId = item.id_row;
	}
}
