import { TestBed } from '@angular/core/testing';
import { PLATFORM_ID, RendererFactory2 } from '@angular/core';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let service: ThemeService;
  let mockRenderer: { setAttribute: jest.Mock; removeAttribute: jest.Mock };
  let mockRendererFactory: { createRenderer: jest.Mock };

  beforeEach(() => {
    mockRenderer = {
      setAttribute: jest.fn(),
      removeAttribute: jest.fn()
    };

    mockRendererFactory = {
      createRenderer: jest.fn().mockReturnValue(mockRenderer)
    };

    // Mock matchMedia on window before each test
    Object.defineProperty(window, 'matchMedia', {
      writable: true,
      value: jest.fn().mockImplementation((query: string) => ({
        matches: false,
        media: query,
        onchange: null,
        addListener: jest.fn(),
        removeListener: jest.fn(),
        addEventListener: jest.fn(),
        removeEventListener: jest.fn(),
        dispatchEvent: jest.fn()
      }))
    });

    TestBed.configureTestingModule({
      providers: [
        ThemeService,
        { provide: RendererFactory2, useValue: mockRendererFactory },
        { provide: PLATFORM_ID, useValue: 'browser' }
      ]
    });

    service = TestBed.inject(ThemeService);
  });

  describe('creation', () => {
    it('should be created', () => {
      expect(service).toBeTruthy();
    });

    it('should create renderer', () => {
      expect(mockRendererFactory.createRenderer).toHaveBeenCalledWith(null, null);
    });
  });

  describe('toggleTheme', () => {
    it('should toggle from light to dark', () => {
      jest.spyOn(document.documentElement, 'getAttribute').mockReturnValue(null);

      service.toggleTheme();

      expect(mockRenderer.setAttribute).toHaveBeenCalledWith(document.documentElement, 'data-theme', 'dark');
    });

    it('should toggle from dark to light', () => {
      jest.spyOn(document.documentElement, 'getAttribute').mockReturnValue('dark');

      service.toggleTheme();

      expect(mockRenderer.removeAttribute).toHaveBeenCalledWith(document.documentElement, 'data-theme');
    });
  });

  describe('isDarkMode', () => {
    it('should return true when dark mode is active', () => {
      jest.spyOn(document.documentElement, 'getAttribute').mockReturnValue('dark');

      expect(service.isDarkMode()).toBe(true);
    });

    it('should return false when light mode is active', () => {
      jest.spyOn(document.documentElement, 'getAttribute').mockReturnValue(null);

      expect(service.isDarkMode()).toBe(false);
    });
  });

  describe('server platform', () => {
    it('should return false for isDarkMode on server', () => {
      TestBed.resetTestingModule();

      TestBed.configureTestingModule({
        providers: [
          ThemeService,
          { provide: RendererFactory2, useValue: mockRendererFactory },
          { provide: PLATFORM_ID, useValue: 'server' }
        ]
      });

      const serverService = TestBed.inject(ThemeService);

      expect(serverService.isDarkMode()).toBe(false);
    });
  });
});
