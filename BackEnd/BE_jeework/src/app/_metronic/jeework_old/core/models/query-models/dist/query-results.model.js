"use strict";
exports.__esModule = true;
exports.QueryResultsModel2 = exports.QueryResultsModel = void 0;
var QueryResultsModel = /** @class */ (function () {
    function QueryResultsModel(_items, _errorMessage) {
        if (_items === void 0) { _items = []; }
        if (_errorMessage === void 0) { _errorMessage = ''; }
        this.items = this.data = _items;
        this.totalCount = _items.length;
    }
    return QueryResultsModel;
}());
exports.QueryResultsModel = QueryResultsModel;
var QueryResultsModel2 = /** @class */ (function () {
    function QueryResultsModel2() {
    }
    return QueryResultsModel2;
}());
exports.QueryResultsModel2 = QueryResultsModel2;
var ErrorModel = /** @class */ (function () {
    function ErrorModel(_code, _errorMessage) {
        if (_code === void 0) { _code = ''; }
        if (_errorMessage === void 0) { _errorMessage = ''; }
        this.code = _code;
        this.message = _errorMessage;
    }
    return ErrorModel;
}());
//data: null
//error: { message: "Tổng số ngày cấp phép mỗi tháng không lớn hơn số ngày đã cho", code: "107" }
//page: null
//status: 0
