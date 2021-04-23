import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router, UrlTree } from '@angular/router';
import { Observable } from 'rxjs';
import { TokenStorage } from './_services/token-storage.service';

@Injectable({
  providedIn: 'root'
})
export class AdminGuard implements CanActivate {
  constructor(
    private tokenStore: TokenStorage,
    private router: Router,
  ) { }
  canActivate(
    next: ActivatedRouteSnapshot,
    state: RouterStateSnapshot): Observable<boolean> | Promise<boolean> | boolean | UrlTree {
    let data = [];
    this.tokenStore.getMenuRoles().subscribe(t => { data = t; });
    if (data.length > 0) {
      let obj = data.find(x => x === '/' + next.routeConfig.path)
      if (obj) {
        return true;
      } else {
        this.router.navigate(['/']);
        return false;
      }
    }
    this.router.navigate(['/']);
    return false;
  }
}
