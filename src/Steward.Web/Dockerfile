FROM node:22-alpine AS build
ARG TARGETARCH
WORKDIR /app
COPY package.json package-lock.json ./
# arm64 is emulated via QEMU in CI; V8's JIT there can hit an emulator SIGILL, so disable it for that arch only.
RUN if [ "$TARGETARCH" = "arm64" ]; then export NODE_OPTIONS=--jitless; fi; npm ci
COPY . .
RUN if [ "$TARGETARCH" = "arm64" ]; then export NODE_OPTIONS=--jitless; fi; npm run build

FROM nginx:alpine AS final
COPY nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=build /app/dist /usr/share/nginx/html
COPY docker-entrypoint.d/40-generate-config.sh /docker-entrypoint.d/40-generate-config.sh
RUN chmod +x /docker-entrypoint.d/40-generate-config.sh
