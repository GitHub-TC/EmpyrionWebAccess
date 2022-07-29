import { Injectable, Input } from '@angular/core';
import { PositionModel } from '../model/player-model';

@Injectable({
  providedIn: 'root'
})
export class PositionService {
  private positionStack: PositionModel[] = [];
  private position     : PositionModel;

  constructor() { }

  get CurrentPosition() {
    return this.position;
  }

  get CurrentPositionStack() {
    return this.positionStack;
  }

  @Input() set CurrentPosition(aPosition: PositionModel) {
    this.position = aPosition;

    if (!this.positionStack.find(pos => pos == aPosition)) {
      this.positionStack = this.positionStack.slice(0, 4);
      this.positionStack.push(aPosition);
    }
  }

}
