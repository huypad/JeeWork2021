import {environment} from './../../../../environments/environment';
import {Pipe, PipeTransform} from '@angular/core';
import * as moment from 'moment';

const isServer = environment.SERVERLIVE;

@Pipe({
    name: 'timezone'
})
export class TimezonePipe implements PipeTransform {

    transform(value: any, format: string = ''): any {

        if (value) {
            return this.convertDate(this.DMYtoMDY(value), format);
        } else {
            return '';
        }
    }

    DMYtoMDY(value) {
        const cutstring = value.split('/');
        if (cutstring.length === 3) {
            return cutstring[1] + '/' + cutstring[0] + '/' + cutstring[2];
        }
        return value;
    }

    convertDate(d: any, format: string = '') {
        let fm = 'DD/MM/YYYY HH:mm';
        if (format !== ''){
            fm = format;
        }
        if (moment(d + 'z').format(fm) === 'Invalid date') {
            return d;
        }
        if (!isServer) {
            return moment(d).format(fm);
        }
        return moment(d + 'z').format(fm);
    }

}
