#!/bin/bash
set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_DIR="$SCRIPT_DIR/.."
CLUSTER_NAME="${1:-kind}"
IMAGE_NAME="rabbitmq-consumer:latest"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log()  { echo -e "${GREEN}[✓]${NC} $1"; }
warn() { echo -e "${YELLOW}[!]${NC} $1"; }
err()  { echo -e "${RED}[✗]${NC} $1"; exit 1; }

echo "=========================================="
echo " RabbitMQ + KEDA Setup"
echo "=========================================="
echo ""

# 1. Verificar .env
if [ ! -f "$SCRIPT_DIR/.env" ]; then
  warn "Arquivo .env não encontrado — copiando de .env.example"
  cp "$SCRIPT_DIR/.env.example" "$SCRIPT_DIR/.env"
  log ".env criado com valores padrão"
fi

# 2. Build da imagem do consumer
log "Building imagem '$IMAGE_NAME'..."
docker build -t "$IMAGE_NAME" "$PROJECT_DIR"
log "Imagem criada"

# 3. Carregar imagem no Kind
log "Carregando imagem no cluster Kind '$CLUSTER_NAME'..."
kind load docker-image "$IMAGE_NAME" --name "$CLUSTER_NAME"
log "Imagem carregada no cluster"

# 4. Aplicar manifests
log "Aplicando manifests via Kustomize..."
kubectl apply -k "$SCRIPT_DIR"
log "Manifests aplicados"

# 5. Aguardar RabbitMQ ficar pronto
echo -n "    Aguardando RabbitMQ..."
if kubectl wait --namespace event-processor --for=condition=ready pod \
  --selector=app.kubernetes.io/name=rabbitmq --timeout=120s > /dev/null 2>&1; then

  echo -e " ${GREEN}OK${NC}"
else
  echo -e " ${RED}TIMEOUT${NC}"
  kubectl get pods -n event-processor
  err "RabbitMQ não ficou pronto em 120s"
fi

# 6. Resumo
echo ""
echo "=========================================="
echo -e " ${GREEN}Setup concluído!${NC}"
echo "=========================================="
echo ""
echo "Recursos criados no namespace 'event-processor':"
echo "  - RabbitMQ (localhost:5672 | management: http://localhost:15672)"
echo "  - Consumer Deployment (escalado pelo KEDA)"
echo "  - ScaledObject (queue: order-processor, threshold: 5 msgs/replica)"
echo ""
echo "Comandos úteis:"
echo "  kubectl get pods -n event-processor"
echo "  kubectl logs -n event-processor -l app.kubernetes.io/name=rabbitmq-consumer -f"
echo ""
echo "Enviar eventos:"
echo "  cd keda/EventEmitter && node src/index.js --broker rabbitmq"
echo ""
