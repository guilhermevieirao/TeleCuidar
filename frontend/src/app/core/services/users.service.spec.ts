import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import { 
  UsersService, 
  User, 
  CreateUserDto, 
  UpdateUserDto, 
  UsersFilter,
  PaginatedResponse,
  PatientProfile,
  ProfessionalProfile
} from './users.service';
import { environment } from '@env/environment';

describe('UsersService', () => {
  let service: UsersService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/users`;

  const mockUser: User = {
    id: '1',
    name: 'João',
    lastName: 'Silva',
    email: 'joao@example.com',
    role: 'PATIENT',
    cpf: '12345678901',
    phone: '11999999999',
    status: 'Active',
    createdAt: '2024-01-01T00:00:00Z',
    emailVerified: true
  };

  const mockPatientProfile: PatientProfile = {
    id: '1',
    cns: '123456789012345',
    socialName: 'João',
    gender: 'Masculino',
    birthDate: '1990-01-01',
    city: 'São Paulo',
    state: 'SP'
  };

  const mockProfessionalProfile: ProfessionalProfile = {
    id: '1',
    crm: 'CRM12345',
    cbo: '225125',
    specialtyId: 'spec-1',
    specialtyName: 'Cardiologia'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [UsersService]
    });

    service = TestBed.inject(UsersService);
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

  describe('getUsers', () => {
    it('should fetch users with pagination', fakeAsync(() => {
      const mockResponse: PaginatedResponse<User> = {
        data: [mockUser],
        total: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1
      };

      let result: PaginatedResponse<User> | undefined;
      service.getUsers().subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('page')).toBe('1');
      expect(req.request.params.get('pageSize')).toBe('10');
      req.flush(mockResponse);

      tick();

      expect(result?.data.length).toBe(1);
      expect(result?.data[0].name).toBe('João');
    }));

    it('should apply search filter', fakeAsync(() => {
      const filter: UsersFilter = { search: 'João' };

      service.getUsers(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('search')).toBe('João');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));

    it('should filter by role', fakeAsync(() => {
      const filter: UsersFilter = { role: 'PROFESSIONAL' };

      service.getUsers(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('role')).toBe('PROFESSIONAL');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));

    it('should filter by status', fakeAsync(() => {
      const filter: UsersFilter = { status: 'Active' };

      service.getUsers(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('status')).toBe('Active');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));

    it('should filter by specialtyId', fakeAsync(() => {
      const filter: UsersFilter = { specialtyId: 'spec-123' };

      service.getUsers(filter).subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('specialtyId')).toBe('spec-123');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });

      tick();
    }));
  });

  describe('getUserById', () => {
    it('should fetch a single user by id', fakeAsync(() => {
      let result: User | undefined;
      service.getUserById('1').subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUser);

      tick();

      expect(result?.id).toBe('1');
      expect(result?.email).toBe('joao@example.com');
    }));
  });

  describe('createUser', () => {
    it('should create a user', fakeAsync(() => {
      const createDto: CreateUserDto = {
        name: 'Maria',
        lastName: 'Santos',
        email: 'maria@example.com',
        cpf: '98765432100',
        phone: '11988888888',
        password: 'Password123!',
        role: 'PATIENT',
        status: 'Active'
      };

      let result: User | undefined;
      service.createUser(createDto).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(createDto);
      req.flush({ ...mockUser, name: 'Maria' });

      tick();

      expect(result?.name).toBe('Maria');
    }));

    it('should create user with patient profile', fakeAsync(() => {
      const createDto: CreateUserDto = {
        name: 'Carlos',
        lastName: 'Oliveira',
        email: 'carlos@example.com',
        cpf: '11122233344',
        password: 'Password123!',
        role: 'PATIENT',
        status: 'Active',
        patientProfile: {
          cns: '123456789012345',
          gender: 'Masculino',
          birthDate: '1985-05-15'
        }
      };

      service.createUser(createDto).subscribe();

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.body.patientProfile).toBeDefined();
      expect(req.request.body.patientProfile.cns).toBe('123456789012345');
      req.flush(mockUser);

      tick();
    }));

    it('should create user with professional profile', fakeAsync(() => {
      const createDto: CreateUserDto = {
        name: 'Dr. Ana',
        lastName: 'Souza',
        email: 'ana@example.com',
        cpf: '55566677788',
        password: 'Password123!',
        role: 'PROFESSIONAL',
        status: 'Active',
        professionalProfile: {
          crm: 'CRM99999',
          cbo: '225125',
          specialtyId: 'spec-1'
        }
      };

      service.createUser(createDto).subscribe();

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.body.professionalProfile).toBeDefined();
      expect(req.request.body.professionalProfile.crm).toBe('CRM99999');
      req.flush({ ...mockUser, role: 'PROFESSIONAL' });

      tick();
    }));
  });

  describe('updateUser', () => {
    it('should update a user', fakeAsync(() => {
      const updateDto: UpdateUserDto = {
        name: 'João Carlos',
        phone: '11977777777'
      };

      let result: User | undefined;
      service.updateUser('1', updateDto).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(updateDto);
      req.flush({ ...mockUser, name: 'João Carlos' });

      tick();

      expect(result?.name).toBe('João Carlos');
    }));

    it('should update user status', fakeAsync(() => {
      const updateDto: UpdateUserDto = {
        status: 'Inactive'
      };

      service.updateUser('1', updateDto).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.body.status).toBe('Inactive');
      req.flush({ ...mockUser, status: 'Inactive' });

      tick();
    }));
  });

  describe('deleteUser', () => {
    it('should delete a user', fakeAsync(() => {
      let completed = false;
      service.deleteUser('1').subscribe(() => {
        completed = true;
      });

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);

      tick();

      expect(completed).toBe(true);
    }));
  });

  describe('getUserStats', () => {
    it('should fetch user stats', fakeAsync(() => {
      const mockStats = {
        totalUsers: 100,
        activeUsers: 80,
        inactiveUsers: 20,
        byRole: {
          PATIENT: 60,
          PROFESSIONAL: 35,
          ADMIN: 5
        }
      };

      let result: any;
      service.getUserStats().subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/stats`);
      expect(req.request.method).toBe('GET');
      req.flush(mockStats);

      tick();

      expect(result.totalUsers).toBe(100);
    }));
  });

  describe('generateInviteLink', () => {
    it('should generate invite link', fakeAsync(() => {
      const inviteData = {
        email: 'novo@example.com',
        role: 'PROFESSIONAL' as const,
        specialtyId: 'spec-1'
      };

      let result: any;
      service.generateInviteLink(inviteData).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${environment.apiUrl}/invites/generate-link`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body).toEqual(inviteData);
      req.flush({ token: 'invite-token-123', link: 'https://...' });

      tick();

      expect(result.token).toBe('invite-token-123');
    }));
  });

  describe('Patient Profile', () => {
    it('should get patient profile', fakeAsync(() => {
      let result: PatientProfile | undefined;
      service.getPatientProfile('1').subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1/patient-profile`);
      expect(req.request.method).toBe('GET');
      req.flush(mockPatientProfile);

      tick();

      expect(result?.cns).toBe('123456789012345');
    }));

    it('should update patient profile', fakeAsync(() => {
      const profileUpdate = {
        cns: '999888777666555',
        city: 'Rio de Janeiro',
        state: 'RJ'
      };

      let result: PatientProfile | undefined;
      service.updatePatientProfile('1', profileUpdate).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1/patient-profile`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(profileUpdate);
      req.flush({ ...mockPatientProfile, ...profileUpdate });

      tick();

      expect(result?.cns).toBe('999888777666555');
    }));
  });

  describe('Professional Profile', () => {
    it('should get professional profile', fakeAsync(() => {
      let result: ProfessionalProfile | undefined;
      service.getProfessionalProfile('1').subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1/professional-profile`);
      expect(req.request.method).toBe('GET');
      req.flush(mockProfessionalProfile);

      tick();

      expect(result?.crm).toBe('CRM12345');
    }));

    it('should update professional profile', fakeAsync(() => {
      const profileUpdate = {
        crm: 'CRM54321',
        specialtyId: 'spec-2'
      };

      let result: ProfessionalProfile | undefined;
      service.updateProfessionalProfile('1', profileUpdate).subscribe(res => {
        result = res;
      });

      const req = httpMock.expectOne(`${apiUrl}/1/professional-profile`);
      expect(req.request.method).toBe('PUT');
      expect(req.request.body).toEqual(profileUpdate);
      req.flush({ ...mockProfessionalProfile, ...profileUpdate });

      tick();

      expect(result?.crm).toBe('CRM54321');
    }));
  });
});
