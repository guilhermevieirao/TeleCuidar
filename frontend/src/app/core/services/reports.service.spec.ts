import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import {
  ReportsService,
  ReportFilter,
  ReportData,
  ReportStatistics,
  UsersByRoleData,
  AppointmentsByStatusData,
  AppointmentsByMonthData,
  SpecialtiesRankingData
} from './reports.service';
import { environment } from '@env/environment';

describe('ReportsService', () => {
  let service: ReportsService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/reports`;

  const mockStatistics: ReportStatistics = {
    totalUsers: 100,
    activeUsers: 85,
    totalAppointments: 500,
    completedAppointments: 400,
    canceledAppointments: 50,
    totalRevenue: 50000,
    averageRating: 4.5,
    totalSpecialties: 10,
    activeSpecialties: 8,
    newUsersThisMonth: 15,
    appointmentsThisMonth: 100,
    revenueThisMonth: 10000
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ReportsService]
    });

    service = TestBed.inject(ReportsService);
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

  describe('getReportData', () => {
    it('should fetch report data with date filter', fakeAsync(() => {
      const filter: ReportFilter = {
        startDate: '2024-01-01',
        endDate: '2024-12-31'
      };

      const mockData: ReportData = {
        statistics: mockStatistics,
        usersByRole: [],
        appointmentsByStatus: [],
        appointmentsByMonth: [],
        specialtiesRanking: []
      };

      let result: ReportData | undefined;
      service.getReportData(filter).subscribe(res => result = res);

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('startDate')).toBe('2024-01-01');
      expect(req.request.params.get('endDate')).toBe('2024-12-31');
      req.flush(mockData);
      tick();

      expect(result?.statistics.totalUsers).toBe(100);
    }));
  });

  describe('getStatistics', () => {
    it('should fetch statistics without date filter', fakeAsync(() => {
      let result: ReportStatistics | undefined;
      service.getStatistics().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/statistics`);
      expect(req.request.method).toBe('GET');
      req.flush(mockStatistics);
      tick();

      expect(result?.activeUsers).toBe(85);
    }));

    it('should fetch statistics with date filter', fakeAsync(() => {
      service.getStatistics('2024-01-01', '2024-12-31').subscribe();

      const req = httpMock.expectOne(request => request.url.includes('/statistics'));
      expect(req.request.params.get('startDate')).toBe('2024-01-01');
      expect(req.request.params.get('endDate')).toBe('2024-12-31');
      req.flush(mockStatistics);
      tick();
    }));
  });

  describe('getUsersByRole', () => {
    it('should fetch users by role distribution', fakeAsync(() => {
      const mockData: UsersByRoleData[] = [
        { role: 'ADMIN', count: 5, percentage: 5 },
        { role: 'PROFESSIONAL', count: 20, percentage: 20 },
        { role: 'PATIENT', count: 75, percentage: 75 }
      ];

      let result: UsersByRoleData[] | undefined;
      service.getUsersByRole().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/users-by-role`);
      expect(req.request.method).toBe('GET');
      req.flush(mockData);
      tick();

      expect(result?.length).toBe(3);
    }));
  });

  describe('getAppointmentsByStatus', () => {
    it('should fetch appointments by status', fakeAsync(() => {
      const mockData: AppointmentsByStatusData[] = [
        { status: 'Scheduled', count: 100, color: '#blue' },
        { status: 'Completed', count: 400, color: '#green' }
      ];

      let result: AppointmentsByStatusData[] | undefined;
      service.getAppointmentsByStatus().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/appointments-by-status`);
      expect(req.request.method).toBe('GET');
      req.flush(mockData);
      tick();

      expect(result?.length).toBe(2);
    }));
  });

  describe('getAppointmentsByMonth', () => {
    it('should fetch appointments by month for a year', fakeAsync(() => {
      const mockData: AppointmentsByMonthData[] = [
        { month: 'Jan', appointments: 50, completed: 45, canceled: 5 },
        { month: 'Feb', appointments: 60, completed: 55, canceled: 5 }
      ];

      let result: AppointmentsByMonthData[] | undefined;
      service.getAppointmentsByMonth(2024).subscribe(res => result = res);

      const req = httpMock.expectOne(request => request.url.includes('/appointments-by-month'));
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('year')).toBe('2024');
      req.flush(mockData);
      tick();

      expect(result?.length).toBe(2);
    }));
  });

  describe('getSpecialtiesRanking', () => {
    it('should fetch specialties ranking', fakeAsync(() => {
      const mockData: SpecialtiesRankingData[] = [
        { specialty: 'Cardiologia', appointments: 150, revenue: 15000 },
        { specialty: 'Neurologia', appointments: 100, revenue: 12000 }
      ];

      let result: SpecialtiesRankingData[] | undefined;
      service.getSpecialtiesRanking().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/specialties-ranking`);
      expect(req.request.method).toBe('GET');
      req.flush(mockData);
      tick();

      expect(result?.[0].specialty).toBe('Cardiologia');
    }));
  });

  describe('exportReport', () => {
    it('should export report as PDF', fakeAsync(() => {
      const filter: ReportFilter = {
        startDate: '2024-01-01',
        endDate: '2024-12-31'
      };

      const blob = new Blob(['pdf content'], { type: 'application/pdf' });

      let result: Blob | undefined;
      service.exportReport(filter, 'pdf').subscribe(res => result = res);

      const req = httpMock.expectOne(request => request.url.includes('/export'));
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('format')).toBe('pdf');
      expect(req.request.responseType).toBe('blob');
      req.flush(blob);
      tick();

      expect(result).toBeTruthy();
    }));

    it('should export report as Excel', fakeAsync(() => {
      const filter: ReportFilter = {
        startDate: '2024-01-01',
        endDate: '2024-12-31'
      };

      service.exportReport(filter, 'excel').subscribe();

      const req = httpMock.expectOne(request => request.url.includes('/export'));
      expect(req.request.params.get('format')).toBe('excel');
      req.flush(new Blob());
      tick();
    }));
  });
});
