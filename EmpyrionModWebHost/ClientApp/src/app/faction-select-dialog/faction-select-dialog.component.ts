import { Component, OnInit, Input, Inject } from '@angular/core';
import { MatDialog, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material';
import { FormControl } from '@angular/forms';
import { FactionService } from '../services/faction.service';
import { FactionModel } from '../model/faction-model';
import { Observable, BehaviorSubject } from 'rxjs';
import { startWith, map } from 'rxjs/operators';

@Component({
  selector: 'app-faction-select-dialog',
  templateUrl: './faction-select-dialog.component.html',
  styleUrls: ['./faction-select-dialog.component.less']
})
export class FactionSelectDialogComponent  {
  constructor(public dialog: MatDialog) { }

  openDialog(aQuestion: string) {
    const dialogRef = this.dialog.open(FactionSelectDialogContentComponent, {
      data: { Question: aQuestion }
    });

    dialogRef.afterClosed().subscribe(result => {
      console.log(`Dialog result: ${result}`);
    });

    return dialogRef;
  }
}

@Component({
  selector: 'app-faction-select-dialog-content',
  templateUrl: 'faction-select-dialog-content.component.html',
  styleUrls: ['./faction-select-dialog.component.less']
})
export class FactionSelectDialogContentComponent {
  SelectedFaction = new FormControl();
  FactionSelectQuestion: string;
  Factions: FactionModel[];

  private filteredFactions: BehaviorSubject<FactionModel[]> = new BehaviorSubject(this.Factions);
  public readonly filteredFactionsObservable: Observable<FactionModel[]> = this.filteredFactions.asObservable();

  constructor(
    private mFactionService: FactionService,
    public dialogRef: MatDialogRef<FactionSelectDialogContentComponent>,
    @Inject(MAT_DIALOG_DATA) public data: any) {
    this.FactionSelectQuestion = data.Question;
    mFactionService.GetFactions().subscribe(F => {
      this.Factions = F;
      this.Factions.push({ Abbrev: "Zrx", Name: "Zirax"   });
      this.Factions.push({ Abbrev: "Tal", Name: "Talons"  });
      this.Factions.push({ Abbrev: "Pol", Name: "Polaris" });
      this.Factions.push({ Abbrev: "Neu", Name: "Neutral" });
    });
  }

  ngOnInit() {
    this.SelectedFaction.valueChanges.subscribe(F => {
      this.filteredFactions.next(this.Factions.filter(option => option.Abbrev.toLowerCase().startsWith(F.toLowerCase())));
    });
  }

  Confirm() {
    this.dialogRef.close(this.SelectedFaction.value);
  }

}
