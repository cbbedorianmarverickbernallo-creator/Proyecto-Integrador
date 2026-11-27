import 'dotenv/config';
import crypto from 'node:crypto';
import fs from 'node:fs';
import path from 'node:path';
import express, { type Request, type Response } from 'express';
import { createServer as createViteServer } from 'vite';
import {
  DEFAULT_GLOBAL_CONFIG_ROW,
  type GlobalInterfaceConfigRow,
  authenticateInstitutionalUser,
  getGlobalConfigFromDb,
  getRelationalDatabaseSnapshot,
  linkOrCreateFirebaseUser,
  loadAppDataFromDb,
  saveGlobalConfigToDb,
  syncAppDataToDb,
} from './src/db/repository.ts';
import { requireAuth, type AuthRequest } from './src/middleware/auth.ts';
import type {
  ActivityRecord,
  FinanceMovement,
  FoundationProject,
  ScholarshipAward,
  SystemUser,
  UserRole,
} from './src/types/foundation.ts';

const PORT = Number(process.env.PORT || 3000);

// Established SHA-256 digests for the exclusive Programmer password (never stored in plaintext)
const DEFAULT_PROGRAMMER_HASHES = [
  'b8204410e7c87545e67820127ef20d674ec0327180ddf14eb780304101c632ed',
  '3b60f8f8aeef0374932c439a463685c30fc5fd1dbfb58a7ed1b76fe71639b895',
];

const HEX_COLOR_RE = /^#[0-9a-fA-F]{6}$/;

function sanitizeText(input: unknown, fallback: string, maxLen: number): string {
  if (typeof input !== 'string') return fallback;
  const cleaned = input.replace(/[\u0000-\u001F\u007F<>]/g, '').trim();
  return cleaned.slice(0, maxLen);
}

function sanitizeColor(input: unknown, fallback: string): string {
  if (typeof input !== 'string') return fallback;
  const trimmed = input.trim();
  return HEX_COLOR_RE.test(trimmed) ? trimmed.toUpperCase() : fallback;
}

function sanitizeConfigPatch(
  raw: unknown,
  current: GlobalInterfaceConfigRow
): GlobalInterfaceConfigRow {
  const obj =
    raw && typeof raw === 'object' && !Array.isArray(raw)
      ? (raw as Record<string, unknown>)
      : {};

  const sedes = Array.isArray(obj.sedesActivas)
    ? obj.sedesActivas
        .map((s) => sanitizeText(s, '', 40))
        .filter((s) => s.length > 0)
        .slice(0, 12)
    : current.sedesActivas;

  return {
    nombreInstitucion:
      sanitizeText(obj.nombreInstitucion, current.nombreInstitucion, 80) ||
      current.nombreInstitucion,
    tituloSistema:
      sanitizeText(obj.tituloSistema, current.tituloSistema, 80) ||
      current.tituloSistema,
    subtituloLogin:
      sanitizeText(obj.subtituloLogin, current.subtituloLogin, 180) ||
      current.subtituloLogin,
    mensajeBienvenida:
      sanitizeText(obj.mensajeBienvenida, current.mensajeBienvenida, 80) ||
      current.mensajeBienvenida,
    avisoGlobal:
      typeof obj.avisoGlobal === 'string'
        ? sanitizeText(obj.avisoGlobal, '', 220)
        : current.avisoGlobal,
    colorPrimario: sanitizeColor(obj.colorPrimario, current.colorPrimario),
    colorSidebar: sanitizeColor(obj.colorSidebar, current.colorSidebar),
    colorAcento: sanitizeColor(obj.colorAcento, current.colorAcento),
    colorFondo: sanitizeColor(obj.colorFondo, current.colorFondo),
    sedesActivas: sedes.length > 0 ? sedes : current.sedesActivas,
    version: current.version + 1,
    updatedAt: new Date().toISOString(),
  };
}

function verifyProgrammerKey(candidate: unknown): boolean {
  if (typeof candidate !== 'string') return false;
  const trimmed = candidate.trim();
  if (!trimmed || trimmed.length > 256) return false;

  const candidateDigest = crypto
    .createHash('sha256')
    .update(trimmed, 'utf-8')
    .digest();

  const configuredHash = (process.env.PROGRAMMER_PASS_HASH || '').trim().toLowerCase();
  const validHexHashes = configuredHash
    ? [configuredHash]
    : DEFAULT_PROGRAMMER_HASHES;

  for (const hex of validHexHashes) {
    if (!/^[0-9a-f]{64}$/.test(hex)) continue;
    const expected = Buffer.from(hex, 'hex');
    if (
      expected.length === candidateDigest.length &&
      crypto.timingSafeEqual(expected, candidateDigest)
    ) {
      return true;
    }
  }
  return false;
}

const sseClients = new Set<Response>();

function broadcastEvent(eventName: string, payload: unknown): void {
  const wire = `event: ${eventName}\ndata: ${JSON.stringify(payload)}\n\n`;
  for (const client of sseClients) {
    try {
      client.write(wire);
    } catch {
      sseClients.delete(client);
    }
  }
}

async function startServer() {
  const app = express();
  app.use(express.json({ limit: '2mb' }));

  // Institutional login verified directly against Cloud SQL PostgreSQL (PBKDF2-SHA256)
  app.post('/api/auth/login', async (req: Request, res: Response) => {
    try {
      const body =
        req.body && typeof req.body === 'object'
          ? (req.body as Record<string, unknown>)
          : {};
      const identifier = typeof body.identifier === 'string' ? body.identifier : '';
      const password = typeof body.password === 'string' ? body.password : '';
      const role =
        body.role === 'Administrativo' || body.role === 'Estudiante'
          ? (body.role as UserRole)
          : undefined;

      const result = await authenticateInstitutionalUser(identifier, password, role);
      if (!result.ok) {
        res.status(401).json(result);
        return;
      }

      res.json(result);
    } catch (error: unknown) {
      console.error('Failed institutional login:', error);
      res.status(500).json({
        ok: false,
        error: 'No se pudo verificar el acceso en la base de datos. Intenta nuevamente.',
      });
    }
  });

  // Firebase Auth protected route: links Google Sign-In identity to PostgreSQL usuarios table
  app.post('/api/auth/google-link', requireAuth, async (req: AuthRequest, res: Response) => {
    try {
      const uid = req.user?.uid;
      const email = req.user?.email;
      const name = typeof req.user?.name === 'string' ? req.user.name : undefined;
      if (!uid || !email) {
        res.status(400).json({ error: 'Token de Google sin correo verificado.' });
        return;
      }
      const linked = await linkOrCreateFirebaseUser(uid, email, name);
      res.json({
        ok: true,
        user: {
          id: linked.id,
          username: linked.username,
          correo: linked.correo,
          nombreCompleto: linked.nombreCompleto,
          uid: linked.uid,
        },
      });
    } catch (error: unknown) {
      console.error('Failed Google account link:', error);
      res.status(500).json({ error: 'No se pudo vincular la cuenta de Google.' });
    }
  });

  // Programmer key verification helper
  app.post('/api/auth/programmer-verify', (req: Request, res: Response) => {
    const candidate =
      req.body && typeof req.body === 'object'
        ? (req.body as Record<string, unknown>).password
        : '';
    const valid = verifyProgrammerKey(candidate);
    res.json({ valid });
  });

  // Global UI configuration backed by Cloud SQL PostgreSQL
  app.get('/api/sync/config', async (_req: Request, res: Response) => {
    res.setHeader('Cache-Control', 'no-store');
    try {
      const cfg = await getGlobalConfigFromDb();
      res.json(cfg);
    } catch (error: unknown) {
      console.error('Failed to read global config:', error);
      res.status(500).json({
        ...DEFAULT_GLOBAL_CONFIG_ROW,
        error: 'No se pudo leer la configuración desde Cloud SQL.',
      });
    }
  });

  // Programmer-only live interface publish endpoint (persisted to Cloud SQL + SSE broadcast)
  app.post('/api/sync/config', async (req: Request, res: Response) => {
    const keyHeader = req.header('x-programmer-key');
    if (!verifyProgrammerKey(keyHeader)) {
      res.status(401).json({
        error:
          'Clave de Programador inválida. Solo el programador autorizado puede publicar cambios globales.',
      });
      return;
    }

    try {
      const current = await getGlobalConfigFromDb();
      const next = sanitizeConfigPatch(req.body, current);
      const saved = await saveGlobalConfigToDb(next);
      broadcastEvent('config', saved);
      res.json({ ok: true, config: saved });
    } catch (error: unknown) {
      console.error('Failed to publish global config:', error);
      res.status(500).json({
        error: 'No se pudo guardar la configuración global en la base de datos.',
      });
    }
  });

  // Shared operational data sync backed by the 10 normalized Cloud SQL tables
  app.get('/api/sync/data', async (_req: Request, res: Response) => {
    res.setHeader('Cache-Control', 'no-store');
    try {
      const data = await loadAppDataFromDb();
      res.json(data);
    } catch (error: unknown) {
      console.error('Failed to load synced data from Cloud SQL:', error);
      res.status(500).json({ error: 'No se pudieron cargar los datos desde Cloud SQL.' });
    }
  });

  app.post('/api/sync/data', async (req: Request, res: Response) => {
    const body =
      req.body && typeof req.body === 'object' && !Array.isArray(req.body)
        ? (req.body as Record<string, unknown>)
        : null;
    if (!body) {
      res.status(400).json({ error: 'Carga de datos inválida.' });
      return;
    }

    try {
      const result = await syncAppDataToDb({
        users: Array.isArray(body.users) ? (body.users as SystemUser[]) : undefined,
        projects: Array.isArray(body.projects)
          ? (body.projects as FoundationProject[])
          : undefined,
        records: Array.isArray(body.records)
          ? (body.records as ActivityRecord[])
          : undefined,
        scholarships: Array.isArray(body.scholarships)
          ? (body.scholarships as ScholarshipAward[])
          : undefined,
        movements: Array.isArray(body.movements)
          ? (body.movements as FinanceMovement[])
          : undefined,
        actorLabel:
          typeof body.actorLabel === 'string'
            ? body.actorLabel.slice(0, 80)
            : 'Aplicación conectada',
      });

      const fresh = await loadAppDataFromDb();
      broadcastEvent('data', fresh);
      res.json({
        ok: true,
        dataVersion: result.dataVersion,
        updatedAt: result.updatedAt,
      });
    } catch (error: unknown) {
      console.error('Failed to sync data to Cloud SQL:', error);
      res.status(500).json({ error: 'No se pudieron guardar los cambios en Cloud SQL.' });
    }
  });

  // Live Relational ER Diagram & 10-Table Inspector (protected with Programmer password)
  app.get('/api/db/schema-snapshot', async (req: Request, res: Response) => {
    res.setHeader('Cache-Control', 'no-store');
    const keyHeader = req.header('x-programmer-key');
    if (!verifyProgrammerKey(keyHeader)) {
      res.status(401).json({
        error:
          'Acceso restringido. Ingresa la clave maestra de Programador para ver la base de datos y el diagrama relacional.',
      });
      return;
    }

    try {
      const snapshot = await getRelationalDatabaseSnapshot();
      res.json(snapshot);
    } catch (error: unknown) {
      console.error('Failed to read relational snapshot:', error);
      res.status(500).json({ error: 'No se pudo consultar el esquema relacional.' });
    }
  });

  // Protected Final Word Report (.docx) download (requires Programmer password)
  app.get('/api/programmer/report-word', (req: Request, res: Response) => {
    res.setHeader('Cache-Control', 'no-store');
    const keyHeader = req.header('x-programmer-key');
    if (!verifyProgrammerKey(keyHeader)) {
      res.status(401).json({
        error:
          'Acceso restringido. Ingresa la clave maestra de Programador para descargar el informe final en Word.',
      });
      return;
    }

    const reportPath = path.resolve(
      process.cwd(),
      'data',
      'Informe_Final_ZERION_Fundacion_UNIFRANZ.docx'
    );
    if (!fs.existsSync(reportPath)) {
      res.status(404).json({
        error: 'El documento Word final no se encuentra disponible en el servidor.',
      });
      return;
    }

    res.setHeader(
      'Content-Type',
      'application/vnd.openxmlformats-officedocument.wordprocessingml.document'
    );
    res.setHeader(
      'Content-Disposition',
      'attachment; filename="Informe_Final_ZERION_Fundacion_UNIFRANZ.docx"'
    );
    res.sendFile(reportPath);
  });

  // Real-time Server-Sent Events stream for instant multi-device updates
  app.get('/api/sync/stream', async (req: Request, res: Response) => {
    res.setHeader('Content-Type', 'text/event-stream');
    res.setHeader('Cache-Control', 'no-cache, no-transform');
    res.setHeader('Connection', 'keep-alive');
    res.flushHeaders?.();

    try {
      const [cfg, data] = await Promise.all([
        getGlobalConfigFromDb(),
        loadAppDataFromDb(),
      ]);
      res.write(`event: config\ndata: ${JSON.stringify(cfg)}\n\n`);
      res.write(`event: data\ndata: ${JSON.stringify(data)}\n\n`);
    } catch (error) {
      console.error('Initial SSE snapshot load failed:', error);
    }

    sseClients.add(res);

    const heartbeat = setInterval(() => {
      try {
        res.write(': keepalive\n\n');
      } catch {
        clearInterval(heartbeat);
        sseClients.delete(res);
      }
    }, 25000);

    req.on('close', () => {
      clearInterval(heartbeat);
      sseClients.delete(res);
    });
  });

  if (process.env.NODE_ENV !== 'production') {
    const vite = await createViteServer({
      server: { middlewareMode: true },
      appType: 'spa',
    });
    app.use(vite.middlewares);
  } else {
    const distDir = path.resolve(process.cwd(), 'dist');
    app.use(express.static(distDir));
    app.get('*', (_req: Request, res: Response) => {
      res.sendFile(path.resolve(distDir, 'index.html'));
    });
  }

  app.listen(PORT, '0.0.0.0', () => {
    console.log(`Fundación UNIFRANZ Cloud SQL sync server running on http://0.0.0.0:${PORT}`);
  });
}

startServer();
