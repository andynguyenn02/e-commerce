# Kubernetes Deployment

This directory holds plain-YAML manifests as a readable reference. The actual deployment is in `ecommerce.Helm/` — use that chart to deploy, not these files.

Do not `kubectl apply -f k8s/` and run Helm against the same cluster. `apply` only knows about the files you hand it; it has no record of what was there last time. You would end up with two copies of every object, and the ones Helm did not create are invisible to it — `helm uninstall` leaves them running and nobody later knows whether they are safe to delete.

## Prerequisites

The chart creates the `ecommerce` namespace, but three objects must exist inside it before `helm install` runs. Each one fails in a different way, so the symptom tells you which is missing:

| Missing | Symptom |
|---|---|
| `seed-sql` | pod stuck in `ContainerCreating`, event `MountVolume.SetUp failed ... configmap "seed-sql" not found` |
| `ghcr-pull` | `ImagePullBackOff` on api, web or migrate |
| `app-secrets` | `CreateContainerConfigError` |

None of these surface as a clear error from `helm install` itself — it reports success and the pods sit broken, so check pod status after every fresh install.

First, create the namespace:

```
kubectl create namespace ecommerce
```

Then create these three objects:

### 1. app-secrets

A generic Secret with three keys. The password appears twice: once in `MSSQL_SA_PASSWORD` and again inside `ConnectionStrings__DefaultConnection` — they must match.

```
kubectl create secret generic app-secrets -n ecommerce \
  --from-literal=MSSQL_SA_PASSWORD='<password-you-choose>' \
  --from-literal=ConnectionStrings__DefaultConnection='Server=db,1433;Database=EcommerceDb;User Id=sa;Password=<same-password>;TrustServerCertificate=True' \
  --from-literal=JwtSettings__Key='<jwt-signing-key>'
```

Reference: `ecommerce.Helm/secret-example.yaml` lists the key names with empty values.

This is created by command rather than committed because a Kubernetes Secret is base64, not encryption — committing one publishes it. Sealed Secrets or External Secrets is the answer once more than one person deploys this.

### 2. seed-sql

A ConfigMap holding the SQL seed file, mounted by the seed Job:

```
kubectl create configmap seed-sql -n ecommerce --from-file=seed.sql=seeds/database/seed.sql
```

Run from the repo root. The seed file is guarded with NOT EXISTS checks and is safe to re-run on every upgrade.

### 3. ghcr-pull

A docker-registry Secret for private GHCR images (api, web, migrate):

```
kubectl create secret docker-registry ghcr-pull -n ecommerce \
  --docker-server=ghcr.io \
  --docker-username=<github-username> \
  --docker-password=<personal-access-token>
```

The token must have the `read:packages` scope.

## Deploying

```
helm upgrade --install ec ecommerce.Helm --wait --timeout 10m
```

There is one values file: `ecommerce.Helm/values.yaml`. No prod overlay — this chart targets one environment.

## How it works

Deploy order is enforced by Helm hooks, not by luck. The migrate Job runs first (post-install/pre-upgrade, hook-weight 0), waits for exit 0, then seed runs (same events, weight 1), and only then the api Deployment updates. If migration fails, the existing api pods keep serving the old version and Helm stops.

Both Jobs use `helm.sh/hook-delete-policy: before-hook-creation`, so exactly one finished Job of each survives and you can still inspect logs after a deploy.

## Rollback

List revisions:

```
helm history ec
```

Revert to a revision:

```
helm rollback ec <revision>
```

Add `--atomic` to the upgrade command in CI so failed deploys roll back automatically.

## Deliberate limitations

| Limitation | Why | What to do |
|-----------|-----|-----------|
| API runs 1 replica only | Inventory uploads go to `/app/Storage` (ReadWriteOnce PVC, one node). The worker processing uploads runs inside the api pod. With 2 replicas, pod A receives the file but pod B's worker tries to read it from its own disk (missing on multi-node). | Move uploads to object storage (MinIO or S3), drop the PVC, move the worker out, then scale. |
| Database on local-path PVC | Simple single-node setup. If the node dies, data is gone. | Add a managed database outside the cluster or a scheduled backup job. |
| Database uses `Recreate` strategy | `db-data` is ReadWriteOnce — rolling update would run two SQL Server processes on one data directory. | Accept downtime on db pod updates, or switch to managed database. |
| No HTTPS, no domain | Ingress has no host, answers on cluster IP/node IP. cert-manager not configured. | Add a domain and cert-manager when needed. |
| No monitoring, no autoscaling | Only `kubectl logs` available. | Add prometheus/grafana and HPA when needed. |
