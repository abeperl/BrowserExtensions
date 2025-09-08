# Advanced Slash Command System

## Purpose
Interactive, context-aware command system with auto-completion, history, and intelligent suggestions based on project context.

## Core Features

### 1. Interactive Command Builder (`/build`)
```bash
/build                    # Launch interactive command builder
/build feature           # Build feature implementation workflow
/build bugfix            # Build bug investigation workflow  
/build review            # Build code review workflow
```

#### Interactive Session Example:
```
🎯 COMMAND BUILDER
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

What would you like to accomplish?
1. 🏗️  Build new feature
2. 🐛 Fix existing bug
3. 🔍 Code review/analysis
4. ⚡ Performance optimization
5. 🛡️  Security audit
6. 📚 Documentation update
7. 🧪 Add tests
8. 🔧 Refactor code

Selection [1-8]: 1

Building Feature Implementation Workflow...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1: Feature Design
/delegate architect "Design: [FEATURE_NAME]"

Step 2: Implementation Planning  
/workflow architect→backend-dev→frontend-dev "Plan implementation"

Step 3: Code Generation
/generate component [COMPONENT_NAME]
/generate api [API_ENDPOINT]

Step 4: Testing Strategy
/delegate test-engineer "Create test plan"

Step 5: Review & Deploy
/commit-smart --type feat
/delegate code-reviewer "Review implementation"

Execute workflow? [Y/n/c(ustomize)]
```

### 2. Context-Aware Suggestions (`/suggest`)
```bash
/suggest                 # Analyze current context and suggest actions
/suggest next           # What should I do next?
/suggest optimize       # Optimization opportunities
/suggest fix            # Potential issues to address
```

#### Context Analysis Output:
```
🔍 CONTEXT ANALYSIS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📍 Current State:
├── 📁 Project: Browser Extensions (TypeScript/JavaScript)
├── 🌿 Branch: main (3 commits ahead)
├── 📝 Modified: 7 files (scan-overlay-extension/)
├── 🧪 Tests: 12 passing, 2 pending
└── 🚀 Last deploy: 2 days ago

🎯 Intelligent Suggestions:

HIGH PRIORITY:
├── 🐛 Fix memory leak in content.js:633 (MutationObserver)
├── 🧪 Add tests for new overlay.js audio handling  
└── 📚 Update README with new enhancement features

MEDIUM PRIORITY:
├── ⚡ Optimize CSS selector performance in styles.css
├── 🔒 Review security of new cleanup patterns
└── 🏗️  Consider extracting audio handler to separate module

LOW PRIORITY:  
├── 📦 Update dependencies (3 minor updates available)
├── 📱 Test extension on mobile browsers
└── 🎨 Improve error message consistency

Execute suggestion? Type number [1-9] or 'all' for batch execution
```

### 3. Smart Command History (`/history`)
```bash
/history                 # Show command history with context
/history search <term>   # Search command history
/history repeat <id>     # Repeat previous command
/history workflow        # Show workflow patterns
```

#### Command History Display:
```
📜 COMMAND HISTORY (Last 10)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

[10] /commit-smart --type feat --scope overlay           ✅ 2min ago
[9]  /multi-role "review audio handling" security,perf   ✅ 15min ago  
[8]  /delegate architect "design cleanup system"         ✅ 1hr ago
[7]  /safe-edit content.js                               ✅ 1hr ago
[6]  /clean-config --preview                             ❌ 2hr ago (cancelled)
[5]  /audit-config                                       ✅ 2hr ago

🔄 COMMON PATTERNS:
├── Feature Development: architect→backend-dev→test (used 5x)
├── Bug Investigation: debugger→security-expert (used 3x)
└── Code Review: security,performance,reviewer (used 8x)

Repeat command: /history repeat <id>
Create workflow: /workflow save "pattern-name" <command-sequence>
```

### 4. Project-Specific Commands (`/project`)
```bash
/project init           # Initialize project-specific commands
/project commands       # Show available project commands  
/project workflow <name># Execute project workflow
/project config         # Configure project settings
```

#### Project Command Detection:
```
🏗️  PROJECT COMMAND DETECTION
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Detected Project Type: Browser Extension (Manifest v3)

📋 Available Project Commands:
├── /ext-build          # Build extension for Chrome/Edge
├── /ext-test          # Test extension in browser
├── /ext-package       # Package for store submission  
├── /ext-manifest      # Validate manifest.json
├── /ext-permissions   # Analyze permission usage
└── /ext-publish       # Publish to extension stores

🔧 Project Workflows:
├── development: edit→test→build→review
├── release: build→test→package→publish
└── debug: analyze→fix→test→commit

Auto-detected from: manifest.json, package.json, file structure
Configure: /project config --customize
```

### 5. Command Composition (`/compose`)
```bash
/compose                # Interactive command composition
/compose save <name>    # Save command sequence
/compose load <name>    # Load saved composition
/compose chain <cmd1> <cmd2> <cmd3>  # Chain commands
```

#### Command Composition Interface:
```
🔧 COMMAND COMPOSER
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Building: Feature Implementation Sequence

[1] /delegate architect "design user authentication"
[2] /safe-edit src/auth/        
[3] /delegate security-expert "review auth implementation"
[4] /commit-smart --type feat --scope auth
[5] /delegate test-engineer "create auth tests"

Options:
[a]dd command  [r]emove  [e]dit  [s]ave  [x]execute  [q]uit

Current sequence will:
- Get architecture design (2-3 min)  
- Safely edit authentication files
- Security review of implementation
- Generate smart commit message
- Create comprehensive tests

Total estimated time: 8-12 minutes
Execute sequence? [Y/n]
```

## Implementation Architecture

### Command Registry System
```javascript
class AdvancedCommandRegistry {
    constructor() {
        this.commands = new Map();
        this.history = [];
        this.workflows = new Map();
        this.suggestions = new ContextSuggester();
    }

    register(name, handler, metadata) {
        this.commands.set(name, {
            handler,
            metadata,
            usage: metadata.usage || [],
            examples: metadata.examples || []
        });
    }

    async execute(input, context) {
        const parsed = this.parseCommand(input);
        const suggestion = await this.suggestions.analyze(context);
        
        return this.executeWithContext(parsed, context, suggestion);
    }
}
```

### Context Analysis Engine
```javascript
class ContextAnalyzer {
    async analyze(projectPath) {
        const context = {
            projectType: await this.detectProjectType(projectPath),
            gitState: await this.getGitState(projectPath),
            files: await this.analyzeFiles(projectPath),
            dependencies: await this.analyzeDependencies(projectPath),
            tests: await this.analyzeTests(projectPath)
        };

        return this.generateSuggestions(context);
    }

    generateSuggestions(context) {
        const suggestions = [];
        
        // Priority-based suggestion engine
        if (context.gitState.uncommitted > 0) {
            suggestions.push({
                priority: 'HIGH',
                action: 'commit-smart',
                reason: 'Uncommitted changes detected'
            });
        }

        return suggestions.sort((a, b) => 
            this.priorityScore(b.priority) - this.priorityScore(a.priority)
        );
    }
}
```

## Benefits

### Enhanced Productivity
- **Intelligent Suggestions**: Context-aware recommendations
- **Command Composition**: Build complex workflows interactively
- **Project Awareness**: Commands adapt to project type
- **History & Patterns**: Learn from previous successful workflows

### Improved Discoverability  
- **Interactive Builders**: Guide users through complex tasks
- **Auto-completion**: Smart command and parameter suggestions
- **Help Integration**: Contextual help and examples
- **Learning System**: Adapts to user preferences and patterns

### Workflow Optimization
- **Sequence Automation**: Save and replay command sequences
- **Pattern Recognition**: Identify and suggest common workflows
- **Context Switching**: Seamless transitions between different tasks
- **Batch Operations**: Execute multiple related commands efficiently

This advanced command system transforms Claude Code from a simple AI assistant into an intelligent development companion that learns, suggests, and adapts to your workflow patterns.