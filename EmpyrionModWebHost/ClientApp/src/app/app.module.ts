import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms'; // <-- NgModel lives here
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { MatNativeDateModule } from '@angular/material';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MaterialModule } from './material-module';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { ChatComponent } from './chat/chat.component';
import { ActivePlayfieldsComponent } from './active-playfields/active-playfields.component';
import { SysteminfoComponent } from './systeminfo/systeminfo.component';
import { ChatListComponent } from './chat-list/chat-list.component';
import { PlayerBackpackComponent } from './player-backpack/player-backpack.component';
import { PlayerDetailsComponent } from './player-details/player-details.component';
import { PlayerListComponent } from './player-list/player-list.component';

import { JwtInterceptor, ErrorInterceptor } from './_helpers';
import { LoginComponent } from './login/login.component';
import { HomeComponent } from './home/home.component';
import { TokenInterceptor } from './_helpers/TokenInterceptor';
import { UserManagerComponent } from './user-manager/user-manager.component';
import { WarpDialogComponent, WarpDialogContentComponent } from './warp-dialog/warp-dialog.component';
import { ItemEditComponent } from './item-edit/item-edit.component';
import { IntegersOnlyDirective } from './_helpers/IntegersOnly';
import { StructuresComponent } from './structures/structures.component';
import { RestoreComponent } from './restore/restore.component';
import { ServerComponent } from './server/server.component';
import { RestoreBackpackComponent } from './restore-backpack/restore-backpack.component';
import { RestoreStructureComponent } from './restore-structure/restore-structure.component';
import { RestorePlayerComponent } from './restore-player/restore-player.component';
import { RestorePlayfieldComponent } from './restore-playfield/restore-playfield.component';
import { FactoryComponent } from './factory/factory.component';
import { ServerSettingsComponent } from './server-settings/server-settings.component';
import { ServerModManagerComponent } from './server-mod-manager/server-mod-manager.component';
import { SelectItemDialogComponent, SelectItemDialogContentComponent } from './select-item-dialog/select-item-dialog.component';
import { TimetableComponent } from './timetable/timetable.component';
import { KeysPipe } from './pipes/keys.pipe';
import { YesNoDialogComponent, YesNoDialogContentComponent } from './yes-no-dialog/yes-no-dialog.component';

@NgModule({
  declarations: [
    AppComponent,
    HomeComponent,
    LoginComponent,
    ChatComponent,
    ActivePlayfieldsComponent,
    SysteminfoComponent,
    ChatListComponent,
    PlayerBackpackComponent,
    PlayerDetailsComponent,
    PlayerListComponent,
    UserManagerComponent,
    WarpDialogComponent,
    WarpDialogContentComponent,
    ItemEditComponent,
    IntegersOnlyDirective,
    StructuresComponent,
    RestoreComponent,
    ServerComponent,
    RestoreBackpackComponent,
    RestoreStructureComponent,
    RestorePlayerComponent,
    RestorePlayfieldComponent,
    FactoryComponent,
    ServerSettingsComponent,
    ServerModManagerComponent,
    SelectItemDialogComponent,
    SelectItemDialogContentComponent,
    TimetableComponent,
    KeysPipe,
    YesNoDialogComponent,
    YesNoDialogContentComponent
  ],
  imports: [
    BrowserModule,
    FormsModule,
    ReactiveFormsModule,
    NgbModule,
    MaterialModule,
    MatNativeDateModule,
    HttpClientModule,
    BrowserAnimationsModule,
    AppRoutingModule
  ],
  entryComponents: [
    WarpDialogComponent,
    WarpDialogContentComponent,
    SelectItemDialogComponent,
    SelectItemDialogContentComponent,
    YesNoDialogComponent,
    YesNoDialogContentComponent
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: TokenInterceptor, multi: true },
  ],
  bootstrap: [AppComponent]
})
export class AppModule {
  ngDoBootstrap(app) { }
}


