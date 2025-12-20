import { CpfMaskPipe } from './cpf-mask.pipe';
import { PhoneMaskPipe } from './phone-mask.pipe';
import { UserRolePipe } from './user-role.pipe';

describe('CpfMaskPipe', () => {
  let pipe: CpfMaskPipe;

  beforeEach(() => {
    pipe = new CpfMaskPipe();
  });

  it('should create', () => {
    expect(pipe).toBeTruthy();
  });

  it('should return empty string for null', () => {
    expect(pipe.transform(null as any)).toBe('');
  });

  it('should return empty string for undefined', () => {
    expect(pipe.transform(undefined as any)).toBe('');
  });

  it('should return empty string for empty string', () => {
    expect(pipe.transform('')).toBe('');
  });

  it('should format 3 digits', () => {
    expect(pipe.transform('123')).toBe('123');
  });

  it('should format 6 digits with first dot', () => {
    expect(pipe.transform('123456')).toBe('123.456');
  });

  it('should format 9 digits with two dots', () => {
    expect(pipe.transform('123456789')).toBe('123.456.789');
  });

  it('should format complete CPF with 11 digits', () => {
    expect(pipe.transform('12345678901')).toBe('123.456.789-01');
  });

  it('should handle already formatted CPF', () => {
    expect(pipe.transform('123.456.789-01')).toBe('123.456.789-01');
  });

  it('should strip non-digits and format', () => {
    expect(pipe.transform('abc123def456ghi789jkl01')).toBe('123.456.789-01');
  });
});

describe('PhoneMaskPipe', () => {
  let pipe: PhoneMaskPipe;

  beforeEach(() => {
    pipe = new PhoneMaskPipe();
  });

  it('should create', () => {
    expect(pipe).toBeTruthy();
  });

  it('should return empty string for null', () => {
    expect(pipe.transform(null as any)).toBe('');
  });

  it('should return empty string for undefined', () => {
    expect(pipe.transform(undefined as any)).toBe('');
  });

  it('should return empty string for empty string', () => {
    expect(pipe.transform('')).toBe('');
  });

  it('should format 2 digits (area code)', () => {
    expect(pipe.transform('11')).toBe('11');
  });

  it('should format 6 digits with parentheses', () => {
    expect(pipe.transform('119999')).toBe('(11) 9999');
  });

  it('should format 10 digits landline', () => {
    expect(pipe.transform('1133334444')).toBe('(11) 3333-4444');
  });

  it('should format 11 digits mobile', () => {
    expect(pipe.transform('11999998888')).toBe('(11) 99999-8888');
  });

  it('should handle already formatted phone', () => {
    expect(pipe.transform('(11) 99999-8888')).toBe('(11) 99999-8888');
  });

  it('should strip non-digits and format', () => {
    expect(pipe.transform('+55 (11) 99999-8888')).toBe('(55) 11999-9988');
  });
});

describe('UserRolePipe', () => {
  let pipe: UserRolePipe;

  beforeEach(() => {
    pipe = new UserRolePipe();
  });

  it('should create', () => {
    expect(pipe).toBeTruthy();
  });

  it('should return empty string for null', () => {
    expect(pipe.transform(null)).toBe('');
  });

  it('should return empty string for undefined', () => {
    expect(pipe.transform(undefined)).toBe('');
  });

  it('should translate PATIENT to Paciente', () => {
    expect(pipe.transform('PATIENT')).toBe('Paciente');
  });

  it('should translate PROFESSIONAL to Profissional', () => {
    expect(pipe.transform('PROFESSIONAL')).toBe('Profissional');
  });

  it('should translate ADMIN to Administrador', () => {
    expect(pipe.transform('ADMIN')).toBe('Administrador');
  });

  it('should return original value for unknown role', () => {
    expect(pipe.transform('UNKNOWN' as any)).toBe('UNKNOWN');
  });
});
