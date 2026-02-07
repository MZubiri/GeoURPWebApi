/*
    GeoURP Web API - Script completo de base de datos + datos de prueba
    Motor objetivo: SQL Server (2019+)
*/

SET NOCOUNT ON;

IF DB_ID('GeoURPDb') IS NULL
BEGIN
    CREATE DATABASE GeoURPDb;
END
GO

USE GeoURPDb;
GO

/* =========================================================
   LIMPIEZA (idempotente para rehacer script)
   ========================================================= */
IF OBJECT_ID('dbo.UserRoles', 'U') IS NOT NULL DROP TABLE dbo.UserRoles;
IF OBJECT_ID('dbo.ContactMessages', 'U') IS NOT NULL DROP TABLE dbo.ContactMessages;
IF OBJECT_ID('dbo.Books', 'U') IS NOT NULL DROP TABLE dbo.Books;
IF OBJECT_ID('dbo.BookCategories', 'U') IS NOT NULL DROP TABLE dbo.BookCategories;
IF OBJECT_ID('dbo.Exams', 'U') IS NOT NULL DROP TABLE dbo.Exams;
IF OBJECT_ID('dbo.ExamCategories', 'U') IS NOT NULL DROP TABLE dbo.ExamCategories;
IF OBJECT_ID('dbo.Researches', 'U') IS NOT NULL DROP TABLE dbo.Researches;
IF OBJECT_ID('dbo.ResearchCategories', 'U') IS NOT NULL DROP TABLE dbo.ResearchCategories;
IF OBJECT_ID('dbo.Events', 'U') IS NOT NULL DROP TABLE dbo.Events;
IF OBJECT_ID('dbo.BoardMembers', 'U') IS NOT NULL DROP TABLE dbo.BoardMembers;
IF OBJECT_ID('dbo.Users', 'U') IS NOT NULL DROP TABLE dbo.Users;
IF OBJECT_ID('dbo.Roles', 'U') IS NOT NULL DROP TABLE dbo.Roles;
GO

/* =========================================================
   TABLAS
   ========================================================= */
CREATE TABLE dbo.Roles
(
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(250) NOT NULL,
    IsActive    BIT NOT NULL CONSTRAINT DF_Roles_IsActive DEFAULT (1),
    CONSTRAINT UQ_Roles_Name UNIQUE (Name)
);
GO

CREATE TABLE dbo.Users
(
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    [Name]    NVARCHAR(150) NOT NULL,
    Email     NVARCHAR(150) NOT NULL,
    [Password] NVARCHAR(200) NOT NULL,
    IsActive  BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CONSTRAINT UQ_Users_Email UNIQUE (Email)
);
GO

CREATE TABLE dbo.UserRoles
(
    UserId INT NOT NULL,
    RoleId INT NOT NULL,
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id) ON DELETE CASCADE,
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES dbo.Roles(Id)
);
GO

CREATE TABLE dbo.BoardMembers
(
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    FullName  NVARCHAR(200) NOT NULL,
    Position  NVARCHAR(120) NOT NULL,
    PhotoUrl  NVARCHAR(500) NOT NULL,
    Bio       NVARCHAR(1000) NOT NULL,
    SortOrder INT NOT NULL,
    IsActive  BIT NOT NULL CONSTRAINT DF_BoardMembers_IsActive DEFAULT (1)
);
GO

CREATE TABLE dbo.Events
(
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Title       NVARCHAR(200) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    StartAt     DATETIME2(0) NOT NULL,
    EndAt       DATETIME2(0) NOT NULL,
    [Location]  NVARCHAR(200) NOT NULL,
    IsPublic    BIT NOT NULL CONSTRAINT DF_Events_IsPublic DEFAULT (1),
    CONSTRAINT CK_Events_StartEnd CHECK (EndAt >= StartAt)
);
GO

CREATE TABLE dbo.ResearchCategories
(
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    [Name]   NVARCHAR(120) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_ResearchCategories_IsActive DEFAULT (1),
    CONSTRAINT UQ_ResearchCategories_Name UNIQUE ([Name])
);
GO

CREATE TABLE dbo.Researches
(
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Title       NVARCHAR(250) NOT NULL,
    Summary     NVARCHAR(2000) NOT NULL,
    FileUrl     NVARCHAR(500) NOT NULL,
    CategoryId  INT NOT NULL,
    PublishedAt DATETIME2(0) NOT NULL,
    IsActive    BIT NOT NULL CONSTRAINT DF_Researches_IsActive DEFAULT (1),
    CONSTRAINT FK_Researches_ResearchCategories FOREIGN KEY (CategoryId) REFERENCES dbo.ResearchCategories(Id)
);
GO

CREATE TABLE dbo.ExamCategories
(
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    [Name]   NVARCHAR(120) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_ExamCategories_IsActive DEFAULT (1),
    CONSTRAINT UQ_ExamCategories_Name UNIQUE ([Name])
);
GO

CREATE TABLE dbo.Exams
(
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Title       NVARCHAR(250) NOT NULL,
    Description NVARCHAR(1000) NOT NULL,
    [Date]      DATETIME2(0) NOT NULL,
    FileUrl     NVARCHAR(500) NOT NULL,
    CategoryId  INT NOT NULL,
    IsActive    BIT NOT NULL CONSTRAINT DF_Exams_IsActive DEFAULT (1),
    CONSTRAINT FK_Exams_ExamCategories FOREIGN KEY (CategoryId) REFERENCES dbo.ExamCategories(Id)
);
GO

CREATE TABLE dbo.BookCategories
(
    Id       INT IDENTITY(1,1) PRIMARY KEY,
    [Name]   NVARCHAR(120) NOT NULL,
    IsActive BIT NOT NULL CONSTRAINT DF_BookCategories_IsActive DEFAULT (1),
    CONSTRAINT UQ_BookCategories_Name UNIQUE ([Name])
);
GO

CREATE TABLE dbo.Books
(
    Id         INT IDENTITY(1,1) PRIMARY KEY,
    Title      NVARCHAR(250) NOT NULL,
    Author     NVARCHAR(180) NOT NULL,
    Editorial  NVARCHAR(180) NOT NULL,
    [Year]     INT NOT NULL,
    FileUrl    NVARCHAR(500) NOT NULL,
    CategoryId INT NOT NULL,
    IsActive   BIT NOT NULL CONSTRAINT DF_Books_IsActive DEFAULT (1),
    CONSTRAINT FK_Books_BookCategories FOREIGN KEY (CategoryId) REFERENCES dbo.BookCategories(Id),
    CONSTRAINT CK_Books_Year CHECK ([Year] BETWEEN 1900 AND 2100)
);
GO

CREATE TABLE dbo.ContactMessages
(
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    FullName  NVARCHAR(180) NOT NULL,
    Email     NVARCHAR(180) NOT NULL,
    Subject   NVARCHAR(250) NOT NULL,
    [Message] NVARCHAR(3000) NOT NULL,
    CreatedAt DATETIME2(0) NOT NULL CONSTRAINT DF_ContactMessages_CreatedAt DEFAULT (SYSUTCDATETIME())
);
GO

/* =========================================================
   DATOS DE PRUEBA
   ========================================================= */
SET IDENTITY_INSERT dbo.Roles ON;
INSERT INTO dbo.Roles (Id, Name, Description, IsActive)
VALUES
(1, N'Admin', N'Administrador general', 1),
(2, N'Editor', N'Editor de contenido', 1),
(3, N'Invitado', N'Usuario con acceso limitado', 1);
SET IDENTITY_INSERT dbo.Roles OFF;
GO

SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users (Id, [Name], Email, [Password], IsActive)
VALUES
(1, N'Administrador', N'admin@geourp.local', N'Admin123*', 1),
(2, N'Editor', N'editor@geourp.local', N'Editor123*', 1),
(3, N'Ana Torres', N'ana.torres@geourp.local', N'Test123*', 1),
(4, N'Carlos Díaz', N'carlos.diaz@geourp.local', N'Test123*', 0);
SET IDENTITY_INSERT dbo.Users OFF;
GO

INSERT INTO dbo.UserRoles (UserId, RoleId)
VALUES
(1, 1),
(2, 2),
(3, 2),
(3, 3),
(4, 3);
GO

SET IDENTITY_INSERT dbo.BoardMembers ON;
INSERT INTO dbo.BoardMembers (Id, FullName, Position, PhotoUrl, Bio, SortOrder, IsActive)
VALUES
(1, N'Dra. Carmen Ruiz', N'Presidenta', N'https://images.unsplash.com/photo-1438761681033-6461ffad8d80', N'Especialista en gestión universitaria.', 1, 1),
(2, N'Mg. Luis Herrera', N'Secretario', N'https://images.unsplash.com/photo-1500648767791-00dcc994a43e', N'Coordinador académico y de calidad.', 2, 1),
(3, N'Dr. Pablo Mejía', N'Vocal', N'https://images.unsplash.com/photo-1472099645785-5658abf4ff4e', N'Investigador en planeamiento territorial.', 3, 1);
SET IDENTITY_INSERT dbo.BoardMembers OFF;
GO

SET IDENTITY_INSERT dbo.Events ON;
INSERT INTO dbo.Events (Id, Title, Description, StartAt, EndAt, [Location], IsPublic)
VALUES
(1, N'Seminario de investigación', N'Presentación de avances de tesis.', DATEADD(DAY, 5, SYSUTCDATETIME()), DATEADD(HOUR, 2, DATEADD(DAY, 5, SYSUTCDATETIME())), N'Auditorio Principal', 1),
(2, N'Taller de SIG aplicado', N'Manejo de herramientas GIS para docentes.', DATEADD(DAY, 12, SYSUTCDATETIME()), DATEADD(HOUR, 3, DATEADD(DAY, 12, SYSUTCDATETIME())), N'Sala de Cómputo 2', 1),
(3, N'Reunión interna de comité', N'Planificación semestral.', DATEADD(DAY, 3, SYSUTCDATETIME()), DATEADD(HOUR, 1, DATEADD(DAY, 3, SYSUTCDATETIME())), N'Sala de Consejo', 0);
SET IDENTITY_INSERT dbo.Events OFF;
GO

SET IDENTITY_INSERT dbo.ResearchCategories ON;
INSERT INTO dbo.ResearchCategories (Id, [Name], IsActive)
VALUES
(1, N'Metodología', 1),
(2, N'Planificación urbana', 1),
(3, N'Geomática', 1);
SET IDENTITY_INSERT dbo.ResearchCategories OFF;
GO

SET IDENTITY_INSERT dbo.Researches ON;
INSERT INTO dbo.Researches (Id, Title, Summary, FileUrl, CategoryId, PublishedAt, IsActive)
VALUES
(1, N'Impacto urbano', N'Estudio de crecimiento urbano en ciudades intermedias.', N'https://example.com/research-impacto-urbano.pdf', 2, DATEADD(MONTH, -2, SYSUTCDATETIME()), 1),
(2, N'Métodos de análisis espacial', N'Aplicación de estadística espacial en zonificación.', N'https://example.com/research-analisis-espacial.pdf', 1, DATEADD(MONTH, -1, SYSUTCDATETIME()), 1),
(3, N'Detección de cambios con imágenes satelitales', N'Comparación temporal de cobertura del suelo.', N'https://example.com/research-cambios-satelitales.pdf', 3, DATEADD(DAY, -20, SYSUTCDATETIME()), 1);
SET IDENTITY_INSERT dbo.Researches OFF;
GO

SET IDENTITY_INSERT dbo.ExamCategories ON;
INSERT INTO dbo.ExamCategories (Id, [Name], IsActive)
VALUES
(1, N'Parcial', 1),
(2, N'Final', 1),
(3, N'Sustitutorio', 1);
SET IDENTITY_INSERT dbo.ExamCategories OFF;
GO

SET IDENTITY_INSERT dbo.Exams ON;
INSERT INTO dbo.Exams (Id, Title, Description, [Date], FileUrl, CategoryId, IsActive)
VALUES
(1, N'Examen parcial 1', N'Evaluación de cartografía.', DATEADD(DAY, 10, SYSUTCDATETIME()), N'https://example.com/exam-parcial-1.pdf', 1, 1),
(2, N'Examen final SIG', N'Evaluación final de sistemas de información geográfica.', DATEADD(DAY, 30, SYSUTCDATETIME()), N'https://example.com/exam-final-sig.pdf', 2, 1),
(3, N'Examen sustitutorio de metodología', N'Recuperación de metodología de investigación.', DATEADD(DAY, 45, SYSUTCDATETIME()), N'https://example.com/exam-susti-metodologia.pdf', 3, 1);
SET IDENTITY_INSERT dbo.Exams OFF;
GO

SET IDENTITY_INSERT dbo.BookCategories ON;
INSERT INTO dbo.BookCategories (Id, [Name], IsActive)
VALUES
(1, N'Geografía', 1),
(2, N'SIG', 1),
(3, N'Investigación', 1);
SET IDENTITY_INSERT dbo.BookCategories OFF;
GO

SET IDENTITY_INSERT dbo.Books ON;
INSERT INTO dbo.Books (Id, Title, Author, Editorial, [Year], FileUrl, CategoryId, IsActive)
VALUES
(1, N'Introducción a SIG', N'Juan Pérez', N'GeoPress', 2024, N'https://example.com/book-intro-sig.pdf', 2, 1),
(2, N'Geografía urbana aplicada', N'María López', N'Andes Editorial', 2022, N'https://example.com/book-geografia-urbana.pdf', 1, 1),
(3, N'Diseño de proyectos de investigación', N'Raúl Mendoza', N'Académica Perú', 2023, N'https://example.com/book-proyectos-investigacion.pdf', 3, 1);
SET IDENTITY_INSERT dbo.Books OFF;
GO

SET IDENTITY_INSERT dbo.ContactMessages ON;
INSERT INTO dbo.ContactMessages (Id, FullName, Email, Subject, [Message], CreatedAt)
VALUES
(1, N'Valeria Castañeda', N'valeria.castaneda@email.test', N'Consulta sobre admisión', N'¿Cuándo inicia el proceso de admisión 2026?', DATEADD(DAY, -5, SYSUTCDATETIME())),
(2, N'Ricardo León', N'ricardo.leon@email.test', N'Solicitud de información de cursos', N'Necesito información sobre los cursos de SIG para externos.', DATEADD(DAY, -2, SYSUTCDATETIME()));
SET IDENTITY_INSERT dbo.ContactMessages OFF;
GO

/* =========================================================
   CONSULTAS RÁPIDAS DE VERIFICACIÓN
   ========================================================= */
SELECT 'Roles' AS [Tabla], COUNT(1) AS Total FROM dbo.Roles
UNION ALL SELECT 'Users', COUNT(1) FROM dbo.Users
UNION ALL SELECT 'UserRoles', COUNT(1) FROM dbo.UserRoles
UNION ALL SELECT 'BoardMembers', COUNT(1) FROM dbo.BoardMembers
UNION ALL SELECT 'Events', COUNT(1) FROM dbo.Events
UNION ALL SELECT 'ResearchCategories', COUNT(1) FROM dbo.ResearchCategories
UNION ALL SELECT 'Researches', COUNT(1) FROM dbo.Researches
UNION ALL SELECT 'ExamCategories', COUNT(1) FROM dbo.ExamCategories
UNION ALL SELECT 'Exams', COUNT(1) FROM dbo.Exams
UNION ALL SELECT 'BookCategories', COUNT(1) FROM dbo.BookCategories
UNION ALL SELECT 'Books', COUNT(1) FROM dbo.Books
UNION ALL SELECT 'ContactMessages', COUNT(1) FROM dbo.ContactMessages;
GO
