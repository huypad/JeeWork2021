import { NgModule } from "@angular/core";
import { Routes, RouterModule } from "@angular/router";
import { LayoutComponent } from "./_layout/layout.component";

const routes: Routes = [
  {
    path: "",
    component: LayoutComponent,
    // data: {
    // 	permissions: {
    // 		only: ['ADMIN', 'USER'],
    // 		except: ['GUEST'],
    // 		redirectTo: 'error/403'
    // 	}
    // },
    children: [
      {
        path: "",
        // canActivate: [AdminGuard],
        redirectTo: "tasks",
      },
      {
        path: "builder",
        loadChildren: () =>
          import("./builder/builder.module").then((m) => m.BuilderModule),
      },
      //#region wework
      {
        path: "depts",
        loadChildren: () =>
          import("./WeWork/List-department/List-department.module").then(
            (m) => m.DepartmentModule
          ),
      },
      {
        path: "wework",
        loadChildren: () =>
          import(
            "./WeWork/projects-team/all-project-team/all-project-team.module"
          ).then((m) => m.AllProjectTeamModule),
      },
      {
        path: "project/:id",
        loadChildren: () =>
          import("./WeWork/projects-team/projects-team.module").then(
            (m) => m.ProjectsTeamModule),
      },
      {
        path: "tasks",
        loadChildren: () =>
          import("./WeWork/work/work.module").then((m) => m.WorkModule),
      },
      {
        path: "users",
        loadChildren: () =>
          import("./WeWork/user/user.module").then((m) => m.UserModule),
      },
      {
        path: "reports",
        loadChildren: () =>
          import("./WeWork/report/report.module").then((m) => m.ReportModule),
      },
      {
        path: "permision",
        loadChildren: () =>
          import("./WeWork/Systems/userright/userright.module").then(
            (m) => m.UserRightModule
          ),
      },
      //#endregion
      {
        path: "dashboard",
        loadChildren: () =>
          import("./dashboard/dashboard.module").then((m) => m.DashboardModule),
      },
      {
        path: "ecommerce",
        loadChildren: () =>
          import("../modules/e-commerce/e-commerce.module").then(
            (m) => m.ECommerceModule
          ),
      },
      {
        path: "user-management",
        loadChildren: () =>
          import("../modules/user-management/user-management.module").then(
            (m) => m.UserManagementModule
          ),
      },
      {
        path: "user-profile",
        loadChildren: () =>
          import("../modules/user-profile/user-profile.module").then(
            (m) => m.UserProfileModule
          ),
      },
      {
        path: "ngbootstrap",
        loadChildren: () =>
          import("../modules/ngbootstrap/ngbootstrap.module").then(
            (m) => m.NgbootstrapModule
          ),
      },
      {
        path: "wizards",
        loadChildren: () =>
          import("../modules/wizards/wizards.module").then(
            (m) => m.WizardsModule
          ),
      },
      {
        path: "material",
        loadChildren: () =>
          import("../modules/material/material.module").then(
            (m) => m.MaterialModule
          ),
      },
    ],
    // children: [
    //   {
    //     path: 'dashboard',
    //     loadChildren: () =>
    //       import('./dashboard/dashboard.module').then((m) => m.DashboardModule),
    //   },
    //   {
    //     path: 'builder',
    //     loadChildren: () =>
    //       import('./builder/builder.module').then((m) => m.BuilderModule),
    //   },
    //   {
    //     path: 'ecommerce',
    //     loadChildren: () =>
    //       import('../modules/e-commerce/e-commerce.module').then(
    //         (m) => m.ECommerceModule
    //       ),
    //   },
    //   {
    //     path: 'user-management',
    //     loadChildren: () =>
    //       import('../modules/user-management/user-management.module').then(
    //         (m) => m.UserManagementModule
    //       ),
    //   },
    //   {
    //     path: 'user-profile',
    //     loadChildren: () =>
    //       import('../modules/user-profile/user-profile.module').then(
    //         (m) => m.UserProfileModule
    //       ),
    //   },
    //   {
    //     path: 'ngbootstrap',
    //     loadChildren: () =>
    //       import('../modules/ngbootstrap/ngbootstrap.module').then(
    //         (m) => m.NgbootstrapModule
    //       ),
    //   },
    //   {
    //     path: 'wizards',
    //     loadChildren: () =>
    //       import('../modules/wizards/wizards.module').then(
    //         (m) => m.WizardsModule
    //       ),
    //   },
    //   {
    //     path: 'material',
    //     loadChildren: () =>
    //       import('../modules/material/material.module').then(
    //         (m) => m.MaterialModule
    //       ),
    //   },
    //   {
    //     path: '',
    //     redirectTo: '/dashboard',
    //     pathMatch: 'full',
    //   },
    //   {
    //     path: '**',
    //     redirectTo: 'error/404',
    //   },
    // ],
  },
];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule],
})
export class PagesRoutingModule {}
