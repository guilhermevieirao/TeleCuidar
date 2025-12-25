# Configurações Nginx

## Estrutura de Arquivos

| Arquivo | Ambiente | Descrição |
|---------|----------|-----------|
| `production.conf` | **Produção** | Configuração principal HTTPS para www.telecuidar.com.br |
| `jitsi.conf` | **Produção** | Proxy para Jitsi Meet (meet.telecuidar.com.br) |
| `localhost.dev.conf` | Desenvolvimento | Configuração HTTP para localhost:4200 |
| `jitsi.dev.conf` | Desenvolvimento | Proxy Jitsi para desenvolvimento local |
| `ssl.conf.example` | Exemplo | Modelo de configuração SSL |

## Importante

### Arquivos `.dev.conf`
- São usados **apenas** para desenvolvimento local
- **Serão removidos automaticamente** pelo script `deploy.sh` durante o deploy
- Não devem conter configurações de produção

### Deploy de Produção
O script `deploy.sh` executa automaticamente:
1. Remove todos os arquivos `*.dev.conf`
2. Mantém apenas `production.conf` e `jitsi.conf`
3. Copia certificados SSL para `docker/ssl/`

### Certificados SSL
Em produção, os certificados devem estar em `docker/ssl/`:
- `telecuidar.crt` - Certificado principal (fullchain)
- `telecuidar.key` - Chave privada
- `meet.fullchain.pem` - Certificado Jitsi
- `meet.privkey.pem` - Chave privada Jitsi
