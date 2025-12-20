import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { AvatarService } from './avatar.service';
import { environment } from '@env/environment';

describe('AvatarService', () => {
  let service: AvatarService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/users`;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AvatarService]
    });

    service = TestBed.inject(AvatarService);
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

  describe('uploadAvatar', () => {
    it('should upload avatar file for user', fakeAsync(() => {
      const file = new File(['content'], 'avatar.jpg', { type: 'image/jpeg' });
      const mockUser = {
        id: 'user-123',
        name: 'Test',
        lastName: 'User',
        email: 'test@test.com',
        avatar: '/uploads/avatars/user-123.jpg'
      };

      let result: any;
      service.uploadAvatar('user-123', file).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/user-123/avatar`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body instanceof FormData).toBe(true);
      req.flush(mockUser);
      tick();

      expect(result.avatar).toBe('/uploads/avatars/user-123.jpg');
    }));
  });

  describe('getAvatarUrl', () => {
    it('should return empty string for empty path', () => {
      expect(service.getAvatarUrl('')).toBe('');
    });

    it('should return http url as-is', () => {
      const url = 'https://example.com/avatar.jpg';
      expect(service.getAvatarUrl(url)).toBe(url);
    });

    it('should construct full URL for relative path', () => {
      const path = '/uploads/avatars/user-123.jpg';
      const result = service.getAvatarUrl(path);
      
      // Should include the base URL without /api
      expect(result).toContain('/uploads/avatars/user-123.jpg');
      expect(result).not.toContain('/api/uploads');
    });
  });
});
