FROM node:22-alpine AS build
WORKDIR /app

COPY datashare-web/package*.json ./
RUN npm ci

COPY datashare-web/ .
RUN npx ng build --configuration=production

FROM nginx:alpine
COPY --from=build /app/dist/datashare-web/browser /usr/share/nginx/html
COPY nginx.conf /etc/nginx/conf.d/default.conf

EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
