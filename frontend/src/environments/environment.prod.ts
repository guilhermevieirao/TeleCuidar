// ========================================
// Este arquivo é gerado automaticamente pelo script generate-env.js
// NÃO EDITE MANUALMENTE - Edite o arquivo .env na raiz do projeto
// ========================================
// Ambiente: Produção (docker-compose.yml)

// Determina dinamicamente a URL da API baseado no host atual
const getApiUrl = () => {
  if (typeof window !== 'undefined') {
    const protocol = window.location.protocol;
    const host = window.location.hostname;
    // Em produção, API está no mesmo host via Nginx proxy
    return `${protocol}//${host}/api`;
  }
  return '/api';
};

// Determina dinamicamente o domínio do Jitsi Self-Hosted
const getJitsiDomain = () => {
  if (typeof window !== 'undefined') {
    const host = window.location.hostname;
    // Em produção, Jitsi está em subdomínio meet.* 
    // O backend retorna a configuração correta via /api/jitsi/config
    return `meet.${host}`;
  }
  return 'meet.telecuidar.com.br';
};

export const environment = {
  production: true,
  apiUrl: getApiUrl(),
  
  // Configurações do Jitsi Meet Self-Hosted
  jitsi: {
    domain: getJitsiDomain(),
    enabled: true,
    requiresAuth: true,
    appId: 'telecuidar'
  }
};
