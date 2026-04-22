# MAINTENANCE — Procédures d'exploitation DataShare

Dernière mise à jour : 2026-04-19

Ce document décrit comment déployer, opérer, sauvegarder et mettre à jour DataShare. Il est destiné à un ingénieur tiers reprenant le projet sans connaissance préalable.

---

## 1. Vue d'ensemble de l'infrastructure

Le déploiement repose sur **Docker Compose** avec trois services :

| Service | Image | Rôle | Port |
|---|---|---|---|
| `web` | buildée depuis `./frontend` (nginx + Angular compilé) | Frontend + reverse proxy vers l'API | `80` |
| `api` | buildée depuis `./backend` (.NET 10 ASP.NET Core) | API REST, auth JWT, stockage fichiers | `8080` (interne) |
| `db` | `postgres:16-alpine` | Base relationnelle | `5432` |

**Réseau** : `datashare` (bridge Docker nommé explicitement, cf. `docker-compose.yml`).

**Volumes persistants** :
- `db-data` → `/var/lib/postgresql/data` sur `db` (données Postgres)
- `files-data` → `/var/datashare/files` sur `api` (blobs uploadés)

Les fichiers uploadés et la base sont donc persistés indépendamment des containers.

---

## 2. Déploiement from scratch

### Prérequis

- Docker 24+ et Docker Compose v2
- Accès au repo Git `https://github.com/kent13n/devops-p3.git`

### Première mise en service

```bash
git clone https://github.com/kent13n/devops-p3.git datashare
cd datashare
docker compose up --build -d
```

L'API applique automatiquement les migrations EF Core au démarrage en environnement Development (`DATASHARE_AUTO_MIGRATE=true` déjà positionné dans `docker-compose.yml`).

### Vérification

```bash
curl http://localhost/api/health
# → {"status":"healthy"}
```

L'interface est accessible sur `http://localhost`.

---

## 3. Configuration par environnement

Les variables d'environnement critiques sont définies dans `docker-compose.yml` pour le dev. **En production, elles doivent être surchargées par un mécanisme externe** (fichier `.env.production` non versionné, Vault, Secrets Manager…).

| Variable | Valeur dev | À changer en prod |
|---|---|---|
| `ConnectionStrings__DefaultConnection` | `Host=db;...;Password=postgres` | **Oui** — mot de passe fort |
| `Jwt__Secret` | `dev-secret-key-change-in-production-minimum-32-chars` | **Oui** — 256 bits aléatoires |
| `Jwt__Issuer` / `Jwt__Audience` | `DataShare` | Oui — identifiant métier |
| `Jwt__ExpirationInHours` | `24` | Selon politique de session |
| `FileStorage__BasePath` | `/var/datashare/files` | Oui si le volume est ailleurs |
| `ASPNETCORE_ENVIRONMENT` | `Development` (ou `Testing` pour bench) | **Production** obligatoire |
| `DATASHARE_AUTO_MIGRATE` | `true` | `false` en prod, migrations pilotées à la main |

En environnement `Production`, le rate limiter est actif et Swagger est désactivé.

---

## 4. Backup / restore PostgreSQL

### Sauvegarde

```bash
docker compose exec -T db pg_dump -U postgres -Fc datashare > backup-$(date +%Y%m%d).dump
```

Fréquence recommandée : **quotidienne**, rétention 30 jours minimum.

### Restauration

```bash
# arrêter l'API pour éviter les connexions concurrentes
docker compose stop api

docker compose exec -T db pg_restore -U postgres -d datashare --clean < backup-YYYYMMDD.dump

docker compose start api
```

Les commandes `docker compose exec` s'affranchissent du nom du container (qui dépend du dossier cloné).

### Test de restore

Tester la procédure au moins une fois par trimestre sur un environnement hors-prod. Un backup qu'on n'a jamais restauré n'est pas un backup.

---

## 5. Backup du volume `files-data`

Les blobs sont indépendants de la base. Un backup séparé est nécessaire.

Le nom exact du volume dépend du nom du dossier compose (ex. `datashare_files-data` si le dossier s'appelle `datashare`). On le récupère dynamiquement via `docker compose ls` ou `docker volume ls | grep files-data`.

### Sauvegarde

```bash
VOLUME=$(docker volume ls --format '{{.Name}}' | grep files-data)
docker run --rm \
  -v "$VOLUME:/source:ro" \
  -v "$(pwd)/backups:/backup" \
  alpine tar czf /backup/files-$(date +%Y%m%d).tar.gz -C /source .
```

### Restauration

```bash
docker compose stop api
VOLUME=$(docker volume ls --format '{{.Name}}' | grep files-data)

docker run --rm \
  -v "$VOLUME:/target" \
  -v "$(pwd)/backups:/backup:ro" \
  alpine sh -c "cd /target && tar xzf /backup/files-YYYYMMDD.tar.gz"

docker compose start api
```

Fréquence : quotidienne, rotation 7 jours (les fichiers eux-mêmes expirent après 7 jours max, les blobs plus anciens ne servent à rien sauf cas d'incident).

---

## 6. Rotation des secrets

### `Jwt__Secret`

**Conséquence : tous les JWT en circulation deviennent invalides.** Les utilisateurs doivent se reconnecter.

```bash
# Générer un nouveau secret 256 bits
openssl rand -base64 48

# Mettre à jour la valeur dans le secret manager (ou .env.production)
# Relancer l'API pour prendre en compte
docker compose up -d --force-recreate api
```

À faire sans attendre en cas de fuite suspectée. À planifier tous les 6 mois sinon.

### Mot de passe Postgres

```bash
# Dans le container db
docker compose exec db psql -U postgres -c "ALTER USER postgres WITH PASSWORD '<nouveau-mdp>';"

# Mettre à jour ConnectionStrings__DefaultConnection dans la conf api, puis :
docker compose up -d --force-recreate api
```

---

## 7. Upgrades

### .NET (10 → 11 par exemple)

1. Modifier `TargetFramework` dans les 5 `.csproj` :
   - `DataShare.Domain`, `DataShare.Application`, `DataShare.Infrastructure`, `DataShare.Api`, `DataShare.Tests`
2. Modifier l'image de base dans `backend/Dockerfile` (stages `build` et `runtime`)
3. `dotnet restore && dotnet build && dotnet test`
4. Si OK, rebuild l'image : `docker compose build api`
5. Déployer en suivant la procédure de mise à jour (§8)

### Migrations EF Core

Migrations actuellement présentes (ordre chronologique) :
- `20260412185438_InitialIdentity`
- `20260417130748_AddFileEntities`
- `20260419063653_AddPurgedFlag`
- `20260419094446_ReplaceIsPurgedIndexWithComposite`

Ajouter une nouvelle migration :

```bash
cd backend
dotnet ef migrations add <NomMigration> \
  --project DataShare.Infrastructure \
  --startup-project DataShare.Api
```

**Revoir à la main** le script SQL généré (`<NomMigration>.cs` + `Up()/Down()`), vérifier :
- Pas de `DROP` accidentel sur une colonne contenant de la donnée
- Indexes créés en `CREATE INDEX CONCURRENTLY` si la table est volumineuse (manuel — EF Core ne le fait pas par défaut)

### Angular (21.x → 22 par exemple)

```bash
cd frontend/datashare-web
npx ng update @angular/core @angular/cli
npm test
npm run build
```

Tester manuellement les parcours critiques (auth, upload, download) avant de pusher.

---

## 8. Monitoring et logs

### Logs applicatifs

Serilog émet en **JSON structuré** sur `stdout` des containers. Événements clés à surveiller :

| Événement | Signification |
|---|---|
| `auth.login.success` / `auth.login.failure` | Auth réussie / échouée (traçage bruteforce) |
| `file.uploaded` | Upload OK, inclut `SizeBytes`, `OwnerId`, `FileId` |
| `file.downloaded` | Download réussi |
| `file.purged` | Blob supprimé par le background service (expiration) |
| `file.hard_deleted` | Ligne DB supprimée après 30 j de rétention post-purge |

### Collecteur recommandé

Tous les collecteurs compatibles stdout Docker conviennent : **Loki + Promtail**, **CloudWatch Logs**, **Datadog**, **Graylog**…

Exemple de requête Loki :

```
{app="DataShare.Api"} | json | EventId_Name="file.uploaded"
```

### Métriques infra à surveiller

| Métrique | Seuil d'alerte |
|---|---|
| Latence p95 API | `> 2 s` pendant 5 min |
| Taux d'erreur 5xx | `> 1 %` sur 5 min |
| Usage disque volume `files-data` | `> 80 %` |
| Redémarrages container api | `> 3` en 1 h |
| Connexions DB | saturation près du `max_connections` |

---

## 9. Playbook incident

### 9.1 — Base de données inaccessible

**Symptômes** : l'API renvoie 500, logs `Npgsql.NpgsqlException: Connection refused`.

1. `docker compose ps` — vérifier l'état du service `db`
2. Si down : `docker compose logs db --tail 100`
3. Si corruption suspectée : `docker compose stop db api` puis restore depuis le dernier backup (§4)
4. Redémarrer : `docker compose start db api`
5. Vérifier santé : `curl http://localhost/api/health`

### 9.2 — Volume `files-data` plein

**Symptômes** : uploads échouent avec erreur disque, espace disque hôte saturé.

1. Vérifier la taille : `docker system df -v | grep files-data`
2. Lister les fichiers purgés qui auraient dû être supprimés (> 30 j post-expiration) :
   ```sql
   SELECT COUNT(*) FROM "StoredFiles"
   WHERE "IsPurged" = true AND "ExpiresAt" < NOW() - INTERVAL '30 days';
   ```
3. Si le cleanup service est en retard, le relancer via restart : `docker compose restart api`
4. En urgence, étendre le stockage hôte ou déplacer `files-data` vers un volume plus grand avec `docker volume create` + `rsync`

### 9.3 — Migration EF Core qui plante en production

**Symptômes** : l'API ne démarre pas au redéploiement, log `Failed executing DbCommand` lors de la migration.

1. Arrêter l'API : `docker compose stop api`
2. Identifier la dernière migration appliquée avec succès :
   ```bash
   docker compose exec -T db psql -U postgres -d datashare -c 'SELECT "MigrationId" FROM "__EFMigrationsHistory" ORDER BY "MigrationId" DESC LIMIT 1;'
   ```
3. Rollback : `dotnet ef database update <MigrationPrécédente> --project DataShare.Infrastructure --startup-project DataShare.Api --connection "$PROD_CS"`
4. Si la migration a écrit des données incohérentes : restaurer la DB depuis le backup (§4)
5. Corriger la migration (souvent un `Up()` qui drop une colonne non vide), tester sur un clone de prod, redéployer

### 9.4 — Fuite du `Jwt__Secret`

**Symptômes** : détection d'une fuite (commit accidentel, log exposé, alerte SIEM).

1. **Immédiatement** : rotation du secret (§6) — tous les JWT actifs sont invalidés
2. Forcer la déconnexion de tous les utilisateurs (déjà automatique par invalidation JWT)
3. Auditer les logs `auth.login.*` sur les heures précédant la détection pour repérer des connexions suspectes
4. Notifier les utilisateurs si une usurpation est avérée

### 9.5 — Rate limiter trop agressif sur un pic de trafic légitime

**Symptômes** : multiples `429 Too Many Requests` sur l'upload alors que le trafic semble normal.

1. Vérifier qu'il ne s'agit pas d'un abus réel (logs d'IP, `auth.login.failure`)
2. Si trafic légitime, ajuster temporairement les limites dans `Program.cs` :
   - `PermitLimit = 10` → `20` sur la policy `"upload"`
3. Rebuild et redéploiement : `docker compose up -d --build api`
4. À froid, réfléchir à une partition plus fine (par user authentifié plutôt que par IP) pour éviter de pénaliser les utilisateurs partageant une IP (NAT d'entreprise)

---

## 10. Contacts & escalade

| Niveau | Rôle | Contact |
|---|---|---|
| Dev | Mainteneur principal | _à renseigner_ |
| Ops | Plateforme Docker / Postgres | _à renseigner_ |
| Sécurité | Incidents, fuites | _à renseigner_ |
| Produit | Décisions fonctionnelles | _à renseigner_ |

Documentation complémentaire :
- [README.md](README.md) — démarrage rapide et structure du projet
- [TESTING.md](TESTING.md) — stratégie de tests et couverture
- [SECURITY.md](SECURITY.md) — mesures de sécurité en place
- [PERF.md](PERF.md) — budget de performance et monitoring
