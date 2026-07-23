import { useEffect, useState } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { getLinkedProviders, startLinkingProvider } from '../lib/api';

const PROVIDERS = ['Google', 'Facebook', 'Apple'];

function LinkedAccounts() {
  const hasToken = Boolean(localStorage.getItem('beer-token'));
  const [searchParams] = useSearchParams();
  const [linkedProviders, setLinkedProviders] = useState([]);
  const [message, setMessage] = useState('');

  useEffect(() => {
    if (!hasToken) return;

    const linked = searchParams.get('linked');
    const error = searchParams.get('error');
    if (linked) setMessage(`${linked} connected.`);
    else if (error) setMessage(`Couldn't connect that account: ${error}`);

    getLinkedProviders()
      .then(setLinkedProviders)
      .catch((err) => setMessage(err.message));
  }, [hasToken, searchParams]);

  const handleConnect = async (provider) => {
    try {
      await startLinkingProvider(provider);
    } catch (error) {
      setMessage(error.message);
    }
  };

  if (!hasToken) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">Linked accounts</h2>
        <p className="mt-2 text-gray-600">
          <Link to="/auth" className="font-medium underline">
            Sign in
          </Link>{' '}
          to view and manage which sign-in providers are connected to your account.
        </p>
      </section>
    );
  }

  return (
    <section className="mx-auto max-w-sm rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
      <h2 className="m-0 text-xl font-bold">Linked accounts</h2>
      <p className="mt-2 text-sm text-gray-600">
        Connect additional sign-in providers to this account. Your progress always stays
        with the same account, however you sign in.
      </p>
      <ul className="mt-4 grid gap-3">
        {PROVIDERS.map((provider) => {
          const isConnected = linkedProviders.includes(provider);
          return (
            <li key={provider} className="flex items-center justify-between">
              <span>{provider}</span>
              {isConnected ? (
                <span className="text-sm font-medium text-green-700">Connected</span>
              ) : (
                <button
                  type="button"
                  onClick={() => handleConnect(provider)}
                  className="rounded-full border-0 bg-gray-900 px-4 py-2 text-sm font-medium text-white hover:bg-gray-700"
                >
                  Connect
                </button>
              )}
            </li>
          );
        })}
      </ul>
      {message && <p className="mt-3 text-sm">{message}</p>}
    </section>
  );
}

export default LinkedAccounts;
