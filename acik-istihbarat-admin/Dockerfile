FROM node:22-alpine AS builder
WORKDIR /app

# Copy deps
COPY package*.json ./
RUN npm install

# Copy source and build
COPY . .
ARG VITE_API_URL
ENV VITE_API_URL=${VITE_API_URL}
RUN npm run build

# Runtime Stage
FROM nginx:alpine
# Copy compiled files to Nginx html directory
COPY --from=builder /app/dist /usr/share/nginx/html

# Expose port 80
EXPOSE 80

CMD ["nginx", "-g", "daemon off;"]
