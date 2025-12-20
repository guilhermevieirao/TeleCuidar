import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import {
  SchedulesService,
  Schedule,
  CreateScheduleDto,
  UpdateScheduleDto,
  PaginatedResponse,
  ProfessionalAvailability
} from './schedules.service';
import { environment } from '@env/environment';

describe('SchedulesService', () => {
  let service: SchedulesService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/schedules`;

  const mockSchedule: Schedule = {
    id: '1',
    professionalId: 'prof-123',
    professionalName: 'Dr. Carlos',
    professionalEmail: 'carlos@hospital.com',
    daysConfig: [
      {
        day: 'Monday',
        isWorking: true,
        timeRange: { startTime: '08:00', endTime: '17:00' },
        consultationDuration: 30,
        intervalBetweenConsultations: 10
      }
    ],
    globalConfig: {
      timeRange: { startTime: '08:00', endTime: '17:00' },
      consultationDuration: 30,
      intervalBetweenConsultations: 10
    },
    validityStartDate: '2024-01-01',
    status: 'Active',
    createdAt: '2024-01-01T00:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SchedulesService]
    });

    service = TestBed.inject(SchedulesService);
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

  describe('getSchedules', () => {
    it('should fetch schedules with pagination', fakeAsync(() => {
      const mockResponse: PaginatedResponse<Schedule> = {
        data: [mockSchedule],
        total: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1
      };

      let result: PaginatedResponse<Schedule> | undefined;
      service.getSchedules().subscribe(res => result = res);

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
      tick();

      expect(result?.data.length).toBe(1);
    }));

    it('should filter by search', fakeAsync(() => {
      service.getSchedules({ search: 'Carlos' }).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('search')).toBe('Carlos');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));

    it('should filter by status', fakeAsync(() => {
      service.getSchedules({ status: 'Active' }).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('status')).toBe('Active');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));

    it('should filter by professionalId', fakeAsync(() => {
      service.getSchedules({ professionalId: 'prof-123' }).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('professionalId')).toBe('prof-123');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));
  });

  describe('getScheduleById', () => {
    it('should fetch a schedule by id', fakeAsync(() => {
      let result: Schedule | undefined;
      service.getScheduleById('1').subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockSchedule);
      tick();

      expect(result?.professionalName).toBe('Dr. Carlos');
    }));
  });

  describe('getScheduleByProfessional', () => {
    it('should fetch schedules for a professional', fakeAsync(() => {
      let result: Schedule[] | undefined;
      service.getScheduleByProfessional('prof-123').subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/professional/prof-123`);
      expect(req.request.method).toBe('GET');
      req.flush([mockSchedule]);
      tick();

      expect(result?.length).toBe(1);
    }));
  });

  describe('getAvailability', () => {
    it('should fetch availability for a professional', fakeAsync(() => {
      const startDate = new Date('2024-12-01');
      const endDate = new Date('2024-12-31');

      const mockAvailability: ProfessionalAvailability = {
        professionalId: 'prof-123',
        professionalName: 'Dr. Carlos',
        slots: [
          { date: '2024-12-01', time: '08:00', isAvailable: true },
          { date: '2024-12-01', time: '08:30', isAvailable: false }
        ]
      };

      let result: ProfessionalAvailability | undefined;
      service.getAvailability('prof-123', startDate, endDate).subscribe(res => result = res);

      const req = httpMock.expectOne(request =>
        request.url.includes('/professional/prof-123/availability')
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockAvailability);
      tick();

      expect(result?.slots.length).toBe(2);
    }));
  });

  describe('createSchedule', () => {
    it('should create a new schedule', fakeAsync(() => {
      const dto: CreateScheduleDto = {
        professionalId: 'prof-123',
        daysConfig: mockSchedule.daysConfig,
        globalConfig: mockSchedule.globalConfig,
        validityStartDate: '2024-01-01',
        status: 'Active'
      };

      let result: Schedule | undefined;
      service.createSchedule(dto).subscribe(res => result = res);

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      req.flush(mockSchedule);
      tick();

      expect(result).toBeTruthy();
    }));
  });

  describe('updateSchedule', () => {
    it('should update a schedule', fakeAsync(() => {
      const dto: UpdateScheduleDto = { status: 'Inactive' };

      let result: Schedule | undefined;
      service.updateSchedule('1', dto).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('PATCH');
      req.flush({ ...mockSchedule, status: 'Inactive' });
      tick();

      expect(result?.status).toBe('Inactive');
    }));
  });

  describe('deleteSchedule', () => {
    it('should delete a schedule', fakeAsync(() => {
      let completed = false;
      service.deleteSchedule('1').subscribe(() => completed = true);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
      tick();

      expect(completed).toBe(true);
    }));
  });

  describe('toggleScheduleStatus', () => {
    it('should toggle from Active to Inactive', fakeAsync(() => {
      let result: Schedule | undefined;
      service.toggleScheduleStatus('1').subscribe(res => result = res);

      // First request: GET to fetch current schedule
      const getReq = httpMock.expectOne(`${apiUrl}/1`);
      expect(getReq.request.method).toBe('GET');
      getReq.flush(mockSchedule);
      tick();

      // Second request: PATCH to update status
      const patchReq = httpMock.expectOne(`${apiUrl}/1`);
      expect(patchReq.request.method).toBe('PATCH');
      expect(patchReq.request.body.status).toBe('Inactive');
      patchReq.flush({ ...mockSchedule, status: 'Inactive' });
      tick();

      expect(result?.status).toBe('Inactive');
    }));
  });
});
