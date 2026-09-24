import React, { useState } from 'react';
import { AlertTriangle, Send, X, RefreshCw } from 'lucide-react';

interface SafetyConfirmationModalProps {
  isOpen: boolean;
  onClose: () => void;
  onConfirm: (forceResend: boolean) => void;
  newsletterTitles: string[];
  totalSubscribers: number;
  isLoading: boolean;
}

export const SafetyConfirmationModal: React.FC<SafetyConfirmationModalProps> = ({
  isOpen,
  onClose,
  onConfirm,
  newsletterTitles,
  totalSubscribers,
  isLoading,
}) => {
  const [forceResend, setForceResend] = useState(false);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50 p-4 backdrop-blur-xs">
      <div className="w-full max-w-lg rounded-2xl bg-white p-6 shadow-xl border border-gray-100">
        <div className="flex items-start justify-between pb-3 border-b border-gray-100">
          <div className="flex items-center gap-2 text-indigo-600">
            <div className="p-2 bg-indigo-50 rounded-lg">
              <Send className="size-5" />
            </div>
            <h3 className="text-lg font-semibold text-gray-900">
              Manuel E-posta Gönderimi Onayı
            </h3>
          </div>
          <button
            onClick={onClose}
            disabled={isLoading}
            className="text-gray-400 hover:text-gray-600 p-1 rounded-lg"
          >
            <X className="size-5" />
          </button>
        </div>

        <div className="mt-4 space-y-4 text-sm text-gray-600">
          <div className="bg-gray-50 rounded-xl p-4 border border-gray-200/60 space-y-2">
            <div className="flex justify-between">
              <span className="text-gray-500 font-medium">Hedef Bülten(ler):</span>
              <span className="font-semibold text-gray-900">
                {newsletterTitles.join(', ')}
              </span>
            </div>
            <div className="flex justify-between">
              <span className="text-gray-500 font-medium">Aktif Abone Havuzu:</span>
              <span className="font-semibold text-emerald-600">
                {totalSubscribers} abone
              </span>
            </div>
          </div>

          <div className="flex items-start gap-2.5 p-3.5 bg-amber-50 rounded-xl border border-amber-200 text-amber-800">
            <AlertTriangle className="size-5 shrink-0 mt-0.5 text-amber-600" />
            <p className="text-xs leading-relaxed">
              Bu işlem e-postaları arka planda sırayla göndermeye başlayacaktır.
              Gmail hız limitlerine uymak için her e-posta arasında kontrollü bekleme süreleri uygulanır.
            </p>
          </div>

          <div className="pt-2">
            <label className="flex items-start gap-3 p-3 bg-gray-50 hover:bg-gray-100/80 rounded-xl border border-gray-200 cursor-pointer transition-colors">
              <input
                type="checkbox"
                checked={forceResend}
                onChange={(e) => setForceResend(e.target.checked)}
                className="mt-1 size-4 rounded border-gray-300 text-indigo-600 focus:ring-indigo-500"
              />
              <div className="text-xs">
                <span className="font-semibold text-gray-800 block">
                  Bugün zaten gönderilmiş olan abonelere tekrar gönder (Force Resend)
                </span>
                <span className="text-gray-500">
                  İşaretlenmezse, bugün bülteni zaten almış olan abonelere mükerrer gönderim yapılmaz.
                </span>
              </div>
            </label>
          </div>
        </div>

        <div className="mt-6 flex justify-end gap-3 pt-3 border-t border-gray-100">
          <button
            type="button"
            onClick={onClose}
            disabled={isLoading}
            className="px-4 py-2 text-sm font-medium text-gray-700 bg-gray-100 hover:bg-gray-200 rounded-xl transition-colors"
          >
            İptal
          </button>
          <button
            type="button"
            onClick={() => onConfirm(forceResend)}
            disabled={isLoading}
            className="px-4 py-2 text-sm font-medium text-white bg-indigo-600 hover:bg-indigo-700 rounded-xl flex items-center gap-2 shadow-sm transition-colors disabled:opacity-50"
          >
            {isLoading ? (
              <>
                <RefreshCw className="size-4 animate-spin" />
                Başlatılıyor...
              </>
            ) : (
              <>
                <Send className="size-4" />
                Evet, Gönderimi Başlat
              </>
            )}
          </button>
        </div>
      </div>
    </div>
  );
};
