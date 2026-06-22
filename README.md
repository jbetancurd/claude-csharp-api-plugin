# Claude C# API Architecture Plugin

A comprehensive guide and template system for building production-grade C# APIs with SOLID principles, Onion architecture, DRY patterns, and TDD with xUnit.

## Quick Start

### Option 1: Clone from GitHub (Recommended)

**First machine:**
```bash
# Clone the repository
git clone https://github.com/jbetancurd/claude-csharp-api-plugin.git
cd claude-csharp-api-plugin

# You're done! The plugin is ready to use
```

**Other machines:**
```bash
# Clone same way
git clone https://github.com/jbetancurd/claude-csharp-api-plugin.git

# Keep it in sync
cd claude-csharp-api-plugin
git pull origin main  # Get latest updates
```

### Option 2: Use Directly from GitHub (No Clone)

You can reference the GitHub repository directly in your Claude Code settings without cloning:

```json
{
  "customTools": {
    "csharpApiGuide": {
      "type": "github",
      "repo": "jbetancurd/claude-csharp-api-plugin",
      "path": "/"
    }
  }
}
```

Or reference specific files:
```json
{
  "documentationPath": "https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/docs/decision-tree.md"
}
```

### Step 2: Integrate with Claude Code

If you cloned it locally, point Claude Code to the directory:

```json
{
  "csharpApiGuide": {
    "path": "/Users/yourname/claude-plugins/claude-csharp-api-plugin"
  }
}
```

Then reference in Claude conversations:
```
"See /docs/decision-tree.md"
"Use template from /templates/shared/services/"
"Check /checklists/architecture-audit.md"
```

### Step 3: Use in Your Projects

When starting a new C# API project:
1. Read `docs/decision-tree.md` to determine your project type
2. Use templates from `templates/[api-style]/`
3. Reference examples in `examples/[api-style]/`
4. Use checklists to audit your implementation

## Plugin Contents

### 📋 Documentation (`docs/`)
- **decision-tree.md** - Interactive questionnaire to guide project setup
- **architecture/** - Onion layers, SOLID principles, DRY patterns
- **api-styles/** - REST, RESTful, and GraphQL guides
- **communication/** - HTTP requests, WebSockets, Protobuf
- **resilience/** - Polly patterns, circuit breakers, retry policies
- **performance/** - Caching strategies, memory optimization

### 📦 Templates (`templates/`)
Organized by API style with shared components:
- **rest-api/** - REST API templates
- **restful-api/** - RESTful API templates
- **graphql-api/** - GraphQL API templates
- **shared/** - Reusable templates (entities, repositories, services, controllers, tests)

### 💡 Examples (`examples/`)
Three complete, working implementations for each API style:
- **REST API:**
  - `todo-microservice/` - Lightweight microservice
  - `ecommerce-full-api/` - Complete REST API with all layers
  - `notification-service/` - Standalone background service

- **RESTful API:**
  - `blog-microservice/` - RESTful microservice
  - `inventory-full-api/` - Complete RESTful API
  - `auth-service/` - Standalone authentication service

- **GraphQL API:**
  - `user-microservice/` - GraphQL microservice
  - `social-full-api/` - Complete GraphQL API
  - `analytics-service/` - Standalone analytics service

### ✅ Checklists (`checklists/`)
- **architecture-audit.md** - Verify Onion architecture compliance
- **api-style-selection.md** - Ensure API style aligns with requirements
- **resilience-audit.md** - Check Polly and resilience patterns
- **performance-audit.md** - Validate caching, memory, and performance

## Key Topics Covered

### Architecture
- ✅ Onion Architecture with clear layer separation
- ✅ Dependency Injection and IoC containers
- ✅ SOLID Principles (SRP, OCP, LSP, ISP, DIP)
- ✅ DRY - Component reuse and abstraction patterns

### Data Access
- ✅ ORM Decision Guide (Dapper vs Entity Framework with Code-First)
- ✅ Repository Pattern
- ✅ Unit of Work Pattern
- ✅ Specification Pattern for complex queries

### API Patterns
- ✅ REST (simple, action-based)
- ✅ RESTful (resource-based with proper HTTP semantics)
- ✅ GraphQL (query language for APIs)

### Communication
- ✅ HTTP requests (standard REST/RESTful)
- ✅ WebSockets (real-time bidirectional)
- ✅ Protocol Buffers (efficient serialization for microservices)

### Resilience
- ✅ Polly Policies (Retry, Circuit Breaker, Timeout, Fallback)
- ✅ Health Checks
- ✅ Graceful degradation
- ✅ Bulkhead pattern

### Testing & TDD
- ✅ xUnit fundamentals
- ✅ AAA (Arrange-Act-Assert) pattern
- ✅ Unit test templates
- ✅ Integration test patterns
- ✅ Fixture and test helper patterns

### Performance & Memory
- ✅ Caching strategies (in-memory, distributed)
- ✅ Memory management and pooling
- ✅ Database query optimization
- ✅ Async/await best practices

## Project Type Determination

The plugin helps you choose the right architecture based on:

| Aspect | Question |
|--------|----------|
| **Project Type** | Is this a microservice, full REST API, or standalone service? |
| **API Style** | Should it be REST (action-based), RESTful (resource-based), or GraphQL? |
| **Communication** | Do you need HTTP only, WebSockets, or both? Use Protobuf? |
| **ORM** | Would Dapper or Entity Framework Code-First fit better? |
| **Resilience** | Do you need Polly for retries, circuit breaker, timeouts? |
| **Caching** | Is in-memory or distributed caching needed? |

## File Organization

```
claude-csharp-api-plugin/
├── README.md                          # This file
├── CLAUDE.md                          # Plugin documentation
├── plugin-manifest.json               # Plugin metadata
├── docs/
│   ├── decision-tree.md              # Interactive questionnaire
│   ├── architecture/
│   │   ├── onion-architecture.md
│   │   ├── solid-principles.md
│   │   ├── dry-principles.md
│   │   └── dependency-injection.md
│   ├── api-styles/
│   │   ├── rest-guide.md
│   │   ├── restful-guide.md
│   │   └── graphql-guide.md
│   ├── communication/
│   │   ├── http-requests.md
│   │   ├── websockets.md
│   │   ├── grpc.md
│   │   └── protobuf-guide.md
│   ├── resilience/
│   │   └── polly-patterns.md
│   └── performance/
│       └── caching-strategies.md
├── templates/
│   ├── rest-api/
│   │   ├── microservice/
│   │   ├── full-api/
│   │   └── standalone/
│   ├── restful-api/
│   │   ├── microservice/
│   │   ├── full-api/
│   │   └── standalone/
│   ├── graphql-api/
│   │   ├── microservice/
│   │   ├── full-api/
│   │   └── standalone/
│   └── shared/
│       ├── entities/
│       ├── repositories/
│       ├── services/
│       ├── controllers/
│       ├── middleware/
│       └── tests/
├── examples/
│   ├── rest-api/
│   ├── restful-api/
│   └── graphql-api/
├── checklists/
│   ├── architecture-audit.md
│   ├── api-style-selection.md
│   ├── resilience-audit.md
│   └── performance-audit.md
└── .claude/
    └── settings-integration.md
```

## How to Use This Plugin

### Scenario 1: Using Locally (Cloned)

**Best for:** Development across multiple machines, offline access

```bash
# Clone once
git clone https://github.com/jbetancurd/claude-csharp-api-plugin.git
cd claude-csharp-api-plugin

# Configure Claude Code to point to local path
# In settings.json: "path": "/full/path/to/claude-csharp-api-plugin"

# Keep updated
git pull origin main
```

Then reference in Claude Code:
```
"See /docs/decision-tree.md in the plugin"
"Use /templates/shared/repositories/ef-repository.template.cs"
```

### Scenario 2: Using from GitHub (No Clone)

**Best for:** Single machine, minimal setup

Reference directly in Claude Code by GitHub path:
```
Read https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/docs/decision-tree.md
See examples at github.com/jbetancurd/claude-csharp-api-plugin/tree/main/examples
```

Or ask Claude: "Use patterns from the C# API plugin on GitHub"

### Scenario 3: Copy What You Need

**Best for:** Starting a new project quickly

```bash
# Clone temporarily
git clone https://github.com/jbetancurd/claude-csharp-api-plugin.git
cd claude-csharp-api-plugin

# Copy relevant template to your project
cp templates/shared/services/application-service.template.cs ~/my-project/Services/

# Copy example to study
cp -r examples/rest-api/todo-microservice/ ~/my-project/reference/

# You can delete the clone, files are in your project
```

---

## Using with Claude Code

### Method 1: Local Reference (Fast)
```bash
# Terminal: clone once
git clone https://github.com/jbetancurd/claude-csharp-api-plugin.git ~/claude-plugins/

# In Claude Code chat:
"See /docs/decision-tree.md in ~/claude-plugins/claude-csharp-api-plugin/"
```

### Method 2: GitHub Reference (No Local Storage)
```
Tell Claude:
"I'm using https://github.com/jbetancurd/claude-csharp-api-plugin"
"Show me the Serilog setup from /templates/shared/logging/"
"Review against /checklists/architecture-audit.md"
```

### Method 3: Copy Template Pattern
```
Tell Claude:
"Use this as a template: [copy-paste from GitHub]"
"Generate following /docs/api-styles/restful-guide.md"
```

---

### For New Projects
1. Run the decision tree to determine project type and API style
2. Copy the appropriate template from `templates/`
3. Follow the architecture and checklist guides
4. Refer to examples for patterns you're unsure about

### For Code Review
1. Use the relevant checklist to audit architecture
2. Verify layer separation and dependency flow
3. Check resilience and caching implementations
4. Validate test coverage with xUnit patterns

### For Learning
1. Study the examples that match your use case
2. Read the corresponding architecture documentation
3. Understand why certain patterns are recommended
4. Apply patterns incrementally to your projects

## Using Across Multiple Machines

### Setup (First Time)

**Machine 1:**
```bash
# Clone to a standard location
mkdir -p ~/claude-plugins
git clone https://github.com/jbetancurd/claude-csharp-api-plugin.git ~/claude-plugins/claude-csharp-api-plugin
```

**Machine 2 (and others):**
```bash
# Same location for consistency
mkdir -p ~/claude-plugins
git clone https://github.com/jbetancurd/claude-csharp-api-plugin.git ~/claude-plugins/claude-csharp-api-plugin
```

### Keeping Updated (Any Machine)

```bash
# Get latest from GitHub
cd ~/claude-plugins/claude-csharp-api-plugin
git pull origin main
```

### Configure Claude Code (All Machines)

Add to Claude Code settings on each machine:
```json
{
  "plugins": {
    "csharpApi": {
      "path": "~/claude-plugins/claude-csharp-api-plugin"
    }
  }
}
```

Or use absolute path:
```json
{
  "plugins": {
    "csharpApi": {
      "path": "/Users/yourname/claude-plugins/claude-csharp-api-plugin"
    }
  }
}
```

---

## Integration with Claude Code

This plugin works with Claude Code's inference system. When working on C# API projects:

**Reference in Claude conversations:**
```
"See /docs/decision-tree.md"
"Use /templates/shared/services/application-service.template.cs as template"
"Review against /checklists/architecture-audit.md"
"Show example from /examples/rest-api/todo-microservice/"
"Apply pattern from /docs/api-styles/restful-guide.md"
```

**Or ask Claude directly:**
```
"Generate a service following the plugin's application service pattern"
"Create Serilog setup using the plugin template"
"Review this code against the architecture checklist"
"Explain the Onion architecture from the plugin"
```

(Full integration details in `.claude/settings-integration.md`)

## Contributing

This is a personal plugin designed for your projects. To extend it:
1. Add new examples as you create projects
2. Update templates based on lessons learned
3. Add new documentation as patterns evolve
4. Keep examples working and up-to-date

## License

Personal use plugin - free to modify and extend.

## Direct GitHub URLs (No Clone Needed)

Use these URLs to reference plugin content directly:

**Decision Tree:**
```
https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/docs/decision-tree.md
```

**Architecture Guides:**
```
https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/docs/architecture/onion-architecture.md
https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/docs/architecture/solid-principles.md
```

**API Style Guides:**
```
https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/docs/api-styles/rest-guide.md
https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/docs/api-styles/restful-guide.md
https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/docs/api-styles/graphql-guide.md
```

**Templates:**
```
https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/templates/shared/repositories/ef-repository.template.cs
https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/templates/shared/services/application-service.template.cs
https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/templates/shared/tests/xunit-test.template.cs
```

**Checklists:**
```
https://raw.githubusercontent.com/jbetancurd/claude-csharp-api-plugin/main/checklists/architecture-audit.md
```

**Note:** Replace `/raw/` with `/blob/` in the URL if you want to view on GitHub.com instead of downloading.

---

## Quick Commands Reference

```bash
# Clone the plugin
git clone https://github.com/jbetancurd/claude-csharp-api-plugin.git

# Update to latest
git pull origin main

# Browse on GitHub
https://github.com/jbetancurd/claude-csharp-api-plugin

# View decision tree
https://github.com/jbetancurd/claude-csharp-api-plugin/blob/main/docs/decision-tree.md

# View templates
https://github.com/jbetancurd/claude-csharp-api-plugin/tree/main/templates

# View examples
https://github.com/jbetancurd/claude-csharp-api-plugin/tree/main/examples
```

---

## Next Steps

### First Time Setup
1. Clone: `git clone https://github.com/jbetancurd/claude-csharp-api-plugin.git`
2. Start with `docs/decision-tree.md` for your next C# API project
3. Copy templates and reference examples as you build

### Multiple Machines
1. Clone to standard location on each machine
2. Keep updated with `git pull`
3. Configure Claude Code to point to local clone

### No Clone (GitHub Only)
1. Reference URLs directly from GitHub
2. Copy templates as needed
3. Use raw.githubusercontent.com URLs in Claude Code settings

---

**GitHub Repository:** https://github.com/jbetancurd/claude-csharp-api-plugin

**Last Updated:** June 2026
**Status:** Ready for use
**License:** MIT
