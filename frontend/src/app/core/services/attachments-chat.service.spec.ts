import { TestBed, fakeAsync, tick, discardPeriodicTasks } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { AttachmentsChatService, AttachmentMessage } from './attachments-chat.service';
import { environment } from '../../../environments/environment';

describe('AttachmentsChatService', () => {
  let service: AttachmentsChatService;
  let httpMock: HttpTestingController;
  const baseUrl = `${environment.apiUrl}/appointments`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AttachmentsChatService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    service = TestBed.inject(AttachmentsChatService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    service.ngOnDestroy();
    httpMock.verify();
  });

  describe('creation', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });
  });

  describe('getMessages', () => {
    it('should return messages observable for appointment', fakeAsync(() => {
      const appointmentId = '1';
      const mockMessages: AttachmentMessage[] = [
        createMockMessage('1', 'PATIENT', 'test.pdf')
      ];

      let messages: AttachmentMessage[] = [];
      service.getMessages(appointmentId).subscribe(m => messages = m);
      tick();

      // Handle the initial fetch request
      const req = httpMock.expectOne(`${baseUrl}/${appointmentId}/attachments-chat`);
      req.flush(mockMessages);
      tick();

      expect(messages.length).toBe(1);

      service.stopPolling(appointmentId);
      discardPeriodicTasks();
    }));
  });

  describe('addMessage', () => {
    it('should add message via API', fakeAsync(() => {
      const appointmentId = '1';
      const message = createMockMessage('2', 'PROFESSIONAL', 'report.pdf');
      const mockResponse = { message: 'Success', data: message };

      let result: AttachmentMessage | undefined;
      service.addMessage(appointmentId, message).subscribe(r => result = r);
      tick();

      const req = httpMock.expectOne(`${baseUrl}/${appointmentId}/attachments-chat`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
      tick();

      expect(result?.fileName).toBe('report.pdf');
    }));

    it('should handle add error', fakeAsync(() => {
      const appointmentId = '1';
      const message = createMockMessage('3', 'PATIENT', 'doc.pdf');

      let error: any;
      service.addMessage(appointmentId, message).subscribe({
        error: e => error = e
      });
      tick();

      const req = httpMock.expectOne(`${baseUrl}/${appointmentId}/attachments-chat`);
      req.error(new ErrorEvent('Server error'), { status: 500 });
      tick();

      expect(error).toBeTruthy();
    }));
  });

  describe('polling', () => {
    it('should start polling and fetch messages periodically', fakeAsync(() => {
      const appointmentId = '1';
      const mockMessages: AttachmentMessage[] = [];

      service.startPolling(appointmentId);
      tick(2000);

      const req = httpMock.expectOne(`${baseUrl}/${appointmentId}/attachments-chat`);
      req.flush(mockMessages);
      tick();

      service.stopPolling(appointmentId);
      discardPeriodicTasks();
    }));

    it('should stop polling when requested', fakeAsync(() => {
      const appointmentId = '1';

      service.startPolling(appointmentId);
      service.stopPolling(appointmentId);

      tick(5000);
      
      // No requests should be made after stopping
      discardPeriodicTasks();
    }));

    it('should not duplicate polling for same appointment', fakeAsync(() => {
      const appointmentId = '1';

      service.startPolling(appointmentId);
      service.startPolling(appointmentId);
      tick(2000);

      const reqs = httpMock.match(`${baseUrl}/${appointmentId}/attachments-chat`);
      expect(reqs.length).toBe(1);
      reqs.forEach(r => r.flush([]));
      tick();

      service.stopPolling(appointmentId);
      discardPeriodicTasks();
    }));
  });

  describe('ngOnDestroy', () => {
    it('should clean up all subscriptions', fakeAsync(() => {
      const appointmentId = '1';

      service.startPolling(appointmentId);
      tick(100);

      service.ngOnDestroy();

      tick(5000);
      discardPeriodicTasks();
    }));
  });

  function createMockMessage(id: string, role: 'PATIENT' | 'PROFESSIONAL', fileName: string): AttachmentMessage {
    return {
      id,
      senderRole: role,
      senderName: 'Test User',
      timestamp: new Date().toISOString(),
      title: 'Test Document',
      fileName,
      fileType: 'application/pdf',
      fileSize: 1024,
      fileUrl: 'data:application/pdf;base64,test'
    };
  }
});
