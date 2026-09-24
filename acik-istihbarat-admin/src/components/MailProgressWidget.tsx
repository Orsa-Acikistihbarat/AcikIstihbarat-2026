import React from 'react';
import { RefreshCw, CheckCircle2, AlertCircle, X, Info } from 'lucide-react';

export interface MailRunStatusDto {
  batchId: string;
  activeNewsletterKeys: string[];
  currentNewsletterKey: string | null;
  isRunning: boolean;
  totalRecipients: number;
  sentCount: number;
  successCount: number;
  failureCount: number;
  statusMessage: string | null;
  startedAtUtc: string | null;
  finishedAtUtc: string | null;
  lastError: string | null;
  progressPercentage: number;
}

interface MailProgressWidgetProps {
  status: MailRunStatusDto | null;
  onDismiss: () => void;
}

export const MailProgressWidget: React.FC<MailProgressWidgetProps> = ({
  status,
  onDismiss,
}) => {
  if (!status || (!status.isRunning && !status.finishedAtUtc && !status.statusMessage)) {
    return null;
  }

  const isZeroRecipientCase = !status.isRunning && status.totalRecipients === 0;

  return (
    <div className="mb-6 overflow-hidden rounded-2xl border border-indigo-100 bg-white shadow-sm transition-all">
      <div className="p-4 sm:p-5">
        <div className="flex items-start justify-between gap-4">
          <div className="flex items-center gap-3">
            {status.isRunning ? (
              <div className="flex size-10 items-center justify-center rounded-xl bg-indigo-50 text-indigo-600">
                <RefreshCw className="size-5 animate-spin" />
              </div>
            ) : isZeroRecipientCase ? (
              <div className="flex size-10 items-center justify-center rounded-xl bg-amber-50 text-amber-600">
                <Info className="size-5" />
              </div>
            ) : status.failureCount > 0 && status.successCount === 0 ? (
              <div className="flex size-10 items-center justify-center rounded-xl bg-red-50 text-red-600">
                <AlertCircle className="size-5" />
              </div>
            ) : (
              <div className="flex size-10 items-center justify-center rounded-xl bg-emerald-50 text-emerald-600">
                <CheckCircle2 className="size-5" />
              </div>
            )}

            <div>
              <div className="flex items-center gap-2">
                <h4 className="font-semibold text-gray-900">
                  {status.isRunning
                    ? 'E-posta Gönderimi Sürüyor...'
                    : isZeroRecipientCase
                    ? 'Gönderim Tamamlandı'
                    : 'Gönderim Tamamlandı'}
                </h4>
                {status.isRunning && (
                  <span className="inline-flex items-center rounded-full bg-indigo-50 px-2.5 py-0.5 text-xs font-medium text-indigo-700">
                    %{status.progressPercentage}
                  </span>
                )}
              </div>
              <p className="mt-0.5 text-xs text-gray-500">
                {status.statusMessage ||
                  (status.isRunning
                    ? `${status.currentNewsletterKey || 'Bülten'} işleniyor...`
                    : 'İşlem sona erdi.')}
              </p>
            </div>
          </div>

          {!status.isRunning && (
            <button
              onClick={onDismiss}
              className="rounded-lg p-1 text-gray-400 hover:bg-gray-100 hover:text-gray-600 transition-colors"
              title="Kapat"
            >
              <X className="size-5" />
            </button>
          )}
        </div>

        {/* Progress Bar (Only when totalRecipients > 0) */}
        {status.totalRecipients > 0 && (
          <div className="mt-4">
            <div className="h-2 w-full overflow-hidden rounded-full bg-gray-100">
              <div
                className={`h-full rounded-full transition-all duration-500 ${
                  status.isRunning
                    ? 'bg-indigo-600'
                    : status.failureCount > 0
                    ? 'bg-amber-500'
                    : 'bg-emerald-600'
                }`}
                style={{ width: `${Math.min(100, Math.max(0, status.progressPercentage))}%` }}
              />
            </div>

            <div className="mt-2 flex flex-wrap items-center justify-between text-xs text-gray-600">
              <span>
                Toplam {status.totalRecipients} alıcıdan{' '}
                <strong className="text-gray-900">{status.sentCount}</strong> adedi işlendi
              </span>
              <div className="flex gap-3">
                <span className="text-emerald-600 font-medium">
                  {status.successCount} başarılı
                </span>
                {status.failureCount > 0 && (
                  <span className="text-red-600 font-medium">
                    {status.failureCount} hatalı
                  </span>
                )}
              </div>
            </div>
          </div>
        )}

        {status.lastError && (
          <div className="mt-3 rounded-lg bg-red-50 p-2.5 text-xs text-red-700">
            <strong>Son Hata:</strong> {status.lastError}
          </div>
        )}
      </div>
    </div>
  );
};
