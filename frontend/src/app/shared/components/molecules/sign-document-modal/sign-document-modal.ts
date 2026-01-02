import { Component, EventEmitter, Input, Output, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { BadgeComponent } from '@shared/components/atoms/badge/badge';
import { 
  DigitalCertificateService, 
  DigitalCertificate,
  CertificateValidationResult,
  SignDocumentResult 
} from '@core/services/digital-certificate.service';

export type SignMode = 'saved' | 'save-new' | 'one-time';

export interface SignDocumentEvent {
  success: boolean;
  result?: SignDocumentResult;
  certificate?: DigitalCertificate;
}

@Component({
  selector: 'app-sign-document-modal',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, IconComponent, BadgeComponent],
  templateUrl: './sign-document-modal.html',
  styleUrls: ['./sign-document-modal.scss']
})
export class SignDocumentModalComponent implements OnInit {
  @Input() documentType: 'prescription' | 'certificate' | 'exam' | 'report' = 'prescription';
  @Input() documentId: string = '';
  @Input() isOpen = false;
  @Output() close = new EventEmitter<void>();
  @Output() signed = new EventEmitter<SignDocumentEvent>();

  certificates: DigitalCertificate[] = [];
  isLoadingCertificates = true;
  
  signMode: SignMode = 'saved';
  selectedCertificateId: string | null = null;
  password = '';
  
  // One-time or save-new mode
  file: File | null = null;
  filePassword = '';
  displayName = '';
  quickUseEnabled = false;
  validation: CertificateValidationResult | null = null;
  
  // States
  isValidating = false;
  isSigning = false;
  errorMessage = '';
  
  // Password visibility toggles
  showPassword = false;
  showFilePassword = false;

  constructor(
    private certificateService: DigitalCertificateService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadCertificates();
  }

  loadCertificates() {
    this.isLoadingCertificates = true;
    this.certificateService.getMyCertificates().subscribe({
      next: (certs) => {
        // Filtrar apenas certificados válidos (não expirados)
        this.certificates = certs.filter(c => !c.isExpired);
        if (this.certificates.length > 0) {
          this.selectedCertificateId = this.certificates[0].id;
        } else {
          // Sem certificados salvos, ir para modo one-time
          this.signMode = 'one-time';
        }
        this.isLoadingCertificates = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.certificates = [];
        this.signMode = 'one-time';
        this.isLoadingCertificates = false;
        this.cdr.detectChanges();
      }
    });
  }

  get selectedCertificate(): DigitalCertificate | undefined {
    return this.certificates.find(c => c.id === this.selectedCertificateId);
  }

  get needsPassword(): boolean {
    const cert = this.selectedCertificate;
    return cert ? !cert.quickUseEnabled : true;
  }

  onFileSelected(event: Event) {
    const input = event.target as HTMLInputElement;
    if (input.files && input.files.length > 0) {
      const file = input.files[0];
      if (!file.name.toLowerCase().endsWith('.pfx') && !file.name.toLowerCase().endsWith('.p12')) {
        this.errorMessage = 'Selecione um arquivo .pfx ou .p12';
        return;
      }
      this.file = file;
      this.validation = null;
      this.errorMessage = '';
    }
  }

  async validateCertificate() {
    if (!this.file || !this.filePassword) {
      this.errorMessage = 'Selecione o arquivo e informe a senha';
      return;
    }

    this.isValidating = true;
    this.errorMessage = '';
    
    try {
      const pfxBase64 = await this.certificateService.fileToBase64(this.file);
      
      this.certificateService.validateCertificate({
        pfxBase64,
        password: this.filePassword
      }).subscribe({
        next: (result) => {
          this.validation = result;
          if (result.isValid && result.nameFromCertificate && !this.displayName) {
            this.displayName = result.nameFromCertificate;
          }
          if (!result.isValid) {
            this.errorMessage = result.errorMessage || 'Certificado inválido';
          } else if (result.isExpired) {
            this.errorMessage = 'Este certificado está expirado';
          }
          this.isValidating = false;
          this.cdr.detectChanges();
        },
        error: () => {
          this.errorMessage = 'Erro ao validar certificado';
          this.isValidating = false;
          this.cdr.detectChanges();
        }
      });
    } catch {
      this.errorMessage = 'Erro ao ler arquivo';
      this.isValidating = false;
    }
  }

  async signDocument() {
    this.isSigning = true;
    this.errorMessage = '';

    try {
      if (this.signMode === 'saved') {
        // Usar certificado salvo
        if (!this.selectedCertificateId) {
          this.errorMessage = 'Selecione um certificado';
          this.isSigning = false;
          return;
        }

        if (this.needsPassword && !this.password) {
          this.errorMessage = 'Informe a senha do certificado';
          this.isSigning = false;
          return;
        }

        this.certificateService.signDocument({
          certificateId: this.selectedCertificateId,
          password: this.needsPassword ? this.password : undefined,
          documentType: this.documentType,
          documentId: this.documentId
        }).subscribe({
          next: (result) => {
            this.handleSignResult(result);
          },
          error: (err) => {
            this.errorMessage = err?.error?.message || 'Erro ao assinar documento';
            this.isSigning = false;
            this.cdr.detectChanges();
          }
        });
      } else if (this.signMode === 'save-new') {
        // Salvar certificado e assinar
        if (!this.file || !this.validation?.isValid || !this.displayName) {
          this.errorMessage = 'Valide o certificado e informe um nome';
          this.isSigning = false;
          return;
        }

        const pfxBase64 = await this.certificateService.fileToBase64(this.file);
        
        this.certificateService.saveAndSign({
          pfxBase64,
          password: this.filePassword,
          displayName: this.displayName,
          quickUseEnabled: this.quickUseEnabled,
          documentType: this.documentType,
          documentId: this.documentId
        }).subscribe({
          next: (result) => {
            this.handleSignResult(result.signResult, result.certificate);
          },
          error: (err) => {
            this.errorMessage = err?.error?.message || 'Erro ao salvar e assinar';
            this.isSigning = false;
            this.cdr.detectChanges();
          }
        });
      } else {
        // One-time - apenas assinar
        if (!this.file || !this.filePassword) {
          this.errorMessage = 'Selecione o arquivo e informe a senha';
          this.isSigning = false;
          return;
        }

        const pfxBase64 = await this.certificateService.fileToBase64(this.file);
        
        this.certificateService.signDocument({
          oneTimePfxBase64: pfxBase64,
          password: this.filePassword,
          documentType: this.documentType,
          documentId: this.documentId
        }).subscribe({
          next: (result) => {
            this.handleSignResult(result);
          },
          error: (err) => {
            this.errorMessage = err?.error?.message || 'Erro ao assinar documento';
            this.isSigning = false;
            this.cdr.detectChanges();
          }
        });
      }
    } catch {
      this.errorMessage = 'Erro ao processar certificado';
      this.isSigning = false;
    }
  }

  private handleSignResult(result: SignDocumentResult, certificate?: DigitalCertificate) {
    this.isSigning = false;
    if (result.success) {
      this.signed.emit({ success: true, result, certificate });
      this.closeModal();
    } else {
      this.errorMessage = result.errorMessage || 'Erro ao assinar documento';
    }
    this.cdr.detectChanges();
  }

  closeModal() {
    this.resetState();
    this.close.emit();
  }

  private resetState() {
    this.password = '';
    this.file = null;
    this.filePassword = '';
    this.displayName = '';
    this.quickUseEnabled = false;
    this.validation = null;
    this.errorMessage = '';
    this.signMode = this.certificates.length > 0 ? 'saved' : 'one-time';
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    return new Date(dateString).toLocaleDateString('pt-BR');
  }

  getDocumentTypeName(): string {
    switch (this.documentType) {
      case 'prescription': return 'Receita';
      case 'certificate': return 'Atestado';
      case 'exam': return 'Solicitação de Exame';
      case 'report': return 'Laudo';
      default: return 'Documento';
    }
  }
}
