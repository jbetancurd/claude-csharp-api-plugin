# Claude C# API Architecture Plugin

A comprehensive guide and template system for building production-grade C# APIs with SOLID principles, Onion architecture, DRY patterns, and TDD with xUnit.

## Quick Start

### 1. Install Locally
```bash
git clone https://github.com/yourusername/claude-csharp-api-plugin.git
cd claude-csharp-api-plugin
```

### 2. Integrate with Claude Code
Copy the plugin path to your Claude Code settings:
```json
{
  "customTools": {
    "csharpApiGuide": {
      "path": "/path/to/claude-csharp-api-plugin"
    }
  }
}
```

### 3. Use in Your Projects
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

## Integration with Claude Code

This plugin works with Claude Code's inference system. When working on C# API projects:

```
/c#-api-audit             # Audit current project against guidelines
/c#-api-scaffold          # Generate project structure from template
/c#-api-explain [file]    # Explain architecture patterns in file
/c#-orm-select            # Guide ORM selection (Dapper vs EF)
/c#-test-generate         # Generate xUnit tests from template
```

(Commands will be configured in `.claude/settings-integration.md`)

## Contributing

This is a personal plugin designed for your projects. To extend it:
1. Add new examples as you create projects
2. Update templates based on lessons learned
3. Add new documentation as patterns evolve
4. Keep examples working and up-to-date

## License

Personal use plugin - free to modify and extend.

## Next Steps

1. Clone this repository to your machines
2. Add it to your Claude Code settings
3. Start with `docs/decision-tree.md` for your next C# API project
4. Reference examples and templates as you build

---

**Last Updated:** June 2026
**Status:** Ready for use
