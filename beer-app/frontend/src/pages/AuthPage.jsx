import { useState } from 'react';
import { login, register } from '../lib/api';

function AuthPage() {
  const [mode, setMode] = useState('login');
  const [email, setEmail] = useState('');
  const [password, setPassword] = useState('');
  const [message, setMessage] = useState('');

  const handleSubmit = async (event) => {
    event.preventDefault();
    try {
      const result = mode === 'login'
        ? await login(email, password)
        : await register(email, password);

      localStorage.setItem('beer-token', result.token);
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
        <input value={email} onChange={(e) => setEmail(e.target.value)} placeholder="Email" />
        <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} placeholder="Password" />
        <button type="submit">{mode === 'login' ? 'Continue' : 'Create account'}</button>
      </form>
      {message && <p>{message}</p>}
    </div>
  );
}

export default AuthPage;
