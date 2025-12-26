// TeleCuidar - Configuração de Interface Customizada do Jitsi
// Este arquivo sobrescreve as configurações padrão de interface
// IMPORTANTE: Estas configurações são aplicadas server-side, reduzindo
// a quantidade de parâmetros passados via URL pelo JitsiMeetExternalAPI

interfaceConfig = {
    // ========================================
    // WATERMARKS E LOGOS - DESABILITADOS
    // ========================================
    SHOW_JITSI_WATERMARK: false,
    SHOW_WATERMARK_FOR_GUESTS: false,
    SHOW_BRAND_WATERMARK: false,
    BRAND_WATERMARK_LINK: '',
    JITSI_WATERMARK_LINK: '',
    
    // Logos vazios
    DEFAULT_LOGO_URL: '',
    DEFAULT_WELCOME_PAGE_LOGO_URL: '',
    
    // ========================================
    // PROMOÇÕES E DEEP LINKING - DESABILITADOS
    // ========================================
    SHOW_POWERED_BY: false,
    SHOW_PROMOTIONAL_CLOSE_PAGE: false,
    MOBILE_APP_PROMO: false,
    HIDE_DEEP_LINKING_LOGO: true,
    
    // ========================================
    // NOTIFICAÇÕES E INDICADORES
    // ========================================
    DISABLE_JOIN_LEAVE_NOTIFICATIONS: false,
    DISABLE_PRESENCE_STATUS: false,
    DISABLE_FOCUS_INDICATOR: false,
    DISABLE_DOMINANT_SPEAKER_INDICATOR: false,
    
    // ========================================
    // CONFIGURAÇÕES GERAIS
    // ========================================
    HIDE_INVITE_MORE_HEADER: true,
    GENERATE_ROOMNAMES_ON_WELCOME_PAGE: false,
    LANG_DETECTION: false,
    
    // ========================================
    // FILMSTRIP
    // ========================================
    filmStripOnly: false,
    VERTICAL_FILMSTRIP: true,
    TILE_VIEW_MAX_COLUMNS: 2,
    
    // ========================================
    // TOOLBAR - Botões disponíveis
    // Nota: Botões de moderador são controlados dinamicamente pelo frontend
    // ========================================
    TOOLBAR_BUTTONS: [
        'microphone',
        'camera',
        'desktop',
        'fullscreen',
        'fodeviceselection',
        'hangup',
        'chat',
        'settings',
        'videoquality',
        'filmstrip',
        'tileview',
        'select-background',
        'mute-everyone',
        'mute-video-everyone'
    ],
    
    // Botões sempre disponíveis no mobile (incluindo compartilhamento de tela)
    TOOLBAR_ALWAYS_VISIBLE: ['microphone', 'camera', 'desktop', 'hangup'],
    
    // ========================================
    // CONFIGURAÇÕES
    // ========================================
    SETTINGS_SECTIONS: ['devices', 'language', 'moderator', 'profile'],
    
    // ========================================
    // NOMES PADRÃO (em Português)
    // ========================================
    DEFAULT_LOCAL_DISPLAY_NAME: 'Eu',
    DEFAULT_REMOTE_DISPLAY_NAME: 'Participante'
};
