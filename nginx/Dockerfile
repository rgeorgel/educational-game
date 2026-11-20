# Imagem do frontend: nginx com os arquivos estáticos e a configuração já dentro.
# (Plataformas como o Coolify não montam arquivos do repositório como volume.)
FROM nginx:1.27-alpine
COPY nginx/default.conf /etc/nginx/conf.d/default.conf
COPY frontend/ /usr/share/nginx/html/
