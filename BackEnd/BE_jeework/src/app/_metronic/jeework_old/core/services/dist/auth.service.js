"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
exports.__esModule = true;
exports.AuthService = void 0;
var core_1 = require("@angular/core");
var http_1 = require("@angular/common/http");
var rxjs_1 = require("rxjs");
var operators_1 = require("rxjs/operators");
var environment_1 = require("environments/environment");
var API_USERS_URL = 'api/users'; //'api/users'
var API_USERS = environment_1.environment.ApiRoot + '/user';
var API_PERMISSION_URL = 'api/permissions';
var API_ROLES_URL = 'api/roles';
var API_LOGIN_URL = environment_1.environment.ApiRoot + '/user/Login';
var API_LOGOUT_URL = environment_1.environment.ApiRoot + '/user/Logout';
var AuthService = /** @class */ (function () {
    function AuthService(http, tokenStorage) {
        this.http = http;
        this.tokenStorage = tokenStorage;
    }
    // Authentication/Authorization
    AuthService.prototype.login = function (username, password, checkReCaptCha, GReCaptCha) {
        var data = {
            username: username,
            password: password,
            checkReCaptCha: checkReCaptCha,
            GReCaptCha: GReCaptCha
        };
        return this.http.post(API_LOGIN_URL, data)
            .pipe(operators_1.map(function (result) {
            return result;
        }), operators_1.tap(this.saveAccessData.bind(this)), operators_1.catchError(this.handleError('login', [])));
    };
    AuthService.prototype.saveAccessData = function (response) {
        if (response && response.status === 1) {
            this.tokenStorage.updateStorage(response.data);
        }
        else {
            rxjs_1.throwError({ msg: 'error' });
        }
    };
    AuthService.prototype.getUserByToken = function () {
        var userToken = localStorage.getItem(environment_1.environment.AUTHTOKENKEY);
        var httpHeaders = new http_1.HttpHeaders();
        httpHeaders.set('Authorization', 'Bearer ' + userToken);
        return this.http.get(API_USERS_URL, { headers: httpHeaders });
    };
    AuthService.prototype.register = function (user) {
        var httpHeaders = new http_1.HttpHeaders();
        httpHeaders.set('Content-Type', 'application/json');
        return this.http.post(API_USERS_URL, user, { headers: httpHeaders })
            .pipe(operators_1.map(function (res) {
            return res;
        }), operators_1.catchError(function (err) {
            return null;
        }));
    };
    /*
     * Submit forgot password request
     *
     * @param {string} email
     * @returns {Observable<any>}
     */
    AuthService.prototype.requestPassword = function (email) {
        return this.http.get(API_USERS + '/ForgotPassword?username=' + email)
            .pipe(operators_1.catchError(this.handleError('forgot-password', [])));
    };
    AuthService.prototype.getAllUsers = function () {
        return this.http.get(API_USERS_URL);
    };
    AuthService.prototype.getUserById = function (userId) {
        return this.http.get(API_USERS_URL + ("/" + userId));
    };
    // DELETE => delete the user from the server
    AuthService.prototype.deleteUser = function (userId) {
        var url = API_USERS_URL + "/" + userId;
        return this.http["delete"](url);
    };
    // UPDATE => PUT: update the user on the server
    AuthService.prototype.updateUser = function (_user) {
        var httpHeaders = new http_1.HttpHeaders();
        httpHeaders.set('Content-Type', 'application/json');
        return this.http.put(API_USERS_URL, _user, { headers: httpHeaders });
    };
    // CREATE =>  POST: add a new user to the server
    AuthService.prototype.createUser = function (user) {
        var httpHeaders = new http_1.HttpHeaders();
        httpHeaders.set('Content-Type', 'application/json');
        return this.http.post(API_USERS_URL, user, { headers: httpHeaders });
    };
    // Method from server should return QueryResultsModel(items: any[], totalsCount: number)
    // items => filtered/sorted result
    AuthService.prototype.findUsers = function (queryParams) {
        var httpHeaders = new http_1.HttpHeaders();
        httpHeaders.set('Content-Type', 'application/json');
        return this.http.post(API_USERS_URL + '/findUsers', queryParams, { headers: httpHeaders });
    };
    // Permission
    AuthService.prototype.getAllPermissions = function () {
        return this.http.get(API_PERMISSION_URL);
    };
    AuthService.prototype.getRolePermissions = function (roleId) {
        return this.http.get(API_PERMISSION_URL + '/getRolePermission?=' + roleId);
    };
    // Roles
    AuthService.prototype.getAllRoles = function () {
        return this.http.get(API_ROLES_URL);
    };
    AuthService.prototype.getRoleById = function (roleId) {
        return this.http.get(API_ROLES_URL + ("/" + roleId));
    };
    // CREATE =>  POST: add a new role to the server
    AuthService.prototype.createRole = function (role) {
        // Note: Add headers if needed (tokens/bearer)
        var httpHeaders = new http_1.HttpHeaders();
        httpHeaders.set('Content-Type', 'application/json');
        return this.http.post(API_ROLES_URL, role, { headers: httpHeaders });
    };
    // UPDATE => PUT: update the role on the server
    AuthService.prototype.updateRole = function (role) {
        var httpHeaders = new http_1.HttpHeaders();
        httpHeaders.set('Content-Type', 'application/json');
        return this.http.put(API_ROLES_URL, role, { headers: httpHeaders });
    };
    // DELETE => delete the role from the server
    AuthService.prototype.deleteRole = function (roleId) {
        var url = API_ROLES_URL + "/" + roleId;
        return this.http["delete"](url);
    };
    // Check Role Before deletion
    AuthService.prototype.isRoleAssignedToUsers = function (roleId) {
        return this.http.get(API_ROLES_URL + '/checkIsRollAssignedToUser?roleId=' + roleId);
    };
    AuthService.prototype.findRoles = function (queryParams) {
        // This code imitates server calls
        var httpHeaders = new http_1.HttpHeaders();
        httpHeaders.set('Content-Type', 'application/json');
        return this.http.post(API_ROLES_URL + '/findRoles', queryParams, { headers: httpHeaders });
    };
    /*
     * Handle Http operation that failed.
     * Let the app continue.
   *
   * @param operation - name of the operation that failed
     * @param result - optional value to return as the observable result
     */
    AuthService.prototype.handleError = function (operation, result) {
        if (operation === void 0) { operation = 'operation'; }
        return function (error) {
            // TODO: send the error to remote logging infrastructure
            console.error(error); // log to console instead
            // Let the app keep running by returning an empty result.
            return rxjs_1.of(result);
        };
    };
    AuthService.prototype.resetSession = function () {
        var userToken = localStorage.getItem(environment_1.environment.AUTHTOKENKEY);
        var httpHeaders = new http_1.HttpHeaders({
            'Authorization': 'Bearer ' + userToken
        });
        httpHeaders.append("Content-Type", "application/json");
        return this.http.post(environment_1.environment.ApiRoot + '/user/ResetSession', null, { headers: httpHeaders })
            .pipe(operators_1.map(function (res) {
            return res;
        }), operators_1.catchError(function (err) {
            return rxjs_1.throwError(err);
        }));
    };
    AuthService.prototype.logout = function (refresh) {
        var _this = this;
        this.logout_new().subscribe(function (res) { }, function (err) { }, function () {
            _this.tokenStorage.clear();
            if (refresh) {
                location.reload(true);
            }
        });
    };
    AuthService.prototype.logout_new = function () {
        var userToken = localStorage.getItem(environment_1.environment.AUTHTOKENKEY);
        var httpHeaders = new http_1.HttpHeaders({
            'Authorization': 'Bearer ' + userToken
        });
        httpHeaders.append("Content-Type", "application/json");
        return this.http.get(API_LOGOUT_URL, { headers: httpHeaders })
            .pipe(operators_1.map(function (res) {
            return res;
        }), operators_1.catchError(function (err) {
            return rxjs_1.throwError(err);
        }));
    };
    //#region service worker
    AuthService.prototype.CreateFCM = function () {
        var userToken = localStorage.getItem(environment_1.environment.AUTHTOKENKEY);
        var httpHeaders = new http_1.HttpHeaders({
            'Authorization': 'Bearer ' + userToken
        });
        return this.http.get(environment_1.environment.ApiRoot + "/fcm/CreateFCM", { headers: httpHeaders });
    };
    AuthService.prototype.DeleteFCM = function (data) {
        var userToken = localStorage.getItem(environment_1.environment.AUTHTOKENKEY);
        var httpHeaders = new http_1.HttpHeaders({
            'Authorization': 'Bearer ' + userToken
        });
        httpHeaders.append("Content-Type", "application/json");
        return this.http.post(environment_1.environment.ApiRoot + "/fcm/DeleteFCM", data, { headers: httpHeaders });
    };
    //#endregion
    //#region vai trò
    AuthService.prototype.getVaiTro = function () {
        var userToken = localStorage.getItem(environment_1.environment.AUTHTOKENKEY);
        var httpHeaders = new http_1.HttpHeaders({
            'Authorization': 'Bearer ' + userToken
        });
        httpHeaders.append("Content-Type", "application/json");
        return this.http.get(environment_1.environment.ApiRoot + '/user/ds-vai-tro', { headers: httpHeaders });
    };
    AuthService.prototype.doiVaiTro = function (id) {
        var userToken = localStorage.getItem(environment_1.environment.AUTHTOKENKEY);
        var httpHeaders = new http_1.HttpHeaders({
            'Authorization': 'Bearer ' + userToken
        });
        httpHeaders.append("Content-Type", "application/json");
        return this.http.get(environment_1.environment.ApiRoot + '/user/doi-vai-tro?VaiTro=' + id, { headers: httpHeaders });
    };
    AuthService = __decorate([
        core_1.Injectable()
    ], AuthService);
    return AuthService;
}());
exports.AuthService = AuthService;
