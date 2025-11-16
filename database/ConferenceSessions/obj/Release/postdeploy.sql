PRINT 'Seeding ConferenceSessions data...';

IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = 'ConferenceSessions' AND schema_id = SCHEMA_ID('dbo'))
BEGIN
    RAISERROR('ConferenceSessions table must exist before seeding data.', 16, 1);
    RETURN;
END;

IF NOT EXISTS (SELECT 1 FROM [dbo].[ConferenceSessions])
BEGIN
    INSERT INTO [dbo].[ConferenceSessions] ([SessionId], [Title], [Speaker], [Level])
    VALUES
        (1, N'Azure SQL Zero-to-Hero', N'Jamie Patel', N'Beginner'),
        (2, N'Securing OIDC Pipelines', N'Linh Nguyen', N'Intermediate'),
        (3, N'Optimizing Data Workflows', N'Carlos Diaz', N'Advanced');
END;
ELSE
BEGIN
    PRINT 'ConferenceSessions already has data; skipping seed insert.';
END;
GO
