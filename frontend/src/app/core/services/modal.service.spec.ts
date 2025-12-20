import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { ModalService, ModalConfig, ModalResult } from './modal.service';

describe('ModalService', () => {
  let service: ModalService;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [ModalService]
    });

    service = TestBed.inject(ModalService);
  });

  describe('creation', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });
  });

  describe('open', () => {
    it('should emit modal config on open', fakeAsync(() => {
      const config: ModalConfig = {
        title: 'Test Modal',
        message: 'This is a test'
      };

      let receivedConfig: ModalConfig | undefined;
      service.modal$.subscribe(c => receivedConfig = c);

      service.open(config);
      tick();

      expect(receivedConfig?.title).toBe('Test Modal');
    }));

    it('should return observable that emits result on close', fakeAsync(() => {
      const config: ModalConfig = {
        title: 'Test Modal',
        type: 'confirm'
      };

      let result: ModalResult | undefined;
      service.open(config).subscribe(r => result = r);

      service.close({ confirmed: true });
      tick();

      expect(result?.confirmed).toBe(true);
    }));
  });

  describe('confirm', () => {
    it('should open confirm modal', fakeAsync(() => {
      let receivedConfig: ModalConfig | undefined;
      service.modal$.subscribe(c => receivedConfig = c);

      service.confirm({ title: 'Confirm Action', message: 'Are you sure?' });
      tick();

      expect(receivedConfig?.type).toBe('confirm');
    }));
  });

  describe('alert', () => {
    it('should open alert modal', fakeAsync(() => {
      let receivedConfig: ModalConfig | undefined;
      service.modal$.subscribe(c => receivedConfig = c);

      service.alert({ title: 'Alert', message: 'Something happened' });
      tick();

      expect(receivedConfig?.type).toBe('alert');
    }));
  });

  describe('prompt', () => {
    it('should open prompt modal', fakeAsync(() => {
      let receivedConfig: ModalConfig | undefined;
      service.modal$.subscribe(c => receivedConfig = c);

      service.prompt({
        title: 'Enter Value',
        prompt: { label: 'Name', placeholder: 'Enter your name' }
      });
      tick();

      expect(receivedConfig?.type).toBe('prompt');
    }));

    it('should return prompt value on close', fakeAsync(() => {
      let result: ModalResult | undefined;
      service.prompt({
        title: 'Enter Value',
        prompt: { label: 'Name' }
      }).subscribe(r => result = r);

      service.close({ confirmed: true, promptValue: 'Test Value' });
      tick();

      expect(result?.promptValue).toBe('Test Value');
    }));
  });

  describe('close', () => {
    it('should emit result on close with confirmed true', fakeAsync(() => {
      let result: ModalResult | undefined;
      service.result$.subscribe(r => result = r);

      service.close({ confirmed: true });
      tick();

      expect(result?.confirmed).toBe(true);
    }));

    it('should emit result on close with confirmed false', fakeAsync(() => {
      let result: ModalResult | undefined;
      service.result$.subscribe(r => result = r);

      service.close({ confirmed: false });
      tick();

      expect(result?.confirmed).toBe(false);
    }));
  });

  describe('z-index management', () => {
    it('should increment z-index for nested modals', () => {
      const first = service.getNextZIndex();
      const second = service.getNextZIndex();

      expect(second).toBeGreaterThan(first);
    });

    it('should reset z-index', () => {
      service.getNextZIndex();
      service.getNextZIndex();
      service.resetZIndex();

      const afterReset = service.getNextZIndex();
      expect(afterReset).toBe(2102); // Base (2100) + 2
    });
  });

  describe('modal variants', () => {
    it('should support danger variant', fakeAsync(() => {
      let receivedConfig: ModalConfig | undefined;
      service.modal$.subscribe(c => receivedConfig = c);

      service.confirm({
        title: 'Delete',
        message: 'This action cannot be undone',
        variant: 'danger'
      });
      tick();

      expect(receivedConfig?.variant).toBe('danger');
    }));

    it('should support warning variant', fakeAsync(() => {
      let receivedConfig: ModalConfig | undefined;
      service.modal$.subscribe(c => receivedConfig = c);

      service.alert({
        title: 'Warning',
        message: 'Please check your input',
        variant: 'warning'
      });
      tick();

      expect(receivedConfig?.variant).toBe('warning');
    }));
  });
});
