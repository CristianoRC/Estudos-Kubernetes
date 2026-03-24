#!/bin/bash
set -e

CLUSTER_NAME="${1:-kind}"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
CLUSTER_CONFIG="$SCRIPT_DIR/cluster.yaml"

GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log()  { echo -e "${GREEN}[✓]${NC} $1"; }
warn() { echo -e "${YELLOW}[!]${NC} $1"; }
err()  { echo -e "${RED}[✗]${NC} $1"; exit 1; }

wait_for_pods() {
  local namespace=$1
  local label=$2
  local timeout=${3:-120}
  echo -n "    Aguardando pods ($namespace)..."
  if kubectl wait --namespace "$namespace" --for=condition=ready pod --selector="$label" --timeout="${timeout}s" > /dev/null 2>&1; then
    echo -e " ${GREEN}OK${NC}"
  else
    echo -e " ${RED}TIMEOUT${NC}"
    kubectl get pods -n "$namespace"
    err "Pods em $namespace não ficaram prontos em ${timeout}s"
  fi
}

# ------------------------------------------------------------------
echo "=========================================="
echo " Kind Cluster Setup"
echo "=========================================="
echo ""

# 1. Verificar pré-requisitos
for cmd in docker kind kubectl; do
  command -v "$cmd" > /dev/null 2>&1 || err "$cmd não encontrado. Instale antes de continuar."
done
log "Pré-requisitos verificados (docker, kind, kubectl)"

# 2. Criar cluster
if kind get clusters 2>/dev/null | grep -q "^${CLUSTER_NAME}$"; then
  warn "Cluster '$CLUSTER_NAME' já existe — pulando criação"
else
  log "Criando cluster '$CLUSTER_NAME'..."
  kind create cluster --name "$CLUSTER_NAME" --config "$CLUSTER_CONFIG"
  log "Cluster criado"
fi

# 3. Ingress Controller (NGINX)
echo ""
log "Instalando Ingress Controller (NGINX)..."
kubectl apply -f https://raw.githubusercontent.com/kubernetes/ingress-nginx/controller-v1.12.3/deploy/static/provider/kind/deploy.yaml > /dev/null
wait_for_pods "ingress-nginx" "app.kubernetes.io/component=controller" 180
log "Ingress Controller instalado"

# 4. KEDA
echo ""
log "Instalando KEDA..."
kubectl apply --server-side --force-conflicts -f https://github.com/kedacore/keda/releases/download/v2.19.0/keda-2.19.0.yaml > /dev/null
wait_for_pods "keda" "app=keda-operator" 180
log "KEDA instalado"

# 5. Resumo
echo ""
echo "=========================================="
echo -e " ${GREEN}Setup concluído!${NC}"
echo "=========================================="
echo ""
echo "Componentes instalados:"
echo "  - Kind cluster: $CLUSTER_NAME"
echo "  - Ingress Controller NGINX (porta 80/443 no localhost)"
echo "  - KEDA v2.19.0 (event-driven autoscaling)"
echo ""
echo "Próximos passos:"
echo "  kubectl get nodes"
echo "  kubectl get pods -A"
echo ""
