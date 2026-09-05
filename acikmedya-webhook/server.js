'use strict';

const crypto = require('crypto');
const fs = require('fs');
const path = require('path');
const express = require('express');

const PORT = process.env.PORT || 4001;
const TOKEN = process.env.WEBHOOK_TOKEN;
const TARGET_DIR = process.env.TARGET_DIR || '/data/acikmedya';

const RATE_LIMIT_WINDOW_MS = 60 * 1000;
const RATE_LIMIT_MAX = 10;
const MAX_BODY_BYTES = 256 * 1024; // 256 KB

if (!TOKEN) {
  console.error('FATAL: WEBHOOK_TOKEN environment variable is not set. Refusing to start.');
  process.exit(1);
}

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
    fs.mkdirSync(TARGET_DIR, { recursive: true });
    const tmpPath = path.join(TARGET_DIR, `.index.html.tmp-${process.pid}`);
    const finalPath = path.join(TARGET_DIR, 'index.html');
    fs.writeFileSync(tmpPath, body, 'utf8');
    fs.renameSync(tmpPath, finalPath);
  } catch (err) {
    console.error('write failed:', err.message);
    return res.status(500).json({ error: 'write failed' });
  }

  logEvent({ ip, status: 200, bytes: body.length });
  return res.status(200).json({ status: 'ok', bytes: body.length });
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
