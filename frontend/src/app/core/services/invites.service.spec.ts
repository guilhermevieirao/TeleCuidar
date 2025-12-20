import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import {
  InvitesService,
  Invite,
  CreateInviteDto,
  PaginatedResponse,
  InvitesFilter
} from './invites.service';
import { environment } from '@env/environment';

describe('InvitesService', () => {
  let service: InvitesService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/invites`;

  const mockInvite: Invite = {
    id: '1',
    email: 'teste@example.com',
    role: 'PROFESSIONAL',
    status: 'Pending',
    createdAt: '2024-01-01T00:00:00Z',
    expiresAt: '2024-01-08T00:00:00Z',
    createdByUserId: 'admin-1',
    createdByUserName: 'Admin User',
    token: 'abc123token'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [InvitesService]
    });

    service = TestBed.inject(InvitesService);
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

  describe('getInvites', () => {
    it('should fetch invites with pagination', fakeAsync(() => {
      const mockResponse: PaginatedResponse<Invite> = {
        data: [mockInvite],
        total: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1
      };

      let result: PaginatedResponse<Invite> | undefined;
      service.getInvites().subscribe(res => result = res);

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');
      req.flush(mockResponse);
      tick();

      expect(result?.data.length).toBe(1);
    }));

    it('should apply search filter', fakeAsync(() => {
      const filter: InvitesFilter = { search: 'teste@example' };

      service.getInvites(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('search')).toBe('teste@example');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));

    it('should filter by role', fakeAsync(() => {
      const filter: InvitesFilter = { role: 'PROFESSIONAL' };

      service.getInvites(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('role')).toBe('PROFESSIONAL');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));

    it('should filter by status', fakeAsync(() => {
      const filter: InvitesFilter = { status: 'Pending' };

      service.getInvites(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('status')).toBe('Pending');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));

    it('should apply sorting', fakeAsync(() => {
      service.getInvites(undefined, { field: 'createdAt', direction: 'desc' }).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('sortBy')).toBe('createdAt');
      expect(req.request.params.get('sortDirection')).toBe('desc');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));
  });

  describe('getInviteById', () => {
    it('should fetch an invite by id', fakeAsync(() => {
      let result: Invite | undefined;
      service.getInviteById('1').subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockInvite);
      tick();

      expect(result?.email).toBe('teste@example.com');
    }));
  });

  describe('getInviteByToken', () => {
    it('should fetch an invite by token', fakeAsync(() => {
      let result: Invite | undefined;
      service.getInviteByToken('abc123token').subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/token/abc123token`);
      expect(req.request.method).toBe('GET');
      req.flush(mockInvite);
      tick();

      expect(result?.token).toBe('abc123token');
    }));
  });

  describe('createInvite', () => {
    it('should create a new invite', fakeAsync(() => {
      const newInvite: CreateInviteDto = {
        email: 'novo@example.com',
        role: 'PROFESSIONAL'
      };

      let result: Invite | undefined;
      service.createInvite(newInvite).subscribe(res => result = res);

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.email).toBe('novo@example.com');
      req.flush({ ...mockInvite, email: 'novo@example.com' });
      tick();

      expect(result?.email).toBe('novo@example.com');
    }));

    it('should create invite with expiration date', fakeAsync(() => {
      const newInvite: CreateInviteDto = {
        email: 'novo@example.com',
        role: 'ADMIN',
        expiresAt: '2024-12-31T23:59:59Z'
      };

      service.createInvite(newInvite).subscribe();

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.body.expiresAt).toBe('2024-12-31T23:59:59Z');
      req.flush(mockInvite);
      tick();
    }));
  });

  describe('resendInvite', () => {
    it('should resend an invite', fakeAsync(() => {
      let result: Invite | undefined;
      service.resendInvite('1').subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/1/resend`);
      expect(req.request.method).toBe('POST');
      req.flush(mockInvite);
      tick();

      expect(result).toBeTruthy();
    }));
  });

  describe('cancelInvite', () => {
    it('should cancel an invite', fakeAsync(() => {
      let completed = false;
      service.cancelInvite('1').subscribe(() => completed = true);

      const req = httpMock.expectOne(`${apiUrl}/1/cancel`);
      expect(req.request.method).toBe('PATCH');
      req.flush(null);
      tick();

      expect(completed).toBe(true);
    }));
  });

  describe('deleteInvite', () => {
    it('should delete an invite', fakeAsync(() => {
      let completed = false;
      service.deleteInvite('1').subscribe(() => completed = true);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
      tick();

      expect(completed).toBe(true);
    }));
  });

  describe('acceptInvite', () => {
    it('should accept an invite by token', fakeAsync(() => {
      let completed = false;
      service.acceptInvite('abc123token').subscribe(() => completed = true);

      const req = httpMock.expectOne(`${apiUrl}/accept`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.token).toBe('abc123token');
      req.flush(null);
      tick();

      expect(completed).toBe(true);
    }));
  });
});
