import crypto from 'node:crypto';
import { asc, desc, eq } from 'drizzle-orm';
import { db } from './index.ts';
import {
  actividades,
  administrativos,
  becasOtorgadas,
  bitacoraAuditoria,
  configuracionGlobal,
  estudiantes,
  movimientosFinancieros,
  proyectos,
  roles,
  sedes,
  usuarios,
} from './schema.ts';
import {
  INITIAL_FINANCE,
  INITIAL_PROJECTS,
  INITIAL_RECORDS,
  INITIAL_SCHOLARSHIPS,
  INITIAL_USERS,
} from '../data/seedData.ts';
import type {
  ActivityRecord,
  ActivityStatus,
  Campus,
  FinanceMovement,
  FinanceType,
  FoundationProject,
  ProjectCategory,
  ProjectStatus,
  ScholarshipAward,
  ScholarshipStatus,
  SystemUser,
  UserRole,
} from '../types/foundation.ts';

export interface GlobalInterfaceConfigRow {
  nombreInstitucion: string;
  tituloSistema: string;
  subtituloLogin: string;
  mensajeBienvenida: string;
  avisoGlobal: string;
  colorPrimario: string;
  colorSidebar: string;
  colorAcento: string;
  colorFondo: string;
  sedesActivas: string[];
  version: number;
  updatedAt: string;
}

export interface SyncedAppDataState {
  users: SystemUser[];
  projects: FoundationProject[];
  records: ActivityRecord[];
  scholarships: ScholarshipAward[];
  movements: FinanceMovement[];
  dataVersion: number;
  updatedAt: string;
}

export const DEFAULT_GLOBAL_CONFIG_ROW: GlobalInterfaceConfigRow = {
  nombreInstitucion: 'FUNDACIÓN UNIFRANZ',
  tituloSistema: 'Sistema de Gestión',
  subtituloLogin: 'Transformando comunidades con educación e impacto social',
  mensajeBienvenida: 'Bienvenido',
  avisoGlobal: '',
  colorPrimario: '#007B8F',
  colorSidebar: '#064A57',
  colorAcento: '#E65A28',
  colorFondo: '#F8FAFC',
  sedesActivas: ['La Paz', 'Cochabamba', 'Santa Cruz', 'El Alto'],
  version: 1,
  updatedAt: new Date().toISOString(),
};

const SEED_ADMIN_DETAILS: Record<
  string,
  { nombres: string; apellidos: string; ci: string; cargo: string; telefono: string }
> = {
  'adm.lpz': {
    nombres: 'Valeria',
    apellidos: 'Mendoza',
    ci: '4821901 LP',
    cargo: 'Directora Nacional de Fundación',
    telefono: '+591 71520101',
  },
  'adm.cbba': {
    nombres: 'Juan',
    apellidos: 'Pérez Morales',
    ci: '5290441 CB',
    cargo: 'Coordinador de Proyectos y Becas',
    telefono: '+591 72210402',
  },
  'adm.scz': {
    nombres: 'María',
    apellidos: 'Gómez Salazar',
    ci: '6348920 SC',
    cargo: 'Coordinadora de Voluntariado y Finanzas',
    telefono: '+591 76340903',
  },
  'adm.eal': {
    nombres: 'Roberto',
    apellidos: 'Mamani',
    ci: '6129384 LP',
    cargo: 'Coordinador Sede El Alto',
    telefono: '+591 73581204',
  },
};

export function hashPasswordPbkdf2(plainPassword: string): string {
  const salt = crypto.randomBytes(16).toString('hex');
  const derived = crypto
    .pbkdf2Sync(plainPassword, salt, 60000, 32, 'sha256')
    .toString('hex');
  return `pbkdf2_sha256$${salt}$${derived}`;
}

export function verifyPasswordPbkdf2(plainPassword: string, storedHash: string): boolean {
  if (!storedHash || !plainPassword) return false;
  const parts = storedHash.split('$');
  if (parts.length !== 3 || parts[0] !== 'pbkdf2_sha256') {
    return false;
  }
  const [, salt, expectedHex] = parts;
  const candidate = crypto.pbkdf2Sync(plainPassword, salt, 60000, 32, 'sha256');
  const expected = Buffer.from(expectedHex, 'hex');
  if (candidate.length !== expected.length) return false;
  return crypto.timingSafeEqual(candidate, expected);
}

function splitFullName(fullName: string): { nombres: string; apellidos: string } {
  const clean = fullName.replace(/^(Dra\.|Dr\.|Lic\.|Ing\.)\s+/i, '').trim();
  const tokens = clean.split(/\s+/).filter(Boolean);
  if (tokens.length <= 1) return { nombres: clean || 'Usuario', apellidos: 'UNIFRANZ' };
  return {
    nombres: tokens[0],
    apellidos: tokens.slice(1).join(' '),
  };
}

let seedPromise: Promise<void> | null = null;

export async function ensureDatabaseSeeded(): Promise<void> {
  if (seedPromise) return seedPromise;
  seedPromise = runSeedIfNeeded().catch((err) => {
    seedPromise = null;
    throw err;
  });
  return seedPromise;
}

async function runSeedIfNeeded(): Promise<void> {
  try {
    const existingRoles = await db.select().from(roles).limit(1);
    if (existingRoles.length > 0) {
      // Ensure configuracion_global exists even if roles were already seeded
      const existingCfg = await db.select().from(configuracionGlobal).limit(1);
      if (existingCfg.length === 0) {
        await db.insert(configuracionGlobal).values({
          nombreInstitucion: DEFAULT_GLOBAL_CONFIG_ROW.nombreInstitucion,
          tituloSistema: DEFAULT_GLOBAL_CONFIG_ROW.tituloSistema,
          subtituloLogin: DEFAULT_GLOBAL_CONFIG_ROW.subtituloLogin,
          mensajeBienvenida: DEFAULT_GLOBAL_CONFIG_ROW.mensajeBienvenida,
          avisoGlobal: DEFAULT_GLOBAL_CONFIG_ROW.avisoGlobal,
          colorPrimario: DEFAULT_GLOBAL_CONFIG_ROW.colorPrimario,
          colorSidebar: DEFAULT_GLOBAL_CONFIG_ROW.colorSidebar,
          colorAcento: DEFAULT_GLOBAL_CONFIG_ROW.colorAcento,
          colorFondo: DEFAULT_GLOBAL_CONFIG_ROW.colorFondo,
          sedesActivasJson: JSON.stringify(DEFAULT_GLOBAL_CONFIG_ROW.sedesActivas),
          version: 1,
          dataVersion: 1,
        });
      }
      return;
    }

    await db.transaction(async (tx) => {
      const insertedRoles = await tx
        .insert(roles)
        .values([
          {
            nombre: 'Administrativo',
            descripcion:
              'Gestión integral del sistema, usuarios, proyectos, becas, finanzas y reportes.',
          },
          {
            nombre: 'Estudiante',
            descripcion:
              'Acceso al portal estudiantil para ver convocatorias y postular a becas.',
          },
        ])
        .returning();

      const roleMap = new Map<string, number>(insertedRoles.map((r) => [r.nombre, r.id]));

      const insertedSedes = await tx
        .insert(sedes)
        .values([
          {
            nombre: 'La Paz',
            ciudad: 'La Paz',
            direccion: 'Av. Héroes del Acre N° 1855, San Pedro',
            activa: true,
          },
          {
            nombre: 'Cochabamba',
            ciudad: 'Cochabamba',
            direccion: 'Av. Villarroel esq. c. Portales N° 359',
            activa: true,
          },
          {
            nombre: 'Santa Cruz',
            ciudad: 'Santa Cruz',
            direccion: 'Av. Busch esq. 2do Anillo N° 1113',
            activa: true,
          },
          {
            nombre: 'El Alto',
            ciudad: 'El Alto',
            direccion: 'Av. del Aeropuerto Internacional N° 1015',
            activa: true,
          },
        ])
        .returning();

      const sedeMap = new Map<string, number>(insertedSedes.map((s) => [s.nombre, s.id]));
      const defaultSedeId = insertedSedes[0].id;
      const adminRoleId = roleMap.get('Administrativo') ?? insertedRoles[0].id;
      const studentRoleId = roleMap.get('Estudiante') ?? insertedRoles[1].id;

      const userExtToDbId = new Map<string, number>();
      const adminBySedeId = new Map<number, number>();
      const adminByName = new Map<string, number>();
      const studentExtToDbId = new Map<string, number>();
      let firstAdminId = 0;

      for (const u of INITIAL_USERS) {
        const sedeId = sedeMap.get(u.sede) ?? defaultSedeId;
        const rolId = u.rol === 'Administrativo' ? adminRoleId : studentRoleId;

        const [insertedUser] = await tx
          .insert(usuarios)
          .values({
            externalId: u.id,
            username: u.username,
            passwordHash: hashPasswordPbkdf2(u.password),
            nombreCompleto: u.nombreCompleto,
            correo: u.correo,
            rolId,
            sedeId,
            activo: u.activo,
            ultimoAcceso: u.ultimoAcceso,
          })
          .returning();

        userExtToDbId.set(u.id, insertedUser.id);

        if (u.rol === 'Administrativo') {
          const detail = SEED_ADMIN_DETAILS[u.username] ?? {
            ...splitFullName(u.nombreCompleto),
            ci: `CI-ADM-${insertedUser.id}`,
            cargo: 'Coordinador Institucional',
            telefono: '+591 70000000',
          };
          const [insertedAdmin] = await tx
            .insert(administrativos)
            .values({
              usuarioId: insertedUser.id,
              sedeId,
              nombres: detail.nombres,
              apellidos: detail.apellidos,
              ci: detail.ci,
              cargo: detail.cargo,
              telefono: detail.telefono,
            })
            .returning();

          if (!firstAdminId) firstAdminId = insertedAdmin.id;
          adminBySedeId.set(sedeId, insertedAdmin.id);
          adminByName.set(u.nombreCompleto.toLowerCase(), insertedAdmin.id);
          adminByName.set(
            `${detail.nombres} ${detail.apellidos}`.toLowerCase(),
            insertedAdmin.id
          );
        } else {
          const parsedName = splitFullName(u.nombreCompleto);
          const profile = u.studentProfile;
          const [insertedEst] = await tx
            .insert(estudiantes)
            .values({
              usuarioId: insertedUser.id,
              sedeId,
              nombres: parsedName.nombres,
              apellidos: parsedName.apellidos,
              ci: profile?.ci ?? `CI-EST-${insertedUser.id}`,
              codigoEstudiante: profile?.codigoEstudiante ?? `EST-2026-${insertedUser.id}`,
              carrera: profile?.carrera ?? 'Ingeniería de Sistemas',
              semestre: profile?.semestre ?? 1,
              promedio: String((profile?.promedio ?? 85).toFixed(2)),
              telefono: '+591 75000000',
            })
            .returning();

          studentExtToDbId.set(u.id, insertedEst.id);
        }
      }

      const projectExtToDbId = new Map<string, number>();

      for (const p of INITIAL_PROJECTS) {
        const sedeId = sedeMap.get(p.sede) ?? defaultSedeId;
        const adminId =
          adminByName.get(p.responsable.toLowerCase()) ??
          adminBySedeId.get(sedeId) ??
          firstAdminId;

        const [insertedProject] = await tx
          .insert(proyectos)
          .values({
            externalId: p.id,
            codigo: p.codigo,
            nombre: p.nombre,
            categoria: p.categoria,
            sedeId,
            administrativoId: adminId,
            responsableNombre: p.responsable,
            fechaInicio: p.fechaInicio,
            fechaFin: p.fechaFin,
            beneficiariosMeta: p.beneficiariosMeta,
            beneficiariosActuales: p.beneficiariosActuales,
            presupuestoBs: p.presupuestoBs.toFixed(2),
            ejecutadoBs: p.ejecutadoBs.toFixed(2),
            estado: p.estado,
            descripcion: p.descripcion,
          })
          .returning();

        projectExtToDbId.set(p.id, insertedProject.id);
      }

      const defaultProjectId = Array.from(projectExtToDbId.values())[0];
      const defaultStudentId = Array.from(studentExtToDbId.values())[0];
      const becaCodeToDbId = new Map<string, number>();

      for (const b of INITIAL_SCHOLARSHIPS) {
        const proyectoId = projectExtToDbId.get(b.projectId) ?? defaultProjectId;
        const estudianteId = studentExtToDbId.get(b.studentUserId) ?? defaultStudentId;
        const evaluadorId =
          b.estado === 'Pendiente'
            ? null
            : adminByName.get(b.evaluadorNombre.toLowerCase()) ?? firstAdminId;

        const [insertedBeca] = await tx
          .insert(becasOtorgadas)
          .values({
            externalId: b.id,
            codigo: b.codigo,
            proyectoId,
            estudianteId,
            administrativoId: evaluadorId,
            evaluadorNombre: b.evaluadorNombre,
            porcentajeBeca: b.porcentajeBeca,
            montoAsignadoBs: b.montoAsignadoBs.toFixed(2),
            fechaSolicitud: b.fechaSolicitud,
            estado: b.estado,
            observacion: b.observacion,
          })
          .returning();

        becaCodeToDbId.set(b.codigo, insertedBeca.id);
      }

      for (const r of INITIAL_RECORDS) {
        const proyectoId = projectExtToDbId.get(r.projectId) ?? defaultProjectId;
        const sedeId = sedeMap.get(r.sede) ?? defaultSedeId;
        const adminId =
          adminByName.get(r.responsable.toLowerCase()) ??
          adminBySedeId.get(sedeId) ??
          firstAdminId;

        await tx.insert(actividades).values({
          externalId: r.id,
          codigo: `ACT-${r.id.padStart(3, '0')}`,
          proyectoId,
          sedeId,
          administrativoId: adminId,
          responsableNombre: r.responsable,
          categoria: r.categoria,
          titulo: r.proyecto,
          beneficiarios: r.beneficiarios,
          fecha: r.fecha,
          estado: r.estado,
          esEventoDelMes: r.esEventoDelMes,
          descripcion: r.descripcion,
        });
      }

      for (const m of INITIAL_FINANCE) {
        const proyectoId = projectExtToDbId.get(m.projectId) ?? defaultProjectId;
        const becaId = m.becaCodigo ? becaCodeToDbId.get(m.becaCodigo) ?? null : null;
        const adminId =
          adminByName.get(m.responsableNombre.toLowerCase()) ?? firstAdminId;

        await tx.insert(movimientosFinancieros).values({
          externalId: m.id,
          numeroComprobante: m.numeroComprobante,
          proyectoId,
          becaId,
          administrativoId: adminId,
          responsableNombre: m.responsableNombre,
          tipo: m.tipo,
          concepto: m.concepto,
          montoBs: m.montoBs.toFixed(2),
          fechaMovimiento: m.fechaMovimiento,
        });
      }

      const firstUserId = userExtToDbId.get('usr-01') ?? null;
      await tx.insert(bitacoraAuditoria).values([
        {
          usuarioId: firstUserId,
          accion: 'INICIALIZACION_BD',
          modulo: 'Base de Datos Cloud SQL',
          detalle:
            'Base de datos relacional normalizada (10 tablas 3FN) creada y poblada con 8 cuentas institucionales.',
        },
        {
          usuarioId: firstUserId,
          accion: 'SINCRONIZACION_ACTIVA',
          modulo: 'Multi-Dispositivo',
          detalle:
            'Sincronización en tiempo real habilitada para Windows PC (.exe), Android y Web.',
        },
      ]);

      await tx.insert(configuracionGlobal).values({
        nombreInstitucion: DEFAULT_GLOBAL_CONFIG_ROW.nombreInstitucion,
        tituloSistema: DEFAULT_GLOBAL_CONFIG_ROW.tituloSistema,
        subtituloLogin: DEFAULT_GLOBAL_CONFIG_ROW.subtituloLogin,
        mensajeBienvenida: DEFAULT_GLOBAL_CONFIG_ROW.mensajeBienvenida,
        avisoGlobal: DEFAULT_GLOBAL_CONFIG_ROW.avisoGlobal,
        colorPrimario: DEFAULT_GLOBAL_CONFIG_ROW.colorPrimario,
        colorSidebar: DEFAULT_GLOBAL_CONFIG_ROW.colorSidebar,
        colorAcento: DEFAULT_GLOBAL_CONFIG_ROW.colorAcento,
        colorFondo: DEFAULT_GLOBAL_CONFIG_ROW.colorFondo,
        sedesActivasJson: JSON.stringify(DEFAULT_GLOBAL_CONFIG_ROW.sedesActivas),
        version: 1,
        dataVersion: 1,
      });
    });
  } catch (error) {
    console.error('Database seed failed:', error);
    throw new Error('Database initialization failed. Please try again later.', {
      cause: error,
    });
  }
}

export async function getGlobalConfigFromDb(): Promise<GlobalInterfaceConfigRow> {
  try {
    await ensureDatabaseSeeded();
    const rows = await db.select().from(configuracionGlobal).orderBy(asc(configuracionGlobal.id)).limit(1);
    if (rows.length === 0) return { ...DEFAULT_GLOBAL_CONFIG_ROW };
    const row = rows[0];
    let parsedSedes: string[] = DEFAULT_GLOBAL_CONFIG_ROW.sedesActivas;
    try {
      const candidate = JSON.parse(row.sedesActivasJson);
      if (Array.isArray(candidate) && candidate.length > 0) {
        parsedSedes = candidate.map(String);
      }
    } catch {
      // keep default
    }
    return {
      nombreInstitucion: row.nombreInstitucion,
      tituloSistema: row.tituloSistema,
      subtituloLogin: row.subtituloLogin,
      mensajeBienvenida: row.mensajeBienvenida,
      avisoGlobal: row.avisoGlobal,
      colorPrimario: row.colorPrimario,
      colorSidebar: row.colorSidebar,
      colorAcento: row.colorAcento,
      colorFondo: row.colorFondo,
      sedesActivas: parsedSedes,
      version: row.version,
      updatedAt: row.updatedAt.toISOString(),
    };
  } catch (error) {
    console.error('Database query failed (getGlobalConfigFromDb):', error);
    throw new Error('Database query failed. Please try again later.', { cause: error });
  }
}

export async function saveGlobalConfigToDb(
  next: GlobalInterfaceConfigRow
): Promise<GlobalInterfaceConfigRow> {
  try {
    await ensureDatabaseSeeded();
    const rows = await db.select().from(configuracionGlobal).orderBy(asc(configuracionGlobal.id)).limit(1);
    const now = new Date();
    if (rows.length === 0) {
      await db.insert(configuracionGlobal).values({
        nombreInstitucion: next.nombreInstitucion,
        tituloSistema: next.tituloSistema,
        subtituloLogin: next.subtituloLogin,
        mensajeBienvenida: next.mensajeBienvenida,
        avisoGlobal: next.avisoGlobal,
        colorPrimario: next.colorPrimario,
        colorSidebar: next.colorSidebar,
        colorAcento: next.colorAcento,
        colorFondo: next.colorFondo,
        sedesActivasJson: JSON.stringify(next.sedesActivas),
        version: next.version,
        dataVersion: 1,
        updatedAt: now,
      });
    } else {
      await db
        .update(configuracionGlobal)
        .set({
          nombreInstitucion: next.nombreInstitucion,
          tituloSistema: next.tituloSistema,
          subtituloLogin: next.subtituloLogin,
          mensajeBienvenida: next.mensajeBienvenida,
          avisoGlobal: next.avisoGlobal,
          colorPrimario: next.colorPrimario,
          colorSidebar: next.colorSidebar,
          colorAcento: next.colorAcento,
          colorFondo: next.colorFondo,
          sedesActivasJson: JSON.stringify(next.sedesActivas),
          version: next.version,
          updatedAt: now,
        })
        .where(eq(configuracionGlobal.id, rows[0].id));
    }

    await db.insert(bitacoraAuditoria).values({
      usuarioId: null,
      accion: 'CONFIG_GLOBAL_PUBLICADA',
      modulo: 'Programador SIP',
      detalle: `Interfaz global actualizada a versión v${next.version} (${next.nombreInstitucion}).`,
    });

    return { ...next, updatedAt: now.toISOString() };
  } catch (error) {
    console.error('Database query failed (saveGlobalConfigToDb):', error);
    throw new Error('Database query failed. Please try again later.', { cause: error });
  }
}

export async function authenticateInstitutionalUser(
  identifier: string,
  plainPassword: string,
  expectedRole?: UserRole
): Promise<{ ok: true; user: SystemUser } | { ok: false; error: string }> {
  try {
    await ensureDatabaseSeeded();
    const cleaned = identifier.trim().toLowerCase();
    if (!cleaned || !plainPassword) {
      return { ok: false, error: 'Ingresa tu usuario y contraseña para continuar.' };
    }

    const [allUsers, allRoles, allSedes, allStudents] = await Promise.all([
      db.select().from(usuarios),
      db.select().from(roles),
      db.select().from(sedes),
      db.select().from(estudiantes),
    ]);

    const roleById = new Map(allRoles.map((r) => [r.id, r.nombre]));
    const sedeById = new Map(allSedes.map((s) => [s.id, s.nombre]));
    const studentByUserId = new Map(allStudents.map((e) => [e.usuarioId, e]));

    const matched = allUsers.find(
      (u) =>
        u.username.toLowerCase() === cleaned || u.correo.toLowerCase() === cleaned
    );

    if (!matched || !verifyPasswordPbkdf2(plainPassword, matched.passwordHash)) {
      return {
        ok: false,
        error: 'Credenciales incorrectas. Verifica tu usuario o contraseña.',
      };
    }

    if (!matched.activo) {
      return {
        ok: false,
        error: 'Esta cuenta se encuentra desactivada. Contacta al área administrativa.',
      };
    }

    const rolName = (roleById.get(matched.rolId) ?? 'Estudiante') as UserRole;
    if (expectedRole && rolName !== expectedRole) {
      return {
        ok: false,
        error: `Esta cuenta pertenece al apartado ${rolName}. Selecciona "${rolName}" arriba para ingresar.`,
      };
    }

    const now = new Date();
    const formatted = `${String(now.getDate()).padStart(2, '0')}/${String(
      now.getMonth() + 1
    ).padStart(2, '0')}/${now.getFullYear()} ${String(now.getHours()).padStart(
      2,
      '0'
    )}:${String(now.getMinutes()).padStart(2, '0')}`;

    await db
      .update(usuarios)
      .set({ ultimoAcceso: formatted })
      .where(eq(usuarios.id, matched.id));

    await db.insert(bitacoraAuditoria).values({
      usuarioId: matched.id,
      accion: 'INICIO_SESION',
      modulo: rolName === 'Administrativo' ? 'Panel Administrativo' : 'Portal Estudiantil',
      detalle: `Inicio de sesión verificado en Cloud SQL (${matched.username}).`,
    });

    const st = studentByUserId.get(matched.id);
    const systemUser: SystemUser = {
      id: matched.externalId,
      username: matched.username,
      password: '',
      nombreCompleto: matched.nombreCompleto,
      correo: matched.correo,
      rol: rolName,
      sede: (sedeById.get(matched.sedeId) ?? 'La Paz') as Campus,
      activo: matched.activo,
      ultimoAcceso: formatted,
      studentProfile: st
        ? {
            codigoEstudiante: st.codigoEstudiante,
            carrera: st.carrera,
            semestre: st.semestre,
            promedio: Number(st.promedio),
            ci: st.ci,
          }
        : undefined,
    };

    return { ok: true, user: systemUser };
  } catch (error) {
    console.error('Database query failed (authenticateInstitutionalUser):', error);
    throw new Error('Database query failed. Please try again later.', { cause: error });
  }
}

export async function linkOrCreateFirebaseUser(
  uid: string,
  email: string,
  displayName?: string
) {
  try {
    await ensureDatabaseSeeded();
    const existingByUid = await db
      .select()
      .from(usuarios)
      .where(eq(usuarios.uid, uid))
      .limit(1);

    if (existingByUid.length > 0) {
      const [updated] = await db
        .update(usuarios)
        .set({
          ultimoAcceso: new Date().toLocaleString('es-BO'),
        })
        .where(eq(usuarios.id, existingByUid[0].id))
        .returning();
      return updated;
    }

    const existingByEmail = await db
      .select()
      .from(usuarios)
      .where(eq(usuarios.correo, email))
      .limit(1);

    if (existingByEmail.length > 0) {
      const [linked] = await db
        .update(usuarios)
        .set({ uid, ultimoAcceso: new Date().toLocaleString('es-BO') })
        .where(eq(usuarios.id, existingByEmail[0].id))
        .returning();
      return linked;
    }

    const allRoles = await db.select().from(roles);
    const allSedes = await db.select().from(sedes);
    const adminRoleId =
      allRoles.find((r) => r.nombre === 'Administrativo')?.id ?? allRoles[0].id;
    const defaultSedeId = allSedes[0].id;
    const baseUsername = email.split('@')[0].replace(/[^a-zA-Z0-9._-]/g, '').slice(0, 28) || 'usuario';
    const uniqueUsername = `${baseUsername}.${uid.slice(0, 5).toLowerCase()}`;

    const [created] = await db
      .insert(usuarios)
      .values({
        externalId: `usr-g-${uid.slice(0, 10)}`,
        uid,
        username: uniqueUsername,
        passwordHash: hashPasswordPbkdf2(crypto.randomBytes(24).toString('hex')),
        nombreCompleto: displayName || email,
        correo: email,
        rolId: adminRoleId,
        sedeId: defaultSedeId,
        activo: true,
        ultimoAcceso: new Date().toLocaleString('es-BO'),
      })
      .onConflictDoUpdate({
        target: usuarios.uid,
        set: {
          ultimoAcceso: new Date().toLocaleString('es-BO'),
        },
      })
      .returning();

    await db.insert(bitacoraAuditoria).values({
      usuarioId: created.id,
      accion: 'VINCULO_GOOGLE_AUTH',
      modulo: 'Autenticación Cloud SQL',
      detalle: `Cuenta Google vinculada correctamente (${email}).`,
    });

    return created;
  } catch (error) {
    console.error('Database query failed (linkOrCreateFirebaseUser):', error);
    throw new Error('Database query failed. Please try again later.', { cause: error });
  }
}

export async function loadAppDataFromDb(): Promise<SyncedAppDataState> {
  try {
    await ensureDatabaseSeeded();
    const [
      allRoles,
      allSedes,
      allUsers,
      allStudents,
      allProjects,
      allBecas,
      allActivities,
      allMovements,
      cfgRows,
    ] = await Promise.all([
      db.select().from(roles),
      db.select().from(sedes),
      db.select().from(usuarios).orderBy(asc(usuarios.id)),
      db.select().from(estudiantes),
      db.select().from(proyectos).orderBy(desc(proyectos.id)),
      db.select().from(becasOtorgadas).orderBy(desc(becasOtorgadas.id)),
      db.select().from(actividades).orderBy(desc(actividades.id)),
      db.select().from(movimientosFinancieros).orderBy(desc(movimientosFinancieros.id)),
      db.select().from(configuracionGlobal).orderBy(asc(configuracionGlobal.id)).limit(1),
    ]);

    const roleById = new Map(allRoles.map((r) => [r.id, r.nombre]));
    const sedeById = new Map(allSedes.map((s) => [s.id, s.nombre]));
    const studentByUserId = new Map(allStudents.map((e) => [e.usuarioId, e]));
    const userById = new Map(allUsers.map((u) => [u.id, u]));
    const studentById = new Map(allStudents.map((e) => [e.id, e]));
    const projectById = new Map(allProjects.map((p) => [p.id, p]));
    const becaById = new Map(allBecas.map((b) => [b.id, b]));

    const mappedUsers: SystemUser[] = allUsers.map((u) => {
      const st = studentByUserId.get(u.id);
      return {
        id: u.externalId,
        username: u.username,
        password: '',
        nombreCompleto: u.nombreCompleto,
        correo: u.correo,
        rol: (roleById.get(u.rolId) ?? 'Estudiante') as UserRole,
        sede: (sedeById.get(u.sedeId) ?? 'La Paz') as Campus,
        activo: u.activo,
        ultimoAcceso: u.ultimoAcceso,
        studentProfile: st
          ? {
              codigoEstudiante: st.codigoEstudiante,
              carrera: st.carrera,
              semestre: st.semestre,
              promedio: Number(st.promedio),
              ci: st.ci,
            }
          : undefined,
      };
    });

    const mappedProjects: FoundationProject[] = allProjects.map((p) => ({
      id: p.externalId,
      codigo: p.codigo,
      nombre: p.nombre,
      categoria: p.categoria as ProjectCategory,
      sede: (sedeById.get(p.sedeId) ?? 'La Paz') as Campus,
      responsable: p.responsableNombre,
      fechaInicio: p.fechaInicio,
      fechaFin: p.fechaFin,
      beneficiariosMeta: p.beneficiariosMeta,
      beneficiariosActuales: p.beneficiariosActuales,
      presupuestoBs: Number(p.presupuestoBs),
      ejecutadoBs: Number(p.ejecutadoBs),
      estado: p.estado as ProjectStatus,
      descripcion: p.descripcion,
    }));

    const mappedScholarships: ScholarshipAward[] = allBecas.map((b) => {
      const prj = projectById.get(b.proyectoId);
      const st = studentById.get(b.estudianteId);
      const usr = st ? userById.get(st.usuarioId) : undefined;
      return {
        id: b.externalId,
        codigo: b.codigo,
        projectId: prj?.externalId ?? 'prj-01',
        proyectoNombre: prj?.nombre ?? 'Proyecto Institucional',
        studentUserId: usr?.externalId ?? 'usr-05',
        estudianteNombre: usr?.nombreCompleto ?? (st ? `${st.nombres} ${st.apellidos}` : 'Estudiante'),
        codigoEstudiante: st?.codigoEstudiante ?? 'EST-2026',
        evaluadorNombre: b.evaluadorNombre,
        porcentajeBeca: b.porcentajeBeca,
        montoAsignadoBs: Number(b.montoAsignadoBs),
        fechaSolicitud: b.fechaSolicitud,
        estado: b.estado as ScholarshipStatus,
        observacion: b.observacion,
      };
    });

    const mappedRecords: ActivityRecord[] = allActivities.map((a) => {
      const prj = projectById.get(a.proyectoId);
      return {
        id: a.externalId,
        projectId: prj?.externalId ?? 'prj-01',
        proyecto: a.titulo,
        categoria: a.categoria as ProjectCategory,
        responsable: a.responsableNombre,
        fecha: a.fecha,
        estado: a.estado as ActivityStatus,
        beneficiarios: a.beneficiarios,
        sede: (sedeById.get(a.sedeId) ?? 'La Paz') as Campus,
        esEventoDelMes: a.esEventoDelMes,
        descripcion: a.descripcion,
      };
    });

    const mappedMovements: FinanceMovement[] = allMovements.map((m) => {
      const prj = projectById.get(m.proyectoId);
      const beca = m.becaId ? becaById.get(m.becaId) : undefined;
      return {
        id: m.externalId,
        numeroComprobante: m.numeroComprobante,
        projectId: prj?.externalId ?? 'prj-01',
        proyectoNombre: prj?.nombre ?? 'Proyecto Institucional',
        becaCodigo: beca?.codigo,
        responsableNombre: m.responsableNombre,
        tipo: m.tipo as FinanceType,
        concepto: m.concepto,
        montoBs: Number(m.montoBs),
        fechaMovimiento: m.fechaMovimiento,
      };
    });

    const dataVersion = cfgRows[0]?.dataVersion ?? 1;
    const updatedAt = cfgRows[0]?.updatedAt.toISOString() ?? new Date().toISOString();

    return {
      users: mappedUsers,
      projects: mappedProjects,
      records: mappedRecords,
      scholarships: mappedScholarships,
      movements: mappedMovements,
      dataVersion,
      updatedAt,
    };
  } catch (error) {
    console.error('Database query failed (loadAppDataFromDb):', error);
    throw new Error('Database query failed. Please try again later.', { cause: error });
  }
}

export async function syncAppDataToDb(payload: {
  users?: SystemUser[];
  projects?: FoundationProject[];
  records?: ActivityRecord[];
  scholarships?: ScholarshipAward[];
  movements?: FinanceMovement[];
  actorLabel?: string;
}): Promise<{ dataVersion: number; updatedAt: string }> {
  try {
    await ensureDatabaseSeeded();
    const now = new Date();

    const resultVersion = await db.transaction(async (tx) => {
      const allRoles = await tx.select().from(roles);
      const allSedes = await tx.select().from(sedes);
      const roleByName = new Map(allRoles.map((r) => [r.nombre, r.id]));
      const sedeByName = new Map(allSedes.map((s) => [s.nombre, s.id]));
      const defaultSedeId = allSedes[0].id;
      const adminRoleId = roleByName.get('Administrativo') ?? allRoles[0].id;
      const studentRoleId = roleByName.get('Estudiante') ?? allRoles[1].id;

      // 1. Sync Usuarios + Administrativos + Estudiantes
      if (Array.isArray(payload.users) && payload.users.length > 0) {
        const existingUsers = await tx.select().from(usuarios);
        const existingByExt = new Map(existingUsers.map((u) => [u.externalId, u]));
        const incomingUserExts = new Set<string>();

        for (const u of payload.users.slice(0, 500)) {
          if (!u || !u.id || !u.username || !u.correo) continue;
          incomingUserExts.add(u.id);
          const rolId = u.rol === 'Administrativo' ? adminRoleId : studentRoleId;
          const sedeId = sedeByName.get(u.sede) ?? defaultSedeId;
          const existing = existingByExt.get(u.id);

          let dbUserId: number;
          if (existing) {
            const nextPasswordHash =
              u.password && u.password.trim().length > 0
                ? hashPasswordPbkdf2(u.password)
                : existing.passwordHash;
            await tx
              .update(usuarios)
              .set({
                username: u.username,
                passwordHash: nextPasswordHash,
                nombreCompleto: u.nombreCompleto,
                correo: u.correo,
                rolId,
                sedeId,
                activo: Boolean(u.activo),
                ultimoAcceso: u.ultimoAcceso || existing.ultimoAcceso,
              })
              .where(eq(usuarios.id, existing.id));
            dbUserId = existing.id;
          } else {
            const [inserted] = await tx
              .insert(usuarios)
              .values({
                externalId: u.id,
                username: u.username,
                passwordHash: hashPasswordPbkdf2(
                  u.password && u.password.trim().length > 0 ? u.password : 'Unifranz#2026'
                ),
                nombreCompleto: u.nombreCompleto,
                correo: u.correo,
                rolId,
                sedeId,
                activo: Boolean(u.activo),
                ultimoAcceso: u.ultimoAcceso || 'Sin accesos previos',
              })
              .onConflictDoUpdate({
                target: usuarios.username,
                set: {
                  nombreCompleto: u.nombreCompleto,
                  rolId,
                  sedeId,
                  activo: Boolean(u.activo),
                  ultimoAcceso: u.ultimoAcceso || 'Sin accesos previos',
                },
              })
              .returning();
            dbUserId = inserted.id;
          }

          const { nombres, apellidos } = splitFullName(u.nombreCompleto);
          if (u.rol === 'Administrativo') {
            const existingAdm = await tx
              .select()
              .from(administrativos)
              .where(eq(administrativos.usuarioId, dbUserId))
              .limit(1);
            if (existingAdm.length === 0) {
              await tx.insert(administrativos).values({
                usuarioId: dbUserId,
                sedeId,
                nombres,
                apellidos,
                ci: `CI-ADM-${dbUserId}-${Date.now().toString().slice(-3)}`,
                cargo: 'Coordinador Institucional',
                telefono: '+591 70000000',
              });
            } else {
              await tx
                .update(administrativos)
                .set({ sedeId, nombres, apellidos })
                .where(eq(administrativos.id, existingAdm[0].id));
            }
          } else {
            const existingEst = await tx
              .select()
              .from(estudiantes)
              .where(eq(estudiantes.usuarioId, dbUserId))
              .limit(1);
            const prof = u.studentProfile;
            if (existingEst.length === 0) {
              await tx.insert(estudiantes).values({
                usuarioId: dbUserId,
                sedeId,
                nombres,
                apellidos,
                ci: prof?.ci || `CI-EST-${dbUserId}-${Date.now().toString().slice(-3)}`,
                codigoEstudiante:
                  prof?.codigoEstudiante || `EST-2026-${String(dbUserId).padStart(3, '0')}`,
                carrera: prof?.carrera || 'Ingeniería de Sistemas',
                semestre: prof?.semestre ?? 1,
                promedio: Number(prof?.promedio ?? 85).toFixed(2),
                telefono: '+591 75000000',
              });
            } else {
              await tx
                .update(estudiantes)
                .set({
                  sedeId,
                  nombres,
                  apellidos,
                  carrera: prof?.carrera || existingEst[0].carrera,
                  semestre: prof?.semestre ?? existingEst[0].semestre,
                  promedio: Number(prof?.promedio ?? Number(existingEst[0].promedio)).toFixed(2),
                })
                .where(eq(estudiantes.id, existingEst[0].id));
            }
          }
        }

        // Delete removed non-seeded users if admin deleted them
        for (const old of existingUsers) {
          if (!incomingUserExts.has(old.externalId) && !old.uid) {
            await tx.delete(usuarios).where(eq(usuarios.id, old.id));
          }
        }
      }

      // Reload foreign-key lookup maps
      const [dbUsers, dbAdmins, dbStudents] = await Promise.all([
        tx.select().from(usuarios),
        tx.select().from(administrativos),
        tx.select().from(estudiantes),
      ]);

      const userExtToDbId = new Map(dbUsers.map((u) => [u.externalId, u.id]));
      const adminBySede = new Map(dbAdmins.map((a) => [a.sedeId, a.id]));
      const adminByName = new Map(
        dbAdmins.map((a) => [`${a.nombres} ${a.apellidos}`.toLowerCase(), a.id])
      );
      const firstAdminId = dbAdmins[0]?.id;
      const studentByUserDbId = new Map(dbStudents.map((e) => [e.usuarioId, e.id]));
      const firstStudentId = dbStudents[0]?.id;

      // 2. Sync Proyectos
      if (Array.isArray(payload.projects) && firstAdminId) {
        const existingProjects = await tx.select().from(proyectos);
        const existingProjByExt = new Map(existingProjects.map((p) => [p.externalId, p]));
        const incomingProjExts = new Set<string>();

        for (const p of payload.projects.slice(0, 500)) {
          if (!p || !p.id || !p.nombre) continue;
          incomingProjExts.add(p.id);
          const sedeId = sedeByName.get(p.sede) ?? defaultSedeId;
          const adminId =
            adminByName.get(p.responsable.toLowerCase()) ??
            adminBySede.get(sedeId) ??
            firstAdminId;

          const existing = existingProjByExt.get(p.id);
          if (existing) {
            await tx
              .update(proyectos)
              .set({
                codigo: p.codigo,
                nombre: p.nombre,
                categoria: p.categoria,
                sedeId,
                administrativoId: adminId,
                responsableNombre: p.responsable,
                fechaInicio: p.fechaInicio,
                fechaFin: p.fechaFin,
                beneficiariosMeta: Number(p.beneficiariosMeta) || 0,
                beneficiariosActuales: Number(p.beneficiariosActuales) || 0,
                presupuestoBs: Number(p.presupuestoBs || 0).toFixed(2),
                ejecutadoBs: Number(p.ejecutadoBs || 0).toFixed(2),
                estado: p.estado,
                descripcion: p.descripcion || '',
              })
              .where(eq(proyectos.id, existing.id));
          } else {
            await tx
              .insert(proyectos)
              .values({
                externalId: p.id,
                codigo: p.codigo,
                nombre: p.nombre,
                categoria: p.categoria,
                sedeId,
                administrativoId: adminId,
                responsableNombre: p.responsable,
                fechaInicio: p.fechaInicio,
                fechaFin: p.fechaFin,
                beneficiariosMeta: Number(p.beneficiariosMeta) || 0,
                beneficiariosActuales: Number(p.beneficiariosActuales) || 0,
                presupuestoBs: Number(p.presupuestoBs || 0).toFixed(2),
                ejecutadoBs: Number(p.ejecutadoBs || 0).toFixed(2),
                estado: p.estado,
                descripcion: p.descripcion || '',
              })
              .onConflictDoUpdate({
                target: proyectos.codigo,
                set: {
                  nombre: p.nombre,
                  estado: p.estado,
                  beneficiariosActuales: Number(p.beneficiariosActuales) || 0,
                  ejecutadoBs: Number(p.ejecutadoBs || 0).toFixed(2),
                },
              });
          }
        }

        for (const old of existingProjects) {
          if (!incomingProjExts.has(old.externalId)) {
            await tx.delete(proyectos).where(eq(proyectos.id, old.id));
          }
        }
      }

      const dbProjects = await tx.select().from(proyectos);
      const projExtToDbId = new Map(dbProjects.map((p) => [p.externalId, p.id]));
      const firstProjectId = dbProjects[0]?.id;

      // 3. Sync Becas Otorgadas
      if (Array.isArray(payload.scholarships) && firstProjectId && firstStudentId) {
        const existingBecas = await tx.select().from(becasOtorgadas);
        const existingBecaByExt = new Map(existingBecas.map((b) => [b.externalId, b]));
        const incomingBecaExts = new Set<string>();

        for (const b of payload.scholarships.slice(0, 1000)) {
          if (!b || !b.id || !b.codigo) continue;
          incomingBecaExts.add(b.id);
          const proyectoId = projExtToDbId.get(b.projectId) ?? firstProjectId;
          const userDbId = userExtToDbId.get(b.studentUserId);
          const estudianteId =
            (userDbId ? studentByUserDbId.get(userDbId) : undefined) ?? firstStudentId;
          const adminId =
            b.estado === 'Pendiente'
              ? null
              : adminByName.get(b.evaluadorNombre.toLowerCase()) ?? firstAdminId ?? null;

          const existing = existingBecaByExt.get(b.id);
          if (existing) {
            await tx
              .update(becasOtorgadas)
              .set({
                proyectoId,
                estudianteId,
                administrativoId: adminId,
                evaluadorNombre: b.evaluadorNombre,
                porcentajeBeca: Number(b.porcentajeBeca) || 50,
                montoAsignadoBs: Number(b.montoAsignadoBs || 0).toFixed(2),
                fechaSolicitud: b.fechaSolicitud,
                estado: b.estado,
                observacion: b.observacion || '',
              })
              .where(eq(becasOtorgadas.id, existing.id));
          } else {
            await tx
              .insert(becasOtorgadas)
              .values({
                externalId: b.id,
                codigo: b.codigo,
                proyectoId,
                estudianteId,
                administrativoId: adminId,
                evaluadorNombre: b.evaluadorNombre,
                porcentajeBeca: Number(b.porcentajeBeca) || 50,
                montoAsignadoBs: Number(b.montoAsignadoBs || 0).toFixed(2),
                fechaSolicitud: b.fechaSolicitud,
                estado: b.estado,
                observacion: b.observacion || '',
              })
              .onConflictDoUpdate({
                target: becasOtorgadas.codigo,
                set: {
                  estado: b.estado,
                  evaluadorNombre: b.evaluadorNombre,
                  observacion: b.observacion || '',
                },
              });
          }
        }

        for (const old of existingBecas) {
          if (!incomingBecaExts.has(old.externalId)) {
            await tx.delete(becasOtorgadas).where(eq(becasOtorgadas.id, old.id));
          }
        }
      }

      // 4. Sync Actividades
      if (Array.isArray(payload.records) && firstProjectId && firstAdminId) {
        const existingActs = await tx.select().from(actividades);
        const existingActByExt = new Map(existingActs.map((a) => [a.externalId, a]));
        const incomingActExts = new Set<string>();

        for (const r of payload.records.slice(0, 1000)) {
          if (!r || !r.id) continue;
          incomingActExts.add(r.id);
          const proyectoId = projExtToDbId.get(r.projectId) ?? firstProjectId;
          const sedeId = sedeByName.get(r.sede) ?? defaultSedeId;
          const adminId =
            adminByName.get(r.responsable.toLowerCase()) ??
            adminBySede.get(sedeId) ??
            firstAdminId;

          const existing = existingActByExt.get(r.id);
          if (existing) {
            await tx
              .update(actividades)
              .set({
                proyectoId,
                sedeId,
                administrativoId: adminId,
                responsableNombre: r.responsable,
                categoria: r.categoria,
                titulo: r.proyecto,
                beneficiarios: Number(r.beneficiarios) || 0,
                fecha: r.fecha,
                estado: r.estado,
                esEventoDelMes: Boolean(r.esEventoDelMes),
                descripcion: r.descripcion || '',
              })
              .where(eq(actividades.id, existing.id));
          } else {
            await tx
              .insert(actividades)
              .values({
                externalId: r.id,
                codigo: `ACT-${r.id.padStart(3, '0')}`,
                proyectoId,
                sedeId,
                administrativoId: adminId,
                responsableNombre: r.responsable,
                categoria: r.categoria,
                titulo: r.proyecto,
                beneficiarios: Number(r.beneficiarios) || 0,
                fecha: r.fecha,
                estado: r.estado,
                esEventoDelMes: Boolean(r.esEventoDelMes),
                descripcion: r.descripcion || '',
              })
              .onConflictDoUpdate({
                target: actividades.codigo,
                set: {
                  titulo: r.proyecto,
                  estado: r.estado,
                  beneficiarios: Number(r.beneficiarios) || 0,
                },
              });
          }
        }

        for (const old of existingActs) {
          if (!incomingActExts.has(old.externalId)) {
            await tx.delete(actividades).where(eq(actividades.id, old.id));
          }
        }
      }

      // 5. Sync Movimientos Financieros
      const dbBecas = await tx.select().from(becasOtorgadas);
      const becaByCode = new Map(dbBecas.map((b) => [b.codigo, b.id]));

      if (Array.isArray(payload.movements) && firstProjectId && firstAdminId) {
        const existingMovs = await tx.select().from(movimientosFinancieros);
        const existingMovByExt = new Map(existingMovs.map((m) => [m.externalId, m]));
        const incomingMovExts = new Set<string>();

        for (const m of payload.movements.slice(0, 1000)) {
          if (!m || !m.id || !m.numeroComprobante) continue;
          incomingMovExts.add(m.id);
          const proyectoId = projExtToDbId.get(m.projectId) ?? firstProjectId;
          const becaId = m.becaCodigo ? becaByCode.get(m.becaCodigo) ?? null : null;
          const adminId =
            adminByName.get(m.responsableNombre.toLowerCase()) ?? firstAdminId;

          const existing = existingMovByExt.get(m.id);
          if (existing) {
            await tx
              .update(movimientosFinancieros)
              .set({
                proyectoId,
                becaId,
                administrativoId: adminId,
                responsableNombre: m.responsableNombre,
                tipo: m.tipo,
                concepto: m.concepto,
                montoBs: Number(m.montoBs || 0).toFixed(2),
                fechaMovimiento: m.fechaMovimiento,
              })
              .where(eq(movimientosFinancieros.id, existing.id));
          } else {
            await tx
              .insert(movimientosFinancieros)
              .values({
                externalId: m.id,
                numeroComprobante: m.numeroComprobante,
                proyectoId,
                becaId,
                administrativoId: adminId,
                responsableNombre: m.responsableNombre,
                tipo: m.tipo,
                concepto: m.concepto,
                montoBs: Number(m.montoBs || 0).toFixed(2),
                fechaMovimiento: m.fechaMovimiento,
              })
              .onConflictDoUpdate({
                target: movimientosFinancieros.numeroComprobante,
                set: {
                  concepto: m.concepto,
                  montoBs: Number(m.montoBs || 0).toFixed(2),
                },
              });
          }
        }

        for (const old of existingMovs) {
          if (!incomingMovExts.has(old.externalId)) {
            await tx.delete(movimientosFinancieros).where(eq(movimientosFinancieros.id, old.id));
          }
        }
      }

      // Bump dataVersion & write audit log
      const cfgRows = await tx
        .select()
        .from(configuracionGlobal)
        .orderBy(asc(configuracionGlobal.id))
        .limit(1);

      const nextVersion = (cfgRows[0]?.dataVersion ?? 1) + 1;
      if (cfgRows[0]) {
        await tx
          .update(configuracionGlobal)
          .set({ dataVersion: nextVersion, updatedAt: now })
          .where(eq(configuracionGlobal.id, cfgRows[0].id));
      }

      await tx.insert(bitacoraAuditoria).values({
        usuarioId: null,
        accion: 'SINCRONIZACION_DATOS',
        modulo: 'Cloud SQL Tiempo Real',
        detalle: `${payload.actorLabel || 'Dispositivo conectado'} guardó cambios (v${nextVersion}: ${
          payload.projects?.length ?? 0
        } proyectos, ${payload.scholarships?.length ?? 0} becas, ${
          payload.records?.length ?? 0
        } actividades).`,
      });

      return nextVersion;
    });

    return { dataVersion: resultVersion, updatedAt: now.toISOString() };
  } catch (error) {
    console.error('Database query failed (syncAppDataToDb):', error);
    throw new Error('Database query failed. Please try again later.', { cause: error });
  }
}

export interface DbColumnMeta {
  name: string;
  type: string;
  key?: 'PK' | 'FK' | 'UQ';
  references?: string;
}

export interface DbTableSnapshot {
  name: string;
  title: string;
  category: 'Seguridad y Catálogos' | 'Personas' | 'Operación Académica' | 'Finanzas y Auditoría';
  columns: DbColumnMeta[];
  rowCount: number;
  rows: Record<string, unknown>[];
}

export interface DbRelationEdge {
  id: string;
  fromTable: string;
  fromColumn: string;
  toTable: string;
  toColumn: string;
  cardinality: 'N:1' | '1:1';
  constraintName: string;
}

export interface RelationalDatabaseSnapshot {
  databaseName: string;
  engine: string;
  normalization: string;
  tableCount: number;
  relationCount: number;
  generatedAt: string;
  tables: DbTableSnapshot[];
  relations: DbRelationEdge[];
}

export async function getRelationalDatabaseSnapshot(): Promise<RelationalDatabaseSnapshot> {
  try {
    await ensureDatabaseSeeded();
    const [
      rowsRoles,
      rowsSedes,
      rowsUsuarios,
      rowsAdmins,
      rowsEstudiantes,
      rowsProyectos,
      rowsBecas,
      rowsActividades,
      rowsMovs,
      rowsBitacora,
    ] = await Promise.all([
      db.select().from(roles).orderBy(asc(roles.id)),
      db.select().from(sedes).orderBy(asc(sedes.id)),
      db.select().from(usuarios).orderBy(asc(usuarios.id)),
      db.select().from(administrativos).orderBy(asc(administrativos.id)),
      db.select().from(estudiantes).orderBy(asc(estudiantes.id)),
      db.select().from(proyectos).orderBy(asc(proyectos.id)),
      db.select().from(becasOtorgadas).orderBy(asc(becasOtorgadas.id)),
      db.select().from(actividades).orderBy(asc(actividades.id)),
      db.select().from(movimientosFinancieros).orderBy(asc(movimientosFinancieros.id)),
      db.select().from(bitacoraAuditoria).orderBy(desc(bitacoraAuditoria.id)).limit(40),
    ]);

    const tables: DbTableSnapshot[] = [
      {
        name: 'roles',
        title: '1. roles',
        category: 'Seguridad y Catálogos',
        columns: [
          { name: 'id', type: 'SERIAL', key: 'PK' },
          { name: 'nombre', type: 'TEXT', key: 'UQ' },
          { name: 'descripcion', type: 'TEXT' },
        ],
        rowCount: rowsRoles.length,
        rows: rowsRoles.map((r) => ({
          id: r.id,
          nombre: r.nombre,
          descripcion: r.descripcion,
        })),
      },
      {
        name: 'sedes',
        title: '2. sedes',
        category: 'Seguridad y Catálogos',
        columns: [
          { name: 'id', type: 'SERIAL', key: 'PK' },
          { name: 'nombre', type: 'TEXT', key: 'UQ' },
          { name: 'ciudad', type: 'TEXT' },
          { name: 'direccion', type: 'TEXT' },
          { name: 'activa', type: 'BOOLEAN' },
        ],
        rowCount: rowsSedes.length,
        rows: rowsSedes.map((s) => ({
          id: s.id,
          nombre: s.nombre,
          ciudad: s.ciudad,
          direccion: s.direccion,
          activa: s.activa ? 'true' : 'false',
        })),
      },
      {
        name: 'usuarios',
        title: '3. usuarios',
        category: 'Seguridad y Catálogos',
        columns: [
          { name: 'id', type: 'SERIAL', key: 'PK' },
          { name: 'username', type: 'TEXT', key: 'UQ' },
          { name: 'password_hash', type: 'TEXT' },
          { name: 'nombre_completo', type: 'TEXT' },
          { name: 'correo', type: 'TEXT', key: 'UQ' },
          { name: 'rol_id', type: 'INTEGER', key: 'FK', references: 'roles.id' },
          { name: 'sede_id', type: 'INTEGER', key: 'FK', references: 'sedes.id' },
          { name: 'activo', type: 'BOOLEAN' },
          { name: 'ultimo_acceso', type: 'TEXT' },
        ],
        rowCount: rowsUsuarios.length,
        rows: rowsUsuarios.map((u) => ({
          id: u.id,
          username: u.username,
          password_hash: 'PBKDF2-SHA256 [CIFRADA]',
          nombre_completo: u.nombreCompleto,
          correo: u.correo,
          rol_id: u.rolId,
          sede_id: u.sedeId,
          activo: u.activo ? 'true' : 'false',
          ultimo_acceso: u.ultimoAcceso,
        })),
      },
      {
        name: 'administrativos',
        title: '4. administrativos',
        category: 'Personas',
        columns: [
          { name: 'id', type: 'SERIAL', key: 'PK' },
          { name: 'usuario_id', type: 'INTEGER', key: 'FK', references: 'usuarios.id' },
          { name: 'sede_id', type: 'INTEGER', key: 'FK', references: 'sedes.id' },
          { name: 'nombres', type: 'TEXT' },
          { name: 'apellidos', type: 'TEXT' },
          { name: 'ci', type: 'TEXT', key: 'UQ' },
          { name: 'cargo', type: 'TEXT' },
          { name: 'telefono', type: 'TEXT' },
        ],
        rowCount: rowsAdmins.length,
        rows: rowsAdmins.map((a) => ({
          id: a.id,
          usuario_id: a.usuarioId,
          sede_id: a.sedeId,
          nombres: a.nombres,
          apellidos: a.apellidos,
          ci: a.ci,
          cargo: a.cargo,
          telefono: a.telefono,
        })),
      },
      {
        name: 'estudiantes',
        title: '5. estudiantes',
        category: 'Personas',
        columns: [
          { name: 'id', type: 'SERIAL', key: 'PK' },
          { name: 'usuario_id', type: 'INTEGER', key: 'FK', references: 'usuarios.id' },
          { name: 'sede_id', type: 'INTEGER', key: 'FK', references: 'sedes.id' },
          { name: 'nombres', type: 'TEXT' },
          { name: 'apellidos', type: 'TEXT' },
          { name: 'ci', type: 'TEXT', key: 'UQ' },
          { name: 'codigo_estudiante', type: 'TEXT', key: 'UQ' },
          { name: 'carrera', type: 'TEXT' },
          { name: 'semestre', type: 'INTEGER' },
          { name: 'promedio', type: 'NUMERIC(5,2)' },
        ],
        rowCount: rowsEstudiantes.length,
        rows: rowsEstudiantes.map((e) => ({
          id: e.id,
          usuario_id: e.usuarioId,
          sede_id: e.sedeId,
          nombres: e.nombres,
          apellidos: e.apellidos,
          ci: e.ci,
          codigo_estudiante: e.codigoEstudiante,
          carrera: e.carrera,
          semestre: e.semestre,
          promedio: e.promedio,
        })),
      },
      {
        name: 'proyectos',
        title: '6. proyectos',
        category: 'Operación Académica',
        columns: [
          { name: 'id', type: 'SERIAL', key: 'PK' },
          { name: 'codigo', type: 'TEXT', key: 'UQ' },
          { name: 'nombre', type: 'TEXT' },
          { name: 'categoria', type: 'TEXT' },
          { name: 'sede_id', type: 'INTEGER', key: 'FK', references: 'sedes.id' },
          { name: 'administrativo_id', type: 'INTEGER', key: 'FK', references: 'administrativos.id' },
          { name: 'beneficiarios_meta', type: 'INTEGER' },
          { name: 'beneficiarios_actuales', type: 'INTEGER' },
          { name: 'presupuesto_bs', type: 'NUMERIC(18,2)' },
          { name: 'ejecutado_bs', type: 'NUMERIC(18,2)' },
          { name: 'estado', type: 'TEXT' },
        ],
        rowCount: rowsProyectos.length,
        rows: rowsProyectos.map((p) => ({
          id: p.id,
          codigo: p.codigo,
          nombre: p.nombre,
          categoria: p.categoria,
          sede_id: p.sedeId,
          administrativo_id: p.administrativoId,
          beneficiarios_meta: p.beneficiariosMeta,
          beneficiarios_actuales: p.beneficiariosActuales,
          presupuesto_bs: p.presupuestoBs,
          ejecutado_bs: p.ejecutadoBs,
          estado: p.estado,
        })),
      },
      {
        name: 'becas_otorgadas',
        title: '7. becas_otorgadas',
        category: 'Operación Académica',
        columns: [
          { name: 'id', type: 'SERIAL', key: 'PK' },
          { name: 'codigo', type: 'TEXT', key: 'UQ' },
          { name: 'proyecto_id', type: 'INTEGER', key: 'FK', references: 'proyectos.id' },
          { name: 'estudiante_id', type: 'INTEGER', key: 'FK', references: 'estudiantes.id' },
          { name: 'administrativo_id', type: 'INTEGER', key: 'FK', references: 'administrativos.id' },
          { name: 'porcentaje_beca', type: 'INTEGER' },
          { name: 'monto_asignado_bs', type: 'NUMERIC(18,2)' },
          { name: 'fecha_solicitud', type: 'TEXT' },
          { name: 'estado', type: 'TEXT' },
        ],
        rowCount: rowsBecas.length,
        rows: rowsBecas.map((b) => ({
          id: b.id,
          codigo: b.codigo,
          proyecto_id: b.proyectoId,
          estudiante_id: b.estudianteId,
          administrativo_id: b.administrativoId ?? 'NULL',
          porcentaje_beca: `${b.porcentajeBeca}%`,
          monto_asignado_bs: b.montoAsignadoBs,
          fecha_solicitud: b.fechaSolicitud,
          estado: b.estado,
        })),
      },
      {
        name: 'actividades',
        title: '8. actividades',
        category: 'Operación Académica',
        columns: [
          { name: 'id', type: 'SERIAL', key: 'PK' },
          { name: 'codigo', type: 'TEXT', key: 'UQ' },
          { name: 'proyecto_id', type: 'INTEGER', key: 'FK', references: 'proyectos.id' },
          { name: 'sede_id', type: 'INTEGER', key: 'FK', references: 'sedes.id' },
          { name: 'administrativo_id', type: 'INTEGER', key: 'FK', references: 'administrativos.id' },
          { name: 'titulo', type: 'TEXT' },
          { name: 'beneficiarios', type: 'INTEGER' },
          { name: 'fecha', type: 'TEXT' },
          { name: 'estado', type: 'TEXT' },
        ],
        rowCount: rowsActividades.length,
        rows: rowsActividades.map((a) => ({
          id: a.id,
          codigo: a.codigo,
          proyecto_id: a.proyectoId,
          sede_id: a.sedeId,
          administrativo_id: a.administrativoId,
          titulo: a.titulo,
          beneficiarios: a.beneficiarios,
          fecha: a.fecha,
          estado: a.estado,
        })),
      },
      {
        name: 'movimientos_financieros',
        title: '9. movimientos_financieros',
        category: 'Finanzas y Auditoría',
        columns: [
          { name: 'id', type: 'SERIAL', key: 'PK' },
          { name: 'numero_comprobante', type: 'TEXT', key: 'UQ' },
          { name: 'proyecto_id', type: 'INTEGER', key: 'FK', references: 'proyectos.id' },
          { name: 'beca_id', type: 'INTEGER', key: 'FK', references: 'becas_otorgadas.id' },
          { name: 'administrativo_id', type: 'INTEGER', key: 'FK', references: 'administrativos.id' },
          { name: 'tipo', type: 'TEXT' },
          { name: 'concepto', type: 'TEXT' },
          { name: 'monto_bs', type: 'NUMERIC(18,2)' },
          { name: 'fecha_movimiento', type: 'TEXT' },
        ],
        rowCount: rowsMovs.length,
        rows: rowsMovs.map((m) => ({
          id: m.id,
          numero_comprobante: m.numeroComprobante,
          proyecto_id: m.proyectoId,
          beca_id: m.becaId ?? 'NULL',
          administrativo_id: m.administrativoId,
          tipo: m.tipo,
          concepto: m.concepto,
          monto_bs: m.montoBs,
          fecha_movimiento: m.fechaMovimiento,
        })),
      },
      {
        name: 'bitacora_auditoria',
        title: '10. bitacora_auditoria',
        category: 'Finanzas y Auditoría',
        columns: [
          { name: 'id', type: 'SERIAL', key: 'PK' },
          { name: 'usuario_id', type: 'INTEGER', key: 'FK', references: 'usuarios.id' },
          { name: 'accion', type: 'TEXT' },
          { name: 'modulo', type: 'TEXT' },
          { name: 'detalle', type: 'TEXT' },
          { name: 'fecha_hora', type: 'TIMESTAMP' },
        ],
        rowCount: rowsBitacora.length,
        rows: rowsBitacora.map((b) => ({
          id: b.id,
          usuario_id: b.usuarioId ?? 'NULL',
          accion: b.accion,
          modulo: b.modulo,
          detalle: b.detalle,
          fecha_hora: b.fechaHora.toISOString().replace('T', ' ').slice(0, 19),
        })),
      },
    ];

    const relationEdges: DbRelationEdge[] = [
      { id: 'fk-1', fromTable: 'usuarios', fromColumn: 'rol_id', toTable: 'roles', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Usuarios_Roles' },
      { id: 'fk-2', fromTable: 'usuarios', fromColumn: 'sede_id', toTable: 'sedes', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Usuarios_Sedes' },
      { id: 'fk-3', fromTable: 'administrativos', fromColumn: 'usuario_id', toTable: 'usuarios', toColumn: 'id', cardinality: '1:1', constraintName: 'FK_Administrativos_Usuarios' },
      { id: 'fk-4', fromTable: 'administrativos', fromColumn: 'sede_id', toTable: 'sedes', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Administrativos_Sedes' },
      { id: 'fk-5', fromTable: 'estudiantes', fromColumn: 'usuario_id', toTable: 'usuarios', toColumn: 'id', cardinality: '1:1', constraintName: 'FK_Estudiantes_Usuarios' },
      { id: 'fk-6', fromTable: 'estudiantes', fromColumn: 'sede_id', toTable: 'sedes', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Estudiantes_Sedes' },
      { id: 'fk-7', fromTable: 'proyectos', fromColumn: 'sede_id', toTable: 'sedes', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Proyectos_Sedes' },
      { id: 'fk-8', fromTable: 'proyectos', fromColumn: 'administrativo_id', toTable: 'administrativos', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Proyectos_Administrativos' },
      { id: 'fk-9', fromTable: 'becas_otorgadas', fromColumn: 'proyecto_id', toTable: 'proyectos', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Becas_Proyectos' },
      { id: 'fk-10', fromTable: 'becas_otorgadas', fromColumn: 'estudiante_id', toTable: 'estudiantes', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Becas_Estudiantes' },
      { id: 'fk-11', fromTable: 'becas_otorgadas', fromColumn: 'administrativo_id', toTable: 'administrativos', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Becas_Administrativos' },
      { id: 'fk-12', fromTable: 'actividades', fromColumn: 'proyecto_id', toTable: 'proyectos', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Actividades_Proyectos' },
      { id: 'fk-13', fromTable: 'actividades', fromColumn: 'sede_id', toTable: 'sedes', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Actividades_Sedes' },
      { id: 'fk-14', fromTable: 'actividades', fromColumn: 'administrativo_id', toTable: 'administrativos', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Actividades_Administrativos' },
      { id: 'fk-15', fromTable: 'movimientos_financieros', fromColumn: 'proyecto_id', toTable: 'proyectos', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Finanzas_Proyectos' },
      { id: 'fk-16', fromTable: 'movimientos_financieros', fromColumn: 'beca_id', toTable: 'becas_otorgadas', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Finanzas_Becas' },
      { id: 'fk-17', fromTable: 'movimientos_financieros', fromColumn: 'administrativo_id', toTable: 'administrativos', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Finanzas_Administrativos' },
      { id: 'fk-18', fromTable: 'bitacora_auditoria', fromColumn: 'usuario_id', toTable: 'usuarios', toColumn: 'id', cardinality: 'N:1', constraintName: 'FK_Bitacora_Usuarios' },
    ];

    return {
      databaseName: 'FundacionUnifranzDB (Cloud SQL PostgreSQL)',
      engine: 'PostgreSQL + Drizzle ORM',
      normalization: 'Tercera Forma Normal (3FN)',
      tableCount: tables.length,
      relationCount: relationEdges.length,
      generatedAt: new Date().toISOString(),
      tables,
      relations: relationEdges,
    };
  } catch (error) {
    console.error('Database query failed (getRelationalDatabaseSnapshot):', error);
    throw new Error('Database query failed. Please try again later.', { cause: error });
  }
}
