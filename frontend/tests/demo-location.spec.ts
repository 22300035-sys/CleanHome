import { test, expect } from '@playwright/test';

test('editar domicilio demo conserva identidad y propietario e impide campos vacios', async ({ page }) => {
  await page.goto('/inicio?demo=1');
  const result = await page.evaluate(async () => {
    const modulePath = '/src/services/demo.ts';
    const { DemoApi, makeDemoStore, demoAccount } = await import(modulePath);
    const account = demoAccount('Customer');
    const api = new DemoApi(makeDemoStore(), account);
    const original = await api.request('/locations', { method: 'POST', body: JSON.stringify({ label: 'Casa', address: 'Calle ficticia 1', city: 'Querétaro' }) });
    const updated = await api.request(`/locations/${original.id}`, { method: 'PUT', body: JSON.stringify({ id: 'otro-id', customerId: 'otro-cliente', label: ' Oficina ', address: ' Calle ficticia 2 ', city: ' Querétaro ' }) });
    let rejected = false;
    try { await api.request(`/locations/${original.id}`, { method: 'PUT', body: JSON.stringify({ label: 'Casa', address: 'Calle ficticia', city: ' ' }) }); }
    catch { rejected = true; }
    return { id: updated.id, owner: updated.customerId, expectedId: original.id, expectedOwner: account.id, label: updated.label, address: updated.address, city: updated.city, rejected };
  });
  expect(result.id).toBe(result.expectedId);
  expect(result.owner).toBe(result.expectedOwner);
  expect(result.label).toBe('Oficina');
  expect(result.address).toBe('Calle ficticia 2');
  expect(result.city).toBe('Querétaro');
  expect(result.rejected).toBe(true);
});
