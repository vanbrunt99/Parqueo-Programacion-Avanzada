// Prueba en navegador de la primera etapa: MVC de automóviles (filtro por años, paginación,
// detalles, crear, editar, borrar y conflicto por ingresos) y mantenimiento de ingresos.
// Uso: node etapa1-automoviles-ingresos.cjs <urlMvc> <origenEsperado> <archivoSalida> <carpetaCapturas>
const { chromium } = require('playwright');
const { crearRegistro, idsDeFilas, origenDelPie } = require('./comun.cjs');

const [urlMvc = 'http://localhost:5130', origenEsperado = 'Json', salida = 'etapa1.txt', capturas = 'capturas'] = process.argv.slice(2);
const prefijo = `etapa1-${origenEsperado.toLowerCase()}`;
const r = crearRegistro(salida, capturas, prefijo);

(async () => {
  const navegador = await chromium.launch();
  const pagina = await navegador.newPage({ viewport: { width: 1280, height: 860 }, locale: 'es-CR' });
  const erroresConsola = [];
  pagina.on('console', (m) => { if (m.type() === 'error') erroresConsola.push(m.text()); });
  pagina.on('pageerror', (e) => erroresConsola.push(e.message));

  r.registrar(`Prueba en navegador — etapa 1 — MVC ${urlMvc} — origen esperado ${origenEsperado}`);
  r.registrar(`Chromium ${navegador.version()} (Playwright), fecha ${new Date().toISOString()}`);
  r.registrar('');

  try {
    r.registrar('1. Inicio');
    await pagina.goto(`${urlMvc}/`);
    r.verificar((await pagina.textContent('#estado-api')).includes('en línea'), 'El inicio informa que el API está en línea');
    r.verificar((await origenDelPie(pagina)) === origenEsperado, `El pie muestra el origen ${origenEsperado} informado por X-Origen-Datos`);
    await r.capturar(pagina, 'inicio', 'página de inicio con el estado del API y el origen de datos');

    r.registrar('2. Filtro por intervalo de años');
    await pagina.click('nav >> text=Automóviles');
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-automoviles')) === '[1,2,3,4,5,6]', 'Sin filtro se listan los 6 automóviles');
    await pagina.fill('#anioDesde', '2015');
    await pagina.fill('#anioHasta', '2019');
    await pagina.click('#filtro-anios button[type=submit]');
    await pagina.waitForURL(/anioDesde=2015/);
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-automoviles')) === '[1,3,5]', 'Años 2015 a 2019 (incluidos): automóviles 1, 3 y 5');
    r.verificar((await pagina.textContent('#resumen-paginacion')).includes('de 3 automóviles'), 'El resumen indica 3 automóviles en total');
    await r.capturar(pagina, 'filtro-2015-2019', 'filtro por años 2015–2019 con botones Detalles, Editar y Borrar');

    await pagina.fill('#anioDesde', '2020');
    await pagina.fill('#anioHasta', '2010');
    await pagina.click('#filtro-anios button[type=submit]');
    await pagina.waitForURL(/anioDesde=2020/);
    r.verificar((await pagina.textContent('.validation-summary-errors')).includes('mayor que el año superior'), 'Intervalo invertido: se muestra el error y no se lista nada');
    r.verificar((await idsDeFilas(pagina, '#tabla-automoviles')).length === 0, 'Intervalo invertido: tabla vacía');
    await r.capturar(pagina, 'intervalo-invertido', 'validación del intervalo invertido (2020–2010)');

    await pagina.goto(`${urlMvc}/Automoviles?anioDesde=2030&anioHasta=2040`);
    r.verificar((await pagina.textContent('#sin-resultados')).includes('No hay automóviles'), 'Sin coincidencias: mensaje "No hay automóviles en el intervalo indicado"');

    r.registrar('3. Paginación conservando el filtro');
    await pagina.goto(`${urlMvc}/Automoviles?anioDesde=2012&tamanoPagina=4`);
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-automoviles')) === '[1,2,3,4]', 'Página 1 de tamaño 4: automóviles 1 a 4');
    await pagina.click('a[aria-label="Página siguiente"]');
    await pagina.waitForURL(/pagina=2/);
    r.verificar(pagina.url().includes('anioDesde=2012'), 'La página 2 conserva el filtro anioDesde=2012');
    r.verificar(JSON.stringify(await idsDeFilas(pagina, '#tabla-automoviles')) === '[5,6]', 'Página 2: automóviles 5 y 6');
    r.verificar((await pagina.textContent('#resumen-paginacion')).includes('Mostrando 5–6 de 6 automóviles'), 'El resumen muestra 5–6 de 6');
    await r.capturar(pagina, 'paginacion', 'segunda página con el filtro conservado');

    r.registrar('4. Detalles con la propiedad de navegación ingresos');
    await pagina.goto(`${urlMvc}/Automoviles/Detalles/2`);
    const filasIngresos = await idsDeFilas(pagina, '#tabla-ingresos-automovil', 'data-consecutivo');
    r.verificar(JSON.stringify(filasIngresos) === '[2,6,16]', 'El automóvil 2 muestra sus ingresos 2, 6 y 16');
    const fila6 = await pagina.textContent('#tabla-ingresos-automovil tr[data-consecutivo="6"]');
    r.verificar(fila6.includes('80') && fila6.includes('1,3333') && fila6.includes('666,67'), 'Ingreso 6: 80 min, 1,3333 h y ₡1 666,67 (calculados por el API)', fila6.replace(/\s+/g, ' '));
    const fila16 = await pagina.textContent('#tabla-ingresos-automovil tr[data-consecutivo="16"]');
    r.verificar(fila16.includes('Sin salida') && fila16.includes('En curso') && !fila16.includes('NaN'), 'Ingreso 16 sin salida: "Sin salida"/"En curso", sin ceros ni NaN');
    await r.capturar(pagina, 'detalles-automovil-2', 'detalles del automóvil 2 con sus ingresos y montos');

    r.registrar('5. Validación del formulario (lado del cliente y del servidor)');
    await pagina.goto(`${urlMvc}/Automoviles/Crear`);
    await pagina.click('button[type=submit]');
    r.verificar((await pagina.textContent('form')).includes('El color es obligatorio.'), 'Formulario vacío: mensajes de validación sin enviar al API');
    await r.capturar(pagina, 'crear-validacion', 'validación del formulario de creación');

    r.registrar('6. Crear, editar y borrar un automóvil temporal');
    await pagina.fill('#Color', 'Turquesa');
    await pagina.fill('#Anio', '2022');
    await pagina.fill('#Fabricante', 'Kia');
    await pagina.selectOption('#Tipo', 'sedán');
    await pagina.click('button[type=submit]');
    await pagina.waitForURL(/\/Automoviles\/Detalles\/\d+/);
    const id = Number(pagina.url().match(/Detalles\/(\d+)/)[1]);
    const aviso = await pagina.textContent('.alert-success');
    r.verificar(aviso.includes(`#${id}`) && aviso.includes('201') && aviso.includes(origenEsperado), `Creado el automóvil #${id} (POST 201, origen ${origenEsperado})`, aviso.trim());
    await r.capturar(pagina, 'creado', `automóvil #${id} creado mediante POST /api/automoviles`);

    await pagina.click('main a:has-text("Editar")');
    await pagina.fill('#Color', 'Turquesa oscuro');
    await pagina.selectOption('#Tipo', '4x4');
    await r.capturar(pagina, 'editar', `formulario de edición del automóvil #${id}`);
    await pagina.click('button[type=submit]');
    await pagina.waitForURL(new RegExp(`/Automoviles/Detalles/${id}`));
    const datos = await pagina.textContent('#datos-automovil');
    r.verificar(datos.includes('Turquesa oscuro') && datos.includes('4x4'), 'La edición se guardó con PUT sobre el mismo id');
    r.verificar((await pagina.textContent('.alert-success')).includes('200'), 'El aviso muestra la respuesta 200 del PUT');

    await pagina.click('main a:has-text("Borrar")');
    r.verificar((await pagina.textContent('main')).includes('¿Desea borrar este automóvil?'), 'El GET de borrar solo pide confirmación');
    await r.capturar(pagina, 'confirmar-borrado', `confirmación antes de borrar el automóvil #${id}`);
    await pagina.click('button:has-text("Confirmar borrado")');
    await pagina.waitForURL(/\/Automoviles(\?|$)/);
    r.verificar((await pagina.textContent('.alert-success')).includes('204'), 'Borrado con DELETE (204) y regreso al listado');
    const respuestaBorrado = await pagina.request.get(`${urlMvc}/Automoviles/Detalles/${id}`);
    r.verificar(respuestaBorrado.status() === 404, `El automóvil #${id} ya no existe (404)`);

    r.registrar('7. Integridad referencial');
    await pagina.goto(`${urlMvc}/Automoviles/Borrar/1`);
    await pagina.click('button:has-text("Confirmar borrado")');
    await pagina.waitForSelector('#error-borrado');
    r.verificar((await pagina.textContent('#error-borrado')).includes('No se puede borrar'), 'Borrar el automóvil 1 (con ingresos) muestra el conflicto 409 del API');
    await r.capturar(pagina, 'conflicto-borrado', 'el API rechaza borrar un automóvil con ingresos');
    await pagina.goto(`${urlMvc}/Automoviles/Detalles/1`);
    r.verificar((await idsDeFilas(pagina, '#tabla-ingresos-automovil', 'data-consecutivo')).length === 3, 'El automóvil 1 conserva sus 3 ingresos');

    r.registrar('8. Mantenimiento de ingresos');
    await pagina.goto(`${urlMvc}/Ingresos`);
    r.verificar((await pagina.textContent('#resumen-paginacion')).includes('Mostrando 1–10 de 17 ingresos'), 'Listado de ingresos paginado: 1–10 de 17');
    await r.capturar(pagina, 'ingresos', 'listado de ingresos con duración y monto del API');
    await pagina.click('main a:has-text("Registrar ingreso")');
    await pagina.selectOption('#IdAutomovil', '4');
    await pagina.selectOption('#IdParqueo', '1');
    await pagina.fill('#FechaHoraEntrada', '2026-10-31T08:00');
    await r.capturar(pagina, 'ingreso-crear', 'registro de un ingreso con la salida vacía');
    await pagina.click('button[type=submit]');
    await pagina.waitForURL(/\/Ingresos\/Detalles\/\d+/);
    const consecutivo = Number(pagina.url().match(/Detalles\/(\d+)/)[1]);
    const datosIngreso = await pagina.textContent('#datos-ingreso');
    r.verificar(datosIngreso.includes('31/10/2026 08:00') && datosIngreso.includes('Sin salida'), `Ingreso #${consecutivo}: entrada 31/10/2026 08:00 sin conversión horaria y salida null`);

    await pagina.click('main a:has-text("Editar")');
    await pagina.fill('#FechaHoraSalida', '2026-10-31T07:00');
    await pagina.click('button[type=submit]');
    await pagina.waitForSelector('.validation-summary-errors');
    r.verificar((await pagina.textContent('.validation-summary-errors')).includes('posterior'), 'Salida anterior a la entrada: se muestra el error del API');
    r.verificar((await pagina.inputValue('#FechaHoraSalida')) === '2026-10-31T07:00', 'Se conserva lo escrito en el formulario');
    await r.capturar(pagina, 'ingreso-error-salida', 'el API rechaza una salida anterior a la entrada');
    await pagina.fill('#FechaHoraSalida', '2026-10-31T09:20');
    await pagina.click('button[type=submit]');
    await pagina.waitForURL(new RegExp(`/Ingresos/Detalles/${consecutivo}`));
    const conSalida = await pagina.textContent('#datos-ingreso');
    r.verificar(conSalida.includes('80') && conSalida.includes('1,3333') && conSalida.includes('666,67'), 'Con salida a las 09:20: 80 min, 1,3333 h y ₡1 666,67 calculados por el API');
    await r.capturar(pagina, 'ingreso-con-salida', `ingreso #${consecutivo} con la salida registrada`);
    await pagina.click('main a:has-text("Borrar")');
    await pagina.click('button:has-text("Confirmar borrado")');
    await pagina.waitForURL(/\/Ingresos/);
    r.verificar((await pagina.request.get(`${urlMvc}/Ingresos/Detalles/${consecutivo}`)).status() === 404, `Ingreso temporal #${consecutivo} borrado`);
    r.verificar((await pagina.request.get(`${urlMvc}/Ingresos/Detalles/999`)).status() === 404, 'Consecutivo inexistente (999): 404');

    r.registrar('9. Segundos de un registro: guardar sin cambios no los recorta');
    await pagina.goto(`${urlMvc}/Ingresos/Crear`);
    await pagina.selectOption('#IdAutomovil', '4');
    await pagina.selectOption('#IdParqueo', '1');
    await pagina.fill('#FechaHoraEntrada', '2026-10-31T08:00:45');
    await pagina.fill('#FechaHoraSalida', '2026-10-31T09:20:15');
    await pagina.click('button[type=submit]');
    await pagina.waitForURL(/\/Ingresos\/Detalles\/\d+/);
    const conSegundos = Number(pagina.url().match(/Detalles\/(\d+)/)[1]);
    r.verificar((await pagina.textContent('#datos-ingreso')).includes('31/10/2026 08:00:45'), `Ingreso #${conSegundos} con segundos: se muestran en los detalles`);
    await pagina.click('main a:has-text("Editar")');
    r.verificar((await pagina.inputValue('#FechaHoraEntrada')) === '2026-10-31T08:00:45' && (await pagina.inputValue('#FechaHoraSalida')) === '2026-10-31T09:20:15',
      'El formulario de edición conserva los segundos de entrada y salida');
    await pagina.click('button[type=submit]');
    await pagina.waitForURL(new RegExp(`/Ingresos/Detalles/${conSegundos}`));
    const trasGuardar = (await pagina.textContent('#datos-ingreso')).replace(/\s+/g, ' ');
    r.verificar(trasGuardar.includes('08:00:45') && trasGuardar.includes('09:20:15') && trasGuardar.includes('79,5') && trasGuardar.includes('656,25'),
      'Guardar sin cambios mantiene 79,5 minutos y ₡1 656,25 calculados por el API', trasGuardar);
    await pagina.click('main a:has-text("Borrar")');
    await pagina.click('button:has-text("Confirmar borrado")');
    await pagina.waitForURL(/\/Ingresos/);
    r.verificar((await pagina.request.get(`${urlMvc}/Ingresos/Detalles/${conSegundos}`)).status() === 404, `Ingreso temporal #${conSegundos} borrado`);

    r.registrar('');
    r.verificar(erroresConsola.length === 0, 'Sin errores de JavaScript en la consola del navegador', erroresConsola.join(' | '));
  } catch (error) {
    r.verificar(false, 'La prueba terminó sin excepciones', error.message);
    await r.capturar(pagina, 'error', 'estado de la página al fallar');
  } finally {
    await navegador.close();
  }

  process.exit(r.terminar() === 0 ? 0 : 1);
})();
