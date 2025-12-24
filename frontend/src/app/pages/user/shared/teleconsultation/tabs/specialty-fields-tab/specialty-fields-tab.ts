import { Component, Input, OnInit, OnDestroy, ChangeDetectorRef } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { IconComponent } from '@shared/components/atoms/icon/icon';
import { ButtonComponent } from '@shared/components/atoms/button/button';
import { AppointmentsService, Appointment } from '@core/services/appointments.service';
import { Subject, takeUntil, debounceTime } from 'rxjs';

interface SpecialtyField {
  name: string;
  type: 'text' | 'textarea' | 'number' | 'select' | 'checkbox' | 'radio' | 'date';
  required?: boolean;
  description?: string;
  defaultValue?: string;
  options?: string[];
  order?: number;
  placeholder?: string;
}

@Component({
  selector: 'app-specialty-fields-tab',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, IconComponent, ButtonComponent],
  templateUrl: './specialty-fields-tab.html',
  styleUrls: ['./specialty-fields-tab.scss']
})
export class SpecialtyFieldsTabComponent implements OnInit, OnDestroy {
  @Input() appointment: Appointment | null = null;
  @Input() appointmentId: string | null = null;
  @Input() userrole: 'PATIENT' | 'PROFESSIONAL' | 'ADMIN' = 'PROFESSIONAL';
  @Input() readonly = false;

  specialtyFieldsForm: FormGroup;
  specialtyFields: SpecialtyField[] = [];
  isSaving = false;
  lastSaved: Date | null = null;
  private destroy$ = new Subject<void>();

  constructor(
    private fb: FormBuilder,
    private appointmentsService: AppointmentsService,
    private cdr: ChangeDetectorRef
  ) {
    this.specialtyFieldsForm = this.fb.group({});
  }

  ngOnInit() {
    this.loadSpecialtyFields();
    
    // Desabilitar para pacientes ou modo readonly (somente visualização)
    if (this.userrole === 'PATIENT' || this.readonly) {
      this.specialtyFieldsForm.disable();
      return;
    }
    
    // Auto-save on value changes (debounced) - apenas para profissionais
    this.specialtyFieldsForm.valueChanges
      .pipe(
        takeUntil(this.destroy$),
        debounceTime(2000)
      )
      .subscribe(() => {
        if (this.specialtyFieldsForm.dirty) {
          this.saveSpecialtyFields();
        }
      });
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }

  loadSpecialtyFields() {
    console.log('=== DEBUG SPECIALTY FIELDS ===');
    console.log('Appointment:', this.appointment);
    console.log('Appointment.specialty:', this.appointment?.specialty);
    console.log('CustomFieldsJson:', this.appointment?.specialty?.customFieldsJson);
    
    // Tentar carregar campos customizados do appointment.specialty
    if (this.appointment?.specialty?.customFieldsJson) {
      try {
        const customFields = JSON.parse(this.appointment.specialty.customFieldsJson);
        console.log('Parsed customFields:', customFields);
        if (Array.isArray(customFields) && customFields.length > 0) {
          // Ordenar campos pelo order se existir
          this.specialtyFields = customFields.sort((a: any, b: any) => (a.order || 0) - (b.order || 0));
          console.log('✅ Campos customizados carregados:', this.specialtyFields);
          
          // Criar controles do formulário dinamicamente
          this.specialtyFields.forEach(field => {
            const fieldId = this.sanitizeFieldName(field.name);
            const defaultValue = field.defaultValue || '';
            this.specialtyFieldsForm.addControl(fieldId, this.fb.control(defaultValue));
          });
          
          // Carregar dados salvos se existirem
          this.loadSavedData();
          this.cdr.detectChanges();
          return;
        }
      } catch (error) {
        console.error('Erro ao parsear customFieldsJson:', error);
      }
    }
    
    console.log('⚠️ Usando campos genéricos (fallback)');
    // Fallback: usar campos genéricos se não houver campos customizados
    this.specialtyFields = this.getGeneralFields();
    
    // Criar controles do formulário dinamicamente
    this.specialtyFields.forEach(field => {
      const fieldId = this.sanitizeFieldName(field.name);
      this.specialtyFieldsForm.addControl(fieldId, this.fb.control(''));
    });

    // Carregar dados salvos se existirem
    this.loadSavedData();
    this.cdr.detectChanges();
  }
  
  // Sanitizar nome do campo para usar como ID (público para uso no template)
  sanitizeFieldName(name: string): string {
    return name
      .toLowerCase()
      .normalize('NFD')
      .replace(/[\u0300-\u036f]/g, '') // Remove acentos
      .replace(/[^a-z0-9]+/g, '_') // Substitui caracteres especiais por _
      .replace(/^_+|_+$/g, ''); // Remove _ no início e fim
  }

  loadSavedData() {
    // Carregar dados salvos do backend
    if (this.appointment?.specialtyFieldsJson) {
      try {
        const savedData = JSON.parse(this.appointment.specialtyFieldsJson);
        this.specialtyFieldsForm.patchValue(savedData);
        this.specialtyFieldsForm.markAsPristine();
      } catch (error) {
        console.error('Erro ao carregar dados salvos:', error);
      }
    }
  }

  saveSpecialtyFields() {
    if (this.specialtyFieldsForm.invalid || !this.appointmentId) return;

    this.isSaving = true;
    
    const specialtyFieldsJson = JSON.stringify(this.specialtyFieldsForm.value);
    
    this.appointmentsService.updateAppointment(this.appointmentId, { specialtyFieldsJson })
      .pipe(takeUntil(this.destroy$))
      .subscribe({
        next: () => {
          this.isSaving = false;
          this.lastSaved = new Date();
          this.specialtyFieldsForm.markAsPristine();
          this.cdr.detectChanges();
        },
        error: (error) => {
          console.error('Erro ao salvar campos da especialidade:', error);
          this.isSaving = false;
          this.cdr.detectChanges();
        }
      });
  }

  onManualSave() {
    this.saveSpecialtyFields();
  }

  // Campos genéricos como fallback
  private getGeneralFields(): SpecialtyField[] {
    return [
      { name: 'Queixas Específicas da Especialidade', type: 'textarea', description: 'Descreva as queixas específicas...', order: 1 },
      { name: 'Exame Clínico Específico', type: 'textarea', description: 'Achados do exame físico...', order: 2 },
      { name: 'Exames Complementares', type: 'textarea', description: 'Exames solicitados ou resultados...', order: 3 },
      { name: 'Observações da Especialidade', type: 'textarea', description: 'Anotações adicionais...', order: 4 }
    ];
  }
}
