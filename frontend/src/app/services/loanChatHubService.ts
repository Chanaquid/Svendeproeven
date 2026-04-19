import * as signalR from '@microsoft/signalr';

export class LoanChatHubService {
  private connection: signalR.HubConnection;

  constructor(token: string) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('https://localhost:7183/hubs/loan-chat', {
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

  // Join a loan group — server checks that the user is a party to the loan
  // Server method: JoinLoan(int loanId)
  async joinLoan(loanId: number): Promise<void> {
    await this.connection.invoke('JoinLoan', loanId);
  }

  // Leave a loan group
  // Server method: LeaveLoan(int loanId)
  async leaveLoan(loanId: number): Promise<void> {
    await this.connection.invoke('LeaveLoan', loanId);
  }

  // Listen for new loan messages
  // Server calls: Clients.Group($"loan_{loanId}").SendAsync("ReceiveMessage", message)
  onReceiveMessage(callback: (message: any) => void): void {
    this.connection.on('ReceiveMessage', callback);
  }

  // Listen for read receipts
  // Server calls: Clients.Group($"loan_{loanId}").SendAsync("MessageRead", messageId)
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