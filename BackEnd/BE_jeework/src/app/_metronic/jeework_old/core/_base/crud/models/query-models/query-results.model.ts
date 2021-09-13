export class QueryResultsModel {
    // fields
    data: any[];
    items: any[];
    page: any;
    totalCount: number;
    errorMessage: string;

    [x: string]: any;

    constructor(_items: any[] = [], _totalCount: number = 0, _errorMessage: string = '') {
        this.items = this.data = _items;
        this.totalCount = _totalCount;
    }
}

export class QueryResultsModel2 {
    // fields
    data: any[];
    page: any;
    error: ErrorModel;
    status: number;
}


class ErrorModel {
    code: string;
    message: string;

    constructor(_code: string = '', _errorMessage: string = '') {
        this.code = _code;
        this.message = _errorMessage;
    }
}

