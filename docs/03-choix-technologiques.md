# DataShare — Justification des choix technologiques

> Document destiné à la fois à l'équipe et aux investisseurs. L'objectif est de montrer que chaque brique technique a été choisie pour des raisons concrètes liées au contexte du projet (MVP, 4 semaines, démo investisseurs, accessibilité PSH exigée), et non par défaut ou par habitude.

## 1. Contexte et contraintes

DataShare est un MVP de plateforme de transfert de fichiers à livrer **en 4 semaines** pour une démo investisseurs. Les spécifications fonctionnelles imposent un cadre :

- 6 User Stories obligatoires (auth, upload connecté, download via lien, historique, suppression) + plusieurs US optionnelles
- Architecture REST avec authentification JWT
- Stockage local **ou** AWS S3
- Stack au choix parmi 4 back-end (Spring, .NET, NestJS, Symfony/Laravel) × 3 front-end (Angular, React, Vue) × 2 BDD (PostgreSQL, MongoDB)
- Couverture de tests ≥ 70 %
- **Accessibilité PSH** prise en compte
- Scripts de déploiement, suivi qualité (TESTING / SECURITY / PERF / MAINTENANCE)
- Une seule US doit être codée par un copilote IA, supervisée par le développeur senior

Ce contexte oriente fortement les choix : on privilégie systématiquement la **maturité**, la **vitesse de mise en place** et la **lisibilité** sur l'innovation ou la flexibilité maximale. Toute brique qui demande un investissement de configuration > 1 jour est rejetée pour le MVP.

## 2. Back-end : .NET 10 + Minimal API

### Choix retenu

**ASP.NET Core 10 (LTS)** avec **Minimal API** (et non Controllers).

### Alternatives évaluées

| Option | Évaluation |
|---|---|
| Spring Boot (Java) | Choix tout aussi valide : maturité excellente, écosystème énorme (Spring Security, Spring Data JPA), très répandu en entreprise. Non retenu car l'équipe est plus compétente sur .NET |
| NestJS (TypeScript) | Choix tout aussi valide : élégant, partage le langage TypeScript avec Angular, architecture modulaire inspirée d'Angular. Non retenu car l'équipe est plus compétente sur .NET |
| PHP Symfony / Laravel | Stack productive avec un large écosystème, mais typage moins strict que C# et outillage d'authentification/ORM moins intégré que ce qu'offre .NET nativement |
| **.NET 10** ✓ | **Retenu** — le facteur décisif est la **compétence de l'équipe** sur cet écosystème, ce qui sécurise le respect du délai de 4 semaines |

### Justification

- **LTS Microsoft** : support garanti jusqu'en novembre 2028, pas de risque de dépendance abandonnée pendant la durée du projet
- **Tooling intégré** : EF Core, ASP.NET Identity, JWT Bearer, OpenAPI sont tous officiellement maintenus par Microsoft, parfaitement intégrés, documentés en français comme en anglais. Pas de patchwork de bibliothèques tierces à arbitrer
- **Performance** : .NET 10 figure régulièrement dans le top 3 des benchmarks TechEmpower — un atout démontrable lors de la soutenance pour rassurer sur la scalabilité
- **C# 14 + nullable reference types** : niveau de sécurité de typage proche de Rust/TypeScript strict, ce qui réduit drastiquement la classe de bugs liés aux nulls et améliore la maintenabilité
- **Compétence** : stack maîtrisée, donc temps de mise en place compressé

### Choix Minimal API plutôt que Controllers

Minimal API (introduit en .NET 6, mature depuis .NET 8) :

- Code plus concis : un endpoint = une expression lambda, pas besoin de classe `Controller` + attributs
- Performances légèrement supérieures (moins de réflexion au démarrage)
- Excellente intégration avec l'OpenAPI generator de .NET 10
- Compatible avec la Clean Architecture : les endpoints sont déclarés dans des extension methods (`MapAuthEndpoints`, `MapFileEndpoints`...) et restent fines couches qui appellent les use cases de la couche `Application`

Le seul inconvénient — moins d'organisation native qu'avec des classes Controllers — est compensé par le découpage par feature dans la couche Api du projet.

## 3. Architecture interne : Clean Architecture en 4 projets

### Choix retenu

Découpage en quatre projets : `DataShare.Domain`, `DataShare.Application`, `DataShare.Infrastructure`, `DataShare.Api` (+ `DataShare.Tests`).

### Alternative évaluée

Un seul projet API monolithique avec dossiers internes (`Models/`, `Services/`, `Endpoints/`...). Plus rapide à mettre en place mais :

- Plus difficile de garantir l'absence de fuites entre couches (un développeur peut référencer EF Core depuis un endpoint sans effort)
- Difficile à tester en isolation (les use cases doivent toujours être instanciés avec leur infrastructure)
- Moins valorisant pour la soutenance (les investisseurs jugeant aussi sur la maintenabilité)

### Justification

La Clean Architecture est légèrement sur-dimensionnée pour 6 US, mais elle :

- **Démontre une démarche professionnelle** auprès des évaluateurs OpenClassrooms (la maintenabilité est un point évalué explicitement)
- **Isole les règles métier** : la génération de token de téléchargement, le calcul d'expiration, la validation de la taille de fichier vivent dans `Domain`/`Application` et sont testables sans infrastructure
- **Facilite l'évolution** : le passage de filesystem à S3 ne nécessitera qu'une nouvelle implémentation dans `Infrastructure`, sans toucher au reste du code
- **Coût cognitif assumé** : le découpage est appris une fois, et toute la suite du projet bénéficie de la structure

## 4. Front-end : Angular 21 + Material + Tailwind

### Choix retenu

**Angular 21** en mode **standalone components** (pas de NgModule) avec **signals**, **Angular Material** pour les composants accessibles, et **Tailwind CSS** pour le custom design.

### Alternatives évaluées

| Option | Évaluation |
|---|---|
| React + bibliothèque UI | Plus populaire, mais demande de choisir entre Vite/Next, plus de glue (routing, formulaires, validation, state) et moins d'opinion = plus de décisions à prendre dans un délai serré |
| Vue 3 | Excellente DX, mais écosystème UI accessible plus limité que Material |
| **Angular 21** ✓ | Choisi |

### Justification d'Angular

- **Framework complet** : routing, forms, http, validation, DI inclus de base. Aucun choix de tooling à arbitrer = on commence à coder le métier dès le jour 1
- **TypeScript natif** : aligné avec le typage strict côté back .NET, garantit la cohérence des contrats
- **CLI puissante** : `ng generate component`, `ng test`, `ng build --prod` réduisent le boilerplate
- **Standalone components et signals** : le bootstrap Angular n'est plus pénalisant comme avant — on peut démarrer une feature en une seule classe sans NgModule
- **Cours OpenClassrooms** : la stack est largement couverte par les supports de formation, donc compatible avec la trajectoire pédagogique

### Justification de Material + Tailwind

Les spécifications imposent l'**accessibilité prise en compte des utilisateurs PSH**. C'est non négociable et c'est un point d'évaluation. Plutôt que de rouler des composants accessibles à la main (champs, modales, tabs, snackbars...), on s'appuie sur **Angular Material** dont l'accessibilité WCAG AA est garantie par Google : labels ARIA, navigation clavier, contraste, focus management.

Material seul ne permet pas de coller au design des maquettes Figma (dégradé orange/corail, bouton upload central rond noir custom, layout très spécifique). On ajoute **Tailwind CSS** pour :

- Le système d'espacement / typo / couleurs custom
- Le dégradé orange/corail des landings et modales
- Le layout responsive (la maquette montre une vue desktop et une vue mobile)

Tailwind cohabite parfaitement avec Material via les classes utilitaires sur les wrappers et les composants Material restent stylables via leur API SCSS.

## 5. Base de données : PostgreSQL 16

### Choix retenu

**PostgreSQL 16** (image officielle `postgres:16-alpine`).

### Alternative évaluée

**MongoDB**. Écarté car :

- Le modèle de données est intrinsèquement relationnel : User → Files (1-N), File ↔ Tag (N-N) avec contrainte d'unicité par utilisateur. Tordre ce modèle dans du document store demanderait des compromis (dénormalisation, garanties d'unicité côté application…)
- ASP.NET Identity est conçu pour relationnel — l'utiliser avec MongoDB demanderait un provider tiers moins maintenu
- Les requêtes du `BackgroundService` de purge sont des `WHERE expires_at < now()` typiques relationnelles, parfaites pour Postgres

### Justification de Postgres vs autre relationnel

- Standard de l'industrie open source, sans risque de licence (vs MySQL Oracle)
- Excellent support des types modernes : `uuid`, `timestamptz`, `jsonb` si on en a besoin un jour
- Image Docker officielle stable et documentée
- Compatible 100% avec EF Core via le provider Npgsql

## 6. Stockage des fichiers : filesystem local (avec abstraction)

### Choix retenu

Stockage des blobs sur **système de fichiers local**, monté en **volume Docker bind** sur le host. Encapsulé derrière une interface `IFileStorageService` pour permettre une bascule future vers S3 ou MinIO sans toucher au code applicatif.

### Alternative évaluée

**AWS S3** directement. Écarté pour le MVP car :

- Demande un compte AWS, des credentials à gérer, une facturation à expliquer aux investisseurs
- Ajoute une dépendance externe : pas de démo offline possible
- Pour la démo, le bénéfice est nul : on ne va pas démontrer l'élasticité de S3

### Justification du filesystem + abstraction

- **Démarrage instantané** : `docker compose up` et tout fonctionne sans config externe
- **Réversible** : l'abstraction `IFileStorageService` est à coder une seule fois et la migration vers S3 devient une simple implémentation supplémentaire enregistrée dans le DI selon une variable d'environnement
- **Suffisant pour l'échelle MVP** : un disque host de quelques dizaines de Go absorbe largement les besoins

L'évolution vers S3 / MinIO est documentée explicitement dans `MAINTENANCE.md` (étape 5) comme l'une des principales pistes de scalabilité.

## 7. Authentification : ASP.NET Identity + JWT Bearer

### Choix retenu

**ASP.NET Core Identity** pour la gestion utilisateur (création, validation, hash de mot de passe), couplé à **JWT Bearer** pour l'authentification stateless des requêtes API.

### Justification

- **Identity** prend en charge gratuitement le hash PBKDF2 + sel aléatoire, la vérification d'unicité de l'email, la normalisation des emails, le `SecurityStamp` (utile pour invalider tous les tokens d'un utilisateur en cas de changement de mot de passe). Réinventer ces primitives serait risqué et chronophage
- **JWT Bearer** : standard d'authentification stateless le plus répandu, parfaitement supporté par .NET (`Microsoft.AspNetCore.Authentication.JwtBearer`) et exigence explicite des specs
- **Configuration minimale** : signature HS256 avec un secret en variable d'environnement, claims standards (`sub`, `email`, `exp`)

### Compromis assumé : pas de refresh token

Pour le MVP, on émet un **access token de 24h sans refresh token**. Avantages : pas de table `RefreshToken`, pas de logique de rotation, pas de risque de fuite de refresh token. Inconvénient : l'utilisateur doit se reconnecter au bout de 24h. Acceptable pour un MVP. L'évolution vers access (15min) + refresh (7j) est documentée dans `MAINTENANCE.md` et `SECURITY.md`.

## 8. Containerisation : Docker + docker-compose

### Choix retenu

`docker compose` orchestrant 3 services : `web` (nginx + Angular), `api` (.NET) et `db` (Postgres).

### Justification

- **Reproductibilité** : un évaluateur OpenClassrooms qui clone le repo lance `docker compose up` et obtient un environnement identique au mien
- **Isolation** : pas de pollution de l'environnement local de l'évaluateur (pas besoin d'installer .NET, Postgres, Node)
- **Couvre l'exigence "scripts de déploiement"** des specs sans avoir à écrire de scripts shell ad-hoc
- **Facilite la démo** : un crash ou un comportement étrange en démo ? `docker compose down -v && docker compose up` repart à zéro en une commande
- **Production-ready light** : le même `docker-compose.yml` (avec quelques variables d'environnement adaptées) peut servir pour un déploiement sur un VPS pour étendre le MVP au-delà de la démo

## 9. Tests : xUnit + Testcontainers + Playwright

### Choix retenus

| Niveau | Outil | Cible |
|---|---|---|
| Unit tests back | **xUnit + AwesomeAssertions + NSubstitute + MockQueryable** | Use cases isolés (génération de token, calcul d'expiration, parsers, services Application) |
| Tests d'intégration back | **Testcontainers.PostgreSQL + WebApplicationFactory** | Endpoints complets avec une vraie base PostgreSQL en container temporaire |
| Unit tests front | **Vitest** | Services Angular, guards, interceptors, composants |
| End-to-end | **Playwright** | 3 scénarios critiques : inscription/connexion/logout, upload+download connecté, upload protégé+download |

### Justification

- **xUnit** est le standard de facto en .NET, parfaitement intégré dans le tooling Visual Studio Code et la CLI dotnet
- **Testcontainers** permet d'exécuter les tests d'intégration contre un **vrai PostgreSQL** dans un container Docker temporaire, plutôt que de mocker EF Core ou d'utiliser SQLite in-memory. Cela évite la classe de bugs « ça passe en mock mais ça plante en prod » et donne une vraie confiance dans les tests
- **Playwright** offre des scénarios E2E lisibles, des screenshots/vidéos/traces automatiques en cas d'échec, le support multi-navigateurs (Chromium, Firefox, WebKit) et une exécution parallèle native — plus rapide et plus fiable que les alternatives

L'objectif de couverture est fixé à **70 %** (seuil indicatif des specs). Une capture d'écran du rapport de couverture sera incluse dans `TESTING.md`.

## 10. Observabilité : Serilog en JSON structuré

### Choix retenu

**Serilog** configuré dès `Program.cs`, sink console JSON structuré.

### Justification

- Les specs `PERF.md` exigent explicitement des **logs structurés** et un **suivi de métriques clés**
- Serilog est la bibliothèque de logging structuré de référence en .NET
- Sortie JSON sur stdout : compatible Docker (capturé automatiquement par `docker logs`) et facilement piped vers un agrégateur (Loki, Datadog, Elastic) en production
- Niveau ajustable par variable d'environnement sans recompilation

## 11. Compromis assumés et évolutions identifiées

| Compromis MVP | Pourquoi acceptable | Évolution prévue |
|---|---|---|
| Access token 24h sans refresh | Simplicité, suffisant pour démo | Refresh token rotatif (étape ultérieure) |
| Stockage filesystem (mono-host) | Démo contrainte à un seul serveur | Bascule vers S3/MinIO via `IFileStorageService` |
| Pas d'antivirus à l'upload | MVP, scope limité | Intégration ClamAV via stream |
| Pas d'email de confirmation | Simplification UX MVP | SendGrid / Mailgun + lien de confirmation |
| Token JWT en `localStorage` côté Angular | Simplicité, pas de back-channel | Cookie httpOnly + double-submit token |
| Pas de pagination de l'historique | Volume modéré attendu | Pagination keyset une fois > 100 fichiers / user |

Toutes ces évolutions sont documentées dans [`MAINTENANCE.md`](../MAINTENANCE.md) avec leur niveau de priorité.

## 12. Synthèse des dépendances principales

### Back-end (.NET 10)

- `Microsoft.AspNetCore.App` (.NET 10)
- `Microsoft.EntityFrameworkCore` + `Npgsql.EntityFrameworkCore.PostgreSQL`
- `Microsoft.AspNetCore.Identity.EntityFrameworkCore`
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `BCrypt.Net-Next` (hash des mots de passe fichier)
- `Serilog.AspNetCore` + `Serilog.Sinks.Console`
- `Swashbuckle.AspNetCore` (Swagger UI)
- Rate limiting via `Microsoft.AspNetCore.RateLimiting` (intégré)
- Tests : `xUnit`, `AwesomeAssertions`, `NSubstitute`, `MockQueryable.NSubstitute`, `Testcontainers.PostgreSql`, `Microsoft.AspNetCore.Mvc.Testing`

### Front-end (Angular)

- `@angular/core`, `@angular/router`, `@angular/forms`, `@angular/common`, `@angular/material`, `@angular/cdk` (21.x)
- `tailwindcss`, `postcss`, `autoprefixer`
- `rxjs` (inclus avec Angular)
- Tests : `vitest`, `@playwright/test`

### Infrastructure

- Docker Engine 24+, docker-compose v2
- Images : `mcr.microsoft.com/dotnet/aspnet:10.0`, `nginx:alpine`, `postgres:16-alpine`
