import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';

export interface Toast {
  id: number;
  message: string;
  type: 'error' | 'success' | 'info' | 'warning';
}

@Injectable({ providedIn: 'root' })
export class ToastService {
  private toasts$ = new BehaviorSubject<Toast[]>([]);
  toasts = this.toasts$.asObservable();
  private counter = 0;

  show(message: string, type: Toast['type'] = 'info', duration = 4000) {
    const id = ++this.counter;
    const current = this.toasts$.getValue();
    this.toasts$.next([...current, { id, message, type }]);
    setTimeout(() => this.dismiss(id), duration);
  }

  dismiss(id: number) {
    this.toasts$.next(this.toasts$.getValue().filter(t => t.id !== id));
  }
}