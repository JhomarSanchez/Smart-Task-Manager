<div align="center">

<br/>

<img src="https://img.shields.io/badge/-Smart%20Task%20Manager-0A0A0A?style=for-the-badge&logoColor=white" height="50" alt="Smart Task Manager"/>

<br/>
<br/>

<p>
  <strong>Un gestor de tareas Kanban de alto rendimiento, con IA integrada y diseño editorial de primera clase.</strong><br/>
  Construido con Angular 22, ASP.NET Core (.NET 10) y PostgreSQL bajo una arquitectura limpia y escalable.
</p>

<br/>

<!-- Badges de stack -->
<p>
  <img src="https://img.shields.io/badge/Angular-22+-DD0031?style=flat-square&logo=angular&logoColor=white" alt="Angular 22+"/>
  <img src="https://img.shields.io/badge/.NET-10-512BD4?style=flat-square&logo=dotnet&logoColor=white" alt=".NET 10"/>
  <img src="https://img.shields.io/badge/PostgreSQL-16-4169E1?style=flat-square&logo=postgresql&logoColor=white" alt="PostgreSQL"/>
  <img src="https://img.shields.io/badge/Docker-ready-2496ED?style=flat-square&logo=docker&logoColor=white" alt="Docker"/>
  <img src="https://img.shields.io/badge/Tailwind_CSS-4-06B6D4?style=flat-square&logo=tailwindcss&logoColor=white" alt="Tailwind CSS"/>
</p>

<!-- Badges de calidad -->
<p>
  <img src="https://img.shields.io/badge/Architecture-Clean_Architecture-4C86FF?style=flat-square" alt="Clean Architecture"/>
  <img src="https://img.shields.io/badge/State-Angular_Signals-FF5FA8?style=flat-square" alt="Angular Signals"/>
  <img src="https://img.shields.io/badge/AI-Gemini_%7C_OpenAI-34D399?style=flat-square" alt="AI Integration"/>
  <img src="https://img.shields.io/badge/Tests-xUnit_%2B_Vitest-FFB800?style=flat-square" alt="Tests"/>
  <img src="https://img.shields.io/badge/API-REST_Minimal_APIs-7C3AED?style=flat-square" alt="Minimal APIs"/>
</p>

<br/>

---

</div>

## ✨ ¿Qué es Smart Task Manager?

**Smart Task Manager** es una aplicación web full-stack de productividad personal que combina la interfaz de un tablero Kanban moderno con la inteligencia artificial generativa. Permite a cualquier usuario organizar sus tareas de forma visual, arrastrarlas entre columnas personalizadas, y —si así lo desea— crear nuevas tareas simplemente escribiendo una frase en lenguaje natural, dejando que la IA interprete la intención, prioridad y fecha.

### ¿Por qué existe?
La mayoría de los gestores de tareas obliga al usuario a rellenar formularios extensos. **Smart Task Manager** invierte ese paradigma: el usuario escribe `"Reunión con el cliente el viernes a las 3pm, urgente"` y la aplicación crea la tarea automáticamente con título, descripción, fecha y prioridad inferidos.

> La IA es **completamente opcional**. Si el usuario no configura una clave API, la app funciona perfectamente como un Kanban clásico de alta calidad.

---

## 🖼️ Interfaz y Diseño

El diseño sigue la filosofía del **Minimalismo Editorial** (inspirado en el estilo tipográfico suizo), con:

- **Modo Claro / Oscuro / Sistema** con persistencia en `localStorage`
- **Paleta dual**: Negro puro `#000` / Blanco puro `#FFF` como lienzo, con **Azul Eléctrico** `#4C86FF` y **Rosa** `#FF5FA8` como únicos acentos de acción
- **Tipografía**: Inter (sans), JetBrains Mono (mono), Source Serif 4 (display)
- **Glassmorphism** sutil en el header y modales
- **Animaciones**: `fadeIn`, drag placeholders, hover transitions, micro-interactions

---

## 🛠️ Stack Tecnológico

### Backend

| Capa | Tecnología | Rol |
|------|-----------|-----|
| **API** | ASP.NET Core (.NET 10) — Minimal APIs | Endpoints REST, middleware, CORS |
| **Aplicación** | FluentValidation + Result Pattern | Validación, DTOs, use cases |
| **Dominio** | C# puro — Value Objects, Enums | Reglas de negocio sin dependencias externas |
| **Infraestructura** | EF Core + Npgsql | Persistencia en PostgreSQL, migraciones Code-First |
| **IA** | Google Generative AI SDK / OpenAI SDK | Parsing de lenguaje natural con structured outputs |

### Frontend

| Capa | Tecnología | Rol |
|------|-----------|-----|
| **Framework** | Angular 22+ (Standalone Components) | UI, routing, inyección de dependencias |
| **Estado** | Angular Signals (`signal`, `computed`, `effect`) | Estado reactivo sin RxJS (excepto HTTP) |
| **Estilos** | Tailwind CSS v4 + CSS Variables HSL | Sistema de tokens de diseño adaptable |
| **Drag & Drop** | `@angular/cdk/drag-drop` | Kanban interactivo entre columnas |
| **HTTP** | `HttpClient` + interceptores | Comunicación con la API REST |
| **Tests** | Vitest (frontend) + xUnit (backend) | Cobertura unitaria e integración |

### Infraestructura

| Herramienta | Uso |
|------------|-----|
| **Docker** | PostgreSQL 16 Alpine en contenedor con volumen persistente |
| **Git** | Control de versiones |
| **dotnet-ef** | CLI para migraciones de base de datos |

---

## 📂 Estructura del Proyecto

```
smart task/task/
│
├── 📁 src/
│   ├── 📁 Backend/
│   │   ├── SmartTask.Domain/          # Entidades, enums, reglas de negocio (0 dependencias)
│   │   ├── SmartTask.Application/     # DTOs, validadores, handlers, interfaces
│   │   ├── SmartTask.Infrastructure/  # EF Core, repositorios, AI services, migraciones
│   │   └── SmartTask.Api/             # Minimal API endpoints, Program.cs, middleware
│   │
│   └── 📁 Frontend/src/app/
│       ├── core/                      # TaskService (Signals store), modelos, interceptores
│       ├── features/
│       │   ├── kanban/                # KanbanBoardComponent — tablero principal
│       │   └── settings/              # SettingsModalComponent — configuración de IA
│       └── shared/
│           ├── task-card/             # TaskCardComponent — tarjeta individual
│           └── components/task-dialog/ # TaskDialogComponent — modal de creación/edición
│
├── 📄 run-dev.ps1                     # ▶️ Script único para iniciar todo el entorno
├── 📄 apply-migration.ps1             # 🗄️ Script para aplicar migraciones DB vía Docker
├── 📄 SmartTask.sln                   # Solución .NET
└── 📁 .ai/                           # Contexto para agentes de IA (AGENTS.md, etc.)
```

---

## 🚀 Inicio Rápido

### Prerrequisitos

Asegúrate de tener instalado:

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- [Node.js 20+](https://nodejs.org/) + npm
- [Docker Desktop](https://www.docker.com/products/docker-desktop/)
- [dotnet-ef](https://learn.microsoft.com/en-us/ef/core/cli/dotnet) (herramienta global)

```powershell
# Instalar dotnet-ef globalmente (solo una vez)
dotnet tool install --global dotnet-ef
```

---

### ⚡ Un Solo Comando para Todo

Desde la raíz del proyecto (`smart task/task/`):

```powershell
.\run-dev.ps1
```

Este script realiza automáticamente:
1. ✅ Inicia el contenedor Docker de PostgreSQL (si no está corriendo)
2. ✅ Lanza el backend ASP.NET Core en `http://localhost:5100`
3. ✅ Lanza el frontend Angular en `http://localhost:4200`

Ambos servidores quedan activos en ventanas independientes. Presiona `Ctrl+C` para detener.

---

### 🔧 Inicio Manual (paso a paso)

Si prefieres iniciar cada servicio manualmente:

**1. Base de datos PostgreSQL**
```powershell
docker run --name smarttask-postgres `
  -e POSTGRES_DB=smarttask `
  -e POSTGRES_USER=postgres `
  -e POSTGRES_PASSWORD=postgres `
  -p 5432:5432 `
  -v smarttask_data:/var/lib/postgresql/data `
  -d postgres:16-alpine
```

**2. Backend .NET**
```powershell
# Aplicar migraciones (primera vez o tras actualizar)
dotnet ef database update `
  --project src\Backend\SmartTask.Infrastructure `
  --startup-project src\Backend\SmartTask.Api

# Iniciar el servidor
dotnet run --project src\Backend\SmartTask.Api
```

**3. Frontend Angular**
```powershell
cd src\Frontend
npm install          # Solo la primera vez
npm start            # Equivale a: ng serve
```

---

### 🗄️ Aplicar Migraciones Pendientes

Si el servidor backend ya está corriendo, usa el script alternativo que ejecuta el SQL directamente en Docker:

```powershell
.\apply-migration.ps1
```

---

## 🤖 Configuración de la IA (Opcional)

La integración con IA es completamente opcional. La app funciona sin configurarla.

1. Abre la app en `http://localhost:4200`
2. Haz clic en el ícono ⚙️ **AI** en la barra superior
3. Configura tu proveedor preferido:

| Campo | Descripción |
|-------|-------------|
| **Proveedor** | `Gemini` o `OpenAI` |
| **API Key** | Tu clave personal de la API |
| **Modelo** | Ej: `gemini-1.5-flash`, `gpt-4o-mini` |

> **Fallback garantizado:** Si la clave es inválida, está vacía, o el servicio falla, la app abre el formulario manual pre-llenado con el texto que escribiste. El sistema **nunca falla** por culpa de la IA.

---

## 🧪 Ejecutar Tests

**Frontend (Vitest)**
```powershell
cd src\Frontend
npx ng test --watch=false
```

**Backend (xUnit)**
```powershell
# Desde la raíz del proyecto
dotnet test
```

---

## 🌐 API Reference

El backend expone los siguientes endpoints REST en `http://localhost:5100`:

| Método | Ruta | Descripción |
|--------|------|-------------|
| `GET` | `/api/tasks` | Lista todas las tareas ordenadas por columna y posición |
| `GET` | `/api/tasks/{id}` | Obtiene una tarea por ID |
| `POST` | `/api/tasks` | Crea una tarea manualmente |
| `PUT` | `/api/tasks/{id}` | Actualiza una tarea (título, estado, posición...) |
| `DELETE` | `/api/tasks/{id}` | Elimina una tarea |
| `POST` | `/api/tasks/smart-parse` | Parsea texto en lenguaje natural con IA |

Los enums se serializan como strings: `"Todo"`, `"Doing"`, `"Done"` / `"Low"`, `"Medium"`, `"High"`.

---

## ⚙️ Variables de Configuración

Edita `src/Backend/SmartTask.Api/appsettings.Development.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=smarttask;Username=postgres;Password=postgres"
  },
  "AiSettings": {
    "Enabled": true,
    "Provider": "Gemini",
    "ApiKey": "TU_API_KEY_AQUÍ",
    "ModelName": "gemini-1.5-flash"
  }
}
```

---

## 🏗️ Arquitectura Clean Architecture

```
┌─────────────────────────────────────────────────────┐
│                   Angular Frontend                   │
│  Signals Store ──► Feature Components ──► CDK D&D   │
└────────────────────────────┬────────────────────────┘
                             │ REST (HTTP/JSON)
┌────────────────────────────▼────────────────────────┐
│              ASP.NET Core Minimal APIs               │
│                                                     │
│  ┌──────────┐  ┌──────────────┐  ┌──────────────┐  │
│  │  Domain  │◄─│  Application │◄─│Infrastructure│  │
│  │ Entities │  │ Handlers/DTOs│  │ EF Core / AI │  │
│  │  Enums   │  │ FluentValid. │  │ Repositories │  │
│  └──────────┘  └──────────────┘  └──────┬───────┘  │
└─────────────────────────────────────────┼───────────┘
                                          │
                             ┌────────────▼───────────┐
                             │   PostgreSQL 16         │
                             │   (Docker Container)    │
                             └────────────────────────┘
```

---

## 📜 Licencia

Este proyecto está bajo la licencia **MIT**. Úsalo, modifícalo y compártelo libremente.

---

<div align="center">

Hecho con ♥ · Angular 22 · .NET 10 · PostgreSQL · Gemini AI

</div>
