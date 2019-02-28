import { Component, OnInit, Input } from '@angular/core';

import { ChatService } from '../services/chat.service'
import { ChatListComponent } from '../chat-list/chat-list.component';
import { AuthenticationService } from '../services/authentication.service';
import { User, UserRole } from '../model/user';
import { RoleService } from '../services/role.service';

@Component({
  selector: 'app-chat',
  templateUrl: './chat.component.html',
  styleUrls: ['./chat.component.less']
})
export class ChatComponent implements OnInit {
  message: string = "";
  chatTarget: string = "All";
  chatToAll: boolean = true;
  chatAsUser: boolean = true;
  @Input() chatList: ChatListComponent;
  currentUser: User;
  UserRole = UserRole;

  constructor(
    private authenticationService: AuthenticationService,
    public ChatService: ChatService,
    public role: RoleService,
  ) {
    this.authenticationService.currentUser.subscribe(x => this.currentUser = x);
  }

  ngOnInit() {
  }

  SendMessage() {
    this.ChatService.SendMessage(this.chatAsUser ? this.currentUser.username : null, this.message);
    this.message = "";
  }
}
