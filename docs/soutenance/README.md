# Soutenance DataShare — Slide deck

Slide deck au format **Marp** (Markdown versionnable) dans `slides.md`. 12 slides, ~15 min de présentation + 3-5 min de démo + Q&A.

## Prévisualiser pendant l'édition

Installer l'extension VSCode **[Marp for VS Code](https://marketplace.visualstudio.com/items?itemName=marp-team.marp-vscode)** puis ouvrir `slides.md` : une prévisualisation live s'affiche à droite, elle se met à jour à chaque sauvegarde.

## Exporter en PDF (pour la projection)

```bash
npx @marp-team/marp-cli docs/soutenance/slides.md --pdf --allow-local-files
```

Produit `docs/soutenance/slides.pdf`. L'option `--allow-local-files` est **nécessaire** pour que les images relatives (`../diagrams/*.jpg`, `../backend-coverage-screenshot.png`, etc.) soient embarquées dans le PDF.

## Exporter en PPTX

```bash
npx @marp-team/marp-cli docs/soutenance/slides.md --pptx --allow-local-files
```

Produit `docs/soutenance/slides.pptx`. Utile si tu veux retoucher les slides dans PowerPoint / LibreOffice Impress / Google Slides avant la soutenance.

## Exporter en HTML (consultation navigateur)

```bash
npx @marp-team/marp-cli docs/soutenance/slides.md --html --allow-local-files
```

## Démo live

Entre les slides 7 (Déploiement Docker) et 8 (Tests), un temps de démo de 3-5 minutes est prévu. Scénario suggéré :

1. Upload anonyme d'un fichier texte → copie du lien de download
2. Ouverture du lien dans un nouvel onglet → download fonctionnel
3. Inscription d'un nouveau compte → connexion → espace "Mes fichiers"
4. Démonstration des filtres (Tous / Actifs / Expiré)
5. Suppression d'un fichier (optimistic update)

L'app doit tourner en local via `docker compose up -d` avant la démo.
