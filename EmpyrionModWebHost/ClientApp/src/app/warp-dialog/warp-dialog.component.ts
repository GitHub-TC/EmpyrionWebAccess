import { Component, OnInit, Inject, Input } from '@angular/core';
import { MatDialog, MatDialogRef } from '@angular/material';
import { HttpClient } from '@angular/common/http';
import { MAT_DIALOG_DATA } from '@angular/material';
import { PositionService } from '../services/position.service';
import { PositionModel } from '../model/player-model';
import { PlayfieldService } from '../services/playfield.service';
import { PlayfieldModel } from '../model/playfield-model';

@Component({
  selector: 'app-warp-dialog',
  templateUrl: './warp-dialog.component.html',
  styleUrls: ['./warp-dialog.component.less']
})
export class WarpDialogComponent {
  @Input() WarpData: PositionModel;

  constructor(public dialog: MatDialog) { }

  openDialog() {
    const dialogRef = this.dialog.open(WarpDialogContentComponent, {
      data: { WarpData: this.WarpData }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log(`Dialog result: ${result}`);
    });
  }
}

@Component({
  selector: 'app-warp-dialog-content',
  templateUrl: 'warp-dialog-content.component.html',
  styleUrls: ['./warp-dialog.component.less']
})
export class WarpDialogContentComponent implements OnInit {
  @Input() WarpData: PositionModel;
  Playfields: PlayfieldModel[];
  error: any;

  constructor(private http: HttpClient,
    private mPlayfields: PlayfieldService,
    public mPositionService: PositionService,
    public dialogRef: MatDialogRef<WarpDialogContentComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any) {
    this.WarpData = JSON.parse(JSON.stringify(data.WarpData));
    if (!this.WarpData.pos) this.WarpData.pos = { x: 0, y: 0, z: 0 };
    if (!this.WarpData.rot) this.WarpData.rot = { x: 0, y: 0, z: 0 };
  }

  ngOnInit() {
    this.mPlayfields.PlayfieldNames.subscribe(PL => this.Playfields = PL);
  }

  setToZeroPosition() {
    this.WarpData.pos = { x: 0, y: 0, z: 0 };
    this.WarpData.rot = { x: 0, y: 0, z: 0 };
  }

  copyPosition() {
    let SaveWarpData = this.WarpData;
    this.WarpData = Object.assign({}, this.mPositionService.CurrentPosition);
    this.WarpData.description = SaveWarpData.description;
    this.WarpData.entityId    = SaveWarpData.entityId;
  }

  execWarp() {
    this.http.post('gameplay/WarpTo/' + this.WarpData.entityId,
      { Playfield: this.WarpData.playfield, PosX: this.WarpData.pos.x, PosY: this.WarpData.pos.y, PosZ: this.WarpData.pos.z })
      .pipe()
      .subscribe(
        R => { },
        error => this.error = error // error path
      );

    this.dialogRef.close();
  }

}
