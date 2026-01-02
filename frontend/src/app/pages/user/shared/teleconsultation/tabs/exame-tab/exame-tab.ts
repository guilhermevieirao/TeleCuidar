import { Component, Input, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Subject, takeUntil } from 'rxjs';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { ModalService } from '@core/services/modal.service';
import { SignDocumentModalComponent, SignDocumentEvent } from '@shared/components/molecules/sign-document-modal/sign-document-modal';
import { DigitalCertificateService } from '@core/services/digital-certificate.service';
import { 
  ExamRequestService, 
  ExamRequest, 
  CreateExamRequestDto,
  UpdateExamRequestDto,
  ExamCategory,
  ExamPriority
} from '@core/services/exam-request.service';

@Component({
  selector: 'app-exame-tab',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, ButtonComponent, IconComponent, SignDocumentModalComponent],
  templateUrl: './exame-tab.html',
  styleUrls: ['./exame-tab.scss']
})
export class ExameTabComponent implements OnInit, OnDestroy {
  @Input() appointmentId: string | null = null;
  @Input() userrole: 'PATIENT' | 'PROFESSIONAL' | 'ADMIN' = 'PATIENT';
  @Input() readonly = false;

  exams: ExamRequest[] = [];
  isLoading = true;
  isSaving = false;
  isGeneratingPdf = false;
  showForm = false;
  editingExam: ExamRequest | null = null;
  
  // Form para solicitação de exame
  examForm: FormGroup;
  
  // Categorias de exame
  categorias: { value: ExamCategory; label: string }[] = [
    { value: 'laboratorial', label: 'Laboratorial' },
    { value: 'imagem', label: 'Imagem' },
    { value: 'cardiologico', label: 'Cardiológico' },
    { value: 'oftalmologico', label: 'Oftalmológico' },
    { value: 'audiometrico', label: 'Audiométrico' },
    { value: 'neurologico', label: 'Neurológico' },
    { value: 'endoscopico', label: 'Endoscópico' },
    { value: 'outro', label: 'Outro' }
  ];

  // Prioridades
  prioridades: { value: ExamPriority; label: string }[] = [
    { value: 'normal', label: 'Normal' },
    { value: 'urgente', label: 'Urgente' }
  ];

  // Modal de assinatura
  showSignModal = false;
  signingExamId: string | null = null;

  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private modalService: ModalService,
    private examService: ExamRequestService,
    private digitalCertificateService: DigitalCertificateService,
    private cdr: ChangeDetectorRef
  ) {
    this.examForm = this.fb.group({
      nomeExame: ['', Validators.required],
      codigoExame: [''],
      categoria: ['laboratorial', Validators.required],
      prioridade: ['normal', Validators.required],
      dataLimite: [''],
      indicacaoClinica: ['', Validators.required],
      hipoteseDiagnostica: [''],
      cid: [''],
      observacoes: [''],
      instrucoesPreparo: ['']
    });
  }

  ngOnInit() {
    if (this.appointmentId) {
      this.loadExams();
    }
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadExams() {
    if (!this.appointmentId) return;

    this.isLoading = true;
    this.examService.getByAppointment(this.appointmentId).subscribe({
      next: (exams) => {
        this.exams = exams;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erro ao carregar solicitações de exame:', err);
        this.exams = [];
        this.isLoading = false;
        this.cdr.detectChanges();
      }
    });
  }

  get isProfessional(): boolean {
    return this.userrole === 'PROFESSIONAL' || this.userrole === 'ADMIN';
  }

  get canEdit(): boolean {
    return this.isProfessional && !this.readonly;
  }

  startNewExam() {
    this.editingExam = null;
    this.examForm.reset({ categoria: 'laboratorial', prioridade: 'normal' });
    this.showForm = true;
  }

  editExam(exam: ExamRequest) {
    if (exam.isSigned) return; // Não pode editar exame assinado
    
    this.editingExam = exam;
    this.examForm.patchValue({
      nomeExame: exam.nomeExame,
      codigoExame: exam.codigoExame || '',
      categoria: exam.categoria,
      prioridade: exam.prioridade,
      dataLimite: exam.dataLimite ? exam.dataLimite.split('T')[0] : '',
      indicacaoClinica: exam.indicacaoClinica,
      hipoteseDiagnostica: exam.hipoteseDiagnostica || '',
      cid: exam.cid || '',
      observacoes: exam.observacoes || '',
      instrucoesPreparo: exam.instrucoesPreparo || ''
    });
    this.showForm = true;
  }

  cancelForm() {
    this.showForm = false;
    this.editingExam = null;
    this.examForm.reset({ categoria: 'laboratorial', prioridade: 'normal' });
  }

  saveExam() {
    if (this.examForm.invalid || !this.appointmentId) return;

    this.isSaving = true;
    const formValue = this.examForm.value;

    if (this.editingExam) {
      // Atualizar solicitação de exame existente
      const updateDto: UpdateExamRequestDto = {
        nomeExame: formValue.nomeExame,
        codigoExame: formValue.codigoExame || undefined,
        categoria: formValue.categoria,
        prioridade: formValue.prioridade,
        dataLimite: formValue.dataLimite || undefined,
        indicacaoClinica: formValue.indicacaoClinica,
        hipoteseDiagnostica: formValue.hipoteseDiagnostica || undefined,
        cid: formValue.cid || undefined,
        observacoes: formValue.observacoes || undefined,
        instrucoesPreparo: formValue.instrucoesPreparo || undefined
      };

      this.examService.update(this.editingExam.id, updateDto).subscribe({
        next: () => {
          this.isSaving = false;
          this.showForm = false;
          this.editingExam = null;
          this.loadExams();
          this.modalService.alert({ title: 'Sucesso', message: 'Solicitação de exame atualizada com sucesso!', variant: 'success' });
        },
        error: (err) => {
          console.error('Erro ao atualizar solicitação de exame:', err);
          this.isSaving = false;
          this.cdr.detectChanges();
          this.modalService.alert({ title: 'Erro', message: err.error?.message || 'Não foi possível atualizar a solicitação de exame.', variant: 'danger' });
        }
      });
    } else {
      // Criar nova solicitação de exame
      const createDto: CreateExamRequestDto = {
        appointmentId: this.appointmentId,
        nomeExame: formValue.nomeExame,
        codigoExame: formValue.codigoExame || undefined,
        categoria: formValue.categoria,
        prioridade: formValue.prioridade,
        dataLimite: formValue.dataLimite || undefined,
        indicacaoClinica: formValue.indicacaoClinica,
        hipoteseDiagnostica: formValue.hipoteseDiagnostica || undefined,
        cid: formValue.cid || undefined,
        observacoes: formValue.observacoes || undefined,
        instrucoesPreparo: formValue.instrucoesPreparo || undefined
      };

      this.examService.create(createDto).subscribe({
        next: () => {
          this.isSaving = false;
          this.showForm = false;
          this.loadExams();
          this.modalService.alert({ title: 'Sucesso', message: 'Solicitação de exame criada com sucesso!', variant: 'success' });
        },
        error: (err) => {
          console.error('Erro ao criar solicitação de exame:', err);
          this.isSaving = false;
          this.cdr.detectChanges();
          this.modalService.alert({ title: 'Erro', message: err.error?.message || 'Não foi possível criar a solicitação de exame.', variant: 'danger' });
        }
      });
    }
  }

  deleteExam(exam: ExamRequest) {
    if (exam.isSigned) {
      this.modalService.alert({ title: 'Erro', message: 'Não é possível excluir uma solicitação de exame já assinada.', variant: 'danger' });
      return;
    }

    this.modalService.confirm({
      title: 'Excluir Solicitação de Exame',
      message: 'Tem certeza que deseja excluir esta solicitação de exame? Esta ação não pode ser desfeita.',
      confirmText: 'Sim, excluir',
      cancelText: 'Cancelar',
      variant: 'danger'
    }).subscribe(result => {
      if (result.confirmed) {
        this.examService.delete(exam.id).subscribe({
          next: () => {
            this.loadExams();
            this.modalService.alert({ title: 'Sucesso', message: 'Solicitação de exame excluída com sucesso.', variant: 'success' });
          },
          error: (err) => {
            console.error('Erro ao excluir solicitação de exame:', err);
            this.modalService.alert({ title: 'Erro', message: err.error?.message || 'Não foi possível excluir a solicitação de exame.', variant: 'danger' });
          }
        });
      }
    });
  }

  generatePdf(exam: ExamRequest) {
    this.isGeneratingPdf = true;
    
    this.examService.generatePdf(exam.id).subscribe({
      next: (response) => {
        // Converter base64 para blob
        const byteCharacters = atob(response.pdfBase64);
        const byteNumbers = new Array(byteCharacters.length);
        for (let i = 0; i < byteCharacters.length; i++) {
          byteNumbers[i] = byteCharacters.charCodeAt(i);
        }
        const byteArray = new Uint8Array(byteNumbers);
        const blob = new Blob([byteArray], { type: 'application/pdf' });
        
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = response.fileName || `exame_${exam.categoria}_${new Date().toISOString().split('T')[0]}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.isGeneratingPdf = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erro ao gerar PDF:', err);
        this.isGeneratingPdf = false;
        this.cdr.detectChanges();
        this.modalService.alert({ title: 'Erro', message: err.error?.message || 'Não foi possível gerar o PDF.', variant: 'danger' });
      }
    });
  }

  // === Download PDF Assinado ===

  downloadSignedPdf(exam: ExamRequest) {
    if (!exam.isSigned) return;
    this.digitalCertificateService.downloadSignedExamPdf(exam.id);
  }

  getCategoriaLabel(categoria: string): string {
    const found = this.categorias.find(c => c.value === categoria);
    return found ? found.label : categoria;
  }

  getPrioridadeLabel(prioridade: string): string {
    const found = this.prioridades.find(p => p.value === prioridade);
    return found ? found.label : prioridade;
  }

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('pt-BR');
  }

  // === Assinatura Digital ===

  openSignModal(exam: ExamRequest) {
    if (exam.isSigned) {
      this.modalService.alert({ 
        title: 'Aviso', 
        message: 'Esta solicitação de exame já está assinada.', 
        variant: 'warning' 
      });
      return;
    }

    this.signingExamId = exam.id;
    this.showSignModal = true;
    this.cdr.detectChanges();
  }

  closeSignModal() {
    this.showSignModal = false;
    this.signingExamId = null;
  }

  onDocumentSigned(event: SignDocumentEvent) {
    if (event.success) {
      this.modalService.alert({ 
        title: 'Sucesso', 
        message: 'Solicitação de exame assinada com sucesso!', 
        variant: 'success' 
      });
      this.loadExams();
    }
    this.closeSignModal();
  }
}
