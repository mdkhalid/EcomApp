export interface SupportMessage {
  id: number;
  conversationId: number;
  content: string;
  sender: 'User' | 'Bot' | 'Admin';
  createdAt: string;
}

export interface SupportConversation {
  id: number;
  userId?: number;
  userEmail?: string;
  status: 'Open' | 'Resolved' | 'Escalated';
  createdAt: string;
  updatedAt: string;
  messages: SupportMessage[];
  messageCount: number;
}

export interface BotResponse {
  reply: string;
  needsEscalation: boolean;
  message: SupportMessage;
}

export interface SendMessageRequest {
  content: string;
}
