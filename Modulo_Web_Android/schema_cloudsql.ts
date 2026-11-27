import { relations } from 'drizzle-orm';
import {
  boolean,
  integer,
  numeric,
  pgTable,
  serial,
  text,
  timestamp,
} from 'drizzle-orm/pg-core';

// 1. TABLA roles (Catálogo de roles: Administrativo / Estudiante)
export const roles = pgTable('roles', {
  id: serial('id').primaryKey(),
  nombre: text('nombre').notNull().unique(),
  descripcion: text('descripcion').notNull(),
});

// 2. TABLA sedes (Catálogo maestro de sedes UNIFRANZ)
export const sedes = pgTable('sedes', {
  id: serial('id').primaryKey(),
  nombre: text('nombre').notNull().unique(),
  ciudad: text('ciudad').notNull(),
  direccion: text('direccion').notNull(),
  activa: boolean('activa').notNull().default(true),
});

// 3. TABLA usuarios (Credenciales y vínculo Firebase UID: FK -> roles, FK -> sedes)
export const usuarios = pgTable('usuarios', {
  id: serial('id').primaryKey(),
  externalId: text('external_id').notNull().unique(),
  uid: text('uid').unique(),
  username: text('username').notNull().unique(),
  passwordHash: text('password_hash').notNull(),
  nombreCompleto: text('nombre_completo').notNull(),
  correo: text('correo').notNull().unique(),
  rolId: integer('rol_id')
    .notNull()
    .references(() => roles.id),
  sedeId: integer('sede_id')
    .notNull()
    .references(() => sedes.id),
  activo: boolean('activo').notNull().default(true),
  ultimoAcceso: text('ultimo_acceso').notNull().default('Sin accesos previos'),
  createdAt: timestamp('created_at').defaultNow(),
});

// 4. TABLA administrativos (Personal y coordinadores: FK -> usuarios, FK -> sedes)
export const administrativos = pgTable('administrativos', {
  id: serial('id').primaryKey(),
  usuarioId: integer('usuario_id')
    .notNull()
    .unique()
    .references(() => usuarios.id, { onDelete: 'cascade' }),
  sedeId: integer('sede_id')
    .notNull()
    .references(() => sedes.id),
  nombres: text('nombres').notNull(),
  apellidos: text('apellidos').notNull(),
  ci: text('ci').notNull().unique(),
  cargo: text('cargo').notNull(),
  telefono: text('telefono').notNull(),
});

// 5. TABLA estudiantes (Beneficiarios y postulantes: FK -> usuarios, FK -> sedes)
export const estudiantes = pgTable('estudiantes', {
  id: serial('id').primaryKey(),
  usuarioId: integer('usuario_id')
    .notNull()
    .unique()
    .references(() => usuarios.id, { onDelete: 'cascade' }),
  sedeId: integer('sede_id')
    .notNull()
    .references(() => sedes.id),
  nombres: text('nombres').notNull(),
  apellidos: text('apellidos').notNull(),
  ci: text('ci').notNull().unique(),
  codigoEstudiante: text('codigo_estudiante').notNull().unique(),
  carrera: text('carrera').notNull(),
  semestre: integer('semestre').notNull().default(1),
  promedio: numeric('promedio', { precision: 5, scale: 2 }).notNull().default('85.00'),
  telefono: text('telefono').notNull().default('+591 70000000'),
});

// 6. TABLA proyectos (Programas y convocatorias: FK -> sedes, FK -> administrativos)
export const proyectos = pgTable('proyectos', {
  id: serial('id').primaryKey(),
  externalId: text('external_id').notNull().unique(),
  codigo: text('codigo').notNull().unique(),
  nombre: text('nombre').notNull(),
  categoria: text('categoria').notNull(),
  sedeId: integer('sede_id')
    .notNull()
    .references(() => sedes.id),
  administrativoId: integer('administrativo_id')
    .notNull()
    .references(() => administrativos.id),
  responsableNombre: text('responsable_nombre').notNull(),
  fechaInicio: text('fecha_inicio').notNull(),
  fechaFin: text('fecha_fin').notNull(),
  beneficiariosMeta: integer('beneficiarios_meta').notNull().default(50),
  beneficiariosActuales: integer('beneficiarios_actuales').notNull().default(0),
  presupuestoBs: numeric('presupuesto_bs', { precision: 18, scale: 2 }).notNull().default('0.00'),
  ejecutadoBs: numeric('ejecutado_bs', { precision: 18, scale: 2 }).notNull().default('0.00'),
  estado: text('estado').notNull().default('Activo'),
  descripcion: text('descripcion').notNull().default(''),
});

// 7. TABLA becas_otorgadas (Postulaciones y becas: FK -> proyectos, estudiantes, administrativos)
export const becasOtorgadas = pgTable('becas_otorgadas', {
  id: serial('id').primaryKey(),
  externalId: text('external_id').notNull().unique(),
  codigo: text('codigo').notNull().unique(),
  proyectoId: integer('proyecto_id')
    .notNull()
    .references(() => proyectos.id, { onDelete: 'cascade' }),
  estudianteId: integer('estudiante_id')
    .notNull()
    .references(() => estudiantes.id, { onDelete: 'cascade' }),
  administrativoId: integer('administrativo_id').references(() => administrativos.id, {
    onDelete: 'set null',
  }),
  evaluadorNombre: text('evaluador_nombre').notNull().default('Pendiente de revisión'),
  porcentajeBeca: integer('porcentaje_beca').notNull().default(50),
  montoAsignadoBs: numeric('monto_asignado_bs', { precision: 18, scale: 2 }).notNull().default('0.00'),
  fechaSolicitud: text('fecha_solicitud').notNull(),
  estado: text('estado').notNull().default('Pendiente'),
  observacion: text('observacion').notNull().default(''),
});

// 8. TABLA actividades (Eventos, ferias y brigadas: FK -> proyectos, sedes, administrativos)
export const actividades = pgTable('actividades', {
  id: serial('id').primaryKey(),
  externalId: text('external_id').notNull().unique(),
  codigo: text('codigo').notNull().unique(),
  proyectoId: integer('proyecto_id')
    .notNull()
    .references(() => proyectos.id, { onDelete: 'cascade' }),
  sedeId: integer('sede_id')
    .notNull()
    .references(() => sedes.id),
  administrativoId: integer('administrativo_id')
    .notNull()
    .references(() => administrativos.id),
  responsableNombre: text('responsable_nombre').notNull(),
  categoria: text('categoria').notNull(),
  titulo: text('titulo').notNull(),
  beneficiarios: integer('beneficiarios').notNull().default(0),
  fecha: text('fecha').notNull(),
  estado: text('estado').notNull().default('Activo'),
  esEventoDelMes: boolean('es_evento_del_mes').notNull().default(false),
  descripcion: text('descripcion').notNull().default(''),
});

// 9. TABLA movimientos_financieros (Ingresos y desembolsos: FK -> proyectos, becas, administrativos)
export const movimientosFinancieros = pgTable('movimientos_financieros', {
  id: serial('id').primaryKey(),
  externalId: text('external_id').notNull().unique(),
  numeroComprobante: text('numero_comprobante').notNull().unique(),
  proyectoId: integer('proyecto_id')
    .notNull()
    .references(() => proyectos.id, { onDelete: 'cascade' }),
  becaId: integer('beca_id').references(() => becasOtorgadas.id, { onDelete: 'set null' }),
  administrativoId: integer('administrativo_id')
    .notNull()
    .references(() => administrativos.id),
  responsableNombre: text('responsable_nombre').notNull(),
  tipo: text('tipo').notNull(),
  concepto: text('concepto').notNull(),
  montoBs: numeric('monto_bs', { precision: 18, scale: 2 }).notNull(),
  fechaMovimiento: text('fecha_movimiento').notNull(),
});

// 10. TABLA bitacora_auditoria (Historial de acciones del sistema: FK -> usuarios)
export const bitacoraAuditoria = pgTable('bitacora_auditoria', {
  id: serial('id').primaryKey(),
  usuarioId: integer('usuario_id').references(() => usuarios.id, { onDelete: 'set null' }),
  accion: text('accion').notNull(),
  modulo: text('modulo').notNull(),
  detalle: text('detalle').notNull(),
  fechaHora: timestamp('fecha_hora').notNull().defaultNow(),
});

// Tabla auxiliar de configuración global del Programador (sincronización UI en vivo)
export const configuracionGlobal = pgTable('configuracion_global', {
  id: serial('id').primaryKey(),
  nombreInstitucion: text('nombre_institucion').notNull(),
  tituloSistema: text('titulo_sistema').notNull(),
  subtituloLogin: text('subtitulo_login').notNull(),
  mensajeBienvenida: text('mensaje_bienvenida').notNull(),
  avisoGlobal: text('aviso_global').notNull().default(''),
  colorPrimario: text('color_primario').notNull(),
  colorSidebar: text('color_sidebar').notNull(),
  colorAcento: text('color_acento').notNull(),
  colorFondo: text('color_fondo').notNull(),
  sedesActivasJson: text('sedes_activas_json').notNull(),
  version: integer('version').notNull().default(1),
  dataVersion: integer('data_version').notNull().default(1),
  updatedAt: timestamp('updated_at').notNull().defaultNow(),
});

// ============================================================================
// RELACIONES DRIZZLE (Esquema Relacional 3FN)
// ============================================================================
export const rolesRelations = relations(roles, ({ many }) => ({
  usuarios: many(usuarios),
}));

export const sedesRelations = relations(sedes, ({ many }) => ({
  usuarios: many(usuarios),
  administrativos: many(administrativos),
  estudiantes: many(estudiantes),
  proyectos: many(proyectos),
  actividades: many(actividades),
}));

export const usuariosRelations = relations(usuarios, ({ one, many }) => ({
  rol: one(roles, {
    fields: [usuarios.rolId],
    references: [roles.id],
  }),
  sede: one(sedes, {
    fields: [usuarios.sedeId],
    references: [sedes.id],
  }),
  administrativo: one(administrativos, {
    fields: [usuarios.id],
    references: [administrativos.usuarioId],
  }),
  estudiante: one(estudiantes, {
    fields: [usuarios.id],
    references: [estudiantes.usuarioId],
  }),
  bitacora: many(bitacoraAuditoria),
}));

export const administrativosRelations = relations(administrativos, ({ one, many }) => ({
  usuario: one(usuarios, {
    fields: [administrativos.usuarioId],
    references: [usuarios.id],
  }),
  sede: one(sedes, {
    fields: [administrativos.sedeId],
    references: [sedes.id],
  }),
  proyectos: many(proyectos),
  becasEvaluadas: many(becasOtorgadas),
  actividades: many(actividades),
  movimientos: many(movimientosFinancieros),
}));

export const estudiantesRelations = relations(estudiantes, ({ one, many }) => ({
  usuario: one(usuarios, {
    fields: [estudiantes.usuarioId],
    references: [usuarios.id],
  }),
  sede: one(sedes, {
    fields: [estudiantes.sedeId],
    references: [sedes.id],
  }),
  becas: many(becasOtorgadas),
}));

export const proyectosRelations = relations(proyectos, ({ one, many }) => ({
  sede: one(sedes, {
    fields: [proyectos.sedeId],
    references: [sedes.id],
  }),
  administrativo: one(administrativos, {
    fields: [proyectos.administrativoId],
    references: [administrativos.id],
  }),
  becas: many(becasOtorgadas),
  actividades: many(actividades),
  movimientos: many(movimientosFinancieros),
}));

export const becasOtorgadasRelations = relations(becasOtorgadas, ({ one, many }) => ({
  proyecto: one(proyectos, {
    fields: [becasOtorgadas.proyectoId],
    references: [proyectos.id],
  }),
  estudiante: one(estudiantes, {
    fields: [becasOtorgadas.estudianteId],
    references: [estudiantes.id],
  }),
  administrativo: one(administrativos, {
    fields: [becasOtorgadas.administrativoId],
    references: [administrativos.id],
  }),
  movimientos: many(movimientosFinancieros),
}));

export const actividadesRelations = relations(actividades, ({ one }) => ({
  proyecto: one(proyectos, {
    fields: [actividades.proyectoId],
    references: [proyectos.id],
  }),
  sede: one(sedes, {
    fields: [actividades.sedeId],
    references: [sedes.id],
  }),
  administrativo: one(administrativos, {
    fields: [actividades.administrativoId],
    references: [administrativos.id],
  }),
}));

export const movimientosFinancierosRelations = relations(movimientosFinancieros, ({ one }) => ({
  proyecto: one(proyectos, {
    fields: [movimientosFinancieros.proyectoId],
    references: [proyectos.id],
  }),
  beca: one(becasOtorgadas, {
    fields: [movimientosFinancieros.becaId],
    references: [becasOtorgadas.id],
  }),
  administrativo: one(administrativos, {
    fields: [movimientosFinancieros.administrativoId],
    references: [administrativos.id],
  }),
}));

export const bitacoraAuditoriaRelations = relations(bitacoraAuditoria, ({ one }) => ({
  usuario: one(usuarios, {
    fields: [bitacoraAuditoria.usuarioId],
    references: [usuarios.id],
  }),
}));
