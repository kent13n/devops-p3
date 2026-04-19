import { test, expect } from '@playwright/test';
import * as path from 'path';

test.describe('Upload protégé + download avec mot de passe', () => {
  let email: string;
  const loginPassword = 'Password123!';
  const filePassword = 'secret123';

  test.beforeEach(async ({ page }) => {
    email = `e2e-pd-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@test.local`;

    await page.goto('/');
    await page.getByRole('button', { name: 'Se connecter' }).click();
    await page.getByRole('button', { name: 'Créer un compte' }).click();
    await page.locator('#register-email').fill(email);
    await page.locator('#register-password').fill(loginPassword);
    await page.locator('#register-confirm').fill(loginPassword);
    await page.getByRole('button', { name: 'Créer mon compte' }).click();
    await expect(page).toHaveURL(/\/my-files/);
  });

  test('mauvais mdp → erreur ; bon mdp → téléchargement déclenché', async ({ page, context }) => {
    await page.getByRole('button', { name: 'Ajouter des fichiers' }).click();
    const fileInput = page.locator('#file-input');
    await fileInput.setInputFiles(path.join(__dirname, 'fixtures/hello.txt'));
    await page.locator('#upload-password').fill(filePassword);
    await page.getByRole('button', { name: 'Téléverser' }).click();

    const link = page.locator('.download-link-box a');
    await expect(link).toBeVisible();
    const downloadUrl = await link.getAttribute('href');

    const downloadPage = await context.newPage();
    await downloadPage.goto(downloadUrl!);

    const passwordField = downloadPage.locator('#dl-password');
    await expect(passwordField).toBeVisible();

    await passwordField.fill('mauvais-mdp');
    await downloadPage.getByRole('button', { name: /Télécharger/ }).click();
    await expect(downloadPage.getByRole('alert')).toContainText(/incorrect|mot de passe/i);

    await passwordField.fill(filePassword);
    const downloadPromise = downloadPage.waitForEvent('download');
    await downloadPage.getByRole('button', { name: /Télécharger/ }).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toBe('hello.txt');
  });
});
