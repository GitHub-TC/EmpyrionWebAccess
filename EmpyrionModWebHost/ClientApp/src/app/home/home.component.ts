import { Component, ViewChild, ElementRef, AfterViewInit } from '@angular/core';
import { first } from 'rxjs/operators';

import { AuthenticationService } from '../services/authentication.service';
import { User } from '../model/user';
import { MatTabChangeEvent, MatTab } from '@angular/material';
import { PlayerDetailsComponent } from '../player-details/player-details.component';
import { PlayerBackpackComponent } from '../player-backpack/player-backpack.component';
import { FactoryComponent } from '../factory/factory.component';

@Component({
  templateUrl: 'home.component.html',
  styleUrls: ['./home.component.less']
})
export class HomeComponent implements AfterViewInit {
  currentUser: User = { id: 0, username: null, password: null };
  CurrentDetailsTab: MatTab;
  CurrentComponent: any;
  @ViewChild(PlayerBackpackComponent) playerBackpackComponent;
  @ViewChild(PlayerDetailsComponent) playerDetailsComponent;
  @ViewChild(FactoryComponent) playerFactoryComponent;

  constructor(private authenticationService: AuthenticationService) {
  }

  ngOnInit() {
    this.authenticationService.currentUser.subscribe(x => this.currentUser = x);
  }

  ngAfterViewInit() {
    this.CurrentComponent = this.playerBackpackComponent;
  }

  public DetailsTabChanged(tabChangeEvent: MatTabChangeEvent): void {
    switch (tabChangeEvent.index) {
      case 0: this.CurrentComponent = this.playerBackpackComponent; break;
      case 1: this.CurrentComponent = this.playerDetailsComponent; break;
      case 2: this.CurrentComponent = this.playerFactoryComponent; break;
    }
  }

}
