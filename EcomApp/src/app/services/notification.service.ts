import { Injectable, signal } from '@angular/core';

export interface Notification {
  message: string;
  type: 'success' | 'error';
  id: number;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  notifications = signal<Notification[]>([]);
  private nextId = 0;

  showSuccess(message: string): void {
    this.add(message, 'success');
  }

  showError(message: string): void {
    this.add(message, 'error');
  }

  dismiss(id: number): void {
    this.notifications.update(n => n.filter(x => x.id !== id));
  }

  private add(message: string, type: 'success' | 'error'): void {
    const id = this.nextId++;
    this.notifications.update(n => [...n, { message, type, id }]);
    setTimeout(() => this.dismiss(id), 3000);
  }
}
