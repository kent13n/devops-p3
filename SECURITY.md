# SECURITY — Analyse de sécurité de DataShare

Dernière mise à jour : 2026-04-19

## 1. Périmètre et méthodologie

Les scans couvrent trois surfaces :

| Surface | Outil | Fréquence |
|---|---|---|
| Dépendances .NET (API) | `dotnet list package --vulnerable --include-transitive` | À chaque push (CI) |
| Dépendances npm (frontend) | `npm audit` | À chaque push (CI) |
| Images Docker (api + web) | Trivy (`aquasec/trivy image`) | Manuel avant release |

Seules les vulnérabilités de sévérité **High** ou **Critical** bloquent la CI. Les **Moderate** sont tracées et évaluées, les **Low/Info** sont acceptées par défaut.

## 2. Résultats des scans

### 2.1 `dotnet list package --vulnerable` — 2026-04-19

```
Les sources suivantes ont été utilisées : https://api.nuget.org/v3/index.json
Le projet spécifié 'DataShare.Domain' n'a aucun package vulnérable.
Le projet spécifié 'DataShare.Application' n'a aucun package vulnérable.
Le projet spécifié 'DataShare.Infrastructure' n'a aucun package vulnérable.
Le projet spécifié 'DataShare.Api' n'a aucun package vulnérable.
Le projet spécifié 'DataShare.Tests' n'a aucun package vulnérable.
```

Aucune vulnérabilité détectée côté .NET.

### 2.2 `npm audit` — 2026-04-19

| Sévérité | Nombre |
|---|---|
| Critical | 0 |
| High | 0 |
| Moderate | 1 |
| Low | 0 |

La vulnérabilité `Moderate` concerne `hono` (< 4.12.14, CVE [GHSA-458j-xx4x-4375](https://github.com/advisories/GHSA-458j-xx4x-4375), XSS via JSX SSR, CVSS 4.3). `hono` est une dépendance **transitive indirecte** et **n'est pas utilisée dans notre chaîne de rendu Angular** (le projet n'utilise ni `hono/jsx` ni SSR). Pas d'exposition.

Un fix est disponible (`npm audit fix`). Sera appliqué lors de la prochaine mise à jour de la chaîne de build.

### 2.3 Trivy sur les images Docker

Commande à exécuter localement :

```bash
docker run --rm -v //var/run/docker.sock:/var/run/docker.sock aquasec/trivy image --severity HIGH,CRITICAL projet-3-api:latest
docker run --rm -v //var/run/docker.sock:/var/run/docker.sock aquasec/trivy image --severity HIGH,CRITICAL projet-3-web:latest
```

Les images de base utilisées sont :
- `mcr.microsoft.com/dotnet/aspnet:10.0-alpine` (API)
- `nginx:alpine` (frontend)
- `postgres:16-alpine` (base)

Ces images officielles sont régulièrement patchées. Un scan mensuel manuel est prévu avant chaque release majeure.

## 3. Décisions sur les vulnérabilités

| Vulnérabilité | Décision | Justification |
|---|---|---|
| `hono` < 4.12.14 (moderate) | Acceptée temporairement | Transitive non utilisée, pas d'impact runtime. Correction au prochain upgrade de dépendances |

## 4. Mesures de sécurité en place

### 4.1 Authentification et mots de passe
- **ASP.NET Identity** pour la gestion des comptes utilisateurs
- **PBKDF2** (défaut Identity) pour les mots de passe des comptes (iterations forte par défaut)
- **BCrypt** (BCrypt.Net-Next) pour les mots de passe de fichier (cost factor 11)
- **JWT HS256** signé avec secret serveur (256 bits) ; durée 24 h ; pas de refresh token (MVP)
- Validation issuer/audience/lifetime/signing-key côté serveur

### 4.2 Protection des endpoints
- **Rate limiter** partitionné par IP : 10 uploads/min et 20 downloads/min par IP (ASP.NET Core RateLimiter)
- **ForwardedHeaders** avec `KnownIPNetworks` pour la confiance des reverse-proxies uniquement
- **CORS** restrictif à l'origine du frontend nginx

### 4.3 Entrées utilisateur
- **Validation d'extension** côté serveur via liste noire (exe, dll, bat, sh, cmd, js, php, etc.)
- **Taille max 1 Go** côté Kestrel + FormOptions
- **Validation DataAnnotations** sur tous les DTOs de l'API
- **EF Core** paramétré : aucune requête SQL concaténée, immune à SQL injection
- **Anti-IDOR** : la suppression d'un fichier renvoie 404 indiscernable (pas de 403) quand l'ownership ne correspond pas

### 4.4 Réponses HTTP
- Header `X-Content-Type-Options: nosniff` sur les réponses
- Pas de stack traces divulguées hors environnement Development
- Liens externes : `rel="noopener noreferrer"` sur `target="_blank"`

### 4.5 Infrastructure
- Images Alpine minimales (surface d'attaque réduite)
- Volumes Docker pour la persistance ; pas de secrets dans les images
- Secrets (JWT:Secret, ConnectionString) injectés via variables d'env ou `appsettings.Production.local.json` gitignoré

## 5. Limitations assumées pour le MVP

Ces limitations sont documentées et traitées dans la roadmap post-MVP :

| Limitation | Impact | Contre-mesure MVP |
|---|---|---|
| Pas de refresh token | Reconnexion obligatoire après 24 h | Durée JWT suffisante pour la session type |
| Pas de 2FA | Vulnérabilité au credential stuffing | Mots de passe longs exigés (≥ 8 car.) |
| Pas d'antivirus sur les uploads | Fichiers potentiellement malveillants stockés | Blocage des extensions exécutables + rel="noopener" sur lien de download ; avertissement utilisateur |
| JWT en `localStorage` | Vulnérable à XSS | CSP nginx + pas d'injection `innerHTML` côté Angular |
| Stockage filesystem non chiffré | Accès serveur = accès fichiers | Accès serveur protégé par SSH/réseau cloud |
| Pas de retention des logs d'accès | Difficulté à détecter intrusion | Serilog JSON structuré + logs conservés 30 j côté infra |
| Pas de CSRF token | Les endpoints modifiants utilisent JWT Bearer, pas de cookie — CSRF impossible par design |

## 6. Évolutions prévues

Les éléments suivants seront traités après le MVP. Détails dans `MAINTENANCE.md` (à produire) :

- Ajout de tests de sécurité automatisés (OWASP ZAP baseline sur CI)
- 2FA (TOTP) pour les comptes utilisateurs
- Chiffrement au repos des blobs (AES-256 avec KMS)
- Rotation des secrets JWT
- Antivirus ClamAV sur les uploads
- Web Application Firewall (ModSecurity ou cloud managé)
- Durcissement headers HTTP (HSTS, CSP strict, Referrer-Policy)
