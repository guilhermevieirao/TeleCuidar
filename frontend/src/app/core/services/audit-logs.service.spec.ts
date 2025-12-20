import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import {
  AuditLogsService,
  AuditLog,
  AuditLogsFilter,
  PaginatedResponse
} from './audit-logs.service';
import { environment } from '@env/environment';

describe('AuditLogsService', () => {
  let service: AuditLogsService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/auditlogs`;

  const mockAuditLog: AuditLog = {
    id: '1',
    userId: 'user-123',
    userName: 'Admin User',
    userRole: 'ADMIN',
    action: 'create',
    entityType: 'User',
    entityId: 'user-456',
    oldValues: null,
    newValues: '{"name": "Novo Usuario"}',
    ipAddress: '192.168.1.1',
    userAgent: 'Mozilla/5.0',
    createdAt: '2024-01-01T10:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AuditLogsService]
    });

    service = TestBed.inject(AuditLogsService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('creation', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });
  });

  describe('getAuditLogs', () => {
    it('should fetch audit logs with pagination', fakeAsync(() => {
      const mockResponse: PaginatedResponse<AuditLog> = {
        data: [mockAuditLog],
        total: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1
      };

      let result: PaginatedResponse<AuditLog> | undefined;
      service.getAuditLogs().subscribe(res => result = res);

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
      tick();

      expect(result?.data.length).toBe(1);
    }));

    it('should filter by entityType', fakeAsync(() => {
      const filter: AuditLogsFilter = { entityType: 'User' };

      service.getAuditLogs(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('entityType')).toBe('User');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));

    it('should not set entityType when "all"', fakeAsync(() => {
      const filter: AuditLogsFilter = { entityType: 'all' };

      service.getAuditLogs(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.has('entityType')).toBe(false);
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));

    it('should filter by userId', fakeAsync(() => {
      const filter: AuditLogsFilter = { userId: 'user-123' };

      service.getAuditLogs(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('userId')).toBe('user-123');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));

    it('should filter by date range', fakeAsync(() => {
      const filter: AuditLogsFilter = {
        startDate: '2024-01-01',
        endDate: '2024-12-31'
      };

      service.getAuditLogs(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('startDate')).toBe('2024-01-01');
      expect(req.request.params.get('endDate')).toBe('2024-12-31');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));
  });

  describe('getAuditLogById', () => {
    it('should fetch a single audit log by id', fakeAsync(() => {
      let result: AuditLog | undefined;
      service.getAuditLogById('1').subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockAuditLog);
      tick();

      expect(result?.action).toBe('create');
    }));
  });

  describe('exportToPDF', () => {
    it('should export audit logs as PDF', fakeAsync(() => {
      const blob = new Blob(['pdf content'], { type: 'application/pdf' });

      let result: Blob | undefined;
      service.exportToPDF().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/export/pdf`);
      expect(req.request.method).toBe('GET');
      expect(req.request.responseType).toBe('blob');
      req.flush(blob);
      tick();

      expect(result).toBeTruthy();
    }));

    it('should export with filters', fakeAsync(() => {
      const filter: AuditLogsFilter = {
        entityType: 'User',
        startDate: '2024-01-01',
        endDate: '2024-12-31'
      };

      service.exportToPDF(filter).subscribe();

      const req = httpMock.expectOne(request => request.url.includes('/export/pdf'));
      expect(req.request.params.get('entityType')).toBe('User');
      expect(req.request.params.get('startDate')).toBe('2024-01-01');
      req.flush(new Blob());
      tick();
    }));
  });
});
