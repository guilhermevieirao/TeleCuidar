import { Injectable, OnDestroy, Inject, PLATFORM_ID } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, Observable, interval, Subscription } from 'rxjs';
import { isPlatformBrowser } from '@angular/common';
import { environment } from '@env/environment';

export interface BiometricsData {
  heartRate?: number; // bpm
  bloodPressureSystolic?: number; // mmHg
  bloodPressureDiastolic?: number; // mmHg
  oxygenSaturation?: number; // %
  temperature?: number; // Celsius
  respiratoryRate?: number; // rpm
  weight?: number; // kg
  height?: number; // cm
  glucose?: number; // mg/dL
  lastUpdated?: string; // ISO date
}

@Injectable({
  providedIn: 'root'
})
export class BiometricsService implements OnDestroy {
  private biometricsSubjects: Map<string, BehaviorSubject<BiometricsData | null>> = new Map();
  private pollingSubscriptions: Map<string, Subscription> = new Map();
  private lastKnownTimestamp: Map<string, string> = new Map();
  
  private readonly POLLING_INTERVAL = 2000; // 2 seconds

  constructor(
    private http: HttpClient,
    @Inject(PLATFORM_ID) private platformId: Object
  ) {}

  ngOnDestroy() {
    // Clean up all polling subscriptions
    this.pollingSubscriptions.forEach(sub => sub.unsubscribe());
    this.pollingSubscriptions.clear();
  }

  private getSubject(appointmentId: string): BehaviorSubject<BiometricsData | null> {
    if (!this.biometricsSubjects.has(appointmentId)) {
      this.biometricsSubjects.set(appointmentId, new BehaviorSubject<BiometricsData | null>(null));
    }
    return this.biometricsSubjects.get(appointmentId)!;
  }

  /**
   * Obtém observable para dados biométricos de uma consulta
   * Inicia polling automaticamente
   */
  getBiometrics(appointmentId: string): Observable<BiometricsData | null> {
    // Fetch initial data
    this.fetchBiometrics(appointmentId);
    
    // Start polling if not already
    this.startPolling(appointmentId);
    
    return this.getSubject(appointmentId).asObservable();
  }

  /**
   * Salva dados biométricos (usado pelo paciente)
   */
  saveBiometrics(appointmentId: string, data: BiometricsData): void {
    const url = `${environment.apiUrl}/appointments/${appointmentId}/biometrics`;
    
    this.http.put<{ message: string; data: BiometricsData }>(url, data).subscribe({
      next: (response) => {
        // Update local subject immediately
        this.getSubject(appointmentId).next(response.data);
        this.lastKnownTimestamp.set(appointmentId, response.data.lastUpdated || '');
      },
      error: (err) => {
        console.error('Error saving biometrics:', err);
      }
    });
  }

  /**
   * Busca dados biométricos do servidor
   */
  private fetchBiometrics(appointmentId: string): void {
    const url = `${environment.apiUrl}/appointments/${appointmentId}/biometrics`;
    
    this.http.get<BiometricsData>(url).subscribe({
      next: (data) => {
        const currentData = this.getSubject(appointmentId).value;
        const currentTimestamp = currentData?.lastUpdated || '';
        const newTimestamp = data?.lastUpdated || '';
        
        // Only update if data changed
        if (newTimestamp !== currentTimestamp) {
          this.getSubject(appointmentId).next(data);
          this.lastKnownTimestamp.set(appointmentId, newTimestamp);
        }
      },
      error: (err) => {
        if (err.status !== 404) {
          console.error('Error fetching biometrics:', err);
        }
      }
    });
  }

  /**
   * Inicia polling para atualizações em tempo real
   */
  startPolling(appointmentId: string): void {
    if (!isPlatformBrowser(this.platformId)) return;
    if (this.pollingSubscriptions.has(appointmentId)) return;

    const subscription = interval(this.POLLING_INTERVAL).subscribe(() => {
      this.fetchBiometrics(appointmentId);
    });
    
    this.pollingSubscriptions.set(appointmentId, subscription);
  }

  /**
   * Para o polling de uma consulta
   */
  stopPolling(appointmentId: string): void {
    const subscription = this.pollingSubscriptions.get(appointmentId);
    if (subscription) {
      subscription.unsubscribe();
      this.pollingSubscriptions.delete(appointmentId);
    }
  }
}
