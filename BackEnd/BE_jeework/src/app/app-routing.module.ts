import { NgModule } from '@angular/core';
import { Routes, RouterModule, PreloadAllModules } from '@angular/router';
import { AppCustomPreloader } from './app-routing-loader';
import { AuthGuard } from './modules/auth/_services/auth.guard';

export const routes: Routes = [
  {
    path: 'auth',
    loadChildren: () =>
      import('./modules/auth/auth.module').then((m) => m.AuthModule),
  },
  {
    path: 'error',
    loadChildren: () =>
      import('./modules/errors/errors.module').then((m) => m.ErrorsModule),
  },
  {
    path: '',
    canActivate: [AuthGuard],
    loadChildren: () =>
      import('./pages/layout.module').then((m) => m.LayoutModule),
    data: { preload: true }
  },

  {
    path: 'aux',
    outlet: 'auxName',
    loadChildren: () => import('./pages/auxiliary-router/auxiliary-router.module').then((m) => m.AuxiliaryRouterModule),
  },
  { path: '**', redirectTo: 'error/404' },
];

@NgModule({
  // imports: [RouterModule.forRoot(routes)],
  // exports: [RouterModule],
  imports: [
    RouterModule.forRoot(routes, {
      preloadingStrategy: AppCustomPreloader
    })
  ],
  exports: [RouterModule],
  providers: [AppCustomPreloader]
})
export class AppRoutingModule { }
