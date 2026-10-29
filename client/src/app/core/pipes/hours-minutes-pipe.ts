import { Pipe,PipeTransform } from "@angular/core";
@Pipe({name:'hm'})
export class HoursMinutesPipe implements PipeTransform{
    transform(hours: number | null | undefined):string {
        if (hours==null) return '-';
        const totalMinutes=Math.round(hours*60);
        const h=Math.floor(totalMinutes/60);
        const m=totalMinutes%60;
        return `${h}h ${m}m`;
        
    }
}