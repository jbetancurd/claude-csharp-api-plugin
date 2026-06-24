# Claude C# API Plugin Setup Guide

This guide explains how to properly install and use this plugin across your C# projects.

---

## Problem: Why Changes Weren't Updating Across Projects

You have **two plugin locations**:
1. **Source**: `/Volumes/Datos/Code/personal/github/claude-csharp-api-plugin/` ✅ (Master copy)
2. **Local copies**: Scattered across projects ❌ (Disconnected)

**Result**: Changes in source never sync to copies in other projects.

---

## Solution: Single Source of Truth

### Option A: Git-Based (Recommended for Teams)

**1. Keep only the SOURCE copy** (`/Volumes/Datos/Code/personal/github/claude-csharp-api-plugin/`)

**2. When starting a NEW project**, reference it via git submodule:

```bash
cd /path/to/your-new-project
git submodule add https://github.com/jbetancurd/claude-csharp-api-plugin.git .claude-plugins/csharp-api

# Then in Claude Code:
# - Open decision tree: `.claude-plugins/csharp-api/docs/decision-tree.md`
# - Reference guides automatically load
```

**3. For existing projects**, create a symlink:

```bash
cd /path/to/existing-project
ln -s /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin/.claude-plugins/csharp-api

# Claude Code will now find the plugin via the link
```

**Benefits**:
- ✅ Single source of truth (master repo)
- ✅ All projects auto-update when you commit changes
- ✅ Easy to manage versions
- ✅ Professional distribution

---

### Option B: Local Installation (Single Machine Only)

**For development on one machine only**:

**1. Add to Claude Code's plugin path** (`~/.claude/plugins/`):

```bash
# Create symlink from Claude's plugin directory
mkdir -p ~/.claude/plugins
ln -s /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin \
      ~/.claude/plugins/claude-csharp-api-plugin

# OR copy it
cp -r /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin \
      ~/.claude/plugins/claude-csharp-api-plugin
```

**2. Claude Code discovers it automatically**

**3. Works across ALL your projects** on this machine

**Benefits**:
- ✅ Works on all projects automatically
- ✅ Simple for solo development
- ✅ No per-project setup needed

**Drawbacks**:
- ❌ Only works on your machine
- ❌ Harder to share with team

---

### Option C: Clean up and Prevent Duplicates

**If you accidentally created copies:**

```bash
# Find all copies
find /Volumes/Datos/Code -name "claude-csharp-api-plugin" -type d

# Delete all except the source
rm -rf /Volumes/Datos/Code/ClaudePlugins/claude-csharp-api-plugin
# Keep /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin

# Now use Option A or B above
```

---

## How Claude Code Uses This Plugin

### When Starting a C# API Project

```
User: "Help me start a C# API project"
  ↓
Claude: "Which C# version are you targeting?" 
  [Loads: docs/decision-tree.md - Step 0]
  ↓
User: "C# 12"
  ↓
Claude: "Let's determine your architecture..."
  [Loads: docs/decision-tree.md - Step 1+]
  [References: docs/csharp-versions/csharp-version-features.md]
  [Uses: templates/ for code generation]
  [Shows: examples/ for patterns]
```

### Plugin Auto-Discovery

Claude Code looks for plugins in this order:

1. **Project-level** (current project `.claude-plugins/` or `.claude/plugins/`)
2. **Global** (`~/.claude/plugins/`)
3. **Installed plugins** (via `/claude-code plugin install`)

---

## Current Setup Status

### What We Just Updated

✅ **CLAUDE.md** - Added C# Coding Conventions + Version Features section  
✅ **docs/decision-tree.md** - Added Step 0: C# Version Selection  
✅ **docs/csharp-versions/** - Created comprehensive version feature guides  
✅ **.claude-plugin/plugin.json** - Updated metadata with new features  

### Version in Repository

**Current**: 1.1.0 (includes C# version-aware guidance)

```json
{
  "version": "1.1.0",
  "features": [
    "C# version-aware code generation",
    "Microsoft C# Coding Conventions",
    "Decision tree with C# version selection",
    ...
  ]
}
```

---

## Setup Instructions by Scenario

### Scenario 1: Start Using Right Now (Simplest)

```bash
# Install to Claude Code's global plugin directory
mkdir -p ~/.claude/plugins
cp -r /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin \
      ~/.claude/plugins/

# Done! Use in any project now
```

Then in Claude Code on any C# project:
```
"Help me design a C# API" → Claude will reference the plugin automatically
```

---

### Scenario 2: Use with Git Submodules (Team Projects)

In your new project:

```bash
cd /my-team-project
git submodule add https://github.com/jbetancurd/claude-csharp-api-plugin.git \
  .claude-plugins/csharp-api
git commit -m "Add C# API plugin as submodule"
```

In Claude Code, reference from `.claude-plugins/csharp-api/` when needed.

---

### Scenario 3: Development & Testing (Current State)

Since you're actively developing the plugin:

```bash
# Work in the source
cd /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin

# Make changes (you just did!)
# Commit to git
git add .
git commit -m "Add C# version selection and coding conventions"
git push

# Changes auto-sync to all projects that reference it via:
# - git submodule (auto-update)
# - symlink (immediate)
# - ~/.claude/plugins (if you copy with `cp -r` occasionally)
```

---

## Preventing Future Duplicate Copies

### Add to your `.gitignore`:

```
# Claude plugin copies (use symlinks or submodules instead)
.claude-plugins/*/
!.claude-plugins/.gitkeep
```

### In your shell profile (`~/.zshrc` or `~/.bash_profile`):

```bash
# Alias to verify plugin source of truth
alias plugin-verify='echo "Source: /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin" && \
  find /Volumes/Datos/Code -name "claude-csharp-api-plugin" -type d'
```

---

## Troubleshooting

### Plugin not showing up in Claude Code

**Check 1**: Is it in the right location?
```bash
ls ~/.claude/plugins/claude-csharp-api-plugin/
```

**Check 2**: Is `.claude-plugin/plugin.json` valid?
```bash
cat ~/.claude/plugins/claude-csharp-api-plugin/.claude-plugin/plugin.json
```

**Check 3**: Restart Claude Code and try again

### Changes not reflecting in projects

**If using symlinks**:
```bash
# Symlinks should auto-reflect changes
cd /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin
# Make changes → immediately visible in linked projects
```

**If using git submodules**:
```bash
cd /my-project
git submodule update --remote  # Pull latest plugin version
```

**If using copies**:
```bash
# You need to manually copy again
rm -rf ~/.claude/plugins/claude-csharp-api-plugin
cp -r /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin \
      ~/.claude/plugins/
```

---

## Summary

| Approach | Setup Time | Auto-Update | Team Friendly | Recommendation |
|----------|-----------|-------------|---------------|----------------|
| **Global symlink** | 1 min | ✅ Yes | ❌ Local only | 👍 **Best for solo dev** |
| **Git submodule** | 2 min | ✅ Yes | ✅ Yes | 👍 **Best for teams** |
| **Copy to ~/.claude/plugins** | 30 sec | ❌ Manual | ❌ Local only | For testing |

**Recommended for you**: Use global symlink (Option B) or git submodule (Option A) for team sharing.

---

## Quick Setup (Copy & Paste)

```bash
# Option B: Global symlink (works everywhere on your machine)
mkdir -p ~/.claude/plugins
ln -s /Volumes/Datos/Code/personal/github/claude-csharp-api-plugin \
      ~/.claude/plugins/claude-csharp-api-plugin
echo "✅ Plugin installed! Restart Claude Code."

# Verify
ls -la ~/.claude/plugins/claude-csharp-api-plugin/.claude-plugin/plugin.json
```

---

**Now your plugin will be available globally and automatically pick up changes!** 🎉
