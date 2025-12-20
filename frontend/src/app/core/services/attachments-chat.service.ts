import { Injectable, Inject, PLATFORM_ID, OnDestroy } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, interval, Subscription } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '@env/environment';

export interface AttachmentMessage {
  id: string;
  senderRole: 'PATIENT' | 'PROFESSIONAL';
  senderName: string;
  timestamp: string;
  title: string;
  fileName: string;
  fileType: string;
  fileSize: number;
  fileUrl: string; // Base64 or Blob URL
}

@Injectable({
  providedIn: 'root'
})
export class AttachmentsChatService implements OnDestroy {
  private chatSubjects: Map<string, BehaviorSubject<AttachmentMessage[]>> = new Map();
  private pollingSubscriptions: Map<string, Subscription> = new Map();
  private messageCount: Map<string, number> = new Map();
  
  private readonly POLLING_INTERVAL = 2000; // 2 seconds

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnDestroy() {
    this.pollingSubscriptions.forEach(sub => sub.unsubscribe());
    this.pollingSubscriptions.clear();
  }

  private getSubject(appointmentId: string): BehaviorSubject<AttachmentMessage[]> {
    if (!this.chatSubjects.has(appointmentId)) {
      this.chatSubjects.set(appointmentId, new BehaviorSubject<AttachmentMessage[]>([]));
    }
    return this.chatSubjects.get(appointmentId)!;
  }

  /**
   * Obtém observable para mensagens de anexos
   * Inicia polling automaticamente
   */
  getMessages(appointmentId: string): Observable<AttachmentMessage[]> {
    // Fetch initial data
    this.fetchMessages(appointmentId);
    
    // Start polling
    this.startPolling(appointmentId);
    
    return this.getSubject(appointmentId).asObservable();
  }

  /**
   * Adiciona uma nova mensagem de anexo
   * Retorna Observable para permitir esperar conclusão
   */
  addMessage(appointmentId: string, message: AttachmentMessage): Observable<AttachmentMessage> {
    const url = `${environment.apiUrl}/appointments/${appointmentId}/attachments-chat`;
    
    return new Observable(observer => {
      this.http.post<{ message: string; data: AttachmentMessage }>(url, message).subscribe({
        next: (response) => {
          // Add to local list immediately
          const currentMessages = this.getSubject(appointmentId).value;
          // Evitar duplicatas verificando pelo ID
          if (!currentMessages.find(m => m.id === response.data.id)) {
            const newMessages = [...currentMessages, response.data];
            this.getSubject(appointmentId).next(newMessages);
            this.messageCount.set(appointmentId, newMessages.length);
          }
          observer.next(response.data);
          observer.complete();
        },
        error: (err) => {
          console.error('Error adding attachment message:', err);
          observer.error(err);
        }
      });
    });
  }

  /**
   * Busca mensagens do servidor
   */
  private fetchMessages(appointmentId: string): void {
    const url = `${environment.apiUrl}/appointments/${appointmentId}/attachments-chat`;
    
    this.http.get<AttachmentMessage[]>(url).subscribe({
      next: (messages) => {
        const currentCount = this.messageCount.get(appointmentId) || 0;
        
        // Only update if count changed
        if (messages.length !== currentCount) {
          this.getSubject(appointmentId).next(messages);
          this.messageCount.set(appointmentId, messages.length);
        }
      },
      error: (err) => {
        if (err.status !== 404) {
          console.error('Error fetching attachment messages:', err);
        }
      }
    });
  }

  /**
   * Inicia polling para atualizações em tempo real
   */
  startPolling(appointmentId: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    if (this.pollingSubscriptions.has(appointmentId)) return;

    const subscription = interval(this.POLLING_INTERVAL).subscribe(() => {
      this.fetchMessages(appointmentId);
    });
    
    this.pollingSubscriptions.set(appointmentId, subscription);
  }

  /**
   * Para o polling de uma consulta
   */
  stopPolling(appointmentId: string): void {
    const subscription = this.pollingSubscriptions.get(appointmentId);
    if (subscription) {
      subscription.unsubscribe();
      this.pollingSubscriptions.delete(appointmentId);
    }
  }
}
