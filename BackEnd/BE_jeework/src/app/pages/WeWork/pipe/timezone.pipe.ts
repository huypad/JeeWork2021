import { environment } from './../../../../environments/environment';
import { Pipe, PipeTransform } from '@angular/core';
import * as moment from 'moment';
const isServer = environment.SERVERLIVE;
@Pipe({
  name: 'timezone'
})
export class TimezonePipe implements PipeTransform {

  transform(value: any): any {  
    
    if(value){
      // console.log(value);
      // console.log(this.DMYtoMDY(value));
      return this.convertDate(this.DMYtoMDY(value));
    }
    else{
      return '';
    }
  }

  DMYtoMDY(value){
    var cutstring = value.split('/');
    // console.log('b1:',cutstring);
    // console.log('b2:',cutstring[1]+'/'+cutstring[0]+'/'+cutstring[2]);
    if(cutstring.length == 3){ 
      return cutstring[1]+'/'+cutstring[0]+'/'+cutstring[2];
    }
    return value;
  }
  
  // convertDate(d:any){  
  //   if(moment(moment(d).format("DD/MM/YYYY HH:mm") + 'z').format("DD/MM/YYYY HH:mm") == 'Invalid date'){
  //     return d;
  //   }
	// 	return moment(d + 'z');
	// }
  convertDate(d:any){  
    if(moment(d + 'z').format("DD/MM/YYYY HH:mm") == 'Invalid date'){
      return d;
    }
    if(!isServer){
      return moment(d).format("DD/MM/YYYY HH:mm");
    }
		return moment(d + 'z').format("DD/MM/YYYY HH:mm");
	}

}
