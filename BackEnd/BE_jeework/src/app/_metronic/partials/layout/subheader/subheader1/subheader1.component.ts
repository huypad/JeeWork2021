import { Component, OnInit, Input, ChangeDetectorRef } from '@angular/core';
import { Observable } from 'rxjs';
import { LayoutService } from '../../../../core';
import { SubheaderService } from '../_services/subheader.service';
import { BreadcrumbItemModel } from '../_models/breadcrumb-item.model';

@Component({
  selector: 'app-subheader1',
  templateUrl: './subheader1.component.html',
})
export class Subheader1Component implements OnInit {
  subheaderCSSClasses = '';
  subheaderContainerCSSClasses = '';
  subheaderMobileToggle = false;
  subheaderDisplayDesc = false;
  subheaderDisplayDaterangepicker = false;
  title$: Observable<string>;
  breadcrumbs$: Observable<BreadcrumbItemModel[]>;
  breadcrumbs: BreadcrumbItemModel[] = [];
  description$: Observable<string>;
  @Input() title: string;

  constructor(
    private layout: LayoutService,
    private subheader: SubheaderService,
    private cdr: ChangeDetectorRef
  ) {
    this.title$ = this.subheader.titleSubject.asObservable();
  }

  ngOnInit() {
    this.title$ = this.subheader.titleSubject.asObservable();
    this.breadcrumbs$ = this.subheader.breadCrumbsSubject.asObservable();
    this.description$ = this.subheader.descriptionSubject.asObservable();
    this.subheaderCSSClasses = this.layout.getStringCSSClasses('subheader');
    this.subheaderContainerCSSClasses = this.layout.getStringCSSClasses(
      'subheader_container'
    );
    this.subheaderMobileToggle = this.layout.getProp('subheader.mobileToggle');
    this.subheaderDisplayDesc = this.layout.getProp('subheader.displayDesc');
    this.subheaderDisplayDaterangepicker = this.layout.getProp(
      'subheader.displayDaterangepicker'
    );
    this.breadcrumbs$.subscribe((res) => {
      this.breadcrumbs = res;
      this.cdr.detectChanges();
    });
  }
}

const html = `
<!--begin::Subheader-->
<div
  class="subheader py-2 py-lg-6"
  [ngClass]="subheaderCSSClasses"
  id="kt_subheader"
>
  <div
    [ngClass]="subheaderContainerCSSClasses"
    class="d-flex align-items-center justify-content-between flex-wrap flex-sm-nowrap"
  >
    <!--begin::Info-->
    <div class="d-flex align-items-center flex-wrap mr-1">
      <ng-container *ngIf="subheaderMobileToggle">
        <!--begin::Mobile Toggle-->
        <button
          class="burger-icon burger-icon-left mr-4 d-inline-block d-lg-none"
          id="kt_subheader_mobile_toggle"
        >
          <span></span>
        </button>
        <!--end::Mobile Toggle-->
      </ng-container>

      <!--begin::Page Heading-->
      <div class="d-flex align-items-baseline flex-wrap mr-5">
        <!--begin::Page Title-->
        <ng-container *ngIf="title$ | async as _title">
          <h5 class="text-dark font-weight-bold my-1 mr-5">
            {{ _title }}
            <ng-container *ngIf="subheaderDisplayDesc">
              <ng-container *ngIf="description$ | async as _description">
                <small>{{ _description }}</small>
              </ng-container>
            </ng-container>
          </h5>
        </ng-container>
        <!--end::Page Title-->

        <!--begin::Breadcrumb-->
        <ul
          class="breadcrumb breadcrumb-transparent breadcrumb-dot font-weight-bold p-0 my-2 font-size-sm"
        >
          <li
            class="breadcrumb-item"
            *ngFor="let bc of breadcrumbs"
            routerLinkActive="active"
          >
            <a [routerLink]="bc.linkPath" class="text-muted">
              {{ bc.linkText }}
            </a>
          </li>
        </ul>
        <!--end::Breadcrumb-->
      </div>
      <!--end::Page Heading-->
    </div>
    <!--end::Info-->

    <!--begin::Toolbar-->
    <div class="d-flex align-items-center">
      <ng-container *ngIf="subheaderDisplayDaterangepicker">
        <!--begin::Daterange-->
        <a
          class="btn btn-light-primary btn-sm font-weight-bold mr-2 cursor-pointer"
          id="kt_dashboard_daterangepicker"
          data-toggle="tooltip"
          title="Select dashboard daterange"
          data-placement="left"
        >
          <span
            class="opacity-60 font-weight-bold mr-2"
            id="kt_dashboard_daterangepicker_title"
            >Today</span
          >
          <span class="font-weight-bold" id="kt_dashboard_daterangepicker_date"
            >Aug 16</span
          >
        </a>
        <!--end::Daterange-->
      </ng-container>
      <ng-container *ngIf="!subheaderDisplayDaterangepicker">
        <!--begin::Actions-->
        <a
          class="btn btn-light-primary font-weight-bolder btn-sm cursor-pointer"
        >
          Actions
        </a>
        <!--end::Actions-->
      </ng-container>

      <!--begin::Dropdown-->
      <div
        class="dropdown dropdown-inline"
        data-toggle="tooltip"
        title="Quick actions"
        placement="bottom-right"
        ngbDropdown
      >
        <a
          class="btn btn-icon cursor-pointer"
          data-toggle="dropdown"
          aria-haspopup="true"
          aria-expanded="false"
          ngbDropdownToggle
        >
          <span
            [inlineSVG]="'./assets/media/svg/icons/Files/File-plus.svg'"
            cacheSVG="true"
            class="svg-icon svg-icon-success svg-icon-2x"
          ></span>
        </a>
        <div
          class="dropdown-menu dropdown-menu-md dropdown-menu-right p-0 m-0"
          ngbDropdownMenu
        >
          <app-dropdown-menu1></app-dropdown-menu1>
        </div>
      </div>
      <!--end::Dropdown-->
    </div>
    <!--end::Toolbar-->
  </div>
</div>
<!--end::Subheader-->

`;
