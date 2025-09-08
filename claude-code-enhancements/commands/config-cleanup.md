# Configuration Cleanup Tools

## Purpose
Automated cleanup of Claude Code configurations, removing bloat, sensitive data, and optimizing for performance and security.

## Commands

### Basic Cleanup
```bash
/clean-config                   # Standard cleanup with preview
/clean-config --execute        # Execute cleanup immediately  
/clean-config --deep           # Aggressive cleanup mode
/audit-config                  # Security and efficiency audit
```

### Targeted Cleanup
```bash
/clean-secrets                 # Remove sensitive data only
/clean-bloat                   # Remove large/unnecessary files
/clean-cache                   # Clear temporary and cache files
/clean-history                 # Optimize conversation history
```

## Cleanup Categories

### 1. Sensitive Data Removal 🔒

#### Detection Patterns
```regex
# API Keys and Tokens
/api[_-]?key.*[=:]\s*["']?[a-zA-Z0-9]{20,}/i
/token.*[=:]\s*["']?[a-zA-Z0-9_\-\.]{20,}/i

# Database Credentials  
/password.*[=:]\s*["']?[^\s"']+/i
/db[_-]?pass.*[=:]\s*["']?[^\s"']+/i

# Cloud Credentials
/aws[_-]?(access|secret).*[=:]\s*["']?[A-Z0-9]{20}/i
/gcp[_-]?key.*[=:]\s*["']?[^\s"']+/i
```

### 2. Performance Optimization ⚡

#### Large File Detection
```
🔍 SCANNING FOR LARGE FILES...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 Size Analysis:
├── 🗂️  Total config size: 15.2 MB → Target: <2 MB
├── 📁 Largest directories:
│   ├── .claude/cache/: 8.4 MB (can be cleared)
│   ├── .claude/logs/: 3.1 MB (archive old logs) 
│   └── .claude/backups/: 2.8 MB (remove old backups)

🎯 OPTIMIZATION OPPORTUNITIES:
├── ✂️  Remove files older than 30 days
├── 🗜️  Compress conversation history  
├── 🧹 Clear temporary cache files
└── 📦 Archive old project contexts
```

### 3. Security Audit 🛡️

#### Vulnerability Checks
```
🔒 SECURITY AUDIT REPORT
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚠️  HIGH RISK:
├── 🔑 API keys found in plain text (3 instances)
├── 🗄️  Database passwords in configuration
└── 🌐 URLs with embedded credentials

⚠️  MEDIUM RISK: 
├── 📝 Verbose logging enabled (may leak data)
├── 🔗 External API calls without encryption
└── 📁 World-readable configuration files

🛠️  RECOMMENDED ACTIONS:
├── Move secrets to environment variables
├── Enable configuration file encryption
├── Reduce logging verbosity in production
└── Review and rotate exposed credentials
```

## Interactive Cleanup Mode

```
🧹 INTERACTIVE CONFIGURATION CLEANUP
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Found 27 optimization opportunities:

1. 🔑 Sensitive Data (5 items)
   [y] Mask API keys in config files
   [y] Remove hardcoded passwords  
   [n] Clear authentication tokens (will require re-login)

2. 📦 Large Files (8 items)  
   [y] Clear cache directory (8.4 MB)
   [y] Compress old conversation history
   [?] Archive project contexts older than 90 days

Apply selected optimizations? [Y/n]
Estimated space savings: 11.3 MB → 1.8 MB (84% reduction)
```

## Cleanup Results

### Before/After Comparison
```
📊 CLEANUP SUMMARY
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

BEFORE:
├── 📁 Total size: 15.2 MB
├── 🔒 Security issues: 8 high, 12 medium
├── 📄 Files: 1,247 (including 890 cache files)
└── ⚡ Performance: Slow (2.3s startup)

AFTER:
├── 📁 Total size: 1.8 MB (↓ 84%)  
├── 🔒 Security issues: 0 high, 2 medium (↓ 90%)
├── 📄 Files: 357 (↓ 71%)
└── ⚡ Performance: Fast (0.4s startup, ↑ 83%)

✅ IMPROVEMENTS:
├── 🔑 5 sensitive data patterns masked/removed
├── 🗑️  890 unnecessary files removed
├── 📦 13.4 MB space freed
├── 🛡️  Security score: C+ → A-
└── ⚡ Startup time improved by 83%
```

This comprehensive cleanup system ensures Claude Code configurations remain secure, efficient, and optimized while providing full transparency and rollback capabilities.