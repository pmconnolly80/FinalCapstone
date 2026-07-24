import { useState } from 'react';
import { confirmBeer, setBeerAvailabilityViaPin, setMyRating } from '../lib/api';

// After this many consecutive failures, a wrong-PIN retry loop and a lockout look
// identical to the bartender — nudge toward asking an admin without revealing which.
const REPEATED_FAILURE_THRESHOLD = 3;

const RATING_VALUES = [1, 2, 3, 4, 5];

// #79: PINs range 6-8 digits (StaffPinsController.MinPinLength/MaxPinLength on the
// backend) rather than a hardcoded 6 — an admin can issue a longer, memorable format
// like an 8-digit birthday instead of a random 6-digit one.
const MIN_PIN_LENGTH = 6;
const MAX_PIN_LENGTH = 8;

// The one-device confirmation moment: this fills the CUSTOMER's screen, and the customer
// hands the phone across the bar. The bartender verifies the beer name, keys their
// personal PIN, and hands it back showing the updated count.
//
// #80: the same PIN also lets the bartender flip this beer's stock status — no separate
// admin session, since the bartender has no device of their own at the bar. Reuses the
// PIN already typed into the field above rather than asking for it twice. A deliberate
// second tap (the "confirming" step below) guards against an accidental tap mid-rush,
// since this sits right next to the routine, fast "Confirm" action.
function ConfirmPinPad({ beer, onClose }) {
  const [pin, setPin] = useState('');
  const [error, setError] = useState('');
  const [isNetworkError, setIsNetworkError] = useState(false);
  const [failureCount, setFailureCount] = useState(0);
  const [submitting, setSubmitting] = useState(false);
  const [result, setResult] = useState(null);

  const [availabilityStep, setAvailabilityStep] = useState('closed'); // closed | confirming | success
  const [availabilityError, setAvailabilityError] = useState('');
  const [availabilitySubmitting, setAvailabilitySubmitting] = useState(false);

  // #74: "How was it?" rating prompt on the success screen — skippable, and still
  // changeable afterward (re-tapping a star just re-submits), editable later from beer
  // detail too since there's no My Beers screen yet for it to live on instead.
  const [ratingValue, setRatingValue] = useState(null);
  const [ratingSkipped, setRatingSkipped] = useState(false);
  const [ratingSubmitting, setRatingSubmitting] = useState(false);
  const [ratingError, setRatingError] = useState('');

  const targetAvailability = beer.availability === 'OutOfStock' ? 'Available' : 'OutOfStock';
  const targetLabel = targetAvailability === 'OutOfStock' ? 'out of stock' : 'available';

  const handlePinChange = (event) => {
    setPin(event.target.value.replace(/\D/g, '').slice(0, MAX_PIN_LENGTH));
    setError('');
  };

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (pin.length < MIN_PIN_LENGTH || pin.length > MAX_PIN_LENGTH) {
      setError("Enter the bartender's PIN.");
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

  const startAvailabilityChange = () => {
    setAvailabilityError('');
    setAvailabilityStep('confirming');
  };

  const cancelAvailabilityChange = () => {
    setAvailabilityError('');
    setAvailabilityStep('closed');
  };

  const submitAvailabilityChange = async () => {
    if (pin.length < MIN_PIN_LENGTH || pin.length > MAX_PIN_LENGTH) {
      setAvailabilityError("Enter the bartender's PIN above first.");
      return;
    }
    setAvailabilitySubmitting(true);
    try {
      await setBeerAvailabilityViaPin(beer.id, pin, targetAvailability);
      setAvailabilityStep('success');
    } catch (err) {
      setAvailabilityError(err.isNetworkError ? 'No signal — try again once you have one.' : err.message);
    } finally {
      setAvailabilitySubmitting(false);
    }
  };

  const submitRating = async (value) => {
    setRatingSubmitting(true);
    setRatingError('');
    try {
      await setMyRating(beer.id, value);
      setRatingValue(value);
    } catch (err) {
      setRatingError(err.message);
    } finally {
      setRatingSubmitting(false);
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
          {!result.mugEarned && result.milestoneReached && (
            <p style={{ fontSize: 20, margin: '0 0 12px' }}>🎉 {result.confirmedCount} beers — nice milestone!</p>
          )}

          {!ratingSkipped && ratingValue == null && (
            <div style={{ margin: '8px 0 16px' }}>
              <p style={{ margin: '0 0 8px', color: '#d1d5db' }}>How was it?</p>
              <div style={{ display: 'flex', gap: 6, justifyContent: 'center' }}>
                {RATING_VALUES.map((value) => (
                  <button
                    key={value}
                    type="button"
                    aria-label={`Rate ${value} star${value === 1 ? '' : 's'}`}
                    disabled={ratingSubmitting}
                    onClick={() => submitRating(value)}
                    style={{ fontSize: 24, padding: '4px 8px', background: 'none', border: '1px solid #4b5563', borderRadius: 8, color: '#fff' }}
                  >
                    ★{value}
                  </button>
                ))}
              </div>
              <button
                type="button"
                onClick={() => setRatingSkipped(true)}
                style={{ background: 'none', border: 'none', color: '#9ca3af', textDecoration: 'underline', fontSize: 13, marginTop: 8 }}
              >
                Skip
              </button>
              {ratingError && <p style={{ color: '#fca5a5', fontSize: 13, marginTop: 8 }}>{ratingError}</p>}
            </div>
          )}
          {ratingValue != null && (
            <p style={{ color: '#d1d5db', fontSize: 14, margin: '0 0 16px' }}>Thanks! Rated ★{ratingValue}.</p>
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

          <div style={{ marginTop: 24, paddingTop: 16, borderTop: '1px solid #374151', width: '100%', maxWidth: 280 }}>
            {availabilityStep === 'closed' && (
              <button
                type="button"
                onClick={startAvailabilityChange}
                style={{ background: 'none', border: 'none', color: '#9ca3af', textDecoration: 'underline', fontSize: 14 }}
              >
                Mark this beer as {targetLabel}
              </button>
            )}
            {availabilityStep === 'confirming' && (
              <>
                <p style={{ margin: '0 0 12px', fontSize: 14 }}>
                  Mark <strong>{beer.name}</strong> as {targetLabel}? This is what customers see immediately.
                </p>
                <div style={{ display: 'flex', gap: 8, justifyContent: 'center' }}>
                  <button type="button" onClick={submitAvailabilityChange} disabled={availabilitySubmitting} style={{ padding: '8px 16px' }}>
                    {availabilitySubmitting ? 'Marking…' : `Yes, mark ${targetLabel}`}
                  </button>
                  <button type="button" onClick={cancelAvailabilityChange} style={{ padding: '8px 16px' }}>
                    Cancel
                  </button>
                </div>
                {availabilityError && (
                  <p style={{ color: '#fca5a5', marginTop: 8, fontSize: 14 }}>{availabilityError}</p>
                )}
              </>
            )}
            {availabilityStep === 'success' && (
              <p style={{ fontSize: 14 }}>✅ Marked {targetLabel}.</p>
            )}
          </div>
        </>
      )}
    </div>
  );
}

export default ConfirmPinPad;
