import { useState } from 'react';
import { Link } from 'react-router-dom';
import { AUTH_CHANGED_EVENT, externalLoginUrl, login, register } from '../lib/api';

const SOCIAL_PROVIDERS = [
  { id: 'Google', label: 'Continue with Google' },
  { id: 'Facebook', label: 'Continue with Facebook' },
  { id: 'Apple', label: 'Continue with Apple' },
];

function AuthPage() {
  const [mode, setMode] = useState('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [marketingConsent, setMarketingConsent] = useState(false);
  const [message, setMessage] = useState('');

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (mode === 'register' && password.length < 8) {
      setMessage('Password is too short.');
      return;
    }
    try {
      const result = mode === 'login'
        ? await login(email, password)
        : await register(email, password, marketingConsent);

      localStorage.setItem('beer-token', result.token);
      window.dispatchEvent(new Event(AUTH_CHANGED_EVENT));
      setMessage(`${mode === 'login' ? 'Logged in' : 'Registered'} successfully.`);
    } catch (error) {
      setMessage(error.message);
    }
  };

  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: 16 }}>
      <h2>{mode === 'login' ? 'Log in' : 'Create account'}</h2>
      <div style={{ display: 'flex', gap: 8, marginBottom: 16 }}>
        <button onClick={() => setMode('login')}>Login</button>
        <button onClick={() => setMode('register')}>Register</button>
      </div>
      <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 12 }}>
        <label style={{ display: 'grid', gap: 4 }}>
          Email
          <input
            type="email"
            autoComplete="email"
            required
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            placeholder="Email"
          />
        </label>
        <label style={{ display: 'grid', gap: 4 }}>
          Password
          <input
            type="password"
            autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
            required
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            placeholder="Password"
          />
        </label>
        {mode === 'register' && (
          <>
            <p style={{ margin: 0, fontSize: 13, color: '#6b7280' }}>
              Passwords need at least 8 characters.
            </p>
            <label style={{ display: 'flex', alignItems: 'center', gap: 8 }}>
              <input
                type="checkbox"
                checked={marketingConsent}
                onChange={(e) => setMarketingConsent(e.target.checked)}
              />
              Send me marketing emails
            </label>
          </>
        )}
        <button type="submit">{mode === 'login' ? 'Continue' : 'Create account'}</button>
      </form>
      <div style={{ display: 'grid', gap: 8, marginTop: 16 }}>
        {SOCIAL_PROVIDERS.map((provider) => (
          <a
            key={provider.id}
            href={externalLoginUrl(provider.id)}
            style={{ display: 'block', textAlign: 'center', textDecoration: 'none' }}
          >
            {provider.label}
          </a>
        ))}
      </div>
      {mode === 'login' && (
        <p>
          <Link to="/forgot-password">Forgot password?</Link>
        </p>
      )}
      {message && <p>{message}</p>}
    </div>
  );
}

export default AuthPage;
