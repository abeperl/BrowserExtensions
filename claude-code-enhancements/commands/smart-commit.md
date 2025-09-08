# Smart Commit Generation

## Purpose
Intelligent commit message generation with comprehensive change analysis and best practices enforcement.

## Commands

### Basic Smart Commit
```bash
/commit-smart                    # Analyze changes and generate commit
/commit-preview                  # Show proposed commit without executing
/commit-interactive             # Step-by-step commit builder
```

### Advanced Options
```bash
/commit-smart --type feat       # Specify conventional commit type
/commit-smart --scope api       # Add scope to commit message
/commit-smart --breaking        # Mark as breaking change
/commit-smart --ticket ABC-123  # Link to issue/ticket
```

## Analysis Process

### 1. Change Detection & Analysis
```
🔍 ANALYZING CHANGES...
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

📊 Change Summary:
├── 📝 Modified: 5 files
├── ➕ Added: 2 files  
├── ❌ Deleted: 1 file
└── 📦 Total: +127 -43 lines

📁 Affected Areas:
├── 🎨 Frontend: src/components/ (3 files)
├── ⚙️  Backend: src/api/ (2 files)  
├── 🧪 Tests: __tests__/ (2 files)
└── 📚 Documentation: README.md (1 file)
```

### 2. Generated Commit Example
```
feat(auth): implement OAuth2 login with Google provider

- Add GoogleAuthProvider component with redirect flow
- Integrate with backend /auth/google endpoint  
- Add user session management with JWT tokens
- Update login page with Google sign-in button

Closes: #156
Breaking Change: Updates authentication flow
Co-authored-by: Claude <noreply@anthropic.com>
```

## Interactive Mode Example

```
🎯 INTERACTIVE COMMIT BUILDER
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

Step 1/4: Commit Type
[ ] feat     - New feature
[x] fix      - Bug fix  
[ ] refactor - Code refactoring
[ ] docs     - Documentation

Step 2/4: Scope (optional)
🎯 Detected scopes: auth, api, frontend
Enter scope: auth

Step 3/4: Description  
🤖 Suggested: "resolve login timeout issue"
✏️  Edit or accept: fix login timeout on slow connections

Step 4/4: Additional Details
[ ] Breaking change
[x] Closes issue #142
[ ] Add co-author

Proceed with commit? [Y/n]
```

## Quality Assurance

### Pre-commit Validations
- **Lint Checks**: Code style and syntax validation
- **Test Requirements**: Ensure tests pass before commit  
- **Security Scan**: Check for secrets or vulnerabilities
- **Breaking Change Review**: Confirm intentional breaking changes

This system ensures every commit is meaningful, well-formatted, and follows best practices while saving developers time on commit message composition.