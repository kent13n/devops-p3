# Checklist de conformité OpenClassrooms — Projet 3

Dernière vérification : 2026-04-19

Checklist interne utilisée pour valider que tous les livrables exigés par la mission OC sont présents dans le repo avant la soutenance.

## Livrables techniques

- [x] **Code source** versionné sur Git public : https://github.com/kent13n/devops-p3
- [x] **CI GitHub Actions** verte (3 jobs : backend, frontend, scans sécurité)
- [x] **Gate de couverture** ≥ 70 % appliqué automatiquement sur chaque push
- [x] **Application déployable** via `docker compose up --build` depuis un clone vierge

## Documentation de conception

- [x] [docs/01-architecture.md](01-architecture.md) — architecture de la solution
- [x] [docs/02-modele-donnees.md](02-modele-donnees.md) — modèle conceptuel de données
- [x] [docs/03-choix-technologiques.md](03-choix-technologiques.md) — justification de la stack
- [x] [docs/api/openapi.yaml](api/openapi.yaml) — contrat d'interface OpenAPI 3.0
- [x] Diagrammes trackés dans `docs/diagrams/` (architecture.jpg, mcd.jpg, sequence-*.jpg)

## Documentation qualité

- [x] [TESTING.md](../TESTING.md) — stratégie de tests, matrice US × niveau, commandes d'exécution, capture couverture 94 %
- [x] [SECURITY.md](../SECURITY.md) — scans .NET / npm / Trivy avec captures, décisions sur les vulnérabilités, mesures en place
- [x] [PERF.md](../PERF.md) — Lighthouse mobile, k6, budget bundle, métriques prod
- [x] [MAINTENANCE.md](../MAINTENANCE.md) — déploiement, backup/restore, upgrades, playbook incident

## Workflow IA (obligation OC)

- [x] [docs/04-utilisation-ia.md](04-utilisation-ia.md) — US05 déléguée, SHA des commits IA + fix humains, retours d'expérience
- [x] Au moins **1 commit AI-only** traçable en Git (3 en réalité : [6fdbcb7](https://github.com/kent13n/devops-p3/commit/6fdbcb7), [c9e1ef6](https://github.com/kent13n/devops-p3/commit/c9e1ef6), [9326dda](https://github.com/kent13n/devops-p3/commit/9326dda))
- [x] Supervision humaine tracée ([e0f00f8](https://github.com/kent13n/devops-p3/commit/e0f00f8), [72e1191](https://github.com/kent13n/devops-p3/commit/72e1191))

## Fonctionnalités (User Stories)

### Obligatoires

- [x] **US01** — Upload avec expiration et mot de passe optionnel
- [x] **US02** — Download via lien temporaire
- [x] **US03** — Inscription
- [x] **US04** — Connexion
- [x] **US05** — Historique personnel (déléguée à l'IA)
- [x] **US06** — Suppression d'un fichier par son propriétaire

### Optionnelles (objectif : 3+ livrées)

- [x] **US07** — Upload anonyme sans compte
- [x] **US08** — Tags sur les fichiers (utilisateurs connectés)
- [x] **US09** — Protection par mot de passe — **absorbée dans US01** (demandée au moment de l'upload, pas de flow séparé)
- [x] **US10** — Expiration automatique 1–7 jours

## Livrables OC administratifs

- [x] [docs/autoevaluation.pdf](autoevaluation.pdf) — grille d'auto-évaluation OC remplie
- [x] [docs/specifications.pdf](specifications.pdf) — cahier des charges original
- [x] [LICENSE](../LICENSE) — licence MIT pour clarifier l'intention

## Soutenance

- [x] [docs/soutenance/slides.md](soutenance/slides.md) — slide deck Marp (12 slides)
- [x] [docs/soutenance/README.md](soutenance/README.md) — commandes d'export et note sur la démo live
- [ ] _à faire le jour J_ : démo live testée sur machine de présentation
- [ ] _à faire le jour J_ : PDF exporté et chargé hors connexion (fallback)

## Métriques finales

| Indicateur | Valeur | Seuil |
|---|---|---|
| Tests verts | 116 / 116 | 100 % |
| Couverture lignes (Application + Api) | 94 % | ≥ 70 % |
| Lighthouse mobile (Perf / A11y / BP / SEO) | 98 / 100 / 100 / 100 | ≥ 80 |
| k6 p95 upload | 14 ms | < 2 000 ms |
| Vulnérabilités HIGH/CRITICAL | 0 | 0 |
| CI | verte | verte |
