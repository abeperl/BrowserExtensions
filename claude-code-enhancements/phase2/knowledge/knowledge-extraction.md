# Knowledge Extraction Tools

## Purpose
Intelligent system to capture, organize, and recall knowledge from Claude Code sessions, creating a persistent memory that improves over time.

## Core Features

### 1. Session Learning (`/learn`)
```bash
/learn                   # Extract key insights from current session
/learn save <topic>      # Save specific knowledge about topic
/learn search <query>    # Search accumulated knowledge
/learn export           # Export knowledge base
/learn stats            # Show learning statistics
```

#### Session Analysis Example:
```
🧠 SESSION LEARNING ANALYSIS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 Session Summary (2h 34m):
├── 🎯 Primary Focus: Browser Extension Enhancement
├── 🔧 Technologies: JavaScript, CSS, Chrome APIs
├── 👥 Agents Used: security-expert, performance-analyst
├── ✅ Tasks Completed: 7/9
└── 📈 Productivity Score: 8.2/10

🔍 Key Insights Discovered:

TECHNICAL LEARNINGS:
├── 🛡️  Security: Content script isolation prevents memory leaks
├── ⚡ Performance: MutationObserver requires manual disconnect
├── 🎨 UI/UX: Audio feedback improves user confidence by 40%
├── 🧪 Testing: Manifest V3 requires specific test configuration

WORKFLOW PATTERNS:
├── 🏗️  Multi-agent analysis → 3x faster problem resolution
├── 🔒 Safety-first approach → 0 accidental deletions
├── 📝 Smart commits → 85% conventional format compliance
└── 🧹 Regular cleanup → 60% storage optimization

ANTI-PATTERNS IDENTIFIED:
├── ❌ Direct DOM manipulation without error handling
├── ⚠️  Large CSS files without organization
└── 🔄 Repeated manual operations (candidates for automation)

FUTURE RECOMMENDATIONS:
├── 📦 Extract audio handler to separate module
├── 🔧 Implement automated testing for overlay system
├── 📚 Document extension architecture patterns
└── 🚀 Consider progressive web app features

Save these insights? [Y/n]
Auto-apply recommendations? [Y/n/selective]
```

### 2. Knowledge Base Management

#### Structured Knowledge Storage
```json
{
  "knowledge_base": {
    "version": "2.0.0",
    "created": "2024-01-15T10:00:00Z",
    "last_updated": "2024-01-20T15:30:00Z",
    "entries": [
      {
        "id": "kb_001",
        "topic": "Browser Extension Memory Management",
        "category": "performance",
        "tags": ["javascript", "chrome-api", "memory-leaks"],
        "confidence": 0.92,
        "source": "session_2024-01-20",
        "content": {
          "problem": "MutationObserver not being disconnected causes memory leaks",
          "solution": "Always disconnect observers in cleanup functions",
          "code_example": "observer.disconnect(); observer = null;",
          "validation": "Verified in production with 3 different extensions",
          "related_topics": ["cleanup-patterns", "event-listeners"]
        },
        "usage_count": 7,
        "success_rate": 0.95,
        "last_applied": "2024-01-20T14:20:00Z"
      }
    ],
    "patterns": [
      {
        "id": "pattern_001", 
        "name": "Multi-Agent Problem Solving",
        "description": "Use multiple specialized agents for complex analysis",
        "workflow": "architect→security-expert→performance-analyst",
        "success_rate": 0.88,
        "avg_time_saved": "12.5 minutes",
        "use_cases": ["feature-design", "code-review", "debugging"]
      }
    ]
  }
}
```

### 3. Intelligent Search & Retrieval

#### Context-Aware Search
```bash
/learn search "memory leak"

🔍 KNOWLEDGE SEARCH RESULTS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Found 4 relevant entries (sorted by relevance):

[1] 🎯 Browser Extension Memory Management (95% match)
    💡 MutationObserver cleanup prevents memory leaks
    🏷️  Tags: javascript, chrome-api, memory-leaks
    📅 Last applied: 2 days ago (Success: 95%)
    
    Quick Apply:
    • Add observer.disconnect() to cleanup functions
    • Set observer references to null
    • Test with Chrome DevTools Memory tab

[2] ⚡ Event Listener Memory Patterns (87% match)  
    💡 Removing event listeners prevents accumulation
    🏷️  Tags: dom-events, cleanup, performance
    📅 Applied: 5 times (Success: 90%)

[3] 🔄 React Component Unmounting (73% match)
    💡 useEffect cleanup functions for memory management
    🏷️  Tags: react, hooks, lifecycle
    📅 Applied: 3 times (Success: 85%)

[4] 📦 Module Loading Memory Impact (68% match)
    💡 Dynamic imports can help reduce initial memory usage
    🏷️  Tags: modules, optimization, loading
    📅 Applied: 1 time (Success: 100%)

Apply solution: /learn apply <number>
Show details: /learn show <number>
Save to workflow: /workflow save "memory-fix" <number>
```

### 4. Learning Pattern Recognition

#### Automatic Pattern Detection
```
🤖 LEARNING PATTERNS DETECTED
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 Analysis of last 50 sessions:

EMERGING PATTERNS:
├── 🔄 "Security Review → Performance Audit" (12 occurrences)
│   └── 92% success rate, saves avg 8 minutes
├── 🧪 "Feature Implementation → Test Creation" (18 occurrences)  
│   └── 88% success rate, improves quality 15%
└── 🔍 "Problem Investigation → Multi-Agent Analysis" (8 occurrences)
    └── 95% success rate, finds root cause 80% faster

SUCCESS PREDICTORS:
├── ✅ Using safety commands first → 87% session success
├── ⚡ Multi-agent for complex tasks → 3x faster resolution  
├── 📝 Smart commits → 92% first-time PR approval
└── 🧹 Regular cleanup → 40% fewer configuration issues

FAILURE PATTERNS:
├── ❌ Skipping safety preview → 23% error rate
├── ⚠️  Large changes without testing → 45% revert rate
└── 🔄 Manual repetitive tasks → 60% productivity loss

RECOMMENDATIONS:
├── 🤖 Auto-suggest multi-agent for tasks >30 min complexity
├── 🛡️  Enforce safety preview for file operations >10KB
├── 📋 Create templates for recurring workflows
└── ⚡ Propose automation for tasks repeated >3x

Accept pattern-based suggestions? [Y/n]
Create auto-workflows? [Y/n/selective]
```

### 5. Knowledge Sharing & Export

#### Team Knowledge Base
```bash
/learn export --team         # Export for team sharing
/learn import --team <file>  # Import team knowledge
/learn sync --remote <url>   # Sync with team repository
/learn anonymize            # Remove personal data for sharing
```

#### Export Formats
```yaml
# team-knowledge.yaml
knowledge_export:
  format: "claude-code-kb-v2"
  exported: "2024-01-20T15:30:00Z"
  team: "browser-extension-team"
  
  best_practices:
    - topic: "Extension Security"
      rules:
        - "Always validate external inputs in content scripts"
        - "Use declarativeNetRequest instead of webRequest when possible"  
        - "Implement CSP headers for extension pages"
      success_metrics:
        - "0 security vulnerabilities in last 6 months"
        - "100% manifest v3 compliance"
        
    - topic: "Performance Optimization"
      rules:
        - "Disconnect MutationObservers in cleanup"
        - "Use passive event listeners where appropriate"
        - "Minimize DOM queries with caching"
      success_metrics:
        - "Memory usage <50MB average"
        - "Load time <200ms"

  workflows:
    - name: "Feature Development"
      steps: ["architect", "security-review", "implementation", "testing"]
      success_rate: 0.92
      avg_time: "45 minutes"
      
    - name: "Bug Investigation"  
      steps: ["debugger", "reproduce", "root-cause", "fix", "validate"]
      success_rate: 0.89
      avg_time: "25 minutes"

  common_solutions:
    - problem: "Memory leaks in content scripts"
      solution: "Implement cleanup functions with observer.disconnect()"
      code: |
        function cleanup() {
          if (observer) {
            observer.disconnect();
            observer = null;
          }
        }
        window.addEventListener('beforeunload', cleanup);
      applications: 15
      success_rate: 0.95
```

### 6. Continuous Learning System

#### Feedback Loop Integration
```javascript
class ContinuousLearningSystem {
    async recordOutcome(sessionId, solution, outcome) {
        const knowledge = await this.getKnowledgeEntry(solution.id);
        
        // Update success metrics
        knowledge.usage_count++;
        knowledge.outcomes.push({
            session: sessionId,
            success: outcome.success,
            time_saved: outcome.timeSaved,
            user_satisfaction: outcome.satisfaction,
            timestamp: new Date()
        });
        
        // Recalculate confidence score
        knowledge.confidence = this.calculateConfidence(knowledge.outcomes);
        
        // Update recommendations if pattern changes
        if (knowledge.confidence < 0.7) {
            await this.flagForReview(knowledge);
        }
        
        await this.saveKnowledge(knowledge);
        return this.generateLearningInsights(knowledge);
    }
    
    async suggestImprovements() {
        const lowPerformance = await this.getKnowledge({
            success_rate: { $lt: 0.8 },
            usage_count: { $gt: 5 }
        });
        
        return lowPerformance.map(kb => ({
            issue: `Low success rate (${kb.success_rate}) for "${kb.topic}"`,
            suggestion: await this.generateImprovement(kb),
            evidence: kb.failure_patterns,
            proposed_fix: await this.proposeAlternative(kb)
        }));
    }
}
```

#### Smart Recommendations
```
💡 LEARNING-BASED RECOMMENDATIONS
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Based on your patterns and team knowledge:

IMMEDIATE ACTIONS:
├── 🔧 Use /multi-role for the current task complexity level
├── 🛡️  Run /audit-config (last scan: 5 days ago)  
└── 📝 Consider /commit-interactive for this feature branch

WORKFLOW OPTIMIZATIONS:
├── ⚡ Automate: CSS organization (detected 3x manual sorting)
├── 📋 Template: "Bug investigation" workflow (used 8x this month)
└── 🎯 Focus: Memory optimization patterns (high success rate)

SKILL DEVELOPMENT:
├── 📚 Learn: Advanced CSS selectors (knowledge gap detected)
├── 🔍 Practice: Multi-agent coordination (low utilization)  
└── 🧪 Explore: Automated testing patterns (team trending)

TEAM INSIGHTS:
├── 🏆 Your security review success rate: 95% (team: 87%)
├── ⚡ Performance optimization time: 20% faster than average
└── 🤝 Knowledge sharing score: Excellent (15 contributions)

Apply recommendations: /learn apply-suggestions
Customize priorities: /learn config --preferences
Share insights: /learn share --team "security-patterns"
```

This knowledge extraction system creates a continuously improving AI assistant that learns from every interaction, building institutional memory that benefits both individual developers and entire teams.