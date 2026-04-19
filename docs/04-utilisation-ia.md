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
| `feat(ai): soft-delete des fichiers expirés pour l'historique (US05)` | _à compléter_ | Flag IsPurged + migration + cleanup service 2 étapes |
| `feat(ai): endpoint GET /api/files avec filtre status (US05)` | _à compléter_ | DTO + enum + service + endpoint |
| `feat(ai): page Mon espace — layout, sidebar, liste de fichiers (US05)` | _à compléter_ | Components Angular + confirm dialog |

### Phase 2 — Corrections après review humaine

| Commit | SHA | Description |
|---|---|---|
| _à compléter lors des reviews_ | | |

## 4. Rôle de supervision

Le référent technique senior a supervisé activement le code produit par l'IA :

- **Revues de code** : chaque commit `feat(ai):` a été relu manuellement avant merge
- **Sécurité** : vérification des contrôles d'accès (anti-IDOR), des validations d'entrée, de l'absence de fuite d'information
- **Conformité Clean Architecture** : les services Application ne doivent pas dépendre d'Infrastructure
- **Cohérence design** : vérification que les composants respectent les maquettes Figma (couleurs, typographie DM Sans, comportement responsive)
- **Cohérence architecturale** : réutilisation des patterns existants (signals, OnPush, panelClass, Result pattern côté back)

### Types de corrections typiquement appliquées

_à compléter avec les commits `fix:` effectifs_

## 5. Recommandations issues de l'expérience

_à compléter en étape 6, après l'ensemble des reviews_

- **Ce qui a bien fonctionné** :
- **Ce qui a nécessité des corrections fréquentes** :
- **Limites identifiées** :
- **Quand privilégier l'IA vs le code manuel** :
