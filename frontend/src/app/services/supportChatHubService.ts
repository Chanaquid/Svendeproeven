import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './authService';

@Injectable({
  providedIn: 'root',
})
export class SupportChatHubService {
  private connection: signalR.HubConnection;

  constructor(private authService: AuthService) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7183/hubs/support-chat', {
        accessTokenFactory: () => this.authService.getToken() ?? '',
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();
  }

  async startConnection(): Promise<void> {
    if (
      this.connection.state === signalR.HubConnectionState.Connected ||
      this.connection.state === signalR.HubConnectionState.Connecting
    ) return;
    await this.connection.start();
  }

  async stopConnection(): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Disconnected) return;
    await this.connection.stop();
  }

  async joinThread(threadId: number): Promise<void> {
    await this.connection.invoke('JoinThread', threadId);
  }

  async leaveThread(threadId: number): Promise<void> {
    await this.connection.invoke('LeaveThread', threadId);
  }

  onReceiveMessage(callback: (message: any) => void): void {
    this.connection.on('ReceiveMessage', callback);
  }

  onThreadUpdated(callback: (update: any) => void): void {
    this.connection.on('ThreadUpdated', callback);
  }

  onError(callback: (error: string) => void): void {
    this.connection.on('Error', callback);
  }

  onDisconnected(callback: () => void): void {
    this.connection.onclose(() => callback());
    this.connection.onreconnecting(() => callback());
  }

  off(): void {
    this.connection.off('ReceiveMessage');
    this.connection.off('ThreadUpdated');
    this.connection.off('Error');
  }

  get state(): signalR.HubConnectionState {
    return this.connection.state;
  }
}