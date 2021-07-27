import { ExtrasModule } from './../../_metronic/partials/layout/extras/extras.module';
import { NgModule } from '@angular/core';
import { CommonModule } from '@angular/common';

import { MyChatRoutingModule } from './my-chat-routing.module';
import { NgxAutoScrollModule } from 'ngx-auto-scroll';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { MatIconModule } from '@angular/material/icon';
import { MatDialogModule } from '@angular/material/dialog';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { BrowserModule } from '@angular/platform-browser';
import { CarouselModule } from 'ngx-bootstrap/carousel';
@NgModule({
  declarations: [],
  imports: [
    CommonModule,
    CarouselModule ,
    RouterModule,
    MatInputModule,
    MatDialogModule,
    MatFormFieldModule,
    ExtrasModule,
    NgxAutoScrollModule,
    MyChatRoutingModule,
    ScrollingModule,
    MatIconModule,
    FormsModule,
  ]
})
export class MyChatModule { }
