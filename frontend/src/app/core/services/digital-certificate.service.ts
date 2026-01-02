import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';

const API_BASE_URL = environment.apiUrl;

export interface DigitalCertificate {
  id: string;
  displayName: string;
  subject: string;
  issuer: string;
  thumbprint: string;
  cpfFromCertificate?: string;
  nameFromCertificate?: string;
  expirationDate: string;
  issuedDate: string;
  quickUseEnabled: boolean;
  isActive: boolean;
  lastUsedAt?: string;
  createdAt: string;
  isExpired: boolean;
  daysUntilExpiration: number;
}

export interface CertificateValidationResult {
  isValid: boolean;
  errorMessage?: string;
  subject: string;
  issuer: string;
  thumbprint: string;
  cpfFromCertificate?: string;
  nameFromCertificate?: string;
  expirationDate: string;
  issuedDate: string;
  isExpired: boolean;
  daysUntilExpiration: number;
}

export interface ValidateCertificateDto {
  pfxBase64: string;
  password: string;
}

export interface SaveCertificateDto {
  pfxBase64: string;
  password: string;
  displayName: string;
  quickUseEnabled: boolean;
}

export interface UpdateCertificateDto {
  displayName?: string;
  quickUseEnabled?: boolean;
  password?: string;
}

export interface SignDocumentRequestDto {
  certificateId?: string;
  password?: string;
  oneTimePfxBase64?: string;
  documentType: 'prescription' | 'certificate' | 'exam' | 'report';
  documentId: string;
}

export interface SignDocumentResult {
  success: boolean;
  errorMessage?: string;
  documentHash?: string;
  certificateSubject?: string;
  signedAt?: string;
}

export interface SaveCertificateAndSignDto {
  pfxBase64: string;
  password: string;
  displayName: string;
  quickUseEnabled: boolean;
  documentType: 'prescription' | 'certificate' | 'exam' | 'report';
  documentId: string;
}

export interface SaveAndSignResult {
  certificate: DigitalCertificate;
  signResult: SignDocumentResult;
}

@Injectable({
  providedIn: 'root'
})
export class DigitalCertificateService {
  private readonly baseUrl = `${API_BASE_URL}/digitalcertificates`;

  constructor(private http: HttpClient) {}

  /**
   * Lista todos os certificados do usuário atual
   */
  getMyCertificates(): Observable<DigitalCertificate[]> {
    return this.http.get<DigitalCertificate[]>(this.baseUrl);
  }

  /**
   * Obtém um certificado específico por ID
   */
  getCertificate(id: string): Observable<DigitalCertificate> {
    return this.http.get<DigitalCertificate>(`${this.baseUrl}/${id}`);
  }

  /**
   * Valida um certificado PFX sem salvar
   */
  validateCertificate(dto: ValidateCertificateDto): Observable<CertificateValidationResult> {
    return this.http.post<CertificateValidationResult>(`${this.baseUrl}/validate`, dto);
  }

  /**
   * Salva um novo certificado digital
   */
  saveCertificate(dto: SaveCertificateDto): Observable<DigitalCertificate> {
    return this.http.post<DigitalCertificate>(this.baseUrl, dto);
  }

  /**
   * Atualiza um certificado existente
   */
  updateCertificate(id: string, dto: UpdateCertificateDto): Observable<DigitalCertificate> {
    return this.http.patch<DigitalCertificate>(`${this.baseUrl}/${id}`, dto);
  }

  /**
   * Remove um certificado
   */
  deleteCertificate(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /**
   * Assina um documento (receita ou atestado)
   */
  signDocument(dto: SignDocumentRequestDto): Observable<SignDocumentResult> {
    return this.http.post<SignDocumentResult>(`${this.baseUrl}/sign`, dto);
  }

  /**
   * Salva certificado e assina documento em uma operação
   */
  saveAndSign(dto: SaveCertificateAndSignDto): Observable<SaveAndSignResult> {
    return this.http.post<SaveAndSignResult>(`${this.baseUrl}/save-and-sign`, dto);
  }

  /**
   * Baixa o PDF assinado de uma receita
   */
  downloadSignedPrescriptionPdf(prescriptionId: string): void {
    this.http.get(`${this.baseUrl}/prescription/${prescriptionId}/signed-pdf`, { 
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) return;
        
        // Extrair nome do arquivo do header Content-Disposition
        const contentDisposition = response.headers.get('Content-Disposition');
        let fileName = `receita_${prescriptionId}_assinada.pdf`;
        if (contentDisposition) {
          const match = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
          if (match && match[1]) {
            fileName = match[1].replace(/['"]/g, '');
          }
        }
        
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Erro ao baixar PDF assinado:', err);
      }
    });
  }

  /**
   * Baixa o PDF assinado de um atestado
   */
  downloadSignedCertificatePdf(certificateId: string): void {
    this.http.get(`${this.baseUrl}/certificate/${certificateId}/signed-pdf`, { 
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) return;
        
        // Extrair nome do arquivo do header Content-Disposition
        const contentDisposition = response.headers.get('Content-Disposition');
        let fileName = `atestado_${certificateId}_assinado.pdf`;
        if (contentDisposition) {
          const match = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
          if (match && match[1]) {
            fileName = match[1].replace(/['"]/g, '');
          }
        }
        
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Erro ao baixar PDF assinado:', err);
      }
    });
  }

  /**
   * Baixa o PDF assinado de uma solicitação de exame
   */
  downloadSignedExamPdf(examId: string): void {
    this.http.get(`${this.baseUrl}/exam/${examId}/signed-pdf`, { 
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) return;
        
        // Extrair nome do arquivo do header Content-Disposition
        const contentDisposition = response.headers.get('Content-Disposition');
        let fileName = `exame_${examId}_assinado.pdf`;
        if (contentDisposition) {
          const match = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
          if (match && match[1]) {
            fileName = match[1].replace(/['"]/g, '');
          }
        }
        
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Erro ao baixar PDF assinado:', err);
      }
    });
  }

  /**
   * Baixa o PDF assinado de um laudo médico
   */
  downloadSignedReportPdf(reportId: string): void {
    this.http.get(`${this.baseUrl}/report/${reportId}/signed-pdf`, { 
      responseType: 'blob',
      observe: 'response'
    }).subscribe({
      next: (response) => {
        const blob = response.body;
        if (!blob) return;
        
        // Extrair nome do arquivo do header Content-Disposition
        const contentDisposition = response.headers.get('Content-Disposition');
        let fileName = `laudo_${reportId}_assinado.pdf`;
        if (contentDisposition) {
          const match = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/);
          if (match && match[1]) {
            fileName = match[1].replace(/['"]/g, '');
          }
        }
        
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = fileName;
        document.body.appendChild(a);
        a.click();
        document.body.removeChild(a);
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Erro ao baixar PDF assinado:', err);
      }
    });
  }

  /**
   * Converte arquivo para Base64
   */
  fileToBase64(file: File): Promise<string> {
    return new Promise((resolve, reject) => {
      const reader = new FileReader();
      reader.readAsDataURL(file);
      reader.onload = () => {
        const result = reader.result as string;
        // Remove o prefixo "data:application/...;base64,"
        const base64 = result.split(',')[1];
        resolve(base64);
      };
      reader.onerror = error => reject(error);
    });
  }
}
