import { useState } from 'react';
import { confirmBeer } from '../lib/api';

// After this many consecutive failures, a wrong-PIN retry loop and a lockout look
// identical to the bartender — nudge toward asking an admin without revealing which.
const REPEATED_FAILURE_THRESHOLD = 3;

// The one-device confirmation moment: this fills the CUSTOMER's screen, and the customer
// hands the phone across the bar. The bartender verifies the beer name, keys their
// personal 6-digit PIN, and hands it back showing the updated count.
function ConfirmPinPad({ beer, onClose }) {
  const [pin, setPin] = useState('');
  const [error, setError] = useState('');
  const [isNetworkError, setIsNetworkError] = useState(false);
  const [failureCount, setFailureCount] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState(null);

  const handlePinChange = (event) => {
    setPin(event.target.value.replace(/\D/g, '').slice(0, 6));
    setError('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (pin.length !== 6) {
      setError('Enter the 6-digit bartender PIN.');
      return;
    }
    setSubmitting(true);
    try {
      const confirmation = await confirmBeer(beer.id, pin);
      setResult(confirmation);
      setFailureCount(0);
    } catch (err) {
      setError(err.message);
      setIsNetworkError(Boolean(err.isNetworkError));
      setPin('');
      if (!err.isNetworkError) setFailureCount((count) => count + 1);
    } finally {
      setSubmitting(false);
    }
  };

  return (
    <div
      role="dialog"
      aria-label="Confirm with bartender"
      style={{
        position: 'fixed', inset: 0, background: '#111827', color: '#fff',
        display: 'flex', flexDirection: 'column', alignItems: 'center',
        justifyContent: 'center', padding: 24, textAlign: 'center', zIndex: 50,
      }}
    >
      {result ? (
        <>
          <p style={{ fontSize: 20, margin: 0 }}>Confirmed!</p>
          <p style={{ fontSize: 44, fontWeight: 700, margin: '12px 0' }}>
            {result.confirmedCount} of {result.goal}
          </p>
          {result.mugEarned && (
            <p style={{ fontSize: 24, margin: '0 0 12px' }}>🏆 Mug earned!</p>
          )}
          <button onClick={onClose} style={{ padding: '12px 24px', fontSize: 16 }}>Done</button>
        </>
      ) : (
        <>
          <p style={{ margin: 0, color: '#9ca3af' }}>Hand your phone to the bartender</p>
          <h2 style={{ fontSize: 32, margin: '8px 0 4px' }}>{beer.name}</h2>
          <p style={{ margin: '0 0 20px', color: '#d1d5db' }}>{beer.brewery}</p>
          <form onSubmit={handleSubmit} style={{ display: 'grid', gap: 12, width: '100%', maxWidth: 280 }}>
            <input
              type="password"
              inputMode="numeric"
              autoComplete="off"
              aria-label="Bartender PIN"
              placeholder="Bartender PIN"
              value={pin}
              onChange={handlePinChange}
              style={{ fontSize: 28, textAlign: 'center', letterSpacing: 8, padding: 12 }}
            />
            <button type="submit" disabled={submitting} style={{ padding: '12px 24px', fontSize: 16 }}>
              {submitting ? 'Confirming…' : 'Confirm'}
            </button>
            <button type="button" onClick={onClose} style={{ padding: '8px 24px' }}>Cancel</button>
          </form>
          {error && isNetworkError && (
            <p style={{ color: '#fca5a5', marginTop: 12 }}>
              No signal — ask the bartender to note it, an admin can add it later.
            </p>
          )}
          {error && !isNetworkError && (
            <p style={{ color: '#fca5a5', marginTop: 12 }}>{error}</p>
          )}
          {error && !isNetworkError && failureCount >= REPEATED_FAILURE_THRESHOLD && (
            <p style={{ color: '#fbbf24', marginTop: 8 }}>
              ⚠️ Still not working after several tries? If this keeps happening, ask an admin.
            </p>
          )}
        </>
      )}
    </div>
  );
}

export default ConfirmPinPad;
