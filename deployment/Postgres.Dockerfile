FROM postgres:18-alpine
RUN apk upgrade --no-cache && rm /usr/local/bin/gosu
# The image already owns its data directory as postgres; no root-to-user helper is needed.
USER postgres
