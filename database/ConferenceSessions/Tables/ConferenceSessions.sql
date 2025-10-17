CREATE TABLE [dbo].[ConferenceSessions]
(
    [SessionId] INT NOT NULL PRIMARY KEY,
    [Title] NVARCHAR(200) NOT NULL,
    [Speaker] NVARCHAR(100) NOT NULL,
    [Level] NVARCHAR(50) NOT NULL
);
