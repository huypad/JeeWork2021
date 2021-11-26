FROM node:14.17.1-slim As builder
ARG NODE_ENV
ENV NODE_OPTIONS=--max-old-space-size=8192

WORKDIR /app
COPY ./package.json ./
COPY ./yarn.lock ./
COPY ./dps-lib-3.1.11.tgz ./
RUN yarn install --production=false
COPY . .
RUN yarn build --configuration=${NODE_ENV}

FROM nginx:1.19.8-alpine
COPY ./backend.nginx.conf /etc/nginx/conf.d/default.conf
COPY --from=builder /app/dist/ /usr/share/nginx/html
EXPOSE 80