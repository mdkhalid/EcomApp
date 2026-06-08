import { Component, inject, signal, ElementRef, viewChild, AfterViewChecked, DestroyRef } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../services/auth.service';
import { SupportService } from '../../services/support.service';
import { SupportMessage } from '../../models/support.model';

@Component({
  selector: 'app-support-bot',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './support-bot.component.html',
  styleUrl: './support-bot.component.scss'
})
export class SupportBotComponent implements AfterViewChecked {
  private readonly destroyRef = inject(DestroyRef);
  private readonly authService = inject(AuthService);
  private readonly supportService = inject(SupportService);

  isOpen = signal(false);
  messages = signal<SupportMessage[]>([]);
  newMessage = signal('');
  sending = signal(false);
  conversationId = signal<number | null>(null);
  needsEscalation = signal(false);

  chatBody = viewChild<ElementRef>('chatBody');

  ngAfterViewChecked(): void {
    this.scrollToBottom();
  }

  toggleChat(): void {
    if (!this.isOpen()) {
      this.isOpen.set(true);
      this.startConversation();
    } else {
      this.isOpen.set(false);
    }
  }

  private startConversation(): void {
    const existing = localStorage.getItem('supportConversationId');
    if (existing) {
      const convId = parseInt(existing, 10);
      if (!isNaN(convId)) {
        this.conversationId.set(convId);
        this.supportService.getMessages(convId).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
          next: (msgs) => {
            this.messages.set(msgs);
            this.needsEscalation.set(msgs.some(m => m.sender === 'Bot' && m.content.includes('escalated')));
          }
        });
        return;
      }
    }

    this.supportService.createConversation().pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (conv) => {
        this.conversationId.set(conv.id);
        localStorage.setItem('supportConversationId', conv.id.toString());
        this.messages.set(conv.messages || []);
      }
    });
  }

  sendMessage(): void {
    const content = this.newMessage().trim();
    if (!content || this.sending()) return;

    const convId = this.conversationId();
    if (!convId) return;

    this.sending.set(true);
    this.newMessage.set('');

    const optimisticMsg: SupportMessage = {
      id: Date.now(),
      conversationId: convId,
      content,
      sender: 'User',
      createdAt: new Date().toISOString()
    };
    this.messages.update(msgs => [...msgs, optimisticMsg]);

    this.supportService.sendMessage(convId, content).pipe(takeUntilDestroyed(this.destroyRef)).subscribe({
      next: (res) => {
        this.messages.update(msgs => [
          ...msgs.filter(m => m.id !== optimisticMsg.id),
          res.message,
          {
            id: Date.now() + 1,
            conversationId: convId,
            content: res.reply,
            sender: 'Bot',
            createdAt: new Date().toISOString()
          }
        ]);
        this.needsEscalation.set(res.needsEscalation);
        this.sending.set(false);
      },
      error: () => {
        this.messages.update(msgs => msgs.filter(m => m.id !== optimisticMsg.id));
        this.sending.set(false);
      }
    });
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Enter' && !event.shiftKey) {
      event.preventDefault();
      this.sendMessage();
    }
  }

  private scrollToBottom(): void {
    const el = this.chatBody();
    if (el) {
      el.nativeElement.scrollTop = el.nativeElement.scrollHeight;
    }
  }

  protected readonly auth = this.authService;
}
