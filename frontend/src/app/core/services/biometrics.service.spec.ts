import { TestBed, fakeAsync, tick, discardPeriodicTasks } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { BiometricsService, BiometricsData } from './biometrics.service';
import { environment } from '../../../environments/environment';

describe('BiometricsService', () => {
  let service: BiometricsService;
  let httpMock: HttpTestingController;
  const apiUrl = environment.apiUrl;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        BiometricsService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    service = TestBed.inject(BiometricsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    service.ngOnDestroy();
  });

  describe('creation', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });
  });

  describe('getBiometrics', () => {
    it('should fetch biometrics and start polling', fakeAsync(() => {
      const appointmentId = '1';
      const mockData: BiometricsData = {
        heartRate: 75,
        bloodPressureSystolic: 120,
        bloodPressureDiastolic: 80,
        oxygenSaturation: 98,
        temperature: 36.5,
        lastUpdated: new Date().toISOString()
      };

      let result: BiometricsData | null = null;
      service.getBiometrics(appointmentId).subscribe(data => result = data);
      tick();

      const req = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      expect(req.request.method).toBe('GET');
      req.flush(mockData);
      tick();

      expect(result?.heartRate).toBe(75);
      expect(result?.bloodPressureSystolic).toBe(120);

      // Stop polling
      service.stopPolling(appointmentId);
      discardPeriodicTasks();
    }));

    it('should return null initially', fakeAsync(() => {
      const appointmentId = '2';

      let result: BiometricsData | null = undefined as any;
      service.getBiometrics(appointmentId).subscribe(data => result = data);
      
      // Before request completes, should be null
      expect(result).toBeNull();

      // Complete the request
      const req = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      req.flush({});
      tick();

      service.stopPolling(appointmentId);
      discardPeriodicTasks();
    }));

    it('should handle 404 error gracefully', fakeAsync(() => {
      const appointmentId = '3';

      let result: BiometricsData | null = null;
      service.getBiometrics(appointmentId).subscribe(data => result = data);
      tick();

      const req = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      req.error(new ErrorEvent('Not Found'), { status: 404 });
      tick();

      expect(result).toBeNull();

      service.stopPolling(appointmentId);
      discardPeriodicTasks();
    }));
  });

  describe('saveBiometrics', () => {
    it('should save biometrics via PUT', fakeAsync(() => {
      const appointmentId = '1';
      const data: BiometricsData = {
        heartRate: 80,
        bloodPressureSystolic: 125,
        bloodPressureDiastolic: 85
      };
      const mockResponse = {
        message: 'Saved',
        data: { ...data, lastUpdated: new Date().toISOString() }
      };

      service.saveBiometrics(appointmentId, data);
      tick();

      const req = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(data);
      req.flush(mockResponse);
      tick();
    }));

    it('should update local subject on save', fakeAsync(() => {
      const appointmentId = '1';
      const data: BiometricsData = {
        heartRate: 90,
        oxygenSaturation: 97
      };
      const lastUpdated = new Date().toISOString();
      const mockResponse = {
        message: 'Saved',
        data: { ...data, lastUpdated }
      };

      // First get biometrics to create subject
      let result: BiometricsData | null = null;
      service.getBiometrics(appointmentId).subscribe(d => result = d);
      tick();

      // Handle initial fetch
      const getReq = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      getReq.flush({});
      tick();

      // Save new data
      service.saveBiometrics(appointmentId, data);
      tick();

      const putReq = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      putReq.flush(mockResponse);
      tick();

      expect(result?.heartRate).toBe(90);

      service.stopPolling(appointmentId);
      discardPeriodicTasks();
    }));

    it('should handle save error gracefully', fakeAsync(() => {
      const appointmentId = '1';
      const data: BiometricsData = { heartRate: 70 };

      const consoleSpy = jest.spyOn(console, 'error').mockImplementation();

      service.saveBiometrics(appointmentId, data);
      tick();

      const req = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      req.error(new ErrorEvent('Server Error'), { status: 500 });
      tick();

      expect(consoleSpy).toHaveBeenCalled();
      consoleSpy.mockRestore();
    }));
  });

  describe('polling', () => {
    it('should poll at regular intervals', fakeAsync(() => {
      const appointmentId = '1';
      const mockData: BiometricsData = { heartRate: 75 };

      service.getBiometrics(appointmentId).subscribe();
      tick();

      // Initial fetch
      const req1 = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      req1.flush(mockData);
      tick();

      // Wait for polling interval (2000ms)
      tick(2000);

      // Polling fetch
      const req2 = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      req2.flush(mockData);
      tick();

      service.stopPolling(appointmentId);
      discardPeriodicTasks();
    }));

    it('should stop polling when requested', fakeAsync(() => {
      const appointmentId = '1';
      const mockData: BiometricsData = { heartRate: 75 };

      service.getBiometrics(appointmentId).subscribe();
      tick();

      const req = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      req.flush(mockData);
      tick();

      service.stopPolling(appointmentId);

      tick(5000);

      // No more requests should be made
      httpMock.expectNone(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      discardPeriodicTasks();
    }));
  });

  describe('stopPolling', () => {
    it('should stop polling for specific appointment', fakeAsync(() => {
      const appointmentId = '1';

      service.getBiometrics(appointmentId).subscribe();
      tick();

      const req = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      req.flush({});
      tick();

      service.stopPolling(appointmentId);

      // Should not throw
      expect(() => service.stopPolling(appointmentId)).not.toThrow();
      discardPeriodicTasks();
    }));
  });

  describe('ngOnDestroy', () => {
    it('should clean up all subscriptions', fakeAsync(() => {
      const appointmentId = '1';

      service.getBiometrics(appointmentId).subscribe();
      tick();

      const req = httpMock.expectOne(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      req.flush({});
      tick();

      service.ngOnDestroy();

      tick(5000);
      httpMock.expectNone(`${apiUrl}/appointments/${appointmentId}/biometrics`);
      discardPeriodicTasks();
    }));
  });
});
