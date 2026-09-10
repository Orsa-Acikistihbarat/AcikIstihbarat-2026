'use client';

import { useState } from 'react';
import { fetchApi } from '@/lib/api';
import { resolveNewsletterLabel } from '@/lib/newsletterLabels';

type SubscribeResultItem = {
  templateBaseName: string;
  success: boolean;
  errorMessage: string | null;
};

type SubscribeResponse = {
  results: SubscribeResultItem[];
};

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

export default function NewsletterSubscribeForm({ folders }: { folders: string[] }) {
  const [email, setEmail] = useState('');
  const [emailError, setEmailError] = useState<string | null>(null);
  const [selectModalOpen, setSelectModalOpen] = useState(false);
  const [summaryModalOpen, setSummaryModalOpen] = useState(false);
  const [selected, setSelected] = useState<Set<string>>(new Set());
  const [selectionError, setSelectionError] = useState<string | null>(null);
  const [submitError, setSubmitError] = useState<string | null>(null);
  const [submitting, setSubmitting] = useState(false);
  const [itemErrors, setItemErrors] = useState<SubscribeResultItem[]>([]);
  const [successNames, setSuccessNames] = useState<string[]>([]);

  function handleOpenClick() {
    if (!EMAIL_PATTERN.test(email)) {
      setEmailError('Geçerli bir e-posta adresi girin.');
      return;
    }
    setEmailError(null);
    setSelectionError(null);
    setSelectModalOpen(true);
  }

  async function handleSubscribeSubmit() {
    if (selected.size === 0) {
      setSelectionError('En az bir bülten seçin.');
      return;
    }
    setSelectionError(null);
    setSubmitError(null);
    setSubmitting(true);
    try {
      const res = await fetchApi<SubscribeResponse>('/mail/subscribe', {
        method: 'POST',
        body: JSON.stringify({ email, templateBaseNames: [...selected] }),
      });
      const succeeded = res.results.filter((r) => r.success).map((r) => r.templateBaseName);
      const failed = res.results.filter((r) => !r.success);
      setSuccessNames(succeeded.map(resolveNewsletterLabel));
      setItemErrors(failed);
      setSelectModalOpen(false);
      setSummaryModalOpen(true);
    } catch {
      setSubmitError('Bir hata oluştu, lütfen tekrar deneyin.');
    } finally {
      setSubmitting(false);
    }
  }

  function closeSummaryModal() {
    setSummaryModalOpen(false);
    setSelected(new Set());
    setEmail('');
  }

  return (
    <>
      <form
        className="flex items-center gap-2 shrink-0"
        onSubmit={(e) => e.preventDefault()}
      >
        <div className="flex flex-col">
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="E-posta adresiniz"
            className="py-2 px-3 rounded-lg text-sm border border-slate-200 bg-white/50 text-slate-700 focus:outline-none focus:ring-2 focus:ring-turquoise-500 dark:bg-slate-900/50 dark:border-slate-700 dark:text-slate-300"
          />
          {emailError && <span className="text-xs text-bordeaux-500 mt-1">{emailError}</span>}
        </div>
        <button
          type="button"
          onClick={handleOpenClick}
          className="py-2 px-4 rounded-lg text-sm font-heading font-semibold text-white bg-turquoise-600 hover:bg-turquoise-700 transition-colors dark:bg-turquoise-700 dark:hover:bg-turquoise-600"
        >
          Abone Ol
        </button>
      </form>

      {selectModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-md rounded-xl bg-white dark:bg-slate-900 p-6 shadow-lg">
            <h3 className="text-lg font-heading font-semibold text-slate-900 dark:text-white mb-4">
              Abone olmak istediğiniz bültenleri seçin
            </h3>
            <div className="flex flex-col gap-3 mb-4">
              {folders.map((folder) => (
                <label key={folder} className="flex items-center gap-2 text-sm text-slate-700 dark:text-slate-300">
                  <input
                    type="checkbox"
                    checked={selected.has(folder)}
                    onChange={(e) =>
                      setSelected((prev) => {
                        const next = new Set(prev);
                        if (e.target.checked) {
                          next.add(folder);
                        } else {
                          next.delete(folder);
                        }
                        return next;
                      })
                    }
                  />
                  {resolveNewsletterLabel(folder)}
                </label>
              ))}
            </div>
            {selectionError && <p className="text-xs text-bordeaux-500 mb-2">{selectionError}</p>}
            {submitError && <p className="text-xs text-bordeaux-500 mb-2">{submitError}</p>}
            <div className="flex justify-end gap-2">
              <button
                type="button"
                onClick={() => setSelectModalOpen(false)}
                className="py-2 px-4 rounded-lg text-sm font-heading font-medium text-slate-600 dark:text-slate-400"
              >
                Vazgeç
              </button>
              <button
                type="button"
                disabled={submitting}
                onClick={handleSubscribeSubmit}
                className="py-2 px-4 rounded-lg text-sm font-heading font-semibold text-white bg-turquoise-600 hover:bg-turquoise-700 disabled:opacity-60 transition-colors dark:bg-turquoise-700 dark:hover:bg-turquoise-600"
              >
                {submitting ? 'Gönderiliyor…' : 'Abone Ol'}
              </button>
            </div>
          </div>
        </div>
      )}

      {summaryModalOpen && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4">
          <div className="w-full max-w-md rounded-xl bg-white dark:bg-slate-900 p-6 shadow-lg">
            <h3 className="text-lg font-heading font-semibold text-slate-900 dark:text-white mb-4">
              {successNames.length > 0 ? 'Tek Bir Adım Kaldı' : 'Abonelik tamamlanamadı'}
            </h3>
            {successNames.length > 0 && (
              <div className="mb-4 text-sm text-slate-700 dark:text-slate-300">
                <p>Lütfen yolladığımız eposta üzerinden aşağıdaki bültenlere aboneliğinizi teyit edin</p>
                <ul className="list-disc list-inside mt-2">
                  {successNames.map((name) => (
                    <li key={name}>{name}</li>
                  ))}
                </ul>
              </div>
            )}
            {itemErrors.length > 0 && (
              <div className="mb-4 text-sm text-bordeaux-500">
                <ul className="list-disc list-inside">
                  {itemErrors.map((item) => (
                    <li key={item.templateBaseName}>
                      {resolveNewsletterLabel(item.templateBaseName)}: {item.errorMessage}
                    </li>
                  ))}
                </ul>
              </div>
            )}
            <div className="flex justify-end">
              <button
                type="button"
                onClick={closeSummaryModal}
                className="py-2 px-4 rounded-lg text-sm font-heading font-semibold text-white bg-turquoise-600 hover:bg-turquoise-700 transition-colors dark:bg-turquoise-700 dark:hover:bg-turquoise-600"
              >
                Tamam
              </button>
            </div>
          </div>
        </div>
      )}
    </>
  );
}
