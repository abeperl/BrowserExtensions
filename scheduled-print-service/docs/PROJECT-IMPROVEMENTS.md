# Scheduled Print Service - Project Improvements & Roadmap

## Current Status (as of November 2025)

### ✅ Completed Features
- **Core PDF Rendering**: Headless Chromium with PuppeteerSharp
- **Dual Print Modes**: File output and Windows printer spooling
- **Service Architecture**: Windows Service integration with .NET 9
- **Configuration**: JSON-based settings with environment variable overrides
- **Database Integration**: SQLite for API configuration management
- **Manual API Execution**: PowerShell scripts for on-demand API calls
- **Token Renewal**: Automatic bearer token refresh system
- **Logging**: Serilog with rolling file logs and structured output
- **API Polling**: Configurable polling for order management APIs
- **Sub-Action System**: Chainable actions with enable/disable control
- **Email Notifications**: SMTP notifications on failures
- **Data Validation**: Selector-based validation for dynamic content

---

## 🚀 High Priority Improvements

### 1. **Service Integration with Database Configuration** ⏳
**Status:** Partial (manual execution only)
**Priority:** HIGH
**Effort:** Medium

**Current State:**
- Database schema complete with PrimaryApi, SubAction, Schedule tables
- Manual execution script (`run-api.ps1`) works with database
- Service still reads from `appsettings.json`

**Needed Work:**
```csharp
// In ApiPollSchedulerService.cs or new DatabaseSchedulerService.cs
- Read schedule from database instead of config file
- Load API configs dynamically from database
- Support multiple API polling in sequence
- Implement execution order based on ScheduleApi.ExecutionOrder
```

**Benefits:**
- Dynamic configuration without restarting service
- Support for multiple API workflows
- Better separation of concerns (credentials vs config)

**Files to Modify:**
- `Services/ApiPollSchedulerService.cs` - Load schedules from DB
- `Services/DatabaseApiConfigService.cs` - Extend for full workflow support
- `Program.cs` - Register database-driven scheduler

---

### 2. **Sub-Action Execution Logic** ⏳
**Status:** Not Implemented
**Priority:** HIGH
**Effort:** High

**Current State:**
- SubAction table defines action types and configurations
- No execution engine exists yet
- Manual script shows configs but doesn't execute sub-actions

**Needed Work:**
```csharp
public interface ISubActionExecutor
{
    Task<SubActionResult> ExecuteAsync(SubAction action, object context);
}

// Implement executors for:
- CreatePicklistBatch: Batch order IDs into picklist creation requests
- PrintManualPicking: Render and print picklist page
- UpdateOrderStatus: POST status updates to orders
- WaitDelay: Simple delay between actions
- PrintShippingLabel: Fetch and print shipping labels
- CustomHttpRequest: Generic HTTP action executor
```

**Benefits:**
- Complete automation of order processing workflow
- Reduces manual intervention
- Enables complex multi-step workflows

**Files to Create:**
- `Services/SubActionEngine.cs` - Orchestration engine
- `Services/SubActionExecutors/` - Individual executor implementations
- `Models/SubActionResult.cs` - Execution result model

---

### 3. **Real-Time Monitoring Dashboard** 🆕
**Status:** Not Started
**Priority:** MEDIUM-HIGH
**Effort:** Medium

**Proposal:**
Create a simple web-based dashboard for monitoring service health

**Features:**
- Live service status and uptime
- Recent log entries with filtering
- API execution history and success rates
- Database configuration viewer
- Manual trigger buttons for APIs
- Print queue status

**Technology Options:**
```csharp
// Option 1: Add ASP.NET Core web host to service
builder.Services.AddHostedService<MonitoringWebHost>();

// Option 2: Separate dashboard app that reads logs/database
// Simpler, no service modification needed
```

**Benefits:**
- Quick visibility into service health
- Easier troubleshooting
- Reduced need for RDP/SSH access
- Historical performance tracking

---

### 4. **Advanced Error Handling & Retry Logic** ⚠️
**Status:** Basic implementation
**Priority:** HIGH
**Effort:** Medium

**Current State:**
- Basic try/catch blocks
- Email notifications on failure
- ContinueOnError flag in config

**Needed Improvements:**
```csharp
// Implement Polly policies
services.AddHttpClient<IOrderApiService, OrderApiService>()
    .AddTransientHttpErrorPolicy(policy =>
        policy.WaitAndRetryAsync(3, retryAttempt =>
            TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .AddTransientHttpErrorPolicy(policy =>
        policy.CircuitBreakerAsync(5, TimeSpan.FromMinutes(1)));

// Add dead letter queue for failed jobs
public class FailedJobQueue
{
    public void Enqueue(JobContext job, Exception error);
    public List<FailedJob> GetPendingRetries();
    public void Retry(FailedJob job);
}
```

**Specific Improvements:**
- **Exponential backoff** for transient errors
- **Circuit breaker** pattern for downstream services
- **Dead letter queue** for failed jobs
- **Manual retry mechanism** for failed operations
- **Detailed error categorization** (transient vs permanent)

---

### 5. **Performance Optimization** 📈
**Status:** Baseline implementation
**Priority:** MEDIUM
**Effort:** Medium

**Areas for Improvement:**

#### a) **Parallel PDF Rendering**
```csharp
// Currently sequential - process one URL at a time
// Improve with:
var renderTasks = urls.Select(url => RenderPdfAsync(url));
await Task.WhenAll(renderTasks);

// With semaphore for resource control
using var semaphore = new SemaphoreSlim(3); // Max 3 concurrent renders
```

#### b) **Browser Pool Management**
```csharp
// Reuse browser instances instead of creating per-job
public class BrowserPool
{
    private readonly ConcurrentBag<IBrowser> _browsers;
    public async Task<IBrowser> AcquireAsync();
    public void Release(IBrowser browser);
}
```

#### c) **PDF Caching**
```csharp
// Cache rendered PDFs based on URL + content hash
public class PdfCache
{
    public async Task<byte[]?> GetCachedPdfAsync(string url, string contentHash);
    public async Task SetCachedPdfAsync(string url, string contentHash, byte[] pdf);
}
```

**Expected Benefits:**
- 3-5x faster processing for batch operations
- Reduced memory usage with browser pooling
- Lower bandwidth with PDF caching

---

### 6. **Security Enhancements** 🔒
**Status:** Basic security
**Priority:** MEDIUM-HIGH
**Effort:** Medium

**Current Issues:**
- Credentials in `appsettings.json` (plaintext)
- Bearer tokens in database (unencrypted)
- No audit trail for configuration changes
- No user authentication for manual scripts

**Improvements:**

#### a) **Credential Encryption**
```csharp
// Use Windows DPAPI for credential storage
using System.Security.Cryptography;

public class SecureCredentialStore
{
    public void SetCredential(string key, string value);
    public string GetCredential(string key);
}
```

#### b) **Audit Logging**
```sql
CREATE TABLE AuditLog (
    Id INTEGER PRIMARY KEY,
    Timestamp TEXT NOT NULL,
    Action TEXT NOT NULL,
    UserId TEXT,
    Details TEXT,
    IPAddress TEXT
);
```

#### c) **Access Control**
```powershell
# run-api.ps1 with authentication
param([PSCredential]$Credential)
if (-not (Test-Authorization $Credential)) {
    throw "Unauthorized access"
}
```

---

### 7. **Configuration UI** 🖥️
**Status:** Not Started
**Priority:** MEDIUM
**Effort:** High

**Proposal:**
Build a configuration management UI instead of direct database editing

**Features:**
- API configuration CRUD operations
- Sub-action editor with drag-drop ordering
- Schedule management with visual cron builder
- Test/validate configurations before saving
- Import/export configurations
- Configuration versioning and rollback

**Technology:**
- Blazor Server (simpler, stays in .NET ecosystem)
- Or simple HTML + JavaScript with REST API backend

**Benefits:**
- Reduces errors from manual SQL editing
- Easier for non-technical users
- Validates configurations before saving
- Version control for configuration changes

---

### 8. **Advanced Logging & Observability** 📊
**Status:** Basic logging
**Priority:** MEDIUM
**Effort:** Medium

**Current State:**
- Serilog with file output
- Basic log levels (Debug, Info, Warning, Error)
- No structured querying

**Improvements:**

#### a) **Structured Logging**
```csharp
Log.Information("Order {OrderId} processed in {Duration}ms with {ItemCount} items",
    orderId, duration, itemCount);
```

#### b) **Log Aggregation**
```csharp
// Send logs to centralized system
Log.Logger = new LoggerConfiguration()
    .WriteTo.Seq("http://log-server:5341") // Option 1: Seq
    .WriteTo.Elasticsearch(...) // Option 2: ELK Stack
    .WriteTo.ApplicationInsights(...) // Option 3: Azure
    .CreateLogger();
```

#### c) **Metrics Collection**
```csharp
public class ServiceMetrics
{
    public Counter ApiCalls { get; set; }
    public Histogram PdfRenderTime { get; set; }
    public Gauge ActiveJobs { get; set; }
    public Counter PrintSuccess { get; set; }
    public Counter PrintFailures { get; set; }
}
```

#### d) **Health Checks**
```csharp
builder.Services.AddHealthChecks()
    .AddCheck<DatabaseHealthCheck>("database")
    .AddCheck<PrinterHealthCheck>("printer")
    .AddCheck<ChromiumHealthCheck>("chromium");

// Expose at /health endpoint
app.MapHealthChecks("/health");
```

---

### 9. **Testing Infrastructure** 🧪
**Status:** No tests
**Priority:** MEDIUM
**Effort:** High

**Needed:**

#### a) **Unit Tests**
```csharp
// Test project structure
ScheduledPrintService.Tests/
├── Services/
│   ├── PdfPrintServiceTests.cs
│   ├── OrderApiServiceTests.cs
│   └── TokenRenewalServiceTests.cs
├── Models/
│   └── ApiConfigTests.cs
└── Integration/
    └── DatabaseIntegrationTests.cs
```

#### b) **Integration Tests**
```csharp
[Test]
public async Task EndToEndWorkflow_ProcessesOrderSuccessfully()
{
    // Arrange: Setup test database and mock API
    // Act: Trigger order processing
    // Assert: Verify PDF created and order marked complete
}
```

#### c) **Mock Services**
```csharp
public class MockOrderApiService : IOrderApiService
{
    public Task<OrderResponse> GetOrdersListAsync() =>
        Task.FromResult(new OrderResponse { Orders = TestData.SampleOrders });
}
```

**Testing Goals:**
- 80%+ code coverage
- Automated CI/CD pipeline with tests
- Mock external dependencies
- Test failure scenarios

---

### 10. **Documentation Improvements** 📚
**Status:** Good but incomplete
**Priority:** MEDIUM
**Effort:** Low-Medium

**Existing Docs:**
- ✅ README.md
- ✅ INSTALL.md
- ✅ API-POLLING-GUIDE.md
- ✅ MANUAL-API-EXECUTION.md
- ✅ TOKEN-RENEWAL.md
- ✅ DATABASE-SCHEMA-DIAGRAM.md

**Needed Additions:**
- **Architecture Diagram**: Visual system overview
- **Troubleshooting Guide**: Common issues and solutions
- **API Reference**: Complete API endpoint documentation
- **Configuration Reference**: All settings explained
- **Migration Guide**: Upgrading from old versions
- **Performance Tuning Guide**: Optimization recommendations
- **Backup & Recovery**: Disaster recovery procedures

---

## 🔮 Future Enhancements (Lower Priority)

### 11. **Multi-Tenant Support**
- Support multiple organizations/warehouses
- Isolated data per tenant
- Tenant-specific configurations
- Usage tracking and billing

### 12. **Advanced Scheduling**
- Complex cron expressions with visual editor
- Conditional execution (only if orders > 10)
- Time-window restrictions
- Holiday calendar integration

### 13. **Webhook Support**
- Trigger workflows from external systems
- POST to /webhook/trigger-api endpoint
- Authentication via API keys
- Webhook delivery tracking

### 14. **PDF Customization**
- Custom headers/footers
- Watermarks
- Page numbering styles
- Custom fonts and styling
- Template system for different document types

### 15. **Print Job Management**
- Print queue with priority levels
- Job cancellation
- Job rescheduling
- Batch operations (cancel all, retry all)

### 16. **API Rate Limiting**
- Respect external API rate limits
- Token bucket algorithm
- Automatic throttling
- Rate limit monitoring

### 17. **Data Archival**
- Automatic archival of old PDFs
- Compress old log files
- Database pruning for old records
- Configurable retention policies

### 18. **Multi-Platform Support**
- Linux service support (systemd)
- Docker containerization
- Kubernetes deployment manifests
- Cross-platform configuration

### 19. **Advanced Reporting**
- Daily/weekly summary emails
- Processing time trends
- Error rate analysis
- Cost tracking (printer supplies)

### 20. **Integration Ecosystem**
- REST API for external integrations
- GraphQL support for flexible queries
- Message queue integration (RabbitMQ, Azure Service Bus)
- Export to BI tools (Power BI, Tableau)

---

## 📊 Priority Matrix

| Feature | Priority | Effort | Impact | Timeline |
|---------|----------|--------|--------|----------|
| Database Integration | HIGH | Medium | High | 2-3 weeks |
| Sub-Action Execution | HIGH | High | High | 3-4 weeks |
| Error Handling & Retry | HIGH | Medium | High | 1-2 weeks |
| Security Enhancements | MED-HIGH | Medium | High | 2-3 weeks |
| Monitoring Dashboard | MED-HIGH | Medium | Medium | 2-3 weeks |
| Performance Optimization | MEDIUM | Medium | Medium | 2-3 weeks |
| Configuration UI | MEDIUM | High | Medium | 4-6 weeks |
| Advanced Logging | MEDIUM | Medium | Medium | 2-3 weeks |
| Testing Infrastructure | MEDIUM | High | High | 3-4 weeks |
| Documentation | MEDIUM | Low-Med | Medium | 1-2 weeks |

---

## 🎯 Recommended Implementation Order

### Phase 1: Core Functionality (6-8 weeks)
1. Database Integration with Service (2-3 weeks)
2. Sub-Action Execution Engine (3-4 weeks)
3. Error Handling & Retry Logic (1-2 weeks)

### Phase 2: Production Readiness (6-8 weeks)
4. Security Enhancements (2-3 weeks)
5. Advanced Logging & Observability (2-3 weeks)
6. Testing Infrastructure (3-4 weeks)

### Phase 3: User Experience (4-6 weeks)
7. Monitoring Dashboard (2-3 weeks)
8. Configuration UI (4-6 weeks)
9. Documentation Updates (1-2 weeks)

### Phase 4: Optimization (2-3 weeks)
10. Performance Optimization (2-3 weeks)

### Phase 5: Future Enhancements (As Needed)
11. Multi-tenant support, webhooks, advanced features

---

## 🐛 Known Issues & Technical Debt

### Issue 1: Browser Memory Leaks
**Problem:** Long-running service accumulates browser instances
**Impact:** High memory usage over time
**Workaround:** Restart service periodically
**Proper Fix:** Implement browser pooling with proper disposal

### Issue 2: Token Expiration Edge Cases
**Problem:** Token might expire mid-workflow
**Impact:** Partial failures in multi-step operations
**Workaround:** Manually retry failed operations
**Proper Fix:** Add token refresh before each API call

### Issue 3: PDF Rendering Race Conditions
**Problem:** Fast API calls can overwhelm browser initialization
**Impact:** Occasional render failures
**Workaround:** Add delays in config
**Proper Fix:** Implement proper async initialization with semaphore

### Issue 4: Configuration Reload
**Problem:** Changes to appsettings.json require service restart
**Impact:** Downtime during configuration changes
**Workaround:** Plan changes during maintenance windows
**Proper Fix:** Implement hot configuration reload or use database config

### Issue 5: Error Context Loss
**Problem:** Nested exceptions lose original context
**Impact:** Difficult troubleshooting
**Workaround:** Manual log correlation
**Proper Fix:** Implement correlation IDs across operations

---

## 💡 Technical Debt Items

1. **Refactor PdfBrowserManager** - Too many responsibilities, needs splitting
2. **Extract Configuration Models** - Create separate library for shared models
3. **Standardize Error Handling** - Consistent exception types and handling
4. **Remove Magic Strings** - Use constants for config keys and selectors
5. **Async/Await Consistency** - Some sync methods should be async
6. **Dependency Injection** - Some classes still use `new` instead of DI
7. **Unit of Work Pattern** - Database transactions need better handling
8. **Request/Response Models** - API models should be separate from business models

---

## 📝 Notes on Implementation

### Database-First Approach
The service now supports database-driven configuration for API workflows. The next major step is integrating this with the hosted service scheduler.

### Backward Compatibility
When implementing database integration, maintain backward compatibility with `appsettings.json` configuration for single-API scenarios.

### Performance Considerations
PDF rendering is CPU and memory intensive. Consider implementing:
- Process pooling for multiple simultaneous renders
- PDF compression for large output files
- Cleanup jobs for old files

### Security Best Practices
- Never log credentials or bearer tokens
- Implement credential rotation
- Use HTTPS for all API calls
- Validate all configuration inputs
- Implement rate limiting to prevent abuse

### Monitoring Metrics to Track
- API call success rate
- PDF render time (p50, p95, p99)
- Service uptime
- Memory usage over time
- Disk space usage
- Error rate by type

---

## 🔗 Related Resources

- **Main README**: `scheduled-print-service/README.md`
- **Installation Guide**: `scheduled-print-service/INSTALL.md`
- **Manual API Execution**: `scheduled-print-service/ScheduledPrintService/MANUAL-API-EXECUTION.md`
- **Database Schema**: `scheduled-print-service/ScheduledPrintService/DATABASE-SCHEMA-DIAGRAM.md`
- **Token Renewal**: `scheduled-print-service/TOKEN-RENEWAL.md`
- **Monitoring Script**: `scheduled-print-service/monitor-service.ps1`

---

**Last Updated:** November 24, 2025
**Version:** 1.0
**Maintainer:** Development Team
