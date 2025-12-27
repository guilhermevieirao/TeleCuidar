import { Injectable } from '@angular/core';
import { Appointment } from './appointments.service';
import { UsersService } from './users.service';
import { BiometricsService } from './biometrics.service';
import { forkJoin, of } from 'rxjs';
import { catchError, map } from 'rxjs/operators';

/**
 * Serviço para coletar dados de todas as abas para usar na IA
 * Garante que TODAS as informações disponíveis sejam incluídas nos resumos e hipóteses diagnósticas
 */
@Injectable({
  providedIn: 'root'
})
export class TeleconsultationDataCollectorService {
  
  constructor(
    private usersService: UsersService,
    private biometricsService: BiometricsService
  ) {}

  /**
   * Coleta TODOS os dados disponíveis de uma consulta para uso na IA
   * Inclui dados de todas as abas
   */
  collectAllData(appointment: Appointment, appointmentId: string) {
    return forkJoin({
      patientData: this.getPatientData(appointment),
      preConsultationData: this.getPreConsultationData(appointment),
      anamnesisData: this.getAnamnesisData(appointment),
      biometricsData: this.getBiometricsData(appointmentId),
      soapData: this.getSoapData(appointment),
      specialtyFieldsData: this.getSpecialtyFieldsData(appointment)
    });
  }

  /**
   * Extrai dados do paciente
   * Fonte: Aba "Dados do Paciente"
   */
  private getPatientData(appointment: Appointment) {
    if (!appointment?.patientId) {
      return of(null);
    }

    return this.usersService.getUserById(appointment.patientId).pipe(
      map(user => {
        const profile = user.patientProfile || {};
        return {
          name: user.name,
          birthDate: profile.birthDate,
          gender: profile.gender,
          email: user.email,
          phone: user.phone,
          cpf: user.cpf
        };
      }),
      catchError(() => of(null))
    );
  }

  /**
   * Extrai dados da pré-consulta
   * Fonte: Aba "Dados da Pré Consulta"
   */
  private getPreConsultationData(appointment: Appointment) {
    if (!appointment?.preConsultationJson) {
      return of(null);
    }

    try {
      const data = JSON.parse(appointment.preConsultationJson);
      return of(data);
    } catch (e) {
      console.warn('Erro ao parsear preConsultationJson:', e);
      return of(null);
    }
  }

  /**
   * Extrai dados da anamnese
   * Fonte: Aba "Anamnese"
   */
  private getAnamnesisData(appointment: Appointment) {
    if (!appointment?.anamnesisJson) {
      return of(null);
    }

    try {
      const data = JSON.parse(appointment.anamnesisJson);
      return of(data);
    } catch (e) {
      console.warn('Erro ao parsear anamnesisJson:', e);
      return of(null);
    }
  }

  /**
   * Extrai dados biométricos
   * Fonte: Aba "Biométricos"
   */
  private getBiometricsData(appointmentId: string) {
    if (!appointmentId) {
      return of(null);
    }

    return this.biometricsService.getBiometrics(appointmentId).pipe(
      catchError(() => of(null))
    );
  }

  /**
   * Extrai dados SOAP
   * Fonte: Aba "SOAP"
   */
  private getSoapData(appointment: Appointment) {
    if (!appointment?.soapJson) {
      return of(null);
    }

    try {
      const data = JSON.parse(appointment.soapJson);
      return of(data);
    } catch (e) {
      console.warn('Erro ao parsear soapJson:', e);
      return of(null);
    }
  }

  /**
   * Extrai campos da especialidade
   * Fonte: Aba "Campos da Especialidade"
   */
  private getSpecialtyFieldsData(appointment: Appointment) {
    if (!appointment?.specialtyFieldsJson) {
      return of({
        specialtyName: appointment?.specialtyName,
        customFields: {}
      });
    }

    try {
      const customFields = JSON.parse(appointment.specialtyFieldsJson);
      return of({
        specialtyName: appointment?.specialtyName,
        customFields
      });
    } catch (e) {
      console.warn('Erro ao parsear specialtyFieldsJson:', e);
      return of({
        specialtyName: appointment?.specialtyName,
        customFields: {}
      });
    }
  }
}
