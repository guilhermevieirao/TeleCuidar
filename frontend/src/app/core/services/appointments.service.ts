import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';

const API_BASE_URL = 'http://localhost:5239/api';

export type AppointmentStatus = 'Scheduled' | 'Confirmed' | 'InProgress' | 'Completed' | 'Cancelled';
export type AppointmentType = 'FirstVisit' | 'Return' | 'Routine' | 'Emergency' | 'Common';

export interface PreConsultationForm {
  personalInfo: {
    fullName: string;
    birthDate: string;
    weight: string;
    height: string;
  };
  medicalHistory: {
    chronicConditions?: string;
    medications?: string;
    allergies?: string;
    surgeries?: string;
    generalObservations?: string;
  };
  lifestyleHabits: {
    smoker: 'sim' | 'nao' | 'ex-fumante';
    alcoholConsumption: 'nenhum' | 'social' | 'frequente';
    physicalActivity: 'nenhuma' | 'leve' | 'moderada' | 'intensa';
    generalObservations?: string;
  };
  vitalSigns: {
    bloodPressure?: string;
    heartRate?: string;
    temperature?: string;
    oxygenSaturation?: string;
    generalObservations?: string;
  };
  currentSymptoms: {
    mainSymptoms: string;
    symptomOnset: string;
    painIntensity?: number;
    generalObservations?: string;
  };
  additionalObservations?: string;
  attachments?: {
    title: string;
    fileUrl: string;
    type: string;
  }[];
}

export interface Appointment {
  id: string;
  patientId: string;
  patientName?: string;
  professionalId: string;
  professionalName?: string;
  specialtyId: string;
  specialtyName?: string;
  date: string;
  time: string;
  endTime?: string;
  type?: AppointmentType;
  status: AppointmentStatus;
  observation?: string;
  meetLink?: string;
  createdAt: string;
  updatedAt?: string;
  preConsultation?: PreConsultationForm;
}

export interface CreateAppointmentDto {
  patientId: string;
  professionalId: string;
  specialtyId: string;
  date: string;
  time: string;
  endTime?: string;
  type?: AppointmentType;
  observation?: string;
  meetLink?: string;
}

export interface UpdateAppointmentDto {
  status?: AppointmentStatus;
  observation?: string;
  preConsultation?: PreConsultationForm;
}

export interface AppointmentsFilter {
  status?: string;
  search?: string;
  startDate?: string;
  endDate?: string;
  patientId?: string;
  professionalId?: string;
  specialtyId?: string;
}

export interface PaginatedResponse<T> {
  data: T[];
  total: number;
  page: number;
  pageSize: number;
  totalPages: number;
}

@Injectable({
  providedIn: 'root'
})
export class AppointmentsService {
  private apiUrl = `${API_BASE_URL}/appointments`;

  constructor(private http: HttpClient) {}

  getAppointments(
    filter?: AppointmentsFilter,
    page: number = 1,
    pageSize: number = 10
  ): Observable<PaginatedResponse<Appointment>> {
    let params = new HttpParams()
      .set('page', page.toString())
      .set('pageSize', pageSize.toString());

    if (filter?.status) {
      params = params.set('status', filter.status);
    }
    if (filter?.search) {
      params = params.set('search', filter.search);
    }
    if (filter?.startDate) {
      params = params.set('startDate', filter.startDate);
    }
    if (filter?.endDate) {
      params = params.set('endDate', filter.endDate);
    }
    if (filter?.patientId) {
      params = params.set('patientId', filter.patientId);
    }
    if (filter?.professionalId) {
      params = params.set('professionalId', filter.professionalId);
    }
    if (filter?.specialtyId) {
      params = params.set('specialtyId', filter.specialtyId);
    }

    return this.http.get<PaginatedResponse<Appointment>>(this.apiUrl, { params });
  }

  getAppointmentById(id: string): Observable<Appointment> {
    return this.http.get<Appointment>(`${this.apiUrl}/${id}`);
  }

  createAppointment(appointment: CreateAppointmentDto): Observable<Appointment> {
    return this.http.post<Appointment>(this.apiUrl, appointment);
  }

  updateAppointment(id: string, updates: UpdateAppointmentDto): Observable<Appointment> {
    return this.http.put<Appointment>(`${this.apiUrl}/${id}`, updates);
  }

  cancelAppointment(id: string, reason?: string): Observable<Appointment> {
    return this.http.patch<Appointment>(`${this.apiUrl}/${id}/cancel`, { reason });
  }

  confirmAppointment(id: string): Observable<Appointment> {
    return this.http.patch<Appointment>(`${this.apiUrl}/${id}/confirm`, {});
  }

  startAppointment(id: string): Observable<Appointment> {
    return this.http.patch<Appointment>(`${this.apiUrl}/${id}/start`, {});
  }

  completeAppointment(id: string, observation?: string): Observable<Appointment> {
    return this.http.patch<Appointment>(`${this.apiUrl}/${id}/complete`, { observation });
  }

  deleteAppointment(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/${id}`);
  }
}
