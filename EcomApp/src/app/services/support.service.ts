import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { BotResponse, SupportConversation, SupportMessage } from '../models/support.model';

@Injectable({ providedIn: 'root' })
export class SupportService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = 'http://localhost:5068/api/support';

  createConversation(): Observable<SupportConversation> {
    return this.http.post<SupportConversation>(`${this.apiUrl}/conversations`, {});
  }

  sendMessage(conversationId: number, content: string): Observable<BotResponse> {
    return this.http.post<BotResponse>(`${this.apiUrl}/conversations/${conversationId}/messages`, { content });
  }

  getMessages(conversationId: number): Observable<SupportMessage[]> {
    return this.http.get<SupportMessage[]>(`${this.apiUrl}/conversations/${conversationId}/messages`);
  }

  getMyConversations(): Observable<SupportConversation[]> {
    return this.http.get<SupportConversation[]>(`${this.apiUrl}/conversations/my`);
  }

  getAll(page: number, pageSize: number, status?: string): Observable<{ items: SupportConversation[]; totalCount: number; pageNumber: number; pageSize: number }> {
    let url = `${this.apiUrl}/conversations?pageNumber=${page}&pageSize=${pageSize}`;
    if (status) url += `&status=${status}`;
    return this.http.get<{ items: SupportConversation[]; totalCount: number; pageNumber: number; pageSize: number }>(url);
  }

  getEscalated(page: number, pageSize: number): Observable<{ items: SupportConversation[]; totalCount: number; pageNumber: number; pageSize: number }> {
    return this.http.get<{ items: SupportConversation[]; totalCount: number; pageNumber: number; pageSize: number }>(
      `${this.apiUrl}/conversations/escalated?pageNumber=${page}&pageSize=${pageSize}`
    );
  }

  updateStatus(id: number, status: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/conversations/${id}/status`, { status });
  }

  adminReply(conversationId: number, content: string): Observable<SupportMessage> {
    return this.http.post<SupportMessage>(`${this.apiUrl}/conversations/${conversationId}/reply`, { content });
  }
}
