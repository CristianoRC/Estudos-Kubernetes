# Deployments

## HPA - Horizontal Pod Autoscaler

### Pré-requisito: Metrics Server

O HPA depende do Metrics Server para ler CPU/memória dos pods. Instale com:

```bash
kubectl apply -f https://github.com/kubernetes-sigs/metrics-server/releases/latest/download/components.yaml
```

**Em clusters locais (Kind, Minikube)** o Metrics Server falha por padrão por causa de certificados TLS. Corrija com:

```bash
kubectl patch deployment metrics-server -n kube-system \
  --type='json' \
  -p='[{"op":"add","path":"/spec/template/spec/containers/0/args/-","value":"--kubelet-insecure-tls"}]'
```

Verifique se está funcionando:

```bash
kubectl get pods -n kube-system | grep metrics
kubectl top nodes  # Deve retornar métricas, não erro
```

---

### Testando o HPA ([deployment-hpa.yaml](./deployment-hpa.yaml))

**1. Aplicar**
```bash
kubectl apply -f deployment-hpa.yaml
```

**2. Confirmar que o HPA está lendo métricas**
```bash
kubectl get hpa hpa-demo
# A coluna TARGETS deve mostrar algo como "10%/50%" — não "<unknown>/50%"
```

> Se aparecer `<unknown>`, o Metrics Server não está funcionando. Veja a seção acima.

**3. Gerar carga (em outro terminal)**
```bash
kubectl run -it --rm load-generator --image=busybox --restart=Never -- \
  sh -c "while true; do wget -q -O- http://hpa-demo; done"
```

**4. Acompanhar o autoscaling**
```bash
kubectl get hpa hpa-demo -w
```

Em ~1 minuto a CPU vai subir, o HPA vai criar novos pods até o limite de `maxReplicas`.

**5. Parar a carga e ver desescalar**

Pressione `Ctrl+C` no load-generator. O HPA aguarda ~5 minutos antes de remover os pods (comportamento padrão para evitar flapping).

**6. Limpar**
```bash
kubectl delete -f deployment-hpa.yaml
```
