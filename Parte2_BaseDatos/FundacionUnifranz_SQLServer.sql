-- ============================================================================
-- SCRIPT T-SQL NORMALIZADO (3FN) PARA MICROSOFT SQL SERVER (SSMS)
-- Base de Datos: FundacionUnifranzDB (10 Tablas Relacionales Conectadas por FK)
-- ============================================================================

IF DB_ID(N'FundacionUnifranzDB') IS NULL
BEGIN
    CREATE DATABASE [FundacionUnifranzDB];
END
GO

USE [FundacionUnifranzDB];
GO

-- ----------------------------------------------------------------------------
-- 1. TABLA dbo.Roles (Define comportamiento diferenciado: Admin / Administrativo / Estudiante)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Roles', N'U') IS NULL
CREATE TABLE dbo.Roles (
    Id          INT IDENTITY(1,1) PRIMARY KEY,
    Nombre      NVARCHAR(40)  NOT NULL UNIQUE,
    Descripcion NVARCHAR(200) NOT NULL
);
GO

-- ----------------------------------------------------------------------------
-- 2. TABLA dbo.Sedes (Catalogo maestro relacionado por SedeId a todo el sistema)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Sedes', N'U') IS NULL
CREATE TABLE dbo.Sedes (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    Nombre    NVARCHAR(60)  NOT NULL UNIQUE,
    Ciudad    NVARCHAR(60)  NOT NULL,
    Direccion NVARCHAR(180) NOT NULL,
    Activa    BIT           NOT NULL DEFAULT 1
);
GO

-- ----------------------------------------------------------------------------
-- 3. TABLA dbo.Usuarios (Credenciales de acceso: FK -> Roles, FK -> Sedes)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NULL
CREATE TABLE dbo.Usuarios (
    Id             INT IDENTITY(1,1) PRIMARY KEY,
    Username       NVARCHAR(60)  NOT NULL UNIQUE,
    PasswordHash   NVARCHAR(255) NOT NULL,
    NombreCompleto NVARCHAR(120) NOT NULL,
    Correo         NVARCHAR(150) NOT NULL UNIQUE,
    RolId          INT           NOT NULL,
    SedeId         INT           NOT NULL,
    Activo         BIT           NOT NULL DEFAULT 1,
    UltimoAcceso   NVARCHAR(40)  NOT NULL DEFAULT N'Sin accesos previos',
    CONSTRAINT FK_Usuarios_Roles FOREIGN KEY (RolId) REFERENCES dbo.Roles(Id),
    CONSTRAINT FK_Usuarios_Sedes FOREIGN KEY (SedeId) REFERENCES dbo.Sedes(Id)
);
GO

-- ----------------------------------------------------------------------------
-- 4. TABLA dbo.Administrativos (Personal y coordinadores: FK -> Usuarios, FK -> Sedes)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Administrativos', N'U') IS NULL
CREATE TABLE dbo.Administrativos (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId INT           NOT NULL UNIQUE,
    SedeId    INT           NOT NULL,
    Nombres   NVARCHAR(80)  NOT NULL,
    Apellidos NVARCHAR(80)  NOT NULL,
    CI        NVARCHAR(25)  NOT NULL UNIQUE,
    Cargo     NVARCHAR(80)  NOT NULL,
    Telefono  NVARCHAR(30)  NOT NULL,
    CONSTRAINT FK_Administrativos_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id),
    CONSTRAINT FK_Administrativos_Sedes    FOREIGN KEY (SedeId)    REFERENCES dbo.Sedes(Id)
);
GO

-- ----------------------------------------------------------------------------
-- 5. TABLA dbo.Estudiantes (Beneficiarios y postulantes: FK -> Usuarios, FK -> Sedes)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Estudiantes', N'U') IS NULL
CREATE TABLE dbo.Estudiantes (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId        INT           NOT NULL UNIQUE,
    SedeId           INT           NOT NULL,
    Nombres          NVARCHAR(80)  NOT NULL,
    Apellidos        NVARCHAR(80)  NOT NULL,
    CI               NVARCHAR(25)  NOT NULL UNIQUE,
    CodigoEstudiante NVARCHAR(30)  NOT NULL UNIQUE,
    Carrera          NVARCHAR(100) NOT NULL,
    Semestre         INT           NOT NULL CHECK (Semestre BETWEEN 1 AND 12),
    Promedio         DECIMAL(5,2)  NOT NULL CHECK (Promedio BETWEEN 0 AND 100),
    Telefono         NVARCHAR(30)  NOT NULL,
    CONSTRAINT FK_Estudiantes_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id),
    CONSTRAINT FK_Estudiantes_Sedes    FOREIGN KEY (SedeId)    REFERENCES dbo.Sedes(Id)
);
GO

-- ----------------------------------------------------------------------------
-- 6. TABLA dbo.Proyectos (Programas y convocatorias de becas disponibles)
--    Relacionada directamente por SedeId -> dbo.Sedes(Id) y AdministrativoId -> dbo.Administrativos(Id)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Proyectos', N'U') IS NULL
CREATE TABLE dbo.Proyectos (
    Id                    INT IDENTITY(1,1) PRIMARY KEY,
    Codigo                NVARCHAR(30)  NOT NULL UNIQUE,
    Nombre                NVARCHAR(150) NOT NULL,
    Categoria             NVARCHAR(80)  NOT NULL,
    SedeId                INT           NOT NULL,
    AdministrativoId      INT           NOT NULL,
    FechaInicio           DATE          NOT NULL,
    FechaFin              DATE          NOT NULL,
    BeneficiariosMeta     INT           NOT NULL DEFAULT 50,
    BeneficiariosActuales INT           NOT NULL DEFAULT 0,
    PresupuestoBs         DECIMAL(18,2) NOT NULL DEFAULT 0,
    EjecutadoBs           DECIMAL(18,2) NOT NULL DEFAULT 0,
    Estado                NVARCHAR(40)  NOT NULL,
    Descripcion           NVARCHAR(500) NOT NULL,
    CONSTRAINT FK_Proyectos_Sedes           FOREIGN KEY (SedeId)           REFERENCES dbo.Sedes(Id),
    CONSTRAINT FK_Proyectos_Administrativos FOREIGN KEY (AdministrativoId) REFERENCES dbo.Administrativos(Id)
);
GO

-- ----------------------------------------------------------------------------
-- 7. TABLA dbo.BecasOtorgadas (Postulaciones y becas otorgadas a estudiantes)
--    Relaciona Proyectos <-> Estudiantes <-> Administrativos (quien aprueba)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.BecasOtorgadas', N'U') IS NULL
CREATE TABLE dbo.BecasOtorgadas (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    Codigo           NVARCHAR(30)  NOT NULL UNIQUE,
    ProyectoId       INT           NOT NULL,
    EstudianteId     INT           NOT NULL,
    AdministrativoId INT           NULL,
    PorcentajeBeca   INT           NOT NULL CHECK (PorcentajeBeca BETWEEN 10 AND 100),
    MontoAsignadoBs  DECIMAL(18,2) NOT NULL DEFAULT 0,
    FechaSolicitud   DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    Estado           NVARCHAR(30)  NOT NULL DEFAULT N'Pendiente',
    Observacion      NVARCHAR(350) NOT NULL DEFAULT N'',
    CONSTRAINT FK_Becas_Proyectos       FOREIGN KEY (ProyectoId)       REFERENCES dbo.Proyectos(Id),
    CONSTRAINT FK_Becas_Estudiantes     FOREIGN KEY (EstudianteId)     REFERENCES dbo.Estudiantes(Id),
    CONSTRAINT FK_Becas_Administrativos FOREIGN KEY (AdministrativoId) REFERENCES dbo.Administrativos(Id)
);
GO

-- ----------------------------------------------------------------------------
-- 8. TABLA dbo.Actividades (Eventos, ferias, brigadas y jornadas ejecutadas)
--    Relacionada por ProyectoId, SedeId y AdministrativoId
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.Actividades', N'U') IS NULL
CREATE TABLE dbo.Actividades (
    Id               INT IDENTITY(1,1) PRIMARY KEY,
    Codigo           NVARCHAR(20)  NOT NULL UNIQUE,
    ProyectoId       INT           NOT NULL,
    SedeId           INT           NOT NULL,
    AdministrativoId INT           NOT NULL,
    Titulo           NVARCHAR(160) NOT NULL,
    Beneficiarios    INT           NOT NULL DEFAULT 0,
    Fecha            DATE          NOT NULL,
    Estado           NVARCHAR(40)  NOT NULL,
    EsEventoDelMes   BIT           NOT NULL DEFAULT 0,
    Descripcion      NVARCHAR(500) NOT NULL,
    CONSTRAINT FK_Actividades_Proyectos       FOREIGN KEY (ProyectoId)       REFERENCES dbo.Proyectos(Id),
    CONSTRAINT FK_Actividades_Sedes           FOREIGN KEY (SedeId)           REFERENCES dbo.Sedes(Id),
    CONSTRAINT FK_Actividades_Administrativos FOREIGN KEY (AdministrativoId) REFERENCES dbo.Administrativos(Id)
);
GO

-- ----------------------------------------------------------------------------
-- 9. TABLA dbo.MovimientosFinancieros (Modulo de Finanzas: Ingresos y Desembolsos)
--    Relacionada por ProyectoId, BecaId (opcional) y AdministrativoId
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.MovimientosFinancieros', N'U') IS NULL
CREATE TABLE dbo.MovimientosFinancieros (
    Id                INT IDENTITY(1,1) PRIMARY KEY,
    NumeroComprobante NVARCHAR(40)  NOT NULL UNIQUE,
    ProyectoId        INT           NOT NULL,
    BecaId            INT           NULL,
    AdministrativoId  INT           NOT NULL,
    Tipo              NVARCHAR(20)  NOT NULL CHECK (Tipo IN (N'Ingreso', N'Egreso')),
    Concepto          NVARCHAR(240) NOT NULL,
    MontoBs           DECIMAL(18,2) NOT NULL CHECK (MontoBs > 0),
    FechaMovimiento   DATE          NOT NULL DEFAULT CAST(GETDATE() AS DATE),
    CONSTRAINT FK_Finanzas_Proyectos       FOREIGN KEY (ProyectoId)       REFERENCES dbo.Proyectos(Id),
    CONSTRAINT FK_Finanzas_Becas           FOREIGN KEY (BecaId)           REFERENCES dbo.BecasOtorgadas(Id),
    CONSTRAINT FK_Finanzas_Administrativos FOREIGN KEY (AdministrativoId) REFERENCES dbo.Administrativos(Id)
);
GO

-- ----------------------------------------------------------------------------
-- 10. TABLA dbo.BitacoraAuditoria (Historial de accesos y acciones del sistema)
-- ----------------------------------------------------------------------------
IF OBJECT_ID(N'dbo.BitacoraAuditoria', N'U') IS NULL
CREATE TABLE dbo.BitacoraAuditoria (
    Id        INT IDENTITY(1,1) PRIMARY KEY,
    UsuarioId INT           NULL,
    Accion    NVARCHAR(80)  NOT NULL,
    Modulo    NVARCHAR(60)  NOT NULL,
    Detalle   NVARCHAR(400) NOT NULL,
    FechaHora DATETIME2     NOT NULL DEFAULT SYSDATETIME(),
    CONSTRAINT FK_Bitacora_Usuarios FOREIGN KEY (UsuarioId) REFERENCES dbo.Usuarios(Id) ON DELETE SET NULL
);
GO

-- ----------------------------------------------------------------------------
-- DATOS SEMILLA BASE (Roles y Sedes)
-- Nota: Usuarios (con PBKDF2), Administrativos, Estudiantes, Proyectos, Becas,
-- Actividades y MovimientosFinancieros se siembran automaticamente al arrancar
-- DatabaseManager.cs para garantizar contraseñas cifradas validas.
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT 1 FROM dbo.Roles)
BEGIN
    INSERT INTO dbo.Roles (Nombre, Descripcion) VALUES
    (N'Administrativo', N'Gestión integral del sistema, usuarios, proyectos, becas, finanzas y reportes.'),
    (N'Estudiante',     N'Acceso al portal estudiantil para ver convocatorias y postular a becas.');
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.Sedes)
BEGIN
    INSERT INTO dbo.Sedes (Nombre, Ciudad, Direccion, Activa) VALUES
    (N'La Paz',     N'La Paz',     N'Av. Héroes del Acre N° 1855, San Pedro', 1),
    (N'Cochabamba', N'Cochabamba', N'Av. Villarroel esq. c. Portales N° 359', 1),
    (N'Santa Cruz', N'Santa Cruz', N'Av. Busch esq. 2do Anillo N° 1113',      1),
    (N'El Alto',    N'El Alto',    N'Av. del Aeropuerto Internacional N° 1015', 1);
END
GO

SELECT N'FundacionUnifranzDB (10 tablas relacionales 3FN) lista para usar.' AS Estado;
GO
