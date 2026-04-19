import { test, expect } from '@playwright/test';
import * as path from 'path';

test.describe('Upload connecté + accès au lien de téléchargement', () => {
  let email: string;
  const password = 'Password123!';

  test.beforeEach(async ({ page }) => {
    email = `e2e-ul-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@test.local`;

    await page.goto('/');
    await page.getByRole('button', { name: 'Se connecter' }).click();
    await page.getByRole('button', { name: 'Créer un compte' }).click();
    await page.locator('#register-email').fill(email);
    await page.locator('#register-password').fill(password);
    await page.locator('#register-confirm').fill(password);
    await page.getByRole('button', { name: 'Créer mon compte' }).click();
    await expect(page).toHaveURL(/\/my-files/);
  });

  test('upload un fichier puis accède à la page de téléchargement via le lien', async ({ page, context }) => {
    await page.getByRole('button', { name: 'Ajouter des fichiers' }).click();
    await expect(page.getByRole('heading', { name: 'Ajouter un fichier' })).toBeVisible();

    const fileInput = page.locator('#file-input');
    await fileInput.setInputFiles(path.join(__dirname, 'fixtures/hello.txt'));

    await page.getByRole('button', { name: 'Téléverser' }).click();

    const link = page.locator('.download-link-box a');
    await expect(link).toBeVisible();
    const downloadUrl = await link.getAttribute('href');
    expect(downloadUrl).toBeTruthy();

    const downloadPage = await context.newPage();
    await downloadPage.goto(downloadUrl!);
    await expect(downloadPage.getByRole('heading', { name: 'Télécharger un fichier' })).toBeVisible();
    await expect(downloadPage.getByText('hello.txt')).toBeVisible();
    await expect(downloadPage.getByRole('button', { name: /Télécharger/ })).toBeEnabled();
  });
});
