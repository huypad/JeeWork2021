#!/bin/sh

export DOMAIN=jee.vn
export VAULT_ENDPOINT=http://vault_vault:8200
export VAULT_TOKEN=s.CnO0sRfOCIvMAYcSnoceE1A5
export KAFKA_BROKER=kafka1:9999,kafka2:9999,kafka3:9999
export CONNECTION_STRING="Data Source=192.168.199.4,1433;Initial Catalog=JeeWork;User ID=jeework;Password=Jee33W0rkdB"
export HOST_JEEWORK_API=https://jeework-api.jee.vn
export HOST_JEEWORK_BE=https://jeework.jee.vn
export HOST_JEEACCOUNT_API=https://jeeaccount-api.jee.vn
export HOST_ISSUER=https://jeework-api.jee.vn
export HOST_MINIO=https://cdn.jee.vn

docker stack deploy -c ./jeework.swarm.yml --with-registry-auth jeework