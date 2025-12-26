import { Injectable, Inject, PLATFORM_ID } from '@angular/core';
import { isPlatformBrowser } from '@angular/common';

/**
 * Serviço para detectar o tipo de dispositivo com alta precisão.
 * Utiliza múltiplos critérios em vez de apenas regex de User Agent.
 * 
 * Critérios de detecção:
 * 1. User Agent (menos confiável, mas um indicador)
 * 2. Touch capabilities (mais confiável para detectar dispositivos reais)
 * 3. Viewport width (móvel típico: < 768px, tablet: 768px-1024px, desktop: > 1024px)
 * 4. Device pixel ratio (dispositivos móveis costumam ter dpi mais alto)
 * 5. Orientação (suporte a orientação é típico de móvel/tablet)
 * 6. Pointer capabilities (touch vs mouse precision)
 */
@Injectable({
  providedIn: 'root'
})
export class DeviceDetectorService {
  private cachedIsMobile: boolean | null = null;
  private cachedIsTablet: boolean | null = null;
  private cachedIsDesktop: boolean | null = null;

  constructor(@Inject(PLATFORM_ID) private platformId: Object) {
    // Limpar cache quando mudar tamanho da tela
    if (isPlatformBrowser(this.platformId)) {
      window.addEventListener('orientationchange', () => {
        this.clearCache();
      });
      
      window.addEventListener('resize', () => {
        this.clearCache();
      });
    }
  }

  /**
   * Detecta se é um dispositivo móvel (smartphone)
   * PRIORIZA: Touch real + User Agent mobile
   * Resolução é critério SECUNDÁRIO
   */
  isMobile(): boolean {
    if (this.cachedIsMobile !== null) {
      return this.cachedIsMobile;
    }

    if (!isPlatformBrowser(this.platformId)) {
      this.cachedIsMobile = false;
      return false;
    }

    const hasRealTouchSupport = this.hasRealTouchSupport();
    const hasMobileUserAgent = this.hasMobileUserAgent();
    const viewportWidth = window.innerWidth;

    /**
     * Nova lógica (não foca em resolução):
     * 1. Se tem touch REAL + user agent mobile + viewport típico de smartphone (< 600px) = É MOBILE
     * 2. Se viewport muito pequeno (< 480px) mesmo sem touch = É MOBILE (fallback)
     * 3. Caso contrário: NÃO é mobile (pode ser tablet ou desktop)
     */

    // Smartphone típico: tem touch + UA mobile + tela pequena
    const isSmartphone = hasRealTouchSupport && hasMobileUserAgent && viewportWidth < 600;
    
    // Fallback: tela muito pequena (provável smartphone antigo)
    const isTinyScreen = viewportWidth < 480;

    this.cachedIsMobile = isSmartphone || isTinyScreen;
    return this.cachedIsMobile;
  }

  /**
   * Detecta se é um dispositivo tablet
   * PRIORIZA: Touch real + User Agent mobile + não é smartphone
   * Resolução é critério SECUNDÁRIO
   */
  isTablet(): boolean {
    if (this.cachedIsTablet !== null) {
      return this.cachedIsTablet;
    }

    if (!isPlatformBrowser(this.platformId)) {
      this.cachedIsTablet = false;
      return false;
    }

    const hasRealTouchSupport = this.hasRealTouchSupport();
    const hasMobileUserAgent = this.hasMobileUserAgent();
    const viewportWidth = window.innerWidth;

    /**
     * Nova lógica (não foca em resolução):
     * - Tem touch REAL + user agent mobile + viewport >= 600px = É TABLET
     * - Independente se está em portrait (800px) ou landscape (1280px)
     */
    this.cachedIsTablet = 
      hasRealTouchSupport &&
      hasMobileUserAgent &&
      viewportWidth >= 600; // Acima de smartphone, qualquer resolução

    return this.cachedIsTablet;
  }

  /**
   * Detecta se é um dispositivo desktop
   */
  isDesktop(): boolean {
    if (this.cachedIsDesktop !== null) {
      return this.cachedIsDesktop;
    }

    if (!isPlatformBrowser(this.platformId)) {
      this.cachedIsDesktop = true;
      return true;
    }

    const isMobileDevice = this.isMobile();
    const isTabletDevice = this.isTablet();

    this.cachedIsDesktop = !isMobileDevice && !isTabletDevice;
    return this.cachedIsDesktop;
  }

  /**
   * Retorna informações detalhadas sobre o dispositivo
   */
  getDeviceInfo() {
    return {
      isMobile: this.isMobile(),
      isTablet: this.isTablet(),
      isDesktop: this.isDesktop(),
      viewportWidth: isPlatformBrowser(this.platformId) ? window.innerWidth : 0,
      viewportHeight: isPlatformBrowser(this.platformId) ? window.innerHeight : 0,
      devicePixelRatio: isPlatformBrowser(this.platformId) ? window.devicePixelRatio : 1,
      userAgent: isPlatformBrowser(this.platformId) ? navigator.userAgent : '',
      orientation: isPlatformBrowser(this.platformId) ? this.getOrientation() : 'unknown'
    };
  }

  // ===== MÉTODOS PRIVADOS =====

  /**
   * Verifica se o dispositivo tem suporte a REAL touch (não simulado em desktop)
   */
  private hasRealTouchSupport(): boolean {
    if (!isPlatformBrowser(this.platformId)) {
      return false;
    }

    // Verificações múltiplas para detectar touch REAL
    const hasTouch = 
      'ontouchstart' in window ||
      ('maxTouchPoints' in navigator && navigator.maxTouchPoints > 0) ||
      ('msMaxTouchPoints' in navigator && (navigator as any).msMaxTouchPoints > 0);

    if (!hasTouch) {
      return false;
    }

    // Verificação adicional: em desktops com resolução baixa, às vezes existe falso "touch"
    // Combinar com User Agent para ter mais certeza
    const userAgent = navigator.userAgent.toLowerCase();
    const isMobileUA = /android|webos|iphone|ipad|ipod|blackberry|iemobile|opera mini/i.test(userAgent);

    // Se tem touch mas User Agent não é móvel, provavelmente é desktop com suporte a touch
    if (!isMobileUA) {
      return false;
    }

    return true;
  }

  /**
   * Verifica o User Agent para sinais de dispositivo móvel
   */
  private hasMobileUserAgent(): boolean {
    if (!isPlatformBrowser(this.platformId)) {
      return false;
    }

    const userAgent = navigator.userAgent.toLowerCase();
    const mobilePatterns = [
      'android',
      'webos',
      'iphone',
      'ipad',
      'ipod',
      'blackberry',
      'iemobile',
      'opera mini',
      'windows phone',
      'firefox.*mobile'
    ];

    return mobilePatterns.some(pattern => new RegExp(pattern).test(userAgent));
  }

  /**
   * Verifica se o dispositivo suporta orientação (móvel/tablet típico)
   */
  private hasOrientationSupport(): boolean {
    if (!isPlatformBrowser(this.platformId)) {
      return false;
    }

    return 'orientation' in window && 'onorientationchange' in window;
  }

  /**
   * Verifica se o dispositivo tem alta densidade de pixels
   */
  private hasHighDpi(): boolean {
    if (!isPlatformBrowser(this.platformId)) {
      return false;
    }

    return window.devicePixelRatio > 1.5;
  }

  /**
   * Retorna a orientação atual do dispositivo
   */
  private getOrientation(): string {
    if (!isPlatformBrowser(this.platformId)) {
      return 'unknown';
    }

    if ('orientation' in window) {
      return (window as any).orientation === 0 || (window as any).orientation === 180
        ? 'portrait'
        : 'landscape';
    }

    return 'unknown';
  }

  /**
   * Limpa o cache de detecção
   */
  private clearCache(): void {
    this.cachedIsMobile = null;
    this.cachedIsTablet = null;
    this.cachedIsDesktop = null;
  }
}
