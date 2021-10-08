import { Pipe, PipeTransform } from '@angular/core';
// import * as moment from 'moment';
import * as moment from 'moment-timezone';


@Pipe({
  name: 'TimeMess',
  pure: false,
})
export class TimeMessPipe implements PipeTransform {
 
  transform(date: string, format: string = 'dddd,HH:mm:ss'): string {
    var tz =moment.tz.guess()
   
     let d=date+'Z'
     var dec = moment(d);
     return  dec.tz(tz).format('dddd,HH:mm');
    // return moment(d).format("HH:mm  ");
  }
}