export interface GlobalInterfaceConfig {
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

export interface SyncedAppDataPayload {
  users?: unknown[];
  projects?: unknown[];
  records?: unknown[];
  scholarships?: unknown[];
  movements?: unknown[];
  dataVersion: number;
  updatedAt: string;
}

export const DEFAULT_GLOBAL_CONFIG: GlobalInterfaceConfig = {
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
  updatedAt: '2026-11-26T00:00:00.000Z',
};

const LOCAL_CONFIG_STORAGE_KEY = 'unifranz_global_interface_config_v1';

export function loadCachedGlobalConfig(): GlobalInterfaceConfig {
  try {
    const raw = window.localStorage.getItem(LOCAL_CONFIG_STORAGE_KEY);
    if (!raw) return DEFAULT_GLOBAL_CONFIG;
    return { ...DEFAULT_GLOBAL_CONFIG, ...(JSON.parse(raw) as Partial<GlobalInterfaceConfig>) };
  } catch {
    return DEFAULT_GLOBAL_CONFIG;
  }
}

export function saveCachedGlobalConfig(cfg: GlobalInterfaceConfig): void {
  try {
    window.localStorage.setItem(LOCAL_CONFIG_STORAGE_KEY, JSON.stringify(cfg));
  } catch {
    // Ignore storage quota errors
  }
}

export function applyGlobalThemeToDocument(cfg: GlobalInterfaceConfig): void {
  if (typeof document === 'undefined') return;
  const root = document.documentElement;
  root.style.setProperty('--color-unifranz-teal', cfg.colorPrimario);
  root.style.setProperty('--color-unifranz-teal-deep', cfg.colorSidebar);
  root.style.setProperty('--color-unifranz-coral', cfg.colorAcento);
  root.style.setProperty('--unifranz-primary', cfg.colorPrimario);
  root.style.setProperty('--unifranz-sidebar', cfg.colorSidebar);
  root.style.setProperty('--unifranz-accent', cfg.colorAcento);
  root.style.setProperty('--unifranz-bg', cfg.colorFondo);
  document.title = `${cfg.tituloSistema} — ${cfg.nombreInstitucion}`;
}

export async function fetchGlobalConfig(): Promise<GlobalInterfaceConfig | null> {
  try {
    const res = await fetch('/api/sync/config', { cache: 'no-store' });
    if (!res.ok) return null;
    const cfg = (await res.json()) as GlobalInterfaceConfig;
    saveCachedGlobalConfig(cfg);
    return cfg;
  } catch {
    return null;
  }
}

export async function publishGlobalConfig(
  patch: Partial<GlobalInterfaceConfig>,
  programmerPassword: string
): Promise<{ ok: boolean; config?: GlobalInterfaceConfig; error?: string }> {
  try {
    const res = await fetch('/api/sync/config', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'X-Programmer-Key': programmerPassword.trim(),
      },
      body: JSON.stringify(patch),
    });
    const body = (await res.json()) as {
      ok?: boolean;
      config?: GlobalInterfaceConfig;
      error?: string;
    };
    if (!res.ok || !body.ok || !body.config) {
      return {
        ok: false,
        error: body.error || 'No se pudo publicar la actualización global.',
      };
    }
    saveCachedGlobalConfig(body.config);
    applyGlobalThemeToDocument(body.config);
    return { ok: true, config: body.config };
  } catch {
    return {
      ok: false,
      error: 'Error de red al conectar con el servidor de sincronización.',
    };
  }
}

export async function fetchSyncedData(): Promise<SyncedAppDataPayload | null> {
  try {
    const res = await fetch('/api/sync/data', { cache: 'no-store' });
    if (!res.ok) return null;
    return (await res.json()) as SyncedAppDataPayload;
  } catch {
    return null;
  }
}

export async function pushSyncedData(
  payload: Omit<SyncedAppDataPayload, 'dataVersion' | 'updatedAt'>
): Promise<number | null> {
  try {
    const res = await fetch('/api/sync/data', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify(payload),
    });
    if (!res.ok) return null;
    const result = (await res.json()) as { ok?: boolean; dataVersion?: number };
    return result.ok && typeof result.dataVersion === 'number'
      ? result.dataVersion
      : null;
  } catch {
    return null;
  }
}

export function subscribeToGlobalSync(
  onConfig: (cfg: GlobalInterfaceConfig) => void,
  onData: (data: SyncedAppDataPayload) => void
): () => void {
  if (typeof window === 'undefined') return () => {};

  let closed = false;
  let source: EventSource | null = null;

  const connect = () => {
    if (closed || typeof EventSource === 'undefined') return;
    try {
      source = new EventSource('/api/sync/stream');
      source.addEventListener('config', (evt) => {
        try {
          const parsed = JSON.parse((evt as MessageEvent).data) as GlobalInterfaceConfig;
          saveCachedGlobalConfig(parsed);
          applyGlobalThemeToDocument(parsed);
          onConfig(parsed);
        } catch {
          // ignore malformed event
        }
      });
      source.addEventListener('data', (evt) => {
        try {
          const parsed = JSON.parse((evt as MessageEvent).data) as SyncedAppDataPayload;
          onData(parsed);
        } catch {
          // ignore malformed event
        }
      });
    } catch {
      // EventSource not available; polling fallback below handles sync
    }
  };

  connect();

  // Lightweight polling fallback every 15s (e.g. if SSE is buffered by a proxy)
  const timer = window.setInterval(async () => {
    if (closed) return;
    const cfg = await fetchGlobalConfig();
    if (cfg) {
      applyGlobalThemeToDocument(cfg);
      onConfig(cfg);
    }
    const data = await fetchSyncedData();
    if (data && data.dataVersion > 0) {
      onData(data);
    }
  }, 15000);

  return () => {
    closed = true;
    window.clearInterval(timer);
    source?.close();
  };
}
