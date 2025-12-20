import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { 
  AppointmentsService, 
  Appointment, 
  CreateAppointmentDto, 
  UpdateAppointmentDto, 
  PaginatedResponse,
  AppointmentsFilter 
} from './appointments.service';
import { environment } from '@env/environment';

describe('AppointmentsService', () => {
  let service: AppointmentsService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/appointments`;

  const mockAppointment: Appointment = {
    id: '1',
    patientId: 'patient-1',
    patientName: 'João Silva',
    professionalId: 'prof-1',
    professionalName: 'Dr. Carlos',
    specialtyId: 'spec-1',
    specialtyName: 'Cardiologia',
    date: '2024-12-01',
    time: '10:00',
    endTime: '10:30',
    type: 'FirstVisit',
    status: 'Scheduled',
    observation: 'Primeira consulta',
    createdAt: '2024-01-01T00:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [AppointmentsService]
    });

    service = TestBed.inject(AppointmentsService);
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

  describe('getAppointments', () => {
    it('should fetch appointments with pagination', fakeAsync(() => {
      const mockResponse: PaginatedResponse<Appointment> = {
        data: [mockAppointment],
        total: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1
      };

      let result: PaginatedResponse<Appointment> | undefined;
      service.getAppointments().subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');
      req.flush(mockResponse);

      tick();

      expect(result?.data.length).toBe(1);
      expect(result?.data[0].patientName).toBe('João Silva');
    }));

    it('should apply status filter', fakeAsync(() => {
      const filter: AppointmentsFilter = { status: 'Scheduled' };

      service.getAppointments(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('status')).toBe('Scheduled');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));

    it('should apply search filter', fakeAsync(() => {
      const filter: AppointmentsFilter = { search: 'João' };

      service.getAppointments(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('search')).toBe('João');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));

    it('should apply date filters', fakeAsync(() => {
      const filter: AppointmentsFilter = { 
        startDate: '2024-01-01', 
        endDate: '2024-12-31' 
      };

      service.getAppointments(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('startDate')).toBe('2024-01-01');
      expect(req.request.params.get('endDate')).toBe('2024-12-31');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));

    it('should filter by patientId', fakeAsync(() => {
      const filter: AppointmentsFilter = { patientId: 'patient-123' };

      service.getAppointments(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('patientId')).toBe('patient-123');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));

    it('should filter by professionalId', fakeAsync(() => {
      const filter: AppointmentsFilter = { professionalId: 'prof-456' };

      service.getAppointments(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('professionalId')).toBe('prof-456');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));
  });

  describe('getAppointmentById', () => {
    it('should fetch a single appointment by id', fakeAsync(() => {
      let result: Appointment | undefined;
      service.getAppointmentById('1').subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockAppointment);

      tick();

      expect(result?.id).toBe('1');
      expect(result?.patientName).toBe('João Silva');
    }));
  });

  describe('createAppointment', () => {
    it('should create an appointment', fakeAsync(() => {
      const createDto: CreateAppointmentDto = {
        patientId: 'patient-1',
        professionalId: 'prof-1',
        specialtyId: 'spec-1',
        date: '2024-12-01',
        time: '14:00',
        type: 'Return',
        observation: 'Retorno'
      };

      let result: Appointment | undefined;
      service.createAppointment(createDto).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(createDto);
      req.flush({ ...mockAppointment, time: '14:00', type: 'Return' });

      tick();

      expect(result?.time).toBe('14:00');
    }));
  });

  describe('updateAppointment', () => {
    it('should update an appointment', fakeAsync(() => {
      const updateDto: UpdateAppointmentDto = {
        status: 'Confirmed',
        observation: 'Confirmado pelo paciente'
      };

      let result: Appointment | undefined;
      service.updateAppointment('1', updateDto).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('PATCH');
      expect(req.request.body).toEqual(updateDto);
      req.flush({ ...mockAppointment, status: 'Confirmed' });

      tick();

      expect(result?.status).toBe('Confirmed');
    }));
  });

  describe('cancelAppointment', () => {
    it('should cancel an appointment with reason', fakeAsync(() => {
      const reason = 'Paciente solicitou cancelamento';

      let result: Appointment | undefined;
      service.cancelAppointment('1', reason).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1/cancel`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ reason });
      req.flush({ ...mockAppointment, status: 'Cancelled' });

      tick();

      expect(result?.status).toBe('Cancelled');
    }));

    it('should cancel without reason', fakeAsync(() => {
      service.cancelAppointment('1').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/1/cancel`);
      expect(req.request.body).toEqual({ reason: undefined });
      req.flush({ ...mockAppointment, status: 'Cancelled' });

      tick();
    }));
  });

  describe('confirmAppointment', () => {
    it('should confirm an appointment', fakeAsync(() => {
      let result: Appointment | undefined;
      service.confirmAppointment('1').subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1/confirm`);
      expect(req.request.method).toBe('PATCH');
      req.flush({ ...mockAppointment, status: 'Confirmed' });

      tick();

      expect(result?.status).toBe('Confirmed');
    }));
  });

  describe('startAppointment', () => {
    it('should start an appointment', fakeAsync(() => {
      let result: Appointment | undefined;
      service.startAppointment('1').subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1/start`);
      expect(req.request.method).toBe('PATCH');
      req.flush({ ...mockAppointment, status: 'InProgress' });

      tick();

      expect(result?.status).toBe('InProgress');
    }));
  });

  describe('completeAppointment', () => {
    it('should complete an appointment with observation', fakeAsync(() => {
      const observation = 'Consulta finalizada com sucesso';

      let result: Appointment | undefined;
      service.completeAppointment('1', observation).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1/finish`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual({ observation });
      req.flush({ ...mockAppointment, status: 'Completed' });

      tick();

      expect(result?.status).toBe('Completed');
    }));
  });

  describe('deleteAppointment', () => {
    it('should delete an appointment', fakeAsync(() => {
      let completed = false;
      service.deleteAppointment('1').subscribe(() => {
        completed = true;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      tick();

      expect(completed).toBe(true);
    }));
  });
});
