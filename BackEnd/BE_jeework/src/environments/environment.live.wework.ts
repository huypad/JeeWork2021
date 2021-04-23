// This file can be replaced during build by using the `fileReplacements` array.
// `ng build --prod` replaces `environment.ts` with `environment.prod.ts`.
// The list of file replacements can be found in `angular.json`.

export const environment = {
	production: false,
	isMockEnabled: false, // You have to switch this, when your real back-end is done
	authTokenKey: 'authce9d77b308c149d5992a80073637e4d5',
	RootCookie: 'vts-demo.com',
	// ApiRoot: 'http://apicustomer.jeehr.com/api',
	// ApiRoot:'http://localhost:65042/api',
	ApiRoot: 'https://api-hrm.vts-demo.com/API',//
	RootWeb: "https://hr.vts-demo.com/",//'http://localhost:4200/',
	//Module: 'LandingPage',
	ApiRootsLanding: "https://api-proxy.vts-demo.com/apild",

	//***Root BTSC******
	ApiRoots: "https://api-proxy.vts-demo.com/",//"https://localhost:44336/",//root chung
	HRMSurfix: "apihrm",//surfix HRM
	BTSCSurfix: "apiscbt",//surfix BTSC
	WMSSurfix: "apiwms",// surfix WMS
	HostSCBTSurfix: "scbt",// Link root cannot api
	//Module: 'QLBTSC',//module BTSC
	//**************/

	ApiRootBTSC: "https://api-proxy.vts-demo.com/apiscbt",//'https://localhost:44336/apiscbt',
	// RootBTSC: 'https://localhost:44349',
	// Module: 'Workflow',

	logoLink: '',
	Module: 'wework',
	WMSApiRoot: "https://api-proxy.vts-demo.com/apiwms",//'https://localhost:44398/API',
	ApiRootAcount: 'https://wms-apisys.bookve.com.vn/api',
	// Module: 'WMS',
};

/*
 * For easier debugging in development mode, you can import the following file
 * to ignore zone related error stack frames such as `zone.run`, `zoneDelegate.invokeTask`.
 *
 * This import should be commented out in production mode because it will have a negative impact
 * on performance if an error is thrown.
 */
// import 'zone.js/dist/zone-error';  // Included with Angular CLI.
