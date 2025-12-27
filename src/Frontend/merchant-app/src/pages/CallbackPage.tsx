import { useEffect } from 'react';
import { useAuth } from 'react-oidc-context';
import { useNavigate } from 'react-router-dom';

export default function CallbackPage() {
  const auth = useAuth();
  const navigate = useNavigate();

  useEffect(() => {
    if (auth.isAuthenticated) {
      navigate('/');
    }
  }, [auth.isAuthenticated, navigate]);

  return (
    <div className="flex items-center justify-center min-h-screen">
      <div className="text-center">
        <div className="spinner mx-auto mb-4"></div>
        <p className="text-neutral-600">Completing authentication...</p>
      </div>
    </div>
  );
}
