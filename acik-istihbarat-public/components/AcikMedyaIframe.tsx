'use client';

import { useEffect, useRef, useState } from 'react';

interface AcikMedyaIframeProps {
  html: string;
  title: string;
}

export default function AcikMedyaIframe({ html, title }: AcikMedyaIframeProps) {
  const [height, setHeight] = useState<number>(0);
  const [resizeFailed, setResizeFailed] = useState(false);
  const iframeRef = useRef<HTMLIFrameElement>(null);

  useEffect(() => {
    const iframe = iframeRef.current;
    if (!iframe) {
      return;
    }

    let ro: ResizeObserver | null = null;

    function handleLoad() {
      const doc = iframe?.contentWindow?.document;
      if (!doc) {
        // Sandbox/cross-origin quirk blocked access - fail safe to a fixed
        // height with internal scroll instead of crashing/blank page.
        setResizeFailed(true);
        return;
      }
      const measure = () => setHeight(doc.documentElement.scrollHeight || doc.body.scrollHeight);
      measure();
      ro = new ResizeObserver(measure);
      ro.observe(doc.body);
    }

    iframe.addEventListener('load', handleLoad);
    return () => {
      iframe.removeEventListener('load', handleLoad);
      ro?.disconnect();
    };
  }, [html]);

  return (
    <div style={{ width: '100%', overflowX: 'hidden' }}>
      <iframe
        ref={iframeRef}
        srcDoc={html}
        title={title}
        sandbox="allow-same-origin allow-popups allow-popups-to-escape-sandbox"
        style={{
          width: '100%',
          border: 'none',
          display: 'block',
          height: resizeFailed ? '80vh' : (height || 'auto'),
          overflow: resizeFailed ? 'auto' : 'hidden',
        }}
      />
    </div>
  );
}
