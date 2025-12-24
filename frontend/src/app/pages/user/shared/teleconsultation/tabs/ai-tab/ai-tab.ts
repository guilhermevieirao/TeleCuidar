import { Component, Input, OnInit, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { AIService, GenerateSummaryRequest, GenerateDiagnosisRequest, AIData } from '@core/services/ai.service';
import { Appointment } from '@core/services/appointments.service';

@Component({
  selector: 'app-ai-tab',
  standalone: true,
  imports: [CommonModule, FormsModule, ButtonComponent, IconComponent],
  templateUrl: './ai-tab.html',
  styleUrls: ['./ai-tab.scss']
})
export class AITabComponent implements OnInit {
  @Input() appointmentId: string | null = null;
  @Input() appointment: Appointment | null = null;
  @Input() userrole: 'PATIENT' | 'PROFESSIONAL' | 'ADMIN' = 'PROFESSIONAL';
  @Input() readonly = false;
  
  // Data from other tabs (to be collected)
  @Input() patientData: any = null;
  @Input() preConsultationData: any = null;
  @Input() anamnesisData: any = null;
  @Input() biometricsData: any = null;
  @Input() soapData: any = null;
  @Input() specialtyFieldsData: any = null;
  
  // AI Results
  summary: string = '';
  summaryGeneratedAt: Date | null = null;
  diagnosticHypothesis: string = '';
  diagnosisGeneratedAt: Date | null = null;
  
  // Form inputs
  additionalContext: string = '';
  
  // UI States
  isGeneratingSummary = false;
  isGeneratingDiagnosis = false;
  isSaving = false;
  isLoading = true;
  errorMessage: string = '';

  constructor(
    private aiService: AIService,
    private cdr: ChangeDetectorRef
  ) {}

  ngOnInit() {
    this.loadExistingData();
  }

  loadExistingData() {
    if (!this.appointmentId) {
      this.isLoading = false;
      return;
    }

    this.aiService.getAIData(this.appointmentId).subscribe({
      next: (data: AIData) => {
        if (data) {
          this.summary = data.summary || '';
          this.summaryGeneratedAt = data.summaryGeneratedAt ? new Date(data.summaryGeneratedAt) : null;
          this.diagnosticHypothesis = data.diagnosticHypothesis || '';
          this.diagnosisGeneratedAt = data.diagnosisGeneratedAt ? new Date(data.diagnosisGeneratedAt) : null;
        }
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error loading AI data:', err);
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  collectAllData(): { patientData: any; preConsultationData: any; anamnesisData: any; biometricsData: any; soapData: any; specialtyFieldsData: any } {
    // Try to extract data from the appointment object if not provided via inputs
    let preConsultation = this.preConsultationData;
    if (!preConsultation && this.appointment?.preConsultationJson) {
      try {
        preConsultation = JSON.parse(this.appointment.preConsultationJson);
      } catch (e) {
        console.warn('Could not parse preConsultationJson');
      }
    }

    // Construct patient data from appointment
    const patientData = this.patientData || {
      name: this.appointment?.patientName,
    };

    // Construct specialty data
    const specialtyFieldsData = this.specialtyFieldsData || {
      specialtyName: this.appointment?.specialtyName,
    };

    return {
      patientData,
      preConsultationData: preConsultation,
      anamnesisData: this.anamnesisData,
      biometricsData: this.biometricsData,
      soapData: this.soapData,
      specialtyFieldsData
    };
  }

  generateSummary() {
    if (!this.appointmentId || this.readonly) return;

    this.isGeneratingSummary = true;
    this.errorMessage = '';

    const collectedData = this.collectAllData();
    const request: GenerateSummaryRequest = {
      appointmentId: this.appointmentId,
      ...collectedData
    };

    this.aiService.generateSummary(request).subscribe({
      next: (response) => {
        this.summary = response.summary;
        this.summaryGeneratedAt = new Date(response.generatedAt);
        this.isGeneratingSummary = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error generating summary:', err);
        this.errorMessage = err.error?.message || 'Erro ao gerar resumo. Verifique a configuração da API de IA.';
        this.isGeneratingSummary = false;
        this.cdr.detectChanges();
      }
    });
  }

  generateDiagnosis() {
    if (!this.appointmentId || this.readonly) return;

    this.isGeneratingDiagnosis = true;
    this.errorMessage = '';

    const collectedData = this.collectAllData();
    const request: GenerateDiagnosisRequest = {
      appointmentId: this.appointmentId,
      additionalContext: this.additionalContext,
      ...collectedData
    };

    this.aiService.generateDiagnosis(request).subscribe({
      next: (response) => {
        this.diagnosticHypothesis = response.diagnosticHypothesis;
        this.diagnosisGeneratedAt = new Date(response.generatedAt);
        this.isGeneratingDiagnosis = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error generating diagnosis:', err);
        this.errorMessage = err.error?.message || 'Erro ao gerar hipótese diagnóstica. Verifique a configuração da API de IA.';
        this.isGeneratingDiagnosis = false;
        this.cdr.detectChanges();
      }
    });
  }

  saveData() {
    if (!this.appointmentId || this.readonly) return;

    this.isSaving = true;
    this.aiService.saveAIData(this.appointmentId, {
      summary: this.summary,
      diagnosticHypothesis: this.diagnosticHypothesis
    }).subscribe({
      next: () => {
        this.isSaving = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Error saving AI data:', err);
        this.isSaving = false;
        this.errorMessage = 'Erro ao salvar dados.';
        this.cdr.detectChanges();
      }
    });
  }

  clearError() {
    this.errorMessage = '';
  }
}
