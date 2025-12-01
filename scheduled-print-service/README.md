# Scheduled Print Service

Automated print service that polls APIs, processes orders, and generates PDFs for printing.

## 📁 Project Structure

```
scheduled-print-service/
├── ScheduledPrintService/     # Main C# project
│   ├── Services/              # Service implementations
│   ├── Models/                # Data models
│   ├── appsettings.json       # Configuration
│   └── Program.cs             # Entry point
├── docs/                      # Documentation
│   ├── README.md              # Main documentation
│   ├── INSTALL.md             # Installation guide
│   ├── API-POLLING-GUIDE.md   # API configuration
│   ├── CHAINING-GUIDE.md      # Action chaining
│   ├── MONITORING-GUIDE.md    # Monitoring & logs
│   └── TOKEN-RENEWAL.md       # Authentication
├── scripts/                   # PowerShell & SQL scripts
│   ├── install-service.ps1    # Service installation
│   ├── deploy-*.ps1           # Deployment scripts
│   ├── configure-*.ps1        # Setup scripts
│   └── *.sql                  # Database migrations
├── data/                      # Runtime data (not in git)
│   ├── api_config.db          # SQLite database
│   └── *.txt                  # Output files
├── logs/                      # Application logs
├── out/                       # Generated PDFs
└── screenshots/               # Diagnostic screenshots
```

## 🚀 Quick Start

### 1. Installation
See [docs/INSTALL.md](docs/INSTALL.md) for complete installation instructions.

```powershell
# Run as Administrator
cd ScheduledPrintService
.\install-service.ps1
```

### 2. Configuration
Configure API endpoints and credentials:

```powershell
cd scripts
.\configure-database.ps1
```

### 3. Start Service
```powershell
Start-Service ScheduledPrintService
```

## 📚 Documentation

- **[Main Documentation](docs/README.md)** - Complete feature documentation
- **[Installation Guide](docs/INSTALL.md)** - Step-by-step setup
- **[API Configuration](docs/API-POLLING-GUIDE.md)** - Configure API endpoints
- **[Monitoring Guide](docs/MONITORING-GUIDE.md)** - Logs and troubleshooting
- **[Token Renewal](docs/TOKEN-RENEWAL.md)** - Authentication setup

## 🛠️ Development

### Build
```bash
dotnet build --configuration Release
```

### Publish
```bash
dotnet publish --configuration Release --output publish
```

### Deploy to Server
```powershell
cd scripts
.\deploy-to-server.ps1
```

## 📋 Features

- **API Polling**: Automated polling of multiple REST APIs
- **PDF Generation**: Convert web pages to PDFs using Puppeteer
- **Automatic Printing**: Send PDFs to network printers
- **Token Renewal**: Automatic authentication token management
- **Action Chaining**: Complex workflows with multiple steps
- **Database Tracking**: SQLite for processed order tracking
- **Configurable Schedules**: Cron-based API polling
- **Error Handling**: Retry logic and comprehensive logging

## 🔧 Key Scripts

| Script | Purpose |
|--------|---------|
| `install-service.ps1` | Install Windows service |
| `deploy-to-server.ps1` | Deploy to production |
| `configure-database.ps1` | Set up database |
| `check-database.ps1` | Verify database schema |
| `diagnose-logs.ps1` | Analyze service logs |

## 📊 APIs Configured

1. **API #1** - Picklists (Polling)
2. **API #2** - Pending Orders (Polling)
3. **API #3** - Personalized Orders (Polling)
4. **API #4** - On-Demand Picklists (Manual trigger)

## 🐛 Troubleshooting

### View Logs
```powershell
Get-Content "logs\log-$(Get-Date -Format 'yyyyMMdd').txt" -Wait -Tail 50
```

### Check Service Status
```powershell
Get-Service ScheduledPrintService
```

### Database Issues
```powershell
cd scripts
.\check-database.ps1
```

See [Monitoring Guide](docs/MONITORING-GUIDE.md) for detailed troubleshooting.

## 📝 License

Internal use only.

## 🔗 Related Projects

Part of the BrowserExtensions repository - automated workflows for 3PL operations.
