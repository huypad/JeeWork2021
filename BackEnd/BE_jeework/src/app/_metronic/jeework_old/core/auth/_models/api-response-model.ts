export interface ErrorModel {
	message: string;
	code: string;
}

export interface ApiResponseModel {
	status: number;
	data: any;
	page: any;
	token: string;
	error: ErrorModel
}
