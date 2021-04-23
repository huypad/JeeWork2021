"use strict";
var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
exports.__esModule = true;
exports.MenuPhanQuyenServices = void 0;
var rxjs_1 = require("rxjs");
var core_1 = require("@angular/core");
var environment_1 = require("../../../../../environments/environment");
var query_params_model_1 = require("../../crud/models/query-models/query-params.model");
var API_ROOT_URL = environment_1.environment.ApiRoots + 'api/menu';
// const API_ROOT_URL = 'https://api-jeework.vts-demo.com/api/menu';
var API_ROOT_WF = environment_1.environment.ApiRoot + '/menu';
// const API_ROOT_URL = 'https://api-jeework.vts-demo.com/api/menu';
var MenuPhanQuyenServices = /** @class */ (function () {
    function MenuPhanQuyenServices(http, httpUtils) {
        this.http = http;
        this.httpUtils = httpUtils;
        this.lastFilter$ = new rxjs_1.BehaviorSubject(new query_params_model_1.QueryParamsModel({}, 'asc', '', 0, 10));
    }
    MenuPhanQuyenServices.prototype.layMenuChucNang = function (mod) {
        var httpHeaders = this.httpUtils.getHTTPHeaders();
        if (mod === "QLBTSC") {
            //api/phan-quyen
            return this.http.get(environment_1.environment.ApiRoots + environment_1.environment.BTSCSurfix + "/phan-quyen" + ("/LayMenuChucNang?v_module=" + mod), { headers: httpHeaders });
        }
        else if (mod === "LandingPage") { //Lây menu landingPage
            return this.http.get(environment_1.environment.ApiRootsLanding + "/menu/LayMenuChucNang", { headers: httpHeaders });
        }
        else if (mod === "Workflow") { //Lây menu Workflow{
            return this.http.get(API_ROOT_WF + ("/LayMenuChucNang?v_module=" + mod), { headers: httpHeaders });
        }
        else {
            return this.http.get(API_ROOT_URL + ("/LayMenuChucNang?v_module=" + mod), { headers: httpHeaders });
        }
    };
    MenuPhanQuyenServices.prototype.AllRoles = function (username) {
        var httpHeaders = this.httpUtils.getHTTPHeaders();
        return this.http.get(environment_1.environment.ApiRoots + environment_1.environment.BTSCSurfix + ("/phan-quyen/GetAllRoleForUser?username=" + username), { headers: httpHeaders });
    };
    // Get quyền cấp 2- WeWork
    MenuPhanQuyenServices.prototype.GetRoleWeWork = function (id_nv) {
        var httpHeaders = this.httpUtils.getHTTPHeaders();
        return this.http.get(API_ROOT_URL + ("/GetRoleWeWork?id_nv=" + id_nv), { headers: httpHeaders });
    };
    // Get quyền cấp 1 (Menu) - WeWork
    MenuPhanQuyenServices.prototype.WW_Roles = function (username) {
        var httpHeaders = this.httpUtils.getHTTPHeaders();
        return this.http.get(environment_1.environment.ApiRoots + ("wework/ww_userrights/GetRolesForUser_WeWork?username=" + username), { headers: httpHeaders });
    };
    MenuPhanQuyenServices.prototype.layMenuChucNangWMS = function () {
        var httpHeaders = this.httpUtils.getHTTPHeaders();
        return this.http.post(environment_1.environment.WMSApiRoot + "/nguoi-dung/LayMenuChucNang", "", { headers: httpHeaders });
    };
    MenuPhanQuyenServices.prototype.WMSRoles = function () {
        var httpHeaders = this.httpUtils.getHTTPHeaders();
        return this.http.post(environment_1.environment.WMSApiRoot + "/nguoi-dung/GetAllRoleForUser", "", { headers: httpHeaders });
    };
    MenuPhanQuyenServices = __decorate([
        core_1.Injectable()
    ], MenuPhanQuyenServices);
    return MenuPhanQuyenServices;
}());
exports.MenuPhanQuyenServices = MenuPhanQuyenServices;
