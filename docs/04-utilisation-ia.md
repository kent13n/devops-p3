# Utilisation de l'IA dans le développement

> Document exigé par la mission OpenClassrooms. Retrace l'usage d'un copilote IA sur une User Story spécifique, le rôle de supervision humaine, et les correctifs apportés.

## 1. User Story déléguée à l'IA

**US05 — Consultation de l'historique**

Un utilisateur connecté peut consulter l'ensemble des fichiers qu'il a envoyés via l'interface de son espace personnel.

### Pourquoi cette US ?

Le choix s'est porté sur l'US05 pour plusieurs raisons :

- **Auto-contenue** : l'US05 mobilise toutes les couches (backend + frontend + intégration avec endpoints existants) sans être au cœur des fonctionnalités critiques (auth, upload, download), donc un bug ou une défaillance de l'IA n'aurait pas d'impact sur la démo principale.
- **Présentable** : la page « Mon espace » est une interface riche (liste, tabs, actions, responsive) qui démontre bien la capacité de l'IA à produire du code UI cohérent.
- **Clean Architecture** : permet de valider que l'IA respecte l'architecture en couches déjà en place.
- **Périmètre clair** : les spécifications US05 sont précises (filtrage par état, affichage des expirés, suppression), donc facilement évaluables.

## 2. Tâches confiées à l'IA

L'IA a pris en charge :

### Backend
- Ajout du flag `IsPurged` sur l'entité `StoredFile` (soft-delete des expirés)
- Migration EF Core `AddPurgedFlag`
- Refonte du `ExpiredFilesCleanupService` en 2 étapes (purge du blob, puis suppression DB après 30 jours)
- DTO `FileHistoryItem` + enum `FileStatusFilter` avec parser strict
- Service Application `FileListService` (Clean Archi, injection de `IApplicationDbContext`)
- Endpoint `GET /api/files?status=all|active|expired` avec validation du query param

### Frontend
- Ajout du modèle `FileHistoryItem` et de la méthode `getMyFiles()` dans `FileService`
- Composant partagé `ConfirmDialogComponent` (réutilisable pour confirmations)
- Composant `MyFilesSidebarComponent` (sidebar desktop + drawer mobile avec overlay)
- Composant `FileListItemComponent` (ligne de fichier avec icône, nom, expiration, actions)
- Page `MyFilesComponent` avec signals, OnPush, tabs, optimistic update + rollback

## 3. Commits tracés

### Phase 1 — Génération par l'IA

| Commit | SHA | Description |
|---|---|---|
| `feat(ai): soft-delete des fichiers expirés pour l'historique (US05)` | [6fdbcb7](https://github.com/kent13n/devops-p3/commit/6fdbcb7) | Flag `IsPurged` sur `StoredFile`, migration `AddPurgedFlag`, refonte du cleanup service en 2 étapes (purge blob puis hard-delete après 30 j) |
| `feat(ai): endpoint GET /api/files avec filtre status (US05)` | [c9e1ef6](https://github.com/kent13n/devops-p3/commit/c9e1ef6) | DTO `FileHistoryItem`, enum `FileStatusFilter`, parser, service Application `FileListService`, endpoint + validation du query param |
| `feat(ai): page Mon espace — layout, sidebar, liste de fichiers (US05)` | [9326dda](https://github.com/kent13n/devops-p3/commit/9326dda) | Composants `MyFilesSidebar`, `FileListItem`, `ConfirmDialog`, page `MyFilesComponent` (signals, OnPush, tabs, optimistic update + rollback) |

### Phase 2 — Corrections après review humaine

| Commit | SHA | Description |
|---|---|---|
| `fix: corrections après review humaine sur US05` | [e0f00f8](https://github.com/kent13n/devops-p3/commit/e0f00f8) | Détachement explicite du `newTag` dans le catch du retry (race condition tags) ; rejet strict des entrées numériques dans `FileStatusFilterParser` ; index composite `(IsPurged, ExpiresAt)` à la place d'un index simple + migration dédiée |
| `fix: corrections UX/design après review humaine sur US05` | [72e1191](https://github.com/kent13n/devops-p3/commit/72e1191) | Alignement du design « Mon espace » sur les maquettes Figma ; extraction du composant partagé `FileIcon` ; `MatChipGrid` pour les tags ; warning banner pour expiration ≤ 24 h ; rafraîchissement systématique de la liste après upload ; correctif `errorInterceptor` sur `/api/download/` |

## 4. Rôle de supervision

Le référent technique senior a supervisé activement le code produit par l'IA :

- **Revues de code** : chaque commit `feat(ai):` a été relu manuellement avant merge
- **Sécurité** : vérification des contrôles d'accès (anti-IDOR), des validations d'entrée, de l'absence de fuite d'information
- **Conformité Clean Architecture** : les services Application ne doivent pas dépendre d'Infrastructure
- **Cohérence design** : vérification que les composants respectent les maquettes Figma (couleurs, typographie DM Sans, comportement responsive)
- **Cohérence architecturale** : réutilisation des patterns existants (signals, OnPush, panelClass, Result pattern côté back)

### Types de corrections typiquement appliquées

Les reviews humaines ont porté sur quatre catégories de défauts, récurrentes :

- **Edge cases d'entrée** — le parser `FileStatusFilterParser` généré par l'IA acceptait des chaînes numériques (`?status=0`, `?status=42`) interprétées par `Enum.TryParse` comme des valeurs d'enum valides. Correction : pré-filtrage explicite des entrées numériques avant le parse.
- **Race conditions non détectées** — dans `FileUploadService.GetOrCreateTagAsync`, la première version attrapait `DbUpdateException` mais laissait le nouveau tag attaché au `ChangeTracker`, provoquant une seconde violation d'unicité au `SaveChanges` final. Correction : `_db.Tags.Remove(newTag)` dans le catch pour basculer en état `Detached`.
- **Performance non anticipée** — index `IsPurged` seul insuffisamment sélectif pour la query `WHERE IsPurged AND ExpiresAt < cutoff` du cleanup service. Correction : migration dédiée pour remplacer par un index composite `(IsPurged, ExpiresAt)`.
- **Détails design et UX** — couleurs approximatives par rapport aux maquettes Figma, composants non extraits en partagé (duplication), absence de warning banner pour expiration imminente, rafraîchissement manquant de la liste après upload. Correction : passe complète sur le design + extraction du composant `FileIcon`.
- **Accessibilité** — ARIA roles partiellement manquants, focus trap absent sur modales, skip link mal positionné. Correction en étape qualité (étape 5).

## 5. Recommandations issues de l'expérience

- **Ce qui a bien fonctionné** — la génération de composants Angular standalone + Material + Tailwind est fidèle aux maquettes Figma à ~85 %. La structure Clean Architecture est respectée sans rappel explicite : l'IA place les DTO dans `Application`, les services métier dans `Application.Services`, les entités dans `Domain`, sans franchir de limites de couches. Les migrations EF Core sont techniquement correctes du premier coup.
- **Ce qui a nécessité des corrections fréquentes** — les edge cases d'entrée (inputs numériques interprétés comme enum, tokens vides, whitespace) et les interactions concurrentes (race conditions sur `SaveChanges`, gestion des conflits de contraintes uniques) demandent presque toujours un aller-retour. De même pour l'accessibilité et les micro-détails visuels.
- **Limites identifiées** — l'IA ne détecte pas les risques de performance au-delà des patterns évidents (pas de proposition spontanée d'index composite, pas d'analyse de sélectivité). Elle n'a pas non plus de vision transverse anti-IDOR : chaque endpoint est généré isolément, les contrôles d'ownership doivent être vérifiés globalement par un humain. Enfin, le comportement au-delà du happy path est rarement pensé spontanément.
- **Quand privilégier l'IA vs le code manuel** — l'IA est particulièrement efficace sur le code UI répétitif (composants de liste, formulaires), le boilerplate Clean Architecture (DTO, interfaces, services avec patterns déjà établis), et les migrations EF mécaniques. Le code manuel garde l'avantage sur les décisions architecturales structurantes (choix d'index, stratégie de concurrence, modélisation des rate limiters), les revues de sécurité transverses, et les points d'intégration critiques (auth, paiement, transactions).
