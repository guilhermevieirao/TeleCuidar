import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { SpecialtiesService, Specialty, CreateSpecialtyDto, UpdateSpecialtyDto, PaginatedResponse } from './specialties.service';
import { environment } from '@env/environment';

describe('SpecialtiesService', () => {
  let service: SpecialtiesService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/specialties`;

  const mockSpecialty: Specialty = {
    id: '1',
    name: 'Cardiologia',
    description: 'Especialidade em doenças do coração',
    status: 'Active',
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-02T00:00:00Z',
    customFields: []
  };

  const mockApiSpecialty = {
    id: '1',
    name: 'Cardiologia',
    description: 'Especialidade em doenças do coração',
    status: 'Active',
    createdAt: '2024-01-01T00:00:00Z',
    updatedAt: '2024-01-02T00:00:00Z',
    customFieldsJson: null
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [SpecialtiesService]
    });

    service = TestBed.inject(SpecialtiesService);
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

  describe('getSpecialties', () => {
    it('should fetch specialties with pagination', fakeAsync(() => {
      const mockResponse = {
        data: [mockApiSpecialty],
        total: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1
      };

      let result: PaginatedResponse<Specialty> | undefined;
      service.getSpecialties().subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');
      req.flush(mockResponse);

      tick();

      expect(result?.data.length).toBe(1);
      expect(result?.data[0].name).toBe('Cardiologia');
      expect(result?.total).toBe(1);
    }));

    it('should apply search filter', fakeAsync(() => {
      service.getSpecialties({ search: 'cardio' }).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('search')).toBe('cardio');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));

    it('should apply status filter', fakeAsync(() => {
      service.getSpecialties({ status: 'Active' }).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('status')).toBe('Active');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));

    it('should apply sorting', fakeAsync(() => {
      service.getSpecialties({}, { field: 'createdAt', direction: 'desc' }).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('sortBy')).toBe('createdAt');
      expect(req.request.params.get('sortDirection')).toBe('desc');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));

    it('should parse customFields from JSON', fakeAsync(() => {
      const customFields = [{ name: 'CRM', type: 'text', required: true }];
      const apiResponse = {
        data: [{
          ...mockApiSpecialty,
          customFieldsJson: JSON.stringify(customFields)
        }],
        total: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1
      };

      let result: PaginatedResponse<Specialty> | undefined;
      service.getSpecialties().subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(request => request.url === apiUrl);
      req.flush(apiResponse);

      tick();

      expect(result?.data[0].customFields).toEqual(customFields);
    }));
  });

  describe('getSpecialtyById', () => {
    it('should fetch a specialty by id', fakeAsync(() => {
      let result: Specialty | undefined;
      service.getSpecialtyById('1').subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockApiSpecialty);

      tick();

      expect(result?.id).toBe('1');
      expect(result?.name).toBe('Cardiologia');
    }));
  });

  describe('createSpecialty', () => {
    it('should create a specialty', fakeAsync(() => {
      const createDto: CreateSpecialtyDto = {
        name: 'Pediatria',
        description: 'Especialidade em crianças',
        status: 'Active'
      };

      let result: Specialty | undefined;
      service.createSpecialty(createDto).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.name).toBe('Pediatria');
      expect(req.request.body.description).toBe('Especialidade em crianças');
      req.flush({ ...mockApiSpecialty, name: 'Pediatria' });

      tick();

      expect(result?.name).toBe('Pediatria');
    }));

    it('should serialize customFields to JSON', fakeAsync(() => {
      const customFields = [{ name: 'Campo1', type: 'text' as const }];
      const createDto: CreateSpecialtyDto = {
        name: 'Test',
        description: 'Test desc',
        status: 'Active',
        customFields
      };

      service.createSpecialty(createDto).subscribe();

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.body.customFieldsJson).toBe(JSON.stringify(customFields));
      req.flush(mockApiSpecialty);

      tick();
    }));
  });

  describe('updateSpecialty', () => {
    it('should update a specialty', fakeAsync(() => {
      const updateDto: UpdateSpecialtyDto = {
        name: 'Cardiologia Pediátrica',
        description: 'Nova descrição'
      };

      let result: Specialty | undefined;
      service.updateSpecialty('1', updateDto).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body.name).toBe('Cardiologia Pediátrica');
      req.flush({ ...mockApiSpecialty, name: 'Cardiologia Pediátrica' });

      tick();

      expect(result?.name).toBe('Cardiologia Pediátrica');
    }));

    it('should update status only', fakeAsync(() => {
      const updateDto: UpdateSpecialtyDto = {
        status: 'Inactive'
      };

      service.updateSpecialty('1', updateDto).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.body.status).toBe('Inactive');
      expect(req.request.body.name).toBeUndefined();
      req.flush({ ...mockApiSpecialty, status: 'Inactive' });

      tick();
    }));
  });

  describe('deleteSpecialty', () => {
    it('should delete a specialty', fakeAsync(() => {
      let completed = false;
      service.deleteSpecialty('1').subscribe(() => {
        completed = true;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      tick();

      expect(completed).toBe(true);
    }));
  });

  describe('toggleSpecialtyStatus', () => {
    it('should toggle status from Active to Inactive', fakeAsync(() => {
      let result: Specialty | undefined;
      service.toggleSpecialtyStatus('1', 'Active').subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body.status).toBe('Inactive');
      req.flush({ ...mockApiSpecialty, status: 'Inactive' });

      tick();

      expect(result?.status).toBe('Inactive');
    }));

    it('should toggle status from Inactive to Active', fakeAsync(() => {
      let result: Specialty | undefined;
      service.toggleSpecialtyStatus('1', 'Inactive').subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.body.status).toBe('Active');
      req.flush({ ...mockApiSpecialty, status: 'Active' });

      tick();

      expect(result?.status).toBe('Active');
    }));
  });
});
