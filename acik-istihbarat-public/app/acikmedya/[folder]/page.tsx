import type { Metadata } from 'next';
import { notFound } from 'next/navigation';
import AcikMedyaLayout from '@/components/AcikMedyaLayout';
import AcikMedyaIframe from '@/components/AcikMedyaIframe';
import { isValidNewsletterFolder, resolveLatestNewsletterFile } from '@/lib/newsletters';

export const dynamic = 'force-dynamic';

export async function generateMetadata({ params }: { params: Promise<{ folder: string }> }): Promise<Metadata> {
  const { folder } = await params;
  return { title: `${folder} - Açık İstihbarat` };
}

export default async function NewsletterFolderPage({ params }: { params: Promise<{ folder: string }> }) {
  const { folder } = await params;
  if (!isValidNewsletterFolder(folder)) {
    notFound();
  }

  const resolved = resolveLatestNewsletterFile(folder);

  return (
    <AcikMedyaLayout>
      {resolved
        ? <AcikMedyaIframe html={resolved.html} title={folder} />
        : <p className="text-center py-20 text-slate-600 dark:text-slate-300">Bu bültenin henüz bir yayını yok.</p>}
    </AcikMedyaLayout>
  );
}
