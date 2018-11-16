import { Pipe, PipeTransform } from '@angular/core';
import { FactionService } from '../services/faction.service';

@Pipe({
  name: 'faction'
})
export class FactionPipe implements PipeTransform {

  constructor(private mFactionService: FactionService) {
  }

  transform(value: number, args?: any): any {
    return this.mFactionService.GetFaction(value)[args ? args : "name"];
  }

}
