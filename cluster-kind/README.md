# Kind - Clusters Kubernetes para desenvolvimento local

<img src="https://kind.sigs.k8s.io/logo/logo.png" width="200"/>

O Kind (Kubernetes In Docker) é uma ferramenta que permite criar clusters Kubernetes localmente usando containers Docker.


## Pré-requisitos

- [Docker](https://docs.docker.com/get-docker/) instalado e rodando
- [Kind](https://kind.sigs.k8s.io/docs/user/quick-start/#installation) instalado
- [kubectl](https://kubernetes.io/docs/tasks/tools/install-kubectl/) instalado

## Comandos básicos do Kind

### Criar um cluster simples

```bash
kind create cluster
```

### Criar um cluster com nome específico

```bash
kind create cluster --name meu-cluster
```

### Criar um cluster com múltiplos nós

1. Crie um arquivo `config.yaml` ou use nosso arquivo de configuração base [cluster.yaml](./cluster.yaml) que já contém uma configuração com 1 control-plane e 3 workers:

```yaml
kind: Cluster
apiVersion: kind.x-k8s.io/v1alpha4
nodes:
- role: control-plane
- role: worker
- role: worker
```

2. Crie o cluster:

```bash
kind create cluster --config cluster.yaml
```

### Listar todos os clusters

```bash
kind get clusters
```

### Deletar um cluster

```bash
kind delete cluster --name meu-cluster
```

### Deletar o cluster padrão

```bash
kind delete cluster
```

### Compartilhar imagens locais com o cluster

```bash
# Construa sua imagem localmente
docker build -t minha-imagem:latest .

# Carregue a imagem no cluster Kind
kind load docker-image minha-imagem:latest
```

## Recursos adicionais

- [Documentação oficial do Kind](https://kind.sigs.k8s.io/)
- [Exemplos de configurações avançadas](https://kind.sigs.k8s.io/docs/user/configuration/)
