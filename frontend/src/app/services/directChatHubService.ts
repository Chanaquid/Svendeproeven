import * as signalR from '@microsoft/signalr';

export class DirectChatHubService {
  private connection: signalR.HubConnection;

  constructor(token: string) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7183/hubs/direct-chat', {
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

  // Join a conversation group — server checks access before adding to group
  // Server method: JoinConversation(int conversationId)
  async joinConversation(conversationId: number): Promise<void> {
    await this.connection.invoke('JoinConversation', conversationId);
  }

  // Leave a conversation group
  // Server method: LeaveConversation(int conversationId)
  async leaveConversation(conversationId: number): Promise<void> {
    await this.connection.invoke('LeaveConversation', conversationId);
  }

  // Listen for new messages
  // Server calls: Clients.Group($"conversation_{id}").SendAsync("ReceiveMessage", message)
  onReceiveMessage(callback: (message: any) => void): void {
    this.connection.on('ReceiveMessage', callback);
  }

  // Listen for read receipts
  // Server calls: Clients.Group($"conversation_{id}").SendAsync("MessageRead", messageId)
  onMessageRead(callback: (messageId: number) => void): void {
    this.connection.on('MessageRead', callback);
  }

  // Listen for access errors returned by the server
  onError(callback: (error: string) => void): void {
    this.connection.on('Error', callback);
  }

  // Remove all listeners (call on component destroy)
  off(): void {
    this.connection.off('ReceiveMessage');
    this.connection.off('MessageRead');
    this.connection.off('Error');
  }
}