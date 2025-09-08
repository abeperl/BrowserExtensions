# Cross-Platform Notification System

## Purpose
Real-time notifications for Claude Code events across Windows, macOS, and Linux with rich formatting and actionable responses.

## Core Features

### 1. Event-Driven Notifications
```bash
/notify setup           # Configure notification preferences
/notify test           # Test notification system
/notify history        # Show notification history
/notify config         # Customize notification settings
```

### 2. Notification Types

#### Task Completion Notifications
```
🎉 Claude Code - Task Complete
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ Multi-agent analysis complete
   3 experts provided recommendations

📊 Results:
├── Security: 2 issues found
├── Performance: 4 optimizations suggested  
├── Code Quality: Score improved 15%

⏱️  Duration: 2m 34s
🔗 View Report | 📋 Copy Summary | ❌ Dismiss
```

#### Error & Warning Alerts
```
⚠️  Claude Code - Attention Required
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

🚨 Sensitive data detected in config

📍 Location: ~/.claude/config.json
🔍 Pattern: API key in plain text (line 23)

🛠️  Actions:
├── 🔒 Auto-mask secrets
├── 🧹 Run cleanup tool
├── 📝 Edit manually

⚡ Quick Fix | 🔍 View Details | ⏰ Remind Later
```

#### Progress Updates
```
🔄 Claude Code - In Progress
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

⚙️  Building feature workflow...

Current Step: Security Review (3/5)
├── ✅ Architecture Design
├── ✅ Implementation Plan
├── 🔄 Security Analysis  
├── ⏳ Testing Strategy
└── ⏳ Code Review

⏱️  Est. remaining: 3-5 minutes
📊 Show Progress | ⏸️  Pause | ❌ Cancel
```

### 3. Platform-Specific Implementation

#### Windows (PowerShell Toast)
```powershell
# Windows 10/11 Toast Notifications
function Send-ClaudeNotification {
    param(
        [string]$Title,
        [string]$Message,
        [string]$Type = "Info",
        [string[]]$Actions = @()
    )
    
    $ToastXml = @"
<toast>
    <visual>
        <binding template="ToastGeneric">
            <image placement="appLogoOverride" src="file:///path/to/claude-icon.png"/>
            <text>$Title</text>
            <text>$Message</text>
        </binding>
    </visual>
    <actions>
        $(foreach($action in $Actions) { "<action content='$action' arguments='claude:$action'/>" })
    </actions>
</toast>
"@
    
    [Windows.UI.Notifications.ToastNotificationManager]::CreateToastNotifier("ClaudeCode").Show($ToastXml)
}
```

#### macOS (terminal-notifier)
```bash
#!/bin/bash
# macOS Notification Center
function claude_notify() {
    local title="$1"
    local message="$2"
    local type="${3:-info}"
    local actions="${4:-}"
    
    terminal-notifier \
        -title "Claude Code - $title" \
        -message "$message" \
        -group "claude-code" \
        -sender "com.anthropic.claude-code" \
        -sound "${type}" \
        -actions "$actions" \
        -execute "claude-handle-notification '$actions'"
}
```

#### Linux (notify-send with dunst)
```bash
#!/bin/bash
# Linux Desktop Notifications
function claude_notify() {
    local title="$1"
    local message="$2" 
    local type="${3:-normal}"
    local urgency="${4:-normal}"
    
    notify-send \
        --app-name="Claude Code" \
        --icon="/usr/share/icons/claude-code.png" \
        --urgency="$urgency" \
        --category="development" \
        --hint=string:desktop-entry:claude-code \
        "$title" "$message"
}
```

### 4. Notification Configuration

#### Global Settings
```json
{
  "notifications": {
    "enabled": true,
    "sound": true,
    "badge": true,
    "types": {
      "task_complete": {
        "enabled": true,
        "sound": "completion.wav",
        "priority": "normal",
        "auto_dismiss": 10000
      },
      "error": {
        "enabled": true,
        "sound": "error.wav", 
        "priority": "high",
        "auto_dismiss": false
      },
      "progress": {
        "enabled": true,
        "sound": false,
        "priority": "low",
        "auto_dismiss": 5000
      },
      "suggestion": {
        "enabled": true,
        "sound": "subtle.wav",
        "priority": "normal", 
        "auto_dismiss": 15000
      }
    },
    "quiet_hours": {
      "enabled": false,
      "start": "22:00",
      "end": "08:00"
    },
    "do_not_disturb": {
      "during_calls": true,
      "during_presentations": true,
      "during_focus_sessions": true
    }
  }
}
```

#### Per-Project Settings
```json
{
  "project_notifications": {
    "browser-extensions": {
      "priority_events": ["build_complete", "test_failed", "security_alert"],
      "suppress_events": ["minor_warnings", "style_suggestions"],
      "custom_sounds": {
        "build_complete": "success-bell.wav",
        "test_failed": "alert-tone.wav"
      }
    }
  }
}
```

### 5. Interactive Notifications

#### Action Buttons
```javascript
const notificationActions = {
    'view_report': async (data) => {
        // Open report in default editor or browser
        await openFile(data.reportPath);
    },
    
    'quick_fix': async (data) => {
        // Execute predefined quick fix
        await executeCommand(data.fixCommand);
        sendNotification('Quick fix applied', 'success');
    },
    
    'remind_later': async (data) => {
        // Schedule reminder notification
        setTimeout(() => {
            sendNotification(data.originalTitle, data.originalMessage);
        }, data.reminderDelay);
    }
};
```

#### Notification Center Integration
```bash
# Create notification history
/notify history

📜 NOTIFICATION HISTORY
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Today (7 notifications):
├── 14:32 ✅ Feature analysis complete - security-expert
├── 14:15 ⚠️  API key detected in config.json  
├── 13:45 🎉 Smart commit generated successfully
├── 13:22 🔄 Multi-agent workflow started (3 agents)
├── 12:58 📊 Configuration cleanup completed
├── 12:45 🧪 Tests passed (15/15)
└── 12:30 🚀 Claude Code enhanced workflow active

Yesterday (12 notifications)
This Week (45 notifications)

Filter: [a]ll [e]rrors [w]arnings [s]uccess [p]rogress
Export: /notify export --format json --period week
```

### 6. Smart Notification Logic

#### Contextual Suppression
```javascript
class SmartNotificationManager {
    shouldSuppressNotification(notification, context) {
        // Don't interrupt during focus sessions
        if (context.focusMode && notification.priority !== 'critical') {
            return true;
        }
        
        // Suppress redundant notifications
        if (this.isDuplicateRecent(notification, 300000)) { // 5 minutes
            return true;
        }
        
        // Respect quiet hours
        if (this.isQuietHours() && notification.priority !== 'high') {
            return true;
        }
        
        return false;
    }
    
    enhanceNotification(notification, context) {
        // Add project context
        if (context.projectName) {
            notification.title = `${context.projectName} - ${notification.title}`;
        }
        
        // Add relevant actions based on notification type
        notification.actions = this.generateActions(notification.type, context);
        
        return notification;
    }
}
```

#### Batch Notifications
```bash
# Group related notifications
🔄 Claude Code - Batch Update (3 items)
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

✅ Architecture review complete
⚡ Performance optimization suggestions ready  
🧪 Test coverage analysis finished

📊 Summary: 15 recommendations across 3 domains
⏱️  Total time: 4m 22s

🔍 View All Reports | 📋 Export Summary | ✅ Mark Complete
```

### 7. Notification Analytics

#### Usage Statistics
```bash
/notify stats

📊 NOTIFICATION STATISTICS (Last 30 Days)  
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📈 Activity Overview:
├── Total notifications: 234
├── Daily average: 7.8
├── Response rate: 68%
├── Most active: Tuesday 2-4pm

📋 By Type:
├── Task Complete: 89 (38%) - 85% clicked
├── Errors/Warnings: 67 (29%) - 92% clicked  
├── Progress Updates: 45 (19%) - 32% clicked
├── Suggestions: 33 (14%) - 45% clicked

⚙️  Performance:
├── Delivery time: avg 0.8s
├── Platform reliability: 99.2%
├── User satisfaction: 4.2/5

💡 Suggestions:
├── Reduce progress notifications (low engagement)
├── Enable smart batching for suggestions
└── Consider quiet hours 10pm-8am
```

This comprehensive notification system ensures you never miss important Claude Code events while maintaining focus and preventing notification fatigue through intelligent filtering and batching.