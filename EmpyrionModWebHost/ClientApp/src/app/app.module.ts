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
import { PlayerComponent } from './player/player.component';
import { SysteminfoComponent } from './systeminfo/systeminfo.component';
import { ChatListComponent } from './chat-list/chat-list.component';

@NgModule({
  declarations: [
    AppComponent,
    ChatComponent,
    ActivePlayfieldsComponent,
    PlayerComponent,
    SysteminfoComponent,
    ChatListComponent
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
