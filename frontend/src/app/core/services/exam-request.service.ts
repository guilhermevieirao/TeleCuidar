import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '@env/environment';

const API_BASE_URL = `${environment.apiUrl}/exam-requests`;

export type ExamCategory = 'laboratorial' | 'imagem' | 'cardiologico' | 'oftalmologico' | 'audiometrico' | 'neurologico' | 'endoscopico' | 'outro';
export type ExamPriority = 'normal' | 'urgente';

export interface ExamRequest {
  id: string;
  appointmentId: string;
  professionalId: string;
  professionalName?: string;
  professionalCrm?: string;
  patientId: string;
  patientName?: string;
  patientCpf?: string;
  nomeExame: string;
  codigoExame?: string;
  categoria: ExamCategory;
  prioridade: ExamPriority;
  dataEmissao: string;
  dataLimite?: string;
  indicacaoClinica: string;
  hipoteseDiagnostica?: string;
  cid?: string;
  observacoes?: string;
  instrucoesPreparo?: string;
  isSigned: boolean;
  certificateSubject?: string;
  signedAt?: string;
  documentHash?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateExamRequestDto {
  appointmentId: string;
  nomeExame: string;
  codigoExame?: string;
  categoria?: string;
  prioridade?: string;
  dataLimite?: string;
  indicacaoClinica: string;
  hipoteseDiagnostica?: string;
  cid?: string;
  observacoes?: string;
  instrucoesPreparo?: string;
}

export interface UpdateExamRequestDto {
  nomeExame?: string;
  codigoExame?: string;
  categoria?: string;
  prioridade?: string;
  dataLimite?: string;
  indicacaoClinica?: string;
  hipoteseDiagnostica?: string;
  cid?: string;
  observacoes?: string;
  instrucoesPreparo?: string;
}

export interface ExamRequestPdf {
  pdfBase64: string;
  fileName: string;
  documentHash: string;
  isSigned: boolean;
}

@Injectable({
  providedIn: 'root'
})
export class ExamRequestService {

  constructor(private http: HttpClient) {}

  /**
   * Obtém uma solicitação de exame por ID
   */
  getById(id: string): Observable<ExamRequest> {
    return this.http.get<ExamRequest>(`${API_BASE_URL}/${id}`);
  }

  /**
   * Obtém todas as solicitações de exame de uma consulta
   */
  getByAppointment(appointmentId: string): Observable<ExamRequest[]> {
    return this.http.get<ExamRequest[]>(`${API_BASE_URL}/appointment/${appointmentId}`);
  }

  /**
   * Obtém todas as solicitações de exame de um paciente
   */
  getByPatient(patientId: string): Observable<ExamRequest[]> {
    return this.http.get<ExamRequest[]>(`${API_BASE_URL}/patient/${patientId}`);
  }

  /**
   * Obtém todas as solicitações de exame emitidas por um profissional
   */
  getByProfessional(professionalId: string): Observable<ExamRequest[]> {
    return this.http.get<ExamRequest[]>(`${API_BASE_URL}/professional/${professionalId}`);
  }

  /**
   * Cria uma nova solicitação de exame
   */
  create(dto: CreateExamRequestDto): Observable<ExamRequest> {
    return this.http.post<ExamRequest>(API_BASE_URL, dto);
  }

  /**
   * Atualiza uma solicitação de exame existente
   */
  update(id: string, dto: UpdateExamRequestDto): Observable<ExamRequest> {
    return this.http.put<ExamRequest>(`${API_BASE_URL}/${id}`, dto);
  }

  /**
   * Exclui uma solicitação de exame
   */
  delete(id: string): Observable<void> {
    return this.http.delete<void>(`${API_BASE_URL}/${id}`);
  }

  /**
   * Gera PDF da solicitação de exame
   */
  generatePdf(id: string): Observable<ExamRequestPdf> {
    return this.http.get<ExamRequestPdf>(`${API_BASE_URL}/${id}/pdf`);
  }

  /**
   * Valida hash de documento
   */
  validateDocument(documentHash: string): Observable<{ valid: boolean; hash: string }> {
    return this.http.get<{ valid: boolean; hash: string }>(`${API_BASE_URL}/validate/${documentHash}`);
  }
}
