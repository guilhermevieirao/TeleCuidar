import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { NgZone } from '@angular/core';
import { DictationService } from './dictation.service';
import { ModalService } from './modal.service';

describe('DictationService', () => {
  let service: DictationService;
  let mockModalService: { alert: jest.Mock };

  beforeEach(() => {
    mockModalService = {
      alert: jest.fn().mockReturnValue({ subscribe: jest.fn() })
    };

    // Mock SpeechRecognition
    const mockSpeechRecognition = jest.fn(() => ({
      continuous: false,
      interimResults: false,
      lang: '',
      onresult: null,
      onerror: null,
      onend: null,
      onstart: null,
      start: jest.fn(),
      stop: jest.fn(),
      abort: jest.fn()
    }));

    (window as any).SpeechRecognition = mockSpeechRecognition;
    (window as any).webkitSpeechRecognition = mockSpeechRecognition;

    TestBed.configureTestingModule({
      providers: [
        DictationService,
        { provide: ModalService, useValue: mockModalService }
      ]
    });

    service = TestBed.inject(DictationService);
  });

  afterEach(() => {
    delete (window as any).SpeechRecognition;
    delete (window as any).webkitSpeechRecognition;
  });

  describe('creation', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });
  });

  describe('isDictationActive$', () => {
    it('should initially be false', fakeAsync(() => {
      let isActive: boolean | undefined;
      service.isDictationActive$.subscribe(active => isActive = active);
      tick();

      expect(isActive).toBe(false);
    }));
  });

  describe('isListening$', () => {
    it('should initially be false', fakeAsync(() => {
      let isListening: boolean | undefined;
      service.isListening$.subscribe(listening => isListening = listening);
      tick();

      expect(isListening).toBe(false);
    }));
  });

  describe('toggleDictation', () => {
    it('should start dictation when not active', fakeAsync(() => {
      service.toggleDictation();
      tick();

      let isActive: boolean | undefined;
      service.isDictationActive$.subscribe(active => isActive = active);
      tick();

      expect(isActive).toBe(true);
    }));

    it('should stop dictation when active', fakeAsync(() => {
      service.toggleDictation();
      tick();

      service.toggleDictation();
      tick();

      let isActive: boolean | undefined;
      service.isDictationActive$.subscribe(active => isActive = active);
      tick();

      expect(isActive).toBe(false);
    }));
  });

  describe('startDictation', () => {
    it('should set isDictationActive to true', fakeAsync(() => {
      service.startDictation();
      tick();

      let isActive: boolean | undefined;
      service.isDictationActive$.subscribe(active => isActive = active);
      tick();

      expect(isActive).toBe(true);
    }));
  });

  describe('stopDictation', () => {
    it('should set isDictationActive to false', fakeAsync(() => {
      service.startDictation();
      tick();

      service.stopDictation();
      tick();

      let isActive: boolean | undefined;
      service.isDictationActive$.subscribe(active => isActive = active);
      tick();

      expect(isActive).toBe(false);
    }));
  });

  describe('browser support', () => {
    it('should show alert when SpeechRecognition is not available', () => {
      delete (window as any).SpeechRecognition;
      delete (window as any).webkitSpeechRecognition;

      TestBed.resetTestingModule();
      TestBed.configureTestingModule({
        providers: [
          DictationService,
          { provide: ModalService, useValue: mockModalService }
        ]
      });

      const newService = TestBed.inject(DictationService);
      newService.startDictation();

      expect(mockModalService.alert).toHaveBeenCalledWith(expect.objectContaining({
        title: 'Recurso Indisponível',
        variant: 'warning'
      }));
    });
  });
});
