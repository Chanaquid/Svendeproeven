# RentIt – Installationsguide

## Forudsætninger

Sørg for at følgende er installeret på din maskine før du går i gang:

| Værktøj | Version | Link |
|--------|---------|------|
| Node.js | v18 eller højere | [nodejs.org](https://nodejs.org) |
| Angular CLI | Seneste | `npm install -g @angular/cli` |
| .NET SDK | Version 9 | [dotnet.microsoft.com](https://dotnet.microsoft.com/download) |
| Docker Desktop | Seneste | [docker.com](https://www.docker.com/products/docker-desktop) |
| Git | Seneste | [git-scm.com](https://git-scm.com) |

---

## 1. Klon repositoriet

Åbn en terminal og kør følgende kommandoer:

```bash
git clone https://github.com/Chanaquid/Svendeproeven.git
cd Svendeproeven
```

---

## 2. Konfigurer miljøfiler

Projektet kræver en `.env` fil som indeholder hemmelige nøgler og forbindelsesstrenge. Denne fil er ikke inkluderet i repositoriet af sikkerhedsmæssige årsager.

Download filen her: [Google Drive](https://drive.google.com/drive/folders/1UTy6Q6d1KqymlwMO3rA0M3oRICdaVUrZ?usp=drive_link)

Placer den downloadede `.env` fil i **projektets rodmappe** (samme niveau som `docker-compose.yml`):

```
RentIt/
├── .env               ← placer filen her
├── docker-compose.yml
├── backend/
└── frontend/
```

> **Bemærk:** Hvis filen hedder `env` i stedet for `.env`, skal du omdøbe den til `.env`.

---

## 3. Start backend (Docker)

Sørg for at Docker Desktop kører, og kør derefter følgende kommando fra projektets rodmappe:

```bash
docker compose up --build -d
```

Dette vil automatisk:
- Starte SQL Server databasen
- Bygge og starte API'et på `http://localhost:7183`
- Køre database migrationer
- Oprette standardkategorier og admin-bruger

Verificér at begge containere kører korrekt:

```bash
docker ps
```

Du bør se følgende output med status **healthy**:

```
rentit-db      Up X minutes (healthy)
rentit-api-1   Up X minutes
```

---

## 4. Verificér API'et

Åbn din browser og gå til:

```
http://localhost:7183/swagger
```

Du bør se Swagger UI med alle tilgængelige endpoints. Hvis siden ikke loader, så vent 30 sekunder og prøv igen — databasen kan være ved at starte op.

---

## 5. Start frontend

Åbn en **ny terminal**, naviger til frontend mappen og installer afhængigheder:

```bash
cd frontend
npm install
ng serve
```

Åbn din browser og gå til:

```
http://localhost:4200
```

---

## 6. Log ind som administrator

Brug følgende legitimationsoplysninger til at logge ind som systemadministrator:

```
Email:      admin@rentit.dk
Password:   AdminPassword123!
```

---

## Stop projektet

```bash
# Stop containere (data bevares)
docker compose down

# Stop containere og slet al data (frisk start)
docker compose down -v
```

---

## Fejlfinding

**Containere starter ikke?**
Tjek logfiler for fejlbeskeder:
```bash
docker logs rentit-api-1
```

**Databaseforbindelse fejler?**
Sørg for at Docker Desktop kører, og prøv en frisk start:
```bash
docker compose down -v
docker compose up --build -d
```

**Frontend kan ikke nå API'et?**
- Kontrollér at API'et kører på port 7183
- Kontrollér at du bruger `http://` og ikke `https://`

**Port er allerede i brug?**
```bash
# Windows
netstat -ano | findstr :7183
```

---

## 📌 Bemærkning – Delt database med eksisterende data

Vil du se systemet med rigtige data?
En cloud-database er tilgængelig med præudfyldt data. For at bruge den skal du erstatte to filer i dit lokale projekt med de opdaterede versioner fra Google Drive mappen:

.env
docker-compose.yml

Download begge filer fra mappen og erstat de eksisterende filer i dit projekt. Derefter kør:
bashdocker compose up -d
Det er alt — ingen andre ændringer er nødvendige.

Google Drive mappen: [Google Drive](https://drive.google.com/drive/folders/1UTy6Q6d1KqymlwMO3rA0M3oRICdaVUrZ?usp=drive_link)
