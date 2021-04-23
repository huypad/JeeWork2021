// Angular
import { Injectable } from '@angular/core';
import { ActivatedRouteSnapshot, CanActivate, Router, RouterStateSnapshot } from '@angular/router';
// RxJS
import { Observable } from 'rxjs';
import { tap } from 'rxjs/operators';
// NGRX
import { select, Store } from '@ngrx/store';
// Auth reducers and selectors
import { AppState} from '../../../core/reducers/';
import { isLoggedIn } from '../_selectors/auth.selectors';
import { TokenStorage } from '../_services/token-storage.service';
import jwt_decode from "jwt-decode";
@Injectable()
export class AuthGuard implements CanActivate {
    constructor(private store: Store<AppState>, private router: Router,
		private tokenStorage: TokenStorage) { }

    async canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): Promise<boolean> {
		let token = await this.tokenStorage.getAccessToken().toPromise()
		// if (token && this.isTokenExpired()) 
		if (token) 
		{
			// logged in so return true
			return true;
		}
		// not logged in so redirect to login page with the return url
		this.router.navigate(['/login']);
		return false;
	}

	getToken(): string {
		
		return localStorage.getItem('accessToken');
	}

	getTokenExpirationDate(token: string): Date {
		// token = atob(token);
		const decoded : any = jwt_decode(token);
		if (decoded.exp === undefined) return null;
		const date = new Date(0);
		date.setUTCSeconds(decoded.exp);
		return date;
	}
	isTokenExpired(token?: string): boolean {
		if (!token) token = this.getToken();
		if (!token) return false;

		const date = this.getTokenExpirationDate(token);
		if (date === undefined) return false;
		return (date.valueOf() > new Date().valueOf());
	}
}
