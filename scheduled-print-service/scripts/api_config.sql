-- Primary API Configurations
-- Each primary API has a unique ApiNumber and stores all configuration details
CREATE TABLE PrimaryApi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ApiNumber INTEGER NOT NULL UNIQUE,
    ApiName TEXT NOT NULL,
    BaseUrl TEXT NOT NULL,
    Endpoint TEXT NOT NULL,
    HttpMethod TEXT NOT NULL DEFAULT 'GET',
    Headers TEXT, -- JSON string of key-value pairs
    Params TEXT,  -- JSON string of key-value pairs
    Payload TEXT, -- JSON string for request body
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    LocalJsonFilePath TEXT, -- Optional: path to local JSON file to use instead of API call
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Sub Actions associated with each Primary API
-- Each sub-action can have its own configuration
CREATE TABLE SubAction (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    PrimaryApiId INTEGER NOT NULL,
    ActionNumber INTEGER NOT NULL,
    ActionName TEXT NOT NULL,
    ActionType TEXT NOT NULL, -- e.g., 'PrintPdf', 'SendEmail', 'CallApi'
    Configuration TEXT, -- JSON string with action-specific config
    ExecutionOrder INTEGER NOT NULL DEFAULT 0,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    FOREIGN KEY (PrimaryApiId) REFERENCES PrimaryApi(Id) ON DELETE CASCADE,
    UNIQUE(PrimaryApiId, ActionNumber)
);

-- Schedules table to define when APIs should be called
CREATE TABLE Schedule (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScheduleName TEXT NOT NULL,
    CronExpression TEXT NOT NULL,
    IsEnabled INTEGER NOT NULL DEFAULT 1,
    CreatedAt TEXT DEFAULT CURRENT_TIMESTAMP,
    UpdatedAt TEXT DEFAULT CURRENT_TIMESTAMP
);

-- Junction table linking Schedules to API Numbers
-- When a schedule triggers, all associated APIs will be called
CREATE TABLE ScheduleApi (
    Id INTEGER PRIMARY KEY AUTOINCREMENT,
    ScheduleId INTEGER NOT NULL,
    ApiNumber INTEGER NOT NULL,
    ExecutionOrder INTEGER NOT NULL DEFAULT 0,
    FOREIGN KEY (ScheduleId) REFERENCES Schedule(Id) ON DELETE CASCADE,
    FOREIGN KEY (ApiNumber) REFERENCES PrimaryApi(ApiNumber) ON DELETE CASCADE,
    UNIQUE(ScheduleId, ApiNumber)
);

-- Create indexes for better query performance
CREATE INDEX idx_primaryapi_apinumber ON PrimaryApi(ApiNumber);
CREATE INDEX idx_primaryapi_isenabled ON PrimaryApi(IsEnabled);
CREATE INDEX idx_subaction_primaryapiid ON SubAction(PrimaryApiId);
CREATE INDEX idx_subaction_executionorder ON SubAction(ExecutionOrder);
CREATE INDEX idx_schedule_isenabled ON Schedule(IsEnabled);
CREATE INDEX idx_scheduleapi_scheduleid ON ScheduleApi(ScheduleId);
CREATE INDEX idx_scheduleapi_apinumber ON ScheduleApi(ApiNumber);