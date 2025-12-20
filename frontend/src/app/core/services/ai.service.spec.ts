import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import {
  AIService,
  GenerateSummaryRequest,
  GenerateDiagnosisRequest,
  AISummaryResponse,
  AIDiagnosisResponse,
  AIData,
  SaveAIData
} from './ai.service';
import { environment } from '@env/environment';

describe('AIService', () => {
  let service: AIService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/ai`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AIService]
    });

    service = TestBed.inject(AIService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('creation', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });
  });

  describe('generateSummary', () => {
    it('should generate summary with appointment data', fakeAsync(() => {
      const request: GenerateSummaryRequest = {
        appointmentId: 'apt-123',
        patientData: { name: 'João Silva', gender: 'M' },
        biometricsData: { heartRate: 72, temperature: 36.5 }
      };

      const mockResponse: AISummaryResponse = {
        summary: 'Paciente apresenta sinais vitais estáveis.',
        generatedAt: '2024-12-01T10:00:00Z'
      };

      let result: AISummaryResponse | undefined;
      service.generateSummary(request).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/summary`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.appointmentId).toBe('apt-123');
      req.flush(mockResponse);
      tick();

      expect(result?.summary).toContain('sinais vitais');
    }));

    it('should include pre-consultation data', fakeAsync(() => {
      const request: GenerateSummaryRequest = {
        appointmentId: 'apt-456',
        preConsultationData: {
          currentSymptoms: { mainSymptoms: 'Dor de cabeça', painIntensity: 7 }
        }
      };

      service.generateSummary(request).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/summary`);
      expect(req.request.body.preConsultationData.currentSymptoms.painIntensity).toBe(7);
      req.flush({ summary: 'Summary', generatedAt: new Date().toISOString() });
      tick();
    }));
  });

  describe('generateDiagnosis', () => {
    it('should generate diagnostic hypothesis', fakeAsync(() => {
      const request: GenerateDiagnosisRequest = {
        appointmentId: 'apt-789',
        additionalContext: 'Paciente com histórico de diabetes'
      };

      const mockResponse: AIDiagnosisResponse = {
        diagnosticHypothesis: 'Possível descompensação glicêmica',
        generatedAt: '2024-12-01T10:00:00Z'
      };

      let result: AIDiagnosisResponse | undefined;
      service.generateDiagnosis(request).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/diagnosis`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
      tick();

      expect(result?.diagnosticHypothesis).toBeTruthy();
    }));

    it('should include SOAP data for diagnosis', fakeAsync(() => {
      const request: GenerateDiagnosisRequest = {
        appointmentId: 'apt-101',
        soapData: {
          subjective: 'Paciente relata dor no peito',
          objective: 'PA: 140/90',
          assessment: 'Hipertensão',
          plan: 'Medicação anti-hipertensiva'
        }
      };

      service.generateDiagnosis(request).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/diagnosis`);
      expect(req.request.body.soapData.subjective).toContain('dor no peito');
      req.flush({ diagnosticHypothesis: 'Hypothesis', generatedAt: new Date().toISOString() });
      tick();
    }));
  });

  describe('getAIData', () => {
    it('should fetch AI data for an appointment', fakeAsync(() => {
      const appointmentId = 'apt-123';
      const mockData: AIData = {
        summary: 'Resumo clínico',
        summaryGeneratedAt: '2024-12-01T10:00:00Z',
        diagnosticHypothesis: 'Hipótese diagnóstica',
        diagnosisGeneratedAt: '2024-12-01T10:05:00Z'
      };

      let result: AIData | undefined;
      service.getAIData(appointmentId).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/appointment/${appointmentId}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockData);
      tick();

      expect(result?.summary).toBe('Resumo clínico');
      expect(result?.diagnosticHypothesis).toBe('Hipótese diagnóstica');
    }));

    it('should handle empty AI data', fakeAsync(() => {
      const appointmentId = 'apt-new';
      const emptyData: AIData = {};

      let result: AIData | undefined;
      service.getAIData(appointmentId).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/appointment/${appointmentId}`);
      req.flush(emptyData);
      tick();

      expect(result?.summary).toBeUndefined();
    }));
  });

  describe('saveAIData', () => {
    it('should save AI data for an appointment', fakeAsync(() => {
      const appointmentId = 'apt-123';
      const data: SaveAIData = {
        summary: 'Novo resumo',
        diagnosticHypothesis: 'Nova hipótese'
      };

      let result: { message: string } | undefined;
      service.saveAIData(appointmentId, data).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/appointment/${appointmentId}`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(data);
      req.flush({ message: 'Dados salvos com sucesso' });
      tick();

      expect(result?.message).toContain('sucesso');
    }));

    it('should save only summary', fakeAsync(() => {
      const appointmentId = 'apt-456';
      const data: SaveAIData = { summary: 'Apenas resumo' };

      service.saveAIData(appointmentId, data).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/appointment/${appointmentId}`);
      expect(req.request.body.summary).toBe('Apenas resumo');
      expect(req.request.body.diagnosticHypothesis).toBeUndefined();
      req.flush({ message: 'OK' });
      tick();
    }));
  });
});
