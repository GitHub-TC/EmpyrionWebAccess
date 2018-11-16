import { Component, OnInit, Input } from '@angular/core';

import { ChatModel } from '../model/chat-model'

import { ChatService } from '../services/chat.service'

@Component({
  selector: 'app-chat-list',
  templateUrl: './chat-list.component.html',
  styleUrls: ['./chat-list.component.less'],
})
export class ChatListComponent implements OnInit {
  displayedColumns = ['type', 'timestamp', 'faction', 'playerName', 'message'];
  messages: ChatModel[];

  message: string;
  autoscroll: boolean = true;

  constructor(private mChatService: ChatService) {
  }

  ngOnInit() {
    this.mChatService.GetMessages().subscribe(messages => {
      this.messages = messages;
    });
  }

  getLineClass(aMsg: ChatModel) {
    return aMsg.mark;
  }

}
