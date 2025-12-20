import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TemporaryUploadService, TemporaryUploadDto } from './temporary-upload.service';
import { environment } from '@env/environment';
import { provideHttpClient } from '@angular/common/http';

describe('TemporaryUploadService', () => {
  let service: TemporaryUploadService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/temporaryuploads`;

  const mockUpload: TemporaryUploadDto = {
    title: 'Documento de teste',
    fileUrl: 'data:image/png;base64,iVBORw0...',
    type: 'image',
    timestamp: Date.now()
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        TemporaryUploadService,
        provideHttpClient(),
        provideHttpClientTesting()
      ]
    });

    service = TestBed.inject(TemporaryUploadService);
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

  describe('storeUpload', () => {
    it('should store upload with token', fakeAsync(() => {
      const token = 'abc123token';

      let result: any;
      service.storeUpload(token, mockUpload).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/${token}`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.title).toBe('Documento de teste');
      expect(req.request.body.type).toBe('image');
      req.flush({ success: true });
      tick();

      expect(result.success).toBe(true);
    }));

    it('should store document type upload', fakeAsync(() => {
      const token = 'doc-token';
      const docUpload: TemporaryUploadDto = {
        ...mockUpload,
        type: 'document',
        title: 'PDF Document'
      };

      service.storeUpload(token, docUpload).subscribe();

      const req = httpMock.expectOne(`${apiUrl}/${token}`);
      expect(req.request.body.type).toBe('document');
      req.flush({ success: true });
      tick();
    }));
  });

  describe('getUpload', () => {
    it('should retrieve upload by token', fakeAsync(() => {
      const token = 'abc123token';

      let result: TemporaryUploadDto | null | undefined;
      service.getUpload(token).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/${token}`);
      expect(req.request.method).toBe('GET');
      req.flush(mockUpload);
      tick();

      expect(result?.title).toBe('Documento de teste');
    }));

    it('should return null on error', fakeAsync(() => {
      const token = 'invalid-token';

      let result: TemporaryUploadDto | null | undefined;
      service.getUpload(token).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/${token}`);
      req.error(new ProgressEvent('error'), { status: 404 });
      tick();

      expect(result).toBeNull();
    }));
  });

  describe('checkUpload', () => {
    it('should return true when upload exists', fakeAsync(() => {
      const token = 'existing-token';

      let result: boolean | undefined;
      service.checkUpload(token).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/${token}`);
      expect(req.request.method).toBe('HEAD');
      req.flush(null, { status: 200, statusText: 'OK' });
      tick();

      expect(result).toBe(true);
    }));

    it('should return false when upload does not exist', fakeAsync(() => {
      const token = 'non-existing-token';

      let result: boolean | undefined;
      service.checkUpload(token).subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/${token}`);
      req.error(new ProgressEvent('error'), { status: 404 });
      tick();

      expect(result).toBe(false);
    }));
  });
});
