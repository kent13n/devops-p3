import { test, expect } from '@playwright/test';

test.describe('Auth complet : register → my-files → logout', () => {
  test('un nouvel utilisateur peut s\'inscrire, accéder à son espace et se déconnecter', async ({ page }) => {
    const email = `e2e-${Date.now()}-${Math.random().toString(36).slice(2, 8)}@test.local`;
    const password = 'Password123!';

    await page.goto('/');

    await page.getByRole('button', { name: 'Se connecter' }).click();
    await expect(page.getByRole('heading', { name: 'Connexion' })).toBeVisible();

    await page.getByRole('button', { name: 'Créer un compte' }).click();
    await expect(page.getByRole('heading', { name: 'Créer un compte' })).toBeVisible();

    await page.locator('#register-email').fill(email);
    await page.locator('#register-password').fill(password);
    await page.locator('#register-confirm').fill(password);
    await page.getByRole('button', { name: 'Créer mon compte' }).click();

    await expect(page).toHaveURL(/\/my-files/);
    await expect(page.getByRole('heading', { name: 'Mes fichiers' })).toBeVisible();

    await page.getByRole('button', { name: 'Déconnexion' }).click();
    await expect(page).toHaveURL(/\/$/);
    await expect(page.getByRole('button', { name: 'Se connecter' })).toBeVisible();
  });
});
