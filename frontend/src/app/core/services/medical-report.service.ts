import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';

const API_BASE_URL = `${environment.apiUrl}/medical-reports`;

export interface MedicalReportDto {
  id: string;
  appointmentId: string;
  professionalId: string;
  patientId: string;
  tipo: string;
  titulo: string;
  dataEmissao: string;
  historicoClinico?: string;
  exameFisico?: string;
  examesComplementares?: string;
  hipoteseDiagnostica?: string;
  cid?: string;
  conclusao: string;
  recomendacoes?: string;
  observacoes?: string;
  professionalName?: string;
  professionalCrm?: string;
  professionalUf?: string;
  professionalSpecialty?: string;
  patientName?: string;
  patientCpf?: string;
  patientCns?: string;
  patientBirthDate?: string;
  isSigned: boolean;
  certificateSubject?: string;
  signedAt?: string;
  documentHash?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateMedicalReportDto {
  appointmentId: string;
  tipo: string;
  titulo: string;
  dataEmissao?: string;
  historicoClinico?: string;
  exameFisico?: string;
  examesComplementares?: string;
  hipoteseDiagnostica?: string;
  cid?: string;
  conclusao: string;
  recomendacoes?: string;
  observacoes?: string;
}

export interface UpdateMedicalReportDto {
  tipo?: string;
  titulo?: string;
  dataEmissao?: string;
  historicoClinico?: string;
  exameFisico?: string;
  examesComplementares?: string;
  hipoteseDiagnostica?: string;
  cid?: string;
  conclusao?: string;
  recomendacoes?: string;
  observacoes?: string;
}

export interface ValidationResult {
  valid: boolean;
  isSigned: boolean;
  report: MedicalReportDto;
}

@Injectable({
  providedIn: 'root'
})
export class MedicalReportService {
  private readonly baseUrl = API_BASE_URL;

  constructor(private http: HttpClient) {}

  /**
   * Obtém um laudo por ID
   */
  getById(id: string): Observable<MedicalReportDto> {
    return this.http.get<MedicalReportDto>(`${this.baseUrl}/${id}`);
  }

  /**
   * Lista todos os laudos de uma consulta
   */
  getByAppointmentId(appointmentId: string): Observable<MedicalReportDto[]> {
    return this.http.get<MedicalReportDto[]>(`${this.baseUrl}/appointment/${appointmentId}`);
  }

  /**
   * Lista todos os laudos de um paciente
   */
  getByPatientId(patientId: string): Observable<MedicalReportDto[]> {
    return this.http.get<MedicalReportDto[]>(`${this.baseUrl}/patient/${patientId}`);
  }

  /**
   * Lista todos os laudos de um profissional
   */
  getByProfessionalId(professionalId: string): Observable<MedicalReportDto[]> {
    return this.http.get<MedicalReportDto[]>(`${this.baseUrl}/professional/${professionalId}`);
  }

  /**
   * Cria um novo laudo
   */
  create(dto: CreateMedicalReportDto): Observable<MedicalReportDto> {
    return this.http.post<MedicalReportDto>(this.baseUrl, dto);
  }

  /**
   * Atualiza um laudo existente
   */
  update(id: string, dto: UpdateMedicalReportDto): Observable<MedicalReportDto> {
    return this.http.put<MedicalReportDto>(`${this.baseUrl}/${id}`, dto);
  }

  /**
   * Remove um laudo
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${this.baseUrl}/${id}`);
  }

  /**
   * Gera e faz download do PDF do laudo
   */
  generatePdf(id: string): Observable<Blob> {
    return this.http.get(`${this.baseUrl}/${id}/pdf`, {
      responseType: 'blob'
    });
  }

  /**
   * Valida um documento pelo hash
   */
  validateDocument(hash: string): Observable<ValidationResult> {
    return this.http.get<ValidationResult>(`${this.baseUrl}/validate/${hash}`);
  }
}
