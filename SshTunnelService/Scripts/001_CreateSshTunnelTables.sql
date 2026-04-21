-- SSH Tunnel Service Database Schema
-- Each SshTunnel record represents an independent SSH connection to a DIFFERENT server.
-- Each tunnel has its own host, credentials, and set of port forwards.
-- Tunnels run in parallel and restart independently of each other.

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SshTunnel')
BEGIN
    CREATE TABLE [dbo].[SshTunnel] (
        [Id]                    INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [Name]                  NVARCHAR(100)     NOT NULL,           -- Friendly name for this tunnel
        [Host]                  NVARCHAR(255)     NOT NULL,           -- SSH server hostname/IP
        [Port]                  INT               NOT NULL DEFAULT 22,-- SSH server port
        [Username]              NVARCHAR(100)     NOT NULL,           -- SSH login username
        [Password]              NVARCHAR(255)     NULL DEFAULT '',    -- SSH password (optional if using key)
        [PrivateKeyPath]        NVARCHAR(500)     NULL DEFAULT '',    -- Path to private key file
        [PrivateKeyPassphrase]  NVARCHAR(255)     NULL DEFAULT '',    -- Passphrase for private key
        [ReconnectDelaySeconds] INT               NOT NULL DEFAULT 10,
        [MaxReconnectAttempts]  INT               NOT NULL DEFAULT 0, -- 0 = unlimited
        [IsActive]              BIT               NOT NULL DEFAULT 1,
        [CreatedAt]             DATETIME2         NOT NULL DEFAULT GETUTCDATE(),
        [UpdatedAt]             DATETIME2         NULL
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'SshTunnelPortForward')
BEGIN
    CREATE TABLE [dbo].[SshTunnelPortForward] (
        [Id]           INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [SshTunnelId]  INT               NOT NULL,                    -- Which SSH server this forward belongs to
        [Name]         NVARCHAR(50)      NOT NULL,                    -- Friendly name for this forward
        [ForwardType]  NVARCHAR(10)      NOT NULL DEFAULT 'Local',    -- Local, Remote, Dynamic
        [BoundHost]    NVARCHAR(255)     NOT NULL DEFAULT '127.0.0.1',
        [BoundPort]    INT               NOT NULL,
        [TargetHost]   NVARCHAR(255)     NULL DEFAULT '127.0.0.1',   -- NULL for Dynamic forwards
        [TargetPort]   INT               NULL,                        -- NULL for Dynamic forwards
        [IsActive]     BIT               NOT NULL DEFAULT 1,
        [CreatedAt]    DATETIME2         NOT NULL DEFAULT GETUTCDATE(),

        CONSTRAINT [FK_SshTunnelPortForward_SshTunnel]
            FOREIGN KEY ([SshTunnelId]) REFERENCES [dbo].[SshTunnel]([Id])
            ON DELETE CASCADE
    );

    CREATE INDEX [IX_SshTunnelPortForward_Tunnel_Type_Port]
        ON [dbo].[SshTunnelPortForward] ([SshTunnelId], [ForwardType], [BoundPort]);
END
GO

-- ============================================================
-- SAMPLE DATA: 3 tunnels, each connecting to a DIFFERENT SSH server
-- ============================================================
/*
-- Tunnel 1: Production database server (SSH into prod bastion)
INSERT INTO [dbo].[SshTunnel] ([Name], [Host], [Port], [Username], [Password], [IsActive])
VALUES ('Production Bastion', 'prod-bastion.company.com', 22, 'svc-tunnel', 'P@ssw0rd!Prod', 1);

DECLARE @ProdTunnel INT = SCOPE_IDENTITY();

INSERT INTO [dbo].[SshTunnelPortForward] ([SshTunnelId], [Name], [ForwardType], [BoundHost], [BoundPort], [TargetHost], [TargetPort])
VALUES (@ProdTunnel, 'ProdPostgres', 'Local', '127.0.0.1', 15432, '10.0.1.5', 5432);

INSERT INTO [dbo].[SshTunnelPortForward] ([SshTunnelId], [Name], [ForwardType], [BoundHost], [BoundPort], [TargetHost], [TargetPort])
VALUES (@ProdTunnel, 'ProdRedis', 'Local', '127.0.0.1', 16379, '10.0.1.6', 6379);


-- Tunnel 2: Staging server (DIFFERENT SSH server)
INSERT INTO [dbo].[SshTunnel] ([Name], [Host], [Port], [Username], [Password], [IsActive])
VALUES ('Staging Server', 'staging.company.com', 2222, 'deploy', 'St@ging2024!', 1);

DECLARE @StagingTunnel INT = SCOPE_IDENTITY();

INSERT INTO [dbo].[SshTunnelPortForward] ([SshTunnelId], [Name], [ForwardType], [BoundHost], [BoundPort], [TargetHost], [TargetPort])
VALUES (@StagingTunnel, 'StagingAPI', 'Remote', '0.0.0.0', 8080, '127.0.0.1', 3000);

INSERT INTO [dbo].[SshTunnelPortForward] ([SshTunnelId], [Name], [ForwardType], [BoundHost], [BoundPort])
VALUES (@StagingTunnel, 'StagingSocks', 'Dynamic', '127.0.0.1', 1080);


-- Tunnel 3: Client VPN gateway (ANOTHER DIFFERENT SSH server)
INSERT INTO [dbo].[SshTunnel] ([Name], [Host], [Port], [Username], [Password], [IsActive])
VALUES ('Client VPN Gateway', '203.0.113.50', 1957, 'vpn-user', 'Cl!entVPN#99', 1);

DECLARE @ClientTunnel INT = SCOPE_IDENTITY();

INSERT INTO [dbo].[SshTunnelPortForward] ([SshTunnelId], [Name], [ForwardType], [BoundHost], [BoundPort], [TargetHost], [TargetPort])
VALUES (@ClientTunnel, 'ClientDB', 'Local', '127.0.0.1', 13306, '192.168.1.100', 3306);

INSERT INTO [dbo].[SshTunnelPortForward] ([SshTunnelId], [Name], [ForwardType], [BoundHost], [BoundPort], [TargetHost], [TargetPort])
VALUES (@ClientTunnel, 'ClientWeb', 'Local', '127.0.0.1', 18080, '192.168.1.101', 80);
*/

-- ============================================================
-- MANAGEMENT QUERIES
-- ============================================================
/*
-- View all tunnels and their forwards:
SELECT t.Id, t.Name, t.Host, t.Port, t.IsActive,
       pf.Name AS ForwardName, pf.ForwardType, pf.BoundHost, pf.BoundPort, pf.TargetHost, pf.TargetPort, pf.IsActive AS ForwardActive
FROM SshTunnel t
LEFT JOIN SshTunnelPortForward pf ON pf.SshTunnelId = t.Id
ORDER BY t.Id, pf.ForwardType;

-- Disable a specific tunnel (stops its connection on next health check):
UPDATE SshTunnel SET IsActive = 0, UpdatedAt = GETUTCDATE() WHERE Id = 1;

-- Disable a specific forward without stopping the tunnel:
UPDATE SshTunnelPortForward SET IsActive = 0 WHERE Id = 3;

-- Re-enable a tunnel:
UPDATE SshTunnel SET IsActive = 1, UpdatedAt = GETUTCDATE() WHERE Id = 1;
*/
