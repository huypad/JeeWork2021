import { NgModule } from '@angular/core';
import { Routes, RouterModule } from '@angular/router';
import { DetailMyChatComponent } from './detail-my-chat/detail-my-chat.component';
import { MyChatComponent } from './my-chat/my-chat.component';
import { SliderChatComponent } from './slider-chat/slider-chat.component';

const routes: Routes = [

  {path: '', component: MyChatComponent,
  children: [
 
    {
      path: '',
      component:    SliderChatComponent,
    },
    {
      path: ':id',
      component: DetailMyChatComponent,
    }
  ]
}

];

@NgModule({
  imports: [RouterModule.forChild(routes)],
  exports: [RouterModule]
})
export class MyChatRoutingModule { }
