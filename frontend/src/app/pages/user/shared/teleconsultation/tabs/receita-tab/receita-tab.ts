import { Component, Input, OnInit, OnDestroy, ViewChild, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { Subject, debounceTime, distinctUntilChanged, takeUntil } from 'rxjs';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { DigitalSignatureComponent, DigitalSignatureResult } from '@shared/components/molecules/digital-signature/digital-signature';
import { ModalService } from '@core/services/modal.service';
import { 
  PrescriptionService, 
  Prescription, 
  CreatePrescriptionDto,
  AddPrescriptionItemDto,
  UpdatePrescriptionItemDto,
  MedicamentoAnvisa,
  PrescriptionItem
} from '@core/services/prescription.service';

@Component({
  selector: 'app-receita-tab',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule, ButtonComponent, IconComponent, DigitalSignatureComponent],
  templateUrl: './receita-tab.html',
  styleUrls: ['./receita-tab.scss']
})
export class ReceitaTabComponent implements OnInit, OnDestroy {
  @Input() appointmentId: string | null = null;
  @Input() userrole: 'PATIENT' | 'PROFESSIONAL' | 'ADMIN' = 'PATIENT';
  @Input() readonly = false;
  
  @ViewChild('digitalSignature') digitalSignature!: DigitalSignatureComponent;

  prescriptions: Prescription[] = [];
  isLoading = true;
  isSaving = false;
  isGeneratingPdf = false;
  
  // Estado do formulário de itens
  showItemForm = false;
  currentPrescriptionId: string | null = null;
  
  // Form para adicionar item
  itemForm: FormGroup;
  
  // Busca de medicamentos
  medicamentoSearch = '';
  medicamentoResults: MedicamentoAnvisa[] = [];
  showMedicamentoDropdown = false;
  isSearching = false;
  
  // Assinatura digital
  isSigning = false;
  signingPrescriptionId: string | null = null;

  // Toast de sucesso
  showSuccessToast = false;
  successToastMessage = '';
  private toastTimeout: any;

  // Modo de edição
  isEditMode = false;
  editingItemId: string | null = null;

  private destroy$ = new Subject<void>();
  private searchSubject = new Subject<string>();

  constructor(
    private fb: FormBuilder,
    private modalService: ModalService,
    private prescriptionService: PrescriptionService,
    private cdr: ChangeDetectorRef
  ) {
    this.itemForm = this.fb.group({
      medicamento: ['', Validators.required],
      codigoAnvisa: [''],
      dosagem: ['', Validators.required],
      frequencia: ['', Validators.required],
      periodo: ['', Validators.required],
      posologia: ['', Validators.required],
      observacoes: ['']
    });
  }

  ngOnInit() {
    if (this.appointmentId) {
      this.loadPrescriptions();
    }
    
    // Setup debounced search for medicamentos
    this.searchSubject.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntil(this.destroy$)
    ).subscribe(query => {
      if (query.length >= 2) {
        this.searchMedicamentos(query);
      } else {
        this.medicamentoResults = [];
        this.showMedicamentoDropdown = false;
      }
    });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
    if (this.toastTimeout) {
      clearTimeout(this.toastTimeout);
    }
  }

  // === Toast de sucesso ===
  
  showToast(message: string) {
    this.successToastMessage = message;
    this.showSuccessToast = true;
    
    // Limpar timeout anterior se existir
    if (this.toastTimeout) {
      clearTimeout(this.toastTimeout);
    }
    
    // Auto-hide após 4 segundos
    this.toastTimeout = setTimeout(() => {
      this.hideToast();
    }, 4000);
    
    this.cdr.detectChanges();
  }

  hideToast() {
    this.showSuccessToast = false;
    this.successToastMessage = '';
    this.cdr.detectChanges();
  }

  loadPrescriptions() {
    if (!this.appointmentId) return;

    this.isLoading = true;
    this.prescriptionService.getPrescriptionsByAppointment(this.appointmentId).subscribe({
      next: (prescriptions) => {
        this.prescriptions = prescriptions;
        this.isLoading = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erro ao carregar receitas:', err);
        this.prescriptions = [];
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

  // === Criação de nova receita ===
  
  startNewPrescription() {
    if (!this.appointmentId) return;
    
    this.isSaving = true;
    const dto: CreatePrescriptionDto = {
      appointmentId: this.appointmentId,
      items: []
    };
    
    this.prescriptionService.createPrescription(dto).subscribe({
      next: (prescription) => {
        this.isSaving = false;
        this.loadPrescriptions();
        // Abrir form de adicionar item para a nova receita
        this.currentPrescriptionId = prescription.id;
        this.showItemForm = true;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erro ao criar receita:', err);
        this.isSaving = false;
        this.cdr.detectChanges();
        this.modalService.alert({ 
          title: 'Erro', 
          message: err.error?.message || 'Não foi possível criar a receita.', 
          variant: 'danger' 
        });
      }
    });
  }

  // === Gerenciamento de itens (medicamentos) ===
  
  openAddItemForm(prescriptionId: string) {
    this.currentPrescriptionId = prescriptionId;
    this.itemForm.reset();
    this.medicamentoSearch = '';
    this.showItemForm = true;
    this.isEditMode = false;
    this.editingItemId = null;
  }

  openEditItemForm(prescription: Prescription, item: PrescriptionItem) {
    if (prescription.isSigned) return;
    
    this.currentPrescriptionId = prescription.id;
    this.editingItemId = item.id;
    this.isEditMode = true;
    this.showItemForm = true;
    
    // Preencher o formulário com os dados do item
    this.itemForm.patchValue({
      medicamento: item.medicamento,
      codigoAnvisa: item.codigoAnvisa || '',
      dosagem: item.dosagem,
      frequencia: item.frequencia,
      periodo: item.periodo,
      posologia: item.posologia,
      observacoes: item.observacoes || ''
    });
    this.medicamentoSearch = item.medicamento;
  }

  cancelItemForm() {
    this.showItemForm = false;
    this.currentPrescriptionId = null;
    this.isEditMode = false;
    this.editingItemId = null;
    this.itemForm.reset();
    this.medicamentoSearch = '';
    this.medicamentoResults = [];
    this.showMedicamentoDropdown = false;
  }

  addItem() {
    if (this.itemForm.invalid || !this.currentPrescriptionId) return;

    this.isSaving = true;
    const formData = this.itemForm.value;

    // Se está em modo de edição, atualizar o item existente
    if (this.isEditMode && this.editingItemId) {
      const updateDto: UpdatePrescriptionItemDto = formData;
      
      this.prescriptionService.updateItem(this.currentPrescriptionId, this.editingItemId, updateDto).subscribe({
        next: () => {
          this.isSaving = false;
          this.itemForm.reset();
          this.medicamentoSearch = '';
          this.isEditMode = false;
          this.editingItemId = null;
          this.showItemForm = false;
          this.loadPrescriptions();
          this.showToast('Medicamento atualizado com sucesso!');
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Erro ao atualizar item:', err);
          this.isSaving = false;
          this.cdr.detectChanges();
          this.modalService.alert({ 
            title: 'Erro', 
            message: err.error?.message || 'Não foi possível atualizar o medicamento.', 
            variant: 'danger' 
          });
        }
      });
    } else {
      // Adicionar novo item
      const item: AddPrescriptionItemDto = formData;

      this.prescriptionService.addItem(this.currentPrescriptionId, item).subscribe({
        next: () => {
          this.isSaving = false;
          this.itemForm.reset();
          this.medicamentoSearch = '';
          this.loadPrescriptions();
          this.showToast('Medicamento adicionado! Você pode adicionar mais ou fechar o formulário.');
          this.cdr.detectChanges();
        },
        error: (err) => {
          console.error('Erro ao adicionar item:', err);
          this.isSaving = false;
          this.cdr.detectChanges();
          this.modalService.alert({ 
            title: 'Erro', 
            message: err.error?.message || 'Não foi possível adicionar o medicamento.', 
            variant: 'danger' 
          });
        }
      });
    }
  }

  removeItem(prescriptionId: string, itemId: string) {
    this.modalService.confirm({
      title: 'Remover Medicamento',
      message: 'Tem certeza que deseja remover este medicamento?',
      confirmText: 'Sim, remover',
      cancelText: 'Cancelar',
      variant: 'danger'
    }).subscribe(result => {
      if (result.confirmed) {
        this.prescriptionService.removeItem(prescriptionId, itemId).subscribe({
          next: () => {
            this.loadPrescriptions();
          },
          error: (err) => {
            console.error('Erro ao remover item:', err);
            this.modalService.alert({ 
              title: 'Erro', 
              message: err.error?.message || 'Não foi possível remover o medicamento.', 
              variant: 'danger' 
            });
          }
        });
      }
    });
  }

  // === Busca de medicamentos ANVISA ===

  onMedicamentoInput(event: Event) {
    const input = event.target as HTMLInputElement;
    this.medicamentoSearch = input.value;
    
    this.itemForm.patchValue({
      medicamento: input.value,
      codigoAnvisa: ''
    });
    
    if (input.value.length >= 2) {
      this.isSearching = true;
    } else {
      this.isSearching = false;
    }
    
    this.searchSubject.next(input.value);
  }

  searchMedicamentos(query: string) {
    this.prescriptionService.searchMedicamentos(query).subscribe({
      next: (results) => {
        this.medicamentoResults = results;
        this.showMedicamentoDropdown = results.length > 0;
        this.isSearching = false;
        this.cdr.detectChanges();
      },
      error: () => {
        this.isSearching = false;
        this.medicamentoResults = [];
        this.cdr.detectChanges();
      }
    });
  }

  selectMedicamento(medicamento: MedicamentoAnvisa) {
    this.itemForm.patchValue({
      medicamento: medicamento.nome,
      codigoAnvisa: medicamento.codigo
    });
    this.medicamentoSearch = medicamento.nome;
    this.showMedicamentoDropdown = false;
  }

  useFreeText() {
    this.itemForm.patchValue({
      medicamento: this.medicamentoSearch,
      codigoAnvisa: ''
    });
    this.showMedicamentoDropdown = false;
  }

  hideDropdown() {
    setTimeout(() => {
      this.showMedicamentoDropdown = false;
    }, 200);
  }

  // === Exclusão de receita ===

  deletePrescription(prescription: Prescription) {
    if (prescription.isSigned) {
      this.modalService.alert({ 
        title: 'Erro', 
        message: 'Não é possível excluir uma receita já assinada.', 
        variant: 'danger' 
      });
      return;
    }

    this.modalService.confirm({
      title: 'Excluir Receita',
      message: 'Tem certeza que deseja excluir esta receita? Esta ação não pode ser desfeita.',
      confirmText: 'Sim, excluir',
      cancelText: 'Cancelar',
      variant: 'danger'
    }).subscribe(result => {
      if (result.confirmed) {
        this.prescriptionService.deletePrescription(prescription.id).subscribe({
          next: () => {
            this.loadPrescriptions();
            this.modalService.alert({ 
              title: 'Sucesso', 
              message: 'Receita excluída com sucesso.', 
              variant: 'success' 
            });
          },
          error: (err) => {
            console.error('Erro ao excluir receita:', err);
            this.modalService.alert({ 
              title: 'Erro', 
              message: err.error?.message || 'Não foi possível excluir a receita.', 
              variant: 'danger' 
            });
          }
        });
      }
    });
  }

  // === Geração de PDF ===

  generatePdf(prescription: Prescription) {
    this.isGeneratingPdf = true;
    
    this.prescriptionService.generatePdf(prescription.id).subscribe({
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
        a.download = response.fileName || `receita_${new Date().toISOString().split('T')[0]}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.isGeneratingPdf = false;
        this.cdr.detectChanges();
      },
      error: (err) => {
        console.error('Erro ao gerar PDF:', err);
        this.isGeneratingPdf = false;
        this.cdr.detectChanges();
        this.modalService.alert({ 
          title: 'Erro', 
          message: err.error?.message || 'Não foi possível gerar o PDF.', 
          variant: 'danger' 
        });
      }
    });
  }

  // === Assinatura Digital ===

  openSignatureOptions(prescription: Prescription) {
    this.signingPrescriptionId = prescription.id;
    this.digitalSignature.open();
  }

  onSign(result: DigitalSignatureResult) {
    console.log('onSign chamado com resultado:', result);
    
    if (!this.signingPrescriptionId) {
      console.error('signingPrescriptionId é null!');
      return;
    }

    this.isSigning = true;

    if (result.type === 'saved-cert') {
      this.prescriptionService.signWithSavedCert(
        this.signingPrescriptionId, 
        result.certificateId!.toString(),
        result.password
      ).subscribe({
        next: () => this.handleSignatureSuccess(),
        error: (err) => this.handleSignatureError(err)
      });
    } else {
      this.prescriptionService.generateSignedPdf(
        this.signingPrescriptionId,
        result.pfxBase64!,
        result.password!
      ).subscribe({
        next: () => this.handleSignatureSuccess(),
        error: (err) => this.handleSignatureError(err)
      });
    }
  }

  private handleSignatureSuccess() {
    this.isSigning = false;
    this.signingPrescriptionId = null;
    this.loadPrescriptions();
    this.cdr.detectChanges();
    this.modalService.alert({ 
      title: 'Sucesso', 
      message: 'Receita assinada com sucesso!', 
      variant: 'success' 
    });
  }

  private handleSignatureError(err: any) {
    console.error('Erro ao assinar receita:', err);
    this.isSigning = false;
    this.cdr.detectChanges();
    this.modalService.alert({ 
      title: 'Erro', 
      message: err.error?.message || 'Não foi possível assinar a receita.', 
      variant: 'danger' 
    });
  }

  onSignatureCancel() {
    this.isSigning = false;
    this.signingPrescriptionId = null;
  }

  onCertificateSaved() {
    // Não faz nada - deixa o modal aberto para o auto-apply do certificado
  }

  // === Helpers ===

  formatDate(dateStr: string): string {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('pt-BR');
  }

  hasItems(prescription: Prescription): boolean {
    return prescription.items && prescription.items.length > 0;
  }
}
