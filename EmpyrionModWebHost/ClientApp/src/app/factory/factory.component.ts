import { Component, OnInit, ViewChild } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { MatMenu } from '@angular/material';
import { PlayerModel } from '../model/player-model';
import { PlayerService } from '../services/player.service';

@Component({
  selector: 'app-factory',
  templateUrl: './factory.component.html',
  styleUrls: ['./factory.component.less']
})
export class FactoryComponent implements OnInit {
  @ViewChild(MatMenu) contextMenu: MatMenu;
  Player: PlayerModel;

  constructor(
    private http: HttpClient,
    private mPlayerService: PlayerService,
  ) { }

  ngOnInit() {
    this.mPlayerService.GetCurrentPlayer().subscribe(P => this.Player = P);
  }

}
