import { UpdateStatusDialogComponent } from './../../../../_shared/update-status-dialog/update-status-dialog.component';
import { FetchEntityDialogComponent } from './../../../../_shared/fetch-entity-dialog/fetch-entity-dialog.component';
import { DeleteEntityDialogComponent } from './../../../../_shared/delete-entity-dialog/delete-entity-dialog.component';
import { ActionNotificationComponent } from './../../../../_shared/action-natification/action-notification.component';
import { TranslateService } from '@ngx-translate/core';
// Angular
import { Injectable } from '@angular/core';

import { MatSnackBar } from '@angular/material/snack-bar';
import { MatPaginator } from '@angular/material/paginator';
import { MatDialog } from '@angular/material/dialog';
// Partials for CRUD
import { environment } from 'src/environments/environment';
import { HttpClient } from '@angular/common/http';

export enum MessageType {
	Create,
	Read,
	Update,
	Delete
}

@Injectable()
export class LayoutUtilsServiceNew {
	/**
	 * Service constructor
	 *
	 * @param snackBar: MatSnackBar
	 * @param dialog: MatDialog
	 */
	constructor(
		private snackBar: MatSnackBar,
		private dialog: MatDialog,
		private http: HttpClient,
		private translate:TranslateService
	) { }

	/**
	 * Showing (Mat-Snackbar) Notification
	 *
	 * @param message: string
	 * @param type: MessageType
	 * @param duration: number
	 * @param showCloseButton: boolean
	 * @param showUndoButton: boolean
	 * @param undoButtonDuration: number
	 * @param verticalPosition: 'top' | 'bottom' = 'top'
	 */
	showActionNotification(
		_message: string,
		_type: MessageType = MessageType.Create,
		_duration: number = 10000,
		_showCloseButton: boolean = true,
		_showUndoButton: boolean = false,
		_undoButtonDuration: number = 3000,
		_verticalPosition: 'top' | 'bottom' = 'top',
		mean: 0 | 1 = 1
	) {
		const _data = {
			message: _message,
			snackBar: this.snackBar,
			showCloseButton: _showCloseButton,
			showUndoButton: _showUndoButton,
			undoButtonDuration: _undoButtonDuration,
			verticalPosition: _verticalPosition,
			type: _type,
			action: 'Undo'
		};
		return this.snackBar.openFromComponent(ActionNotificationComponent, {
			duration: _duration,
			data: _data,
			verticalPosition: _verticalPosition,
		});
	}

	showError(_message: string) {
		let _type: MessageType = MessageType.Read,
			_duration: number = 10000,
			_showCloseButton: boolean = true,
			_showUndoButton: boolean = false,
			_undoButtonDuration: number = 3000,
			_verticalPosition: 'top' | 'bottom' = 'top',
			mean: 0 | 1 = 1

		const _data = {
			message: _message,
			snackBar: this.snackBar,
			showCloseButton: _showCloseButton,
			showUndoButton: _showUndoButton,
			undoButtonDuration: _undoButtonDuration,
			verticalPosition: _verticalPosition,
			type: _type,
			action: 'Undo'
		};
		return this.snackBar.openFromComponent(ActionNotificationComponent, {
			duration: _duration,
			data: _data,
			verticalPosition: _verticalPosition,
		});
	}
	showInfo(_message: string) {
		let _type: MessageType = MessageType.Create,
			_duration: number = 10000,
			_showCloseButton: boolean = true,
			_showUndoButton: boolean = false,
			_undoButtonDuration: number = 3000,
			_verticalPosition: 'top' | 'bottom' = 'top',
			mean: 0 | 1 = 1

		const _data = {
			message: _message,
			snackBar: this.snackBar,
			showCloseButton: _showCloseButton,
			showUndoButton: _showUndoButton,
			undoButtonDuration: _undoButtonDuration,
			verticalPosition: _verticalPosition,
			type: _type,
			action: 'Undo'
		};
		return this.snackBar.openFromComponent(ActionNotificationComponent, {
			duration: _duration,
			data: _data,
			verticalPosition: _verticalPosition,
		});
	}
	/**
	 * Showing Confirmation (Mat-Dialog) before Entity Removing
	 *
	 * @param title: stirng
	 * @param description: stirng
	 * @param waitDesciption: string
	 */
	deleteElement(title: string = '', description: string = '', waitDesciption: string = '', nameButtonOK: string = '', nameButtonCancel: string = '') {
		return this.dialog.open(DeleteEntityDialogComponent, {
			data: { title, description, waitDesciption, nameButtonOK, nameButtonCancel },
			width: '440px'
		});
	}

	/**
	 * Showing Fetching Window(Mat-Dialog)
	 *
	 * @param _data: any
	 */
	fetchElements(_data) {
		return this.dialog.open(FetchEntityDialogComponent, {
			data: _data,
			width: '400px'
		});
	}

	/**
	 * Showing Update Status for Entites Window
	 *
	 * @param title: string
	 * @param statuses: string[]
	 * @param messages: string[]
	 */
	updateStatusForEntities(title, statuses, messages) {
		return this.dialog.open(UpdateStatusDialogComponent, {
			data: { title, statuses, messages },
			width: '480px'
		});
	}

	menuSelectColumns_On_Off(type: 0 | 1 = 0) {
		let v, p, _className, _style;
		v = document.querySelector("body[m-root]");
		p = document.querySelector("m-pages");
		if (v && p) {
			_className = "no-overflow";
			_style = v.attributes["style"].nodeValue;
			_style = _style.replace(/--scrollwidth.*\;/g, "");
			if (v.classList.contains(_className)) {
				//q.setAttribute("style", "--scrollwidth:0px");
				_style = "--scrollwidth:0px;" + _style;
				v.classList.remove(_className);
			}
			else {
				_style = "--scrollwidth:" + (window.innerWidth - v["offsetWidth"]) + "px;" + _style
				v.classList.add(_className);
			}
			v.setAttribute("style", _style);
		}
	}

	setUpPaginationLabels(pagination: MatPaginator) {
		var trongso = this.translate.instant('notify.trongtongso');
		pagination._intl.firstPageLabel = this.translate.instant('filter.trangdau');
		pagination._intl.getRangeLabel = (page: number, pageSize: number, length: number) => {
			if (length == 0 || pageSize == 0) { return `0 ${trongso} ${length}`; }

			length = Math.max(length, 0);

			const startIndex = page * pageSize;

			// If the start index exceeds the list length, do not try and fix the end index to the end.
			const endIndex = startIndex < length ?
				Math.min(startIndex + pageSize, length) :
				startIndex + pageSize;

			return `${startIndex + 1} - ${endIndex} ${trongso} ${length}`;
		};
		pagination._intl.itemsPerPageLabel = this.translate.instant('notify.sodongtrentrang')
		pagination._intl.lastPageLabel = this.translate.instant('filter.trangcuoi');
		pagination._intl.nextPageLabel = this.translate.instant('filter.trangke');
		pagination._intl.previousPageLabel = this.translate.instant('filter.trangtruoc');
	}
	getBreadcrumb(href: string): any {
	
	}
}
