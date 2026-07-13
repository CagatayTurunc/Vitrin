# ==============================================================
# Vitrin Frontend (Next.js) - Multi-Stage Dockerfile
# Standalone output modu ile ~150-200MB imaj boyutu
# ==============================================================

# Aşama 1: Bağımlılıkları kur
FROM node:20-alpine AS deps
WORKDIR /app

# pnpm kurulumu
RUN npm install -g pnpm

COPY package.json pnpm-lock.yaml ./
RUN pnpm install --no-frozen-lockfile

# ==============================================================
# Aşama 2: Build
# ==============================================================
FROM node:20-alpine AS builder
WORKDIR /app

# Önce kaynak kodu kopyala (node_modules hariç)
COPY . .

# Sonra deps aşamasındaki node_modules'i üzerine kopyala
COPY --from=deps /app/node_modules ./node_modules

ENV NEXT_TELEMETRY_DISABLED=1

# NEXT_PUBLIC_* değişkenleri build zamanında gömülür
ARG NEXT_PUBLIC_API_URL=http://localhost:5000
ARG NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME
ARG NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET

ENV NEXT_PUBLIC_API_URL=$NEXT_PUBLIC_API_URL
ENV NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME=$NEXT_PUBLIC_CLOUDINARY_CLOUD_NAME
ENV NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET=$NEXT_PUBLIC_CLOUDINARY_UPLOAD_PRESET

RUN npm install -g pnpm && pnpm run build

# ==============================================================
# Aşama 3: Runtime (sadece standalone çıktısı)
# ==============================================================
FROM node:20-alpine AS runtime
WORKDIR /app

ENV NODE_ENV=production
ENV NEXT_TELEMETRY_DISABLED=1

# Non-root kullanıcı
RUN addgroup --system --gid 1001 nodejs && \
    adduser --system --uid 1001 nextjs

COPY --from=builder /app/public ./public
COPY --from=builder --chown=nextjs:nodejs /app/.next/standalone ./
COPY --from=builder --chown=nextjs:nodejs /app/.next/static ./.next/static

USER nextjs

EXPOSE 3000
ENV PORT=3000
ENV HOSTNAME="0.0.0.0"

ENTRYPOINT ["node", "server.js"]
