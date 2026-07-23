import { useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { resetPassword } from '../lib/api';

function ResetPassword() {
  const [searchParams] = useSearchParams();
  const email = searchParams.get('email') || '';
  const token = searchParams.get('token') || '';
  const [newPassword, setNewPassword] = useState('');
  const [message, setMessage] = useState('');
  const [succeeded, setSucceeded] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (newPassword.length < 8) {
      setMessage('Password is too short.');
      return;
    }
    try {
      await resetPassword(email, token, newPassword);
      setSucceeded(true);
      setMessage('Your password has been reset. You can now sign in.');
    } catch (error) {
      setMessage(error.message);
    }
  };

  if (!email || !token) {
    return (
      <div style={{ maxWidth: 420, margin: '0 auto', padding: 16 }}>
        <h2>Reset password</h2>
        <p>This password reset link is invalid or has expired.</p>
        <p>
          <Link to="/forgot-password">Request a new link</Link>
        </p>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: 16 }}>
      <h2>Reset password</h2>
      {!succeeded && (
        <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 12 }}>
          <label style={{ display: 'grid', gap: 4 }}>
            New password
            <input
              type="password"
              autoComplete="new-password"
              required
              value={newPassword}
              onChange={(e) => setNewPassword(e.target.value)}
              placeholder="New password"
            />
          </label>
          <p style={{ margin: 0, fontSize: 13, color: '#6b7280' }}>
            Passwords need at least 8 characters.
          </p>
          <button type="submit">Reset password</button>
        </form>
      )}
      {message && <p>{message}</p>}
      {succeeded && (
        <p>
          <Link to="/auth">Sign in</Link>
        </p>
      )}
    </div>
  );
}

export default ResetPassword;
