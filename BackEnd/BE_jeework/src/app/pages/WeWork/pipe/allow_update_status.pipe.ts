import { Pipe, PipeTransform } from '@angular/core';



@Pipe({
    name:'allow_update_status'
})

export class AllowUpdateStatus implements PipeTransform{
    transform(value : any):any {
        var newValue = value.filter(x=>x.allow_update);
        return newValue;
    }
 
}