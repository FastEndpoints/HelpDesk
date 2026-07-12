FROM node:26-alpine AS build
WORKDIR /src
RUN npm install --global pnpm@11.10.0
COPY package.json pnpm-lock.yaml pnpm-workspace.yaml .npmrc ./
COPY frontend/package.json frontend/package.json
RUN pnpm install --frozen-lockfile --ignore-scripts
COPY frontend frontend
RUN pnpm --dir frontend prepare \
 && pnpm --dir frontend build \
 && pnpm --filter @helpdesk/frontend deploy --prod --legacy /runtime

FROM node:26-alpine
ENV NODE_ENV=production
WORKDIR /app
COPY --from=build --chown=node:node /runtime ./
COPY --from=build --chown=node:node /src/frontend/build ./build
USER node
EXPOSE 3000
CMD ["node", "build"]
