# Manual Deployment në Azure Kubernetes Service

## Parakushtet
- ✅ AKS Cluster: bookrating-aks
- ✅ ACR: bookratingacr
- ✅ Docker image: Duhet të build dhe push në ACR

---

## HAPI 1: Build dhe Push Docker Image në ACR

### 1.1 Login në ACR
```bash
az acr login --name bookratingacr
```

### 1.2 Build Image
```bash
# Nga root directory e projektit
docker build -t bookratingacr.azurecr.io/bookrating-api:latest -f Dockerfile .
```

### 1.3 Push Image në ACR
```bash
docker push bookratingacr.azurecr.io/bookrating-api:latest
```

### 1.4 Verifiko që Image u Push
```bash
az acr repository list --name bookratingacr --output table
az acr repository show-tags --name bookratingacr --repository bookrating-api --output table
```

---

## HAPI 2: Connect në AKS Cluster

```bash
# Get credentials për kubectl
az aks get-credentials --resource-group bookrating-rg --name bookrating-aks --overwrite-existing

# Verifiko connection
kubectl get nodes
```

---

## HAPI 3: Deploy në Kubernetes

### 3.1 Krijo Namespace
```bash
kubectl apply -f .azure/manifests/namespace.yaml
```

### 3.2 Deploy Application
```bash
kubectl apply -f .azure/manifests/deployment.yaml
```

### 3.3 Deploy Service
```bash
kubectl apply -f .azure/manifests/service.yaml
```

---

## HAPI 4: Verifiko Deployment

### 4.1 Shiko Pods
```bash
kubectl get pods -n bookrating

# Prit derisa të jenë Running
kubectl get pods -n bookrating -w
```

### 4.2 Shiko Logs
```bash
# Merr pod name
kubectl get pods -n bookrating

# Shiko logs
kubectl logs -n bookrating <POD_NAME>

# Ose për të gjitha pods
kubectl logs -n bookrating -l app=bookrating-api --tail=50
```

### 4.3 Shiko Service dhe Merr External IP
```bash
kubectl get svc -n bookrating

# Prit derisa EXTERNAL-IP të mos jetë <pending>
kubectl get svc bookrating-api-service -n bookrating -w
```

---

## HAPI 5: Testo API

Pasi të marrësh External IP:

```bash
# Ndrysho <EXTERNAL-IP> me IP-në që more
curl http://<EXTERNAL-IP>/health

# Hap Swagger në browser
open http://<EXTERNAL-IP>/swagger
```

---

## Troubleshooting

### Pod nuk starton
```bash
# Shiko status
kubectl describe pod -n bookrating <POD_NAME>

# Shiko logs
kubectl logs -n bookrating <POD_NAME>
```

### ImagePullBackOff Error
```bash
# Verifiko që ACR është i lidhur me AKS
az aks check-acr --resource-group bookrating-rg --name bookrating-aks --acr bookratingacr.azurecr.io

# Verifiko që image ekziston në ACR
az acr repository show-tags --name bookratingacr --repository bookrating-api
```

### CrashLoopBackOff
- Shiko logs për errors
- Verifiko që port 8080 është i saktë
- Verifiko që /health endpoint funksionon

---

## Update Deployment (Pas Ndryshimeve)

```bash
# 1. Build dhe push version të ri
docker build -t bookratingacr.azurecr.io/bookrating-api:v1.1 .
docker push bookratingacr.azurecr.io/bookrating-api:v1.1

# 2. Update deployment
kubectl set image deployment/bookrating-api \
  bookrating-api=bookratingacr.azurecr.io/bookrating-api:v1.1 \
  -n bookrating

# 3. Verifiko rollout
kubectl rollout status deployment/bookrating-api -n bookrating
```

---

## Cleanup

```bash
# Fshi aplikacionin
kubectl delete namespace bookrating

# Fshi AKS cluster (nëse dëshiron)
az aks delete --resource-group bookrating-rg --name bookrating-aks --yes

# Fshi të gjitha resources
az group delete --name bookrating-rg --yes
```

---

## Quick Commands Reference

```bash
# Status
kubectl get all -n bookrating
kubectl get pods -n bookrating
kubectl get svc -n bookrating

# Logs
kubectl logs -f -n bookrating -l app=bookrating-api

# Restart
kubectl rollout restart deployment/bookrating-api -n bookrating

# Scale
kubectl scale deployment/bookrating-api --replicas=3 -n bookrating

# Delete
kubectl delete deployment bookrating-api -n bookrating
kubectl delete service bookrating-api-service -n bookrating
```
