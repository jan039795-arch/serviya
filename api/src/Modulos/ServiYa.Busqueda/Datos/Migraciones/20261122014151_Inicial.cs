using System;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ServiYa.Busqueda.Datos.Migraciones
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "catalogos");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "municipios",
                schema: "catalogos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    clave_inegi = table.Column<string>(type: "character(5)", fixedLength: true, maxLength: 5, nullable: false),
                    nombre = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    estado = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    poligono = table.Column<MultiPolygon>(type: "geography (multipolygon, 4326)", nullable: false),
                    centroide = table.Column<Point>(type: "geography (point, 4326)", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_municipios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "oficios",
                schema: "catalogos",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    slug = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    icono = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    activo = table.Column<bool>(type: "boolean", nullable: false),
                    updated_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    created_at = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("pk_oficios", x => x.id);
                });

            migrationBuilder.InsertData(
                schema: "catalogos",
                table: "oficios",
                columns: new[] { "id", "activo", "updated_at", "created_at", "icono", "nombre", "slug" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-7000-8000-000000000001"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "ladrillo", "Albañil", "albanil" },
                    { new Guid("a0000000-0000-7000-8000-000000000002"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "llave", "Fontanero", "fontanero" },
                    { new Guid("a0000000-0000-7000-8000-000000000003"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "rayo", "Electricista", "electricista" },
                    { new Guid("a0000000-0000-7000-8000-000000000004"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "serrucho", "Carpintero", "carpintero" },
                    { new Guid("a0000000-0000-7000-8000-000000000005"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "brocha", "Pintor", "pintor" },
                    { new Guid("a0000000-0000-7000-8000-000000000006"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "planta", "Jardinero", "jardinero" },
                    { new Guid("a0000000-0000-7000-8000-000000000007"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "auto", "Chofer", "chofer" },
                    { new Guid("a0000000-0000-7000-8000-000000000008"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "libro", "Maestro particular", "maestro-particular" },
                    { new Guid("a0000000-0000-7000-8000-000000000009"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "escoba", "Limpieza del hogar", "limpieza-del-hogar" },
                    { new Guid("a0000000-0000-7000-8000-000000000010"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "nino", "Cuidado de niños", "cuidado-de-ninos" },
                    { new Guid("a0000000-0000-7000-8000-000000000011"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "manos", "Cuidado de adultos mayores", "cuidado-de-adultos-mayores" },
                    { new Guid("a0000000-0000-7000-8000-000000000012"), true, new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), new DateTimeOffset(new DateTime(2026, 11, 22, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)), "llave-puerta", "Cerrajero", "cerrajero" }
                });

            migrationBuilder.CreateIndex(
                name: "ix_municipios_clave_inegi",
                schema: "catalogos",
                table: "municipios",
                column: "clave_inegi",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_municipios_poligono",
                schema: "catalogos",
                table: "municipios",
                column: "poligono")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "ix_oficios_slug",
                schema: "catalogos",
                table: "oficios",
                column: "slug",
                unique: true);

            // Catálogos de solo lectura para la app: los oficios vienen en las migraciones y los
            // municipios del importador, que corre el dueño del esquema (T066).
            migrationBuilder.Sql("REVOKE INSERT, UPDATE, DELETE, TRUNCATE ON catalogos.oficios, catalogos.municipios FROM serviya_app;");

            // Importador de municipios desde un GeoJSON del Marco Geoestadístico de INEGI (WGS 84), con
            // CVEGEO y NOMGEO en las propiedades. Repetirlo actualiza sin duplicar. Lo envuelve
            // Datos/Semillas/importar-municipios.sql; las pruebas lo llaman directo.
            migrationBuilder.Sql("""
                CREATE FUNCTION catalogos.importar_municipios(geojson jsonb) RETURNS integer
                LANGUAGE plpgsql
                SET search_path = pg_catalog, public
                AS $funcion$
                DECLARE
                    v_rasgo jsonb;
                    v_clave text;
                    v_nombre text;
                    v_estado text;
                    v_poligono geometry;
                    v_total integer := 0;
                BEGIN
                    IF jsonb_typeof(geojson -> 'features') IS DISTINCT FROM 'array'
                        OR jsonb_array_length(geojson -> 'features') = 0 THEN
                        RAISE EXCEPTION 'El GeoJSON no trae municipios en "features".';
                    END IF;

                    FOR v_rasgo IN SELECT r FROM jsonb_array_elements(geojson -> 'features') AS r LOOP
                        v_clave := v_rasgo -> 'properties' ->> 'CVEGEO';
                        v_nombre := btrim(v_rasgo -> 'properties' ->> 'NOMGEO');
                        IF v_clave IS NULL OR v_clave !~ '^[0-9]{5}$' THEN
                            RAISE EXCEPTION 'Clave CVEGEO no válida: %', v_clave;
                        END IF;
                        IF v_nombre IS NULL OR v_nombre = '' OR length(v_nombre) > 120 THEN
                            RAISE EXCEPTION 'Nombre NOMGEO no válido en %', v_clave;
                        END IF;

                        v_estado := CASE left(v_clave, 2)
                        WHEN '01' THEN 'Aguascalientes'
                        WHEN '02' THEN 'Baja California'
                        WHEN '03' THEN 'Baja California Sur'
                        WHEN '04' THEN 'Campeche'
                        WHEN '05' THEN 'Coahuila'
                        WHEN '06' THEN 'Colima'
                        WHEN '07' THEN 'Chiapas'
                        WHEN '08' THEN 'Chihuahua'
                        WHEN '09' THEN 'Ciudad de México'
                        WHEN '10' THEN 'Durango'
                        WHEN '11' THEN 'Guanajuato'
                        WHEN '12' THEN 'Guerrero'
                        WHEN '13' THEN 'Hidalgo'
                        WHEN '14' THEN 'Jalisco'
                        WHEN '15' THEN 'Estado de México'
                        WHEN '16' THEN 'Michoacán'
                        WHEN '17' THEN 'Morelos'
                        WHEN '18' THEN 'Nayarit'
                        WHEN '19' THEN 'Nuevo León'
                        WHEN '20' THEN 'Oaxaca'
                        WHEN '21' THEN 'Puebla'
                        WHEN '22' THEN 'Querétaro'
                        WHEN '23' THEN 'Quintana Roo'
                        WHEN '24' THEN 'San Luis Potosí'
                        WHEN '25' THEN 'Sinaloa'
                        WHEN '26' THEN 'Sonora'
                        WHEN '27' THEN 'Tabasco'
                        WHEN '28' THEN 'Tamaulipas'
                        WHEN '29' THEN 'Tlaxcala'
                        WHEN '30' THEN 'Veracruz'
                        WHEN '31' THEN 'Yucatán'
                        WHEN '32' THEN 'Zacatecas'
                        WHEN '99' THEN 'Estado de Prueba'
                        END;
                        IF v_estado IS NULL THEN
                            RAISE EXCEPTION 'Entidad desconocida en %', v_clave;
                        END IF;

                        -- Solo las partes de polígono, ya corregidas; un punto o una línea no son un municipio.
                        v_poligono := ST_Multi(ST_CollectionExtract(ST_MakeValid(
                            ST_SetSRID(ST_GeomFromGeoJSON((v_rasgo -> 'geometry')::text), 4326)), 3));
                        IF v_poligono IS NULL OR ST_IsEmpty(v_poligono) THEN
                            RAISE EXCEPTION 'El municipio % no tiene polígono.', v_clave;
                        END IF;

                        INSERT INTO catalogos.municipios (id, clave_inegi, nombre, estado, poligono, centroide, created_at, updated_at)
                        VALUES (gen_random_uuid(), v_clave, v_nombre, v_estado, v_poligono::geography,
                                ST_PointOnSurface(v_poligono)::geography, now(), now())
                        ON CONFLICT (clave_inegi) DO UPDATE
                        SET nombre = EXCLUDED.nombre,
                            estado = EXCLUDED.estado,
                            poligono = EXCLUDED.poligono,
                            centroide = EXCLUDED.centroide,
                            updated_at = now();
                        v_total := v_total + 1;
                    END LOOP;

                    RETURN v_total;
                END
                $funcion$;
                """);
            migrationBuilder.Sql("REVOKE EXECUTE ON FUNCTION catalogos.importar_municipios(jsonb) FROM PUBLIC, serviya_app;");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("DROP FUNCTION IF EXISTS catalogos.importar_municipios(jsonb);");

            migrationBuilder.DropTable(
                name: "municipios",
                schema: "catalogos");

            migrationBuilder.DropTable(
                name: "oficios",
                schema: "catalogos");
        }
    }
}
