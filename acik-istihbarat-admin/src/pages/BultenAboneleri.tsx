import React, { useState, useEffect } from 'react';
import api from '../lib/axios';

interface SubscriberRow {
  email: string;
  newsletterDisplayName: string;
  subscriptionDate: string | null;
  unsubscriptionDate: string | null;
}

const formatDate = (value: string | null) =>
  value ? new Date(value).toLocaleDateString('tr-TR') : '-';

const BultenAboneleri: React.FC = () => {
  const [rows, setRows] = useState<SubscriberRow[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    fetchSubscribers();
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

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h2 className="text-2xl font-semibold text-gray-800">Bültenlere Kimler Abone Oldu</h2>
      </div>

      <div className="bg-white rounded-xl shadow-sm border border-gray-200 overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">E-posta Adresi</th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Abone Olunan Bülten</th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Abonelik Tarihi</th>
              <th className="px-6 py-3 text-start text-xs font-medium text-gray-500 uppercase">Abonelik İptal Tarihi</th>
            </tr>
          </thead>
          <tbody className="divide-y divide-gray-200">
            {loading ? (
              <tr><td colSpan={4} className="px-6 py-4 text-center">Yükleniyor...</td></tr>
            ) : rows.length === 0 ? (
              <tr><td colSpan={4} className="px-6 py-4 text-center text-gray-500">Henüz abone bulunmuyor.</td></tr>
            ) : rows.map((r, i) => (
              <tr key={`${r.email}-${r.newsletterDisplayName}-${i}`}>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{r.email}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{r.newsletterDisplayName}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{formatDate(r.subscriptionDate)}</td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-800">{formatDate(r.unsubscriptionDate)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
};

export default BultenAboneleri;
