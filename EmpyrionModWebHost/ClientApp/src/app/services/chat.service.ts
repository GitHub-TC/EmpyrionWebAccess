import { Injectable, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, from, BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators'
import { HubConnection } from '@aspnet/signalr';

import { ChatModel } from '../model/chat-model'
import { CHAT } from '../model/chat-mock';
import { PlayerModel } from '../model/player-model';
import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';

@Injectable({
  providedIn: 'root'
})
export class ChatService implements OnInit {
  public hubConnection: HubConnection;

  private mMessages: ChatModel[] = [];// CHAT;

  private messages: BehaviorSubject<ChatModel[]> = new BehaviorSubject(this.mMessages);
  public readonly messagesObservable: Observable<ChatModel[]> = this.messages.asObservable();

  private mChatToPlayer: PlayerModel;
    error: any;
  
  constructor(private http: HttpClient, private builder: AuthHubConnectionBuilder) {
    this.hubConnection = builder.withAuthUrl('/hubs/chat').build();

    // message coming from the server
    this.hubConnection.on("Send", (message) => {
      this.messages.next(this.messages.getValue().concat(JSON.parse(message)));
    });

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }
  }

  ngOnInit(): void {
  }

  GetMessages(): Observable<ChatModel[]> {
    let locationsSubscription = this.http.get<ODataResponse<ChatModel[]>>("odata/Chats")
      .pipe(map(S => S.value))
      .subscribe(
        M => this.messages.next(this.mMessages = M),
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 10000);

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
