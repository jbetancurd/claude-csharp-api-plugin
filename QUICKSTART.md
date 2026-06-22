# Quick Start Guide

Your C# API Architecture Plugin is ready. Here's how to get started in 5 minutes.

## What You Have

✅ Complete plugin structure for building production C# APIs
✅ Comprehensive documentation (1000+ lines)
✅ Working code templates
✅ 9 complete example projects
✅ Architecture audit checklists
✅ TDD/xUnit patterns
✅ Polly resilience patterns

## File Structure Created

```
claude-csharp-api-plugin/
├── README.md                          # Project overview
├── CLAUDE.md                          # Plugin documentation
├── QUICKSTART.md                      # This file
├── plugin-manifest.json               # Plugin metadata
│
├── docs/                              # 📖 DOCUMENTATION
│   ├── decision-tree.md               ← START HERE for new projects
│   ├── architecture/
│   │   ├── onion-architecture.md      # 4-layer architecture guide
│   │   ├── solid-principles.md        # SOLID explained
│   │   ├── dry-principles.md          # DRY patterns
│   │   └── dependency-injection.md    # DI setup
│   ├── api-styles/
│   │   ├── rest-guide.md              # Action-based API
│   │   ├── restful-guide.md           # Resource-based API
│   │   └── graphql-guide.md           # Query language API
│   ├── communication/
│   │   ├── http-requests.md
│   │   ├── websockets.md
│   │   └── protobuf-guide.md
│   ├── resilience/
│   │   └── polly-patterns.md          # Retry, circuit breaker, etc.
│   └── performance/
│       └── caching-strategies.md
│
├── templates/                         # 📋 REUSABLE TEMPLATES
│   ├── rest-api/
│   │   ├── microservice/              # Template structure
│   │   ├── full-api/
│   │   └── standalone/
│   ├── restful-api/                   # Same structure
│   ├── graphql-api/                   # Same structure
│   └── shared/
│       ├── repositories/
│       │   └── dapper-repository.template.cs
│       ├── services/
│       │   └── application-service.template.cs
│       ├── controllers/
│       ├── tests/
│       │   └── xunit-test.template.cs
│       ├── middleware/
│       └── resilience/
│
├── examples/                          # 💡 WORKING EXAMPLES
│   ├── rest-api/
│   │   ├── todo-microservice/         # Complete, ready to run
│   │   ├── ecommerce-full-api/
│   │   └── notification-service/
│   ├── restful-api/
│   │   ├── blog-microservice/
│   │   ├── inventory-full-api/
│   │   └── auth-service/
│   └── graphql-api/
│       ├── user-microservice/
│       ├── social-full-api/
│       └── analytics-service/
│
├── checklists/                        # ✅ AUDIT CHECKLISTS
│   ├── architecture-audit.md          # Verify Onion Architecture
│   ├── api-style-selection.md
│   ├── resilience-audit.md
│   └── performance-audit.md
│
└── .claude/
    └── settings-integration.md        # Claude Code integration guide
```

## 5-Minute Start Path

### For a New Project

**1. Open the decision tree:**
```
/docs/decision-tree.md
```

**2. Answer the 9 questions** (2 min):
- What type of project? (Microservice, Full API, Standalone)
- What API style? (REST, RESTful, GraphQL)
- Communication? (HTTP, WebSockets, both)
- ORM? (Dapper, EF Code-First)
- Resilience? (Basic, Polly)
- Caching? (None, In-Memory, Distributed)

**3. Find your path** (1 min):
Based on answers, decision tree shows:
- Which template to copy: `/templates/{style}/{type}/`
- Which example to study: `/examples/{style}/{type}/`

**4. Study the example** (2 min):
Look at `/examples/rest-api/todo-microservice/Program.cs` for complete working code showing:
- Domain layer (Entities)
- Application layer (Services, DTOs)
- Infrastructure layer (Repositories, DbContext)
- Presentation layer (Controllers)
- Dependency injection setup
- Tests (xUnit with AAA pattern)

**5. Copy and customize** for your project

---

## Common Scenarios (Choose Your Path)

### Scenario 1: REST Microservice with Dapper (FAST & LIGHT)

```
decision-tree answer:
  Project Type → Microservice
  API Style → REST
  ORM → Dapper
  Resilience → Yes (Polly)

Copy from:  templates/rest-api/microservice/
Study:      examples/rest-api/todo-microservice/Program.cs
Follow:     docs/api-styles/rest-guide.md
Repository: templates/shared/repositories/dapper-repository.template.cs
Tests:      templates/shared/tests/xunit-test.template.cs
```

### Scenario 2: RESTful Full API with EF Code-First (COMPLETE)

```
decision-tree answer:
  Project Type → Full REST API
  API Style → RESTful
  ORM → Entity Framework Code-First
  Caching → Distributed (Redis)
  Resilience → Yes (Polly)

Copy from:  templates/restful-api/full-api/
Study:      examples/restful-api/ecommerce-full-api/
Follow:     docs/api-styles/restful-guide.md
Services:   templates/shared/services/application-service.template.cs
Tests:      templates/shared/tests/xunit-test.template.cs
Resilience: docs/resilience/polly-patterns.md
```

### Scenario 3: GraphQL API (FLEXIBLE)

```
decision-tree answer:
  API Style → GraphQL
  (Other answers same as above)

Copy from:  templates/graphql-api/{type}/
Study:      examples/graphql-api/{specific}/
Follow:     docs/api-styles/graphql-guide.md
```

---

## Key Files by Task

### Starting a New Project
- 📖 `docs/decision-tree.md` - Questionnaire
- 📋 Template in `/templates/`
- 💡 Example in `/examples/`

### Understanding Architecture
- 📖 `docs/architecture/onion-architecture.md` - Layer definitions
- 📖 `docs/architecture/solid-principles.md` - Design principles
- ✅ `checklists/architecture-audit.md` - Verify your code

### Building Features
- 📋 `templates/shared/services/application-service.template.cs` - Services
- 📋 `templates/shared/repositories/dapper-repository.template.cs` - Data access
- 📋 `templates/shared/tests/xunit-test.template.cs` - Tests (TDD)
- 💡 `examples/rest-api/todo-microservice/Program.cs` - Complete example

### Implementing Resilience
- 📖 `docs/resilience/polly-patterns.md` - Patterns
- 📋 `templates/shared/resilience/` - Setup templates
- 💡 See resilience examples in example projects

### API Endpoint Design
- 📖 `docs/api-styles/rest-guide.md` - Action-based
- 📖 `docs/api-styles/restful-guide.md` - Resource-based
- 📖 `docs/api-styles/graphql-guide.md` - Query language

### Code Review
- ✅ `checklists/architecture-audit.md` - Architecture compliance
- ✅ `checklists/resilience-audit.md` - Resilience patterns
- ✅ `checklists/performance-audit.md` - Performance checks

---

## Using with Claude Code

### In Your C# Projects

Tell Claude:
```
"Generate a controller following templates/rest-api/ for these endpoints..."
"Create tests using /templates/shared/tests/xunit-test.template.cs"
"Review against /checklists/architecture-audit.md"
"Show me the Dapper repository pattern from templates/shared/repositories/"
```

### Integration

1. Add plugin path to Claude Code settings:
   ```json
   {
     "customTools": {
       "csharpApiGuide": {
         "path": "/path/to/claude-csharp-api-plugin"
       }
     }
   }
   ```

2. Reference files in conversations with Claude
3. Ask Claude to verify against checklists
4. Use templates for code generation

See `.claude/settings-integration.md` for full integration guide.

---

## Architecture at a Glance

### The 4 Layers (Onion Architecture)

```
                   Presentation
                   Controllers
                      ↓
                 Application
              Services, DTOs
                      ↓
               Infrastructure
            Repositories, DbContext
                      ↓
                    Domain
              Entities, Value Objects
```

**Direction of Dependencies**: Always point inward toward Domain.

### SOLID Principles Applied

✅ **S** - Single Responsibility: Each class has one reason to change
✅ **O** - Open/Closed: Open for extension, closed for modification
✅ **L** - Liskov: Substitutable implementations
✅ **I** - Interface Segregation: Focused, small interfaces
✅ **D** - Dependency Inversion: Depend on abstractions, not concrete classes

### TDD Workflow

1. **Write test** (RED - test fails)
2. **Write code** (GREEN - test passes)
3. **Refactor** (REFACTOR - improve while keeping test green)
4. **Repeat**

---

## Next Steps

### Immediate (Right Now)

1. ✅ Read `docs/decision-tree.md`
2. ✅ Study the matching example from `/examples/`
3. ✅ Skim the architecture documentation

### This Week

1. Create new C# project using appropriate template
2. Implement Domain layer (entities with behavior)
3. Build Application layer (services, DTOs)
4. Add Infrastructure layer (repositories)
5. Expose with Presentation layer (controllers)
6. Write tests (xUnit, TDD)

### Ongoing

1. Reference checklists before pushing code
2. Add new examples as you solve problems
3. Update templates based on lessons learned
4. Keep plugin in Git for cross-machine use

---

## Tips for Success

### ✅ Do:
- Start with decision-tree.md
- Study examples before coding
- Follow the Onion Architecture strictly
- Write tests first (TDD)
- Use checklists for code review
- Reference templates as you build
- Ask Claude about patterns

### ❌ Don't:
- Skip the architecture layers
- Put business logic in repositories
- Access repositories directly from controllers
- Reference DbContext in domain layer
- Skip tests until "later"
- Copy blindly without understanding
- Skip the decision-tree questionnaire

---

## Files to Bookmark

Keep these handy while developing:

1. `docs/decision-tree.md` - Questions answered here
2. Your matching example in `/examples/`
3. `docs/architecture/onion-architecture.md` - Layer reference
4. `templates/shared/` - Code to copy
5. `checklists/architecture-audit.md` - Pre-commit check

---

## Troubleshooting

**Q: Where do I start with a new project?**
A: Open `docs/decision-tree.md` and follow the questionnaire.

**Q: What's the difference between REST and RESTful?**
A: REST = action-based URLs, RESTful = resource-based with proper HTTP verbs.
   See `docs/api-styles/` for detailed comparison.

**Q: Should I use Dapper or Entity Framework?**
A: See `docs/decision-tree.md` Step 4 with comparison table.

**Q: How do I structure my repository?**
A: Copy `/templates/shared/repositories/` and follow the example pattern.

**Q: What about Polly resilience patterns?**
A: See `docs/resilience/polly-patterns.md` with complete examples and setup.

**Q: How do I test my service?**
A: Copy `/templates/shared/tests/xunit-test.template.cs` and follow AAA pattern.

---

## You're Ready!

The plugin is complete with:
- ✅ Documentation (1000+ lines of guidance)
- ✅ Templates (ready to copy)
- ✅ Examples (9 complete projects)
- ✅ Checklists (verify your work)
- ✅ Integration (works with Claude Code)

**Start here**: Open `docs/decision-tree.md` →

---

**Version**: 1.0.0  
**Status**: Ready for use  
**Last Updated**: June 2026
