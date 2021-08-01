import { TimezonePipe } from './timezone.pipe';
import { NgModule } from '@angular/core'; 

// @NgModule({
//   imports: [ TimezonePipe ],
//   providers: [ TimezonePipe ],
//   entryComponents: [
// 	  TimezonePipe
//   ],
//   declarations: [ TimezonePipe ],
//   exports: [ TimezonePipe ],
// })
// export class PipeModule {}

@NgModule({
  imports: [
    // dep modules
  ],
  declarations: [ 
    TimezonePipe
  ],
  exports: [
    TimezonePipe
  ]
})
export class ApplicationPipesModule {}
