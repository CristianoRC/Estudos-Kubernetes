# Como Testar Serviços Kubernetes

## ClusterIP

1. **Aplicar o serviço**
   ```bash
   kubectl apply -f cluster-ip.yaml
   ```

2. **Verificar status**
   ```bash
   kubectl get svc
   kubectl get pods
   ```

3. **Testar acesso (escolha um método)**

   - **Port-Forward:**
     ```bash
     kubectl port-forward service/meu-servico-clusterip 8080:80
     # Acesse: http://localhost:8080
     ```

   - **De dentro do cluster:**
     ```bash
     # Criar pod temporário
     kubectl run temp --rm -it --image=curlimages/curl -- sh
     
     # Dentro do pod, executar:
     curl meu-servico-clusterip
     ```

4. **Ver balanceamento de carga**
   ```bash
   # Executar várias vezes para ver diferentes pods respondendo
   for i in {1..5}; do curl meu-servico-clusterip; echo; done 
   ```

5. **Limpar recursos**
   ```bash
   kubectl delete -f cluster-ip.yaml
   ```
