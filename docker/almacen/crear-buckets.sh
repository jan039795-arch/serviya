#!/bin/sh
# Crea los buckets de desarrollo (research R3). Lo ejecuta el servicio almacen-buckets.
set -eu

crear() {
  if aws s3api head-bucket --bucket "$1" 2>/dev/null; then
    echo "Ya existe el bucket $1"
  else
    aws s3api create-bucket --bucket "$1"
    echo "Bucket $1 creado"
  fi
}

crear serviya-privado
crear serviya-publico

# Solo el portafolio aprobado se lee sin firma; el bucket privado nunca tiene acceso público.
aws s3api put-bucket-policy --bucket serviya-publico --policy file:///config/politica-publico.json

# El navegador sube con PUT directo a la URL firmada desde el servidor de desarrollo de Angular.
aws s3api put-bucket-cors --bucket serviya-privado --cors-configuration file:///config/cors.json \
  || echo "Aviso: no se pudo aplicar CORS a serviya-privado"
aws s3api put-bucket-cors --bucket serviya-publico --cors-configuration file:///config/cors.json \
  || echo "Aviso: no se pudo aplicar CORS a serviya-publico"
