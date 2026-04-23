import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './authService';

@Injectable({
  providedIn: 'root',
})
export class NotificationHubService {
  private connection: signalR.HubConnection;

  constructor(private authService: AuthService) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7183/hubs/notifications', {
        accessTokenFactory: () => this.authService.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();
  }

  async start(): Promise<void> {
    await this.connection.start();
  }

  async stop(): Promise<void> {
    await this.connection.stop();
  }

  onReceiveNotification(callback: (notification: any) => void): void {
    this.connection.on('ReceiveNotification', callback);
  }

  onUnreadCountUpdated(callback: (count: number) => void): void {
    this.connection.on('UnreadCountUpdated', callback);
  }

  onNewMessageNotification(callback: (data: { loanId: number; itemTitle: string; from: string }) => void): void {
    this.connection.on('NewMessageNotification', callback);
  }

  off(): void {
    this.connection.off('ReceiveNotification');
    this.connection.off('UnreadCountUpdated');
    this.connection.off('NewMessageNotification');
  }
}