import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import {
  NotificationsService,
  Notification,
  CreateNotificationDto,
  PaginatedResponse
} from './notifications.service';
import { AuthService } from './auth.service';
import { environment } from '@env/environment';
import { signal } from '@angular/core';

describe('NotificationsService', () => {
  let service: NotificationsService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/notifications`;

  const mockUser = { id: 'user-123', email: 'test@test.com', name: 'Test', role: 'ADMIN' };

  const mockNotification: Notification = {
    id: '1',
    userId: 'user-123',
    title: 'Nova mensagem',
    message: 'Você tem uma nova notificação',
    type: 'Info',
    isRead: false,
    createdAt: '2024-01-01T00:00:00Z'
  };

  beforeEach(() => {
    const authServiceMock = {
      currentUser: signal(mockUser)
    };

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        NotificationsService,
        { provide: AuthService, useValue: authServiceMock }
      ]
    });

    service = TestBed.inject(NotificationsService);
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

  describe('getNotifications', () => {
    it('should fetch notifications for current user', fakeAsync(() => {
      const mockResponse: PaginatedResponse<Notification> = {
        data: [mockNotification],
        total: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1
      };

      let result: PaginatedResponse<Notification> | undefined;
      service.getNotifications().subscribe(res => result = res);

      const req = httpMock.expectOne(request => request.url === `${apiUrl}/user/user-123`);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
      tick();

      expect(result?.data.length).toBe(1);
    }));

    it('should filter by read status', fakeAsync(() => {
      service.getNotifications({ isRead: false }).subscribe();

      const req = httpMock.expectOne(request => request.url.includes('/user/'));
      expect(req.request.params.get('isRead')).toBe('false');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));

    it('should filter by type', fakeAsync(() => {
      service.getNotifications({ type: 'Warning' }).subscribe();

      const req = httpMock.expectOne(request => request.url.includes('/user/'));
      expect(req.request.params.get('type')).toBe('Warning');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));
  });

  describe('getNotificationById', () => {
    it('should fetch a single notification by id', fakeAsync(() => {
      let result: Notification | undefined;
      service.getNotificationById('1').subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockNotification);
      tick();

      expect(result?.title).toBe('Nova mensagem');
    }));
  });

  describe('getUnreadCount', () => {
    it('should fetch unread count for user', fakeAsync(() => {
      let result: { count: number } | undefined;
      service.getUnreadCount().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/user/user-123/unread-count`);
      expect(req.request.method).toBe('GET');
      req.flush({ count: 5 });
      tick();

      expect(result?.count).toBe(5);
    }));
  });

  describe('markAsRead', () => {
    it('should mark a notification as read', fakeAsync(() => {
      let completed = false;
      service.markAsRead('1').subscribe(() => completed = true);

      const req = httpMock.expectOne(`${apiUrl}/1/read`);
      expect(req.request.method).toBe('PATCH');
      req.flush({});
      tick();

      expect(completed).toBe(true);
    }));
  });

  describe('markAllAsRead', () => {
    it('should mark all notifications as read for user', fakeAsync(() => {
      let completed = false;
      service.markAllAsRead().subscribe(() => completed = true);

      const req = httpMock.expectOne(`${apiUrl}/user/user-123/read-all`);
      expect(req.request.method).toBe('PATCH');
      req.flush({});
      tick();

      expect(completed).toBe(true);
    }));
  });

  describe('deleteNotification', () => {
    it('should delete a notification', fakeAsync(() => {
      let completed = false;
      service.deleteNotification('1').subscribe(() => completed = true);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
      tick();

      expect(completed).toBe(true);
    }));
  });

  describe('createNotification', () => {
    it('should create a new notification', fakeAsync(() => {
      const newNotification: CreateNotificationDto = {
        userId: 'user-456',
        title: 'Teste',
        message: 'Mensagem de teste',
        type: 'Success'
      };

      let result: Notification | undefined;
      service.createNotification(newNotification).subscribe(res => result = res);

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.title).toBe('Teste');
      req.flush({ ...mockNotification, ...newNotification });
      tick();

      expect(result?.title).toBe('Teste');
    }));
  });
});
