// Prueba en navegador de la segunda etapa: maestro/detalle con JavaScript.
// Uso: node etapa2-maestro-detalle.cjs <urlMvc> <urlApi> <origenEsperado> <archivoSalida> <carpetaCapturas>
const http = require('http');
const { chromium } = require('playwright');
const { crearRegistro, idsDeFilas } = require('./comun.cjs');

const [urlMvc = 'http://localhost:5130', urlApi = 'http://localhost:5246', origenEsperado = 'Json', salida = 'etapa2.txt', capturas = 'capturas'] = process.argv.slice(2);
const r = crearRegistro(salida, capturas, `etapa2-${origenEsperado.toLowerCase()}`);

const textoDe = (pagina, selector) => pagina.textContent(selector).then((t) => (t ?? '').replace(/\s+/g, ' ').trim());

async function consultarTipo(pagina, tipo) {
  await pagina.selectOption('#tipo-automovil', tipo);
  await pagina.click('#boton-consultar');
  await pagina.waitForFunction(() => !document.querySelector('#estado-maestro').textContent.startsWith('Cargando'));
}

async function seleccionar(pagina, id) {
  await pagina.click(`#tabla-maestro tr[data-id="${id}"] button`);
  await pagina.waitForFunction(() => !document.querySelector('#estado-detalle').textContent.startsWith('Cargando'));
}

(async () => {
  const navegador = await chromium.launch();
  const contexto = await navegador.newContext({ viewport: { width: 1280, height: 900 }, locale: 'es-CR' });
  const pagina = await contexto.newPage();
  const erroresConsola = [];
  pagina.on('console', (m) => { if (m.type() === 'error') erroresConsola.push(m.text()); });
  pagina.on('pageerror', (e) => erroresConsola.push(e.message));
  let idTemporal = null;

  r.registrar(`Prueba en navegador — etapa 2 (maestro/detalle con JavaScript) — MVC ${urlMvc} — API ${urlApi} — origen esperado ${origenEsperado}`);
  r.registrar(`Chromium ${navegador.version()} (Playwright), fecha ${new Date().toISOString()}`);
  r.registrar('');

  try {
    r.registrar('1. Pantalla inicial y combo de tipos');
    await pagina.goto(`${urlMvc}/MaestroDetalle`);
    const opciones = await pagina.$$eval('#tipo-automovil option', (o) => o.map((x) => x.value).filter((v) => v));
    r.verificar(JSON.stringify(opciones) === JSON.stringify(['sedán', '4x4', 'moto']), 'El combo ofrece los tipos sedán, 4x4 y moto (valores exactos)');
    r.verificar((await textoDe(pagina, '#estado-detalle')).includes('Seleccione un automóvil'), 'El detalle empieza vacío con una indicación');
    await pagina.click('#boton-consultar');
    r.verificar((await textoDe(pagina, '#estado-maestro')).includes('Seleccione un tipo'), 'Consultar sin tipo pide elegir uno');
    await r.capturar(pagina, 'inicial', 'pantalla maestro/detalle antes de consultar');

    r.registrar('2. Consulta por cada tipo');
    for (const [tipo, esperados] of [['sedán', [1, 3]], ['moto', [4, 6]], ['4x4', [2, 5]]]) {
      await consultarTipo(pagina, tipo);
      const ids = await idsDeFilas(pagina, '#tabla-maestro');
      r.verificar(JSON.stringify(ids) === JSON.stringify(esperados), `Tipo ${tipo}: automóviles ${esperados.join(' y ')}`, JSON.stringify(ids));
      r.verificar((await textoDe(pagina, '#estado-maestro')).includes(`Origen de datos informado por el API: ${origenEsperado}`), `Tipo ${tipo}: el estado muestra el origen ${origenEsperado}`);
    }
    await r.capturar(pagina, 'maestro-4x4', 'tabla maestra con los automóviles 4x4');

    r.registrar('3. Selección y detalle (solo los ingresos del automóvil elegido)');
    const urlAntes = pagina.url();
    await seleccionar(pagina, 2);
    r.verificar(pagina.url() === urlAntes, 'El detalle se actualizó sin recargar ni cambiar de página');
    r.verificar((await pagina.getAttribute('#tabla-detalle', 'data-id-automovil')) === '2', 'El detalle corresponde al automóvil 2');
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-detalle', 'data-consecutivo')) === '[2,6,16]', 'Automóvil 2: ingresos 2, 6 y 16 (orden de entrada)');
    const desborde = await pagina.$eval('#tabla-detalle', (t) => t.parentElement.scrollWidth - t.parentElement.clientWidth);
    r.verificar(desborde <= 0, 'Las ocho columnas del detalle caben sin desplazamiento horizontal (ventana de 1280 px)', `${desborde}px de desborde`);
    const fila6 = await pagina.$$eval('#tabla-detalle tr[data-consecutivo="6"] td', (c) => c.map((x) => x.textContent));
    r.verificar(fila6[1] === '1', 'Ingreso 6: id del parqueo 1 (tomado de ingreso.parqueo.id)', JSON.stringify(fila6));
    r.verificar(fila6[3] === '05/10/2026 12:00' && fila6[4] === '05/10/2026 13:20', 'Ingreso 6: entrada 05/10/2026 12:00 y salida 13:20, sin desfase horario', JSON.stringify(fila6));
    r.verificar(fila6[5] === '80' && fila6[6] === '1,3333' && fila6[7].replace(/\s/g, ' ').includes('1 666,67'), 'Ingreso 6: 80 minutos, 1,3333 horas y ₡1 666,67 (valores del API)', JSON.stringify(fila6));
    const fila16 = await pagina.$$eval('#tabla-detalle tr[data-consecutivo="16"] td', (c) => c.map((x) => x.textContent));
    r.verificar(fila16[4] === 'Sin salida (en curso)' && fila16[5] === 'En curso' && fila16[6] === 'En curso' && fila16[7].startsWith('Pendiente'),
      'Ingreso 16 sin salida: textos "Sin salida", "En curso" y "Pendiente" (no 0 ni NaN)', JSON.stringify(fila16));
    r.verificar((await pagina.getAttribute('#tabla-maestro tr[data-id="2"] button', 'aria-pressed')) === 'true', 'La fila seleccionada queda marcada (aria-pressed)');
    await r.capturar(pagina, 'detalle-automovil-2', 'detalle del automóvil 2 con id de parqueo, entrada, salida, minutos, horas y monto');

    await seleccionar(pagina, 5);
    r.verificar((await pagina.getAttribute('#tabla-detalle', 'data-id-automovil')) === '5', 'Al elegir otro automóvil, el detalle cambia al 5');
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-detalle', 'data-consecutivo')) === '[4,11,15]', 'Automóvil 5: ingresos 4, 11 y 15');
    r.verificar((await pagina.getAttribute('#tabla-maestro tr[data-id="2"] button', 'aria-pressed')) === 'false', 'La selección anterior se desmarca');
    await r.capturar(pagina, 'detalle-automovil-5', 'detalle del automóvil 5');

    r.registrar('4. Cambiar el filtro limpia la selección');
    await pagina.selectOption('#tipo-automovil', 'moto');
    r.verificar((await pagina.isHidden('#tabla-detalle')) && (await textoDe(pagina, '#estado-detalle')).includes('Seleccione un automóvil'), 'Al cambiar el tipo, el detalle anterior desaparece');
    r.verificar((await pagina.isHidden('#tabla-maestro')) && (await textoDe(pagina, '#estado-maestro')).includes('Presione Consultar'), 'La tabla del tipo anterior se limpia hasta consultar de nuevo');

    r.registrar('5. Automóvil sin ingresos');
    const creado = await pagina.request.post(`${urlApi}/api/automoviles`, { data: { color: 'Gris perla', anio: 2024, fabricante: 'Vespa', tipo: 'moto' } });
    idTemporal = (await creado.json()).id;
    r.verificar(creado.status() === 201, `Automóvil temporal #${idTemporal} creado en el API para la prueba`);
    await consultarTipo(pagina, 'moto');
    await seleccionar(pagina, idTemporal);
    r.verificar((await textoDe(pagina, '#estado-detalle')).includes('no tiene ingresos registrados') && (await pagina.isHidden('#tabla-detalle')), 'Sin ingresos: mensaje claro y sin tabla');
    await r.capturar(pagina, 'sin-ingresos', `automóvil #${idTemporal} sin ingresos`);

    r.registrar('6. Respuesta tardía: no sobrescribe la selección vigente');
    let demoradas = 0;
    await pagina.route('**/MaestroDetalle/Ingresos/4', async (ruta) => {
      demoradas++;
      await new Promise((resolver) => setTimeout(resolver, 1500));
      try { await ruta.continue(); } catch { /* la solicitud ya fue cancelada por el navegador */ }
    });
    await pagina.click('#tabla-maestro tr[data-id="4"] button');
    await pagina.click('#tabla-maestro tr[data-id="6"] button');
    await pagina.waitForFunction(() => document.querySelector('#tabla-detalle').dataset.idAutomovil === '6');
    await pagina.waitForTimeout(2000); // deja que la respuesta del 4 (demorada) termine o se cancele
    r.verificar(demoradas === 1, 'La solicitud del automóvil 4 se demoró 1,5 s a propósito');
    r.verificar((await pagina.getAttribute('#tabla-detalle', 'data-id-automovil')) === '6', 'Tras la respuesta tardía, el detalle sigue siendo del automóvil 6');
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-detalle', 'data-consecutivo')) === '[7,12]', 'Se muestran solo los ingresos del 6 (7 y 12)');
    r.verificar((await textoDe(pagina, '#titulo-detalle')).includes('#6'), 'El título del panel indica el automóvil 6');
    await pagina.unroute('**/MaestroDetalle/Ingresos/4');
    await r.capturar(pagina, 'respuesta-tardia', 'el detalle conserva la última selección (automóvil 6)');

    r.registrar('7. Errores HTTP y de conexión');
    await pagina.route('**/MaestroDetalle/Ingresos/4', (ruta) => ruta.fulfill({
      status: 503, contentType: 'application/problem+json',
      body: JSON.stringify({ title: 'API no disponible', status: 503, detail: 'No se pudo comunicar con el API del parqueo (simulado en la prueba).' }),
    }));
    await seleccionar(pagina, 4);
    r.verificar((await textoDe(pagina, '#estado-detalle')).includes('No se pudieron cargar los ingresos del automóvil #4') && (await pagina.isHidden('#tabla-detalle')),
      'Error 503: mensaje en español sin datos engañosos');
    await r.capturar(pagina, 'error-503', 'mensaje ante un error HTTP 503');
    await pagina.unroute('**/MaestroDetalle/Ingresos/4');
    await pagina.route('**/MaestroDetalle/Automoviles**', (ruta) => ruta.abort('connectionrefused'));
    await consultarTipo(pagina, 'sedán');
    r.verificar((await textoDe(pagina, '#estado-maestro')).includes('No se pudo conectar con el servidor'), 'Sin conexión: mensaje "No se pudo conectar con el servidor"');
    await pagina.unroute('**/MaestroDetalle/Automoviles**');
    await consultarTipo(pagina, 'sedán');
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-maestro')) === '[1,3]', 'Al restablecerse la conexión, la consulta vuelve a funcionar');

    r.registrar('8. Seguridad: los datos se escriben como texto');
    const malicioso = await pagina.request.post(`${urlApi}/api/automoviles`, { data: { color: '<img src=x onerror=alert(1)>', anio: 2024, fabricante: '<b>X</b>', tipo: 'sedán' } });
    const idMalicioso = (await malicioso.json()).id;
    let dialogos = 0;
    pagina.on('dialog', async (d) => { dialogos++; await d.dismiss(); });
    await consultarTipo(pagina, 'sedán');
    const celdaColor = await pagina.$eval(`#tabla-maestro tr[data-id="${idMalicioso}"] td:nth-child(2)`, (c) => ({ texto: c.textContent, hijos: c.children.length }));
    r.verificar(celdaColor.texto === '<img src=x onerror=alert(1)>' && celdaColor.hijos === 0 && dialogos === 0, 'Un color con HTML se muestra como texto (textContent): no se ejecuta ni se interpreta');
    await pagina.request.delete(`${urlApi}/api/automoviles/${idMalicioso}`);

    r.registrar('9. Cambio de tipo mientras llega una respuesta lenta (encabezados enseguida, cuerpo 1,5 s después)');
    // Proxy local delante del MVC: reproduce una red lenta en la que la cancelación ocurre mientras
    // se lee el cuerpo (no antes de la respuesta), el caso que una ruta simulada no cubre.
    const destino = new URL(urlMvc);
    const proxy = http.createServer((solicitud, respuesta) => {
      const reenvio = http.request({ host: destino.hostname, port: destino.port, path: solicitud.url, method: solicitud.method, headers: solicitud.headers }, (original) => {
        const lenta = /^\/MaestroDetalle\/(Ingresos|Automoviles)/.test(solicitud.url);
        const encabezados = { ...original.headers };
        delete encabezados['content-length'];
        respuesta.writeHead(original.statusCode, encabezados);
        if (lenta) respuesta.flushHeaders();
        const trozos = [];
        original.on('data', (trozo) => (lenta ? trozos.push(trozo) : respuesta.write(trozo)));
        original.on('end', () => (lenta ? setTimeout(() => respuesta.end(Buffer.concat(trozos)), 1500) : respuesta.end()));
      });
      reenvio.on('error', () => respuesta.destroy());
      solicitud.pipe(reenvio);
    });
    await new Promise((listo) => proxy.listen(0, '127.0.0.1', listo));
    const lenta = await contexto.newPage();
    lenta.on('pageerror', (e) => erroresConsola.push(e.message));
    try {
      await lenta.goto(`http://127.0.0.1:${proxy.address().port}/MaestroDetalle`);
      await lenta.selectOption('#tipo-automovil', 'sedán');
      await lenta.click('#boton-consultar');
      await lenta.waitForTimeout(500);
      r.verificar((await textoDe(lenta, '#estado-maestro')).startsWith('Cargando'), 'Consulta de sedán en curso: encabezados recibidos, cuerpo demorado');
      await lenta.selectOption('#tipo-automovil', 'moto');
      await lenta.waitForTimeout(2000);
      const estadoMaestro = await textoDe(lenta, '#estado-maestro');
      r.verificar(estadoMaestro.startsWith('Presione Consultar'), 'Cambiar el tipo durante la carga no muestra un error falso de conexión', estadoMaestro);
      r.verificar(await lenta.isHidden('#tabla-maestro'), 'No aparecen automóviles sedán con el tipo moto elegido');
      await lenta.click('#boton-consultar');
      await lenta.waitForFunction(() => !document.querySelector('#estado-maestro').textContent.startsWith('Cargando'), null, { timeout: 10000 });
      await lenta.click('#tabla-maestro tr[data-id="4"] button');
      await lenta.waitForTimeout(500);
      r.verificar((await textoDe(lenta, '#estado-detalle')).startsWith('Cargando'), 'Detalle del automóvil 4 en curso');
      await lenta.selectOption('#tipo-automovil', '4x4');
      await lenta.waitForTimeout(2000);
      const estadoDetalle = await textoDe(lenta, '#estado-detalle');
      r.verificar(estadoDetalle.startsWith('Seleccione un automóvil') && (await lenta.isHidden('#tabla-detalle')),
        'Cambiar el tipo mientras llega el detalle deja el panel limpio, sin error falso ni ingresos del automóvil anterior', estadoDetalle);
    } finally {
      await lenta.close();
      proxy.close();
    }

    r.registrar('');
    r.verificar(erroresConsola.filter((e) => !e.includes('503') && !e.includes('ERR_CONNECTION_REFUSED')).length === 0,
      'Sin errores de JavaScript en la consola (salvo los provocados a propósito: 503 y conexión rechazada)', erroresConsola.join(' | '));
  } catch (error) {
    r.verificar(false, 'La prueba terminó sin excepciones', error.message);
    await r.capturar(pagina, 'error', 'estado de la página al fallar');
  } finally {
    if (idTemporal) {
      await pagina.request.delete(`${urlApi}/api/automoviles/${idTemporal}`);
    }
    await navegador.close();
  }

  process.exit(r.terminar() === 0 ? 0 : 1);
})();
