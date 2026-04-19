# PERF — Performance de DataShare

Dernière mise à jour : 2026-04-19

## 1. Périmètre

| Couche | Outil | Cible |
|---|---|---|
| API upload/download | k6 (via Docker `grafana/k6`) | Charge progressive 10 VUs, p95 < 2 s |
| Front (landing + my-files + download) | Lighthouse (Chrome DevTools ou `npx lighthouse`) | Score perf ≥ 80, LCP < 2,5 s |
| Bundle Angular | Rapport de build `ng build` | JS total < 500 KB gzippé |

## 2. Test de charge API — k6

### 2.1 Méthode

Script `scripts/perf/upload.js` : upload d'un fichier 100 KB en `multipart/form-data` vers `POST /api/files` (endpoint anonyme). Charge progressive de 0 à 10 VUs pendant 30 s, plateau à 10 VUs pendant 1 min, descente à 0 pendant 30 s.

Seuils configurés :
- `http_req_duration p(95) < 2000ms`
- `http_req_failed rate < 0.01`

### 2.2 Exécution

Prérequis : l'app tourne via `docker compose up -d` et le réseau Docker nommé `datashare` est créé.

```bash
docker run --rm -i --network datashare \
  -v "$(pwd)/scripts/perf:/scripts" \
  grafana/k6 run /scripts/upload.js
```

### 2.3 Résultats attendus

Sur une machine de dev (Windows 11, 16 GB RAM, SSD NVMe) :

| Métrique | Seuil | Observation attendue |
|---|---|---|
| Requêtes totales | — | ~1800–2400 (1 min plateau × 10 VUs × ~3 req/s) |
| p95 latence | < 2000 ms | Typiquement 150–400 ms pour uploads 100 KB |
| Taux d'échec | < 1 % | 0 % hors limite de rate limiter (10/min/IP) |

Note : le rate limiter partitionné par IP (10 uploads/min) limite artificiellement un test mono-IP. Pour mesurer la vraie capacité, désactiver temporairement le rate limiter ou spawner k6 depuis plusieurs IPs.

## 3. Lighthouse — Performance front

### 3.1 Méthode

Audit desktop headless sur la landing page via `npx lighthouse http://localhost`.

### 3.2 Résultats — 2026-04-19 (après optimisations)

| Catégorie | Score |
|---|---|
| Performance | 98 |
| Accessibilité | 100 |
| Bonnes pratiques | 100 |

### 3.3 Core Web Vitals

| Métrique | Valeur | Cible | Statut |
|---|---|---|---|
| First Contentful Paint (FCP) | 1.7 s | < 1.8 s | OK |
| Largest Contentful Paint (LCP) | 2.1 s | < 2.5 s | OK |
| Total Blocking Time (TBT) | 50 ms | < 200 ms | Excellent |
| Cumulative Layout Shift (CLS) | 0.002 | < 0.1 | Excellent |
| Speed Index | 1.7 s | < 3.4 s | Excellent |
| Total bytes transférés | 216 KB | < 1 MB | Excellent |

### 3.4 Optimisations appliquées

Score passé de **77** à **98** (+21 points) grâce à deux ajustements ciblés :

1. **Compression gzip nginx** (`frontend/nginx.conf`) : `gzip on` sur `application/javascript`, `text/css`, `image/svg+xml`, `font/woff2`, etc. avec `gzip_comp_level 6`. Réduction des bytes transférés : **681 KB → 216 KB** (−68 %).
2. **Chargement non-bloquant des fonts Google** (`src/index.html`) : `rel="preload" as="style"` puis swap vers `rel="stylesheet"` via `onload`, avec fallback `<noscript>`. Le rendu du HTML ne bloque plus en attendant le CSS des fonts. Gain FCP : −2.2 s.

Le Total Blocking Time reste très faible (50 ms) : aucun JS bloquant significatif, Angular hydratation rapide.

## 4. Budget frontend

| Élément | Budget | Mesure actuelle | Statut |
|---|---|---|---|
| Bundle JS initial (raw) | < 500 KB raw | 463 KB | OK (limite) |
| Bundle JS initial (gzipped) | < 150 KB | ~130 KB (estimé) | OK |
| CSS (raw) | < 50 KB | 13 KB | OK |
| Taille de page transférée | < 1 MB | 681 KB | OK |
| LCP | < 2.5 s | 2.1 s | OK |

Les budgets Angular sont déjà configurés dans `angular.json` (500 KB warning, 1 MB error sur `initial`).

## 5. Métriques à suivre en production

### 5.1 API (Serilog JSON structuré)

Événements tracés (via `ILogger<T>` + enrichissement `application=DataShare.Api`) :
- `auth.login.success` / `auth.login.failure`
- `file.uploaded` (avec `SizeBytes`, `OwnerId`, `FileId`)
- `file.downloaded` (avec `FileId`, `Token`)
- `file.purged` (blob supprimé par background service)
- `file.hard_deleted` (ligne DB retirée après 30 j)

Les logs sortent sur `stdout` en JSON — agrégeables par n'importe quel collecteur (Loki, CloudWatch, Datadog, stdout du conteneur).

Requêtes Loki type :
```
{app="DataShare.Api"} | json | EventId.Name="file.uploaded"
```

### 5.2 Infrastructure

| Métrique | Alerte |
|---|---|
| Temps de réponse API (p50, p95, p99) | p95 > 2 s pendant 5 min |
| Taille moyenne des fichiers uploadés | — (suivi tendance) |
| Taux d'erreur 5xx | > 1 % sur 5 min |
| Usage disque volume `files-data` | > 80 % de la capacité |
| Pods/containers DataShare.Api redémarrages | > 3 en 1 h |

### 5.3 Base de données

- Vacuum et analyse quotidiens (cron Postgres)
- Suivi de la taille des tables `StoredFiles`, `FileTags`, `AspNetUsers`
- Surveiller la latence des requêtes de `FileListService.GetUserFilesAsync` — si > 500 ms, vérifier les index `(OwnerId, CreatedAt)` et `(IsPurged, ExpiresAt)`

## 6. Optimisations possibles post-MVP

1. **Brotli** en plus de gzip sur nginx — gain marginal supplémentaire de −10 % sur les assets textuels
2. **HTTP/2** sur le reverse-proxy (nginx `http2;`) — parallélisation des assets
3. **Lazy loading des routes** secondaires (`/my-files`, `/d/:token`) — réduction du main bundle
4. **CDN pour les assets statiques** (js, css, svg, fonts) — latence perçue
5. **Streaming des uploads/downloads** côté serveur — baisse de la consommation mémoire API, utile pour les fichiers > 100 MB
