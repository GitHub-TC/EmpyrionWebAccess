import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, from, BehaviorSubject } from 'rxjs';
import { HubConnection, HubConnectionBuilder } from '@aspnet/signalr';

import { ChatModel } from '../model/chat-model'
import { CHAT } from '../model/chat-mock';
import { Player } from '@angular/core/src/render3/interfaces/player';
import { PlayerModel } from '../model/player-model';
import { SessionService } from './session.service';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  public hubConnection: HubConnection;

  private mMessages: ChatModel[] = CHAT;

  private messages: BehaviorSubject<ChatModel[]> = new BehaviorSubject(this.mMessages);
  public readonly messagesObservable: Observable<ChatModel[]> = this.messages.asObservable();

  private mChatToPlayer: PlayerModel;
  
  constructor(private http: HttpClient) {
    let builder = new HubConnectionBuilder();

    // as per setup in the startup.cs
    this.hubConnection = builder.withUrl('/hubs/chat').build();

    // message coming from the server
    this.hubConnection.on("Send", (message) => {
      this.messages.next(this.messages.getValue().concat(JSON.parse(message)));
    });

    // starting the connection
    this.hubConnection.start();
  }

  GetMessages(): Observable<ChatModel[]> {
    this.http.get<ODataResponse<ChatModel[]>>("odata/Chats")
      .map(S => S.value)
      .subscribe(M => this.messages.next(this.mMessages = M));

    return this.messagesObservable;
  }

  ChatToPlayer(aPlayer: PlayerModel) {
    this.mChatToPlayer = aPlayer;
  }

  get ChatTarget() {
    return this.mChatToPlayer ? "@" + this.mChatToPlayer.PlayerName : "All";
  }

  get ChatToAll() {
    return !this.mChatToPlayer;
  }

  set ChatToAll(aToAll: boolean) {
    this.mChatToPlayer = null;
  }

  SendMessage(aAsUser: string, aMessage: string): void {
    let chatTarget = null;
    if (this.mChatToPlayer) chatTarget = "p:" + this.mChatToPlayer.EntityId;

    this.hubConnection.invoke("SendMessage", chatTarget, aAsUser, aMessage);
  }
}
