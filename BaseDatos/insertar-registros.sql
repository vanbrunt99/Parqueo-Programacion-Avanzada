-- =====================================================================
-- Trabajo en Clase N.º 2 — Programación Avanzada (BSI-19)
-- Archivo : insertar-registros.sql
-- Requisito: al menos 5 automóviles, 2 parqueos y 15 ingresos.
-- Contenido: 6 automóviles, 3 parqueos y 17 ingresos
--            (15 estadías finalizadas y 2 en curso con salida NULL).
--
-- Ejecutar después de diseño-bd.sql, sobre la misma base y esquema.
-- Los identificadores se insertan explícitamente para que coincidan con
-- los archivos JSON de la carpeta Datos/. Al final se sincronizan las
-- secuencias de identidad para que los siguientes INSERT continúen
-- después del último id cargado.
-- El script no borra datos; si los registros ya existen, la transacción
-- falla completa por llave primaria duplicada y no deja cambios parciales.
-- =====================================================================

SET client_encoding = 'UTF8';

BEGIN;

-- ---------------------------------------------------------------------
-- Automóviles: distintos colores, años, fabricantes y tipos.
-- ---------------------------------------------------------------------
INSERT INTO prq_automovil (id, color, anio, fabricante, tipo) VALUES
    (1, 'Rojo',   2019, 'Toyota',     'sedán'),
    (2, 'Blanco', 2021, 'Hyundai',    '4x4'),
    (3, 'Negro',  2015, 'Nissan',     'sedán'),
    (4, 'Azul',   2023, 'Yamaha',     'moto'),
    (5, 'Gris',   2018, 'Mitsubishi', '4x4'),
    (6, 'Verde',  2012, 'Honda',      'moto');

-- ---------------------------------------------------------------------
-- Parqueos: tres provincias y tres tarifas distintas (colones por hora).
-- ---------------------------------------------------------------------
INSERT INTO prq_parqueo (id, nombre_provincia, nombre, precio_por_hora) VALUES
    (1, 'San José', 'Parqueo Central Avenida Segunda', 1250.00),
    (2, 'Heredia',  'Parqueo Mercado de Heredia',       800.00),
    (3, 'Cartago',  'Parqueo Las Ruinas',               675.50);

-- ---------------------------------------------------------------------
-- Ingresos de automóviles (octubre de 2026).
-- Duración y monto esperados con cobro proporcional por minuto:
--   consecutivo  duración   precio    monto
--    1           150 min    1250.00   3125.00
--    2            45 min     800.00    600.00
--    3            30 min    1250.00    625.00
--    4           165 min     675.50   1857.63
--    5           480 min     800.00   6400.00
--    6            80 min    1250.00   1666.67
--    7            50 min     675.50    562.92
--    8            90 min     800.00   1200.00
--    9           270 min    1250.00   5625.00
--   10            45 min     675.50    506.63
--   11            90 min     800.00   1200.00
--   12            30 min    1250.00    625.00
--   13           180 min     675.50   2026.50
--   14            20 min     800.00    266.67
--   15           135 min    1250.00   2812.50
--   16           en curso   1250.00   NULL
--   17           en curso    800.00   NULL
-- ---------------------------------------------------------------------
INSERT INTO prq_ingreso_automovil
    (consecutivo, id_parqueo, id_automovil, fecha_hora_entrada, fecha_hora_salida) VALUES
    ( 1, 1, 1, '2026-10-01 07:45:00', '2026-10-01 10:15:00'),
    ( 2, 2, 2, '2026-10-01 09:00:00', '2026-10-01 09:45:00'),
    ( 3, 1, 4, '2026-10-02 08:10:00', '2026-10-02 08:40:00'),
    ( 4, 3, 5, '2026-10-03 13:20:00', '2026-10-03 16:05:00'),
    ( 5, 2, 3, '2026-10-04 10:00:00', '2026-10-04 18:00:00'),
    ( 6, 1, 2, '2026-10-05 12:00:00', '2026-10-05 13:20:00'),
    ( 7, 3, 6, '2026-10-06 07:00:00', '2026-10-06 07:50:00'),
    ( 8, 2, 1, '2026-10-07 15:30:00', '2026-10-07 17:00:00'),
    ( 9, 1, 3, '2026-10-08 08:00:00', '2026-10-08 12:30:00'),
    (10, 3, 4, '2026-10-09 18:15:00', '2026-10-09 19:00:00'),
    (11, 2, 5, '2026-10-10 09:40:00', '2026-10-10 11:10:00'),
    (12, 1, 6, '2026-10-12 06:55:00', '2026-10-12 07:25:00'),
    (13, 3, 1, '2026-10-14 11:00:00', '2026-10-14 14:00:00'),
    (14, 2, 4, '2026-10-15 16:45:00', '2026-10-15 17:05:00'),
    (15, 1, 5, '2026-10-16 10:30:00', '2026-10-16 12:45:00'),
    (16, 1, 2, '2026-10-17 08:05:00', NULL),
    (17, 2, 3, '2026-10-17 09:20:00', NULL);

-- ---------------------------------------------------------------------
-- Sincronizar las secuencias de identidad con los id insertados.
-- ---------------------------------------------------------------------
SELECT setval(pg_get_serial_sequence('prq_automovil', 'id'),
              (SELECT max(id) FROM prq_automovil));
SELECT setval(pg_get_serial_sequence('prq_parqueo', 'id'),
              (SELECT max(id) FROM prq_parqueo));
SELECT setval(pg_get_serial_sequence('prq_ingreso_automovil', 'consecutivo'),
              (SELECT max(consecutivo) FROM prq_ingreso_automovil));

COMMIT;

-- ---------------------------------------------------------------------
-- Verificación (solo lectura): conteos y montos calculados en SQL.
-- ---------------------------------------------------------------------
SELECT 'prq_automovil' AS tabla, count(*) AS registros FROM prq_automovil
UNION ALL
SELECT 'prq_parqueo', count(*) FROM prq_parqueo
UNION ALL
SELECT 'prq_ingreso_automovil', count(*) FROM prq_ingreso_automovil;

SELECT i.consecutivo,
       a.tipo,
       p.nombre_provincia,
       i.fecha_hora_entrada,
       i.fecha_hora_salida,
       round(extract(epoch FROM (i.fecha_hora_salida - i.fecha_hora_entrada)) / 60, 2) AS minutos,
       round(extract(epoch FROM (i.fecha_hora_salida - i.fecha_hora_entrada)) * p.precio_por_hora / 3600, 2) AS monto
FROM prq_ingreso_automovil i
JOIN prq_automovil a ON a.id = i.id_automovil
JOIN prq_parqueo   p ON p.id = i.id_parqueo
ORDER BY i.consecutivo;
