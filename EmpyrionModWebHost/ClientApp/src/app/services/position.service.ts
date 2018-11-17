import { Injectable, Input } from '@angular/core';
import { PositionModel } from '../model/player-model';

@Injectable({
  providedIn: 'root'
})
export class PositionService {
  private position: PositionModel;

  constructor() { }

  get CurrentPosition() {
    return this.position;
  }

  @Input() set CurrentPosition(aPosition: PositionModel) {
    this.position = aPosition;
  }

}
