import React, { useState, useEffect } from 'react';
import { UserX, UserCheck } from 'lucide-react';
import api from '../lib/axios';

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

  useEffect(() => {
    fetchSubscribers();
    fetchSummary();
  }, []);

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

  const handleDeactivate = async (id: number) => {
    if (window.confirm('Bu e-postanın bülten aboneliğini iptal etmek istediğinize emin misiniz?')) {
      try {
        await api.put(`/mail/subscribers/${id}/deactivate`);
        fetchSubscribers();
      } catch (error) {
        alert('İşlem başarısız.');
      }
    }
  };

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Bültenlere Kimler Abone Oldu</h2>
      </div>

      {summary.length > 0 && (
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4 mb-6">
          {NEWSLETTER_ORDER.map((key) => {
            const item = summary.find((s) => s.key === key);
            if (!item) return null;
            return (
              <div key={key} className="bg-white rounded-xl shadow-sm border border-gray-200 p-4">
                <h3 className="text-sm font-medium text-gray-500 mb-2">{item.title}</h3>
                <div className="flex justify-between text-sm">
                  <span className="text-emerald-600 font-semibold">{item.subscribedCount} abone</span>
                  <span className="text-red-600 font-semibold">{item.unsubscribedCount} iptal</span>
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
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">E-posta Adresi</th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Abone Olunan Bülten</th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Abonelik Tarihi</th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Abonelik İptal Tarihi</th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">İşlemler</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {loading ? (
              <tr><td colSpan={5} className="px-6 py-4 text-center">Yükleniyor...</td></tr>
            ) : rows.length === 0 ? (
              <tr><td colSpan={5} className="px-6 py-4 text-center text-gray-500">Henüz abone bulunmuyor.</td></tr>
            ) : rows.map((r) => (
              <tr key={r.id}>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{r.email}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{r.newsletterDisplayName}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{formatDate(r.subscriptionDate)}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{formatDate(r.unsubscriptionDate)}</td>
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
                    <span title="Zaten pasif" className="inline-flex text-gray-300 cursor-not-allowed">
                      <UserCheck className="size-4" />
                    </span>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default BultenAboneleri;
