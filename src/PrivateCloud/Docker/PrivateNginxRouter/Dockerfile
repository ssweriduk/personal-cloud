FROM nginx:stable-alpine

RUN apk add --no-cache openssl

RUN openssl req -x509 -nodes -days 365 \
-subj "/C=US/ST=New York/L=New York/O=Sweriduk Inc./CN=Sweriduk Private NGINX SSL Certificate" \
-addext "subjectAltName=DNS:sweriduk.com" -newkey rsa:2048 \
-keyout /etc/ssl/private/nginx-selfsigned.key \
-out /etc/ssl/certs/nginx-selfsigned.crt;

COPY default.conf /etc/nginx/conf.d/default.conf
COPY helloworld.html /usr/share/nginx/html/index.html