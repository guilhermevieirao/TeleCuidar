import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import {
  ScheduleBlocksService,
  ScheduleBlock,
  CreateScheduleBlockDto,
  UpdateScheduleBlockDto,
  PaginatedResponse
} from './schedule-blocks.service';
import { environment } from '@env/environment';

describe('ScheduleBlocksService', () => {
  let service: ScheduleBlocksService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/scheduleblocks`;

  const mockBlock: ScheduleBlock = {
    id: '1',
    professionalId: 'prof-123',
    professionalName: 'Dr. Carlos',
    type: 'Single',
    date: '2024-12-25',
    reason: 'Feriado de Natal',
    status: 'Pending',
    createdAt: '2024-01-01T00:00:00Z'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [ScheduleBlocksService]
    });

    service = TestBed.inject(ScheduleBlocksService);
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

  describe('getScheduleBlocks', () => {
    it('should fetch schedule blocks with pagination', fakeAsync(() => {
      const mockResponse: PaginatedResponse<ScheduleBlock> = {
        data: [mockBlock],
        total: 1,
        page: 1,
        pageSize: 10,
        totalPages: 1
      };

      let result: PaginatedResponse<ScheduleBlock> | undefined;
      service.getScheduleBlocks().subscribe(res => result = res);

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
      tick();

      expect(result?.data.length).toBe(1);
    }));

    it('should filter by professionalId', fakeAsync(() => {
      service.getScheduleBlocks('prof-123').subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('professionalId')).toBe('prof-123');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));

    it('should filter by status', fakeAsync(() => {
      service.getScheduleBlocks(undefined, 'Approved').subscribe();

      const req = httpMock.expectOne(request => request.url === apiUrl);
      expect(req.request.params.get('status')).toBe('Approved');
      req.flush({ data: [], total: 0, page: 1, pageSize: 10, totalPages: 0 });
      tick();
    }));
  });

  describe('getScheduleBlockById', () => {
    it('should fetch a single block by id', fakeAsync(() => {
      let result: ScheduleBlock | undefined;
      service.getScheduleBlockById('1').subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('GET');
      req.flush(mockBlock);
      tick();

      expect(result?.reason).toBe('Feriado de Natal');
    }));
  });

  describe('createScheduleBlock', () => {
    it('should create a single day block', fakeAsync(() => {
      const dto: CreateScheduleBlockDto = {
        professionalId: 'prof-123',
        type: 'Single',
        date: '2024-12-25',
        reason: 'Feriado'
      };

      let result: ScheduleBlock | undefined;
      service.createScheduleBlock(dto).subscribe(res => result = res);

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.type).toBe('Single');
      req.flush(mockBlock);
      tick();

      expect(result).toBeTruthy();
    }));

    it('should create a range block', fakeAsync(() => {
      const dto: CreateScheduleBlockDto = {
        professionalId: 'prof-123',
        type: 'Range',
        startDate: '2024-12-20',
        endDate: '2024-12-31',
        reason: 'Férias'
      };

      service.createScheduleBlock(dto).subscribe();

      const req = httpMock.expectOne(apiUrl);
      expect(req.request.body.type).toBe('Range');
      expect(req.request.body.startDate).toBe('2024-12-20');
      req.flush({ ...mockBlock, type: 'Range' });
      tick();
    }));

    it('should emit blocksChanged after create', fakeAsync(() => {
      let changed = false;
      service.blocksChanged$.subscribe(() => changed = true);

      service.createScheduleBlock({
        professionalId: 'prof-123',
        type: 'Single',
        date: '2024-12-25',
        reason: 'Test'
      }).subscribe();

      const req = httpMock.expectOne(apiUrl);
      req.flush(mockBlock);
      tick();

      expect(changed).toBe(true);
    }));
  });

  describe('updateScheduleBlock', () => {
    it('should update a block', fakeAsync(() => {
      const dto: UpdateScheduleBlockDto = { reason: 'Motivo atualizado' };

      let result: ScheduleBlock | undefined;
      service.updateScheduleBlock('1', dto).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('PATCH');
      req.flush({ ...mockBlock, reason: 'Motivo atualizado' });
      tick();

      expect(result?.reason).toBe('Motivo atualizado');
    }));
  });

  describe('deleteScheduleBlock', () => {
    it('should delete a block', fakeAsync(() => {
      let completed = false;
      service.deleteScheduleBlock('1').subscribe(() => completed = true);

      const req = httpMock.expectOne(`${apiUrl}/1`);
      expect(req.request.method).toBe('DELETE');
      req.flush(null);
      tick();

      expect(completed).toBe(true);
    }));
  });

  describe('approveScheduleBlock', () => {
    it('should approve a block', fakeAsync(() => {
      let result: ScheduleBlock | undefined;
      service.approveScheduleBlock('1', { approvedBy: 'admin-1' }).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/1/approve`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.approvedBy).toBe('admin-1');
      req.flush({ ...mockBlock, status: 'Approved' });
      tick();

      expect(result?.status).toBe('Approved');
    }));
  });

  describe('rejectScheduleBlock', () => {
    it('should reject a block with reason', fakeAsync(() => {
      let result: ScheduleBlock | undefined;
      service.rejectScheduleBlock('1', {
        rejectedBy: 'admin-1',
        rejectionReason: 'Período não disponível'
      }).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/1/reject`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.rejectionReason).toBe('Período não disponível');
      req.flush({ ...mockBlock, status: 'Rejected' });
      tick();

      expect(result?.status).toBe('Rejected');
    }));
  });

  describe('checkConflict', () => {
    it('should check for conflicts on a date', fakeAsync(() => {
      let result: { hasConflict: boolean } | undefined;
      service.checkConflict('prof-123', '2024-12-25').subscribe(res => result = res);

      const req = httpMock.expectOne(request => request.url.includes('/check-conflict'));
      expect(req.request.method).toBe('GET');
      expect(req.request.params.get('date')).toBe('2024-12-25');
      req.flush({ hasConflict: true });
      tick();

      expect(result?.hasConflict).toBe(true);
    }));

    it('should check for conflicts on a date range', fakeAsync(() => {
      service.checkConflict('prof-123', undefined, '2024-12-20', '2024-12-31').subscribe();

      const req = httpMock.expectOne(request => request.url.includes('/check-conflict'));
      expect(req.request.params.get('startDate')).toBe('2024-12-20');
      expect(req.request.params.get('endDate')).toBe('2024-12-31');
      req.flush({ hasConflict: false });
      tick();
    }));

    it('should exclude block by id when checking conflicts', fakeAsync(() => {
      service.checkConflict('prof-123', '2024-12-25', undefined, undefined, 'block-1').subscribe();

      const req = httpMock.expectOne(request => request.url.includes('/check-conflict'));
      expect(req.request.params.get('excludeBlockId')).toBe('block-1');
      req.flush({ hasConflict: false });
      tick();
    }));
  });
});
