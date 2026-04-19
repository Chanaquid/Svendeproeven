import * as signalR from '@microsoft/signalr';

export class NotificationHubService {
  private connection: signalR.HubConnection;

  constructor(token: string) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7183/hubs/notifications', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .build();
  }

  // Start the connection
  async start(): Promise<void> {
    await this.connection.start();
  }

  // Stop the connection
  async stop(): Promise<void> {
    await this.connection.stop();
  }

  // Listen for incoming notifications
  // Server calls: Clients.Group($"user_{userId}").SendAsync("ReceiveNotification", notif)
  onReceiveNotification(callback: (notification: any) => void): void {
    this.connection.on('ReceiveNotification', callback);
  }

  // Listen for unread count updates
  // Server calls: Clients.Group($"user_{userId}").SendAsync("UnreadCountUpdated", count)
  onUnreadCountUpdated(callback: (count: number) => void): void {
    this.connection.on('UnreadCountUpdated', callback);
  }

  // Remove all listeners (call on component destroy)
  off(): void {
    this.connection.off('ReceiveNotification');
    this.connection.off('UnreadCountUpdated');
  }
}