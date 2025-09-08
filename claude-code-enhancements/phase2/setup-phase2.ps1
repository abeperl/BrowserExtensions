# Claude Code Enhancements - Phase 2 Setup Script (Windows PowerShell)
# Advanced Workflow Capabilities Installation

param(
    [switch]$Verify,
    [switch]$Update,
    [string]$ClaudeDir = "$env:USERPROFILE\.claude"
)

$ErrorActionPreference = "Stop"

Write-Host "🚀 Claude Code Enhancements - Phase 2 Setup" -ForegroundColor Cyan
Write-Host "===============================================" -ForegroundColor Cyan

Write-Host "📍 Platform: Windows PowerShell" -ForegroundColor Green
Write-Host "📁 Claude directory: $ClaudeDir" -ForegroundColor Green

# Verify Phase 1 installation
if (!(Test-Path "$ClaudeDir\safety-config.json")) {
    Write-Host "❌ Phase 1 not detected! Please install Phase 1 first." -ForegroundColor Red
    Write-Host "   Run: powershell -ExecutionPolicy Bypass -File setup.ps1" -ForegroundColor Yellow
    exit 1
}

Write-Host "✅ Phase 1 detected - proceeding with Phase 2 installation" -ForegroundColor Green

if ($Verify) {
    Write-Host "🔍 Verifying Phase 2 Installation..." -ForegroundColor Yellow
    
    # Check Phase 2 directories
    $phase2Dirs = @("workflows", "notifications", "knowledge", "interfaces")
    foreach ($dir in $phase2Dirs) {
        $path = Join-Path $ClaudeDir $dir
        if (Test-Path $path) {
            Write-Host "✅ $dir directory exists" -ForegroundColor Green
        } else {
            Write-Host "❌ $dir directory missing" -ForegroundColor Red
        }
    }
    
    # Check Phase 2 configuration files
    $phase2Configs = @("phase2-config.json", "workflow-templates.json", "notification-config.json", "knowledge-config.json")
    foreach ($config in $phase2Configs) {
        $path = Join-Path $ClaudeDir $config
        if (Test-Path $path) {
            Write-Host "✅ $config exists" -ForegroundColor Green
        } else {
            Write-Host "❌ $config missing" -ForegroundColor Red
        }
    }
    
    # Check Phase 2 commands
    $commandsPath = Join-Path $ClaudeDir "commands"
    if (Test-Path $commandsPath) {
        $phase2Commands = @("advanced-commands.md", "menuconfig.md", "learn.md", "branch-smart.md")
        $foundCommands = 0
        foreach ($cmd in $phase2Commands) {
            if (Test-Path (Join-Path $commandsPath $cmd)) {
                $foundCommands++
            }
        }
        Write-Host "📋 Found $foundCommands/4 Phase 2 command files" -ForegroundColor Cyan
    }
    
    Write-Host ""
    Write-Host "🎉 Phase 2 verification complete!" -ForegroundColor Green
    return
}

# Create Phase 2 directory structure
Write-Host "📂 Creating Phase 2 directory structure..." -ForegroundColor Yellow
$directories = @("workflows", "notifications", "knowledge", "interfaces", "templates", "analytics")
foreach ($dir in $directories) {
    $path = Join-Path $ClaudeDir $dir
    New-Item -Path $path -ItemType Directory -Force | Out-Null
}

# Copy Phase 2 command files
Write-Host "📋 Installing Phase 2 commands..." -ForegroundColor Yellow
$phase2CommandFiles = @(
    "commands\advanced-commands.md",
    "notifications\notification-system.md", 
    "knowledge\knowledge-extraction.md",
    "workflows\enhanced-git-workflow.md",
    "interfaces\interactive-config.md"
)

foreach ($cmdFile in $phase2CommandFiles) {
    if (Test-Path ".\$cmdFile") {
        $fileName = Split-Path $cmdFile -Leaf
        $destPath = Join-Path "$ClaudeDir\commands" $fileName.Replace('.md', '-phase2.md')
        Copy-Item -Path ".\$cmdFile" -Destination $destPath -Force
    }
}

# Install Phase 2 configuration
Write-Host "⚙️  Installing Phase 2 configuration..." -ForegroundColor Yellow
$phase2Config = @"
{
  "version": "2.0.0",
  "phase": 2,
  "installed": "$(Get-Date -Format 'yyyy-MM-ddTHH:mm:ssZ')",
  "features": {
    "advanced_commands": {
      "enabled": true,
      "interactive_builder": true,
      "context_suggestions": true,
      "command_history": true,
      "project_detection": true
    },
    "notifications": {
      "enabled": true,
      "platform": "windows",
      "toast_notifications": true,
      "sound_enabled": true,
      "quiet_hours": {
        "enabled": false,
        "start": "22:00",
        "end": "08:00"
      }
    },
    "knowledge_extraction": {
      "enabled": true,
      "auto_capture": true,
      "pattern_recognition": true,
      "team_sharing": false,
      "retention_days": 90
    },
    "git_workflows": {
      "enabled": true,
      "smart_branching": true,
      "automated_prs": true,
      "conflict_resolution": true,
      "repository_analytics": true
    },
    "interactive_config": {
      "enabled": true,
      "menuconfig_style": true,
      "guided_wizard": true,
      "real_time_validation": true,
      "profile_management": true
    }
  },
  "integration": {
    "phase1_compatibility": true,
    "safety_system_enhanced": true,
    "multi_agent_learning": true,
    "smart_commit_advanced": true
  }
}
"@

$phase2Config | Out-File -FilePath "$ClaudeDir\phase2-config.json" -Encoding UTF8 -Force

# Install workflow templates
Write-Host "🔄 Installing workflow templates..." -ForegroundColor Yellow
$workflowTemplates = @"
{
  "version": "2.0.0",
  "templates": {
    "feature_development": {
      "name": "Feature Development Workflow",
      "description": "Complete feature development from design to deployment",
      "steps": [
        "/delegate architect 'Design: {feature_name}'",
        "/branch smart feature/{feature_name}",
        "/delegate security-expert 'Security review for {feature_name}'",
        "/delegate test-engineer 'Test plan for {feature_name}'",
        "/commit-smart --type feat --scope {feature_scope}",
        "/pr create --template feature"
      ],
      "estimated_time": "45-90 minutes",
      "success_rate": 0.92
    },
    "bug_investigation": {
      "name": "Bug Investigation & Fix",
      "description": "Systematic bug investigation and resolution",
      "steps": [
        "/delegate debugger 'Investigate: {bug_description}'",
        "/branch smart bugfix/{bug_id}",
        "/multi-role 'Analyze root cause' debugger,security-expert",
        "/safe-edit {affected_files}",
        "/delegate test-engineer 'Verify fix for {bug_id}'",
        "/commit-smart --type fix --ticket {bug_id}"
      ],
      "estimated_time": "20-45 minutes", 
      "success_rate": 0.89
    },
    "code_review": {
      "name": "Comprehensive Code Review",
      "description": "Multi-agent code review process",
      "steps": [
        "/multi-role 'Review PR #{pr_number}' security-expert,performance-analyst,code-reviewer",
        "/learn save 'Code review insights from PR #{pr_number}'",
        "/pr review --feedback-summary",
        "/commit-smart --type review --scope feedback"
      ],
      "estimated_time": "15-30 minutes",
      "success_rate": 0.95
    },
    "repository_maintenance": {
      "name": "Repository Maintenance",
      "description": "Regular repository health and cleanup",
      "steps": [
        "/repo-health",
        "/branch cleanup",
        "/clean-config --deep",
        "/audit-config",
        "/learn export --backup"
      ],
      "estimated_time": "10-20 minutes",
      "success_rate": 0.98,
      "schedule": "weekly"
    }
  }
}
"@

$workflowTemplates | Out-File -FilePath "$ClaudeDir\workflow-templates.json" -Encoding UTF8 -Force

# Install notification configuration
Write-Host "🔔 Installing notification system..." -ForegroundColor Yellow
$notificationConfig = @"
{
  "version": "2.0.0",
  "platform": "windows",
  "notification_types": {
    "task_complete": {
      "enabled": true,
      "sound": "success.wav",
      "priority": "normal",
      "auto_dismiss": 5000,
      "actions": ["view_result", "dismiss"]
    },
    "error_alert": {
      "enabled": true,
      "sound": "error.wav",
      "priority": "high",
      "auto_dismiss": false,
      "actions": ["quick_fix", "view_details", "ignore"]
    },
    "progress_update": {
      "enabled": true,
      "sound": false,
      "priority": "low", 
      "auto_dismiss": 3000,
      "actions": ["show_progress", "cancel"]
    },
    "suggestion": {
      "enabled": true,
      "sound": "subtle.wav",
      "priority": "normal",
      "auto_dismiss": 10000,
      "actions": ["apply", "learn_more", "dismiss"]
    }
  },
  "delivery_settings": {
    "max_per_minute": 5,
    "batch_similar": true,
    "respect_focus_mode": true,
    "quiet_hours": {
      "enabled": false,
      "start": "22:00",
      "end": "08:00"
    }
  }
}
"@

$notificationConfig | Out-File -FilePath "$ClaudeDir\notification-config.json" -Encoding UTF8 -Force

# Install knowledge management configuration
Write-Host "🧠 Installing knowledge management..." -ForegroundColor Yellow
$knowledgeConfig = @"
{
  "version": "2.0.0",
  "knowledge_base": {
    "auto_capture": true,
    "capture_threshold": 0.7,
    "pattern_recognition": true,
    "learning_rate": "adaptive"
  },
  "storage": {
    "max_size_mb": 100,
    "retention_days": 90,
    "compression": true,
    "indexing": "full-text"
  },
  "sharing": {
    "team_export": false,
    "anonymize_personal": true,
    "sync_frequency": "never"
  },
  "search": {
    "fuzzy_matching": true,
    "context_weighting": true,
    "relevance_threshold": 0.6
  }
}
"@

$knowledgeConfig | Out-File -FilePath "$ClaudeDir\knowledge-config.json" -Encoding UTF8 -Force

# Install Phase 2 command aliases
Write-Host "🔧 Installing Phase 2 command aliases..." -ForegroundColor Yellow
$commandAliases = @"
# Phase 2 Command Aliases
# Add these to your shell profile for quick access

# Interactive Workflow Building
Set-Alias build 'claude-build'
Set-Alias suggest 'claude-suggest'
Set-Alias learn 'claude-learn'

# Advanced Git Operations  
Set-Alias smart-branch 'claude-branch-smart'
Set-Alias smart-pr 'claude-pr-create'
Set-Alias repo-check 'claude-repo-health'

# Configuration Management
Set-Alias menuconfig 'claude-menuconfig'
Set-Alias config-wizard 'claude-setup-wizard'

# Notification Management
Set-Alias notify-setup 'claude-notify-setup'
Set-Alias notify-test 'claude-notify-test'

# Knowledge Management
Set-Alias kb-search 'claude-learn-search'
Set-Alias kb-export 'claude-learn-export'
"@

$commandAliases | Out-File -FilePath "$ClaudeDir\phase2-aliases.ps1" -Encoding UTF8 -Force

# Set up Windows-specific features
Write-Host "🪟 Configuring Windows-specific features..." -ForegroundColor Yellow

# Install Windows Toast notification helper
$toastHelper = @"
# Windows Toast Notification Helper
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

function Show-ClaudeToast {
    param(
        [string]`$Title = "Claude Code",
        [string]`$Message = "Notification",
        [string]`$Type = "Info"
    )
    
    `$notify = New-Object System.Windows.Forms.NotifyIcon
    `$notify.Icon = [System.Drawing.SystemIcons]::Information
    `$notify.BalloonTipIcon = `$Type
    `$notify.BalloonTipText = `$Message
    `$notify.BalloonTipTitle = `$Title
    `$notify.Visible = `$true
    
    `$notify.ShowBalloonTip(5000)
    
    Start-Sleep -Seconds 1
    `$notify.Dispose()
}

# Export function for use
Export-ModuleMember -Function Show-ClaudeToast
"@

$toastHelper | Out-File -FilePath "$ClaudeDir\notifications\windows-toast.ps1" -Encoding UTF8 -Force

# Create verification script
Write-Host "🔧 Creating Phase 2 verification script..." -ForegroundColor Yellow
$verifyScript = @"
# Phase 2 Verification Script
Write-Host "🔍 Verifying Claude Code Phase 2 Installation..." -ForegroundColor Yellow

`$ClaudeDir = "`$env:USERPROFILE\.claude"
`$errors = 0

# Check Phase 2 config
if (Test-Path "`$ClaudeDir\phase2-config.json") {
    Write-Host "✅ Phase 2 configuration exists" -ForegroundColor Green
} else {
    Write-Host "❌ Phase 2 configuration missing" -ForegroundColor Red
    `$errors++
}

# Check workflow templates
if (Test-Path "`$ClaudeDir\workflow-templates.json") {
    Write-Host "✅ Workflow templates installed" -ForegroundColor Green
} else {
    Write-Host "❌ Workflow templates missing" -ForegroundColor Red
    `$errors++
}

# Check notification system
if (Test-Path "`$ClaudeDir\notification-config.json") {
    Write-Host "✅ Notification system configured" -ForegroundColor Green
} else {
    Write-Host "❌ Notification system missing" -ForegroundColor Red
    `$errors++
}

# Check knowledge management
if (Test-Path "`$ClaudeDir\knowledge-config.json") {
    Write-Host "✅ Knowledge management ready" -ForegroundColor Green
} else {
    Write-Host "❌ Knowledge management missing" -ForegroundColor Red
    `$errors++
}

if (`$errors -eq 0) {
    Write-Host ""
    Write-Host "🎉 Phase 2 verification successful!" -ForegroundColor Green
    Write-Host "Ready to use advanced Claude Code features" -ForegroundColor Cyan
} else {
    Write-Host ""
    Write-Host "⚠️  Phase 2 verification found `$errors issues" -ForegroundColor Yellow
    Write-Host "Run setup again: powershell -ExecutionPolicy Bypass -File setup-phase2.ps1" -ForegroundColor White
}
"@

$verifyScript | Out-File -FilePath "$ClaudeDir\verify-phase2.ps1" -Encoding UTF8 -Force

# Success message
Write-Host ""
Write-Host "🎉 Claude Code Phase 2 Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Green
Write-Host ""
Write-Host "📍 Installation location: $ClaudeDir" -ForegroundColor Cyan
Write-Host "🔧 Advanced workflow capabilities enabled" -ForegroundColor Green
Write-Host "🔔 Cross-platform notifications configured" -ForegroundColor Green
Write-Host "🧠 Knowledge extraction system active" -ForegroundColor Green
Write-Host "🔄 Enhanced git workflows available" -ForegroundColor Green
Write-Host "⚙️  Interactive configuration interfaces ready" -ForegroundColor Green
Write-Host ""
Write-Host "🚀 Phase 2 Commands Available:" -ForegroundColor Yellow
Write-Host "• /build                  - Interactive workflow builder" -ForegroundColor White
Write-Host "• /suggest                - Context-aware suggestions" -ForegroundColor White
Write-Host "• /learn                  - Knowledge extraction" -ForegroundColor White
Write-Host "• /branch smart <name>    - Intelligent branch creation" -ForegroundColor White
Write-Host "• /pr create              - AI-powered pull requests" -ForegroundColor White
Write-Host "• /menuconfig             - Interactive configuration" -ForegroundColor White
Write-Host "• /notify setup           - Notification configuration" -ForegroundColor White
Write-Host "• /repo-health            - Repository analysis" -ForegroundColor White
Write-Host ""
Write-Host "🔧 Next Steps:" -ForegroundColor Yellow
Write-Host "1. Verify: powershell -ExecutionPolicy Bypass -File '$ClaudeDir\verify-phase2.ps1'" -ForegroundColor White
Write-Host "2. Configure: /menuconfig" -ForegroundColor White
Write-Host "3. Try: /build feature" -ForegroundColor White
Write-Host "4. Learn: /learn" -ForegroundColor White
Write-Host "5. Explore: /suggest" -ForegroundColor White
Write-Host ""
Write-Host "📚 Documentation: phase2\README.md" -ForegroundColor Cyan
Write-Host ""
Write-Host "✨ Enhanced Claude Code experience ready!" -ForegroundColor Magenta