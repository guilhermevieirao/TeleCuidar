import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { Router, NavigationEnd } from '@angular/router';
import { TitleService } from './title.service';
import { Subject } from 'rxjs';

describe('TitleService', () => {
  let service: TitleService;
  let titleServiceMock: { setTitle: jest.Mock };
  let routerEventsSubject: Subject<any>;

  beforeEach(() => {
    titleServiceMock = { setTitle: jest.fn() };
    routerEventsSubject = new Subject();

    const routerMock = {
      events: routerEventsSubject.asObservable(),
      url: '/'
    };

    TestBed.configureTestingModule({
      providers: [
        TitleService,
        { provide: Title, useValue: titleServiceMock },
        { provide: Router, useValue: routerMock }
      ]
    });

    service = TestBed.inject(TitleService);
  });

  describe('creation', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });
  });

  describe('setTitle', () => {
    it('should set page title with app name prefix', () => {
      service.setTitle('Test Page');

      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Test Page');
    });

    it('should set different page titles', () => {
      service.setTitle('Login');
      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Login');

      service.setTitle('Painel');
      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Painel');
    });
  });

  describe('title mapping', () => {
    it('should map known routes to titles', () => {
      const router = TestBed.inject(Router) as any;

      // Test login route
      router.url = '/entrar';
      routerEventsSubject.next(new NavigationEnd(1, '/entrar', '/entrar'));
      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Login');
    });

    it('should map painel route', () => {
      const router = TestBed.inject(Router) as any;
      router.url = '/painel';
      routerEventsSubject.next(new NavigationEnd(1, '/painel', '/painel'));
      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Painel');
    });

    it('should map consultas route', () => {
      const router = TestBed.inject(Router) as any;
      router.url = '/consultas';
      routerEventsSubject.next(new NavigationEnd(1, '/consultas', '/consultas'));
      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Consultas');
    });

    it('should handle teleconsulta dynamic routes', () => {
      const router = TestBed.inject(Router) as any;
      router.url = '/teleconsulta/abc-123';
      routerEventsSubject.next(new NavigationEnd(1, '/teleconsulta/abc-123', '/teleconsulta/abc-123'));
      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Teleconsulta');
    });

    it('should handle pre-consulta dynamic routes', () => {
      const router = TestBed.inject(Router) as any;
      router.url = '/pre-consulta/patient-123';
      routerEventsSubject.next(new NavigationEnd(1, '/pre-consulta/patient-123', '/pre-consulta/patient-123'));
      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Pré-Consulta');
    });

    it('should use fallback for unknown routes', () => {
      const router = TestBed.inject(Router) as any;
      router.url = '/unknown-route';
      routerEventsSubject.next(new NavigationEnd(1, '/unknown-route', '/unknown-route'));
      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Página');
    });

    it('should strip query params from URL', () => {
      const router = TestBed.inject(Router) as any;
      router.url = '/entrar?redirect=/painel';
      routerEventsSubject.next(new NavigationEnd(1, '/entrar?redirect=/painel', '/entrar'));
      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Login');
    });

    it('should strip hash fragments from URL', () => {
      const router = TestBed.inject(Router) as any;
      router.url = '/painel#section';
      routerEventsSubject.next(new NavigationEnd(1, '/painel#section', '/painel'));
      expect(titleServiceMock.setTitle).toHaveBeenCalledWith('TeleCuidar - Painel');
    });
  });
});
