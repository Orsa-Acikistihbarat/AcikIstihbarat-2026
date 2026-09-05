FROM node:22-alpine

WORKDIR /app

RUN addgroup -S webhook && adduser -S webhook -G webhook

COPY package*.json ./
RUN npm install --omit=dev

COPY server.js ./

RUN mkdir -p /data/acikmedya && chown -R webhook:webhook /data/acikmedya /app

USER webhook

EXPOSE 4001

CMD ["node", "server.js"]
