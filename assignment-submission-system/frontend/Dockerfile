# Build context is the repository root: docker build -f frontend/Dockerfile .
FROM node:20-alpine AS deps
WORKDIR /app
COPY frontend/package.json frontend/package-lock.json* ./
RUN npm install

FROM node:20-alpine AS build
WORKDIR /app
COPY --from=deps /app/node_modules ./node_modules
COPY frontend/ ./

# Baked into the client bundle at build time, so it must point at the API the browser can reach.
ARG NEXT_PUBLIC_API_BASE_URL=http://localhost:5080
ENV NEXT_PUBLIC_API_BASE_URL=$NEXT_PUBLIC_API_BASE_URL
ENV NEXT_TELEMETRY_DISABLED=1

RUN npm run build

FROM node:20-alpine AS runtime
WORKDIR /app
ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

COPY --from=build /app/public ./public
COPY --from=build /app/.next/standalone ./
COPY --from=build /app/.next/static ./.next/static

EXPOSE 3000
CMD ["node", "server.js"]
