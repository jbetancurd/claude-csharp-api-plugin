# Pull Request

## Description

<!-- Provide a clear and concise description of what this PR does -->
<!-- Example: "Add user authentication endpoint with JWT token validation" -->

**What problem does this solve?**
<!-- Briefly explain the motivation and context -->

**What is the solution?**
<!-- Describe your implementation approach -->

---

## Type of Change

<!-- Check the relevant option -->

- [ ] 🐛 **Bug Fix** - Fixes an existing issue
- [ ] ✨ **Feature** - Adds new functionality
- [ ] 🔄 **Refactoring** - Code restructuring without behavior change
- [ ] 📚 **Documentation** - Documentation updates
- [ ] 🎨 **Style** - Code style/formatting changes
- [ ] ⚡ **Performance** - Performance improvement
- [ ] 🔒 **Security** - Security-related changes

---

## Related Issues

<!-- Link to related GitHub issues -->
<!-- Example: Closes #123, Related to #456 -->

Closes #[issue-number]

---

## Changes Made

<!-- Provide detailed list of changes -->

### Domain Layer Changes
- [ ] Added/Modified entities
- [ ] Added/Modified value objects
- [ ] Added/Modified domain services

### Application Layer Changes
- [ ] Added/Modified application services
- [ ] Added/Modified DTOs
- [ ] Added/Modified specifications
- [ ] Updated mappings

### Infrastructure Layer Changes
- [ ] Added/Modified repositories
- [ ] Added database migrations
- [ ] Added/Modified external service implementations

### Presentation Layer Changes
- [ ] Added/Modified controllers
- [ ] Added/Modified middleware
- [ ] Added/Modified filters
- [ ] Updated configuration

### Tests
- [ ] Added unit tests (target 70% coverage)
- [ ] Added integration tests
- [ ] Updated existing tests

---

## Testing

### How to Test This Change?

<!-- Provide step-by-step testing instructions -->

1. [Step 1]
2. [Step 2]
3. [Step 3]

### Test Coverage

- [ ] Unit tests added/updated
- [ ] Integration tests added/updated
- [ ] Coverage maintained above 70%
- [ ] All existing tests still pass

### Manual Testing (if applicable)

- [ ] Tested locally
- [ ] Tested with Swagger UI
- [ ] Tested edge cases
- [ ] Tested error scenarios

---

## Testing Checklist

- [ ] `dotnet build` passes with no warnings
- [ ] `dotnet test` passes (all tests)
- [ ] Code coverage >= 70% for new code
- [ ] No hardcoded values or console.writelines in production code
- [ ] No TODO comments without context

---

## Architecture Review Checklist

### Onion Architecture Compliance
- [ ] No circular dependencies between layers
- [ ] Presentation only depends on Application
- [ ] Application only depends on Domain and Interfaces
- [ ] Infrastructure implements Application interfaces
- [ ] Domain has no framework dependencies

### SOLID Principles
- [ ] Single Responsibility - Each class has one reason to change
- [ ] Open/Closed - Open for extension, closed for modification
- [ ] Liskov Substitution - Subclasses are substitutable for base classes
- [ ] Interface Segregation - Clients depend on small, focused interfaces
- [ ] Dependency Inversion - Depend on abstractions, not concretions

### Code Quality
- [ ] No code duplication (DRY principle)
- [ ] Meaningful variable and method names
- [ ] Methods are focused and not too long
- [ ] Error handling is appropriate
- [ ] No magic numbers (use constants)

### Async/Await
- [ ] All I/O operations are async
- [ ] No `.Result` or `.Wait()` blocking calls
- [ ] Async propagates through call stack
- [ ] ConfigureAwait used where appropriate

### Database
- [ ] No N+1 queries
- [ ] Queries are optimized (indexed, selective)
- [ ] Migration is provided (if schema changed)
- [ ] Relationships are properly configured (lazy loading)

### Logging
- [ ] Structured logging used (not string concatenation)
- [ ] Appropriate log levels (Debug/Info/Warning/Error)
- [ ] Sensitive data is not logged (passwords, tokens)
- [ ] Context information included (IDs, correlation IDs)

---

## Performance Impact

<!-- Describe any performance implications -->

- [ ] No breaking changes
- [ ] Performance neutral
- [ ] Performance improvement (describe)
- [ ] Performance regression (justified by other benefits)

**Benchmark** (if performance-critical):
```
Before: [metric]
After:  [metric]
Improvement: X%
```

---

## Security Considerations

<!-- Describe security implications and mitigations -->

- [ ] No hardcoded secrets or credentials
- [ ] Input validation on all user inputs
- [ ] SQL injection protection (parameterized queries)
- [ ] Authorization checks in place
- [ ] HTTPS enforced where needed
- [ ] No information disclosure in error messages
- [ ] Sensitive data not logged

---

## Database Changes

<!-- If schema changed, describe migrations -->

- [ ] No database changes
- [ ] Schema migration provided
- [ ] Backward compatible migration
- [ ] Data migration script (if needed)

**Migration Name**: [e.g., AddUserTable]

```sql
-- Migration SQL (if applicable)
```

---

## Breaking Changes

<!-- List any breaking changes -->

- [ ] No breaking changes
- [ ] Breaking changes (justify):
  - [ ] API endpoint changed
  - [ ] DTO structure changed
  - [ ] Database schema changed
  - [ ] Dependency versions changed

**Migration Path** (if breaking):
- [ ] Deprecation warning provided
- [ ] Transition period (how long?)
- [ ] Documentation updated

---

## Documentation

### Code Documentation
- [ ] XML comments on public methods
- [ ] Complex logic documented
- [ ] Architecture decisions explained

### External Documentation
- [ ] README updated (if needed)
- [ ] API documentation updated
- [ ] Architecture documentation updated (if architectural change)
- [ ] Deployment documentation updated (if deployment change)

---

## Dependencies

### New Dependencies
- [ ] No new dependencies added
- [ ] New dependencies added (list below):
  - [ ] Package: [Name], Version: [X.Y.Z], Reason: [Why]
  - [ ] Package: [Name], Version: [X.Y.Z], Reason: [Why]

### Version Updates
- [ ] No version updates
- [ ] Dependencies updated (list below):
  - [ ] Package: [Name], [Old Version] → [New Version]

---

## Deployment Notes

<!-- Any special deployment instructions? -->

- [ ] No special deployment required
- [ ] Requires migration: [details]
- [ ] Requires configuration changes: [details]
- [ ] Requires service restart: [details]
- [ ] Requires approval from: [who]

---

## Screenshots / Demo

<!-- If applicable, add screenshots or demo links -->

### Before
[Screenshot or description]

### After
[Screenshot or description]

---

## Reviewers' Checklist

<!-- For reviewers to verify before approving -->

- [ ] Code follows project standards
- [ ] Architecture is sound (Onion, SOLID)
- [ ] Tests are comprehensive
- [ ] Documentation is clear
- [ ] Performance is acceptable
- [ ] Security is considered
- [ ] No merge conflicts
- [ ] CI/CD pipeline passes

---

## How to Review This PR

1. **Read** the description to understand the change
2. **Check** the files changed (look for surprises)
3. **Review** architecture (layers, dependencies)
4. **Verify** test coverage (70% minimum)
5. **Test** locally if significant changes
6. **Comment** on any concerns

---

## Size

This PR is **[small/medium/large]**

- Small: < 200 lines, single concern
- Medium: 200-500 lines, related changes
- Large: > 500 lines (consider splitting)

---

## Feedback

<!-- Any feedback or concerns from your side? -->

---

---

## Checklist Summary

Before marking as ready for review:

- [ ] All tests pass locally
- [ ] No warnings in build output
- [ ] Code follows style guidelines
- [ ] Architecture preserved (Onion, SOLID)
- [ ] Documentation is complete
- [ ] No hardcoded values
- [ ] No console.writeline in production code
- [ ] Coverage >= 70% for new code
- [ ] Description is clear and complete
- [ ] Related issues are linked

---

## Note for Reviewers

<!-- Anything special reviewers should know? -->

---

**PR Type**: [Feature/Bug/Refactor/Docs]  
**Complexity**: [Low/Medium/High]  
**Risk**: [Low/Medium/High]

---

<sub>Thank you for reviewing! Questions? 💬 Comment below</sub>
