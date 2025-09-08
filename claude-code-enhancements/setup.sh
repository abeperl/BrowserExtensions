#!/bin/bash

# Claude Code Enhancements - Phase 1 Setup Script
# Installs safety defaults, multi-agent system, smart commits, and cleanup tools

set -e  # Exit on error

echo "🚀 Claude Code Enhancements - Phase 1 Setup"
echo "============================================="

# Detect operating system
OS="$(uname -s)"
case "${OS}" in
    Linux*)     MACHINE=Linux;;
    Darwin*)    MACHINE=Mac;;
    CYGWIN*)    MACHINE=Cygwin;;
    MINGW*)     MACHINE=MinGw;;
    *)          MACHINE="UNKNOWN:${OS}"
esac

echo "📍 Detected OS: ${MACHINE}"

# Set Claude directory based on OS
if [[ "$MACHINE" == "Mac" || "$MACHINE" == "Linux" ]]; then
    CLAUDE_DIR="$HOME/.claude"
elif [[ "$MACHINE" == "Cygwin" || "$MACHINE" == "MinGw" ]]; then
    CLAUDE_DIR="$USERPROFILE/.claude"
else
    echo "❌ Unsupported operating system: $MACHINE"
    exit 1
fi

echo "📁 Claude directory: $CLAUDE_DIR"

# Create necessary directories
echo "📂 Creating directory structure..."
mkdir -p "$CLAUDE_DIR/commands"
mkdir -p "$CLAUDE_DIR/agents" 
mkdir -p "$CLAUDE_DIR/backups"
mkdir -p "$CLAUDE_DIR/cleanup-rules"

# Copy command files
echo "📋 Installing commands..."
if [ -d "./commands" ]; then
    cp -r ./commands/* "$CLAUDE_DIR/commands/"
    echo "✅ Commands installed"
else
    echo "⚠️  Commands directory not found - run from claude-code-enhancements directory"
fi

# Install safety system
echo "🛡️  Installing safety system..."
cp safety-system.md "$CLAUDE_DIR/"
cat > "$CLAUDE_DIR/safety-config.json" << 'EOF'
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
EOF

# Install multi-agent configuration  
echo "🤖 Installing multi-agent system..."
cat > "$CLAUDE_DIR/agents/agent-config.json" << 'EOF'
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
EOF

# Install smart commit configuration
echo "💬 Installing smart commit system..."  
cat > "$CLAUDE_DIR/commit-config.json" << 'EOF'
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
EOF

# Install cleanup configuration
echo "🧹 Installing cleanup system..."
cat > "$CLAUDE_DIR/cleanup-config.json" << 'EOF'
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
        "api[_-]?key.*[=:]\\s*[\"']?[a-zA-Z0-9]{20,}",
        "token.*[=:]\\s*[\"']?[a-zA-Z0-9_\\-\\.]{20,}",
        "password.*[=:]\\s*[\"']?[^\\s\"']+",
        "aws[_-]?(access|secret).*[=:]\\s*[\"']?[A-Z0-9]{20}"
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
EOF

# Create sample CLAUDE.md enhancement
echo "📄 Creating CLAUDE.md enhancement template..."
cat > "$CLAUDE_DIR/CLAUDE-enhancements.md" << 'EOF'
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
EOF

# Set up git hooks if in git repository
if [ -d ".git" ]; then
    echo "🔗 Setting up git hooks..."
    mkdir -p .git/hooks
    
    # Pre-commit hook for safety checks
    cat > .git/hooks/pre-commit << 'EOF'
#!/bin/bash
# Claude Code Enhanced Pre-commit Hook

echo "🔍 Running Claude Code safety checks..."

# Check for sensitive data
if grep -r "api[_-]\?key\|password\|token" --include="*.js" --include="*.json" --include="*.env" .; then
    echo "⚠️  Sensitive data detected! Run /clean-secrets first"
    exit 1
fi

# Check configuration size
config_size=$(du -sh ~/.claude 2>/dev/null | cut -f1 | sed 's/M.*//' | sed 's/K.*/0.1/' || echo "0")
if (( $(echo "$config_size > 10" | bc -l 2>/dev/null || echo "0") )); then
    echo "⚠️  Configuration size large (${config_size}MB). Consider running /clean-config"
fi

echo "✅ Pre-commit checks passed"
EOF
    
    chmod +x .git/hooks/pre-commit
    echo "✅ Git hooks installed"
fi

# Final setup and verification
echo "🔧 Finalizing setup..."

# Create verification script
cat > "$CLAUDE_DIR/verify-setup.sh" << 'EOF'
#!/bin/bash
echo "🔍 Verifying Claude Code Enhancement Setup..."
echo

# Check directories
dirs=("commands" "agents" "backups" "cleanup-rules")
for dir in "${dirs[@]}"; do
    if [ -d "$HOME/.claude/$dir" ]; then
        echo "✅ $dir directory exists"
    else
        echo "❌ $dir directory missing"
    fi
done

# Check configuration files
configs=("safety-config.json" "agents/agent-config.json" "commit-config.json" "cleanup-config.json")
for config in "${configs[@]}"; do
    if [ -f "$HOME/.claude/$config" ]; then
        echo "✅ $config exists"
    else
        echo "❌ $config missing" 
    fi
done

# Check commands
command_count=$(ls "$HOME/.claude/commands"/*.md 2>/dev/null | wc -l || echo "0")
echo "📋 Found $command_count command files"

echo
echo "🎉 Setup verification complete!"
EOF

chmod +x "$CLAUDE_DIR/verify-setup.sh"

# Success message
echo
echo "🎉 Claude Code Enhancements - Phase 1 Setup Complete!"
echo "=================================================="
echo
echo "📍 Installation location: $CLAUDE_DIR"
echo "🔧 Configuration files created"
echo "📋 Command system installed"
echo "🤖 Multi-agent system ready"
echo "💬 Smart commits enabled"
echo "🧹 Cleanup tools configured"
echo
echo "🚀 Next Steps:"
echo "1. Run: $CLAUDE_DIR/verify-setup.sh"
echo "2. Try: /safe-delete --help"
echo "3. Try: /delegate architect \"help me get started\""
echo "4. Try: /commit-smart (in a git repository)"
echo "5. Try: /clean-config --preview"
echo
echo "📚 Documentation: $CLAUDE_DIR/CLAUDE-enhancements.md"
echo
echo "✨ Happy coding with enhanced Claude Code!"