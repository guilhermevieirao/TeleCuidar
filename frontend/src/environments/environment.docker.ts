// ========================================
// Este arquivo é gerado automaticamente pelo script generate-env.js
// NÃO EDITE MANUALMENTE - Edite o arquivo .env na raiz do projeto
// ========================================
// Ambiente: Docker Development (docker-compose.dev.yml)

// Determina dinamicamente a URL da API baseado no host atual
const getApiUrl = () => {
  if (typeof window !== 'undefined') {
    const protocol = window.location.protocol;
    const host = window.location.hostname;
    // Em Docker dev, API está no mesmo host via Nginx proxy
    return `${protocol}//${host}/api`;
  }
  return '/api';
};

// Determina dinamicamente o domínio do Jitsi Self-Hosted
const getJitsiDomain = () => {
  if (typeof window !== 'undefined') {
    const host = window.location.hostname;
    // Em Docker dev local, Jitsi está na porta 8443
    return `${host}:8443`;
  }
  return 'localhost:8443';
};

export const environment = {
  production: false,
  apiUrl: getApiUrl(),
  
  // Configurações do Jitsi Meet Self-Hosted
  jitsi: {
    domain: getJitsiDomain(),
    enabled: true,
    requiresAuth: true,
    appId: 'telecuidar'
  }
};
