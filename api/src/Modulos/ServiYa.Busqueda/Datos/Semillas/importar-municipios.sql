-- Importa municipios a catalogos.municipios (tasks.md T066, research R5).
--
-- Entrada: un GeoJSON en WGS 84 (EPSG:4326) con CVEGEO y NOMGEO en las propiedades de cada rasgo,
-- como la capa de municipios del Marco Geoestadístico de INEGI. INEGI la publica como shapefile en
-- otra proyección; para convertirla y quedarse con la zona del piloto (por ejemplo, Querétaro = 22):
--   ogr2ogr -f GeoJSON -t_srs EPSG:4326 -where "CVE_ENT = '22'" municipios.geojson 00mun.shp
-- Mientras no se defina la ciudad del piloto, en desarrollo se usa zona-prueba.geojson (6 municipios
-- ficticios del prototipo).
--
-- Uso desde la raíz del repositorio, con `docker compose up -d` corriendo (funciona igual en
-- PowerShell). Repetirlo actualiza nombres y polígonos sin duplicar:
--   docker compose cp api/src/Modulos/ServiYa.Busqueda/Datos/Semillas/zona-prueba.geojson postgres:/tmp/municipios.geojson
--   docker compose cp api/src/Modulos/ServiYa.Busqueda/Datos/Semillas/importar-municipios.sql postgres:/tmp/importar-municipios.sql
--   docker compose exec postgres psql -U serviya_owner -d serviya -f /tmp/importar-municipios.sql
--
-- La función catalogos.importar_municipios la crea la migración Inicial de Búsqueda y solo la puede
-- ejecutar el dueño del esquema: la app no puede cambiar los catálogos.
\set ON_ERROR_STOP on
\set geojson `cat /tmp/municipios.geojson`
SELECT catalogos.importar_municipios(:'geojson'::jsonb) AS municipios_importados;
