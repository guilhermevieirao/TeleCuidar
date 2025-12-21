/**
 * Script para gerar os arquivos environment.ts a partir do .env
 * 
 * Este script lê o arquivo .env na raiz do projeto e gera os arquivos
 * de environment do Angular automaticamente.
 * 
 * Uso: node scripts/generate-env.js
 */

const fs = require('fs');
const path = require('path');

// Caminho para o .env na raiz do projeto (um nível acima do frontend)
const envPath = path.resolve(__dirname, '..', '..', '.env');
const envExamplePath = path.resolve(__dirname, '..', '..', '.env.example');

// Destino dos arquivos de environment
const environmentDir = path.resolve(__dirname, '..', 'src', 'environments');

/**
 * Lê e parseia um arquivo .env
 */
function parseEnvFile(filePath) {
  if (!fs.existsSync(filePath)) {
    console.warn(`⚠️  Arquivo não encontrado: ${filePath}`);
    return {};
  }

  const content = fs.readFileSync(filePath, 'utf-8');
  const env = {};

  content.split('\n').forEach(line => {
    // Ignorar comentários e linhas vazias
    const trimmedLine = line.trim();
    if (!trimmedLine || trimmedLine.startsWith('#')) {
      return;
    }

    // Encontrar o primeiro = e dividir
    const equalIndex = trimmedLine.indexOf('=');
    if (equalIndex === -1) return;

    const key = trimmedLine.substring(0, equalIndex).trim();
    let value = trimmedLine.substring(equalIndex + 1).trim();

    // Remover aspas se existirem
    if ((value.startsWith('"') && value.endsWith('"')) ||
        (value.startsWith("'") && value.endsWith("'"))) {
      value = value.slice(1, -1);
    }

    env[key] = value;
  });

  return env;
}

/**
 * Gera o conteúdo do arquivo environment.ts para desenvolvimento
 */
function generateDevEnvironment(env) {
  const backendPort = env.BACKEND_PORT || '5239';
  const jitsiEnabled = env.JITSI_ENABLED === 'true';
  const jitsiRequiresAuth = env.JITSI_REQUIRES_AUTH !== 'false';
  const jitsiAppId = env.JITSI_APP_ID || 'telecuidar';
  
  return `// ========================================
// Este arquivo é gerado automaticamente pelo script generate-env.js
// NÃO EDITE MANUALMENTE - Edite o arquivo .env na raiz do projeto
// ========================================

// Determina dinamicamente a URL da API baseado no host atual
const getApiUrl = () => {
  if (typeof window !== 'undefined') {
    const host = window.location.hostname;
    // Se acessando via IP ou não-localhost, usar o mesmo host para API
    if (host !== 'localhost' && host !== '127.0.0.1') {
      return \`http://\${host}:${backendPort}/api\`;
    }
  }
  return 'http://localhost:${backendPort}/api';
};

// Determina dinamicamente o domínio do Jitsi Self-Hosted
const getJitsiDomain = () => {
  if (typeof window !== 'undefined') {
    const host = window.location.hostname;
    // Em desenvolvimento local, Jitsi roda em localhost:8443
    if (host === 'localhost' || host === '127.0.0.1') {
      return 'localhost:8443';
    }
    // Se acessando via IP da rede, usar mesmo IP para Jitsi
    return \`\${host}:8443\`;
  }
  return 'localhost:8443';
};

export const environment = {
  production: false,
  apiUrl: getApiUrl(),
  
  // Configurações do Jitsi Meet Self-Hosted
  jitsi: {
    domain: getJitsiDomain(),
    enabled: ${jitsiEnabled},
    requiresAuth: ${jitsiRequiresAuth},
    appId: '${jitsiAppId}'
  }
};
`;
}

/**
 * Gera o conteúdo do arquivo environment.ts para Docker (dev)
 */
function generateDockerEnvironment(env) {
  const jitsiEnabled = env.JITSI_ENABLED === 'true';
  const jitsiRequiresAuth = env.JITSI_REQUIRES_AUTH !== 'false';
  const jitsiAppId = env.JITSI_APP_ID || 'telecuidar';
  
  return `// ========================================
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
    return \`\${protocol}//\${host}/api\`;
  }
  return '/api';
};

// Determina dinamicamente o domínio do Jitsi Self-Hosted
const getJitsiDomain = () => {
  if (typeof window !== 'undefined') {
    const host = window.location.hostname;
    // Em Docker dev local, Jitsi está na porta 8443
    return \`\${host}:8443\`;
  }
  return 'localhost:8443';
};

export const environment = {
  production: false,
  apiUrl: getApiUrl(),
  
  // Configurações do Jitsi Meet Self-Hosted
  jitsi: {
    domain: getJitsiDomain(),
    enabled: ${jitsiEnabled},
    requiresAuth: ${jitsiRequiresAuth},
    appId: '${jitsiAppId}'
  }
};
`;
}

/**
 * Gera o conteúdo do arquivo environment.ts para produção
 */
function generateProdEnvironment(env) {
  const jitsiEnabled = env.JITSI_ENABLED === 'true';
  const jitsiRequiresAuth = env.JITSI_REQUIRES_AUTH !== 'false';
  const jitsiAppId = env.JITSI_APP_ID || 'telecuidar';
  
  return `// ========================================
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
    return \`\${protocol}//\${host}/api\`;
  }
  return '/api';
};

// Determina dinamicamente o domínio do Jitsi Self-Hosted
const getJitsiDomain = () => {
  if (typeof window !== 'undefined') {
    const host = window.location.hostname;
    // Em produção, Jitsi está em subdomínio meet.* 
    // O backend retorna a configuração correta via /api/jitsi/config
    return \`meet.\${host}\`;
  }
  return 'meet.telecuidar.com.br';
};

export const environment = {
  production: true,
  apiUrl: getApiUrl(),
  
  // Configurações do Jitsi Meet Self-Hosted
  jitsi: {
    domain: getJitsiDomain(),
    enabled: ${jitsiEnabled},
    requiresAuth: ${jitsiRequiresAuth},
    appId: '${jitsiAppId}'
  }
};
`;
}

/**
 * Função principal
 */
function main() {
  console.log('🔧 Gerando arquivos de environment a partir do .env...\n');

  // Tentar ler .env, se não existir usar .env.example
  let env = parseEnvFile(envPath);
  
  if (Object.keys(env).length === 0) {
    console.log('📋 Arquivo .env não encontrado, usando .env.example como base...');
    env = parseEnvFile(envExamplePath);
  }

  if (Object.keys(env).length === 0) {
    console.error('❌ Nenhum arquivo de configuração encontrado (.env ou .env.example)');
    process.exit(1);
  }

  // Garantir que o diretório existe
  if (!fs.existsSync(environmentDir)) {
    fs.mkdirSync(environmentDir, { recursive: true });
  }

  // Gerar environment.ts (desenvolvimento - padrão)
  const devContent = generateDevEnvironment(env);
  const devPath = path.join(environmentDir, 'environment.ts');
  fs.writeFileSync(devPath, devContent);
  console.log(`✅ Gerado: ${devPath}`);

  // Gerar environment.development.ts (cópia do dev)
  const devEnvPath = path.join(environmentDir, 'environment.development.ts');
  fs.writeFileSync(devEnvPath, devContent);
  console.log(`✅ Gerado: ${devEnvPath}`);

  // Gerar environment.docker.ts (Docker development)
  const dockerContent = generateDockerEnvironment(env);
  const dockerPath = path.join(environmentDir, 'environment.docker.ts');
  fs.writeFileSync(dockerPath, dockerContent);
  console.log(`✅ Gerado: ${dockerPath}`);

  // Gerar environment.prod.ts (produção)
  const prodContent = generateProdEnvironment(env);
  const prodPath = path.join(environmentDir, 'environment.prod.ts');
  fs.writeFileSync(prodPath, prodContent);
  console.log(`✅ Gerado: ${prodPath}`);

  console.log('\n🎉 Arquivos de environment gerados com sucesso!');
  console.log('📝 Para alterar as configurações, edite o arquivo .env na raiz do projeto.');
  console.log('');
  console.log('📋 Configurações detectadas:');
  console.log(`   • BACKEND_PORT: ${env.BACKEND_PORT || '5239'}`);
  console.log(`   • JITSI_ENABLED: ${env.JITSI_ENABLED || 'true'}`);
  console.log(`   • JITSI_APP_ID: ${env.JITSI_APP_ID || 'telecuidar'}`);
}

main();
