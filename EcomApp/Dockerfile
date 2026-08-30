FROM node:22-alpine AS build
WORKDIR /app

COPY EcomApp/package*.json ./
RUN npm ci

COPY EcomApp/ ./
RUN npm run build

FROM nginx:alpine AS final
COPY --from=build /app/dist/ecom-app/browser /usr/share/nginx/html
COPY EcomApp/nginx.conf /etc/nginx/conf.d/default.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]