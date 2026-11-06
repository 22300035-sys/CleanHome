import { test, expect, type Page } from '@playwright/test';
import { mkdir } from 'node:fs/promises';
import path from 'node:path';

const screenshotDir = path.resolve('..', 'docs', 'screenshots');
test('pantallas de cliente, trabajador y administración sin imágenes rotas ni desbordamientos', async ({ page }) => {
  const errors: string[] = [];
  page.on('pageerror', error => errors.push(error.message));
  const screens = [
    { name: 'acceso', url: '/acceso', text: 'Acceso a tu cuenta' },
    { name: 'inicio-cliente', url: '/inicio?demo=1', text: '¿Qué necesitas limpiar?' },
    { name: 'solicitar-paso-1', url: '/solicitar/1?demo=1', text: '¿Qué necesitas limpiar?' },
    { name: 'solicitar-paso-2', url: '/solicitar/2?demo=1', text: 'Cuéntanos sobre el lugar' },
    { name: 'solicitar-paso-3', url: '/solicitar/3?demo=1', text: '¿Cuándo y dónde necesitas el servicio?' },
    { name: 'cotizaciones', url: '/solicitudes/request-open/cotizaciones?demo=1', text: 'Cotizaciones recibidas' },
    { name: 'perfil-profesional', url: '/profesionales/d1111111-1111-4111-8111-111111111111?demo=1', text: 'Perfil profesional' },
    { name: 'reservas', url: '/reservas?demo=1', text: 'Mis reservas' },
    { name: 'pago', url: '/reservas/booking-next/pago?demo=1', text: 'Resumen de reserva y pago' },
    { name: 'perfil-cliente', url: '/perfil?demo=1', text: 'Mi perfil' },
    { name: 'notificaciones', url: '/notificaciones?demo=1', text: 'Notificaciones' },
    { name: 'chat', url: '/mensajes/booking-active?demo=1', text: 'Sophia Carter' },
    { name: 'radar-trabajador', url: '/inicio?demo=1&role=Worker', text: 'Hola, Sophia' },
    { name: 'enviar-cotizacion', url: '/cotizar/request-radar?demo=1&role=Worker', text: 'Enviar cotización' },
    { name: 'ganancias', url: '/ganancias?demo=1&role=Worker', text: 'Pagos de tus servicios' },
    { name: 'documentos', url: '/documentos?demo=1&role=Worker', text: 'Mis documentos' },
  ];
  await mkdir(screenshotDir, { recursive: true });
  for (const screen of screens) {
    await page.goto(screen.url);
    await expect(page.getByRole('heading', { name: screen.text, exact: false }).first()).toBeVisible();
    await page.evaluate(() => document.fonts.ready);
    await expect(page.locator('.loading')).toHaveCount(0);
    await expect(page.getByText('No pudimos mostrar esta pantalla')).toHaveCount(0);
    await expect.poll(() => page.locator('img').evaluateAll(images => images.every(image => image.complete && image.naturalWidth > 0))).toBe(true);
    const overflow = await page.evaluate(() => document.documentElement.scrollWidth > window.innerWidth + 1);
    expect(overflow, screen.name + ' desborda horizontalmente').toBe(false);
    const geometry = await page.locator('.icon img:visible').evaluateAll(images => images.every(image => Math.abs(image.getBoundingClientRect().width - Number(image.getAttribute('width'))) < 1.5 && Math.abs(image.getBoundingClientRect().height - Number(image.getAttribute('height'))) < 1.5));
    expect(geometry, screen.name + ' altera la geometría de SVG').toBe(true);
    await page.screenshot({ path: path.join(screenshotDir, screen.name + '-movil.png'), fullPage: false });
  }
  await page.setViewportSize({ width: 1440, height: 1000 });
  await page.goto('/admin?demo=1&role=Admin');
  await expect(page.getByRole('heading', { name: 'Panel administrativo CleanHome' })).toBeVisible();
  await expect(page.getByRole('table')).toBeVisible();
  await page.screenshot({ path: path.join(screenshotDir, 'administracion-escritorio.png'), fullPage: true });
  expect(errors).toEqual([]);
});

test('recorrido completo de demostración: solicitud, cotización, pago simulado y servicio', async ({ page }) => {
  const apiCalls: string[] = [];
  page.on('request', request => { if (new URL(request.url()).pathname.startsWith('/api/')) apiCalls.push(request.url()); });
  await page.goto('/inicio?demo=1');
  await page.getByRole('link', { name: '+ Solicitar limpieza', exact: true }).first().click();
  await page.getByRole('button', { name: /Limpieza profunda Desinfección/ }).click();
  await page.getByRole('button', { name: 'Continuar al Paso 2' }).click();
  await page.getByLabel('Instrucciones especiales o áreas prioritarias').fill('Atender especialmente los canceles y la campana.');
  await page.getByRole('button', { name: 'Continuar al Paso 3' }).click();
  await page.getByLabel('Indicaciones de acceso y referencias').fill('Código privado de prueba 7788');
  await page.getByRole('button', { name: 'Revisar y publicar solicitud' }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Publicar solicitud', exact: true }).click();
  await expect(page.getByRole('heading', { name: 'Tu solicitud ya está en el radar' })).toBeVisible();
  const requestPath = new URL(page.url()).pathname;
  await page.getByLabel('Cambiar perspectiva').selectOption('Worker');
  await expect(page.getByText('Código privado de prueba 7788')).toHaveCount(0);
  const requestCard = page.locator('.request-card').filter({ has: page.getByRole('heading', { name: 'Limpieza general + Limpieza profunda' }) });
  await requestCard.getByRole('link', { name: 'Enviar cotización' }).click();
  await page.getByLabel('Tarifa total propuesta').fill('650.50');
  await page.getByRole('button', { name: 'Enviar cotización', exact: false }).last().click();
  await page.getByRole('dialog').getByRole('button', { name: 'Enviar cotización', exact: true }).click();
  await expect(page.getByRole('status').filter({ hasText: 'Cotización enviada' })).toBeVisible();
  await page.getByLabel('Cambiar perspectiva').selectOption('Customer');
  await page.locator(`a[href="${requestPath}"]`).first().click();
  await page.getByRole('button', { name: 'Aceptar cotización', exact: false }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Aceptar y revisar reserva' }).click();
  await expect(page.getByRole('heading', { name: 'Resumen de reserva y pago' })).toBeVisible();
  await page.getByRole('radio', { name: /Mercado Pago/ }).check();
  await page.getByRole('button', { name: /Simular pago/ }).click();
  await expect(page.getByText('Pagado', { exact: true })).toBeVisible();
  const bookingPath = new URL(page.url()).pathname;
  await page.getByLabel('Cambiar perspectiva').selectOption('Worker');
  await page.getByRole('link', { name: 'Reservas', exact: true }).click();
  await page.locator(`a[href="${bookingPath}"]`).click();
  await page.getByRole('button', { name: 'Iniciar servicio', exact: true }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Iniciar servicio', exact: true }).click();
  await expect(page.getByText('En progreso', { exact: true })).toBeVisible();
  await page.getByLabel('Cambiar perspectiva').selectOption('Customer');
  await page.getByRole('link', { name: 'Reservas', exact: true }).click();
  await page.locator(`a[href="${bookingPath}"]`).click();
  await page.getByRole('button', { name: /Confirmar servicio terminado/ }).click();
  await page.getByRole('dialog').getByRole('button', { name: 'Confirmar terminación' }).click();
  await expect(page.getByText('Completada', { exact: true })).toBeVisible();
  expect(apiCalls).toEqual([]);
});

test('chat persiste el mensaje de demostración y lo presenta como texto, sin ejecutar HTML', async ({ page }) => {
  await page.goto('/mensajes/booking-active?demo=1');
  const payload = '<img src=x onerror=alert(1)> Mensaje de prueba';
  await page.getByLabel('Escribe un mensaje', { exact: true }).fill(payload);
  await page.getByRole('button', { name: 'Enviar mensaje', exact: true }).click();
  await expect(page.locator('.message-bubble').filter({ hasText: payload })).toHaveCount(1);
  await expect(page.locator('.message-bubble img')).toHaveCount(0);
  await page.getByRole('link', { name: 'Volver a mensajes' }).click();
  await page.locator('a[href="/mensajes/booking-active"]').click();
  await expect(page.locator('.message-bubble').filter({ hasText: payload })).toHaveCount(1);
});

test('administración requiere una cuenta autorizada y la demostración no hace peticiones reales', async ({ page }) => {
  await page.goto('/admin');
  await expect(page).toHaveURL(/\/acceso$/);
  await page.goto('/admin?demo=1&role=Customer');
  await expect(page).toHaveURL(/\/inicio$/);
  await page.goto('/admin/documentos?demo=1&role=Admin');
  await page.getByRole('button', { name: 'Rechazar', exact: true }).click();
  await page.getByLabel('Observaciones').fill('Requiere imagen más legible.');
  await page.getByRole('dialog').getByRole('button', { name: 'Guardar revisión' }).click();
  await expect(page.getByText('Rechazado', { exact: true })).toBeVisible();
});

test('login muestra errores del servidor sin revelar tokens ni entrar a la cuenta', async ({ page }) => {
  await page.route('**/api/v1/auth/login', route => route.fulfill({ status: 401, contentType: 'application/problem+json', body: JSON.stringify({ title: 'Correo o contraseña incorrectos.' }) }));
  await page.goto('/acceso');
  await page.getByLabel('Correo electrónico', { exact: true }).fill('prueba@example.test');
  await page.getByLabel('Contraseña', { exact: true }).fill('Incorrecta!2026');
  await page.getByRole('button', { name: 'Iniciar sesión', exact: true }).click();
  await expect(page.getByRole('alert')).toContainText('Correo o contraseña incorrectos.');
  await expect(page).toHaveURL(/\/acceso$/);
});

test('cliente real: registro, dirección privada y publicación contra PostgreSQL aislado', async ({ page }) => {
  test.skip(process.env.CLEANHOME_LIVE_TESTS !== '1', 'Requiere API y cleanhome_test locales; no usa credenciales externas.');
  await page.goto('/acceso');
  await page.getByRole('button', { name: 'Crear Cuenta', exact: true }).click();
  await page.getByLabel('Nombre completo', { exact: true }).fill('Cliente de prueba visual');
  await page.getByLabel('Correo electrónico', { exact: true }).fill(`frontend-${Date.now()}@example.test`);
  await page.getByLabel('Contraseña', { exact: true }).fill('CleanHome-Visual!2026');
  await page.locator('form').getByRole('button', { name: 'Crear cuenta', exact: true }).click();
  await expect(page.getByRole('heading', { name: /Hola, Cliente/ })).toBeVisible();
  await page.getByRole('link', { name: 'Perfil', exact: true }).click();
  await page.getByRole('link', { name: '+ Agregar', exact: true }).click();
  await page.getByRole('button', { name: '+ Agregar nueva dirección' }).click();
  await page.getByLabel('Nombre de la dirección').fill('Casa de pruebas');
  await page.getByLabel('Ciudad y zona', { exact: true }).fill('CDMX');
  await page.getByLabel('Dirección completa', { exact: true }).fill('Domicilio ficticio de pruebas 123');
  await page.getByRole('button', { name: 'Guardar dirección', exact: true }).click();
  await expect(page.getByText('Casa de pruebas', { exact: true })).toBeVisible();
  await page.getByRole('link', { name: 'Inicio', exact: true }).click();
  await page.getByRole('link', { name: '+ Solicitar limpieza', exact: true }).first().click();
  await page.getByRole('button', { name: 'Continuar al Paso 2' }).click();
  await page.getByLabel('Instrucciones especiales o áreas prioritarias').fill('Atender la cocina en la prueba de conexión.');
  await page.getByRole('button', { name: 'Continuar al Paso 3' }).click();
  await page.getByLabel('Indicaciones de acceso y referencias').fill('Código privado de prueba 1234');
  await page.getByRole('button', { name: 'Revisar y publicar solicitud' }).click();
  const posted = page.waitForResponse(response => response.url().endsWith('/api/v1/requests') && response.request().method() === 'POST');
  await page.getByRole('dialog').getByRole('button', { name: 'Publicar solicitud', exact: true }).click();
  const response = await posted;
  expect(response.status()).toBe(201);
  const request = await response.json();
  expect(request.description).not.toContain('Código privado');
  await expect(page.getByRole('heading', { name: 'Tu solicitud ya está en el radar' })).toBeVisible();
  await page.screenshot({ path: path.join(screenshotDir, 'solicitud-real-postgresql.png'), fullPage: false });
});

test('PWA instala solo recursos estáticos y permite explorar la demostración sin conexión', async ({ page, context }) => {
  await page.goto('http://127.0.0.1:5174/inicio?demo=1');
  await expect(page.getByRole('heading', { name: '¿Qué necesitas limpiar?' })).toBeVisible();
  await page.evaluate(async () => { await navigator.serviceWorker.ready; });
  await expect.poll(() => page.evaluate(() => Boolean(navigator.serviceWorker.controller)), { timeout: 30000 }).toBe(true);
  const cached = await page.evaluate(async () => { const result: string[] = []; for (const key of await caches.keys()) for (const request of await (await caches.open(key)).keys()) result.push(request.url); return result; });
  expect(cached.length).toBeGreaterThan(20);
  expect(cached.some(url => new URL(url).pathname.startsWith('/api/'))).toBe(false);
  await context.setOffline(true);
  await page.goto('http://127.0.0.1:5174/reservas?demo=1');
  await expect(page.getByRole('heading', { name: 'Mis reservas' })).toBeVisible();
  await expect(page.getByRole('link', { name: 'Ver reserva', exact: true }).first()).toBeVisible();
  await context.setOffline(false);
});

test('responsive: los controles permanecen accesibles en 320, 768 y 1440 píxeles', async ({ page }) => {
  for (const width of [320, 768, 1440]) {
    await page.setViewportSize({ width, height: 900 });
    for (const url of ['/inicio?demo=1', '/solicitudes/request-open/cotizaciones?demo=1', '/perfil?demo=1', '/admin?demo=1&role=Admin']) {
      await page.goto(url);
      await expect(page.locator('.loading')).toHaveCount(0);
      expect(await page.evaluate(() => document.documentElement.scrollWidth <= innerWidth + 1), `${width}: ${url}`).toBe(true);
      await expect(page.getByText('No pudimos mostrar esta pantalla')).toHaveCount(0);
    }
  }
});

test('flujo conectado local: administración verifica al trabajador y el chat llega por SignalR', async ({ page, browser }) => {
  test.skip(process.env.CLEANHOME_LIVE_TESTS !== '1' || !process.env.CLEANHOME_TEST_ADMIN_EMAIL || !process.env.CLEANHOME_TEST_ADMIN_PASSWORD, 'Requiere cuentas ficticias y API contra cleanhome_test; nunca usa una cuenta de pagos.');
  const suffix = Date.now().toString();
  const workerContext = await browser.newContext({ baseURL: 'http://127.0.0.1:5173', viewport: { width: 390, height: 844 }, locale: 'es-MX', timezoneId: 'America/Mexico_City' });
  const adminContext = await browser.newContext({ baseURL: 'http://127.0.0.1:5173', viewport: { width: 1440, height: 1000 }, locale: 'es-MX', timezoneId: 'America/Mexico_City' });
  const workerPage = await workerContext.newPage();
  const adminPage = await adminContext.newPage();
  const signup = async (p: Page, worker: boolean) => {
    await p.goto('/acceso');
    if (worker) await p.getByRole('button', { name: /^Soy trabajador/ }).click();
    await p.getByRole('button', { name: 'Crear Cuenta', exact: true }).click();
    await p.getByLabel('Nombre completo', { exact: true }).fill((worker ? 'ProfesionalVisual ' : 'ClienteVisual ') + suffix);
    await p.getByLabel('Correo electrónico', { exact: true }).fill(`${worker ? 'worker' : 'customer'}-visual-${suffix}@example.test`);
    await p.getByLabel('Contraseña', { exact: true }).fill('CleanHome-Conectado!2026');
    await p.locator('form').getByRole('button', { name: 'Crear cuenta', exact: true }).click();
    await expect(p.getByRole('heading', { name: worker ? /Hola, ProfesionalVisual/ : /Hola, ClienteVisual/ })).toBeVisible();
  };
  try {
    await signup(page, false);
    await page.getByRole('link', { name: 'Perfil', exact: true }).click();
    await page.getByRole('link', { name: '+ Agregar', exact: true }).click();
    await page.getByRole('button', { name: '+ Agregar nueva dirección' }).click();
    await page.getByLabel('Nombre de la dirección').fill('Casa del flujo conectado');
    await page.getByLabel('Ciudad y zona', { exact: true }).fill('CDMX');
    await page.getByLabel('Dirección completa', { exact: true }).fill('Domicilio ficticio reservado de prueba 987');
    await page.getByRole('button', { name: 'Guardar dirección', exact: true }).click();
    await expect(page.getByText('Casa del flujo conectado', { exact: true })).toBeVisible();
    await page.getByRole('link', { name: 'Inicio', exact: true }).click();
    await page.getByRole('link', { name: '+ Solicitar limpieza', exact: true }).first().click();
    await page.getByRole('button', { name: 'Continuar al Paso 2' }).click();
    await page.getByLabel('Instrucciones especiales o áreas prioritarias').fill('Solicitud visual conectada ' + suffix);
    await page.getByRole('button', { name: 'Continuar al Paso 3' }).click();
    await page.getByRole('button', { name: 'Revisar y publicar solicitud' }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Publicar solicitud', exact: true }).click();
    await expect(page.getByRole('heading', { name: 'Tu solicitud ya está en el radar' })).toBeVisible();

    await signup(workerPage, true);
    await workerPage.getByRole('link', { name: 'Subir mis documentos' }).click();
    const filename = `evidencia-visual-${suffix}.pdf`;
    await workerPage.getByLabel('Archivo PDF, PNG o JPEG').setInputFiles({ name: filename, mimeType: 'application/pdf', buffer: Buffer.from('%PDF-1.4\nDOCUMENTO FICTICIO DE PRUEBA AUTOMATIZADA\n%%EOF') });
    await workerPage.getByRole('button', { name: 'Subir documento privado' }).click();
    await expect(workerPage.getByText(filename, { exact: true })).toBeVisible();
    await adminPage.goto('/acceso');
    await adminPage.getByLabel('Correo electrónico', { exact: true }).fill(process.env.CLEANHOME_TEST_ADMIN_EMAIL!);
    await adminPage.getByLabel('Contraseña', { exact: true }).fill(process.env.CLEANHOME_TEST_ADMIN_PASSWORD!);
    await adminPage.getByRole('button', { name: 'Iniciar sesión', exact: true }).click();
    await expect(adminPage.getByRole('heading', { name: 'Panel administrativo CleanHome' })).toBeVisible();
    await adminPage.getByRole('link', { name: 'Verificaciones', exact: true }).click();
    for (let i = 0; i < 15 && !await adminPage.getByText(filename, { exact: true }).count(); i++) {
      await expect(adminPage.locator('.loading')).toHaveCount(0);
      if (await adminPage.getByText(filename, { exact: true }).count()) break;
      await adminPage.getByRole('button', { name: 'Siguiente' }).click();
    }
    const documentCard = adminPage.locator('section.card').filter({ has: adminPage.getByRole('heading', { name: filename, exact: true }) });
    await documentCard.getByRole('button', { name: 'Aprobar', exact: true }).click();
    await adminPage.getByLabel('Observaciones').fill('Documento ficticio del ensayo local de interfaz.');
    await adminPage.getByRole('dialog').getByRole('button', { name: 'Guardar revisión' }).click();
    await expect(documentCard.getByText('Aprobado', { exact: true })).toBeVisible();
    await adminPage.getByRole('link', { name: 'Clientes y especialistas' }).click();
    const workerName = 'ProfesionalVisual ' + suffix;
    for (let i = 0; i < 15 && !await adminPage.getByText(workerName, { exact: true }).count(); i++) {
      await expect(adminPage.locator('.loading')).toHaveCount(0);
      if (await adminPage.getByText(workerName, { exact: true }).count()) break;
      await adminPage.getByRole('button', { name: 'Siguiente' }).click();
    }
    const workerCard = adminPage.locator('.admin-list article').filter({ hasText: workerName });
    await workerCard.getByRole('button', { name: 'Verificar', exact: true }).click();
    await adminPage.getByRole('dialog').getByRole('button', { name: 'Confirmar operación' }).click();
    await expect(workerCard.getByText('Verificado', { exact: true })).toBeVisible();
    await adminPage.screenshot({ path: path.join(screenshotDir, 'verificacion-real-administrador.png'), fullPage: false });
    await workerPage.getByRole('link', { name: 'Inicio', exact: true }).click();
    await workerPage.getByRole('button', { name: 'Ya envié mis documentos, actualizar estado' }).click();
    const opportunity = workerPage.locator('.request-card').filter({ hasText: 'Solicitud visual conectada ' + suffix });
    await expect(opportunity).toBeVisible();
    await expect(workerPage.getByText('Domicilio ficticio reservado de prueba 987')).toHaveCount(0);
    await opportunity.getByRole('link', { name: 'Enviar cotización' }).click();
    await workerPage.getByLabel('Tarifa total propuesta').fill('500.50');
    await workerPage.getByLabel('Mensaje y carta de presentación').fill('Propuesta ficticia para validar el recorrido visual conectado.');
    await workerPage.getByRole('button', { name: 'Enviar cotización', exact: false }).last().click();
    await workerPage.getByRole('dialog').getByRole('button', { name: 'Enviar cotización', exact: true }).click();
    await expect(workerPage.getByRole('status').filter({ hasText: 'Cotización enviada' })).toBeVisible();
    await page.getByRole('button', { name: 'Actualizar cotizaciones' }).click();
    await page.getByRole('button', { name: 'Aceptar cotización', exact: false }).click();
    await page.getByRole('dialog').getByRole('button', { name: 'Aceptar y revisar reserva' }).click();
    await expect(page.getByRole('heading', { name: 'Resumen de reserva y pago' })).toBeVisible();
    const bookingPath = new URL(page.url()).pathname.replace(/\/pago$/, '');
    await page.getByRole('link', { name: 'CleanHome, inicio' }).count();
    // Sin abrir checkout: esta prueba no llama a Stripe ni Mercado Pago.
    await page.getByRole('button', { name: 'Regresar', exact: true }).first().click();
    await page.getByRole('link', { name: 'Reservas', exact: true }).click();
    await page.locator(`a[href="${bookingPath}"]`).click();
    await page.getByRole('link', { name: 'Abrir chat de la reserva' }).click();
    await expect(page.getByText('Chat disponible', { exact: true })).toBeVisible();
    await workerPage.getByRole('link', { name: 'Reservas', exact: true }).click();
    await workerPage.locator(`a[href="${bookingPath}"]`).click();
    await workerPage.getByRole('link', { name: 'Abrir chat de la reserva' }).click();
    await expect(workerPage.getByText('Chat disponible', { exact: true })).toBeVisible();
    const message = 'Mensaje real por SignalR ' + suffix;
    await workerPage.getByLabel('Escribe un mensaje', { exact: true }).fill(message);
    await workerPage.getByRole('button', { name: 'Enviar mensaje', exact: true }).click();
    await expect(page.locator('.message-bubble').filter({ hasText: message })).toBeVisible({ timeout: 10000 });
    await page.screenshot({ path: path.join(screenshotDir, 'chat-real-signalr.png'), fullPage: false });
  } finally { await workerContext.close(); await adminContext.close(); }
});

