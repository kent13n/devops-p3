# DataShare

Plateforme de transfert sécurisé de fichiers destinée aux freelances et petites entreprises. Les utilisateurs peuvent téléverser des fichiers, obtenir un lien de téléchargement temporaire et le partager avec des destinataires, avec la possibilité de protéger l'accès par mot de passe.

## Stack technique

| Composant | Technologie |
|---|---|
| Back-end | ASP.NET Core 10 (Minimal API, Clean Architecture) |
| Front-end | Angular 21 (standalone components, Material, Tailwind) |
| Base de données | PostgreSQL 16 |
| Stockage fichiers | Système de fichiers local (volume Docker) |
| Authentification | ASP.NET Identity + JWT Bearer |
| Containerisation | Docker + Docker Compose |

## Prérequis

- [Docker](https://docs.docker.com/get-docker/) 24+ et Docker Compose v2
- [.NET SDK 10](https://dotnet.microsoft.com/download) (pour le développement local)
- [Node.js 22](https://nodejs.org/) + npm (pour le développement local)

## Lancement rapide (Docker)

```bash
git clone https://github.com/kent13n/devops-p3.git
cd devops-p3
docker compose up --build
```

L'application est accessible sur :
- **http://localhost** — interface web
- **http://localhost/api/health** — vérification de l'API
- **http://localhost:5432** — PostgreSQL (accès direct)

## Développement local (sans Docker)

### Base de données

Démarrer une instance PostgreSQL (via Docker ou installation locale) :

```bash
docker run -d --name datashare-db -p 5432:5432 \
  -e POSTGRES_DB=datashare \
  -e POSTGRES_USER=postgres \
  -e POSTGRES_PASSWORD=postgres \
  postgres:16-alpine
```

### Back-end

```bash
cd backend
dotnet restore
dotnet run --project DataShare.Api
```

L'API démarre sur `http://localhost:5000`. Swagger UI disponible sur `http://localhost:5000/swagger`.

### Front-end

```bash
cd frontend/datashare-web
npm install
npx ng serve
```

L'application Angular démarre sur `http://localhost:4200`.

## Structure du projet

```
├── backend/
│   ├── DataShare.Domain/           # Entités, interfaces, exceptions métier
│   ├── DataShare.Application/      # Use cases, DTOs, validators, services
│   ├── DataShare.Infrastructure/   # EF Core, Identity, JWT, stockage fichiers
│   ├── DataShare.Api/              # Minimal API endpoints, DI, middleware
│   ├── DataShare.Tests/            # Tests unitaires et d'intégration
│   └── Dockerfile
├── frontend/
│   ├── datashare-web/              # Application Angular 21
│   ├── nginx.conf                  # Config reverse proxy
│   └── Dockerfile
├── docs/
│   ├── 01-architecture.md          # Architecture de la solution
│   ├── 02-modele-donnees.md        # Modèle conceptuel de données
│   ├── 03-choix-technologiques.md  # Justification des choix techniques
│   ├── api/openapi.yaml            # Contrat d'interface OpenAPI 3.0
│   └── diagrams/                   # Diagrammes (architecture, MCD, séquences)
├── docker-compose.yml
└── README.md
```

## Documentation

- [Architecture de la solution](docs/01-architecture.md)
- [Modèle de données](docs/02-modele-donnees.md)
- [Choix technologiques](docs/03-choix-technologiques.md)
- [Contrat d'interface (OpenAPI)](docs/api/openapi.yaml)
- [Plan de tests et couverture](TESTING.md)
- [Analyse de sécurité](SECURITY.md)
- [Performance et budget front](PERF.md)

## Tests

Voir [TESTING.md](TESTING.md) pour le plan complet. En résumé :

```bash
# Backend (unit + intégration avec Testcontainers, Docker requis)
dotnet test backend/DataShare.sln

# Frontend unit (Vitest)
cd frontend/datashare-web && npm test

# Frontend E2E (Playwright, app démarrée via docker compose up)
cd frontend/datashare-web && npm run e2e
```

## Licence

Projet réalisé dans le cadre d'une formation OpenClassrooms.
