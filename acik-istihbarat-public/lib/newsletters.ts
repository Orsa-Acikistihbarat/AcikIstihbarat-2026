import fs from 'fs';
import path from 'path';

// Zero-padded DD/MM/YY, no separators, e.g. index060926.html — must match the
// exact pattern the webhook writes (acikmedya-webhook/server.js todayDDMMYY()).
const NEWSLETTER_FILENAME_RE = /^index(\d{2})(\d{2})(\d{2})\.html$/;
const FOLDER_NAME_RE = /^[A-Za-z0-9_-]+$/;

let cachedFolders: string[] | null = null;

function getConfigPath(): string {
  const configPath = process.env.NEWSLETTERS_CONFIG_PATH;
  if (!configPath) {
    throw new Error('NEWSLETTERS_CONFIG_PATH environment variable is not set');
  }
  return configPath;
}

function getDataDir(): string {
  const dataDir = process.env.NEWSLETTERS_DATA_DIR;
  if (!dataDir) {
    throw new Error('NEWSLETTERS_DATA_DIR environment variable is not set');
  }
  return dataDir;
}

export function getNewsletterFolders(): string[] {
  if (cachedFolders) {
    return cachedFolders;
  }

  const raw = fs.readFileSync(getConfigPath(), 'utf8');
  const parsed = JSON.parse(raw);

  const foldersValid = Array.isArray(parsed.folders)
    && parsed.folders.length > 0
    && parsed.folders.every((f: unknown) => typeof f === 'string' && f.length > 0);

  if (!foldersValid) {
    throw new Error('NEWSLETTERS_CONFIG_PATH must contain { "folders": string[] } (non-empty)');
  }

  cachedFolders = parsed.folders;
  return cachedFolders as string[];
}

export function isValidNewsletterFolder(folder: string): boolean {
  return FOLDER_NAME_RE.test(folder) && getNewsletterFolders().includes(folder);
}

export function resolveLatestNewsletterFile(folder: string): { filename: string; html: string } | null {
  const folderDir = path.join(getDataDir(), folder);

  let entries: string[];
  try {
    entries = fs.readdirSync(folderDir);
  } catch (e) {
    if ((e as NodeJS.ErrnoException).code === 'ENOENT') {
      return null;
    }
    throw e;
  }

  const parts = new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Europe/Istanbul', day: '2-digit', month: '2-digit', year: '2-digit',
  }).formatToParts(new Date());
  const todayDD = parts.find((p) => p.type === 'day')!.value;
  const todayMM = parts.find((p) => p.type === 'month')!.value;
  const todayYY = parts.find((p) => p.type === 'year')!.value;
  // 2-digit year assumed to be 20XX - known limitation, not a near-term concern.
  const todayValue = Number(`20${todayYY}${todayMM}${todayDD}`);

  let bestFilename: string | null = null;
  let bestValue = -1;

  for (const entry of entries) {
    const match = NEWSLETTER_FILENAME_RE.exec(entry);
    if (!match) {
      continue;
    }
    const [, dd, mm, yy] = match;
    const value = Number(`20${yy}${mm}${dd}`);
    if (value > todayValue) {
      continue; // future-dated file, ignore
    }
    if (value === todayValue) {
      bestFilename = entry;
      bestValue = value;
      break; // exact match for today wins outright
    }
    if (value > bestValue) {
      bestValue = value;
      bestFilename = entry;
    }
  }

  if (!bestFilename) {
    return null;
  }

  const html = fs.readFileSync(path.join(folderDir, bestFilename), 'utf8');
  return { filename: bestFilename, html };
}
