# Project Documentation Templates

Quick-start templates for documenting your C# API project.

## Files Included

### 1. ARCHITECTURE.md
**Purpose**: Document your project's architecture and design decisions  
**Use when**: Starting new project or major architectural changes  
**Who reads**: Developers, architects, new team members  
**Sections**:
- Technology stack with rationale
- Onion Architecture layer explanations
- API style (REST/RESTful/GraphQL)
- Data persistence & ORM choice
- Resilience & caching strategies
- Error handling approach
- Testing strategy overview
- Logging & monitoring setup
- Deployment architecture
- Key design decisions

**Time to customize**: 30-45 minutes

### 2. DEVELOPER_ONBOARDING.md
**Purpose**: Get new developers up and running quickly  
**Use when**: New team members join  
**Who reads**: New developers, contractors, interns  
**Sections**:
- Prerequisites & system requirements
- Step-by-step initial setup (30 minutes)
- Project structure walkthrough
- How to run the API
- First development task example
- Common commands reference
- Code style & standards
- Testing guidelines
- Debugging tips
- Troubleshooting common issues

**Time to customize**: 20-30 minutes

### 3. PULL_REQUEST_TEMPLATE.md
**Purpose**: Standardize PR submissions and code review process  
**Use when**: First PR or creating GitHub workflow  
**Who reads**: All developers submitting code  
**Sections**:
- PR description template
- Type of change checklist
- Testing checklist
- Architecture review checklist
- SOLID principles verification
- Security considerations
- Performance impact
- Database changes
- Breaking changes
- Documentation updates
- Dependencies
- Deployment notes
- Reviewer guidelines

**Time to customize**: 10-15 minutes

---

## How to Use These Templates

### Quick Start (5 minutes)

1. **Copy** files to your project `/documents` folder:
   ```bash
   mkdir -p documents
   cp *.md documents/
   ```

2. **Customize** each template:
   - Replace `[Project Name]` with your API name
   - Replace `[Details]` with your specific choices
   - Remove sections not applicable to your project

3. **Setup PR template** in GitHub:
   ```bash
   # Copy for GitHub integration
   cp PULL_REQUEST_TEMPLATE.md .github/PULL_REQUEST_TEMPLATE.md
   ```

### Detailed Setup

#### ARCHITECTURE.md

```bash
# Edit the file
nano documents/ARCHITECTURE.md

# Replace these placeholders:
# [Your API Name] - Your actual API name
# [REST/RESTful/GraphQL] - Your choice
# [Dapper/EF/LiteDB] - Your ORM
# [Reason] - Your specific reasons
```

**Key sections to customize**:
- Technology Stack table
- Layer responsibilities (match your actual structure)
- API Style section (choose one)
- Database Schema (create ER diagram)
- Caching Strategy (match Step 8 decision)
- Key Design Decisions table

#### DEVELOPER_ONBOARDING.md

```bash
# Edit the file
nano documents/DEVELOPER_ONBOARDING.md

# Key customizations:
# - Prerequisites: Your specific OS/tool requirements
# - Database: Your database type (SQL Server/PostgreSQL/SQLite)
# - Connection string: Your actual connection format
# - Port: Your default port (5000/8080/etc)
# - Project names: Your actual project names
# - Commands: Adjust for your project structure
```

**Key sections to customize**:
- Prerequisites (add your specific tools)
- Database setup instructions
- Connection string examples
- Project structure (show your actual layout)
- First Development Task (use your domain model)

#### PULL_REQUEST_TEMPLATE.md

```bash
# Copy to .github folder for GitHub integration
mkdir -p .github
cp PULL_REQUEST_TEMPLATE.md .github/PULL_REQUEST_TEMPLATE.md

# Edit the file
nano .github/PULL_REQUEST_TEMPLATE.md
```

**GitHub setup**:
1. Push to `main` branch
2. Go to GitHub repo → Settings → Pull Requests
3. Check "Template" checkbox
4. PR template appears automatically for all new PRs

---

## Integration with Decision Tree

These templates are **Step 14** in the decision tree workflow:

```
Step 1:  Project Type
Step 2:  API Style
...
Step 13: Swagger & Health Checks
Step 14: Documentation Setup ← You are here
Step 15: Your Complete Path
```

**Decision**: Do you want to create documentation?

- **Option A**: Skip (production-only projects)
- **Option B**: Create `/documents` folder ← This guide
  - Recommended for team/open-source projects
  - Requires 1 hour to customize all 3 templates
  - Significant time savings for onboarding

---

## File Checklist

Before committing documentation:

### ARCHITECTURE.md
- [ ] Project name filled in
- [ ] All technology decisions explained
- [ ] API style section matches your choice (REST/RESTful/GraphQL)
- [ ] ORM choice explained (Dapper/EF/LiteDB)
- [ ] Database schema section completed
- [ ] Caching strategy matches your Step 8 decision
- [ ] Design decisions table completed
- [ ] Deployment architecture diagram (if needed)
- [ ] References updated to match your tech

### DEVELOPER_ONBOARDING.md
- [ ] Prerequisites updated (SDK version, OS requirements)
- [ ] Database instructions match your setup
- [ ] Connection string examples are correct
- [ ] Port numbers match your defaults
- [ ] Project structure shows your actual layout
- [ ] Project names match your real projects
- [ ] First Development Task uses your domain model
- [ ] Commands tested and working
- [ ] Troubleshooting section updated
- [ ] Contact email updated

### PULL_REQUEST_TEMPLATE.md (if using GitHub)
- [ ] Copied to `.github/PULL_REQUEST_TEMPLATE.md`
- [ ] Architecture review checklist matches your patterns
- [ ] Database changes section relevant (or remove)
- [ ] Breaking changes section relevant (or remove)
- [ ] Deployment notes section relevant (or remove)
- [ ] Reviewer guidelines updated
- [ ] Pushed to `main` branch

---

## Maintenance

### Keep Documentation Fresh

Review and update these documents:
- **Monthly**: Add new common issues to troubleshooting
- **After major changes**: Update architecture decisions
- **After dependencies upgrade**: Update prerequisites
- **When onboarding new person**: Note what they struggled with

### Feedback Loop

After onboarding:
1. Ask new developer what was unclear
2. Update DEVELOPER_ONBOARDING.md
3. Add troubleshooting section for issues
4. Share updates with team

---

## Examples

### Before Customization
```markdown
# Architecture Documentation
**Project Name**: [Your API Name]
```

### After Customization
```markdown
# Architecture Documentation
**Project Name**: E-Commerce Order Service
```

### Before Customization
```markdown
- **ORM** | [Dapper/EF/LiteDB] | [Reason for choice]
```

### After Customization
```markdown
- **ORM** | Dapper | Performance critical queries, existing stored procedures, tight control over SQL
```

---

## Folder Structure After Setup

```
project-root/
├── .github/
│   └── PULL_REQUEST_TEMPLATE.md         ← For GitHub
├── documents/
│   ├── README.md                        ← This file
│   ├── ARCHITECTURE.md                  ← Design decisions
│   ├── DEVELOPER_ONBOARDING.md          ← Getting started
│   ├── PULL_REQUEST_TEMPLATE.md         ← PR guidelines
│   ├── API_ENDPOINTS.md                 ← (Optional) API reference
│   ├── DATABASE_SCHEMA.md               ← (Optional) Data model
│   └── TROUBLESHOOTING.md               ← (Optional) Common issues
├── src/
├── tests/
└── README.md
```

---

## Common Customizations

### For Microservice
- ARCHITECTURE.md: Emphasize service independence
- DEVELOPER_ONBOARDING.md: Add inter-service communication
- PULL_REQUEST_TEMPLATE.md: Add "Contract Breaking Changes" section

### For Full API
- ARCHITECTURE.md: Detail all domain entities
- DEVELOPER_ONBOARDING.md: More complex setup steps
- PULL_REQUEST_TEMPLATE.md: Expand database changes section

### For Team Project
- ARCHITECTURE.md: Include team members and roles
- DEVELOPER_ONBOARDING.md: Add team communication channels
- PULL_REQUEST_TEMPLATE.md: Add code owner requirements

### For Open Source
- ARCHITECTURE.md: Very detailed, external-facing
- DEVELOPER_ONBOARDING.md: Step-by-step for non-team devs
- PULL_REQUEST_TEMPLATE.md: Contribute guidelines

---

## Tips for Great Documentation

### ARCHITECTURE.md
- ✅ **Explain the WHY**: Why Dapper not EF? Why Redis not memory cache?
- ✅ **Use diagrams**: ASCII diagrams for layers and flow
- ✅ **Rationale table**: Show decisions and reasons side-by-side
- ✅ **Link resources**: Reference external docs and guides
- ❌ **Don't** just list technologies without context

### DEVELOPER_ONBOARDING.md
- ✅ **Step-by-step**: Number each step clearly
- ✅ **Expected output**: Show what success looks like
- ✅ **Example code**: Provide actual code snippets
- ✅ **Troubleshooting**: Add issues as people discover them
- ❌ **Don't** assume developers know your setup

### PULL_REQUEST_TEMPLATE.md
- ✅ **Checklists**: Use checkboxes for easy verification
- ✅ **Clear categories**: Organize by type of review
- ✅ **Examples**: Show good vs. bad PR descriptions
- ✅ **Educational**: Help developers improve their PRs
- ❌ **Don't** make it too long (keep < 500 lines)

---

## Updating Your Docs

### Monthly Review
```bash
# Schedule reminder
# Review and update:
# 1. DEVELOPER_ONBOARDING.md
#    - Add any new troubleshooting issues
#    - Update prerequisites if versions changed
# 2. ARCHITECTURE.md
#    - Note any new technology decisions
# 3. PULL_REQUEST_TEMPLATE.md
#    - Update based on recent PRs (what worked well?)
```

### When Something Breaks
- Add to TROUBLESHOOTING section
- Include solution steps
- Include how to prevent next time

### After Major Change
- Update ARCHITECTURE.md
- Update relevant DEVELOPER_ONBOARDING.md sections
- Add migration notes if needed

---

## Next Steps

1. **Copy** these templates to `/documents`
2. **Customize** for your project (1-2 hours)
3. **Review** with team (30 minutes)
4. **Commit** to main branch
5. **Link** in main README.md
6. **Use** for next onboarding!

---

**Questions?** See [../decision-tree.md#step-14](../decision-tree.md#step-14-project-documentation-setup)
