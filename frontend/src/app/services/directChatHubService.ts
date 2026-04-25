import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './authService';

@Injectable({
  providedIn: 'root',
})
export class DirectChatHubService {
  private connection: signalR.HubConnection;

  constructor(private authService: AuthService) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7183/hubs/direct-chat', {
        accessTokenFactory: () => this.authService.getToken() ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000]) // retry intervals ms
      .build();
  }

  async startConnection(): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Connected ||
        this.connection.state === signalR.HubConnectionState.Connecting) return;
    await this.connection.start();
  }

  async stopConnection(): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Disconnected) return;
    await this.connection.stop();
  }

  async joinConversation(conversationId: number): Promise<void> {
    await this.connection.invoke('JoinConversation', conversationId);
  }

  async leaveConversation(conversationId: number): Promise<void> {
    await this.connection.invoke('LeaveConversation', conversationId);
  }

  onReceiveMessage(callback: (message: any) => void): void {
    this.connection.on('ReceiveMessage', callback);
  }

  onMessageRead(callback: (messageId: number) => void): void {
    this.connection.on('MessageRead', callback);
  }

  onError(callback: (error: string) => void): void {
    this.connection.on('Error', callback);
  }

  // #4 — expose disconnection event for reconnect logic
  onDisconnected(callback: () => void): void {
    this.connection.onclose(() => callback());
    this.connection.onreconnecting(() => callback());
  }

  offReceiveMessage(): void {
    this.connection.off('ReceiveMessage');
  }

  onConversationUpdated(callback: (update: any) => void): void {
    this.connection.on('ConversationUpdated', callback);
  }

  off(): void {
    this.connection.off('ReceiveMessage');
    this.connection.off('MessageRead');
    this.connection.off('Error');
    this.connection.off('ConversationUpdated');  // ← add this
  }
  get state(): signalR.HubConnectionState {
    return this.connection.state;
  }
}