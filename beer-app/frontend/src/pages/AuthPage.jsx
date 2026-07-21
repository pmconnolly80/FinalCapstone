import { useState } from 'react';
import { AUTH_CHANGED_EVENT, login, register } from '../lib/api';

function AuthPage() {
  const [mode, setMode] = useState('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
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
        : await register(email, password);

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
          <p style={{ margin: 0, fontSize: 13, color: '#6b7280' }}>
            Passwords need at least 8 characters.
          </p>
        )}
        <button type="submit">{mode === 'login' ? 'Continue' : 'Create account'}</button>
      </form>
      {message && <p>{message}</p>}
    </div>
  );
}

export default AuthPage;
