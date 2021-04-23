// This file can be replaced during build by using the `fileReplacements` array.
// `ng build --prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

export const environment = {
	production: false,
	isMockEnabled: false, // You have to switch this, when your real back-end is done
	authTokenKey: 'authce9d77b308c149d5992a80073637e4d5',
	RootCookie: 'localhost',
	RootWeb: 'http://localhost:4200/',
	ApiRootsLanding: "http://localhost:54225/API",
	ApiRoots: "https://localhost:44366/",//root chung
	Module: 'wework',
	WMSApiRoot: 'https://localhost:44398/API',
	ApiRootAcount: 'http://wms-apisys.bookve.com.vn/api',
	logoLink: '',
	appVersion: "v717demo1",
	USERDATA_KEY: "authf649fc9a5f55",
	apiUrl: "api",
	ApiIdentity: 'https://identityserver.jee.vn',
	redirectUrl:'https://portal.jee.vn/?redirectUrl='
};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/dist/zone-error';  // Included with Angular CLI.
