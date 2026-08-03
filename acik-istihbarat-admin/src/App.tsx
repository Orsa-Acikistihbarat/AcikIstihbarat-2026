import React from 'react';
import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { AuthProvider, useAuth } from './contexts/AuthContext';
import Layout from './components/Layout';
import Login from './pages/Login';
import Dashboard from './pages/Dashboard';
import Categories from './pages/Categories';
import MediaLibrary from './pages/MediaLibrary';
import HaberlerList from './pages/HaberlerList';
import HaberEditor from './pages/HaberEditor';

const ProtectedRoute = ({ children }: { children: React.ReactNode }) => {
  const { user, loading } = useAuth();
  if (loading) return <div>Yükleniyor...</div>;
  if (!user) return <Navigate to="/login" replace />;
  return <>{children}</>;
};

function AppRoutes() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route path="/" element={<ProtectedRoute><Layout /></ProtectedRoute>}>
        <Route index element={<Dashboard />} />
        <Route path="kategoriler" element={<Categories />} />
        <Route path="medya" element={<MediaLibrary />} />
        <Route path="haberler" element={<HaberlerList />} />
        <Route path="haberler/yeni" element={<HaberEditor />} />
        <Route path="haberler/duzenle/:id" element={<HaberEditor />} />
      </Route>
    </Routes>
  );
}

function App() {
  return (
    <AuthProvider>
      <Router>
        <AppRoutes />
      </Router>
    </AuthProvider>
  );
}

export default App;
