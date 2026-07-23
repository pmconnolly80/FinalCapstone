import { useEffect } from 'react';
import { Link, useNavigate, useSearchParams } from 'react-router-dom';
import { AUTH_CHANGED_EVENT } from '../lib/api';

function AuthCallback() {
  const [searchParams] = useSearchParams();
  const navigate = useNavigate();
  const token = searchParams.get('token');
  const error = searchParams.get('error');

  useEffect(() => {
    if (token) {
      localStorage.setItem('beer-token', token);
      window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
      navigate('/', { replace: true });
    }
  }, [token, navigate]);

  if (error) {
    return (
      <div style={{ maxWidth: 420, margin: '0 auto', padding: 16 }}>
        <h2>Sign-in failed</h2>
        <p>Something went wrong signing you in. Please try again.</p>
        <p>
          <Link to="/auth">Back to sign in</Link>
        </p>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: 16 }}>
      <p>Signing you in&hellip;</p>
    </div>
  );
}

export default AuthCallback;
