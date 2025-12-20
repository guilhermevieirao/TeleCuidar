import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { PLATFORM_ID } from '@angular/core';
import { AuthService } from './auth.service';
import { AUTH_ENDPOINTS, STORAGE_KEYS } from '@app/core/constants/auth.constants';
import { LoginRequest, LoginResponse, RegisterRequest, User, userrole } from '@app/core/models/auth.model';

describe('AuthService', () => {
  let service: AuthService;
  let httpMock: HttpTestingController;
  let localStorageMock: { [key: string]: string };
  let sessionStorageMock: { [key: string]: string };

  const mockUser: User = {
    id: '1',
    name: 'Test',
    lastName: 'User',
    email: 'test@example.com',
    cpf: '12345678901',
    role: 'PATIENT' as userrole,
    status: 'Active',
    emailVerified: true,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  };

  const mockLoginResponse: LoginResponse = {
    user: mockUser,
    accessToken: 'access-token-123',
    refreshToken: 'refresh-token-456'
  };

  beforeEach(() => {
    localStorageMock = {};
    sessionStorageMock = {};

    // Mock localStorage
    const localStorageSpy = {
      getItem: jest.fn((key: string) => localStorageMock[key] || null),
      setItem: jest.fn((key: string, value: string) => { localStorageMock[key] = value; }),
      removeItem: jest.fn((key: string) => { delete localStorageMock[key]; }),
      clear: jest.fn(() => { localStorageMock = {}; })
    };
    Object.defineProperty(window, 'localStorage', { value: localStorageSpy, writable: true });

    // Mock sessionStorage
    const sessionStorageSpy = {
      getItem: jest.fn((key: string) => sessionStorageMock[key] || null),
      setItem: jest.fn((key: string, value: string) => { sessionStorageMock[key] = value; }),
      removeItem: jest.fn((key: string) => { delete sessionStorageMock[key]; }),
      clear: jest.fn(() => { sessionStorageMock = {}; })
    };
    Object.defineProperty(window, 'sessionStorage', { value: sessionStorageSpy, writable: true });

    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [
        AuthService,
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    service = TestBed.inject(AuthService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  describe('creation', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });

    it('should have initial auth state as unauthenticated', () => {
      expect(service.isAuthenticated()).toBe(false);
      expect(service.currentUser()).toBeNull();
    });
  });

  describe('login', () => {
    it('should login successfully and update auth state', fakeAsync(() => {
      const loginRequest: LoginRequest = {
        email: 'test@example.com',
        password: 'Password123!',
        rememberMe: true
      };

      let response: LoginResponse | undefined;
      service.login(loginRequest).subscribe(res => {
        response = res;
      });

      const req = httpMock.expectOne(AUTH_ENDPOINTS.LOGIN);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(loginRequest);
      req.flush(mockLoginResponse);

      tick();

      expect(response).toEqual(mockLoginResponse);
      expect(service.isAuthenticated()).toBe(true);
      expect(service.currentUser()).toEqual(mockUser);
    }));

    it('should store tokens in localStorage when rememberMe is true', fakeAsync(() => {
      const loginRequest: LoginRequest = {
        email: 'test@example.com',
        password: 'Password123!',
        rememberMe: true
      };

      service.login(loginRequest).subscribe();

      const req = httpMock.expectOne(AUTH_ENDPOINTS.LOGIN);
      req.flush(mockLoginResponse);

      tick();

      expect(localStorage.setItem).toHaveBeenCalledWith(STORAGE_KEYS.ACCESS_TOKEN, mockLoginResponse.accessToken);
      expect(localStorage.setItem).toHaveBeenCalledWith(STORAGE_KEYS.REFRESH_TOKEN, mockLoginResponse.refreshToken);
      expect(localStorage.setItem).toHaveBeenCalledWith(STORAGE_KEYS.USER, JSON.stringify(mockUser));
      expect(localStorage.setItem).toHaveBeenCalledWith(STORAGE_KEYS.REMEMBER_ME, 'true');
    }));

    it('should store tokens in sessionStorage when rememberMe is false', fakeAsync(() => {
      const loginRequest: LoginRequest = {
        email: 'test@example.com',
        password: 'Password123!',
        rememberMe: false
      };

      service.login(loginRequest).subscribe();

      const req = httpMock.expectOne(AUTH_ENDPOINTS.LOGIN);
      req.flush(mockLoginResponse);

      tick();

      expect(sessionStorage.setItem).toHaveBeenCalledWith(STORAGE_KEYS.ACCESS_TOKEN, mockLoginResponse.accessToken);
      expect(sessionStorage.setItem).toHaveBeenCalledWith(STORAGE_KEYS.REFRESH_TOKEN, mockLoginResponse.refreshToken);
      expect(sessionStorage.setItem).toHaveBeenCalledWith(STORAGE_KEYS.USER, JSON.stringify(mockUser));
    }));

    it('should handle login error', fakeAsync(() => {
      const loginRequest: LoginRequest = {
        email: 'test@example.com',
        password: 'wrong-password'
      };

      let error: any;
      service.login(loginRequest).subscribe({
        error: (err) => { error = err; }
      });

      const req = httpMock.expectOne(AUTH_ENDPOINTS.LOGIN);
      req.flush({ message: 'Invalid credentials' }, { status: 401, statusText: 'Unauthorized' });

      tick();

      expect(error).toBeDefined();
      expect(service.isAuthenticated()).toBe(false);
    }));
  });

  describe('register', () => {
    it('should register successfully', fakeAsync(() => {
      const registerRequest: RegisterRequest = {
        name: 'New',
        lastName: 'User',
        email: 'new@example.com',
        password: 'Password123!',
        confirmPassword: 'Password123!',
        cpf: '12345678901',
        phone: '11999999999',
        acceptTerms: true
      };

      const registerResponse = {
        user: mockUser,
        message: 'Conta criada com sucesso!'
      };

      let response: any;
      service.register(registerRequest).subscribe(res => {
        response = res;
      });

      const req = httpMock.expectOne(AUTH_ENDPOINTS.REGISTER);
      expect(req.request.method).toBe('POST');
      req.flush(registerResponse);

      tick();

      expect(response).toEqual(registerResponse);
    }));

    it('should handle registration error', fakeAsync(() => {
      const registerRequest: RegisterRequest = {
        name: 'New',
        lastName: 'User',
        email: 'existing@example.com',
        password: 'Password123!',
        confirmPassword: 'Password123!',
        cpf: '12345678901',
        phone: '11999999999',
        acceptTerms: true
      };

      let error: any;
      service.register(registerRequest).subscribe({
        error: (err) => { error = err; }
      });

      const req = httpMock.expectOne(AUTH_ENDPOINTS.REGISTER);
      req.flush({ message: 'Email já está em uso' }, { status: 400, statusText: 'Bad Request' });

      tick();

      expect(error).toBeDefined();
    }));
  });

  describe('logout', () => {
    it('should clear auth state on logout', fakeAsync(() => {
      // First login
      const loginRequest: LoginRequest = {
        email: 'test@example.com',
        password: 'Password123!',
        rememberMe: true
      };

      service.login(loginRequest).subscribe();

      const req = httpMock.expectOne(AUTH_ENDPOINTS.LOGIN);
      req.flush(mockLoginResponse);
      tick();

      expect(service.isAuthenticated()).toBe(true);

      // Then logout
      service.logout();

      expect(service.isAuthenticated()).toBe(false);
      expect(service.currentUser()).toBeNull();
      expect(localStorage.removeItem).toHaveBeenCalled();
    }));
  });

  describe('forgotPassword', () => {
    it('should send forgot password request', fakeAsync(() => {
      const email = 'test@example.com';
      const response = { message: 'Email enviado com sucesso!' };

      let result: any;
      service.forgotPassword({ email }).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(AUTH_ENDPOINTS.FORGOT_PASSWORD);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email });
      req.flush(response);

      tick();

      expect(result).toEqual(response);
    }));
  });

  describe('resetPassword', () => {
    it('should reset password successfully', fakeAsync(() => {
      const request = {
        token: 'reset-token',
        password: 'NewPassword123!',
        confirmPassword: 'NewPassword123!'
      };
      const response = { message: 'Senha alterada com sucesso!' };

      let result: any;
      service.resetPassword(request).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(AUTH_ENDPOINTS.RESET_PASSWORD);
      expect(req.request.method).toBe('POST');
      req.flush(response);

      tick();

      expect(result).toEqual(response);
    }));
  });

  describe('verifyEmail', () => {
    it('should verify email successfully', fakeAsync(() => {
      const request = { token: 'verify-token' };
      const response = { 
        user: mockUser, 
        message: 'Email verificado com sucesso!' 
      };

      let result: any;
      service.verifyEmail(request).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(AUTH_ENDPOINTS.VERIFY_EMAIL);
      expect(req.request.method).toBe('POST');
      req.flush(response);

      tick();

      expect(result).toEqual(response);
      expect(service.currentUser()).toEqual(mockUser);
    }));
  });

  describe('refreshToken', () => {
    it('should refresh token successfully', fakeAsync(() => {
      localStorageMock[STORAGE_KEYS.REFRESH_TOKEN] = 'old-refresh-token';

      service.refreshToken().subscribe();

      const req = httpMock.expectOne(AUTH_ENDPOINTS.REFRESH_TOKEN);
      expect(req.request.method).toBe('POST');
      req.flush(mockLoginResponse);

      tick();

      expect(service.isAuthenticated()).toBe(true);
    }));
  });

  describe('getAccessToken', () => {
    it('should return access token from localStorage', () => {
      localStorageMock[STORAGE_KEYS.ACCESS_TOKEN] = 'test-access-token';

      const token = service.getAccessToken();

      expect(token).toBe('test-access-token');
    });

    it('should return null when no token exists', () => {
      const token = service.getAccessToken();

      expect(token).toBeNull();
    });
  });

  describe('resendVerificationEmail', () => {
    it('should resend verification email', fakeAsync(() => {
      const email = 'test@example.com';

      service.resendVerificationEmail(email).subscribe();

      const req = httpMock.expectOne(AUTH_ENDPOINTS.RESEND_VERIFICATION);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ email });
      req.flush({ message: 'Email reenviado!' });

      tick();
    }));
  });
});
