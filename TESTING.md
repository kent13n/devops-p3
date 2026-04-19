# TESTING — Plan de tests DataShare

Dernière mise à jour : 2026-04-19

## 1. Stratégie de tests

Pyramide classique : beaucoup de tests unitaires rapides, moins de tests d'intégration, un minimum d'E2E sur les parcours critiques.

| Niveau | Responsabilité | Outils | Exécution |
|---|---|---|---|
| Unit (backend) | Logique métier des services, parser, hashing | xUnit + AwesomeAssertions + NSubstitute + MockQueryable | `dotnet test` |
| Integration (backend) | Endpoints complets avec DB Postgres réelle | xUnit + WebApplicationFactory + Testcontainers.PostgreSql | `dotnet test` (Docker requis) |
| Unit (frontend) | Services, guards, interceptors, composants purs | Vitest + jsdom + Angular TestBed | `npm test` |
| E2E (frontend) | Parcours utilisateur complets dans un vrai navigateur | Playwright + Chromium | `npm run e2e` (app doit tourner) |

Objectif de couverture : **≥ 70 %** sur `DataShare.Application` + `DataShare.Api`. La couche `DataShare.Infrastructure` (EF Core, persistance) est testée indirectement par les tests d'intégration.

## 2. Plan de tests — User stories × niveaux

| US | Fonctionnalité | Unit | Intégration | E2E | Critères d'acceptation principaux |
|---|---|---|---|---|---|
| US01 | Upload anonyme | FileUploadService | POST /api/files | upload-download.spec.ts | Fichier ≤ 1 Go, extensions whitelist, token retourné, lien valide |
| US02 | Download via lien | FileDownloadService | GET/POST /api/download/:token | upload-download.spec.ts, protected-download.spec.ts | Métadonnées + stream renvoyés, 410 si expiré, 401 si mdp manquant |
| US03 | Inscription | — | POST /api/auth/register | auth.spec.ts | 201 + JWT, 409 email déjà pris |
| US04 | Connexion | TokenService | POST /api/auth/login | auth.spec.ts | 200 + JWT, 401 mdp incorrect |
| US05 | Historique (my-files) | FileListService, FileStatusFilterParser | GET /api/files?status=… | auth.spec.ts (accès espace) | Filtre all/active/expired, tri CreatedAt DESC, purged visible sans lien |
| US06 | Suppression fichier | FileDeleteService | DELETE /api/files/:id | — | 204 propriétaire, 404 ownership différent (anti-IDOR) |
| US07 | Upload anonyme sans tags | FileUploadService | POST /api/files (anon) | — | Tags ignorés si ownerId absent |
| US08 | Tags utilisateur | FileUploadService.GetOrCreateTagAsync (flux de retry) | POST /api/files avec tags + 2 uploads concurrents même tag (non-régression) | — | Tags persistés, race condition gérée sans DbUpdateException |
| US10 | Expiration + purge | ExpiredFilesCleanupService | — | — | Blob purgé après expiration, ligne DB retirée après 30 j |
| Sécurité | Hash mot de passe fichier | FilePasswordHasher | — | protected-download.spec.ts | BCrypt, Verify OK sur bon mdp, KO sinon |

## 3. Exécution locale

### 3.1 Backend

```bash
# Tous les tests (nécessite Docker pour les tests d'intégration)
dotnet test backend/DataShare.sln

# Uniquement les tests unitaires (sans Docker)
dotnet test backend/DataShare.sln --filter "FullyQualifiedName!~Integration"

# Avec couverture (filtrée sur Application + Api)
dotnet test backend/DataShare.sln --collect:"XPlat Code Coverage" --settings backend/coverlet.runsettings

# Génération du rapport HTML
reportgenerator -reports:"backend/DataShare.Tests/TestResults/*/coverage.cobertura.xml" -targetdir:backend/coverage-html -reporttypes:"Html;TextSummary"
```

### 3.2 Frontend

```bash
cd frontend/datashare-web

# Tests unitaires
npm test

# Tests unitaires + couverture HTML
npm run test:coverage

# Tests E2E (nécessite l'app démarrée via docker compose up)
npm run e2e

# Mode UI interactif pour Playwright
npm run e2e:ui
```

### 3.3 Prérequis pour les E2E

```bash
docker compose up -d   # démarre web + api + db
npm run e2e            # depuis frontend/datashare-web
```

## 4. Rapport de couverture

Snapshot du **2026-04-19** (filtre `[DataShare.Application]*,[DataShare.Api]*`) :

```
Line coverage:   94.6 %
Branch coverage: 71.4 %
Method coverage: 100  %

DataShare.Api                                  92.1 %
  AuthEndpoints                                100  %
  DownloadEndpoints                            100  %
  FileEndpoints                                100  %
  TagEndpoints                                 100  %
  Program                                      90.6 %

DataShare.Application                          100  %
  Services.FileDeleteService                   100  %
  Services.FileDownloadService                 100  %
  Services.FileListService                     100  %
  Services.FileStatusFilterParser              100  %
  Services.FileUploadService                   100  %
```

Capture d'écran du rapport HTML : `docs/coverage-screenshot.png` (à ajouter manuellement — ouvrir `backend/coverage-html/index.html` dans un navigateur).

**Note** : `DataShare.Infrastructure` n'est pas comptée dans ce pourcentage (filtre volontaire). Les services `ExpiredFilesCleanupService`, `LocalFileStorageService`, `FilePasswordHasher` et `TokenService` sont tout de même couverts par leurs propres tests unitaires.

### 4.1 Décompte total des tests

| Catégorie | Nombre | Statut |
|---|---|---|
| Backend Unit | 60 | Tous verts |
| Backend Integration (Testcontainers) | 23 | Tous verts |
| Frontend Unit (Vitest) | 30 | Tous verts |
| Frontend E2E (Playwright) | 3 | Tous verts |
| **Total** | **116** | **100 % verts** |

## 5. Intégration continue

Workflow GitHub Actions `.github/workflows/ci.yml` déclenché sur `push` et `pull_request` vers `main`. Trois jobs parallèles :

| Job | Étapes |
|---|---|
| backend | restore → build Release → test avec couverture → upload artefact |
| frontend | npm ci → ng build production → npm test |
| security | dotnet list --vulnerable + npm audit --audit-level=high |

Les tests d'intégration démarrent leur propre container Postgres via Testcontainers — aucun `services: postgres` n'est configuré dans le workflow.

Un **gate automatique** fait échouer le build si la couverture passe sous **70 %** (évaluée sur `Summary.txt` généré par `reportgenerator` après les tests).

Les résultats sont visibles dans l'onglet **Actions** du repo GitHub.

## 6. Mocks et fakes utilisés

### 6.1 Backend

| Dépendance | Technique |
|---|---|
| `IApplicationDbContext` | `MockQueryable.NSubstitute.BuildMockDbSet()` (support `ToListAsync`, `FirstOrDefaultAsync`, `IAsyncEnumerable`) |
| `IFileStorageService`, `IFilePasswordHasher`, `IConfiguration`, `UserManager` | NSubstitute |
| `ApplicationDbContext` (pour `ExpiredFilesCleanupService`) | EF Core `UseInMemoryDatabase` — évite le mocking du ChangeTracker |
| `Postgres` (pour endpoints) | Testcontainers.PostgreSql — vraie base éphémère par fixture |

### 6.2 Frontend

| Dépendance | Technique |
|---|---|
| `HttpClient` | `provideHttpClientTesting` + `HttpTestingController` |
| `AuthService`, `Router` | `useValue` avec `vi.fn()` pour les effets |
| `localStorage` | Vrai localStorage — réinitialisé dans `beforeEach` |

## 7. Évolutions prévues

Post-MVP, selon le trafic réel :

- Tests de charge étendus (> 5 min, 50–100 VUs) avec dataset Postgres pré-peuplé
- Tests de mutation (Stryker.NET pour .NET, Stryker JS pour Angular) sur la couche Application
- Tests de sécurité automatisés (OWASP ZAP baseline scan dans la CI)
- Tests de régression visuelle pour le front (Playwright `toHaveScreenshot`)
- Tests d'accessibilité automatisés (`@axe-core/playwright`)
