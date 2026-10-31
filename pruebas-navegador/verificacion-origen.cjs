// Verificación en navegador del origen de datos elegido en Parqueo.Api/appsettings.json.
// Se ejecuta una vez con "OrigenDatos": "Json" y otra con "OrigenDatos": "BaseDatos", con el MVC
// apuntando siempre a la misma URL del API. El origen mostrado sale del encabezado X-Origen-Datos
// de cada respuesta del API (no es una etiqueta fija del MVC).
// Uso: node verificacion-origen.cjs <urlMvc> <origenEsperado> <archivoSalida> <carpetaCapturas>
const { chromium } = require('playwright');
const { crearRegistro, idsDeFilas, origenDelPie } = require('./comun.cjs');

const [urlMvc = 'http://localhost:5130', origenEsperado = 'Json', salida = 'verificacion.txt', capturas = 'capturas'] = process.argv.slice(2);
const r = crearRegistro(salida, capturas, `verificacion-${origenEsperado.toLowerCase()}`);

const normalizar = (t) => (t ?? '').replace(/\s+/g, ' ').trim();

(async () => {
  const navegador = await chromium.launch();
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 }, locale: 'es-CR' });
  const pagina = await contexto.newPage();
  const erroresConsola = [];
  pagina.on('console', (m) => { if (m.type() === 'error') erroresConsola.push(m.text()); });
  pagina.on('pageerror', (e) => erroresConsola.push(e.message));

  r.registrar(`Verificación del origen configurado en appsettings.json — MVC ${urlMvc} — origen esperado ${origenEsperado}`);
  r.registrar(`Chromium ${navegador.version()} (Playwright), fecha ${new Date().toISOString()}`);
  r.registrar('');

  try {
    r.registrar('1. Listado de automóviles filtrado por años (2015 a 2020, inclusivo)');
    await pagina.goto(`${urlMvc}/Automoviles?anioDesde=2015&anioHasta=2020`);
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-automoviles')) === '[1,3,5]', 'Automóviles 1, 3 y 5');
    r.verificar((await origenDelPie(pagina)) === origenEsperado, `El pie informa el origen ${origenEsperado} recibido en X-Origen-Datos`);
    await r.capturar(pagina, 'automoviles-2015-2020', `listado filtrado servido por el origen ${origenEsperado}`);

    r.registrar('2. Detalle del ingreso de referencia (80 minutos a 1250 por hora)');
    await pagina.goto(`${urlMvc}/Ingresos/Detalles/6`);
    const datos = normalizar(await pagina.textContent('#datos-ingreso'));
    r.verificar(datos.includes('80') && datos.includes('1,3333'), 'Duración de 80 minutos y 1,3333 horas', datos);
    r.verificar(datos.includes('₡1 666,67'), 'Monto a cancelar ₡1 666,67 calculado por el API', datos);
    r.verificar((await origenDelPie(pagina)) === origenEsperado, `El pie informa el origen ${origenEsperado}`);
    await r.capturar(pagina, 'ingreso-6', 'ingreso 6 con el monto de referencia');

    r.registrar('3. Maestro/detalle con JavaScript');
    await pagina.goto(`${urlMvc}/MaestroDetalle`);
    await pagina.selectOption('#tipo-automovil', '4x4');
    await pagina.click('#boton-consultar');
    await pagina.waitForFunction(() => !document.querySelector('#estado-maestro').textContent.startsWith('Cargando'));
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-maestro')) === '[2,5]', 'Tipo 4x4: automóviles 2 y 5');
    r.verificar(normalizar(await pagina.textContent('#estado-maestro')).includes(`Origen de datos informado por el API: ${origenEsperado}`),
      `El estado del maestro muestra el origen ${origenEsperado} recibido en la respuesta`);
    await pagina.click('#tabla-maestro tr[data-id="2"] button');
    await pagina.waitForFunction(() => !document.querySelector('#estado-detalle').textContent.startsWith('Cargando'));
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-detalle', 'data-consecutivo')) === '[2,6,16]', 'Automóvil 2: ingresos 2, 6 y 16');
    const desborde = await pagina.$eval('#tabla-detalle', (t) => t.parentElement.scrollWidth - t.parentElement.clientWidth);
    r.verificar(desborde <= 0, 'Las ocho columnas del detalle caben sin desplazamiento horizontal (ventana de 1280 px)', `${desborde}px de desborde`);
    const fila16 = (await pagina.$$eval('#tabla-detalle tr[data-consecutivo="16"] td', (c) => c.map((x) => x.textContent.trim())));
    r.verificar(fila16.slice(4).every((t) => /sin salida|en curso|pendiente/i.test(t)), 'Ingreso 16 en curso: salida y campos calculados sin valor', JSON.stringify(fila16));
    await r.capturar(pagina, 'maestro-detalle', `maestro/detalle servido por el origen ${origenEsperado}`);

    r.verificar(erroresConsola.length === 0, 'Sin errores en la consola del navegador', erroresConsola.join(' | '));
  } catch (error) {
    r.verificar(false, 'La verificación terminó sin excepciones', error.message);
  } finally {
    await navegador.close();
  }
  process.exitCode = r.terminar() === 0 ? 0 : 1;
})();
