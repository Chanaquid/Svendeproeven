import { Injectable } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { AuthService } from './authService';
import { environment } from '../../environments/environment';

@Injectable({ providedIn: 'root' })
export class LoanChatHubService {
  private connection: signalR.HubConnection;

  constructor(private authService: AuthService) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl(`${environment.baseUrl}/hubs/loan-chat`, {
        accessTokenFactory: () => this.authService.getToken() ?? '',
      })
      .withAutomaticReconnect()
      .build();
  }

  async start(): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Disconnected) {
      await this.connection.start();
    }
  }

  async stop(): Promise<void> {
    try {
      if (this.connection.state !== signalR.HubConnectionState.Disconnected) {
        await this.connection.stop();
      }
    } catch {
      //Connection already closed
    }
  }

  async joinLoan(loanId: number): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('JoinLoan', loanId);
    }
  }

  async leaveLoan(loanId: number): Promise<void> {
    if (this.connection.state === signalR.HubConnectionState.Connected) {
      await this.connection.invoke('LeaveLoan', loanId);
    }
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

  off(): void {
    this.connection.off('ReceiveMessage');
    this.connection.off('MessageRead');
    this.connection.off('Error');
  }
}