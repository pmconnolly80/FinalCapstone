import { useState } from 'react';
import { Link } from 'react-router-dom';
import { setMyPin } from '../lib/api';

// Staff-only screen (#13): set or change the personal 6-digit PIN typed on a customer's
// phone to confirm a beer. Customers who submit here get the API's 403 message.
function MyPin() {
  const hasToken = Boolean(localStorage.getItem('beer-token'));
  const [pin, setPin] = useState('');
  const [message, setMessage] = useState('');

  const handleSubmit = async (event) => {
    event.preventDefault();
    if (!/^\d{6}$/.test(pin)) {
      setMessage('PINs are exactly 6 digits.');
      return;
    }
    try {
      await setMyPin(pin);
      setMessage('PIN updated.');
      setPin('');
    } catch (error) {
      setMessage(error.message);
    }
  };

  if (!hasToken) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">My PIN</h2>
        <p className="mt-2 text-gray-600">
          <Link to="/auth" className="font-medium underline">
            Sign in
          </Link>{' '}
          with your staff account to manage your confirmation PIN.
        </p>
      </section>
    );
  }

  return (
    <section className="mx-auto max-w-sm rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
      <h2 className="m-0 text-xl font-bold">My PIN</h2>
      <p className="mt-2 text-sm text-gray-600">
        Staff only: the 6-digit PIN you type on a customer&apos;s phone to confirm a beer.
        Changes take effect immediately.
      </p>
      <form onSubmit={handleSubmit} className="mt-4 grid gap-3">
        <input
          type="password"
          inputMode="numeric"
          maxLength={6}
          value={pin}
          onChange={(e) => setPin(e.target.value)}
          placeholder="New 6-digit PIN"
          className="text-center tracking-[8px]"
        />
        <button
          type="submit"
          className="rounded-full border-0 bg-gray-900 px-6 py-3 font-medium text-white hover:bg-gray-700"
        >
          Save PIN
        </button>
      </form>
      {message && <p className="mt-3">{message}</p>}
    </section>
  );
}

export default MyPin;
