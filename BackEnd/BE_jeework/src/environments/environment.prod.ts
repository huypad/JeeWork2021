export const environment = {
  production: true,
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
	ApiRoots: "https://localhost:44366/",//"https://localhost:44336/",//root chung
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
	
	ApiIdentity: 'https://identityserver.jee.vn',
	redirectUrl:'https://portal.jee.vn/?redirectUrl=',
  appVersion: 'v717demo1',
  USERDATA_KEY: 'authf649fc9a5f55',
  apiUrl: 'api'
  // apiUrl: 'mysite.com/api'
};
