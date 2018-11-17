import { Component, OnInit, Input, ViewChild, ElementRef } from '@angular/core';

import { ChatModel } from '../model/chat-model'

import { ChatService } from '../services/chat.service'
import { MatTable } from '@angular/material';
import { PlayerService } from '../services/player.service';

@Component({
  selector: 'app-chat-list',
  templateUrl: './chat-list.component.html',
  styleUrls: ['./chat-list.component.less'],
})
export class ChatListComponent implements OnInit {
  @ViewChild(MatTable, { read: ElementRef }) table: ElementRef;

  displayedColumns = ['type', 'timestamp', 'faction', 'playerName', 'message'];

  messages: ChatModel[];
  message: string;
  autoscroll: boolean = true;

  constructor(private mChatService: ChatService, private mPlayerService: PlayerService) {
  }

  ngOnInit() {
    this.mChatService.GetMessages().subscribe(messages => {
      this.messages = messages;

      if(this.autoscroll) setTimeout(() => this.table.nativeElement.scrollIntoView(false), 0);
    });
  }

  ChatTo(aMsg: ChatModel) {
    this.mChatService.ChatToPlayer(this.mPlayerService.GetPlayer(P => P.playerName == aMsg.playerName));
  }

  getLineClass(aMsg: ChatModel) {
    return aMsg.mark;
  }

}
