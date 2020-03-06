import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule, ReactiveFormsModule } from '@angular/forms'; // <-- NgModel lives here
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { MatNativeDateModule } from '@angular/material/core';
import { BrowserAnimationsModule } from '@angular/platform-browser/animations';
import { MaterialModule } from './material-module';
import { HttpClientModule, HTTP_INTERCEPTORS } from '@angular/common/http';

import { OwlDateTimeModule, OwlNativeDateTimeModule } from 'ng-pick-datetime';
import { OWL_DATE_TIME_LOCALE } from 'ng-pick-datetime';
import { NgJsonEditorModule } from 'ang-jsoneditor';

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
import { StructuresListComponent } from './structures-list/structures-list.component';
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
import { FactionSelectDialogComponent, FactionSelectDialogContentComponent } from './faction-select-dialog/faction-select-dialog.component';
import { RestoreManagerComponent } from './restore-manager/restore-manager.component';
import { PlayfieldViewComponent } from './playfield-view/playfield-view.component';
import { EntitiesComponent } from './entities/entities.component';
import { FileUploadComponent } from './file-upload/file-upload.component';
import { HistoryBookOfComponent } from './history-book-of/history-book-of.component';
import { GalaxyMapComponent } from './galaxy-map/galaxy-map.component';
import { ThreeJsModule } from './three-js/three-js.module';
import { PlayerNoteComponent } from './player-note/player-note.component';
import { PlayfieldPlanetview3dComponent } from './playfield-planetview3d/playfield-planetview3d.component';
import { PlayfieldSpaceview3dComponent } from './playfield-spaceview3d/playfield-spaceview3d.component';
import { ModConfigurationComponent } from './mod-configuration/mod-configuration.component';
import { RestoreFactoryItemsComponent } from './restore-factoryitems/restore-factoryitems.component';

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
    StructuresListComponent,
    RestoreComponent,
    ServerComponent,
    RestoreFactoryItemsComponent,
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
    YesNoDialogComponent, YesNoDialogContentComponent,
    FactionSelectDialogComponent, FactionSelectDialogContentComponent,
    RestoreManagerComponent,
    PlayfieldViewComponent,
    EntitiesComponent,
    FileUploadComponent,
    HistoryBookOfComponent,
    GalaxyMapComponent,
    PlayerNoteComponent,
    PlayfieldPlanetview3dComponent,
    PlayfieldSpaceview3dComponent,
    ModConfigurationComponent
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
    AppRoutingModule,
    OwlDateTimeModule,
    OwlNativeDateTimeModule,
    ThreeJsModule,
    NgJsonEditorModule,
  ],
  entryComponents: [
    WarpDialogComponent,          WarpDialogContentComponent,
    SelectItemDialogComponent,    SelectItemDialogContentComponent,
    YesNoDialogComponent,         YesNoDialogContentComponent,
    FactionSelectDialogComponent, FactionSelectDialogContentComponent,
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: TokenInterceptor, multi: true },
    { provide: OWL_DATE_TIME_LOCALE, useValue: 'de' },
  ],
  bootstrap: [AppComponent]
})
export class AppModule {
  ngDoBootstrap(app) { }
}


