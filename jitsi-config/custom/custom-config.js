// TeleCuidar - Configuração Customizada do Jitsi Meet
// Este arquivo configura o Jitsi para funcionar apenas via autenticação JWT
// IMPORTANTE: Estas configurações são aplicadas server-side, reduzindo
// a quantidade de parâmetros passados via URL pelo JitsiMeetExternalAPI

var config = {
    // ========================================
    // SEGURANÇA - JWT OBRIGATÓRIO
    // ========================================
    // Hosts e domínios
    hosts: {
        domain: 'meet.jitsi',
        muc: 'muc.meet.jitsi',
        focus: 'focus.meet.jitsi',
    },
    
    // ========================================
    // CONFIGURAÇÕES DE ENTRADA NA SALA
    // ========================================
    // Desabilitar completamente a página inicial
    enableWelcomePage: false,
    enableClosePage: false,
    
    // Prejoin: habilitado para configurar microfone, câmera e nome antes de entrar
    prejoinPageEnabled: true,
    
    // Iniciar com áudio e vídeo ativos (após prejoin)
    startWithAudioMuted: false,
    startWithVideoMuted: false,
    
    // Desabilitar deep linking (apps nativos)
    disableDeepLinking: true,
    
    // Display name
    requireDisplayName: false,
    
    // ========================================
    // IDIOMA E LOCALIZAÇÃO
    // ========================================
    defaultLanguage: 'ptBR',
    
    // ========================================
    // LOBBY / SALA DE ESPERA
    // ========================================
    enableLobbyChat: false,
    hideLobbyButton: true,
    autoKnockLobby: true,
    lobby: {
        autoKnock: true,
        enableChat: false
    },
    
    // ========================================
    // INTERFACE DA CHAMADA
    // ========================================
    hideConferenceSubject: true,
    hideConferenceTimer: false,
    hideParticipantsStats: true,
    
    // ========================================
    // FUNCIONALIDADES
    // ========================================
    // Desabilitar recursos desnecessários
    disableInviteFunctions: true,
    doNotStoreRoom: true,
    disablePolls: true,
    disableReactions: false,
    disableProfile: true,
    disableLocalVideoFlip: false,
    disableAddingBackgroundImages: false,
    
    // Gravação e streaming desabilitados
    fileRecordingsEnabled: false,
    liveStreamingEnabled: false,
    
    // ========================================
    // MODERAÇÃO
    // ========================================
    // Nota: O Jitsi usa o claim "moderator" do JWT para determinar permissões.
    // Moderadores (profissionais de saúde) têm acesso a mutar/expulsar.
    // Não-moderadores (pacientes) não têm esses controles.
    disableRemoteMute: false, // Moderadores podem mutar outros
    remoteVideoMenu: {
        disableKick: false, // Moderadores podem expulsar
        disableGrantModerator: false, // Moderadores podem promover outros
        disablePrivateChat: false
    },
    
    // Notificações
    notifications: [],
    disableJoinLeaveSounds: false,
    
    // ========================================
    // DETECÇÃO DE ÁUDIO
    // ========================================
    enableNoisyMicDetection: true,
    enableNoAudioDetection: true,
    
    // ========================================
    // SEGURANÇA E PRIVACIDADE
    // ========================================
    // Desabilitar requisições a terceiros
    disableThirdPartyRequests: true,
    
    // ========================================
    // PERFORMANCE
    // ========================================
    // Qualidade de vídeo
    constraints: {
        video: {
            height: {
                ideal: 720,
                max: 720,
                min: 240
            }
        }
    },
    
    // Resolução de vídeo
    resolution: 720,
    
    // Desabilitar P2P para forçar uso do servidor
    p2p: {
        enabled: false
    },
    
    // ========================================
    // COMPARTILHAMENTO DE TELA
    // ========================================
    // Habilitar compartilhamento de tela em dispositivos móveis
    disableScreensharingVirtualBackground: false,
    enableScreensharingFilmstripParticipant: true,
    
    // ========================================
    // BRANDING
    // ========================================
    // Remover todos os brandings do Jitsi
    defaultLogoUrl: '',
    defaultWelcomePageLogoUrl: '',
    
    // ========================================
    // INDICADORES E ESTATÍSTICAS
    // ========================================
    connectionIndicators: {
        disabled: false,
        disableDetails: true
    },
    
    // Desabilitar analytics e feedback
    feedbackPercentage: 0
};
