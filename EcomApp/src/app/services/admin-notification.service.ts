import { inject, Injectable, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { API_URL } from '../utils/api-config';


export interface AdminNotificationItem {
  id: number;
  message: string;
  type: string;
  orderId?: number;
  isRead: boolean;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class AdminNotificationService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = API_URL + '/notifications';

  unreadCount = signal(0);
  notifications = signal<AdminNotificationItem[]>([]);
  showDropdown = signal(false);

  loadCount(): void {
    this.http.get<{ count: number }>(`${this.apiUrl}/count`).subscribe({
      next: (res) => this.unreadCount.set(res.count)
    });
  }

  loadAll(): void {
    this.http.get<{ items: AdminNotificationItem[] }>(`${this.apiUrl}?pageSize=50`).subscribe({
      next: (res) => this.notifications.set(res.items)
    });
  }

  markRead(id: number): void {
    this.http.put(`${this.apiUrl}/${id}/read`, {}).subscribe({
      next: () => {
        this.notifications.update(list => list.map(n => n.id === id ? { ...n, isRead: true } : n));
        this.unreadCount.update(c => Math.max(0, c - 1));
      }
    });
  }

  markAllRead(): void {
    this.http.put(`${this.apiUrl}/read-all`, {}).subscribe({
      next: () => {
        this.notifications.update(list => list.map(n => ({ ...n, isRead: true })));
        this.unreadCount.set(0);
      }
    });
  }

  toggleDropdown(): void {
    this.showDropdown.update(v => !v);
    if (this.showDropdown()) {
      this.loadAll();
    }
  }

  getRelativeTime(dateStr: string): string {
    const diff = Date.now() - new Date(dateStr).getTime();
    const mins = Math.floor(diff / 60000);
    if (mins < 1) return 'Just now';
    if (mins < 60) return `${mins}m ago`;
    const hours = Math.floor(mins / 60);
    if (hours < 24) return `${hours}h ago`;
    const days = Math.floor(hours / 24);
    return `${days}d ago`;
  }
}
