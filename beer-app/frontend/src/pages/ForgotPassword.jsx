import { useState } from 'react';
import { Link } from 'react-router-dom';
import { forgotPassword } from '../lib/api';

function ForgotPassword() {
  const [email, setEmail] = useState('');
  const [message, setMessage] = useState('');
  const [submitted, setSubmitted] = useState(false);

  const handleSubmit = async (event) => {
    event.preventDefault();
    try {
      await forgotPassword(email);
      setSubmitted(true);
      setMessage('If an account with that email exists, a password reset link has been sent.');
    } catch (error) {
      setMessage(error.message);
    }
  };

  return (
    <div style={{ maxWidth: 420, margin: '0 auto', padding: 16 }}>
      <h2>Forgot password</h2>
      {!submitted && (
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
          <button type="submit">Send reset link</button>
        </form>
      )}
      {message && <p>{message}</p>}
      <p>
        <Link to="/auth">Back to sign in</Link>
      </p>
    </div>
  );
}

export default ForgotPassword;
