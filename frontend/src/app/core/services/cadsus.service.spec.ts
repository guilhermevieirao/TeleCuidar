import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { HttpClientTestingModule, HttpTestingController } from '@angular/common/http/testing';
import {
  CadsusService,
  CadsusCidadao,
  CadsusTokenStatus,
  CadsusTokenRenewResponse
} from './cadsus.service';
import { environment } from '@env/environment';

describe('CadsusService', () => {
  let service: CadsusService;
  let httpMock: HttpTestingController;
  const apiUrl = `${environment.apiUrl}/cadsus`;

  const mockCidadao: CadsusCidadao = {
    cns: '123456789012345',
    cpf: '12345678901',
    nome: 'João da Silva',
    dataNascimento: '1990-01-15',
    statusCadastro: 'Ativo',
    nomeMae: 'Maria da Silva',
    nomePai: 'José da Silva',
    sexo: 'Masculino',
    racaCor: 'Branca',
    tipoLogradouro: 'Rua',
    logradouro: 'das Flores',
    numero: '123',
    complemento: 'Apto 45',
    cidade: 'São Paulo',
    codigoCidade: '3550308',
    paisEnderecoAtual: 'Brasil',
    cep: '01234567',
    enderecoCompleto: 'Rua das Flores, 123, Apto 45 - São Paulo/SP',
    cidadeNascimento: 'Rio de Janeiro',
    codigoCidadeNascimento: '3304557',
    paisNascimento: 'Brasil',
    codigoPaisNascimento: '076',
    telefones: ['11999998888'],
    emails: ['joao@email.com']
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [HttpClientTestingModule],
      providers: [CadsusService]
    });

    service = TestBed.inject(CadsusService);
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

  describe('consultarCpf', () => {
    it('should query citizen by CPF', fakeAsync(() => {
      let result: CadsusCidadao | undefined;
      service.consultarCpf('12345678901').subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/consultar-cpf`);
      expect(req.request.method).toBe('POST');
      expect(req.request.body.cpf).toBe('12345678901');
      req.flush(mockCidadao);
      tick();

      expect(result?.nome).toBe('João da Silva');
    }));

    it('should clean CPF before sending', fakeAsync(() => {
      service.consultarCpf('123.456.789-01').subscribe();

      const req = httpMock.expectOne(`${apiUrl}/consultar-cpf`);
      expect(req.request.body.cpf).toBe('12345678901');
      req.flush(mockCidadao);
      tick();
    }));
  });

  describe('getTokenStatus', () => {
    it('should get token status', fakeAsync(() => {
      const mockStatus: CadsusTokenStatus = {
        hasToken: true,
        isValid: true,
        expiresAt: '2024-12-31T23:59:59Z',
        expiresIn: '30 days',
        expiresInMs: 2592000000
      };

      let result: CadsusTokenStatus | undefined;
      service.getTokenStatus().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/token/status`);
      expect(req.request.method).toBe('GET');
      req.flush(mockStatus);
      tick();

      expect(result?.isValid).toBe(true);
    }));

    it('should handle no token', fakeAsync(() => {
      const mockStatus: CadsusTokenStatus = {
        hasToken: false,
        isValid: false,
        expiresInMs: 0,
        message: 'Token não configurado'
      };

      let result: CadsusTokenStatus | undefined;
      service.getTokenStatus().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/token/status`);
      req.flush(mockStatus);
      tick();

      expect(result?.hasToken).toBe(false);
    }));
  });

  describe('renewToken', () => {
    it('should renew token', fakeAsync(() => {
      const mockResponse: CadsusTokenRenewResponse = {
        success: true,
        message: 'Token renovado com sucesso',
        hasToken: true,
        isValid: true,
        expiresAt: '2024-12-31T23:59:59Z',
        expiresIn: '30 days'
      };

      let result: CadsusTokenRenewResponse | undefined;
      service.renewToken().subscribe(res => result = res);

      const req = httpMock.expectOne(`${apiUrl}/token/renew`);
      expect(req.request.method).toBe('POST');
      req.flush(mockResponse);
      tick();

      expect(result?.success).toBe(true);
    }));
  });

  describe('utility methods', () => {
    describe('formatCpf', () => {
      it('should format CPF correctly', () => {
        expect(service.formatCpf('12345678901')).toBe('123.456.789-01');
      });

      it('should return original if not 11 digits', () => {
        expect(service.formatCpf('123')).toBe('123');
      });

      it('should handle already formatted CPF', () => {
        expect(service.formatCpf('123.456.789-01')).toBe('123.456.789-01');
      });
    });

    describe('cleanCpf', () => {
      it('should remove formatting', () => {
        expect(service.cleanCpf('123.456.789-01')).toBe('12345678901');
      });

      it('should return same if already clean', () => {
        expect(service.cleanCpf('12345678901')).toBe('12345678901');
      });
    });

    describe('isValidCpfFormat', () => {
      it('should return true for valid format', () => {
        expect(service.isValidCpfFormat('12345678901')).toBe(true);
        expect(service.isValidCpfFormat('123.456.789-01')).toBe(true);
      });

      it('should return false for invalid format', () => {
        expect(service.isValidCpfFormat('123')).toBe(false);
        expect(service.isValidCpfFormat('123456789012')).toBe(false);
      });
    });
  });
});
