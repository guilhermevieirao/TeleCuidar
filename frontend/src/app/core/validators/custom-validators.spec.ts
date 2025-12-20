import { FormControl, FormGroup } from '@angular/forms';
import { CustomValidators } from './custom-validators';

describe('CustomValidators', () => {
  describe('cpf', () => {
    const validator = CustomValidators.cpf();

    it('should return null for valid CPF without formatting', () => {
      const control = new FormControl('12345678909');
      expect(validator(control)).toBeNull();
    });

    it('should return null for valid CPF with formatting', () => {
      const control = new FormControl('123.456.789-09');
      expect(validator(control)).toBeNull();
    });

    it('should return null for another valid CPF', () => {
      const control = new FormControl('52998224725');
      expect(validator(control)).toBeNull();
    });

    it('should return error for CPF with all same digits', () => {
      const control = new FormControl('11111111111');
      expect(validator(control)).toEqual({ cpfInvalid: true });
    });

    it('should return error for CPF with wrong length', () => {
      const control = new FormControl('1234567890');
      expect(validator(control)).toEqual({ cpfInvalid: true });
    });

    it('should return error for invalid CPF', () => {
      const control = new FormControl('12345678901');
      expect(validator(control)).toEqual({ cpfInvalid: true });
    });

    it('should return null for empty value', () => {
      const control = new FormControl('');
      expect(validator(control)).toBeNull();
    });

    it('should return null for null value', () => {
      const control = new FormControl(null);
      expect(validator(control)).toBeNull();
    });
  });

  describe('phone', () => {
    const validator = CustomValidators.phone();

    it('should return null for valid mobile phone (11 digits)', () => {
      const control = new FormControl('11999999999');
      expect(validator(control)).toBeNull();
    });

    it('should return null for valid landline phone (10 digits)', () => {
      const control = new FormControl('1133333333');
      expect(validator(control)).toBeNull();
    });

    it('should return null for phone with formatting', () => {
      const control = new FormControl('(11) 99999-9999');
      expect(validator(control)).toBeNull();
    });

    it('should return error for phone with wrong length', () => {
      const control = new FormControl('123456789');
      expect(validator(control)).toEqual({ phoneInvalid: true });
    });

    it('should return null for empty value', () => {
      const control = new FormControl('');
      expect(validator(control)).toBeNull();
    });
  });

  describe('strongPassword', () => {
    const validator = CustomValidators.strongPassword();

    it('should return null for valid strong password', () => {
      const control = new FormControl('Test@123');
      expect(validator(control)).toBeNull();
    });

    it('should return null for another valid password', () => {
      const control = new FormControl('P@ssword1');
      expect(validator(control)).toBeNull();
    });

    it('should return error for password without uppercase', () => {
      const control = new FormControl('test@123');
      expect(validator(control)).toEqual({ weakPassword: true });
    });

    it('should return error for password without lowercase', () => {
      const control = new FormControl('TEST@123');
      expect(validator(control)).toEqual({ weakPassword: true });
    });

    it('should return error for password without number', () => {
      const control = new FormControl('Test@abc');
      expect(validator(control)).toEqual({ weakPassword: true });
    });

    it('should return error for password without special char', () => {
      const control = new FormControl('Test1234');
      expect(validator(control)).toEqual({ weakPassword: true });
    });

    it('should return error for password too short', () => {
      const control = new FormControl('Te@1');
      expect(validator(control)).toEqual({ weakPassword: true });
    });

    it('should return null for empty value', () => {
      const control = new FormControl('');
      expect(validator(control)).toBeNull();
    });
  });

  describe('passwordMatch', () => {
    it('should return null when passwords match', () => {
      const form = new FormGroup({
        password: new FormControl('Test@123'),
        confirmPassword: new FormControl('Test@123'),
      });
      
      const validator = CustomValidators.passwordMatch('password', 'confirmPassword');
      expect(validator(form)).toBeNull();
    });

    it('should return error when passwords do not match', () => {
      const form = new FormGroup({
        password: new FormControl('Test@123'),
        confirmPassword: new FormControl('Different@123'),
      });
      
      const validator = CustomValidators.passwordMatch('password', 'confirmPassword');
      expect(validator(form)).toEqual({ passwordMismatch: true });
    });

    it('should return null when confirmPassword is empty', () => {
      const form = new FormGroup({
        password: new FormControl('Test@123'),
        confirmPassword: new FormControl(''),
      });
      
      const validator = CustomValidators.passwordMatch('password', 'confirmPassword');
      expect(validator(form)).toBeNull();
    });
  });
});
