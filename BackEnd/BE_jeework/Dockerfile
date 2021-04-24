FROM node:14.15.5-slim As builder
WORKDIR /app
COPY BackEnd/BE_jeework/package.json ./
COPY BackEnd/BE_jeework/dps-lib-3.1.11.tgz ./
RUN yarn install
COPY BackEnd/BE_jeework /app/
RUN yarn build --prod

FROM nginx:1.19.8-alpine
COPY backend.nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /app/dist/ /usr/share/nginx/html
EXPOSE 80