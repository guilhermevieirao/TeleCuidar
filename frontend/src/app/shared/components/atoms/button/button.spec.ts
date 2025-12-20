import { ComponentFixture, TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ButtonComponent, ButtonVariant, ButtonSize } from './button';

describe('ButtonComponent', () => {
  let component: ButtonComponent;
  let fixture: ComponentFixture<ButtonComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ButtonComponent]
    }).compileComponents();
  });

  function createComponent(overrides: Partial<ButtonComponent> = {}): void {
    fixture = TestBed.createComponent(ButtonComponent);
    component = fixture.componentInstance;
    
    // Apply overrides before first detectChanges
    Object.assign(component, overrides);
    
    fixture.detectChanges();
  }

  describe('creation', () => {
    it('should create', () => {
      createComponent();
      expect(component).toBeTruthy();
    });
  });

  describe('default values', () => {
    beforeEach(() => createComponent());

    it('should have default variant as primary', () => {
      expect(component.variant).toBe('primary');
    });

    it('should have default size as md', () => {
      expect(component.size).toBe('md');
    });

    it('should have default disabled as false', () => {
      expect(component.disabled).toBe(false);
    });

    it('should have default fullWidth as false', () => {
      expect(component.fullWidth).toBe(false);
    });

    it('should have default loading as false', () => {
      expect(component.loading).toBe(false);
    });

    it('should have default type as button', () => {
      expect(component.type).toBe('button');
    });
  });

  describe('variant classes', () => {
    const variants: ButtonVariant[] = ['primary', 'secondary', 'outline', 'ghost', 'danger', 'red', 'green', 'blue'];

    variants.forEach(variant => {
      it(`should apply btn-${variant} class when variant is ${variant}`, () => {
        createComponent({ variant });

        const button = fixture.debugElement.query(By.css('button'));
        expect(button.nativeElement.classList).toContain(`btn-${variant}`);
      });
    });
  });

  describe('size classes', () => {
    const sizes: ButtonSize[] = ['sm', 'md', 'lg'];

    sizes.forEach(size => {
      it(`should apply btn-${size} class when size is ${size}`, () => {
        createComponent({ size });

        const button = fixture.debugElement.query(By.css('button'));
        expect(button.nativeElement.classList).toContain(`btn-${size}`);
      });
    });
  });

  describe('disabled state', () => {
    it('should not be disabled by default', () => {
      createComponent();
      const button = fixture.debugElement.query(By.css('button'));
      expect(button.nativeElement.disabled).toBe(false);
    });

    it('should be disabled when disabled input is true', () => {
      createComponent({ disabled: true });

      const button = fixture.debugElement.query(By.css('button'));
      expect(button.nativeElement.disabled).toBe(true);
    });
  });

  describe('fullWidth', () => {
    it('should not have btn-full class by default', () => {
      createComponent();
      const button = fixture.debugElement.query(By.css('button'));
      expect(button.nativeElement.classList).not.toContain('btn-full');
    });

    it('should have btn-full class when fullWidth is true', () => {
      createComponent({ fullWidth: true });

      const button = fixture.debugElement.query(By.css('button'));
      expect(button.nativeElement.classList).toContain('btn-full');
    });
  });

  describe('button type', () => {
    it('should have type button by default', () => {
      createComponent();
      const button = fixture.debugElement.query(By.css('button'));
      expect(button.nativeElement.type).toBe('button');
    });

    it('should have type submit when set', () => {
      createComponent({ type: 'submit' });

      const button = fixture.debugElement.query(By.css('button'));
      expect(button.nativeElement.type).toBe('submit');
    });

    it('should have type reset when set', () => {
      createComponent({ type: 'reset' });

      const button = fixture.debugElement.query(By.css('button'));
      expect(button.nativeElement.type).toBe('reset');
    });
  });
});
