# Safe Delete Command

## Purpose
Preview file/directory deletion before execution with comprehensive safety checks.

## Usage
```bash
/safe-delete <path> [--force] [--backup]
```

## Options
- `--force`: Skip some safety checks (still shows preview)
- `--backup`: Create backup before deletion
- `--recursive`: Allow directory deletion (with explicit confirmation)

## Behavior

### Analysis Phase
1. **File/Directory Analysis**
   - Size and type information
   - Last modified date
   - Git status (tracked, untracked, modified)
   - Dependencies (imported by other files)

2. **Risk Assessment**
   - Critical files (package.json, .env, etc.)
   - Large directories
   - Files with uncommitted changes
   - Recently modified files

### Preview Output
```
DELETION PREVIEW:
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📁 Target: src/components/old-component/
   📊 Size: 45KB (12 files)
   📅 Last modified: 2 days ago
   🔄 Git status: 3 modified, 1 untracked
   ⚠️  Risk: MEDIUM (contains uncommitted changes)

📋 Contents to be deleted:
├── index.js (2KB, modified)
├── component.jsx (15KB, modified) 
├── styles.css (8KB, clean)
├── __tests__/ (20KB, 8 files)

🔗 Dependencies found:
- Imported by: src/pages/dashboard.js (line 15)
- Referenced in: src/routes.js (line 42)

⚠️  WARNINGS:
- Contains uncommitted Git changes
- Referenced by other files (potential breaking changes)

💡 SUGGESTIONS:
- Commit changes first: git add . && git commit -m "Save before deletion"
- Update references in dependent files
- Consider using --backup to create safety copy

━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
Execute deletion? Use: /execute-confirmed
Cancel operation? Use: /cancel
Create backup first? Use: /safe-delete <path> --backup
```

## Safety Features

- **Git Integration**: Checks for uncommitted changes
- **Dependency Analysis**: Finds files that import/reference the target
- **Size Warnings**: Alerts for large deletions
- **Backup Options**: Easy backup creation before deletion
- **Undo Information**: Provides recovery instructions

This command ensures users never accidentally delete important files or directories.