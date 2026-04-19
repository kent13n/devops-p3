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

### 3.2 Résultats — 2026-04-19

| Catégorie | Score |
|---|---|
| Performance | 77 |
| Accessibilité | 100 |
| Bonnes pratiques | 100 |

### 3.3 Core Web Vitals

| Métrique | Valeur | Cible | Statut |
|---|---|---|---|
| First Contentful Paint (FCP) | 3.9 s | < 1.8 s | À améliorer |
| Largest Contentful Paint (LCP) | 4.2 s | < 2.5 s | À améliorer |
| Total Blocking Time (TBT) | 30 ms | < 200 ms | OK |
| Cumulative Layout Shift (CLS) | 0.002 | < 0.1 | Excellent |
| Speed Index | 3.9 s | < 3.4 s | À améliorer |
| Total bytes transférés | 681 KB | < 1 MB | OK |

### 3.4 Analyse

Le score Performance est en dessous de la cible 80 à cause du FCP/LCP élevés (~4 s). Le goulot est probablement :

- **Fonts self-hosted non preloaded** — DM Sans chargée sans `<link rel="preload">`
- **Bundle Angular principal** — 463 KB raw (~130 KB gzippé), chargé synchrone
- **Absence de compression gzip côté nginx** (à vérifier dans `nginx.conf`)

TBT (30 ms) et CLS (0.002) sont excellents : aucun décalage visuel, aucun blocage JS significatif.

## 4. Budget frontend

| Élément | Budget | Mesure actuelle | Statut |
|---|---|---|---|
| Bundle JS initial (raw) | < 500 KB raw | 463 KB | OK (limite) |
| Bundle JS initial (gzipped) | < 150 KB | ~130 KB (estimé) | OK |
| CSS (raw) | < 50 KB | 13 KB | OK |
| Taille de page transférée | < 1 MB | 681 KB | OK |
| LCP | < 2.5 s | 4.2 s | **À améliorer** |

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

Par ordre d'impact estimé :

1. **Compression gzip/Brotli dans nginx** (`gzip on; gzip_types …`) — gain attendu LCP −30 %
2. **Preload des fonts self-hosted** (`<link rel="preload" as="font" crossorigin>`) — gain FCP −500 ms
3. **HTTP/2** sur le reverse-proxy (nginx `http2;`) — parallélisation des assets
4. **Lazy loading des routes** secondaires (`/my-files`, `/d/:token`) — réduction du main bundle
5. **CDN pour les assets statiques** (js, css, svg, fonts) — latence perçue
6. **Streaming des uploads/downloads** côté serveur — baisse de la consommation mémoire API, utile pour les fichiers > 100 MB
7. **Cache HTTP (Cache-Control)** sur les chunks Angular avec `ng build --output-hashing=all` — évite le re-download lors des revisites
