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

import { JwtInterceptor, ErrorInterceptor, fakeBackendProvider } from './_helpers';
import { LoginComponent } from './login/login.component';
import { HomeComponent } from './home/home.component';
import { TokenInterceptor } from './_helpers/TokenInterceptor';
import { UserManagerComponent } from './user-manager/user-manager.component';
import { PlayerWarpDialogComponent, PlayerWarpDialogContentComponent } from './player-warp-dialog/player-warp-dialog.component';
import { ItemEditComponent } from './item-edit/item-edit.component';
import { IntegersOnlyDirective } from './_helpers/IntegersOnly';


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
    PlayerWarpDialogComponent,
    PlayerWarpDialogContentComponent,
    ItemEditComponent,
    IntegersOnlyDirective
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
    PlayerWarpDialogComponent,
    PlayerWarpDialogContentComponent
  ],
  providers: [
    { provide: HTTP_INTERCEPTORS, useClass: JwtInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: ErrorInterceptor, multi: true },
    { provide: HTTP_INTERCEPTORS, useClass: TokenInterceptor, multi: true },
    // provider used to create fake backend
    fakeBackendProvider
  ],
  bootstrap: [AppComponent]
})
export class AppModule {
  ngDoBootstrap(app) { }
}
