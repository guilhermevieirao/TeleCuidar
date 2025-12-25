#!/bin/bash
# ========================================
# TeleCuidar - Script de Deploy Automatizado
# Ubuntu Server 24.04 LTS
# ========================================
# Uso: sudo bash deploy.sh
# ========================================

set -e  # Parar em caso de erro

# ========================================
# CORES PARA OUTPUT
# ========================================
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
CYAN='\033[0;36m'
NC='\033[0m' # No Color

# ========================================
# FUNÇÕES AUXILIARES
# ========================================
log_info() {
    echo -e "${BLUE}[INFO]${NC} $1"
}

log_success() {
    echo -e "${GREEN}[OK]${NC} $1"
}

log_warning() {
    echo -e "${YELLOW}[AVISO]${NC} $1"
}

log_error() {
    echo -e "${RED}[ERRO]${NC} $1"
}

log_step() {
    echo -e "\n${CYAN}========================================${NC}"
    echo -e "${CYAN}$1${NC}"
    echo -e "${CYAN}========================================${NC}\n"
}

# ========================================
# VERIFICAÇÕES INICIAIS
# ========================================
log_step "VERIFICAÇÕES INICIAIS"

# Verificar se está rodando como root
if [ "$EUID" -ne 0 ]; then
    log_error "Este script deve ser executado como root (sudo)"
    exit 1
fi

# Verificar se é Ubuntu
if ! grep -q "Ubuntu" /etc/os-release 2>/dev/null; then
    log_warning "Este script foi testado apenas no Ubuntu. Continuando mesmo assim..."
fi

# Verificar versão do Ubuntu
UBUNTU_VERSION=$(lsb_release -rs 2>/dev/null || echo "unknown")
log_info "Ubuntu versão: $UBUNTU_VERSION"

# ========================================
# VARIÁVEIS DE CONFIGURAÇÃO
# ========================================
# Diretório de instalação (onde o script está sendo executado)
INSTALL_DIR=$(pwd)
ENV_FILE="$INSTALL_DIR/.env"
ENV_PROD_FILE="$INSTALL_DIR/.env.prod"

# Domínios
DOMAIN_MAIN="telecuidar.com.br"
DOMAIN_WWW="www.telecuidar.com.br"
DOMAIN_ALT="telecuidar.com"
DOMAIN_ALT_WWW="www.telecuidar.com"
DOMAIN_MEET="meet.telecuidar.com.br"

# Diretórios
SSL_DIR="$INSTALL_DIR/docker/ssl"
CERTS_DIR="$INSTALL_DIR/certs"
JITSI_KEYS_DIR="$INSTALL_DIR/jitsi-config/keys"

# Email para Let's Encrypt (será solicitado)
CERTBOT_EMAIL=""

# ========================================
# SOLICITAR INFORMAÇÕES
# ========================================
log_step "CONFIGURAÇÃO INICIAL"

# Verificar se .env.prod existe
if [ ! -f "$ENV_PROD_FILE" ]; then
    log_error "Arquivo .env.prod não encontrado em $INSTALL_DIR"
    log_info "Certifique-se de que está executando o script no diretório do projeto"
    exit 1
fi

# Solicitar email para Let's Encrypt
echo -e "${YELLOW}Digite o email para o Let's Encrypt (para avisos de renovação):${NC}"
read -r CERTBOT_EMAIL

if [ -z "$CERTBOT_EMAIL" ]; then
    log_error "Email é obrigatório para o Let's Encrypt"
    exit 1
fi

# Verificar se o .env já foi configurado
if grep -q "ALTERE_ESTA_CHAVE" "$ENV_PROD_FILE" 2>/dev/null; then
    log_warning "Detectado que o .env.prod ainda contém valores padrão (ALTERE_*)"
    echo -e "${YELLOW}Deseja gerar automaticamente as chaves secretas? (s/n):${NC}"
    read -r GENERATE_KEYS
    
    if [ "$GENERATE_KEYS" = "s" ] || [ "$GENERATE_KEYS" = "S" ]; then
        log_info "Gerando chaves secretas..."
        
        # Gerar chaves
        JWT_SECRET=$(openssl rand -base64 64 | tr -d '\n')
        JITSI_SECRET=$(openssl rand -base64 64 | tr -d '\n')
        JICOFO_AUTH=$(openssl rand -hex 32)
        JICOFO_COMPONENT=$(openssl rand -hex 32)
        JVB_AUTH=$(openssl rand -hex 32)
        
        # Substituir no .env.prod
        sed -i "s|JWT_SECRET_KEY=ALTERE_ESTA_CHAVE_PARA_PRODUCAO_openssl_rand_base64_64|JWT_SECRET_KEY=$JWT_SECRET|g" "$ENV_PROD_FILE"
        sed -i "s|JITSI_APP_SECRET=ALTERE_ESTA_CHAVE_JITSI_openssl_rand_base64_64|JITSI_APP_SECRET=$JITSI_SECRET|g" "$ENV_PROD_FILE"
        sed -i "s|JICOFO_AUTH_PASSWORD=ALTERE_openssl_rand_hex_32_jicofo|JICOFO_AUTH_PASSWORD=$JICOFO_AUTH|g" "$ENV_PROD_FILE"
        sed -i "s|JICOFO_COMPONENT_SECRET=ALTERE_openssl_rand_hex_32_component|JICOFO_COMPONENT_SECRET=$JICOFO_COMPONENT|g" "$ENV_PROD_FILE"
        sed -i "s|JVB_AUTH_PASSWORD=ALTERE_openssl_rand_hex_32_jvb|JVB_AUTH_PASSWORD=$JVB_AUTH|g" "$ENV_PROD_FILE"
        
        log_success "Chaves secretas geradas e configuradas!"
    fi
fi

# Obter IP público
log_info "Obtendo IP público do servidor..."
PUBLIC_IP=$(curl -4 -s ifconfig.me || curl -4 -s icanhazip.com || echo "")

if [ -z "$PUBLIC_IP" ]; then
    log_error "Não foi possível obter o IP público"
    echo -e "${YELLOW}Digite o IP público do servidor manualmente:${NC}"
    read -r PUBLIC_IP
fi

log_info "IP Público: $PUBLIC_IP"

# Atualizar DOCKER_HOST_ADDRESS no .env.prod
if grep -q "DOCKER_HOST_ADDRESS=SEU_IP_PUBLICO_DO_SERVIDOR" "$ENV_PROD_FILE" 2>/dev/null; then
    sed -i "s|DOCKER_HOST_ADDRESS=SEU_IP_PUBLICO_DO_SERVIDOR|DOCKER_HOST_ADDRESS=$PUBLIC_IP|g" "$ENV_PROD_FILE"
    log_success "DOCKER_HOST_ADDRESS atualizado para $PUBLIC_IP"
fi

# ========================================
# ATUALIZAÇÃO DO SISTEMA
# ========================================
log_step "ATUALIZANDO SISTEMA"

apt-get update -y
apt-get upgrade -y
log_success "Sistema atualizado"

# ========================================
# INSTALAÇÃO DE DEPENDÊNCIAS
# ========================================
log_step "INSTALANDO DEPENDÊNCIAS"

# Instalar pacotes necessários
apt-get install -y \
    apt-transport-https \
    ca-certificates \
    curl \
    gnupg \
    lsb-release \
    software-properties-common \
    ufw \
    certbot \
    git \
    jq \
    openssl \
    sqlite3 \
    dos2unix

log_success "Dependências instaladas"

# ========================================
# INSTALAÇÃO DO DOCKER
# ========================================
log_step "INSTALANDO DOCKER"

# Verificar se Docker já está instalado
if command -v docker &> /dev/null; then
    log_info "Docker já está instalado: $(docker --version)"
else
    # Adicionar chave GPG oficial do Docker
    install -m 0755 -d /etc/apt/keyrings
    curl -fsSL https://download.docker.com/linux/ubuntu/gpg | gpg --dearmor -o /etc/apt/keyrings/docker.gpg
    chmod a+r /etc/apt/keyrings/docker.gpg

    # Adicionar repositório Docker
    echo \
        "deb [arch=$(dpkg --print-architecture) signed-by=/etc/apt/keyrings/docker.gpg] https://download.docker.com/linux/ubuntu \
        $(. /etc/os-release && echo "$VERSION_CODENAME") stable" | \
        tee /etc/apt/sources.list.d/docker.list > /dev/null

    # Instalar Docker
    apt-get update -y
    apt-get install -y docker-ce docker-ce-cli containerd.io docker-buildx-plugin docker-compose-plugin

    # Habilitar Docker
    systemctl enable docker
    systemctl start docker

    log_success "Docker instalado: $(docker --version)"
fi

# Verificar Docker Compose
if docker compose version &> /dev/null; then
    log_success "Docker Compose: $(docker compose version)"
else
    log_error "Docker Compose não está disponível"
    exit 1
fi

# ========================================
# CONFIGURAÇÃO DO FIREWALL
# ========================================
log_step "CONFIGURANDO FIREWALL (UFW)"

# Configurar UFW
ufw --force reset
ufw default deny incoming
ufw default allow outgoing

# Permitir SSH
ufw allow 22/tcp comment 'SSH'

# Permitir HTTP/HTTPS
ufw allow 80/tcp comment 'HTTP'
ufw allow 443/tcp comment 'HTTPS'

# Permitir JVB (Jitsi Video Bridge)
ufw allow 10000/udp comment 'Jitsi JVB'

# Habilitar UFW
ufw --force enable

log_success "Firewall configurado"
ufw status verbose

# ========================================
# CRIAR ESTRUTURA DE DIRETÓRIOS
# ========================================
log_step "CRIANDO ESTRUTURA DE DIRETÓRIOS"

# Criar diretórios necessários
mkdir -p "$SSL_DIR"
mkdir -p "$CERTS_DIR"
mkdir -p "$JITSI_KEYS_DIR"
mkdir -p "$INSTALL_DIR/docker/nginx/conf.d"

log_success "Diretórios criados"

# ========================================
# GERAR CERTIFICADOS SSL
# ========================================
log_step "GERANDO CERTIFICADOS SSL (Let's Encrypt)"

# Parar qualquer serviço na porta 80
log_info "Parando serviços que possam estar usando a porta 80..."
systemctl stop nginx 2>/dev/null || true
systemctl stop apache2 2>/dev/null || true
docker compose down 2>/dev/null || true
docker stop $(docker ps -q) 2>/dev/null || true

# Função para gerar certificado
generate_cert() {
    local domains="$1"
    local cert_name="$2"
    
    log_info "Tentando gerar certificado para: $domains"
    
    if certbot certonly --standalone --non-interactive --agree-tos \
        --email "$CERTBOT_EMAIL" \
        $domains \
        --cert-name "$cert_name" 2>&1; then
        return 0
    else
        return 1
    fi
}

# Estratégia de certificados com fallback
SSL_SUCCESS=false
MEET_SSL_SUCCESS=false

# ========================================
# TENTATIVA 1: Certificado único para todos os domínios
# ========================================
log_info "Tentativa 1: Certificado único para todos os domínios..."
if generate_cert "-d $DOMAIN_MAIN -d $DOMAIN_WWW -d $DOMAIN_ALT -d $DOMAIN_ALT_WWW -d $DOMAIN_MEET" "telecuidar-all"; then
    log_success "Certificado único gerado para todos os domínios!"
    
    # Copiar certificados
    cp /etc/letsencrypt/live/telecuidar-all/fullchain.pem "$SSL_DIR/telecuidar.crt"
    cp /etc/letsencrypt/live/telecuidar-all/privkey.pem "$SSL_DIR/telecuidar.key"
    cp /etc/letsencrypt/live/telecuidar-all/fullchain.pem "$SSL_DIR/meet.fullchain.pem"
    cp /etc/letsencrypt/live/telecuidar-all/privkey.pem "$SSL_DIR/meet.privkey.pem"
    cp /etc/letsencrypt/live/telecuidar-all/fullchain.pem "$JITSI_KEYS_DIR/cert.crt"
    cp /etc/letsencrypt/live/telecuidar-all/privkey.pem "$JITSI_KEYS_DIR/cert.key"
    
    SSL_SUCCESS=true
    MEET_SSL_SUCCESS=true
else
    log_warning "Falha na tentativa 1"
fi

# ========================================
# TENTATIVA 2: Certificados separados (.com.br e .com)
# ========================================
if [ "$SSL_SUCCESS" = false ]; then
    log_info "Tentativa 2: Certificado para domínios .com.br..."
    if generate_cert "-d $DOMAIN_MAIN -d $DOMAIN_WWW" "telecuidar-br"; then
        log_success "Certificado .com.br gerado!"
        
        cp /etc/letsencrypt/live/telecuidar-br/fullchain.pem "$SSL_DIR/telecuidar.crt"
        cp /etc/letsencrypt/live/telecuidar-br/privkey.pem "$SSL_DIR/telecuidar.key"
        
        SSL_SUCCESS=true
    else
        log_warning "Falha na tentativa 2"
    fi
fi

# ========================================
# TENTATIVA 3: Apenas www.telecuidar.com.br
# ========================================
if [ "$SSL_SUCCESS" = false ]; then
    log_info "Tentativa 3: Apenas www.telecuidar.com.br..."
    if generate_cert "-d $DOMAIN_WWW" "telecuidar-www"; then
        log_success "Certificado www gerado!"
        
        cp /etc/letsencrypt/live/telecuidar-www/fullchain.pem "$SSL_DIR/telecuidar.crt"
        cp /etc/letsencrypt/live/telecuidar-www/privkey.pem "$SSL_DIR/telecuidar.key"
        
        SSL_SUCCESS=true
    else
        log_warning "Falha na tentativa 3"
    fi
fi

# ========================================
# TENTATIVA 4: Certificado para meet.telecuidar.com.br (separado)
# ========================================
if [ "$MEET_SSL_SUCCESS" = false ]; then
    log_info "Tentativa 4: Certificado para meet.telecuidar.com.br..."
    if generate_cert "-d $DOMAIN_MEET" "telecuidar-meet"; then
        log_success "Certificado meet gerado!"
        
        cp /etc/letsencrypt/live/telecuidar-meet/fullchain.pem "$SSL_DIR/meet.fullchain.pem"
        cp /etc/letsencrypt/live/telecuidar-meet/privkey.pem "$SSL_DIR/meet.privkey.pem"
        cp /etc/letsencrypt/live/telecuidar-meet/fullchain.pem "$JITSI_KEYS_DIR/cert.crt"
        cp /etc/letsencrypt/live/telecuidar-meet/privkey.pem "$JITSI_KEYS_DIR/cert.key"
        
        MEET_SSL_SUCCESS=true
    else
        log_warning "Falha na tentativa 4"
    fi
fi

# Verificar resultados
if [ "$SSL_SUCCESS" = false ]; then
    log_error "Não foi possível gerar certificado SSL para o domínio principal"
    log_info "Verifique se o DNS está configurado corretamente apontando para $PUBLIC_IP"
    log_info "Você pode tentar novamente mais tarde executando:"
    echo "    sudo certbot certonly --standalone -d www.telecuidar.com.br"
    echo ""
    echo -e "${YELLOW}Deseja continuar sem os certificados? (s/n):${NC}"
    read -r CONTINUE_WITHOUT_SSL
    if [ "$CONTINUE_WITHOUT_SSL" != "s" ] && [ "$CONTINUE_WITHOUT_SSL" != "S" ]; then
        exit 1
    fi
else
    log_success "Certificados SSL configurados com sucesso!"
fi

# Verificar e copiar certificados de todas as fontes possíveis
log_info "Garantindo que certificados estão nos locais corretos..."

# Função para copiar certificados de forma segura
copy_certs_from_letsencrypt() {
    local cert_name="$1"
    local cert_dir="/etc/letsencrypt/live/$cert_name"
    
    if [ -d "$cert_dir" ]; then
        log_info "Copiando certificados de $cert_dir"
        
        # Certificados principais (telecuidar.crt/key)
        if [ -f "$cert_dir/fullchain.pem" ]; then
            cp "$cert_dir/fullchain.pem" "$SSL_DIR/telecuidar.crt"
            cp "$cert_dir/privkey.pem" "$SSL_DIR/telecuidar.key"
            
            # Certificados Jitsi (meet.fullchain.pem/meet.privkey.pem)
            cp "$cert_dir/fullchain.pem" "$SSL_DIR/meet.fullchain.pem"
            cp "$cert_dir/privkey.pem" "$SSL_DIR/meet.privkey.pem"
            
            # Certificados para Jitsi Keys
            cp "$cert_dir/fullchain.pem" "$JITSI_KEYS_DIR/cert.crt"
            cp "$cert_dir/privkey.pem" "$JITSI_KEYS_DIR/cert.key"
            
            log_success "Certificados copiados de $cert_name"
            return 0
        fi
    fi
    return 1
}

# Tentar copiar de diferentes nomes de certificado
for cert_name in "telecuidar-all" "telecuidar-br" "telecuidar-www" "telecuidar-meet"; do
    if copy_certs_from_letsencrypt "$cert_name"; then
        break
    fi
done

# Verificar se os certificados foram copiados
if [ -f "$SSL_DIR/telecuidar.crt" ] && [ -f "$SSL_DIR/telecuidar.key" ]; then
    log_success "Certificados SSL prontos em $SSL_DIR"
    ls -la "$SSL_DIR/"
else
    log_warning "Certificados SSL não encontrados em $SSL_DIR"
fi

# Configurar permissões dos certificados
chmod 644 "$SSL_DIR"/*.pem "$SSL_DIR"/*.crt "$SSL_DIR"/*.key 2>/dev/null || true
chmod 644 "$JITSI_KEYS_DIR"/*.crt "$JITSI_KEYS_DIR"/*.key 2>/dev/null || true

# ========================================
# CONFIGURAR RENOVAÇÃO AUTOMÁTICA
# ========================================
log_step "CONFIGURANDO RENOVAÇÃO AUTOMÁTICA DE CERTIFICADOS"

# Criar script de renovação
cat > /usr/local/bin/telecuidar-renew-certs.sh << 'RENEW_SCRIPT'
#!/bin/bash
# Script de renovação de certificados TeleCuidar

INSTALL_DIR="PLACEHOLDER_INSTALL_DIR"
SSL_DIR="$INSTALL_DIR/docker/ssl"
JITSI_KEYS_DIR="$INSTALL_DIR/jitsi-config/keys"

# Renovar certificados
certbot renew --quiet

# Copiar certificados atualizados
for cert_dir in /etc/letsencrypt/live/telecuidar-*; do
    if [ -d "$cert_dir" ]; then
        cert_name=$(basename "$cert_dir")
        
        if [[ "$cert_name" == *"meet"* ]]; then
            cp "$cert_dir/fullchain.pem" "$SSL_DIR/meet.fullchain.pem"
            cp "$cert_dir/privkey.pem" "$SSL_DIR/meet.privkey.pem"
            cp "$cert_dir/fullchain.pem" "$JITSI_KEYS_DIR/cert.crt"
            cp "$cert_dir/privkey.pem" "$JITSI_KEYS_DIR/cert.key"
        else
            cp "$cert_dir/fullchain.pem" "$SSL_DIR/telecuidar.crt"
            cp "$cert_dir/privkey.pem" "$SSL_DIR/telecuidar.key"
        fi
    fi
done

# Recarregar nginx
cd "$INSTALL_DIR"
docker compose exec -T nginx nginx -s reload 2>/dev/null || true
RENEW_SCRIPT

# Substituir placeholder com o diretório real
sed -i "s|PLACEHOLDER_INSTALL_DIR|$INSTALL_DIR|g" /usr/local/bin/telecuidar-renew-certs.sh
chmod +x /usr/local/bin/telecuidar-renew-certs.sh

# Adicionar ao crontab (todo dia às 3h da manhã)
(crontab -l 2>/dev/null | grep -v "telecuidar-renew-certs"; echo "0 3 * * * /usr/local/bin/telecuidar-renew-certs.sh >> /var/log/telecuidar-certs.log 2>&1") | crontab -

log_success "Renovação automática configurada (diariamente às 3h)"

# ========================================
# COPIAR .ENV.PROD PARA .ENV
# ========================================
log_step "CONFIGURANDO AMBIENTE"

cp "$ENV_PROD_FILE" "$ENV_FILE"
log_success "Arquivo .env criado a partir de .env.prod"

# ========================================
# CORRIGIR ENCODING DO .ENV (Windows → Linux)
# ========================================
log_info "Corrigindo encoding do arquivo .env (caso venha do Windows)..."

# Usar dos2unix para converter line endings e remover problemas de encoding
if command -v dos2unix &> /dev/null; then
    dos2unix "$ENV_FILE" 2>/dev/null
    log_success "Line endings convertidos com dos2unix"
else
    # Fallback: conversão manual
    # Detectar e converter de UTF-16LE (Windows) para UTF-8 (Linux)
    if file "$ENV_FILE" | grep -q "UTF-16"; then
        log_warning "Arquivo .env está em UTF-16 (Windows), convertendo para UTF-8..."
        iconv -f UTF-16LE -t UTF-8 "$ENV_FILE" > "$ENV_FILE.tmp" && mv "$ENV_FILE.tmp" "$ENV_FILE"
        log_success "Encoding convertido para UTF-8"
    fi

    # Remover BOM (Byte Order Mark) se existir
    if head -c 3 "$ENV_FILE" | grep -q $'\xEF\xBB\xBF'; then
        log_warning "Removendo BOM do arquivo .env..."
        sed -i '1s/^\xEF\xBB\xBF//' "$ENV_FILE"
    fi

    # Converter line endings de Windows (CRLF) para Unix (LF)
    sed -i 's/\r$//' "$ENV_FILE"
fi

log_success "Arquivo .env configurado corretamente"

# Converter também outros arquivos importantes
for config_file in docker-compose.yml docker-compose.*.yml; do
    if [ -f "$INSTALL_DIR/$config_file" ]; then
        dos2unix "$INSTALL_DIR/$config_file" 2>/dev/null || sed -i 's/\r$//' "$INSTALL_DIR/$config_file"
    fi
done

# ========================================
# PREPARAR CONFIGURAÇÃO NGINX PARA PRODUÇÃO
# ========================================
log_info "Preparando configuração Nginx para produção..."

# Remover arquivos de configuração de desenvolvimento (*.dev.conf)
# Esses arquivos são usados apenas para desenvolvimento local
for dev_conf in "$INSTALL_DIR/docker/nginx/conf.d"/*.dev.conf; do
    if [ -f "$dev_conf" ]; then
        rm -f "$dev_conf"
        log_info "Removido: $(basename $dev_conf) (config de desenvolvimento)"
    fi
done

# Também remover configs antigas de dev (caso existam)
rm -f "$INSTALL_DIR/docker/nginx/conf.d/default.conf" 2>/dev/null
rm -f "$INSTALL_DIR/docker/nginx/conf.d/jitsi-dev.conf" 2>/dev/null

log_success "Configuração Nginx preparada para produção"

# ========================================
# CRIAR SCRIPT DE BACKUP
# ========================================
log_step "CONFIGURANDO BACKUP AUTOMÁTICO"

mkdir -p "$INSTALL_DIR/backups"

cat > /usr/local/bin/telecuidar-backup.sh << 'BACKUP_SCRIPT'
#!/bin/bash
# Script de backup TeleCuidar

INSTALL_DIR="PLACEHOLDER_INSTALL_DIR"
BACKUP_DIR="$INSTALL_DIR/backups"
DATE=$(date +%Y%m%d_%H%M%S)

# Criar diretório de backup se não existir
mkdir -p "$BACKUP_DIR"

# Backup do banco de dados SQLite
docker exec telecuidar-backend sqlite3 /app/data/telecuidar.db ".backup '/app/data/backup-$DATE.db'" 2>/dev/null
docker cp telecuidar-backend:/app/data/backup-$DATE.db "$BACKUP_DIR/telecuidar-$DATE.db" 2>/dev/null

# Backup do .env
cp "$INSTALL_DIR/.env" "$BACKUP_DIR/env-$DATE.backup"

# Limpar backups antigos (manter últimos 30 dias)
find "$BACKUP_DIR" -name "*.db" -mtime +30 -delete
find "$BACKUP_DIR" -name "*.backup" -mtime +30 -delete

echo "[$(date)] Backup concluído: telecuidar-$DATE.db"
BACKUP_SCRIPT

sed -i "s|PLACEHOLDER_INSTALL_DIR|$INSTALL_DIR|g" /usr/local/bin/telecuidar-backup.sh
chmod +x /usr/local/bin/telecuidar-backup.sh

# Adicionar backup diário ao crontab (às 2h da manhã)
(crontab -l 2>/dev/null | grep -v "telecuidar-backup"; echo "0 2 * * * /usr/local/bin/telecuidar-backup.sh >> /var/log/telecuidar-backup.log 2>&1") | crontab -

log_success "Backup automático configurado (diariamente às 2h)"

# ========================================
# INSTALAR FAIL2BAN
# ========================================
log_step "INSTALANDO FAIL2BAN (Proteção contra ataques)"

apt-get install -y fail2ban

# Configurar fail2ban básico
cat > /etc/fail2ban/jail.local << 'FAIL2BAN'
[DEFAULT]
bantime = 1h
findtime = 10m
maxretry = 5

[sshd]
enabled = true
port = ssh
filter = sshd
logpath = /var/log/auth.log
maxretry = 3
FAIL2BAN

systemctl enable fail2ban
systemctl restart fail2ban

log_success "Fail2ban instalado e configurado"

# ========================================
# BUILD E INICIAR CONTAINERS
# ========================================
log_step "CONSTRUINDO E INICIANDO APLICAÇÃO"

cd "$INSTALL_DIR"

# Limpar containers e volumes antigos (se existirem)
log_info "Limpando containers antigos..."
docker compose down -v 2>/dev/null || true

# Build das imagens
log_info "Construindo imagens Docker (isso pode demorar alguns minutos)..."
docker compose build --no-cache

# Iniciar containers
log_info "Iniciando containers..."
docker compose up -d

# Aguardar containers iniciarem
log_info "Aguardando containers iniciarem..."
sleep 30

# Verificar status
log_info "Verificando status dos containers..."
docker compose ps

# ========================================
# VERIFICAÇÃO FINAL
# ========================================
log_step "VERIFICAÇÃO FINAL"

# Verificar se todos os containers estão rodando
CONTAINERS_RUNNING=$(docker compose ps --format json 2>/dev/null | jq -r '.State' | grep -c "running" || echo "0")
CONTAINERS_TOTAL=$(docker compose ps --format json 2>/dev/null | jq -r '.State' | wc -l || echo "0")

if [ "$CONTAINERS_RUNNING" -gt 0 ]; then
    log_success "Containers rodando: $CONTAINERS_RUNNING"
else
    log_warning "Verificando containers de outra forma..."
    docker compose ps
fi

# Testar endpoints
log_info "Testando endpoints..."

# Testar backend
if curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/health 2>/dev/null | grep -q "200"; then
    log_success "Backend: OK"
else
    log_warning "Backend: Verificar logs com 'docker compose logs backend'"
fi

# Testar frontend
if curl -s -o /dev/null -w "%{http_code}" http://localhost:4000 2>/dev/null | grep -q "200"; then
    log_success "Frontend: OK"
else
    log_warning "Frontend: Verificar logs com 'docker compose logs frontend'"
fi

# Testar HTTPS (se certificados foram gerados)
if [ "$SSL_SUCCESS" = true ]; then
    sleep 5
    if curl -s -o /dev/null -w "%{http_code}" -k https://localhost 2>/dev/null | grep -q "200\|301\|302"; then
        log_success "HTTPS: OK"
    else
        log_warning "HTTPS: Verificar logs com 'docker compose logs nginx'"
    fi
fi

# ========================================
# RESUMO FINAL
# ========================================
log_step "DEPLOY CONCLUÍDO!"

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}    TeleCuidar - Deploy Finalizado!${NC}"
echo -e "${GREEN}========================================${NC}"
echo ""
echo -e "${CYAN}URLs de Acesso:${NC}"
echo -e "  • Frontend: ${GREEN}https://www.telecuidar.com.br${NC}"
echo -e "  • API:      ${GREEN}https://www.telecuidar.com.br/api${NC}"
echo -e "  • Jitsi:    ${GREEN}https://meet.telecuidar.com.br${NC}"
echo ""
echo -e "${CYAN}Credenciais Admin Padrão:${NC}"
echo -e "  • Email:    $(grep SEED_ADMIN_EMAIL "$ENV_FILE" | cut -d'=' -f2)"
echo -e "  • Senha:    $(grep SEED_ADMIN_PASSWORD "$ENV_FILE" | cut -d'=' -f2)"
echo -e "  ${YELLOW}⚠️  ALTERE ESTAS CREDENCIAIS IMEDIATAMENTE!${NC}"
echo ""
echo -e "${CYAN}Comandos Úteis:${NC}"
echo -e "  • Ver logs:        ${BLUE}docker compose logs -f${NC}"
echo -e "  • Reiniciar:       ${BLUE}docker compose restart${NC}"
echo -e "  • Parar:           ${BLUE}docker compose down${NC}"
echo -e "  • Status:          ${BLUE}docker compose ps${NC}"
echo -e "  • Backup manual:   ${BLUE}/usr/local/bin/telecuidar-backup.sh${NC}"
echo ""
echo -e "${CYAN}Arquivos Importantes:${NC}"
echo -e "  • Configuração:    ${BLUE}$ENV_FILE${NC}"
echo -e "  • Certificados:    ${BLUE}$SSL_DIR/${NC}"
echo -e "  • Backups:         ${BLUE}$INSTALL_DIR/backups/${NC}"
echo -e "  • Cert. Digital:   ${BLUE}$CERTS_DIR/ (coloque seu .pfx aqui)${NC}"
echo ""
echo -e "${CYAN}Tarefas Agendadas:${NC}"
echo -e "  • Backup:          Diariamente às 02:00"
echo -e "  • Renovar SSL:     Diariamente às 03:00"
echo ""

if [ "$SSL_SUCCESS" = false ]; then
    echo -e "${YELLOW}⚠️  ATENÇÃO: Certificados SSL não foram gerados!${NC}"
    echo -e "${YELLOW}   Verifique se o DNS está configurado e execute:${NC}"
    echo -e "${YELLOW}   sudo certbot certonly --standalone -d www.telecuidar.com.br${NC}"
    echo ""
fi

if grep -q "ALTERE_PARA" "$ENV_FILE" 2>/dev/null; then
    echo -e "${YELLOW}⚠️  ATENÇÃO: Ainda há configurações pendentes no .env:${NC}"
    grep "ALTERE_PARA" "$ENV_FILE" | head -5
    echo -e "${YELLOW}   Edite o arquivo e reinicie: docker compose restart${NC}"
    echo ""
fi

echo -e "${GREEN}========================================${NC}"
echo -e "${GREEN}    Deploy concluído com sucesso!${NC}"
echo -e "${GREEN}========================================${NC}"
