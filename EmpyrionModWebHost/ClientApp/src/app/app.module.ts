import { BrowserModule } from '@angular/platform-browser';
import { NgModule } from '@angular/core';
import { FormsModule } from '@angular/forms'; // <-- NgModel lives here
import { NgbModule } from '@ng-bootstrap/ng-bootstrap';
import { MatNativeDateModule } from '@angular/material';
import { MaterialModule } from './material-module';

import { AppRoutingModule } from './app-routing.module';
import { AppComponent } from './app.component';
import { ChatComponent } from './chat/chat.component';
import { ActivePlayfieldsComponent } from './active-playfields/active-playfields.component';
import { SysteminfoComponent } from './systeminfo/systeminfo.component';
import { ChatListComponent } from './chat-list/chat-list.component';
import { PlayerBackpackComponent } from './player-backpack/player-backpack.component';
import { PlayerDetailsComponent } from './player-details/player-details.component';
import { PlayerListComponent } from './player-list/player-list.component';
import { FactionPipe } from './pipes/faction.pipe';

@NgModule({
  declarations: [
    AppComponent,
    ChatComponent,
    ActivePlayfieldsComponent,
    SysteminfoComponent,
    ChatListComponent,
    PlayerBackpackComponent,
    PlayerDetailsComponent,
    PlayerListComponent,
    FactionPipe
  ],
  imports: [
    BrowserModule,
    FormsModule,
    NgbModule,
    MaterialModule,
    MatNativeDateModule,
    AppRoutingModule
  ],
  providers: [],
  bootstrap: [AppComponent]
})
export class AppModule {
  ngDoBootstrap(app) { }
}
