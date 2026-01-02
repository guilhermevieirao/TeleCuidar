import { Component, Input, OnInit, OnDestroy, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { SignDocumentModalComponent, SignDocumentEvent } from '@shared/components/molecules/sign-document-modal/sign-document-modal';
import { MedicalReportService, MedicalReportDto, CreateMedicalReportDto, UpdateMedicalReportDto } from '@core/services/medical-report.service';
import { DigitalCertificateService } from '@core/services/digital-certificate.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
  selector: 'app-laudo-tab',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonComponent,
    IconComponent,
    SignDocumentModalComponent
  ],
  templateUrl: './laudo-tab.html',
  styleUrls: ['./laudo-tab.scss']
})
export class LaudoTabComponent implements OnInit, OnDestroy {
  @Input() appointmentId: string | null = null;
  @Input() userrole: 'PATIENT' | 'PROFESSIONAL' | 'ADMIN' = 'PATIENT';
  @Input() readonly = false;

  // State signals
  reports = signal<MedicalReportDto[]>([]);
  isLoading = signal(false);
  isSaving = signal(false);
  error = signal<string | null>(null);
  isFormOpen = signal(false);
  editingId = signal<string | null>(null);

  // Form data
  formData = signal<Partial<CreateMedicalReportDto>>({
    tipo: 'Exame',
    titulo: '',
    historicoClinico: '',
    exameFisico: '',
    examesComplementares: '',
    hipoteseDiagnostica: '',
    cid: '',
    conclusao: '',
    recomendacoes: '',
    observacoes: ''
  });

  // Signature modal
  isSignModalOpen = signal(false);
  selectedReportForSign = signal<MedicalReportDto | null>(null);

  // Computed
  canEdit = computed(() => this.userrole === 'PROFESSIONAL' || this.userrole === 'ADMIN');
  hasReports = computed(() => this.reports().length > 0);

  // Options
  tipoOptions = [
    { value: 'Exame', label: 'Laudo de Exame' },
    { value: 'Procedimento', label: 'Laudo de Procedimento' },
    { value: 'Pericial', label: 'Laudo Pericial' },
    { value: 'Avaliacao', label: 'Laudo de Avaliação' },
    { value: 'ParecerTecnico', label: 'Parecer Técnico' },
    { value: 'Complementar', label: 'Laudo Complementar' },
    { value: 'Outro', label: 'Outro' }
  ];

  private destroy$ = new Subject<void>();

  constructor(
    private reportService: MedicalReportService,
    private certificateService: DigitalCertificateService
  ) {}

  ngOnInit(): void {
    if (this.appointmentId) {
      this.loadReports();
    }
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadReports(): void {
    if (!this.appointmentId) return;

    this.isLoading.set(true);
    this.error.set(null);

    this.reportService.getByAppointmentId(this.appointmentId)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: (reports) => {
          this.reports.set(reports);
          this.isLoading.set(false);
        },
        error: (err) => {
          console.error('Erro ao carregar laudos:', err);
          this.error.set('Erro ao carregar laudos');
          this.isLoading.set(false);
        }
      });
  }

  openNewForm(): void {
    this.editingId.set(null);
    this.formData.set({
      tipo: 'Exame',
      titulo: '',
      historicoClinico: '',
      exameFisico: '',
      examesComplementares: '',
      hipoteseDiagnostica: '',
      cid: '',
      conclusao: '',
      recomendacoes: '',
      observacoes: ''
    });
    this.isFormOpen.set(true);
  }

  openEditForm(report: MedicalReportDto): void {
    if (report.isSigned) {
      this.error.set('Não é possível editar um laudo já assinado');
      return;
    }

    this.editingId.set(report.id);
    this.formData.set({
      tipo: report.tipo,
      titulo: report.titulo,
      historicoClinico: report.historicoClinico || '',
      exameFisico: report.exameFisico || '',
      examesComplementares: report.examesComplementares || '',
      hipoteseDiagnostica: report.hipoteseDiagnostica || '',
      cid: report.cid || '',
      conclusao: report.conclusao,
      recomendacoes: report.recomendacoes || '',
      observacoes: report.observacoes || ''
    });
    this.isFormOpen.set(true);
  }

  cancelForm(): void {
    this.isFormOpen.set(false);
    this.editingId.set(null);
  }

  updateFormField(field: string, value: string): void {
    this.formData.update(data => ({ ...data, [field]: value }));
  }

  saveReport(): void {
    if (!this.appointmentId) return;

    const data = this.formData();
    if (!data.titulo || !data.conclusao) {
      this.error.set('Título e conclusão são obrigatórios');
      return;
    }

    this.isSaving.set(true);
    this.error.set(null);

    if (this.editingId()) {
      // Update
      const updateDto: UpdateMedicalReportDto = { ...data };
      this.reportService.update(this.editingId()!, updateDto)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.loadReports();
            this.cancelForm();
            this.isSaving.set(false);
          },
          error: (err) => {
            console.error('Erro ao atualizar laudo:', err);
            this.error.set(err.error?.message || 'Erro ao atualizar laudo');
            this.isSaving.set(false);
          }
        });
    } else {
      // Create
      const createDto: CreateMedicalReportDto = {
        appointmentId: this.appointmentId,
        tipo: data.tipo || 'Exame',
        titulo: data.titulo!,
        historicoClinico: data.historicoClinico,
        exameFisico: data.exameFisico,
        examesComplementares: data.examesComplementares,
        hipoteseDiagnostica: data.hipoteseDiagnostica,
        cid: data.cid,
        conclusao: data.conclusao!,
        recomendacoes: data.recomendacoes,
        observacoes: data.observacoes
      };

      this.reportService.create(createDto)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: () => {
            this.loadReports();
            this.cancelForm();
            this.isSaving.set(false);
          },
          error: (err) => {
            console.error('Erro ao criar laudo:', err);
            this.error.set(err.error?.message || 'Erro ao criar laudo');
            this.isSaving.set(false);
          }
        });
    }
  }

  deleteReport(report: MedicalReportDto): void {
    if (report.isSigned) {
      this.error.set('Não é possível excluir um laudo já assinado');
      return;
    }

    if (!confirm('Deseja realmente excluir este laudo?')) return;

    this.reportService.delete(report.id)
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => this.loadReports(),
        error: (err) => {
          console.error('Erro ao excluir laudo:', err);
          this.error.set(err.error?.message || 'Erro ao excluir laudo');
        }
      });
  }

  downloadPdf(report: MedicalReportDto): void {
    if (report.isSigned) {
      this.downloadSignedPdf(report);
    } else {
      this.reportService.generatePdf(report.id)
        .pipe(takeUntil(this.destroy$))
        .subscribe({
          next: (blob) => {
            const url = window.URL.createObjectURL(blob);
            const a = document.createElement('a');
            a.href = url;
            a.download = `Laudo_${report.titulo}_${new Date(report.dataEmissao).toLocaleDateString('pt-BR').replace(/\//g, '-')}.pdf`;
            a.click();
            window.URL.revokeObjectURL(url);
          },
          error: (err) => {
            console.error('Erro ao baixar PDF:', err);
            this.error.set('Erro ao baixar PDF');
          }
        });
    }
  }

  downloadSignedPdf(report: MedicalReportDto): void {
    this.certificateService.downloadSignedReportPdf(report.id);
  }

  openSignModal(report: MedicalReportDto): void {
    if (report.isSigned) {
      this.error.set('Este laudo já está assinado');
      return;
    }
    this.selectedReportForSign.set(report);
    this.isSignModalOpen.set(true);
  }

  closeSignModal(): void {
    this.isSignModalOpen.set(false);
    this.selectedReportForSign.set(null);
  }

  onDocumentSigned(event: SignDocumentEvent): void {
    if (event.success) {
      this.loadReports();
      this.closeSignModal();
    } else {
      this.error.set('Erro ao assinar documento');
    }
  }

  getTipoLabel(tipo: string): string {
    const option = this.tipoOptions.find(o => o.value === tipo);
    return option?.label || tipo;
  }

  formatDate(dateString: string): string {
    if (!dateString) return '';
    return new Date(dateString).toLocaleDateString('pt-BR');
  }

  formatDateTime(dateString: string): string {
    if (!dateString) return '';
    return new Date(dateString).toLocaleString('pt-BR');
  }
}
