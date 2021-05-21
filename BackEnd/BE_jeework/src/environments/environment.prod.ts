export const environment = {
	
  production: true,
	isMockEnabled: false, // You have to switch this, when your real back-end is done
	authTokenKey: 'authce9d77b308c149d5992a80073637e4d5',
	// RootCookie: 'jeework.jee.vn',

	RootWeb: "https://jeework.jee.vn/", //'http://localhost:4200/',
	ApiRootsLanding: "https://api-proxy.vts-demo.com/apild",
	ApiRoots: "https://jeework-api.jee.vn/", //"https://localhost:44336/",//root chung
	logoLink: "",
	Module: "wework",
	// ApiRootAcount: "https://wms-apisys.bookve.com.vn/api",
	// WMSApiRoot: 'https://wms-apisys.bookve.com.vn/api',
	appVersion: "v717demo1",
	USERDATA_KEY: "authf649fc9a5f55",
	apiUrl: "api",

	ApiIdentity: 'https://identityserver.jee.vn',
	ApiIdentity_Logout: 'https://identityserver.jee.vn/user/logout',
	ApiIdentity_GetUser: 'https://identityserver.jee.vn/user/me',
	ApiIdentity_Refresh: 'https://identityserver.jee.vn/user/refresh',
	redirectUrl: 'https://portal.jee.vn/?redirectUrl=',
	sso: 'sso_token',

	//notification
	webSocket: 'wss://socket.jee.vn',
	apiNotification: 'https://notification.jee.vn/notification',
	//các link tùy chỉnh
	linkREQ: "https://jeerequest.jee.vn/",
	linkAccount: "http://jeeaccount.jee.vn/",
	//jee-account
	JeeAccountApi: 'https://jeeaccount-api.jee.vn/api'
};
