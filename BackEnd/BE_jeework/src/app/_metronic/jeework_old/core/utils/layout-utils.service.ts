import { MatPaginator } from '@angular/material/paginator';
import { environment } from 'src/environments/environment';
import { TranslateService } from '@ngx-translate/core';
import { HttpClient } from '@angular/common/http';
import { UpdateStatusDialogComponent } from './../../_shared/update-status-dialog/update-status-dialog.component';
import { FetchEntityDialogComponent } from './../../_shared/fetch-entity-dialog/fetch-entity-dialog.component';
import { DeleteEntityDialogComponent } from './../../_shared/delete-entity-dialog/delete-entity-dialog.component';
import { DocViewerComponent } from './../../_shared/doc-viewer/doc-viewer.component';
import { Injectable } from '@angular/core';
import { ActionNotificationComponent } from '../../_shared/action-natification/action-notification.component';
import { MatDialog } from '@angular/material/dialog';
import { MatSnackBar } from '@angular/material/snack-bar';


export enum MessageType {
    Create,
    Read,
    Update,
    Delete
}

@Injectable()
export class LayoutUtilsService {

    /**
     * Service constructor
     *
     * @param snackBar: MatSnackBar
     * @param dialog: MatDialog
     */

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

    constructor(
        private snackBar: MatSnackBar,
        private dialog: MatDialog,
        private http: HttpClient,
        private translate: TranslateService
    ) {
    }

    // SnackBar for notifications
    showActionNotification(
        message: string,
        type: MessageType = MessageType.Create,
        duration: number = 1000,
        showCloseButton: boolean = true,
        showUndoButton: boolean = false,
        undoButtonDuration: number = 3000,
        verticalPosition: 'top' | 'bottom' = 'top',
        mean: 0 | 1 = 1
    ) {
        return this.snackBar.openFromComponent(ActionNotificationComponent, {
            duration: duration,
            data: {
                message,
                snackBar: this.snackBar,
                showCloseButton: showCloseButton,
                showUndoButton: showUndoButton,
                undoButtonDuration,
                verticalPosition,
                type,
                action: 'Undo',
                meanMes: mean
            },
            verticalPosition: verticalPosition
        });
    }

    showInfo(
        message: string,
    ) {
        let type: MessageType = MessageType.Create,
            duration: number = 4000,
            showCloseButton: boolean = true,
            showUndoButton: boolean = false,
            undoButtonDuration: number = 4000,
            verticalPosition: 'top' | 'bottom' = 'top',
            mean: 0 | 1 = 1;
        return this.snackBar.openFromComponent(ActionNotificationComponent, {
            duration: duration,
            data: {
                message,
                snackBar: this.snackBar,
                showCloseButton: showCloseButton,
                showUndoButton: showUndoButton,
                undoButtonDuration,
                verticalPosition,
                type,
                action: 'Undo',
                meanMes: mean
            },
            verticalPosition: verticalPosition
        });
    }

    showError(
        message: string,
    ) {
        let type: MessageType = MessageType.Read,
            duration: number = 300_000,
            showCloseButton: boolean = true,
            showUndoButton: boolean = false,
            undoButtonDuration: number = 300_000,
            verticalPosition: 'top' | 'bottom' = 'top',
            mean: 0 | 1 = 0;
        return this.snackBar.openFromComponent(ActionNotificationComponent, {
            duration: duration,
            data: {
                message,
                snackBar: this.snackBar,
                showCloseButton: showCloseButton,
                showUndoButton: showUndoButton,
                undoButtonDuration,
                verticalPosition,
                type,
                action: 'Undo',
                meanMes: mean
            },
            verticalPosition: verticalPosition
        });
    }

    // Method returns instance of MatDialog
    deleteElement(title: string = '', description: string = '', waitDesciption: string = '', doPositiveBtn: string = 'Delete') {
        return this.dialog.open(DeleteEntityDialogComponent, {
            data: { title, description, waitDesciption, doPositiveBtn },
            width: '440px'
        });
    }

    // Method returns instance of MatDialog
    fetchElements(_data) {
        return this.dialog.open(FetchEntityDialogComponent, {
            data: _data,
            width: '400px'
        });
    }

    // Method returns instance of MatDialog
    updateStatusForCustomers(title, statuses, messages) {
        return this.dialog.open(UpdateStatusDialogComponent, {
            data: { title, statuses, messages },
            width: '480px'
        });
    }

    showWaitingDiv() {
        let v_idWaiting: string = 'nemo-process-waiting-id';//id waiting
        let v_idWaitingLoader: string = 'nemo-process-waiting-loader';//id waiting
        let _show: string = 'nemo-show-wait';
        let _hide: string = 'nemo-hide-wait';
        let divWait = document.getElementById(v_idWaiting);
        let loader = document.getElementById(v_idWaitingLoader);

        if (divWait.classList.contains(_show)) {
            divWait.classList.remove(_show);
            divWait.classList.add(_hide);

            loader.classList.remove(_show);
            loader.classList.add(_hide);
        } else {
            if (divWait.classList.contains(_hide)) {
                divWait.classList.remove(_hide);
                divWait.classList.add(_show);

                loader.classList.remove(_hide);
                loader.classList.add(_show);
            } else {
                divWait.classList.remove(_show);
                divWait.classList.add(_hide);

                loader.classList.remove(_show);
                loader.classList.add(_hide);
            }
        }
    }

    OffWaitingDiv() {
        let v_idWaiting: string = 'nemo-process-waiting-id';//id waiting
        let v_idWaitingLoader: string = 'nemo-process-waiting-loader';//id waiting
        let _show: string = 'nemo-show-wait';
        let _hide: string = 'nemo-hide-wait';
        let divWait = document.getElementById(v_idWaiting);
        let loader = document.getElementById(v_idWaitingLoader);

        divWait.classList.remove(_show);
        divWait.classList.add(_hide);

        loader.classList.remove(_show);
        loader.classList.add(_hide);

    }
    // Dùng cho flowchart
    showWaitingFlow() {
        let v_idWaiting: string = 'flow-process-waiting-id';//id waiting
        let v_idWaitingLoader: string = 'flow-process-waiting-loader';//id waiting
        let _show: string = 'flow-show-wait';
        let _hide: string = 'flow-hide-wait';
        let divWait = document.getElementById(v_idWaiting);
        let loader = document.getElementById(v_idWaitingLoader);

        if (divWait.classList.contains(_show)) {
            divWait.classList.remove(_show);
            divWait.classList.add(_hide);

            loader.classList.remove(_show);
            loader.classList.add(_hide);
        } else {
            if (divWait.classList.contains(_hide)) {
                divWait.classList.remove(_hide);
                divWait.classList.add(_show);

                loader.classList.remove(_hide);
                loader.classList.add(_show);
            } else {
                divWait.classList.remove(_show);
                divWait.classList.add(_hide);

                loader.classList.remove(_show);
                loader.classList.add(_hide);
            }
        }
    }

    OffWaitingFlow() {
        let v_idWaiting: string = 'flow-process-waiting-id';//id waiting
        let v_idWaitingLoader: string = 'flow-process-waiting-loader';//id waiting
        let _show: string = 'flow-show-wait';
        let _hide: string = 'flow-hide-wait';
        let divWait = document.getElementById(v_idWaiting);
        let loader = document.getElementById(v_idWaitingLoader);

        divWait.classList.remove(_show);
        divWait.classList.add(_hide);

        loader.classList.remove(_show);
        loader.classList.add(_hide);

    }

    // view doc
    ViewDoc(url) {
        return this.dialog.open(DocViewerComponent, {
            data: url,
            width: '95vw'
        });
    }

    // ==========================   lay out mới


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
        v = document.querySelector('body[m-root]');
        p = document.querySelector('m-pages');
        if (v && p) {
            _className = 'no-overflow';
            _style = v.attributes['style'].nodeValue;
            _style = _style.replace(/--scrollwidth.*\;/g, '');
            if (v.classList.contains(_className)) {
                //q.setAttribute("style", "--scrollwidth:0px");
                _style = '--scrollwidth:0px;' + _style;
                v.classList.remove(_className);
            } else {
                _style = '--scrollwidth:' + (window.innerWidth - v['offsetWidth']) + 'px;' + _style;
                v.classList.add(_className);
            }
            v.setAttribute('style', _style);
        }
    }

    setUpPaginationLabels(pagination: MatPaginator) {
        var trongso = this.translate.instant('notify.trongtongso');
        pagination._intl.firstPageLabel = this.translate.instant('filter.trangdau');
        pagination._intl.getRangeLabel = (page: number, pageSize: number, length: number) => {
            if (length == 0 || pageSize == 0) {
                return `0 ${trongso} ${length}`;
            }

            length = Math.max(length, 0);

            const startIndex = page * pageSize;

            // If the start index exceeds the list length, do not try and fix the end index to the end.
            const endIndex = startIndex < length ?
                Math.min(startIndex + pageSize, length) :
                startIndex + pageSize;

            return `${startIndex + 1} - ${endIndex} ${trongso} ${length}`;
        };
        pagination._intl.itemsPerPageLabel = this.translate.instant('notify.sodongtrentrang');
        pagination._intl.lastPageLabel = this.translate.instant('filter.trangcuoi');
        pagination._intl.nextPageLabel = this.translate.instant('filter.trangke');
        pagination._intl.previousPageLabel = this.translate.instant('filter.trangtruoc');
    }

    getBreadcrumb(href: string): any {
    }
}
