# ---- build ----
FROM node:24-alpine AS build
WORKDIR /app

COPY package.json package-lock.json ./
RUN npm ci

COPY . .
# Origin-agnostic: the FE calls relative /api paths, and nginx below proxies
# them, so no build-time URL is baked into this image.
RUN npm run build

# ---- serve ----
FROM nginx:1.29-alpine
# Runtime upstream: same image for every environment. Default matches
# compose-full and k8s (Service/container name "api" on :8080); the dev
# stack overrides it to reach the API on the host instead.
ENV API_UPSTREAM=api:8080
# Limits the nginx image's envsubst templating to $API_* vars, so it doesn't
# also try to substitute nginx's own runtime vars ($host, $remote_addr,
# $proxy_add_x_forwarded_for, $scheme) if same-named env vars ever exist,
# which would silently break the proxy headers.
ENV NGINX_ENVSUBST_FILTER=API_
COPY default.conf.template /etc/nginx/templates/default.conf.template
COPY --from=build /app/dist /usr/share/nginx/html
EXPOSE 80
