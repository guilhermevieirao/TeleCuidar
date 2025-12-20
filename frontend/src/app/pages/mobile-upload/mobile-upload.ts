import { Component, OnInit, ElementRef, ViewChild, NgZone, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { ThemeToggleComponent } from '@shared/components/atoms/theme-toggle/theme-toggle';
import { AttachmentsChatService, AttachmentMessage } from '@core/services/attachments-chat.service';
import { ModalService } from '@core/services/modal.service';
import { TemporaryUploadService, TemporaryUploadDto } from '@core/services/temporary-upload.service';

interface SelectedFileItem {
  file: File;
  title: string;
  previewUrl: string;
}

@Component({
  selector: 'app-mobile-upload',
  standalone: true,
  imports: [CommonModule, FormsModule, IconComponent, ButtonComponent, ThemeToggleComponent],
  templateUrl: './mobile-upload.html',
  styleUrls: ['./mobile-upload.scss']
})
export class MobileUploadComponent implements OnInit {
  appointmentId: string | null = null;
  token: string | null = null;
  
  @ViewChild('cameraInput') cameraInput!: ElementRef<HTMLInputElement>;
  @ViewChild('galleryInput') galleryInput!: ElementRef<HTMLInputElement>;
  @ViewChild('fileInput') fileInput!: ElementRef<HTMLInputElement>;
  
  // Multiple files support
  selectedFiles: SelectedFileItem[] = [];
  isUploading = false;
  uploadSuccess = false;
  uploadCount = 0;
  currentUploadIndex = 0;

  constructor(
    private route: ActivatedRoute,
    private chatService: AttachmentsChatService,
    private modalService: ModalService,
    private temporaryUploadService: TemporaryUploadService,
    private ngZone: NgZone,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.route.queryParams.subscribe(params => {
      this.appointmentId = params['appointmentId'];
      this.token = params['token'];
    });
  }

  triggerCamera() {
    this.cameraInput.nativeElement.click();
  }

  triggerGallery() {
    this.galleryInput.nativeElement.click();
  }

  triggerFiles() {
    this.fileInput.nativeElement.click();
  }

  async onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      for (let i = 0; i < input.files.length; i++) {
        const file = input.files[i];
        const previewUrl = await this.createPreview(file);
        this.selectedFiles.push({
          file,
          title: file.name.replace(/\.[^/.]+$/, ""),
          previewUrl
        });
      }
      this.uploadSuccess = false;
      this.cdr.detectChanges();
    }
    input.value = '';
  }

  private createPreview(file: File): Promise<string> {
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

  removeFile(index: number) {
    this.selectedFiles.splice(index, 1);
    this.cdr.detectChanges();
  }

  cancelUpload() {
    this.selectedFiles = [];
    this.uploadSuccess = false;
    this.cdr.detectChanges();
  }

  async sendAllAttachments() {
    if (this.selectedFiles.length === 0) return;
    
    if (!this.appointmentId && !this.token) {
        this.modalService.alert({
          title: 'Erro',
          message: 'Sessão inválida.',
          variant: 'danger'
        }).subscribe();
        return;
    }
    
    this.isUploading = true;
    this.currentUploadIndex = 0;
    this.cdr.detectChanges();

    try {
      for (let i = 0; i < this.selectedFiles.length; i++) {
        this.currentUploadIndex = i + 1;
        this.cdr.detectChanges();
        
        const item = this.selectedFiles[i];
        let base64: string;
        
        if (item.file.type.startsWith('image/')) {
          base64 = await this.compressImage(item.file);
        } else {
          base64 = await this.fileToBase64(item.file) as string;
        }
        
        if (this.appointmentId) {
          const newMessage: AttachmentMessage = {
            id: crypto.randomUUID(),
            senderRole: 'PATIENT',
            senderName: 'Upload via Mobile',
            timestamp: new Date().toISOString(),
            title: item.title || item.file.name,
            fileName: item.file.name,
            fileType: item.file.type,
            fileSize: item.file.size,
            fileUrl: base64
          };
          
          // Aguardar resposta antes de enviar próximo
          await new Promise<void>((resolve, reject) => {
            this.chatService.addMessage(this.appointmentId!, newMessage).subscribe({
              next: () => resolve(),
              error: (err) => reject(new Error(err.error?.message || 'Erro ao enviar anexo.'))
            });
          });
        } else if (this.token) {
          const payload: TemporaryUploadDto = {
            title: item.title || item.file.name,
            fileUrl: base64,
            type: item.file.type.startsWith('image/') ? 'image' : 'document',
            timestamp: new Date().getTime()
          };
          
          await new Promise<void>((resolve, reject) => {
            this.temporaryUploadService.storeUpload(this.token!, payload).subscribe({
              next: () => resolve(),
              error: (err) => reject(new Error(err.error?.message || 'Erro ao enviar arquivo.'))
            });
          });
        }
      }

      this.ngZone.run(() => {
        this.uploadSuccess = true;
        this.isUploading = false;
        this.uploadCount += this.selectedFiles.length;
        this.selectedFiles = [];
        this.cdr.detectChanges();
      });

    } catch (error: any) {
      console.error('Error processing files', error);
      this.ngZone.run(() => {
        this.isUploading = false;
        this.cdr.detectChanges();
        this.modalService.alert({
          title: 'Erro',
          message: error.message || 'Erro ao processar arquivos.',
          variant: 'danger'
        }).subscribe();
      });
    }
  }

  private compressImage(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = (event: any) => {
        const img = new Image();
        img.src = event.target.result;
        img.onload = () => {
          const canvas = document.createElement('canvas');
          const MAX_WIDTH = 800;
          const MAX_HEIGHT = 800;
          let width = img.width;
          let height = img.height;

          if (width > height) {
            if (width > MAX_WIDTH) {
              height *= MAX_WIDTH / width;
              width = MAX_WIDTH;
            }
          } else {
            if (height > MAX_HEIGHT) {
              width *= MAX_HEIGHT / height;
              height = MAX_HEIGHT;
            }
          }

          canvas.width = width;
          canvas.height = height;
          const ctx = canvas.getContext('2d');
          ctx?.drawImage(img, 0, 0, width, height);
          
          const dataUrl = canvas.toDataURL('image/jpeg', 0.7);
          resolve(dataUrl);
        };
        img.onerror = (error) => reject(error);
      };
      reader.onerror = (error) => reject(error);
    });
  }

  private fileToBase64(file: File): Promise<string | ArrayBuffer | null> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => resolve(reader.result);
      reader.onerror = error => reject(error);
    });
  }

  formatBytes(bytes: number, decimals = 2) {
    if (bytes === 0) return '0 Bytes';
    const k = 1024;
    const dm = decimals < 0 ? 0 : decimals;
    const sizes = ['Bytes', 'KB', 'MB', 'GB', 'TB', 'PB', 'EB', 'ZB', 'YB'];
    const i = Math.floor(Math.log(bytes) / Math.log(k));
    return parseFloat((bytes / Math.pow(k, i)).toFixed(dm)) + ' ' + sizes[i];
  }
}
