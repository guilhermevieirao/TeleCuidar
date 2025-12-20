import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import {
  StatsService,
  PlatformStats,
  DashboardStatsDto
} from './stats.service';
import { environment } from '@env/environment';

describe('StatsService', () => {
  let service: StatsService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/reports`;

  const mockDashboardStats: DashboardStatsDto = {
    users: {
      totalUsers: 100,
      activeUsers: 85,
      patients: 60,
      professionals: 20,
      admins: 5
    },
    appointments: {
      total: 500,
      scheduled: 50,
      confirmed: 30,
      inProgress: 10,
      completed: 400,
      cancelled: 50
    }
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [StatsService]
    });

    service = TestBed.inject(StatsService);
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

  describe('getPlatformStats', () => {
    it('should fetch and transform dashboard stats', fakeAsync(() => {
      let result: PlatformStats | undefined;
      service.getPlatformStats().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/dashboard`);
      expect(req.request.method).toBe('GET');
      req.flush(mockDashboardStats);
      tick();

      expect(result?.totalUsers).toBe(100);
      expect(result?.activeProfessionals).toBe(20);
      expect(result?.activePatients).toBe(60);
      expect(result?.appointmentsScheduled).toBe(50);
    }));

    it('should set default values for missing data', fakeAsync(() => {
      let result: PlatformStats | undefined;
      service.getPlatformStats().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/dashboard`);
      req.flush(mockDashboardStats);
      tick();

      expect(result?.occupancyRate).toBe(0);
      expect(result?.averageRating).toBe(4.5);
      expect(result?.averageConsultationTime).toBe(35);
    }));
  });
});
