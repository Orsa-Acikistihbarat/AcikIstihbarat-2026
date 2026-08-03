import React, { useState, useEffect } from 'react';
import api from '../lib/axios';
import type { Medya } from '../pages/MediaLibrary';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onSelect: (medya: Medya[]) => void;
  multiple?: boolean;
}

const MediaSelectorModal: React.FC<Props> = ({ isOpen, onClose, onSelect, multiple = false }) => {
  const [mediaList, setMediaList] = useState<Medya[]>([]);
  const [selected, setSelected] = useState<Medya[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (isOpen) {
      fetchMedia();
      setSelected([]);
    }
  }, [isOpen]);

  const fetchMedia = async () => {
    try {
      const response = await api.get('/medya');
      setMediaList(response.data);
    } catch (error) {
      console.error('Failed to fetch media', error);
    } finally {
      setLoading(false);
    }
  };

  const toggleSelect = (m: Medya) => {
    if (multiple) {
      if (selected.find(s => s.id === m.id)) {
        setSelected(selected.filter(s => s.id !== m.id));
      } else {
        setSelected([...selected, m]);
      }
    } else {
      setSelected([m]);
    }
  };

  const handleConfirm = () => {
    onSelect(selected);
    onClose();
  };

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto bg-black bg-opacity-50 flex items-center justify-center p-4 sm:p-0">
      <div className="bg-white rounded-xl shadow-xl w-full max-w-4xl max-h-[90vh] flex flex-col">
        <div className="flex justify-between items-center p-4 border-b border-gray-200">
          <h3 className="font-semibold text-lg text-gray-800">Medya Seç</h3>
          <button onClick={onClose} className="text-gray-500 hover:text-gray-800 focus:outline-none">
            <svg className="size-5" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M18 6 6 18"/><path d="m6 6 12 12"/></svg>
          </button>
        </div>
        
        <div className="p-4 overflow-y-auto flex-grow">
          {loading ? (
            <p>Yükleniyor...</p>
          ) : (
            <div className="grid grid-cols-3 sm:grid-cols-4 md:grid-cols-5 gap-4">
              {mediaList.map((m) => {
                const isSelected = selected.some(s => s.id === m.id);
                return (
                  <div 
                    key={m.id} 
                    onClick={() => toggleSelect(m)}
                    className={`cursor-pointer rounded-lg overflow-hidden border-2 transition ${isSelected ? 'border-blue-600' : 'border-transparent hover:border-gray-300'}`}
                  >
                    <div className="aspect-w-1 aspect-h-1 w-full bg-gray-100 flex justify-center items-center h-24">
                      {m.dosyaTipi.startsWith('image/') ? (
                        <img src={`http://localhost:5128${m.dosyaUrl}`} alt={m.baslik} className="object-cover h-full w-full" />
                      ) : (
                        <svg className="size-8 text-gray-400" xmlns="http://www.w3.org/2000/svg" width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>
                      )}
                    </div>
                    <div className="p-2 bg-gray-50 text-center">
                      <span className="text-[10px] truncate block" title={m.baslik || m.dosyaAdi}>{m.baslik || m.dosyaAdi}</span>
                    </div>
                  </div>
                );
              })}
            </div>
          )}
        </div>

        <div className="p-4 border-t border-gray-200 flex justify-end gap-2">
          <button onClick={onClose} className="py-2 px-3 inline-flex justify-center items-center text-sm font-medium rounded-lg border border-gray-200 bg-white text-gray-800 shadow-sm hover:bg-gray-50">İptal</button>
          <button onClick={handleConfirm} disabled={selected.length === 0} className="py-2 px-3 inline-flex justify-center items-center text-sm font-medium rounded-lg border border-transparent bg-blue-600 text-white hover:bg-blue-700 disabled:opacity-50">Seç ({selected.length})</button>
        </div>
      </div>
    </div>
  );
};

export default MediaSelectorModal;
