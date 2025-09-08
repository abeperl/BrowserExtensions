# Multi-Agent Task Delegation System

## Purpose
Delegate complex tasks to specialized AI agents with fresh context to prevent context bloat and improve solution quality.

## Available Agents

### Core Development Agents
- **`architect`** - System design and architecture decisions
- **`frontend-dev`** - UI/UX implementation and React/Vue expertise
- **`backend-dev`** - API design, database, and server-side logic
- **`security-expert`** - Security analysis and vulnerability assessment
- **`performance-analyst`** - Performance optimization and profiling
- **`test-engineer`** - Testing strategies and implementation

### Analysis Agents
- **`code-reviewer`** - Code quality and best practices analysis
- **`tech-debt-analyst`** - Technical debt identification and prioritization
- **`debugger`** - Error diagnosis and resolution
- **`refactor-specialist`** - Code restructuring and modernization

## Commands

### Single Agent Delegation
```bash
/delegate <agent> "<task description>"
```

### Multi-Agent Analysis
```bash
/multi-role "<problem>" [agent1,agent2,agent3]
/consensus "<decision>" [agent1,agent2,agent3]
/parallel-analysis "<codebase-review>"
```

### Sequential Workflow
```bash
/workflow architect→backend-dev→test-engineer "Build user authentication system"
```

## Usage Examples

### Complex Feature Implementation
```bash
/delegate architect "Design a real-time chat system with 10k concurrent users"
# Agent provides: Architecture diagrams, technology stack, scaling considerations

/workflow architect→backend-dev→frontend-dev→test-engineer "Implement the chat system"
# Sequential implementation with each agent building on previous work
```

### Code Quality Assessment
```bash
/multi-role "Review this React component for production readiness" security-expert,performance-analyst,code-reviewer
# Parallel analysis from multiple perspectives
```

### Problem Diagnosis
```bash
/delegate debugger "Application crashes under high load - investigate root cause"
# Specialized debugging with fresh context and focused analysis
```

## Benefits

1. **Context Management**: Prevents overwhelming main conversation
2. **Specialized Expertise**: Domain-specific knowledge application
3. **Parallel Processing**: Multiple perspectives simultaneously
4. **Extended Conversations**: Effectively unlimited conversation length
5. **Quality Improvement**: Expert-level analysis for each domain

This system transforms Claude Code into a team of specialized AI experts working together on complex development challenges.