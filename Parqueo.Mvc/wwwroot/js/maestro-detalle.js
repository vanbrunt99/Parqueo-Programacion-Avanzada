// Maestro/detalle del Trabajo 04: automóviles por tipo (maestro) e ingresos del automóvil
// seleccionado (detalle), sin recargar la página.
//
// El navegador solo llama a acciones del MVC en el mismo origen
// (/MaestroDetalle/Automoviles y /MaestroDetalle/Ingresos/{id}); el MVC obtiene los datos del
// API por HTTP. Todo dato recibido se escribe con textContent (nunca se arma HTML con datos).
(() => {
    'use strict';

    const urls = document.getElementById('maestro-detalle-urls').dataset;
    const formulario = document.getElementById('form-tipo');
    const comboTipo = document.getElementById('tipo-automovil');
    const botonConsultar = document.getElementById('boton-consultar');
    const estadoMaestro = document.getElementById('estado-maestro');
    const tablaMaestro = document.getElementById('tabla-maestro');
    const cuerpoMaestro = tablaMaestro.querySelector('tbody');
    const tituloDetalle = document.getElementById('titulo-detalle');
    const estadoDetalle = document.getElementById('estado-detalle');
    const tablaDetalle = document.getElementById('tabla-detalle');
    const cuerpoDetalle = tablaDetalle.querySelector('tbody');
    const enlaceDetalles = document.getElementById('enlace-detalles');

    // Control de solicitudes vigentes: cada consulta nueva, y también cada cancelación (cambio de
    // tipo, limpieza del detalle), cancela la anterior y aumenta un número de solicitud. Una
    // respuesta cuyo número ya no es el vigente se descarta, aunque llegue después de cancelar.
    let solicitudMaestro = { controlador: null, numero: 0 };
    let solicitudDetalle = { controlador: null, numero: 0 };
    let automovilSeleccionado = null;

    // ---------------------------------------------------------------- Formato (es-CR)
    const formatoMinutos = new Intl.NumberFormat('es-CR', { maximumFractionDigits: 2 });
    const formatoHoras = new Intl.NumberFormat('es-CR', { maximumFractionDigits: 4 });
    const formatoColones = new Intl.NumberFormat('es-CR', { style: 'currency', currency: 'CRC', minimumFractionDigits: 2 });

    /**
     * "2026-10-05T12:00:00" (hora local sin zona) → "05/10/2026 12:00", sin conversiones horarias.
     * Los segundos se muestran solo si el registro los tiene ("…T10:00:45" → "… 10:00:45").
     */
    function fechaHora(texto) {
        const partes = /^(\d{4})-(\d{2})-(\d{2})T(\d{2}):(\d{2})(?::(\d{2}))?/.exec(texto ?? '');
        if (!partes) {
            return String(texto ?? '');
        }

        const segundos = partes[6] && partes[6] !== '00' ? `:${partes[6]}` : '';
        return `${partes[3]}/${partes[2]}/${partes[1]} ${partes[4]}:${partes[5]}${segundos}`;
    }

    /** Un valor null (estadía en curso) se muestra como texto, nunca como 0 ni NaN. */
    const numeroONulo = (valor, formato, textoNulo) =>
        valor === null || valor === undefined ? textoNulo : formato.format(Number(valor));

    const etiquetaTipo = (tipo) => ({ 'sedán': 'Sedán', moto: 'Moto' }[tipo] ?? tipo);

    function celda(fila, texto, clase) {
        const td = document.createElement('td');
        td.textContent = texto;
        if (clase) {
            td.className = clase;
        }
        fila.append(td);
        return td;
    }

    function mostrarEstado(elemento, texto, tipo = 'info') {
        elemento.textContent = texto;
        elemento.className = `mb-2 ${{ info: 'text-muted', error: 'text-danger fw-semibold', ok: 'text-success' }[tipo]}`;
    }

    // ---------------------------------------------------------------- Comunicación con el MVC
    const esCancelacion = (error) => error?.name === 'AbortError';

    async function obtenerJson(url, senal) {
        const respuesta = await fetch(url, { headers: { Accept: 'application/json' }, signal: senal });
        let cuerpo = null;
        try {
            cuerpo = await respuesta.json();
        } catch (error) {
            // Una cancelación mientras llega el cuerpo se propaga como cancelación; un cuerpo que
            // no es JSON (por ejemplo, una página de error de un proxy) se trata como vacío.
            if (esCancelacion(error)) {
                throw error;
            }

            cuerpo = null;
        }

        if (!respuesta.ok) {
            const mensaje = cuerpo?.detail ?? cuerpo?.title ?? `El servidor respondió ${respuesta.status}.`;
            throw new Error(mensaje);
        }

        if (cuerpo === null) {
            throw new Error('El servidor envió una respuesta que no es JSON.');
        }

        return cuerpo;
    }

    /** Cancela la solicitud vigente e invalida su número: su respuesta ya no se mostrará. */
    function cancelar(anterior) {
        anterior.controlador?.abort();
        return { controlador: null, numero: anterior.numero + 1 };
    }

    function nuevaSolicitud(anterior) {
        const { numero } = cancelar(anterior);
        return { controlador: new AbortController(), numero };
    }

    const mensajeError = (error) => error instanceof TypeError
        ? 'No se pudo conectar con el servidor. Verifique que el MVC y el API estén en ejecución.'
        : error.message;

    // ---------------------------------------------------------------- Detalle
    function limpiarDetalle(texto = 'Seleccione un automóvil de la tabla superior.') {
        solicitudDetalle = cancelar(solicitudDetalle);
        automovilSeleccionado = null;
        cuerpoDetalle.replaceChildren();
        tablaDetalle.hidden = true;
        enlaceDetalles.hidden = true;
        tituloDetalle.textContent = 'Ingresos del automóvil seleccionado';
        mostrarEstado(estadoDetalle, texto);
        for (const boton of cuerpoMaestro.querySelectorAll('button[aria-pressed]')) {
            boton.setAttribute('aria-pressed', 'false');
            boton.closest('tr').classList.remove('table-primary');
        }
    }

    function marcarSeleccion(id) {
        for (const fila of cuerpoMaestro.rows) {
            const elegida = Number(fila.dataset.id) === id;
            fila.classList.toggle('table-primary', elegida);
            fila.querySelector('button').setAttribute('aria-pressed', String(elegida));
        }
    }

    function dibujarDetalle(datos) {
        const a = datos.automovil;
        tituloDetalle.textContent = `Ingresos del automóvil #${a.id} · ${a.fabricante} ${a.color} ${a.anio} (${etiquetaTipo(a.tipo)})`;
        enlaceDetalles.href = `${urls.urlDetalles}/${a.id}`;
        enlaceDetalles.hidden = false;
        tablaDetalle.dataset.idAutomovil = String(a.id);
        cuerpoDetalle.replaceChildren();

        if (datos.ingresos.length === 0) {
            tablaDetalle.hidden = true;
            mostrarEstado(estadoDetalle, `El automóvil #${a.id} no tiene ingresos registrados. (Origen: ${datos.origenDatos ?? 'no informado'})`);
            return;
        }

        for (const ingreso of datos.ingresos) {
            const fila = document.createElement('tr');
            fila.dataset.consecutivo = String(ingreso.consecutivo);
            const enCurso = ingreso.fechaHoraSalida === null;
            celda(fila, String(ingreso.consecutivo), 'numero');
            celda(fila, String(ingreso.idParqueo), 'numero');
            celda(fila, ingreso.nombreParqueo);
            celda(fila, fechaHora(ingreso.fechaHoraEntrada));
            celda(fila, enCurso ? 'Sin salida (en curso)' : fechaHora(ingreso.fechaHoraSalida), enCurso ? 'estado-en-curso' : '');
            celda(fila, numeroONulo(ingreso.duracionEstadiaMinutos, formatoMinutos, 'En curso'), 'numero');
            celda(fila, numeroONulo(ingreso.duracionEstadiaHoras, formatoHoras, 'En curso'), 'numero');
            celda(fila, numeroONulo(ingreso.montoACancelar, formatoColones, 'Pendiente (sin salida)'), 'numero');
            cuerpoDetalle.append(fila);
        }

        tablaDetalle.hidden = false;
        const enCurso = datos.ingresos.filter((i) => i.fechaHoraSalida === null).length;
        mostrarEstado(estadoDetalle,
            `${datos.ingresos.length} ingreso(s)${enCurso ? `, ${enCurso} en curso` : ''}. Origen de datos informado por el API: ${datos.origenDatos ?? 'no informado'}.`,
            'ok');
    }

    // Volver a seleccionar el mismo automóvil recarga su detalle (por ejemplo, tras un error).
    async function seleccionarAutomovil(id) {
        solicitudDetalle = nuevaSolicitud(solicitudDetalle);
        const { controlador, numero } = solicitudDetalle;
        automovilSeleccionado = id;
        marcarSeleccion(id);
        cuerpoDetalle.replaceChildren();
        tablaDetalle.hidden = true;
        enlaceDetalles.hidden = true;
        tituloDetalle.textContent = `Ingresos del automóvil #${id}`;
        mostrarEstado(estadoDetalle, `Cargando los ingresos del automóvil #${id}…`);

        try {
            const datos = await obtenerJson(`${urls.urlIngresos}/${encodeURIComponent(id)}`, controlador.signal);
            if (numero !== solicitudDetalle.numero || datos.automovil.id !== automovilSeleccionado) {
                return; // respuesta de una selección anterior: se descarta
            }

            dibujarDetalle(datos);
        } catch (error) {
            if (esCancelacion(error) || numero !== solicitudDetalle.numero) {
                return;
            }

            mostrarEstado(estadoDetalle, `No se pudieron cargar los ingresos del automóvil #${id}: ${mensajeError(error)}`, 'error');
        }
    }

    // ---------------------------------------------------------------- Maestro
    function dibujarMaestro(datos) {
        cuerpoMaestro.replaceChildren();
        if (datos.automoviles.length === 0) {
            tablaMaestro.hidden = true;
            mostrarEstado(estadoMaestro, `No hay automóviles de tipo ${etiquetaTipo(datos.tipo)}. (Origen: ${datos.origenDatos ?? 'no informado'})`);
            return;
        }

        for (const a of datos.automoviles) {
            const fila = document.createElement('tr');
            fila.dataset.id = String(a.id);
            fila.className = 'fila-seleccionable';
            celda(fila, String(a.id), 'numero');
            celda(fila, a.color);
            celda(fila, String(a.anio), 'numero');
            celda(fila, a.fabricante);
            celda(fila, etiquetaTipo(a.tipo));
            const acciones = celda(fila, '', 'text-end');
            const boton = document.createElement('button');
            boton.type = 'button';
            boton.className = 'btn btn-sm btn-outline-primary';
            boton.textContent = 'Ver ingresos';
            boton.setAttribute('aria-pressed', 'false');
            boton.setAttribute('aria-label', `Ver los ingresos del automóvil ${a.id}, ${a.fabricante} ${a.color}`);
            acciones.append(boton);
            cuerpoMaestro.append(fila);
        }

        tablaMaestro.hidden = false;
        mostrarEstado(estadoMaestro,
            `${datos.total} automóvil(es) de tipo ${etiquetaTipo(datos.tipo)}. Origen de datos informado por el API: ${datos.origenDatos ?? 'no informado'}. Seleccione uno para ver sus ingresos.`,
            'ok');
    }

    async function consultar() {
        const tipo = comboTipo.value;
        limpiarDetalle();
        cuerpoMaestro.replaceChildren();
        tablaMaestro.hidden = true;
        if (!tipo) {
            mostrarEstado(estadoMaestro, 'Seleccione un tipo de automóvil antes de consultar.', 'error');
            comboTipo.focus();
            return;
        }

        solicitudMaestro = nuevaSolicitud(solicitudMaestro);
        const { controlador, numero } = solicitudMaestro;
        botonConsultar.disabled = true;
        mostrarEstado(estadoMaestro, `Cargando los automóviles de tipo ${etiquetaTipo(tipo)}…`);
        try {
            const datos = await obtenerJson(`${urls.urlAutomoviles}?tipo=${encodeURIComponent(tipo)}`, controlador.signal);
            if (numero === solicitudMaestro.numero) {
                dibujarMaestro(datos);
            }
        } catch (error) {
            if (!esCancelacion(error) && numero === solicitudMaestro.numero) {
                mostrarEstado(estadoMaestro, `No se pudieron cargar los automóviles: ${mensajeError(error)}`, 'error');
            }
        } finally {
            if (numero === solicitudMaestro.numero) {
                botonConsultar.disabled = false;
            }
        }
    }

    // ---------------------------------------------------------------- Eventos
    formulario.addEventListener('submit', (evento) => {
        evento.preventDefault();
        consultar();
    });

    // Cambiar el tipo invalida la tabla y el detalle mostrados: no quedan datos de otro tipo.
    comboTipo.addEventListener('change', () => {
        solicitudMaestro = cancelar(solicitudMaestro);
        limpiarDetalle();
        cuerpoMaestro.replaceChildren();
        tablaMaestro.hidden = true;
        botonConsultar.disabled = false;
        mostrarEstado(estadoMaestro, comboTipo.value
            ? `Presione Consultar para ver los automóviles de tipo ${etiquetaTipo(comboTipo.value)}.`
            : 'Seleccione un tipo y presione Consultar.');
    });

    // Un clic en la fila o en su botón selecciona el automóvil (el botón también funciona con teclado).
    cuerpoMaestro.addEventListener('click', (evento) => {
        const fila = evento.target.closest('tr[data-id]');
        if (fila) {
            seleccionarAutomovil(Number(fila.dataset.id));
        }
    });
})();
