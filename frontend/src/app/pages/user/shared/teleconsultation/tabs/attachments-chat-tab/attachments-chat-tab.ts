import { Component, Input, OnInit, OnDestroy, ElementRef, ViewChild, Inject, PLATFORM_ID, ChangeDetectorRef } from '@angular/core';
import { CommonModule, isPlatformBrowser } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { MediaPreviewModalComponent } from '@shared/components/molecules/media-preview-modal/media-preview-modal';
import { MobileUploadButtonComponent, MobileUploadReceivedEvent } from '@shared/components/organisms/mobile-upload-button/mobile-upload-button';
import { AttachmentsChatService, AttachmentMessage } from '@core/services/attachments-chat.service';
import { ModalService } from '@core/services/modal.service';
import { DeviceDetectorService } from '@core/services/device-detector.service';
import { TeleconsultationRealTimeService, AttachmentEvent } from '@core/services/teleconsultation-realtime.service';
import { Subject, takeUntil } from 'rxjs';

interface PendingFile {
  file: File;
  title: string;
  previewUrl: string;
  loading?: boolean;
}

@Component({
  selector: 'app-attachments-chat-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, ButtonComponent, MediaPreviewModalComponent, MobileUploadButtonComponent],
  templateUrl: './attachments-chat-tab.html',
  styleUrls: ['./attachments-chat-tab.scss']
})
export class AttachmentsChatTabComponent implements OnInit, OnDestroy {
  @Input() appointmentId: string | null = null;
  @Input() userrole: 'PATIENT' | 'PROFESSIONAL' | 'ADMIN' = 'PATIENT';
  @Input() readonly = false;

  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  @ViewChild('chatContainer') chatContainer!: ElementRef<HTMLDivElement>;

  messages: AttachmentMessage[] = [];
  isMobile = false;
  isDragOver = false;
  
  // Multiple files pending (for batch add)
  pendingFiles: PendingFile[] = [];
  isAddingAttachment = false;
  isUploading = false;
  
  // Single file fallback
  selectedFile: File | null = null;
  selectedFilePreview: string | null = null;
  attachmentTitle = '';

  // Editing existing message
  editingMessageId: string | null = null;

  // Preview State
  previewModalOpen = false;
  previewUrl = '';
  previewTitle = '';
  previewType: 'image' | 'file' = 'image';
  previewMessage: AttachmentMessage | null = null;

  private destroy$ = new Subject<void>();
  private isBrowser: boolean;

  constructor(
    private chatService: AttachmentsChatService,
    private modalService: ModalService,
    private cdr: ChangeDetectorRef,
    private deviceDetector: DeviceDetectorService,
    private teleconsultationRealTime: TeleconsultationRealTimeService,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {
    this.isBrowser = isPlatformBrowser(platformId);
  }

  ngOnInit() {
    this.checkPlatform();
    
    if (this.appointmentId) {
      this.chatService.getMessages(this.appointmentId)
        .pipe(takeUntil(this.destroy$))
        .subscribe(msgs => {
          this.messages = msgs;
          this.scrollToBottom();
          try { this.cdr.detectChanges(); } catch (e) { /* noop */ }
        });
      
      // Setup real-time subscriptions
      if (this.isBrowser) {
        this.setupRealTimeSubscriptions();
      }
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  private setupRealTimeSubscriptions(): void {
    // Listen for new attachments from other participant
    this.teleconsultationRealTime.attachmentAdded$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event: AttachmentEvent) => {
        if (event.attachment) {
          // Add to messages if not already present
          const exists = this.messages.some(m => m.id === event.attachment.id);
          if (!exists) {
            this.messages.push(event.attachment);
            this.cdr.detectChanges();
            this.scrollToBottom();
          }
        }
      });

    // Listen for removed attachments
    this.teleconsultationRealTime.attachmentRemoved$
      .pipe(takeUntil(this.destroy$))
      .subscribe((event: AttachmentEvent) => {
        if (event.attachmentId) {
          this.messages = this.messages.filter(m => m.id !== event.attachmentId);
          this.cdr.detectChanges();
        }
      });
  }

  checkPlatform() {
    if (isPlatformBrowser(this.platformId)) {
      // Tablets também devem usar interface mobile (têm câmera e galeria)
      this.isMobile = this.deviceDetector.isMobile() || this.deviceDetector.isTablet();
    }
  }

  scrollToBottom() {
    if (isPlatformBrowser(this.platformId)) {
      setTimeout(() => {
        if (this.chatContainer) {
          this.chatContainer.nativeElement.scrollTop = this.chatContainer.nativeElement.scrollHeight;
        }
      }, 100);
    }
  }

  // ====== DRAG AND DROP ======
  onDragOver(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = true;
  }

  onDragLeave(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
  }

  onDrop(event: DragEvent) {
    event.preventDefault();
    event.stopPropagation();
    this.isDragOver = false;
    
    const files = event.dataTransfer?.files;
    if (files && files.length > 0) {
      this.handleMultipleFiles(files);
    }
  }

  // ====== FILE SELECTION ======
  triggerFileInput() {
    this.fileInput.nativeElement.click();
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      this.handleMultipleFiles(input.files);
    }
    // Reset input
    input.value = '';
  }

  handleMultipleFiles(files: FileList) {
    // Add files immediately with loading state
    for (let i = 0; i < files.length; i++) {
      const file = files[i];
      const pendingIndex = this.pendingFiles.length;
      
      // Add immediately with loading placeholder
      this.pendingFiles.push({
        file,
        title: file.name.replace(/\.[^/.]+$/, ''),
        previewUrl: '',
        loading: true
      });
      
      // Load preview in background
      this.createPreviewUrl(file).then(previewUrl => {
        if (this.pendingFiles[pendingIndex]) {
          this.pendingFiles[pendingIndex].previewUrl = previewUrl;
          this.pendingFiles[pendingIndex].loading = false;
          this.cdr.detectChanges();
        }
      });
    }
    this.isAddingAttachment = true;
    this.cdr.detectChanges();
  }

  private createPreviewUrl(file: File): Promise<string> {
    return new Promise((resolve) => {
      if (file.type.startsWith('image/')) {
        const reader = new FileReader();
        reader.onload = (e: any) => resolve(e.target.result);
        reader.readAsDataURL(file);
      } else {
        resolve('');
      }
    });
  }

  removePendingFile(index: number) {
    this.pendingFiles.splice(index, 1);
    if (this.pendingFiles.length === 0) {
      this.isAddingAttachment = false;
    }
  }

  startAddingAttachment() {
    this.isAddingAttachment = true;
  }

  cancelAddingAttachment() {
    this.isAddingAttachment = false;
    this.pendingFiles = [];
    this.selectedFile = null;
    this.selectedFilePreview = null;
    this.attachmentTitle = '';
    try { this.cdr.detectChanges(); } catch (e) { /* noop */ }
  }

  // ====== SEND ATTACHMENTS ======
  private generateUUID(): string {
    // Fallback para quando crypto.randomUUID não está disponível
    if (typeof crypto !== 'undefined' && crypto.randomUUID) {
      return crypto.randomUUID();
    }
    // Gera UUID v4 compatível
    return 'xxxxxxxx-xxxx-4xxx-yxxx-xxxxxxxxxxxx'.replace(/[xy]/g, (c) => {
      const r = Math.random() * 16 | 0;
      const v = c === 'x' ? r : (r & 0x3 | 0x8);
      return v.toString(16);
    });
  }

  async sendAttachments() {
    if (!this.appointmentId || this.pendingFiles.length === 0) return;
    
    this.isUploading = true;

    try {
      for (const pending of this.pendingFiles) {
        const base64 = await this.fileToBase64(pending.file);
        
        const newMessage: AttachmentMessage = {
          id: this.generateUUID(),
          senderRole: this.userrole === 'PROFESSIONAL' ? 'PROFESSIONAL' : 'PATIENT',
          senderName: this.userrole === 'PROFESSIONAL' ? 'Profissional' : 'Você',
          timestamp: new Date().toISOString(),
          title: pending.title || pending.file.name,
          fileName: pending.file.name,
          fileType: pending.file.type,
          fileSize: pending.file.size,
          fileUrl: base64 as string
        };

        // Aguardar resposta antes de enviar próximo
        await new Promise<void>((resolve, reject) => {
          this.chatService.addMessage(this.appointmentId!, newMessage).subscribe({
            next: () => {
              // Notify other participant via SignalR
              this.teleconsultationRealTime.notifyAttachmentAdded(this.appointmentId!, newMessage);
              resolve();
            },
            error: (err) => reject(err)
          });
        });
      }
      
      this.cancelAddingAttachment();
    } catch (error) {
      console.error('Error sending attachments:', error);
      this.modalService.alert({
        title: 'Erro',
        message: 'Erro ao enviar anexos.',
        variant: 'danger'
      }).subscribe();
    } finally {
      this.isUploading = false;
      try { this.cdr.detectChanges(); } catch (e) { /* noop */ }
    }
  }

  private fileToBase64(file: File): Promise<string | ArrayBuffer | null> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => resolve(reader.result);
      reader.onerror = error => reject(error);
    });
  }

  // ====== MOBILE UPLOAD (via component) ======
  /**
   * Handles mobile upload received from MobileUploadButtonComponent
   */
  onMobileUploadReceived(event: MobileUploadReceivedEvent) {
    if (!this.appointmentId) return;

    // Create message from the received upload
    const newMessage: AttachmentMessage = {
      id: crypto.randomUUID(),
      senderRole: this.userrole === 'PROFESSIONAL' ? 'PROFESSIONAL' : 'PATIENT',
      senderName: this.userrole === 'PROFESSIONAL' ? 'Profissional' : 'Paciente',
      timestamp: new Date().toISOString(),
      title: event.title,
      fileName: event.title,
      fileType: event.type === 'image' ? 'image/jpeg' : 'application/octet-stream',
      fileSize: 0,
      fileUrl: event.fileUrl
    };

    // Add to chat
    this.chatService.addMessage(this.appointmentId, newMessage).subscribe({
      next: () => {
        // Notify other participant via SignalR
        this.teleconsultationRealTime.notifyAttachmentAdded(this.appointmentId!, newMessage);
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erro ao salvar upload mobile:', err);
      }
    });
  }

  // ====== MOBILE DIRECT FILE SELECTION ======
  onFileSelectedDirectly(event: any) {
    const files = event.target.files;
    if (files && files.length > 0) {
      this.handleMultipleFiles(files);
      this.cdr.detectChanges();
      // Auto-send for streamlined mobile UX
      setTimeout(() => {
        this.sendAttachments();
      }, 500);
    }
    event.target.value = '';
  }

  // ====== EDIT MESSAGE TITLE ======
  editMessage(message: AttachmentMessage) {
    this.editingMessageId = message.id;
  }

  saveMessageEdit(message: AttachmentMessage) {
    this.editingMessageId = null;
  }

  cancelMessageEdit() {
    this.editingMessageId = null;
  }

  // ====== PREVIEW & DOWNLOAD ======
  openPreview(message: AttachmentMessage) {
    this.previewUrl = message.fileUrl;
    this.previewTitle = message.title;
    this.previewType = this.isImage(message.fileType) ? 'image' : 'file';
    this.previewMessage = message;
    this.previewModalOpen = true;
  }

  closePreview() {
    this.previewModalOpen = false;
    this.previewMessage = null;
  }

  downloadPreview() {
    if (this.previewMessage) {
      this.downloadFile(this.previewMessage);
    }
  }

  downloadFile(message: AttachmentMessage) {
    const link = document.createElement('a');
    link.href = message.fileUrl;
    link.download = message.fileName;
    link.click();
  }

  // ====== UTILS ======
  formatBytes(bytes: number, decimals = 2) {
    if (!bytes || bytes === 0) return '0 Bytes';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
  }

  isImage(mimeType: string): boolean {
    return mimeType?.startsWith('image/') || false;
  }
}
