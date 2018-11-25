import { Component, OnInit, Inject, Input } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material';
import { HttpClient } from '@angular/common/http';
import { MAT_DIALOG_DATA } from '@angular/material';
import { PositionService } from '../services/position.service';
import { PositionModel } from '../model/player-model';

@Component({
  selector: 'app-player-warp-dialog',
  templateUrl: './player-warp-dialog.component.html',
  styleUrls: ['./player-warp-dialog.component.less']
})
export class PlayerWarpDialogComponent implements OnInit {
  @Input() WarpData: PositionModel;

  constructor(private http: HttpClient, public dialog: MatDialog) {}

  ngOnInit() {
  }

  openDialog() {
    const dialogRef = this.dialog.open(PlayerWarpDialogContentComponent, {
      data: { WarpData: this.WarpData }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log(`Dialog result: ${result}`);
    });
  }
}

@Component({
  selector: 'app-player-warp-dialog-content',
  templateUrl: 'player-warp-dialog-content.component.html',
  styleUrls: ['./player-warp-dialog.component.less']
})
export class PlayerWarpDialogContentComponent implements OnInit {
  Playfields: string[];
  @Input() WarpData: PositionModel;
  error: any;

  constructor(private http: HttpClient,
    public mPositionService: PositionService,
    public dialogRef: MatDialogRef<PlayerWarpDialogContentComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any) {
    this.WarpData = JSON.parse(JSON.stringify(data.WarpData));
    if (!this.WarpData.pos) this.WarpData.pos = { x: 0, y: 0, z: 0 };
    if (!this.WarpData.rot) this.WarpData.rot = { x: 0, y: 0, z: 0 };
  }

  ngOnInit() {
    this.http.get<string[]>("gameplay/GetAllPlayfieldNames")
      .pipe()
      .subscribe(
        L => this.Playfields = L,
        error => this.error = error // error path
      );
  }

  setToZeroPosition() {
    this.WarpData.pos = { x: 0, y: 0, z: 0 };
    this.WarpData.rot = { x: 0, y: 0, z: 0 };
  }

  copyPosition() {
    this.WarpData = JSON.parse(JSON.stringify(this.mPositionService.CurrentPosition));
  }

  execWarp() {
    this.http.post('gameplay/PlayerWarpTo/' + this.WarpData.entityId,
      { Playfield: this.WarpData.playfield, PosX: this.WarpData.pos.x, PosY: this.WarpData.pos.y, PosZ: this.WarpData.pos.z })
      .pipe()
      .subscribe(
        R => { },
        error => this.error = error // error path
      );

    this.dialogRef.close();
  }

}
