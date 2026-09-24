import React, { useState, useEffect } from 'react';
import { UserX, UserCheck, Send, Layers } from 'lucide-react';
import api from '../lib/axios';
import { SafetyConfirmationModal } from '../components/SafetyConfirmationModal';
import { MailProgressWidget } from '../components/MailProgressWidget';
import type { MailRunStatusDto } from '../components/MailProgressWidget';

interface SubscriberRow {
  id: number;
  email: string;
  newsletterDisplayName: string;
  subscriptionDate: string | null;
  unsubscriptionDate: string | null;
  isActive: boolean;
}

interface SummaryItem {
  key: string;
  title: string;
  subscribedCount: number;
  unsubscribedCount: number;
}

const formatDate = (value: string | null) =>
  value ? new Date(value).toLocaleDateString('tr-TR') : '-';

const NEWSLETTER_ORDER = ['AcikGazete', 'AcikKose'];

const BultenAboneleri: React.FC = () => {
  const [rows, setRows] = useState<SubscriberRow[]>([]);
  const [loading, setLoading] = useState(true);
  const [summary, setSummary] = useState<SummaryItem[]>([]);

  // Manual Trigger & Polling State
  const [runStatus, setRunStatus] = useState<MailRunStatusDto | null>(null);
  const [isModalOpen, setIsModalOpen] = useState(false);
  const [targetKeys, setTargetKeys] = useState<string[]>([]);
  const [isTriggering, setIsTriggering] = useState(false);

  useEffect(() => {
    fetchSubscribers();
    fetchSummary();
    fetchRunStatus();
  }, []);

  // Polling hook: runs every 2.5s while active
  useEffect(() => {
    if (!runStatus?.isRunning) return;

    const timer = setInterval(async () => {
      try {
        const response = await api.get('/mail/status');
        setRunStatus(response.data);

        // When run finishes, refresh data tables
        if (!response.data?.isRunning) {
          fetchSubscribers();
          fetchSummary();
        }
      } catch (error) {
        console.error('Failed to poll mail run status', error);
      }
    }, 2500);

    return () => clearInterval(timer);
  }, [runStatus?.isRunning]);

  const fetchSubscribers = async () => {
    try {
      const response = await api.get('/mail/subscribers');
      setRows(response.data || []);
    } catch (error) {
      console.error('Failed to fetch subscribers', error);
    } finally {
      setLoading(false);
    }
  };

  const fetchSummary = async () => {
    try {
      const response = await api.get('/mail/subscribers/summary');
      setSummary(response.data || []);
    } catch (error) {
      console.error('Failed to fetch subscriber summary', error);
    }
  };

  const fetchRunStatus = async () => {
    try {
      const response = await api.get('/mail/status');
      setRunStatus(response.data || null);
    } catch (error) {
      console.error('Failed to fetch current mail run status', error);
    }
  };

  const handleOpenModal = (keys: string[]) => {
    setTargetKeys(keys);
    setIsModalOpen(true);
  };

  const handleTriggerConfirm = async (forceResend: boolean) => {
    setIsTriggering(true);
    try {
      await api.post('/mail/trigger', {
        newsletterKeys: targetKeys,
        forceResend,
      });

      setIsModalOpen(false);
      // Immediately refresh status to enter running polling state
      await fetchRunStatus();
    } catch (error: any) {
      if (error.response?.status === 409) {
        alert(
          error.response.data?.message ||
            'Şu anda devam eden bir e-posta gönderimi var. Lütfen tamamlanmasını bekleyin.'
        );
      } else {
        alert(
          error.response?.data?.message ||
            'E-posta gönderim işlemi başlatılırken bir hata oluştu.'
        );
      }
    } finally {
      setIsTriggering(false);
    }
  };

  const handleDeactivate = async (id: number) => {
    if (window.confirm('Bu e-postanın bülten aboneliğini iptal etmek istediğinize emin misiniz?')) {
      try {
        await api.put(`/mail/subscribers/${id}/deactivate`);
        fetchSubscribers();
        fetchSummary();
      } catch {
        alert('İşlem başarısız.');
      }
    }
  };

  const modalTitles = targetKeys.map(
    (k) => summary.find((s) => s.key === k)?.title || k
  );

  const modalRecipientTotal = targetKeys.reduce(
    (sum, k) => sum + (summary.find((s) => s.key === k)?.subscribedCount || 0),
    0
  );

  return (
    <div>
      <div className="flex flex-col sm:flex-row justify-between items-start sm:items-center gap-4 mb-6">
        <div>
          <h2 className="text-2xl font-semibold text-gray-800">
            Bültenlere Kimler Abone Oldu
          </h2>
          <p className="text-xs text-gray-500 mt-1">
            Abone listesini görüntüleyebilir ve manuel bülten gönderimlerini yönetebilirsiniz.
          </p>
        </div>

        <button
          onClick={() => handleOpenModal(NEWSLETTER_ORDER)}
          disabled={runStatus?.isRunning}
          className="px-4 py-2 bg-indigo-600 hover:bg-indigo-700 text-white text-sm font-medium rounded-xl flex items-center gap-2 shadow-sm transition-all disabled:opacity-50 disabled:cursor-not-allowed"
          title={
            runStatus?.isRunning
              ? 'Devam eden bir gönderim var'
              : 'Her iki bülteni sırayla gönder'
          }
        >
          <Layers className="size-4" />
          Her İkisini de Gönder
        </button>
      </div>

      {/* Real-time Progress & Status Widget */}
      <MailProgressWidget
        status={runStatus}
        onDismiss={() => setRunStatus(null)}
      />

      {summary.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
          {NEWSLETTER_ORDER.map((key) => {
            const item = summary.find((s) => s.key === key);
            if (!item) return null;
            return (
              <div
                key={key}
                className="bg-white rounded-xl shadow-sm border border-gray-200 p-5 flex flex-col justify-between"
              >
                <div>
                  <div className="flex items-center justify-between mb-3">
                    <h3 className="text-base font-semibold text-gray-800">
                      {item.title}
                    </h3>
                    <button
                      onClick={() => handleOpenModal([key])}
                      disabled={runStatus?.isRunning}
                      className="px-3 py-1.5 bg-indigo-50 hover:bg-indigo-100 text-indigo-700 text-xs font-semibold rounded-lg flex items-center gap-1.5 transition-colors disabled:opacity-50 disabled:cursor-not-allowed"
                      title={
                        runStatus?.isRunning
                          ? 'Devam eden bir gönderim var'
                          : `${item.title} için manuel gönderim başlat`
                      }
                    >
                      <Send className="size-3.5" />
                      Manuel Gönder
                    </button>
                  </div>
                  <div className="flex gap-4 text-sm mt-2">
                    <span className="text-emerald-600 font-semibold bg-emerald-50 px-2.5 py-1 rounded-md">
                      {item.subscribedCount} aktif abone
                    </span>
                    <span className="text-gray-500 font-medium bg-gray-50 px-2.5 py-1 rounded-md">
                      {item.unsubscribedCount} iptal
                    </span>
                  </div>
                </div>
              </div>
            );
          })}
        </div>
      )}

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">
                E-posta Adresi
              </th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">
                Abone Olunan Bülten
              </th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">
                Abonelik Tarihi
              </th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">
                Abonelik İptal Tarihi
              </th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">
                İşlemler
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {loading ? (
              <tr>
                <td colSpan={5} className="px-6 py-4 text-center text-gray-500">
                  Yükleniyor...
                </td>
              </tr>
            ) : rows.length === 0 ? (
              <tr>
                <td colSpan={5} className="px-6 py-4 text-center text-gray-500">
                  Henüz abone bulunmuyor.
                </td>
              </tr>
            ) : (
              rows.map((r) => (
                <tr key={r.id}>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">
                    {r.email}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">
                    {r.newsletterDisplayName}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">
                    {formatDate(r.subscriptionDate)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">
                    {formatDate(r.unsubscriptionDate)}
                  </td>
                  <td className="px-6 py-4 whitespace-nowrap text-sm text-center">
                    {r.isActive ? (
                      <button
                        onClick={() => handleDeactivate(r.id)}
                        title="Aboneliği İptal Et"
                        className="inline-flex text-red-600 hover:text-red-800"
                      >
                        <UserX className="size-4" />
                      </button>
                    ) : (
                      <span
                        title="Zaten pasif"
                        className="inline-flex text-gray-300 cursor-not-allowed"
                      >
                        <UserCheck className="size-4" />
                      </span>
                    )}
                  </td>
                </tr>
              ))
            )}
          </tbody>
        </table>
      </div>

      {/* Safety Confirmation Modal */}
      <SafetyConfirmationModal
        isOpen={isModalOpen}
        onClose={() => setIsModalOpen(false)}
        onConfirm={handleTriggerConfirm}
        newsletterTitles={modalTitles}
        totalSubscribers={modalRecipientTotal}
        isLoading={isTriggering}
      />
    </div>
  );
};

export default BultenAboneleri;
