// Utilidades de las pruebas en navegador (Playwright + Chromium) del Trabajo 04.
// Se ejecutan contra el MVC y el API ya levantados; registran cada verificación en un
// archivo de texto y guardan capturas de pantalla reales.
const fs = require('fs');
const path = require('path');

function crearRegistro(archivoSalida, carpetaCapturas, prefijo) {
  const lineas = [];
  let fallas = 0;
  let numero = 0;
  fs.mkdirSync(carpetaCapturas, { recursive: true });

  const registrar = (texto) => { lineas.push(texto); console.log(texto); };

  return {
    registrar,
    verificar(condicion, descripcion, detalle = '') {
      if (condicion) {
        registrar(`  [OK]    ${descripcion}`);
      } else {
        fallas++;
        registrar(`  [FALLA] ${descripcion}${detalle ? ' — ' + detalle : ''}`);
      }
    },
    async capturar(pagina, nombre, descripcion) {
      numero++;
      const archivo = `${prefijo}-${String(numero).padStart(2, '0')}-${nombre}.png`;
      await pagina.screenshot({ path: path.join(carpetaCapturas, archivo), fullPage: true });
      registrar(`  [captura] ${archivo}: ${descripcion}`);
      return archivo;
    },
    terminar() {
      registrar('');
      registrar(fallas === 0 ? 'RESULTADO: todas las verificaciones pasaron.' : `RESULTADO: ${fallas} verificación(es) fallaron.`);
      fs.writeFileSync(archivoSalida, lineas.join('\n') + '\n', 'utf8');
      return fallas;
    },
  };
}

/** Ids de la columna data-* de las filas visibles de una tabla. */
async function idsDeFilas(pagina, selectorTabla, atributo = 'data-id') {
  return pagina.$$eval(`${selectorTabla} tbody tr[${atributo}]`, (filas, a) => filas.map((f) => Number(f.getAttribute(a))), atributo);
}

/** Origen informado en el pie de página (viene del encabezado X-Origen-Datos del API). */
async function origenDelPie(pagina) {
  const elemento = await pagina.$('#origen-datos [data-origen]');
  return elemento ? elemento.getAttribute('data-origen') : null;
}

module.exports = { crearRegistro, idsDeFilas, origenDelPie };
