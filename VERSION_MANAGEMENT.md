# Plugin Version Management & Distribution

This guide explains how to manage versions, update the plugin, and distribute it across your projects.

---

## Current Version

**Version**: 1.1.0  
**Released**: 2026-06-24  
**Status**: ✅ Active with C# version-aware guidance

### What's in 1.1.0

- ✅ C# version selection (Step 0 in decision tree)
- ✅ Microsoft C# Coding Conventions guidance
- ✅ C# 12, 11, 10, 9, 8 feature guides
- ✅ Updated marketplace metadata
- ✅ Plugin setup instructions
- ✅ Version management documentation

---

## Files That Track Versions

### 1. **`.claude-plugin/plugin.json`** (Primary)
Claude Code reads this for plugin discovery:
```json
{
  "version": "1.1.0",
  "released": "2026-06-24",
  "changelog": { ... }
}
```

### 2. **`.claude-plugin/marketplace.json`** (Distribution)
Controls how plugin appears in marketplaces:
```json
{
  "id": "csharp-api-plugin-v1.1",
  "metadata": { "version": "1.1.0" },
  "plugins": [{ "version": "1.1.0", "released": "2026-06-24" }]
}
```

### 3. **`plugin-manifest.json`** (Reference)
High-level plugin information:
```json
{
  "version": "1.0.0"  // Update when needed
}
```

### 4. **`CHANGELOG.md`** (If Created)
Detailed change history for users

---

## Version Numbering Scheme

**Format**: `MAJOR.MINOR.PATCH`

- **MAJOR** (1→2): Breaking changes, complete rewrites
  - Example: New architecture approach, incompatible with old projects
  
- **MINOR** (1→2): New features, non-breaking
  - Example: Added C# version guidance, new templates, new checklist
  
- **PATCH** (1→2): Bug fixes, documentation fixes
  - Example: Fixed typo, corrected code example

### Current Examples

- **1.0.0** → **1.1.0**: MINOR bump (added C# version guidance)
- **1.1.0** → **1.1.1**: Would be PATCH (fix in docs)
- **1.1.0** → **2.0.0**: Would be MAJOR (new architecture approach)

---

## How to Update the Plugin

### Step 1: Make Changes
```bash
cd /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin

# Edit files as needed
# Example: Update CLAUDE.md with new guidance
nano CLAUDE.md
```

### Step 2: Update Version Files

**For MINOR update** (new features):

```bash
# Update .claude-plugin/plugin.json
{
  "version": "1.2.0",  # Changed from 1.1.0
  "released": "2026-06-25",  # Today's date
  "changelog": {
    "1.2.0": [
      "Added WebSocket best practices",
      "Added gRPC communication guide",
      "..."
    ],
    # Keep previous versions
    "1.1.0": [ ... ],
    "1.0.0": [ ... ]
  }
}

# Update .claude-plugin/marketplace.json
{
  "id": "csharp-api-plugin-v1.2",  # Increment version in ID
  "metadata": { "version": "1.2.0" },
  "plugins": [{
    "version": "1.2.0",
    "released": "2026-06-25"
  }]
}

# Update plugin-manifest.json (optional)
{ "version": "1.2.0" }
```

**For PATCH update** (bug fixes):

```bash
# Same as above, but:
{
  "version": "1.1.1",  # Changed only PATCH number
  "released": "2026-06-25"
}
```

### Step 3: Commit to Git

```bash
git add .
git commit -m "v1.2.0: Add WebSocket and gRPC guidance

- Added WebSocket best practices guide
- Added gRPC communication patterns
- Updated decision tree with communication options"

git tag v1.2.0
git push origin main --tags
```

### Step 4: Notify Users (if distributed)

If sharing with team, notify them:
```bash
# Push changes
git push

# Tag for version
git tag v1.2.0
git push --tags

# Announce in team channel
"New plugin version 1.2.0 released with WebSocket guidance!"
```

---

## Managing Updates Across Projects

### Scenario 1: Using Git Submodules

```bash
# In your project
git submodule update --remote

# Gets latest plugin version automatically
# Commit the submodule update
git add .gitmodules
git commit -m "Update C# plugin to v1.2.0"
```

### Scenario 2: Using Symlinks

**Automatic!** Symlinks always point to latest:
```bash
ls -la ~/.claude/plugins/claude-csharp-api-plugin

# If it's a symlink, changes are immediate
lrwxr-xr-x  1 user  admin  ... -> /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin
```

### Scenario 3: Manual Copies

```bash
# When updating, recopy
cp -r /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin \
      ~/.claude/plugins/

# Or per-project
cp -r /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin \
      /my-project/.claude-plugins/csharp-api
```

---

## Publishing to Plugin Marketplace

### If You Want to Share Publicly

1. **Ensure plugin is in public GitHub repo**
   ```bash
   # Repo: https://github.com/jbetancurd/claude-csharp-api-plugin
   git push origin main
   git push --tags  # Push version tags
   ```

2. **Update `.claude-plugin/marketplace.json`**
   - Ensure `repository.url` is public
   - Ensure `homepage` is accessible
   - Ensure `documentation` URL exists

3. **Submit to Claude Code marketplace**
   - Marketplace submission process (varies by Claude Code version)
   - Usually: `claude code plugin submit`

4. **Track in stats**
   ```json
   "stats": {
     "downloads": 0,
     "rating": 0,
     "reviews": 0,
     "active": true,
     "lastUpdate": "2026-06-25"
   }
   ```

---

## Semantic Versioning Quick Reference

| Change | From | To | Type |
|--------|------|-----|------|
| Bug fix | 1.1.0 | 1.1.1 | PATCH |
| New feature | 1.1.0 | 1.2.0 | MINOR |
| Breaking change | 1.1.0 | 2.0.0 | MAJOR |
| New major feature | 1.1.0 | 1.2.0 | MINOR |
| Multiple features | 1.1.0 | 1.3.0 | MINOR |

---

## Changelog Best Practices

### Good Changelog Entry

```markdown
"1.2.0": {
  "date": "2026-06-25",
  "changes": [
    "Added WebSocket best practices section",
    "Added gRPC communication guide with examples",
    "Enhanced decision tree with communication options",
    "Updated C# 12 features for latest .NET 9 patterns",
    "Fixed typo in Polly configuration example"
  ]
}
```

### What to Include

✅ **DO**
- Clear, user-facing descriptions
- What changed and why
- Links to new docs/guides
- Breaking changes clearly marked

❌ **DON'T**
- Commit hashes
- "Fixed bug" without detail
- Too technical details
- Internal refactoring notes

---

## Deployment Checklist

Before releasing a new version:

- [ ] All changes tested locally
- [ ] Version number updated in all config files:
  - [ ] `.claude-plugin/plugin.json`
  - [ ] `.claude-plugin/marketplace.json`
  - [ ] `plugin-manifest.json`
- [ ] Changelog entry added
- [ ] `released` date set to today
- [ ] All relative links work (`../docs/...`)
- [ ] README reflects new version
- [ ] Git tags created (`git tag v1.2.0`)
- [ ] Pushed to GitHub

---

## Quick Commands

```bash
# View current version
grep '"version"' .claude-plugin/plugin.json

# Create a new version commit
git add . && git commit -m "v1.2.0: Add new features" && git tag v1.2.0

# Push with tags
git push origin main --tags

# Check version files match
echo "plugin.json:" && grep '"version"' .claude-plugin/plugin.json
echo "marketplace.json:" && grep '"version"' .claude-plugin/marketplace.json
echo "manifest.json:" && grep '"version"' plugin-manifest.json
```

---

## Current Version Status

```
Version: 1.1.0
Released: 2026-06-24
Status: ✅ Active
Location: /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin
Distribution: Symlink at ~/.claude/plugins/claude-csharp-api-plugin

Next Update: TBD (when adding new features)
```

---

## Need to Update?

Follow this flow:

1. **Make changes** to docs/templates/code
2. **Decide version type** (MAJOR/MINOR/PATCH)
3. **Update version files** (all 3 config files)
4. **Add changelog entry**
5. **Commit & tag** in git
6. **Push** to GitHub
7. **Announce** if distributing to team

That's it! Your symlinks and submodules will pick it up automatically.
