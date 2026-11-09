-- Roles de ServiYa (tasks.md T039). Lo ejecuta la imagen de PostgreSQL solo al crear el volumen.
--   serviya_owner: dueño de la base y de los esquemas; con él corren las migraciones.
--   serviya_app:   con el que se conecta la API; solo lee y escribe datos. A la bitácora se le
--                  quitan UPDATE y DELETE en su migración (T049).
-- Las contraseñas llegan como variables de entorno del contenedor (desde .env), nunca en este archivo.
\set ON_ERROR_STOP on
\getenv owner_password SERVIYA_OWNER_PASSWORD
\getenv app_password SERVIYA_APP_PASSWORD

CREATE ROLE serviya_owner LOGIN PASSWORD :'owner_password';
CREATE ROLE serviya_app LOGIN PASSWORD :'app_password';

ALTER DATABASE serviya OWNER TO serviya_owner;
ALTER SCHEMA public OWNER TO serviya_owner;

REVOKE ALL ON DATABASE serviya FROM PUBLIC;
GRANT CONNECT, TEMPORARY ON DATABASE serviya TO serviya_app;

-- Cada módulo crea su esquema en sus migraciones (cuentas, prestadores, pagos...). Lo que cree
-- serviya_owner queda disponible para la app sin más pasos.
ALTER DEFAULT PRIVILEGES FOR ROLE serviya_owner GRANT USAGE ON SCHEMAS TO serviya_app;
ALTER DEFAULT PRIVILEGES FOR ROLE serviya_owner GRANT SELECT, INSERT, UPDATE, DELETE ON TABLES TO serviya_app;
ALTER DEFAULT PRIVILEGES FOR ROLE serviya_owner GRANT USAGE, SELECT ON SEQUENCES TO serviya_app;
ALTER DEFAULT PRIVILEGES FOR ROLE serviya_owner GRANT EXECUTE ON FUNCTIONS TO serviya_app;
GRANT USAGE ON SCHEMA public TO serviya_app;
