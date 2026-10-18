-- =====================================================================
-- Trabajo en Clase N.º 2 — Programación Avanzada (BSI-19)
-- Archivo : verificar-restricciones.sql  (opcional, de apoyo)
-- Objetivo: comprobar que las restricciones de diseño-bd.sql rechazan
--           datos inválidos. Cada sentencia DEBE fallar; ninguna deja
--           cambios. Ejecutar con psql (no se detiene ante los errores):
--   psql "<conexión>" -f BaseDatos/verificar-restricciones.sql
-- Nota: los INSERT rechazados consumen valores de las secuencias de
--       identidad (huecos en los id), lo cual es normal en PostgreSQL.
-- =====================================================================

SET client_encoding = 'UTF8';
\set ON_ERROR_STOP 0
\echo '-- Tipo inválido'
INSERT INTO prq_automovil (color, anio, fabricante, tipo) VALUES ('Café', 2020, 'Kia', 'camión');
\echo '-- Año fuera de rango'
INSERT INTO prq_automovil (color, anio, fabricante, tipo) VALUES ('Café', 1700, 'Kia', 'sedán');
\echo '-- Precio por hora en cero'
INSERT INTO prq_parqueo (nombre_provincia, nombre, precio_por_hora) VALUES ('Limón', 'Parqueo Puerto', 0);
\echo '-- Provincia inexistente'
INSERT INTO prq_parqueo (nombre_provincia, nombre, precio_por_hora) VALUES ('Panamá', 'Parqueo X', 500);
\echo '-- Salida anterior a la entrada'
INSERT INTO prq_ingreso_automovil (id_parqueo, id_automovil, fecha_hora_entrada, fecha_hora_salida) VALUES (1, 1, '2026-10-18 10:00', '2026-10-18 09:00');
\echo '-- Parqueo inexistente (llave foránea)'
INSERT INTO prq_ingreso_automovil (id_parqueo, id_automovil, fecha_hora_entrada) VALUES (99, 1, '2026-10-18 10:00');
\echo '-- Borrar automóvil con ingresos (ON DELETE RESTRICT)'
DELETE FROM prq_automovil WHERE id = 1;
\echo '-- Conteos sin cambios'
SELECT (SELECT count(*) FROM prq_automovil) autos, (SELECT count(*) FROM prq_parqueo) parqueos, (SELECT count(*) FROM prq_ingreso_automovil) ingresos;
