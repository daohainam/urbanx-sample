import { useEffect } from 'react';
import { useNavigate } from 'react-router-dom';
import { userManager } from '../services/auth';

const OidcCallbackPage = () => {
    const navigate = useNavigate();

    useEffect(() => {
        userManager.signinRedirectCallback()
            .then(() => navigate('/'))
            .catch(err => {
                console.error('OIDC callback error:', err);
                navigate('/login');
            });
    }, [navigate]);

    return (
        <div className="flex items-center justify-center min-h-screen">
            <p className="text-gray-500">Signing you in...</p>
        </div>
    );
};

export default OidcCallbackPage;
