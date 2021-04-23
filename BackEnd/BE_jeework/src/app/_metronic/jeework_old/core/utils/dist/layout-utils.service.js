"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
exports.__esModule = true;
exports.LayoutUtilsService = exports.MessageType = void 0;
var doc_viewer_component_1 = require("./../../_shared/doc-viewer/doc-viewer.component");
var core_1 = require("@angular/core");
var action_notification_component_1 = require("../../_shared/action-natification/action-notification.component");
var delete_entity_dialog_component_1 = require("../../_shared/delete-entity-dialog/delete-entity-dialog.component");
var fetch_entity_dialog_component_1 = require("../../_shared/fetch-entity-dialog/fetch-entity-dialog.component");
var update_status_dialog_component_1 = require("../../_shared/update-status-dialog/update-status-dialog.component");
var MessageType;
(function (MessageType) {
    MessageType[MessageType["Create"] = 0] = "Create";
    MessageType[MessageType["Read"] = 1] = "Read";
    MessageType[MessageType["Update"] = 2] = "Update";
    MessageType[MessageType["Delete"] = 3] = "Delete";
})(MessageType = exports.MessageType || (exports.MessageType = {}));
var LayoutUtilsService = /** @class */ (function () {
    function LayoutUtilsService(snackBar, dialog) {
        this.snackBar = snackBar;
        this.dialog = dialog;
    }
    // SnackBar for notifications
    LayoutUtilsService.prototype.showActionNotification = function (message, type, duration, showCloseButton, showUndoButton, undoButtonDuration, verticalPosition, mean) {
        if (type === void 0) { type = MessageType.Create; }
        if (duration === void 0) { duration = 1000; }
        if (showCloseButton === void 0) { showCloseButton = true; }
        if (showUndoButton === void 0) { showUndoButton = false; }
        if (undoButtonDuration === void 0) { undoButtonDuration = 3000; }
        if (verticalPosition === void 0) { verticalPosition = 'top'; }
        if (mean === void 0) { mean = 1; }
        return this.snackBar.openFromComponent(action_notification_component_1.ActionNotificationComponent, {
            duration: duration,
            data: {
                message: message,
                snackBar: this.snackBar,
                showCloseButton: showCloseButton,
                showUndoButton: showUndoButton,
                undoButtonDuration: undoButtonDuration,
                verticalPosition: verticalPosition,
                type: type,
                action: 'Undo',
                meanMes: mean
            },
            verticalPosition: verticalPosition
        });
    };
    LayoutUtilsService.prototype.showInfo = function (message) {
        var type = MessageType.Create, duration = 4000, showCloseButton = true, showUndoButton = false, undoButtonDuration = 3000, verticalPosition = 'top', mean = 1;
        return this.snackBar.openFromComponent(action_notification_component_1.ActionNotificationComponent, {
            duration: duration,
            data: {
                message: message,
                snackBar: this.snackBar,
                showCloseButton: showCloseButton,
                showUndoButton: showUndoButton,
                undoButtonDuration: undoButtonDuration,
                verticalPosition: verticalPosition,
                type: type,
                action: 'Undo',
                meanMes: mean
            },
            verticalPosition: verticalPosition
        });
    };
    LayoutUtilsService.prototype.showError = function (message) {
        var type = MessageType.Read, duration = 3000, showCloseButton = true, showUndoButton = false, undoButtonDuration = 3000, verticalPosition = 'top', mean = 0;
        return this.snackBar.openFromComponent(action_notification_component_1.ActionNotificationComponent, {
            duration: duration,
            data: {
                message: message,
                snackBar: this.snackBar,
                showCloseButton: showCloseButton,
                showUndoButton: showUndoButton,
                undoButtonDuration: undoButtonDuration,
                verticalPosition: verticalPosition,
                type: type,
                action: 'Undo',
                meanMes: mean
            },
            verticalPosition: verticalPosition
        });
    };
    // Method returns instance of MatDialog
    LayoutUtilsService.prototype.deleteElement = function (title, description, waitDesciption, doPositiveBtn) {
        if (title === void 0) { title = ''; }
        if (description === void 0) { description = ''; }
        if (waitDesciption === void 0) { waitDesciption = ''; }
        if (doPositiveBtn === void 0) { doPositiveBtn = 'Delete'; }
        return this.dialog.open(delete_entity_dialog_component_1.DeleteEntityDialogComponent, {
            data: { title: title, description: description, waitDesciption: waitDesciption, doPositiveBtn: doPositiveBtn },
            width: '440px'
        });
    };
    // Method returns instance of MatDialog
    LayoutUtilsService.prototype.fetchElements = function (_data) {
        return this.dialog.open(fetch_entity_dialog_component_1.FetchEntityDialogComponent, {
            data: _data,
            width: '400px'
        });
    };
    // Method returns instance of MatDialog
    LayoutUtilsService.prototype.updateStatusForCustomers = function (title, statuses, messages) {
        return this.dialog.open(update_status_dialog_component_1.UpdateStatusDialogComponent, {
            data: { title: title, statuses: statuses, messages: messages },
            width: '480px'
        });
    };
    LayoutUtilsService.prototype.showWaitingDiv = function () {
        var v_idWaiting = 'nemo-process-waiting-id'; //id waiting
        var v_idWaitingLoader = 'nemo-process-waiting-loader'; //id waiting
        var _show = 'nemo-show-wait';
        var _hide = 'nemo-hide-wait';
        var divWait = document.getElementById(v_idWaiting);
        var loader = document.getElementById(v_idWaitingLoader);
        if (divWait.classList.contains(_show)) {
            divWait.classList.remove(_show);
            divWait.classList.add(_hide);
            loader.classList.remove(_show);
            loader.classList.add(_hide);
        }
        else {
            if (divWait.classList.contains(_hide)) {
                divWait.classList.remove(_hide);
                divWait.classList.add(_show);
                loader.classList.remove(_hide);
                loader.classList.add(_show);
            }
            else {
                divWait.classList.remove(_show);
                divWait.classList.add(_hide);
                loader.classList.remove(_show);
                loader.classList.add(_hide);
            }
        }
    };
    LayoutUtilsService.prototype.OffWaitingDiv = function () {
        var v_idWaiting = 'nemo-process-waiting-id'; //id waiting
        var v_idWaitingLoader = 'nemo-process-waiting-loader'; //id waiting
        var _show = 'nemo-show-wait';
        var _hide = 'nemo-hide-wait';
        var divWait = document.getElementById(v_idWaiting);
        var loader = document.getElementById(v_idWaitingLoader);
        divWait.classList.remove(_show);
        divWait.classList.add(_hide);
        loader.classList.remove(_show);
        loader.classList.add(_hide);
    };
    // Dùng cho flowchart
    LayoutUtilsService.prototype.showWaitingFlow = function () {
        var v_idWaiting = 'flow-process-waiting-id'; //id waiting
        var v_idWaitingLoader = 'flow-process-waiting-loader'; //id waiting
        var _show = 'flow-show-wait';
        var _hide = 'flow-hide-wait';
        var divWait = document.getElementById(v_idWaiting);
        var loader = document.getElementById(v_idWaitingLoader);
        if (divWait.classList.contains(_show)) {
            divWait.classList.remove(_show);
            divWait.classList.add(_hide);
            loader.classList.remove(_show);
            loader.classList.add(_hide);
        }
        else {
            if (divWait.classList.contains(_hide)) {
                divWait.classList.remove(_hide);
                divWait.classList.add(_show);
                loader.classList.remove(_hide);
                loader.classList.add(_show);
            }
            else {
                divWait.classList.remove(_show);
                divWait.classList.add(_hide);
                loader.classList.remove(_show);
                loader.classList.add(_hide);
            }
        }
    };
    LayoutUtilsService.prototype.OffWaitingFlow = function () {
        var v_idWaiting = 'flow-process-waiting-id'; //id waiting
        var v_idWaitingLoader = 'flow-process-waiting-loader'; //id waiting
        var _show = 'flow-show-wait';
        var _hide = 'flow-hide-wait';
        var divWait = document.getElementById(v_idWaiting);
        var loader = document.getElementById(v_idWaitingLoader);
        divWait.classList.remove(_show);
        divWait.classList.add(_hide);
        loader.classList.remove(_show);
        loader.classList.add(_hide);
    };
    // view doc
    LayoutUtilsService.prototype.ViewDoc = function (url) {
        return this.dialog.open(doc_viewer_component_1.DocViewerComponent, {
            data: url,
            width: '95vw'
        });
    };
    LayoutUtilsService = __decorate([
        core_1.Injectable()
    ], LayoutUtilsService);
    return LayoutUtilsService;
}());
exports.LayoutUtilsService = LayoutUtilsService;
