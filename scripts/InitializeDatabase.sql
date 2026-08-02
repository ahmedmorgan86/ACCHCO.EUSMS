-- ============================================================
-- ACCHCO EUSMS - Database Initialization Script
-- Alexandria Container & Cargo Handling Company
-- End User Support Management System
-- ============================================================

-- Create Database
IF NOT EXISTS (SELECT name FROM sys.databases WHERE name = 'ACCHCO_EUSMS')
BEGIN
    CREATE DATABASE ACCHCO_EUSMS;
END
GO

USE ACCHCO_EUSMS;
GO

-- Create Tables
IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Users')
BEGIN
    CREATE TABLE [Users] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Username] NVARCHAR(100) NOT NULL,
        [FullName] NVARCHAR(200) NOT NULL,
        [Email] NVARCHAR(200) NULL,
        [Phone] NVARCHAR(50) NULL,
        [Section] NVARCHAR(100) NULL,
        [Title] NVARCHAR(200) NULL,
        [Role] INT NOT NULL DEFAULT 3,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [IsFromAd] BIT NOT NULL DEFAULT 0,
        [AdSid] NVARCHAR(200) NULL,
        [LastLoginDate] DATETIME2 NULL,
        [FailedLoginAttempts] INT NOT NULL DEFAULT 0,
        [LockoutEnd] DATETIME2 NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        CONSTRAINT UQ_Users_Username UNIQUE ([Username])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Tickets')
BEGIN
    CREATE TABLE [Tickets] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [TicketNumber] NVARCHAR(20) NOT NULL,
        [TicketDate] DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
        [TicketTime] TIME NOT NULL DEFAULT CAST(GETDATE() AS TIME),
        [Shift] INT NOT NULL DEFAULT 1,
        [SupportSpecialistId] INT NULL,
        [RequesterName] NVARCHAR(200) NOT NULL,
        [RequesterSection] NVARCHAR(100) NOT NULL,
        [RequesterEmail] NVARCHAR(200) NULL,
        [RequesterPhone] NVARCHAR(50) NULL,
        [Location] NVARCHAR(200) NULL,
        [EquipmentType] INT NOT NULL DEFAULT 1,
        [EquipmentName] NVARCHAR(200) NULL,
        [AssetTag] NVARCHAR(100) NULL,
        [ComputerName] NVARCHAR(100) NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [OperatingSystem] NVARCHAR(100) NULL,
        [FaultType] INT NOT NULL DEFAULT 1,
        [Priority] INT NOT NULL DEFAULT 2,
        [Status] INT NOT NULL DEFAULT 1,
        [ProblemDescription] NVARCHAR(4000) NOT NULL,
        [ActionsTaken] NVARCHAR(4000) NULL,
        [Solution] NVARCHAR(4000) NULL,
        [StartTime] DATETIME2 NULL,
        [FinishTime] DATETIME2 NULL,
        [ResolutionTimeMinutes] FLOAT NULL,
        [Notes] NVARCHAR(4000) NULL,
        [IsUrgent] BIT NOT NULL DEFAULT 0,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        CONSTRAINT UQ_Tickets_TicketNumber UNIQUE ([TicketNumber]),
        CONSTRAINT FK_Tickets_SupportSpecialist FOREIGN KEY ([SupportSpecialistId])
            REFERENCES [Users]([Id]) ON DELETE SET NULL
    );

    CREATE INDEX IX_Tickets_TicketDate ON [Tickets]([TicketDate]);
    CREATE INDEX IX_Tickets_Status ON [Tickets]([Status]);
    CREATE INDEX IX_Tickets_TicketNumber ON [Tickets]([TicketNumber]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TicketAttachments')
BEGIN
    CREATE TABLE [TicketAttachments] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [TicketId] INT NOT NULL,
        [FileName] NVARCHAR(500) NOT NULL,
        [OriginalFileName] NVARCHAR(500) NOT NULL,
        [ContentType] NVARCHAR(200) NOT NULL,
        [FileSize] BIGINT NOT NULL,
        [FileData] VARBINARY(MAX) NOT NULL,
        [UploadedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UploadedBy] NVARCHAR(200) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_TicketAttachments_Ticket FOREIGN KEY ([TicketId])
            REFERENCES [Tickets]([Id]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'TicketHistories')
BEGIN
    CREATE TABLE [TicketHistories] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [TicketId] INT NOT NULL,
        [FieldName] NVARCHAR(100) NOT NULL,
        [OldValue] NVARCHAR(4000) NULL,
        [NewValue] NVARCHAR(4000) NULL,
        [ChangedBy] NVARCHAR(200) NOT NULL,
        [ChangedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ChangeDescription] NVARCHAR(4000) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_TicketHistories_Ticket FOREIGN KEY ([TicketId])
            REFERENCES [Tickets]([Id]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Equipment')
BEGIN
    CREATE TABLE [Equipment] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Name] NVARCHAR(200) NOT NULL,
        [Type] INT NOT NULL DEFAULT 1,
        [AssetTag] NVARCHAR(100) NULL,
        [SerialNumber] NVARCHAR(200) NULL,
        [Manufacturer] NVARCHAR(200) NULL,
        [Model] NVARCHAR(200) NULL,
        [ComputerName] NVARCHAR(100) NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [MacAddress] NVARCHAR(50) NULL,
        [OperatingSystem] NVARCHAR(100) NULL,
        [Location] NVARCHAR(200) NULL,
        [Section] NVARCHAR(100) NULL,
        [AssignedTo] NVARCHAR(200) NULL,
        [Department] NVARCHAR(200) NULL,
        [PurchaseDate] DATETIME2 NULL,
        [WarrantyExpiry] DATETIME2 NULL,
        [Notes] NVARCHAR(4000) NULL,
        [IsActive] BIT NOT NULL DEFAULT 1,
        [Status] INT NOT NULL DEFAULT 1,
        [BarcodeNumber] NVARCHAR(100) NULL,
        [SubnetMask] NVARCHAR(50) NULL,
        [DefaultGateway] NVARCHAR(50) NULL,
        [DNSServers] NVARCHAR(200) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );

    CREATE INDEX IX_Equipment_AssetTag ON [Equipment]([AssetTag]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KnowledgeBaseArticles')
BEGIN
    CREATE TABLE [KnowledgeBaseArticles] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Title] NVARCHAR(500) NOT NULL,
        [Content] NVARCHAR(MAX) NOT NULL,
        [Category] NVARCHAR(200) NULL,
        [RelatedEquipmentType] INT NULL,
        [RelatedFaultType] INT NULL,
        [Tags] NVARCHAR(1000) NULL,
        [ViewCount] INT NOT NULL DEFAULT 0,
        [IsPublished] BIT NOT NULL DEFAULT 1,
        [AuthorId] INT NULL,
        [LastViewedDate] DATETIME2 NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_KB_Author FOREIGN KEY ([AuthorId])
            REFERENCES [Users]([Id]) ON DELETE SET NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'KBArticleAttachments')
BEGIN
    CREATE TABLE [KBArticleAttachments] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [ArticleId] INT NOT NULL,
        [FileName] NVARCHAR(500) NOT NULL,
        [OriginalFileName] NVARCHAR(500) NOT NULL,
        [ContentType] NVARCHAR(200) NOT NULL,
        [FileSize] BIGINT NOT NULL,
        [FileData] VARBINARY(MAX) NOT NULL,
        [UploadedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UploadedBy] NVARCHAR(200) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_KBArticleAttachments_Article FOREIGN KEY ([ArticleId])
            REFERENCES [KnowledgeBaseArticles]([Id]) ON DELETE CASCADE
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShiftHandovers')
BEGIN
    CREATE TABLE [ShiftHandovers] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [HandoverDate] DATE NOT NULL DEFAULT CAST(GETDATE() AS DATE),
        [FromShift] INT NOT NULL DEFAULT 1,
        [ToShift] INT NOT NULL DEFAULT 2,
        [FromSpecialistId] INT NULL,
        [ToSpecialistId] INT NULL,
        [Summary] NVARCHAR(4000) NOT NULL,
        [PendingTasks] NVARCHAR(4000) NULL,
        [ImportantNotes] NVARCHAR(4000) NULL,
        [EscalationItems] NVARCHAR(4000) NULL,
        [TotalTicketsHandled] INT NOT NULL DEFAULT 0,
        [PendingTicketCount] INT NOT NULL DEFAULT 0,
        [IsAcknowledged] BIT NOT NULL DEFAULT 0,
        [AcknowledgedDate] DATETIME2 NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_ShiftHandovers_FromSpecialist FOREIGN KEY ([FromSpecialistId])
            REFERENCES [Users]([Id]) ON DELETE SET NULL,
        CONSTRAINT FK_ShiftHandovers_ToSpecialist FOREIGN KEY ([ToSpecialistId])
            REFERENCES [Users]([Id]) ON DELETE SET NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ShiftHandoverTickets')
BEGIN
    CREATE TABLE [ShiftHandoverTickets] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [ShiftHandoverId] INT NOT NULL,
        [TicketId] INT NOT NULL,
        [HandoverNotes] NVARCHAR(4000) NULL,
        [IsPending] BIT NOT NULL DEFAULT 1,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        CONSTRAINT FK_SHT_Handover FOREIGN KEY ([ShiftHandoverId])
            REFERENCES [ShiftHandovers]([Id]) ON DELETE CASCADE,
        CONSTRAINT FK_SHT_Ticket FOREIGN KEY ([TicketId])
            REFERENCES [Tickets]([Id])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AuditLogs')
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] BIGINT IDENTITY(1,1) PRIMARY KEY,
        [Timestamp] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UserId] NVARCHAR(200) NULL,
        [Username] NVARCHAR(200) NULL,
        [Action] NVARCHAR(200) NOT NULL,
        [EntityName] NVARCHAR(200) NULL,
        [EntityId] NVARCHAR(100) NULL,
        [OldValues] NVARCHAR(MAX) NULL,
        [NewValues] NVARCHAR(MAX) NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [MachineName] NVARCHAR(200) NULL,
        [Details] NVARCHAR(MAX) NULL
    );

    CREATE INDEX IX_AuditLogs_Timestamp ON [AuditLogs]([Timestamp]);
    CREATE INDEX IX_AuditLogs_Action ON [AuditLogs]([Action]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Settings')
BEGIN
    CREATE TABLE [Settings] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Key] NVARCHAR(200) NOT NULL,
        [Value] NVARCHAR(4000) NOT NULL,
        [Description] NVARCHAR(1000) NULL,
        [GroupName] NVARCHAR(200) NULL,
        [DataType] NVARCHAR(50) NOT NULL DEFAULT 'string',
        [IsEncrypted] BIT NOT NULL DEFAULT 0,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0,
        CONSTRAINT UQ_Settings_Key UNIQUE ([Key])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdComputers')
BEGIN
    CREATE TABLE [AdComputers] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [ComputerName] NVARCHAR(200) NOT NULL,
        [DistinguishedName] NVARCHAR(1000) NULL,
        [OperatingSystem] NVARCHAR(200) NULL,
        [IpAddress] NVARCHAR(50) NULL,
        [Description] NVARCHAR(500) NULL,
        [ManagedBy] NVARCHAR(200) NULL,
        [OuPath] NVARCHAR(1000) NULL,
        [IsEnabled] BIT NOT NULL DEFAULT 1,
        [LastLogon] DATETIME2 NULL,
        [AdSid] NVARCHAR(200) NULL,
        [LastSyncDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [WhenCreated] NVARCHAR(100) NULL,
        [WhenChanged] NVARCHAR(100) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );

    CREATE INDEX IX_AdComputers_Name ON [AdComputers]([ComputerName]);
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'AdUsers')
BEGIN
    CREATE TABLE [AdUsers] (
        [Id] INT IDENTITY(1,1) PRIMARY KEY,
        [Username] NVARCHAR(100) NOT NULL,
        [DisplayName] NVARCHAR(200) NOT NULL,
        [Email] NVARCHAR(200) NULL,
        [Phone] NVARCHAR(50) NULL,
        [Department] NVARCHAR(200) NULL,
        [Title] NVARCHAR(200) NULL,
        [DistinguishedName] NVARCHAR(1000) NULL,
        [OuPath] NVARCHAR(1000) NULL,
        [IsEnabled] BIT NOT NULL DEFAULT 1,
        [LastLogon] DATETIME2 NULL,
        [AdSid] NVARCHAR(200) NULL,
        [LastSyncDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [WhenCreated] NVARCHAR(100) NULL,
        [WhenChanged] NVARCHAR(100) NULL,
        [Manager] NVARCHAR(200) NULL,
        [Office] NVARCHAR(200) NULL,
        [CreatedDate] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [ModifiedDate] DATETIME2 NULL,
        [CreatedBy] NVARCHAR(200) NULL,
        [ModifiedBy] NVARCHAR(200) NULL,
        [IsDeleted] BIT NOT NULL DEFAULT 0
    );

    CREATE INDEX IX_AdUsers_Username ON [AdUsers]([Username]);
END
GO

-- Insert Default Admin User
IF NOT EXISTS (SELECT * FROM [Users] WHERE [Username] = 'admin')
BEGIN
    INSERT INTO [Users] ([Username], [FullName], [Email], [Role], [IsActive], [IsFromAd], [CreatedDate])
    VALUES ('admin', 'System Administrator', 'admin@acchco.com', 1, 1, 0, GETDATE());
END
GO

-- Insert Default Settings
IF NOT EXISTS (SELECT * FROM [Settings] WHERE [Key] = 'CompanyName')
BEGIN
    INSERT INTO [Settings] ([Key], [Value], [Description], [GroupName], [DataType], [CreatedDate])
    VALUES
        ('CompanyName', 'Alexandria Container & Cargo Handling Company', 'Company Name', 'General', 'string', GETDATE()),
        ('CompanyNameAr', 'شركة الأسكندرية لتداول الحاويات و البضائع', 'Company Name (Arabic)', 'General', 'string', GETDATE()),
        ('CompanyShortName', 'ACCHCO', 'Company Short Name', 'General', 'string', GETDATE()),
        ('DepartmentName', 'End User Support Department', 'Department Name', 'General', 'string', GETDATE()),
        ('DepartmentNameAr', 'إدارة دعم المستخدمين للأجهزة', 'Department Name (Arabic)', 'General', 'string', GETDATE()),
        ('AppTitle', 'ACCHCO EUSMS - نظام إدارة دعم المستخدمين للأجهزة', 'Application Title', 'General', 'string', GETDATE()),
        ('CompanyPhone', '03/4800633 - 03/4800634 - 03/4835085', 'Company Phone', 'General', 'string', GETDATE()),
        ('CompanyAddress', 'Quay 23 Port of Alexandria - Alexandria - Egypt', 'Company Address', 'General', 'string', GETDATE()),
        ('CompanyAddressAr', 'رصيف 23 ميناء الإسكندرية - الإسكندرية - مصر', 'Company Address (Arabic)', 'General', 'string', GETDATE()),
        ('CompanyWebsite', 'https://alexcont.com', 'Company Website', 'General', 'string', GETDATE()),
        ('TicketPrefix', 'TK', 'Ticket Number Prefix', 'Tickets', 'string', GETDATE()),
        ('DefaultPriority', 'Medium', 'Default Ticket Priority', 'Tickets', 'string', GETDATE()),
        ('DefaultShift', 'أحمر', 'Default Shift', 'Tickets', 'string', GETDATE()),
        ('AdSyncEnabled', 'true', 'Enable AD Synchronization', 'Active Directory', 'string', GETDATE()),
        ('AdDomain', 'dct.local', 'AD Domain Name', 'Active Directory', 'string', GETDATE()),
        ('AdServer', '172.17.50.12', 'AD Domain Controller Server', 'Active Directory', 'string', GETDATE()),
        ('BackupPath', 'C:\EUSMS_Backups', 'Backup Directory Path', 'Backup', 'string', GETDATE()),
        ('MaxFileSize', '10', 'Max Upload File Size (MB)', 'General', 'string', GETDATE()),
        ('PrimaryColor', '#FF1565C0', 'Primary Theme Color', 'Appearance', 'string', GETDATE()),
        ('AccentColor', '#FF0D47A1', 'Accent Theme Color', 'Appearance', 'string', GETDATE());
END
GO

PRINT 'ACCHCO EUSMS Database initialized successfully!';
PRINT 'Database: ACCHCO_EUSMS';
PRINT 'Server: ' + @@SERVERNAME;
PRINT 'Date: ' + CAST(GETDATE() AS NVARCHAR(50));
GO
