# 17 — Deploy to Kubernetes

**9–11 hours** · Move the stack from `docker-compose.full.yml` onto k8s. Plain YAML first to see every object, Helm after.

> **This file deliberately contains no code.** It says *what to do* and *why*. Look up syntax in the official docs when you get to each step — syntax is easy to find, ordering and reasoning are not.

> **Why plain YAML first, Helm second:** Helm injects placeholders into the middle of YAML, so learning Helm first leaves you unable to tell "this is Kubernetes" from "this is Helm". Once plain YAML works, switching to Helm is just replacing hardcoded values with slots — the YAML carries over almost untouched.

## Goals

1. **App runs end to end inside the cluster:** DB → migration → seed → API → FE → mail, talking to each other over internal Service DNS.
2. **One FE image for every environment:** Ingress routes `/api` to the API and `/` to the Web on the same domain, FE calls relative paths.
3. **Migrations run in the right order:** schema is updated first, API only switches version after that — never the reverse.
4. **One-command deploy and rollback:** Helm keeps release history so a bad deploy can be reverted.
5. **Auto-renewing HTTPS:** cert-manager + Let's Encrypt.

## Steps

### Phase 0 — Prerequisites (1 hour)

#### 1. Install the cluster (25 min)

Pick **one**, based on what you need:

| | Use when | What you get |
|---|---|---|
| **k3s** | Deploying for real on a VPS / your own server | One binary, managed by systemd, public IP you can point a domain at |
| **k3d** | Learning on your laptop | Runs k3s inside Docker, spins up a multi-node cluster in 30 seconds → lets you learn scheduling and node failure |

Both install with a single script from their homepage.

> **k3s ships 5 things EKS/GKE do not:** Traefik (ingress controller), CoreDNS, metrics-server, local-path (storage provisioner), ServiceLB. So you never install an ingress controller or configure a StorageClass here — but it also means you *will* have to install those yourself if you move to a real cloud.

**Done when:** you can list nodes and they report `Ready`.

#### 2. Point kubectl at the cluster (10 min)

Install `kubectl` on your workstation. Grab the kubeconfig from the server (k3s puts it at `/etc/rancher/k3s/k3s.yaml`; k3d writes to `~/.kube/config` itself), copy it over, and change the server address inside it to the public IP if you are working remotely.

**Done when:** you can read cluster info and the node list from your laptop.

#### 3. Build and push the images (25 min)

**Kubernetes does not build code — it only pulls images.** Everything under `build:` in the compose file cannot be carried over; images must be built and pushed first.

1. Pick a registry: GHCR (free, supports private, convenient if the code is already on GitHub) or Docker Hub.
2. Build **three** images, not two — the third is easy to forget:
   - API, from `ecommerce.Api/Dockerfile` target `api`
   - Migrator, from the same Dockerfile target `migrator` (this is what the migrate Job runs)
   - Web, from `ecommerce.Web/Dockerfile`
3. Tag all three for your registry and push them.

> **Do not use the `latest` tag.** Use a version number or commit hash. Reason: with `latest` the tag never changes, so k8s sees no reason to re-pull, the old pod keeps running the old image, and you lose half an hour debugging it. With a fresh tag per build, deploying *is* changing the tag.

**Done when:** you can pull all three images from a different machine.

---

### Phase 1 — Fix the app before it goes near the cluster (1.5–2 hours)

Three problems. Skip them and the deploy succeeds but the app is unusable.

#### 4. VITE_API_URL is build-time (45 min)

**Problem:** Vite bakes `VITE_API_URL` into the bundle *at build time*; it never reads env at runtime. The current FE image will forever call `http://localhost:5118`. Changing domain means rebuilding the image.

**Chosen fix — same-domain Ingress + relative paths:**

1. Ingress will route `/api` to the API Service and `/` to the Web Service, on the **same** domain.
2. Strip the base URL out of the FE and call the API with relative paths starting with `/api`. The browser prepends whatever domain is open.
3. Hunt down every remaining hardcode: the API client config, the axios instance / interceptor, and any direct `fetch` calls.

**Result:** a single FE image that works on localhost, staging and prod with no rebuild.

**Rejected alternative:** one FE image per domain — an image per environment, messier CI, rebuild on every domain change.

> **Fix the API prefix while you are here.** If the route is `/api` but the API actually serves `/products` rather than `/api/products`, you must either have Ingress strip the `/api` prefix before forwarding, or add `/api` to the API's own route prefix. Pick the second — less magic in the Ingress, and Swagger paths stay correct.

**Done when:** a freshly built FE image, served behind a proxy on a different domain, still hits the API.

#### 5. CORS is hardcoded (30 min)

**Problem:** the API's CORS policy only allows `http://localhost:5173`. On a real domain it blocks everything.

Move the allowed origin list into configuration so it can be overridden by an environment variable, following how the other settings already work (`appsettings.json` + env override). Remove the literal from the startup code.

> If step 4 is done, the FE and API share an origin and CORS is **almost** irrelevant. Fix it anyway: the hardcoded `localhost:5173` will block Swagger on another domain and block every other client you add later.

**Done when:** changing an env var changes the allowed origin, with no API rebuild.

#### 6. Lock the API to 1 replica (30 min)

**Problem:** inventory uploads are written to `/app/Storage` — the disk of that specific pod. The background worker that processes those files runs inside the API pod too.

Together those give you a hard limit:

1. The PVC k3s provisions by default is **ReadWriteOnce** — only one node can mount it at a time.
2. With 2 replicas on 2 nodes: replica A receives the upload, the worker on replica B goes looking for the file and does not find it.

**Decision for this phase: exactly 1 API replica.** No code change needed, just do not try to scale.

**Upgrade path when scaling actually matters:** move file storage to object storage (MinIO in-cluster, or S3 / Azure Blob). Then any pod can read any other pod's files and the `api-storage` PVC disappears entirely.

**Done when:** this limit is written down in the k8s folder's README so nobody wonders about it three months from now.

---

### Phase 2 — Plain YAML (3–4 hours)

Create a `k8s/` folder at the repo root, one object per file, with filenames that say what they are.

The compose-to-k8s translation — follow this order, each step is verifiable on its own:

| Step | Object | Replaces which part of compose |
|---|---|---|
| 7 | Namespace | — (groups the app, makes cleanup easy) |
| 8 | Secret + ConfigMap | `environment:` |
| 9 | PVC + Deployment + Service for SQL Server | service `db` + volume `db-data` |
| 10 | migrate Job | service `migrate` |
| 11 | seed Job | service `seed` |
| 12 | Deployment + Service for mailpit | service `mailpit` |
| 13 | PVC + Deployment + Service for the API | service `api` + volume `api-storage` |
| 14 | Deployment + Service for the Web | service `web` |
| 15 | Ingress | the `ports:` that expose things publicly (one route — see step 15) |

#### 7. Namespace (5 min)

Create a dedicated namespace. Every object below declares that it belongs to it.

#### 8. Secret and ConfigMap (30 min)

**Secret:** SQL Server SA password, connection string, JWT signing key.
**ConfigMap:** everything else — `ASPNETCORE_ENVIRONMENT`, `Smtp__Host`, `Smtp__Port`, allowed origins.

> **A k8s Secret is base64, not encryption.** Do not commit Secret manifests. The right approach at this level: create the Secret with a direct command from your machine, and commit a template file that lists the key names with no values. When this needs to be serious, use Sealed Secrets or External Secrets.

**Done when:** the Secret and ConfigMap are listed in the namespace.

#### 9. SQL Server (45 min)

Three objects: a PVC claiming disk for `db-data`, a Deployment running the SQL Server container with its password pulled from the Secret, and a Service so other pods reach it by name.

**Deployment or StatefulSet?** With exactly 1 replica, both work. StatefulSet is more correct conceptually (it gives the pod a stable identity and a stable volume, which is what a database wants) but with 1 replica the difference barely shows. Use a Deployment for simplicity; if you want to practise StatefulSets, this is a good place to do it.

Add a readiness probe based on the healthcheck already in the compose file (run a trivial query).

> **What a Service is for:** pods get a new IP on every restart. A Service gives you a stable DNS name inside the cluster and other pods only need that name. Name the Service `db` and the connection string from compose carries over almost unchanged.

**Done when:** you can port-forward and connect to the in-cluster DB from a local SQL client.

#### 10. migrate Job (30 min)

A Job using the migrator image from step 3, with the connection string from the Secret, running once and exiting.

> **Kubernetes has NO `depends_on`.** The Job will start before the DB is ready and fail. That is normal: set an appropriate `restartPolicy` and a `backoffLimit` of a few attempts, and k8s retries until the DB is up. Do not go hunting for a way to declare ordering — Helm hooks handle that in Phase 3.

**Done when:** the Job reports Completed and the DB has tables.

#### 11. seed Job (20 min)

A Job using the SQL Server image to run `sqlcmd` against `seeds/database/seed.sql`. Get the SQL file into the pod via a ConfigMap (the file is small) — mount it at a path and point `sqlcmd` there.

**Done when:** you can log in as `admin` once the API is up.

#### 12. mailpit (15 min)

Deployment + Service. The Service exposes two ports: 1025 for SMTP (the API dials in) and 8025 for the UI.

**Done when:** port-forward to 8025 opens the mailpit UI.

#### 13. API (50 min)

PVC for `api-storage`, Deployment with 1 replica, Service.

Four things this Deployment must have:

1. **Env** from the Secret (connection string) and the ConfigMap (SMTP, environment, CORS).
2. **Volume mount** of the `api-storage` PVC at `/app/Storage`.
3. **readiness and liveness probes** — see the note below.
4. **resource requests and limits** — declare them now, even with RAM to spare. Without them k8s treats the pod as lowest priority and kills it first under memory pressure.

> **readiness vs liveness:**
> - **readiness** = "is this pod ready to serve requests?" Fails → the Service and Ingress stop sending traffic, but the pod is **not** killed.
> - **liveness** = "is this pod still alive?" Fails → k8s kills the pod and starts a new one.
>
> With no readiness probe, Ingress sends traffic the instant the pod starts, while the app is still booting → users get 502s on every deploy. With a too-aggressive liveness probe (short timeout) the pod gets killed in a loop when it is merely slow.

If the API has no health endpoint yet, add one that returns 200 without touching the DB for liveness, and one that does check the DB for readiness.

**Done when:** port-forward to the API pod, open Swagger, and successfully call an endpoint that reads the DB.

#### 14. Web (20 min)

Deployment + Service, nginx container on port 80. No volume. One env var: `API_UPSTREAM`, which already defaults to `api:8080` in the image — so as long as the API Service is named `api` on port 8080, there is nothing to set here.

**Done when:** port-forward to the web Service and the homepage loads.

#### 15. Ingress (30 min)

One Ingress, **one route**: `/` to the Web Service. nginx inside the Web pod already proxies `/api` to the API Service itself, so Ingress does not need a second route.

> This is simpler than originally planned. It came out of step 4: giving nginx the `/api` proxy made the same config work in compose and in k8s, and left Ingress with one job.

k3s already runs Traefik as the ingress controller, so nothing to install; just declare the right ingress class.

**Done when — the biggest milestone in this plan:** open a browser at the IP/domain, log in, browse products, add to cart, check out. No port-forward, no stray ports.

#### 16. See the orphan trap before moving to Phase 3 (5 min)

This experiment takes two minutes and is the reason Phase 3 exists: delete the mailpit YAML file from `k8s/`, re-apply the whole folder, then check — **mailpit is still running**. Apply only knows about the files you hand it; it has no idea what was there last time.

Six months of that and the cluster is full of orphaned objects nobody dares delete. This is exactly what Helm solves.

---

### Phase 3 — Move to Helm (2 hours)

#### 17. Generate the chart skeleton (15 min)

Use Helm's chart-creation command, then **delete all** the sample templates it generates (service account, HPA, test connection, NOTES) — keep the empty folder. The skeleton only needs `Chart.yaml`, `values.yaml` and `templates/`.

#### 18. Move the Phase 2 YAML into templates (40 min)

Copy the YAML files into `templates/` as-is, then replace **hardcoded values** with slots read from values. Only the things that genuinely vary per environment:

1. Image tags (API, migrator, web) — this is what changes on every deploy
2. Replica counts
3. The Ingress host
4. PVC size
5. Resource requests/limits

Do not parameterise everything. Every placeholder is one more thing you must read in two places to understand; if a value never changes, leave it hardcoded.

#### 19. Two values files for two environments (25 min)

`values-dev.yaml` and `values-prod.yaml`. The important difference is not replica counts, it is **whether certain objects exist at all**:

- **dev:** renders the SQL Server and mailpit Deployments (DB and mail inside the cluster).
- **prod:** renders neither. The DB is an external service and SMTP is a real provider — only the connection string and SMTP host remain, in the Secret.

Do this by wrapping those two templates entirely in a conditional, toggled by a flag in values.

> This is the thing `sed` and `envsubst` cannot do: they can substitute strings, but they cannot make an object disappear.

#### 20. Hook for the migrate Job (20 min)

**This is the main reason to use Helm for this app.** Mark the migrate Job as a pre-install and pre-upgrade hook, and the seed Job as running after it (use hook weights to order them).

Helm will then: run migrate → **wait for it to exit successfully** → only then update the API Deployment. If the migration fails, Helm stops and the API pods keep running the previous, working version.

Compare with Phase 2, where apply pushes every file up more or less simultaneously and the API can boot against the old schema.

Add a hook delete policy for old Jobs, otherwise every upgrade leaves a finished Job lying around.

#### 21. Check the chart before deploying (10 min)

Have Helm **render the chart to YAML without deploying**, then read the output and compare it against the Phase 2 YAML. This is the only chart debugging technique you need — a template mistake shows up directly in the output.

**Done when:** the rendered YAML matches the Phase 2 YAML.

#### 22. Install, then rehearse a rollback (15 min)

1. Install the chart with the dev values. The app comes back up as before.
2. Deploy again with a different image tag so there are two versions in history.
3. **Roll back to version 1.** Confirm the pods return to the old image.

Learn that rollback works *before* you need it at 11pm.

For CI/CD, also enable wait-until-ready with a timeout, plus automatic rollback on timeout. A bad deploy then reverts itself with no human involved.

**Done when:** one command deploys, one command reverts.

---

### Phase 4 — HTTPS and domain (1–1.5 hours)

#### 23. Point DNS (10 min + propagation)

Create an A record pointing the domain at the server IP. Wait for propagation (minutes to hours).

**Done when:** the domain resolves to the server IP.

#### 24. Install cert-manager (20 min)

Install it from its official chart — this is the clearest example of "a chart that already exists": you write no YAML, you just install it.

What cert-manager does: watches your Ingress, requests certificates for it, stores them in a Secret, and **renews them automatically** before expiry. Without it you are doing this by hand in 90 days.

#### 25. Create a Let's Encrypt issuer (20 min)

Declare an issuer using Let's Encrypt with HTTP-based validation (it temporarily adds a route to your Ingress so Let's Encrypt can verify you own the domain).

> **Use the Let's Encrypt staging environment first.** Production has a weekly request limit; a few misconfigured attempts get you rate-limited and waiting. Staging issues certificates browsers do not trust, but that is enough to confirm the flow works. Switch to production once it does.

#### 26. Attach TLS to the Ingress (15 min)

Add the TLS section to the Ingress: the real host and the Secret name for cert-manager to write the certificate into. Add the annotation naming the issuer. Change the host from IP/localhost to the real domain in the prod values file.

#### 27. Verify, then switch to the production issuer (15 min)

Confirm the certificate was issued, switch the issuer from staging to production, redeploy, open a browser.

**Done when:** the site loads over https with no certificate warning and the app works end to end.

---

## Accepted limitations

| Limitation | Current state | Upgrade when | How |
|---|---|---|---|
| **API at 1 replica** | Exactly 1, because uploads live on the pod's disk | You need throughput or true zero-downtime | Move file storage to MinIO/S3, drop the `api-storage` PVC, then scale |
| **DB inside the cluster** | SQL Server on a local-path PVC. Node dies, data is gone | As soon as there is real data | Managed DB outside the cluster (RDS/Azure SQL). At minimum: a scheduled backup job writing off-cluster |
| **No autoscaling** | One node, no HPA | Load varies by time of day | Add nodes, enable HPA. HPA needs the resource requests from step 13 |
| **No monitoring** | `kubectl logs` only, no alerts | You have real users | Prometheus + Grafana for metrics, Loki for logs |
| **Secrets are only base64** | Created by hand, not in Git | More than one person deploys | Sealed Secrets or External Secrets |

The first two rows matter. The last three can wait.

## What a single-node k3s hides from you

1. **The ReadWriteOnce constraint.** On one node every pod lands in the same place, so sharing a volume works and you never see the error. On a multi-node cluster — and on any cloud, where disks are also ReadWriteOnce — the second replica gets scheduled elsewhere and **cannot mount**, leaving it stuck pending. That, not any weakness in k3s, is the real reason for the 1-replica limit in step 6.
2. **Scheduling.** With one node there is always room. You never meet `nodeSelector`, affinity, taints/tolerations or PodDisruptionBudgets — which skips exactly the "k8s decides where pods go" part that is the main reason k8s exists.
3. **Node failure.** On one node, node death is cluster death and teaches nothing. On three nodes you watch pods get rescheduled — and watch which pods *cannot* move because they are pinned to a local volume.
4. **requests/limits.** A laptop with spare RAM runs fine without them. On a smaller node, a pod with no requests is the first one evicted. Declare them from the start (step 13) so you do not learn this through an incident.

**Mitigation:** create a 3-node k3d cluster (1 control plane + 2 agents) to practise items 1–3. It is still k3s, so moving to a server later requires no relearning.

## Schedule

| Session | Phases | Hours | What you have at the end |
|---|---|---|---|
| 1 | 0 + 1 | ~3 | Cluster up, images pushed, the three blocking app issues fixed |
| 2 | 2 | 3–4 | **App running end to end in the cluster through Ingress** |
| 3 | 3 + 4 | 3–3.5 | One-command deploy/rollback, auto-renewing HTTPS |

Phases 0+1+2 are the minimum to get the app running. Stopping there is a complete milestone — Phases 3 and 4 are polish and can come later.
