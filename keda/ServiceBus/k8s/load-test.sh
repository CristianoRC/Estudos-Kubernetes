#!/bin/bash

URL="${1:-http://localhost/event-processor/events/publish}"
CALLS=20        # 20 chamadas x 50 eventos cada = 1000 mensagens
CONCURRENT=5

echo "Disparando $CALLS chamadas (50 eventos cada) = $((CALLS * 50)) mensagens no Service Bus"
echo "Concorrência: $CONCURRENT requests simultâneos"
echo "URL: $URL"
echo "---"

sent=0
failed=0

for i in $(seq 1 $CONCURRENT $CALLS); do
  pids=()
  for j in $(seq 0 $((CONCURRENT - 1))); do
    idx=$((i + j))
    [ "$idx" -gt "$CALLS" ] && break

    (
      status=$(curl -s -o /dev/null -w "%{http_code}" -X POST "$URL")
      if [ "$status" = "200" ]; then
        echo "  [$idx/$CALLS] OK (HTTP $status)"
      else
        echo "  [$idx/$CALLS] FALHOU (HTTP $status)"
      fi
    ) &
    pids+=($!)
  done

  for pid in "${pids[@]}"; do
    wait "$pid"
  done
done

echo "---"
echo "Feito! Verifique o scaling:"
echo "  kubectl get pods -n event-processor -w"
echo "  kubectl get hpa -n event-processor"
