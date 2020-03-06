import { Component, OnInit, Input, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';

export class YesNoData {
  title?: string;
  question?: string;
  result?: boolean;
}

@Component({
  selector: 'app-yes-no-dialog',
  templateUrl: './yes-no-dialog.component.html',
  styleUrls: ['./yes-no-dialog.component.less']
})
export class YesNoDialogComponent  {
  constructor(public dialog: MatDialog) { }

  openDialog(aQuestion: YesNoData) {
    const dialogRef = this.dialog.open(YesNoDialogContentComponent, {
      data: { YesNoData: aQuestion }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log(`Dialog result: ${result}`);
    });

    return dialogRef;
  }
}

@Component({
  selector: 'app-yes-no-dialog-content',
  templateUrl: 'yes-no-dialog-content.component.html',
  styleUrls: ['./yes-no-dialog.component.less']
})
export class YesNoDialogContentComponent {
  @Input() YesNoQuestion: YesNoData = {};

  constructor(
    public dialogRef: MatDialogRef<YesNoDialogContentComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any) {
    this.YesNoQuestion = Object.assign({}, data.YesNoData);
  }

  Yes() {
    this.YesNoQuestion.result = true;
    this.dialogRef.close(this.YesNoQuestion);
  }

}
