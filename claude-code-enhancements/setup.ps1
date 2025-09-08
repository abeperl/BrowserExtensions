# Claude Code Enhancements - Phase 1 Setup Script (Windows PowerShell)
# Installs safety defaults, multi-agent system, smart commits, and cleanup tools

param(
    [switch]$Verify,
    [string]$ClaudeDir = "$env:USERPROFILE\.claude"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Claude Code Enhancements - Phase 1 Setup" -ForegroundColor Cyan
Write-Host "=============================================" -ForegroundColor Cyan

Write-Host "📍 Detected OS: Windows" -ForegroundColor Green
Write-Host "📁 Claude directory: $ClaudeDir" -ForegroundColor Green

if ($Verify) {
    Write-Host "🔍 Verifying Claude Code Enhancement Setup..." -ForegroundColor Yellow
    
    # Check directories
    $dirs = @("commands", "agents", "backups", "cleanup-rules")
    foreach ($dir in $dirs) {
        $path = Join-Path $ClaudeDir $dir
        if (Test-Path $path) {
            Write-Host "✅ $dir directory exists" -ForegroundColor Green
        } else {
            Write-Host "❌ $dir directory missing" -ForegroundColor Red
        }
    }
    
    # Check configuration files
    $configs = @("safety-config.json", "agents\agent-config.json", "commit-config.json", "cleanup-config.json")
    foreach ($config in $configs) {
        $path = Join-Path $ClaudeDir $config
        if (Test-Path $path) {
            Write-Host "✅ $config exists" -ForegroundColor Green
        } else {
            Write-Host "❌ $config missing" -ForegroundColor Red
        }
    }
    
    # Check commands
    $commandsPath = Join-Path $ClaudeDir "commands"
    if (Test-Path $commandsPath) {
        $commandCount = (Get-ChildItem -Path $commandsPath -Filter "*.md" -ErrorAction SilentlyContinue).Count
        Write-Host "📋 Found $commandCount command files" -ForegroundColor Cyan
    } else {
        Write-Host "📋 Commands directory not found" -ForegroundColor Yellow
    }
    
    Write-Host ""
    Write-Host "🎉 Setup verification complete!" -ForegroundColor Green
    return
}

# Create necessary directories
Write-Host "📂 Creating directory structure..." -ForegroundColor Yellow
$directories = @("commands", "agents", "backups", "cleanup-rules")
foreach ($dir in $directories) {
    $path = Join-Path $ClaudeDir $dir
    New-Item -Path $path -ItemType Directory -Force | Out-Null
}

# Copy command files if they exist
Write-Host "📋 Installing commands..." -ForegroundColor Yellow
if (Test-Path ".\commands") {
    Copy-Item -Path ".\commands\*" -Destination "$ClaudeDir\commands\" -Recurse -Force
    Write-Host "✅ Commands installed" -ForegroundColor Green
} else {
    Write-Host "⚠️  Commands directory not found - run from claude-code-enhancements directory" -ForegroundColor Yellow
}

# Install safety system
Write-Host "🛡️  Installing safety system..." -ForegroundColor Yellow
if (Test-Path ".\safety-system.md") {
    Copy-Item -Path ".\safety-system.md" -Destination $ClaudeDir -Force
}

$safetyConfig = @"
{
  "version": "1.0.0",
  "safety_mode": "enabled",
  "dry_run_default": true,
  "destructive_operations": {
    "require_confirmation": ["delete", "overwrite", "move", "commit"],
    "create_backups": true,
    "show_previews": true
  },
  "risk_assessment": {
    "file_size_warning_mb": 10,
    "directory_size_warning_mb": 100,
    "git_uncommitted_warning": true
  }
}
"@

$safetyConfig | Out-File -FilePath "$ClaudeDir\safety-config.json" -Encoding UTF8 -Force

# Install multi-agent configuration
Write-Host "🤖 Installing multi-agent system..." -ForegroundColor Yellow
$agentConfig = @"
{
  "version": "1.0.0",
  "enabled_agents": [
    "architect",
    "frontend-dev", 
    "backend-dev",
    "security-expert",
    "performance-analyst",
    "test-engineer",
    "code-reviewer",
    "debugger"
  ],
  "agent_settings": {
    "max_context_size": 100000,
    "timeout_seconds": 300,
    "parallel_limit": 3
  },
  "workflows": {
    "full_feature": "architect→backend-dev→frontend-dev→test-engineer",
    "code_review": "security-expert,performance-analyst,code-reviewer",
    "bug_fix": "debugger→test-engineer",
    "refactor": "architect→refactor-specialist→code-reviewer"
  }
}
"@

$agentConfig | Out-File -FilePath "$ClaudeDir\agents\agent-config.json" -Encoding UTF8 -Force

# Install smart commit configuration
Write-Host "💬 Installing smart commit system..." -ForegroundColor Yellow
$commitConfig = @"
{
  "version": "1.0.0",
  "style": "conventional",
  "scopes": ["feat", "fix", "docs", "style", "refactor", "test", "chore"],
  "auto_detection": {
    "breaking_changes": true,
    "ticket_references": true,  
    "co_authors": true
  },
  "quality_checks": {
    "subject_max_length": 50,
    "body_line_length": 72,
    "require_description": false,
    "spell_check": false
  },
  "integrations": {
    "github_issues": true,
    "jira": false,
    "linear": false
  }
}
"@

$commitConfig | Out-File -FilePath "$ClaudeDir\commit-config.json" -Encoding UTF8 -Force

# Install cleanup configuration
Write-Host "🧹 Installing cleanup system..." -ForegroundColor Yellow
$cleanupConfig = @"
{
  "version": "1.0.0", 
  "schedule": "weekly",
  "max_config_size_mb": 5,
  "retention_days": 30,
  "cleanup_rules": {
    "sensitive_data": {
      "enabled": true,
      "action": "mask",
      "patterns": [
        "api[_-]?key.*[=:]\\\\s*[\"']?[a-zA-Z0-9]{20,}",
        "token.*[=:]\\\\s*[\"']?[a-zA-Z0-9_\\\\-\\\\.]{20,}",
        "password.*[=:]\\\\s*[\"']?[^\\\\s\"']+",
        "aws[_-]?(access|secret).*[=:]\\\\s*[\"']?[A-Z0-9]{20}"
      ]
    },
    "performance": {
      "enabled": true,
      "clear_cache": true,
      "compress_logs": true,
      "remove_temp_files": true
    },
    "security": {
      "enabled": true,
      "audit_permissions": true,
      "check_vulnerabilities": true,
      "rotate_backups": true
    }
  }
}
"@

$cleanupConfig | Out-File -FilePath "$ClaudeDir\cleanup-config.json" -Encoding UTF8 -Force

# Create sample CLAUDE.md enhancement
Write-Host "📄 Creating CLAUDE.md enhancement template..." -ForegroundColor Yellow
$claudeEnhancement = @"
# Claude Code Enhanced Workflow

## Phase 1 Enhancements Active

### Safety-First Commands
- `/safe-delete <path>` - Preview deletion before executing
- `/safe-edit <file>` - Show changes before applying  
- `/safe-commit` - Preview commit before executing
- `/execute-confirmed` - Execute last previewed operation

### Multi-Agent Delegation  
- `/delegate <agent> "<task>"` - Single specialized agent
- `/multi-role "<task>" agent1,agent2` - Multiple perspectives
- `/workflow agent1→agent2 "<task>"` - Sequential workflow

### Smart Git Operations
- `/commit-smart` - Intelligent commit generation
- `/commit-preview` - Show proposed commit
- `/commit-interactive` - Step-by-step commit builder

### Configuration Management
- `/clean-config` - Preview cleanup operations
- `/audit-config` - Security and efficiency audit
- `/clean-secrets` - Remove sensitive data only

## Available Agents
- `architect` - System design and architecture
- `security-expert` - Security analysis and recommendations
- `performance-analyst` - Performance optimization
- `code-reviewer` - Code quality and best practices
- `debugger` - Error diagnosis and resolution
- `test-engineer` - Testing strategies

## Example Workflows

### Feature Development
```bash
/delegate architect "Design user authentication system"
/workflow architect→backend-dev→frontend-dev→test-engineer "Implement authentication"
/commit-smart --type feat --scope auth
```

### Code Review Process  
```bash
/multi-role "Review this component for production readiness" security-expert,performance-analyst,code-reviewer
/commit-smart --type fix "Address review feedback"
```

### Maintenance Tasks
```bash
/audit-config  # Weekly security audit
/clean-config --preview  # Monthly cleanup
/delegate tech-debt-analyst "Prioritize technical debt"
```
"@

$claudeEnhancement | Out-File -FilePath "$ClaudeDir\CLAUDE-enhancements.md" -Encoding UTF8 -Force

# Set up git hooks if in git repository
if (Test-Path ".git") {
    Write-Host "🔗 Setting up git hooks..." -ForegroundColor Yellow
    
    $hooksDir = ".git\hooks"
    if (!(Test-Path $hooksDir)) {
        New-Item -Path $hooksDir -ItemType Directory -Force | Out-Null
    }
    
    # Pre-commit hook for safety checks
    $preCommitHook = @"
#!/bin/bash
# Claude Code Enhanced Pre-commit Hook

echo "🔍 Running Claude Code safety checks..."

# Check for sensitive data (Windows compatible)
if (Get-ChildItem -Recurse -Include *.js,*.json,*.env | Select-String -Pattern "api[_-]?key|password|token" -Quiet) {
    Write-Host "⚠️  Sensitive data detected! Run /clean-secrets first" -ForegroundColor Red
    exit 1
}

# Check configuration size
`$configPath = "`$env:USERPROFILE\.claude"
if (Test-Path `$configPath) {
    `$configSize = (Get-ChildItem -Recurse `$configPath | Measure-Object -Property Length -Sum).Sum / 1MB
    if (`$configSize -gt 10) {
        `$sizeRounded = [math]::Round(`$configSize, 1)
        Write-Host "⚠️  Configuration size large (`$sizeRounded MB). Consider running /clean-config" -ForegroundColor Yellow
    }
}

Write-Host "✅ Pre-commit checks passed" -ForegroundColor Green
"@
    
    $preCommitHook | Out-File -FilePath "$hooksDir\pre-commit" -Encoding UTF8 -Force
    Write-Host "✅ Git hooks installed" -ForegroundColor Green
}

# Create verification script
Write-Host "🔧 Finalizing setup..." -ForegroundColor Yellow
$verifyScript = @"
# Verify setup by running: powershell -ExecutionPolicy Bypass -File setup.ps1 -Verify
param([string]`$ClaudeDir = "`$env:USERPROFILE\.claude")

Write-Host "🔍 Verifying Claude Code Enhancement Setup..." -ForegroundColor Yellow
Write-Host ""

# Check directories
`$dirs = @("commands", "agents", "backups", "cleanup-rules")
foreach (`$dir in `$dirs) {
    `$path = Join-Path `$ClaudeDir `$dir
    if (Test-Path `$path) {
        Write-Host "✅ `$dir directory exists" -ForegroundColor Green
    } else {
        Write-Host "❌ `$dir directory missing" -ForegroundColor Red
    }
}

# Check configuration files
`$configs = @("safety-config.json", "agents\agent-config.json", "commit-config.json", "cleanup-config.json")
foreach (`$config in `$configs) {
    `$path = Join-Path `$ClaudeDir `$config
    if (Test-Path `$path) {
        Write-Host "✅ `$config exists" -ForegroundColor Green
    } else {
        Write-Host "❌ `$config missing" -ForegroundColor Red
    }
}

# Check commands
`$commandsPath = Join-Path `$ClaudeDir "commands"
if (Test-Path `$commandsPath) {
    `$commandCount = (Get-ChildItem -Path `$commandsPath -Filter "*.md" -ErrorAction SilentlyContinue).Count
    Write-Host "📋 Found `$commandCount command files" -ForegroundColor Cyan
} else {
    Write-Host "📋 Commands directory not found" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "🎉 Setup verification complete!" -ForegroundColor Green
"@

$verifyScript | Out-File -FilePath "$ClaudeDir\verify-setup.ps1" -Encoding UTF8 -Force

# Success message
Write-Host ""
Write-Host "🎉 Claude Code Enhancements - Phase 1 Setup Complete!" -ForegroundColor Green
Write-Host "==================================================" -ForegroundColor Green
Write-Host ""
Write-Host "📍 Installation location: $ClaudeDir" -ForegroundColor Cyan
Write-Host "🔧 Configuration files created" -ForegroundColor Green
Write-Host "📋 Command system installed" -ForegroundColor Green
Write-Host "🤖 Multi-agent system ready" -ForegroundColor Green
Write-Host "💬 Smart commits enabled" -ForegroundColor Green
Write-Host "🧹 Cleanup tools configured" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Run: powershell -ExecutionPolicy Bypass -File '$ClaudeDir\verify-setup.ps1'" -ForegroundColor White
Write-Host "2. Try: /safe-delete --help" -ForegroundColor White
Write-Host "3. Try: /delegate architect `"help me get started`"" -ForegroundColor White
Write-Host "4. Try: /commit-smart (in a git repository)" -ForegroundColor White
Write-Host "5. Try: /clean-config --preview" -ForegroundColor White
Write-Host ""
Write-Host "📚 Documentation: $ClaudeDir\CLAUDE-enhancements.md" -ForegroundColor Cyan
Write-Host ""
Write-Host "✨ Happy coding with enhanced Claude Code!" -ForegroundColor Magenta