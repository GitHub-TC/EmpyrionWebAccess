import { Injectable, OnInit } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, of, from, BehaviorSubject } from 'rxjs';
import { map } from 'rxjs/operators'
import { HubConnection } from '@aspnet/signalr';

import { ChatModel } from '../model/chat-model'
import { CHAT } from '../model/chat-mock';
import { PlayerModel } from '../model/player-model';
import { AuthHubConnectionBuilder } from '../_helpers/AuthHubConnectionBuilder';
import { FactionModel } from '../model/faction-model';

@Injectable({
  providedIn: 'root'
})
export class ChatService {
  public hubConnection: HubConnection;
  mFilterServerMsg: boolean = true;

  private mMessages: ChatModel[] = [];// CHAT;
  private messages: BehaviorSubject<ChatModel[]> = new BehaviorSubject(this.mMessages);
  public readonly messagesObservable: Observable<ChatModel[]> = this.messages.asObservable();

  private mLastMessages: ChatModel[] = [];// CHAT;
  private lastMessages: BehaviorSubject<ChatModel[]> = new BehaviorSubject(this.mLastMessages);
  public readonly lastMessagesObservable: Observable<ChatModel[]> = this.lastMessages.asObservable();

  private mChatTo: string;
  private mChatToInfo: string;
    error: any;
  
  constructor(private http: HttpClient, private builder: AuthHubConnectionBuilder) {
    this.hubConnection = builder.withAuthUrl('/hubs/chat').build();

    // message coming from the server
    this.hubConnection.on("Send", (message) => {
      let Msg = JSON.parse(message);
      if (this.mFilterServerMsg && Msg.FactionName == "SERV") return;
      this.messages.next(this.messages.getValue().concat());
      this.lastMessages.next(this.lastMessages.getValue().concat(Msg));
    });

    // starting the connection
    try {
      this.hubConnection.start();
    } catch (Error) {
      this.error = Error;
    }
  }

  GetMessages(): Observable<ChatModel[]> {
    let locationsSubscription = this.http.get<ODataResponse<ChatModel[]>>("odata/Chats")
      .pipe(map(S => S.value))
      .subscribe(
        M => this.messages.next(this.mMessages = M),
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);

    return this.messagesObservable;
  }

  get filterServerMsg(): boolean {
    return this.mFilterServerMsg;
  }

  set filterServerMsg(aFilter: boolean) {
    this.mFilterServerMsg = aFilter;
    this.GetLastMessages();
  }

  GetLastMessages(): any {
    let locationsSubscription = this.http.get<ODataResponse<ChatModel[]>>("odata/Chats?$top=500&$orderby=Timestamp desc" +
      (this.mFilterServerMsg ? "&$filter=FactionName ne 'SERV'" : ""))
      .pipe(map(S => S.value))
      .subscribe(
        M => this.lastMessages.next(this.mLastMessages = M.reverse()),
        error => this.error = error // error path
      );
    // Stop listening for location after 10 seconds
    setTimeout(() => { locationsSubscription.unsubscribe(); }, 120000);

    return this.lastMessagesObservable;
  }

  ChatToPlayer(aPlayer: PlayerModel) {
    this.mChatToInfo = aPlayer.PlayerName;
    this.mChatTo     = "p:" + aPlayer.EntityId;
  }

  ChatToFaction(aFaction: FactionModel) {
    this.mChatToInfo = aFaction.Abbrev;
    this.mChatTo     = "f:" + aFaction.Abbrev;
  }

  get ChatTarget() {
    return this.mChatTo ? "@" + this.mChatToInfo : "All";
  }

  get ChatToAll() {
    return !this.mChatTo;
  }

  set ChatToAll(aToAll: boolean) {
    this.mChatTo = null;
  }

  SendMessage(aAsUser: string, aMessage: string): void {
    let chatTarget = null;
    let chatTargetHint = null;
    if (this.mChatTo) {
      chatTarget     = this.mChatTo;
      chatTargetHint = "@" + this.mChatToInfo + ": ";
    }

    this.hubConnection.invoke("SendMessage", chatTarget, chatTargetHint, aAsUser, aMessage);
  }
}
