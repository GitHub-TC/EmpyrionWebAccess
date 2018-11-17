import { Pipe, PipeTransform } from '@angular/core';
import { FactionService } from '../services/faction.service';

@Pipe({
  name: 'faction'
})
export class FactionPipe implements PipeTransform {

  constructor(private mFactionService: FactionService) {
  }

  transform(value: number, args?: any): any {
    let fac = this.mFactionService.GetFaction(value);
    return fac ? fac[args ? args : "name"] : null;
  }

}
