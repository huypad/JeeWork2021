import { Pipe, PipeTransform } from '@angular/core';



@Pipe({
    name:'allow_update_status'
})

export class AllowUpdateStatus implements PipeTransform{
    transform(value : any):any {
        if(value)
            return value.filter(x=>x.allow_update);
        return value;
    }
 
}