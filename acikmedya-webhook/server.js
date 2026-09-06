'use strict';

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const express = require('express');

const PORT = process.env.PORT || 4001;
const TOKEN = process.env.WEBHOOK_TOKEN;
const TARGET_DIR = process.env.TARGET_DIR || '/data/acikmedya';
const NEWSLETTERS_CONFIG_PATH = process.env.NEWSLETTERS_CONFIG_PATH;
const NEWSLETTERS_DATA_DIR = process.env.NEWSLETTERS_DATA_DIR;

const RATE_LIMIT_WINDOW_MS = 60 * 1000;
const RATE_LIMIT_MAX = 10;
const MAX_BODY_BYTES = 256 * 1024; // 256 KB

if (!TOKEN) {
  console.error('FATAL: WEBHOOK_TOKEN environment variable is not set. Refusing to start.');
  process.exit(1);
}

if (!NEWSLETTERS_CONFIG_PATH) {
  console.error('FATAL: NEWSLETTERS_CONFIG_PATH environment variable is not set. Refusing to start.');
  process.exit(1);
}

if (!NEWSLETTERS_DATA_DIR) {
  console.error('FATAL: NEWSLETTERS_DATA_DIR environment variable is not set. Refusing to start.');
  process.exit(1);
}

let newsletterConfigRaw;
try {
  newsletterConfigRaw = fs.readFileSync(NEWSLETTERS_CONFIG_PATH, 'utf8');
} catch (err) {
  console.error(`FATAL: cannot read NEWSLETTERS_CONFIG_PATH at ${NEWSLETTERS_CONFIG_PATH}: ${err.message}`);
  process.exit(1);
}

let newsletterConfigParsed;
try {
  newsletterConfigParsed = JSON.parse(newsletterConfigRaw);
} catch (err) {
  console.error(`FATAL: NEWSLETTERS_CONFIG_PATH is not valid JSON: ${err.message}`);
  process.exit(1);
}

const foldersValid = Array.isArray(newsletterConfigParsed.folders)
  && newsletterConfigParsed.folders.length > 0
  && newsletterConfigParsed.folders.every((f) => typeof f === 'string' && f.length > 0);

if (!foldersValid) {
  console.error('FATAL: acikmedya-newsletters.json must be { "folders": string[] } (non-empty)');
  process.exit(1);
}

const ALLOWED_NEWSLETTER_FOLDERS = new Set(newsletterConfigParsed.folders);
console.log(JSON.stringify({ event: 'startup', allowedFolders: [...ALLOWED_NEWSLETTER_FOLDERS] }));

const FOLDER_NAME_RE = /^[A-Za-z0-9_-]+$/;

const app = express();

// Required because all traffic arrives via Caddy's reverse_proxy. Without this,
// req.ip resolves to Caddy's internal address and the per-IP rate limiter below
// collapses into a single shared global bucket.
app.set('trust proxy', true);

app.use(express.text({ type: 'text/html', limit: MAX_BODY_BYTES }));

// --- simple in-memory sliding-window rate limiter, keyed by client IP ---
const hits = new Map(); // ip -> { count, windowStart }

function isRateLimited(ip) {
  const now = Date.now();
  const entry = hits.get(ip);

  // Opportunistically evict stale entries to avoid unbounded memory growth.
  if (hits.size > 1000) {
    for (const [key, val] of hits) {
      if (now - val.windowStart > RATE_LIMIT_WINDOW_MS * 2) {
        hits.delete(key);
      }
    }
  }

  if (!entry || now - entry.windowStart > RATE_LIMIT_WINDOW_MS) {
    hits.set(ip, { count: 1, windowStart: now });
    return false;
  }

  entry.count += 1;
  if (entry.count > RATE_LIMIT_MAX) {
    return true;
  }
  return false;
}

function logEvent(fields) {
  console.log(JSON.stringify({ ts: new Date().toISOString(), ...fields }));
}

function tokensMatch(provided, expected) {
  const providedBuf = Buffer.from(provided, 'utf8');
  const expectedBuf = Buffer.from(expected, 'utf8');
  if (providedBuf.byteLength !== expectedBuf.byteLength) {
    return false;
  }
  return crypto.timingSafeEqual(providedBuf, expectedBuf);
}

// Atomically writes `content` to `finalPath` via a tmp-file-then-rename, so a
// concurrent reader never observes a partially-written file.
function atomicWriteHtml(dir, finalPath, content) {
  fs.mkdirSync(dir, { recursive: true });
  const tmpPath = path.join(dir, `.${path.basename(finalPath)}.tmp-${process.pid}`);
  fs.writeFileSync(tmpPath, content, 'utf8');
  fs.renameSync(tmpPath, finalPath);
}

// Computes today's date in Europe/Istanbul as a zero-padded DDMMYY string,
// regardless of the container's own system timezone.
function todayDDMMYY() {
  const parts = new Intl.DateTimeFormat('en-GB', {
    timeZone: 'Europe/Istanbul', day: '2-digit', month: '2-digit', year: '2-digit',
  }).formatToParts(new Date());
  const dd = parts.find((p) => p.type === 'day').value;
  const mm = parts.find((p) => p.type === 'month').value;
  const yy = parts.find((p) => p.type === 'year').value;
  return `${dd}${mm}${yy}`;
}

app.post('/hooks/acikmedya', (req, res) => {
  const ip = req.ip;

  if (isRateLimited(ip)) {
    logEvent({ ip, status: 429 });
    res.set('Retry-After', String(Math.ceil(RATE_LIMIT_WINDOW_MS / 1000)));
    return res.status(429).json({ error: 'rate limit exceeded' });
  }

  const authHeader = req.get('Authorization') || '';
  const match = /^Bearer (.+)$/.exec(authHeader);
  if (!match || !tokensMatch(match[1], TOKEN)) {
    logEvent({ ip, status: 401 });
    return res.status(401).json({ error: 'unauthorized' });
  }

  if (!req.is('text/html')) {
    logEvent({ ip, status: 400, reason: 'bad-content-type' });
    return res.status(400).json({ error: 'Content-Type must be text/html' });
  }

  const body = typeof req.body === 'string' ? req.body.trim() : '';
  if (!body || !/^(<!doctype html|<html)/i.test(body)) {
    logEvent({ ip, status: 400, reason: 'failed-sanity-check' });
    return res.status(400).json({ error: 'payload failed HTML sanity check' });
  }

  try {
    atomicWriteHtml(TARGET_DIR, path.join(TARGET_DIR, 'index.html'), body);
  } catch (err) {
    console.error('write failed:', err.message);
    return res.status(500).json({ error: 'write failed' });
  }

  logEvent({ ip, status: 200, bytes: body.length });
  return res.status(200).json({ status: 'ok', bytes: body.length });
});

app.post('/hooks/acikmedya/:folder', (req, res) => {
  const ip = req.ip;
  const { folder } = req.params;

  if (isRateLimited(ip)) {
    logEvent({ ip, status: 429, folder });
    res.set('Retry-After', String(Math.ceil(RATE_LIMIT_WINDOW_MS / 1000)));
    return res.status(429).json({ error: 'rate limit exceeded' });
  }

  const authHeader = req.get('Authorization') || '';
  const match = /^Bearer (.+)$/.exec(authHeader);
  if (!match || !tokensMatch(match[1], TOKEN)) {
    logEvent({ ip, status: 401, folder });
    return res.status(401).json({ error: 'unauthorized' });
  }

  if (!FOLDER_NAME_RE.test(folder)) {
    logEvent({ ip, status: 400, folder, reason: 'invalid-folder-name' });
    return res.status(400).json({ error: 'invalid folder name' });
  }

  if (!ALLOWED_NEWSLETTER_FOLDERS.has(folder)) {
    logEvent({ ip, status: 404, folder, reason: 'unknown-folder' });
    return res.status(404).json({ error: 'unknown newsletter folder' });
  }

  if (!req.is('text/html')) {
    logEvent({ ip, status: 400, folder, reason: 'bad-content-type' });
    return res.status(400).json({ error: 'Content-Type must be text/html' });
  }

  const body = typeof req.body === 'string' ? req.body.trim() : '';
  if (!body || !/^(<!doctype html|<html)/i.test(body)) {
    logEvent({ ip, status: 400, folder, reason: 'failed-sanity-check' });
    return res.status(400).json({ error: 'payload failed HTML sanity check' });
  }

  const filename = `index${todayDDMMYY()}.html`;
  const folderDir = path.join(NEWSLETTERS_DATA_DIR, folder);

  try {
    atomicWriteHtml(folderDir, path.join(folderDir, filename), body);
  } catch (err) {
    console.error('write failed:', err.message);
    logEvent({ ip, status: 500, folder, reason: 'write-failed' });
    return res.status(500).json({ error: 'write failed' });
  }

  logEvent({ ip, status: 200, folder, filename, bytes: body.length });
  return res.status(200).json({ status: 'ok', folder, filename, bytes: Buffer.byteLength(body) });
});

// Express's built-in body-parser error handler (e.g. payload too large) falls
// through to the default error middleware unless we intercept it here.
app.use((err, req, res, next) => {
  if (err && err.type === 'entity.too.large') {
    logEvent({ ip: req.ip, status: 413 });
    return res.status(413).json({ error: 'payload too large' });
  }
  console.error('unhandled error:', err && err.message);
  return res.status(500).json({ error: 'internal error' });
});

app.listen(PORT, '0.0.0.0', () => {
  console.log(`acikmedya-webhook listening on ${PORT}`);
});
