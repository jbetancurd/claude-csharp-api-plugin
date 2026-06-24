# C# Version Strategy for Claude C# API Plugin

This directory contains comprehensive guidance on how to leverage C# version-specific features in API development.

---

## Files in This Directory

### 1. **csharp-version-features.md** (Main Feature Guide)
The definitive reference for all C# version features and how to use them.

**Covers:**
- Feature highlights for C# 12, 11, 10, 9, and 8
- Before/after code examples
- Performance considerations
- Migration paths between versions
- Best practices per version

**Use when**: You need to understand what features are available in each version

**Key Sections:**
- Primary Constructors (C# 12)
- Collection Expressions (C# 12)
- Records (C# 10+)
- Required Members (C# 11+)
- File-Scoped Types (C# 11+)
- Init-Only Properties (C# 10+)
- Pattern Matching (All modern versions)

---

### 2. **version-selection-guide.md** (Claude Reference)
Quick reference guide for Claude Code to understand how to ask about versions and apply them.

**Covers:**
- How to ask about C# version
- How to apply version knowledge when generating code
- How to review code for version appropriateness
- Version feature matrix
- Examples by version
- Upgrade recommendations

**Use when**: Implementing the version-aware behavior in Claude

**Key Sections:**
- Question to ask first
- How to apply version knowledge
- Feature matrix for quick lookup
- Examples for common patterns
- Upgrade recommendations

---

### 3. **README.md** (This File)
Overview of the C# version strategy and how all pieces fit together.

---

## How This Works in Practice

### User Flow

```
1. User asks Claude: "Help me start a C# API"
   ↓
2. Claude asks: "Which C# version are you targeting?"
   - C# 12 (Latest)
   - C# 11 (LTS)
   - C# 10
   - C# 9
   - C# 8 or earlier
   ↓
3. User selects C# version (e.g., "C# 12")
   ↓
4. Claude refers to version-selection-guide.md
   ↓
5. All subsequent code generation uses C# 12 patterns:
   - Primary constructors for services
   - Collection expressions for lists
   - Records for DTOs
   - Modern async patterns
   ↓
6. When reviewing code:
   - Checks it uses C# 12 idioms
   - Suggests improvements using C# 12 features
   - References csharp-version-features.md for guidance
```

---

## Key Integration Points

### Decision Tree (Step 0)
**File**: `../decision-tree.md` (Step 0)

The decision tree now starts with C# version selection before asking about architecture:
- Explains each version
- Links to feature guides
- Sets context for all subsequent recommendations

### CLAUDE.md Documentation
**File**: `../../CLAUDE.md`

Updated to include:
- C# Coding Conventions (with Microsoft link)
- C# Version-Specific Features section
- How Claude applies version knowledge
- Integration points for version-aware behavior

### Code Generation Templates
All templates should be updated to support multiple versions with version-specific code:

**Example for Services**:
```csharp
// C# 12
public class UserService(IUserRepository repository, ILogger<UserService> logger)
{
    public async Task<User> GetAsync(int id) => await repository.GetByIdAsync(id);
}

// C# 11
public class UserService
{
    private readonly IUserRepository _repository;
    private readonly ILogger<UserService> _logger;
    
    public UserService(IUserRepository repository, ILogger<UserService> logger)
    {
        _repository = repository;
        _logger = logger;
    }
    
    public async Task<User> GetAsync(int id) => await _repository.GetByIdAsync(id);
}
```

---

## Features by Version Reference

**Quick lookup table** for which version each feature was introduced:

| Feature | Introduced | Recommended |
|---------|------------|------------|
| **Primary Constructors** | C# 12 | Latest projects |
| **Collection Expressions** | C# 12 | C# 12+ only |
| **Inline Arrays** | C# 12 | High-performance APIs |
| **Required Members** | C# 11 | DTO/API contract safety |
| **File-Scoped Types** | C# 11 | Encapsulation/organization |
| **Raw String Literals** | C# 11 | SQL/JSON/XML in code |
| **Records** | C# 10 | DTOs, immutable data |
| **Init-Only Properties** | C# 10 | Controlled immutability |
| **File-Scoped Namespaces** | C# 10 | Cleaner file structure |
| **Pattern Matching** | C# 7-12 | Validation, conditional logic |
| **Nullable Reference Types** | C# 8 | All modern versions |

---

## Claude Behavior by Version

When Claude Code knows the user's C# version, it will:

### For C# 12
✅ Use primary constructors in all services  
✅ Use collection expressions for lists/arrays  
✅ Use inline arrays for performance-critical code  
✅ Recommend modern async patterns  
✅ Leverage latest compiler optimizations  

### For C# 11
✅ Use required members in DTOs and entities  
✅ Use file-scoped types for internal helpers  
✅ Use raw string literals for SQL/JSON  
✅ Recommend LTS stability benefits  
✅ Suggest C# 12 upgrade path when beneficial  

### For C# 10
✅ Use records for immutable data structures  
✅ Use init-only properties for controlled mutations  
✅ Use file-scoped namespaces for organization  
✅ Recommend C# 11 for required members  

### For C# 9
✅ Use records (basic support)  
✅ Use init properties  
✅ Use pattern matching (basic)  
✅ Recommend upgrade to C# 10+  

### For C# 8 or earlier
⚠️ Limited modern features  
⚠️ Recommend strong upgrade path to C# 11 (LTS) or C# 12  

---

## Implementation Checklist

- [ ] Claude asks about C# version at project start (Step 0 of decision tree)
- [ ] Store user's chosen version in conversation context
- [ ] Reference version-selection-guide.md when generating code
- [ ] Use csharp-version-features.md when explaining features
- [ ] Apply version-appropriate patterns in templates
- [ ] Suggest version-specific optimizations in code review
- [ ] Reference csharp-version-history when recommending upgrades
- [ ] Link to Microsoft docs for additional context

---

## References

- **Microsoft C# What's New**: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/
- **C# Version History**: https://learn.microsoft.com/en-us/dotnet/csharp/whats-new/csharp-version-history
- **C# Coding Conventions**: https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions
- **Plugin Decision Tree**: `../decision-tree.md` (Step 0)
- **Plugin Documentation**: `../../CLAUDE.md`

---

## Version Support Matrix

| Version | Release | .NET Support | Status | EoL |
|---------|---------|--------------|--------|-----|
| **C# 12** | Nov 2023 | .NET 9 | ✅ Current | TBD |
| **C# 11** | Nov 2022 | .NET 8 | ✅ LTS | Nov 2026 |
| **C# 10** | Nov 2021 | .NET 6 | ✅ LTS | Nov 2024 |
| **C# 9** | Nov 2020 | .NET 5 | ⚠️ Aging | Nov 2022 |
| **C# 8** | Sep 2019 | .NET Core 3 | ⚠️ Old | Support varies |

---

## Getting Started

1. **Read first**: `csharp-version-features.md` to understand available features
2. **Reference while coding**: `version-selection-guide.md` for quick patterns
3. **In decision tree**: See `../decision-tree.md` Step 0 for user-facing version selection
4. **In CLAUDE.md**: See section on "C# Version-Specific Features"

---

## Next Steps

When implementing version-aware code generation:

1. Update all code templates to include version-specific variants
2. Add version checks to code generation logic
3. Create example projects showcasing each C# version
4. Build validation that generated code matches target version
5. Add migration helpers for upgrading between versions
