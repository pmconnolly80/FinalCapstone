import { useState } from 'react';
import { Link } from 'react-router-dom';
import { AUTH_CHANGED_EVENT, externalLoginUrl, login, register } from '../lib/api';

const SOCIAL_PROVIDERS = [
  { id: 'Google', label: 'Continue with Google' },
  { id: 'Facebook', label: 'Continue with Facebook' },
  { id: 'Apple', label: 'Continue with Apple' },
];

const inputClass =
  'w-full rounded-lg border border-gray-300 px-3 py-2 text-base focus:border-gray-900 focus:outline-none';

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
    <div className="mx-auto max-w-sm">
      <div className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)] sm:p-8">
        {/* Placeholder wordmark — swap for a real logo asset once the color theme lands. */}
        <div className="mb-6 flex flex-col items-center gap-1">
          <span className="text-3xl" aria-hidden="true">
            🍺
          </span>
          <span className="text-lg font-bold tracking-tight text-gray-900">Beer App</span>
        </div>

        <h2 className="m-0 text-center text-xl font-bold">
          {mode === 'login' ? 'Log in' : 'Create account'}
        </h2>

        <div className="mt-4 flex gap-2">
          <button
            type="button"
            onClick={() => setMode('login')}
            className={`flex-1 rounded-full border px-4 py-2 text-sm font-medium ${
              mode === 'login' ? 'border-gray-900 bg-gray-900 text-white' : 'border-gray-300 bg-white text-gray-700'
            }`}
          >
            Login
          </button>
          <button
            type="button"
            onClick={() => setMode('register')}
            className={`flex-1 rounded-full border px-4 py-2 text-sm font-medium ${
              mode === 'register' ? 'border-gray-900 bg-gray-900 text-white' : 'border-gray-300 bg-white text-gray-700'
            }`}
          >
            Register
          </button>
        </div>

        <form onSubmit={handleSubmit} className="mt-4 grid gap-3">
          <label className="grid gap-1 text-sm font-medium text-gray-700">
            Email
            <input
              type="email"
              autoComplete="email"
              required
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Email"
              className={inputClass}
            />
          </label>
          <label className="grid gap-1 text-sm font-medium text-gray-700">
            Password
            <input
              type="password"
              autoComplete={mode === 'login' ? 'current-password' : 'new-password'}
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              placeholder="Password"
              className={inputClass}
            />
          </label>
          {mode === 'register' && (
            <>
              <p className="m-0 text-xs text-gray-500">Passwords need at least 8 characters.</p>
              <label className="flex items-center gap-2 text-sm text-gray-700">
                <input
                  type="checkbox"
                  checked={marketingConsent}
                  onChange={(e) => setMarketingConsent(e.target.checked)}
                />
                Send me marketing emails
              </label>
            </>
          )}
          <button
            type="submit"
            className="mt-1 rounded-full border-0 bg-gray-900 px-6 py-3 font-medium text-white hover:bg-gray-700"
          >
            {mode === 'login' ? 'Continue' : 'Create account'}
          </button>
        </form>

        <div className="mt-4 grid gap-2">
          {SOCIAL_PROVIDERS.map((provider) => (
            <a
              key={provider.id}
              href={externalLoginUrl(provider.id)}
              className="block rounded-full border border-gray-300 px-4 py-2 text-center text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              {provider.label}
            </a>
          ))}
        </div>

        {mode === 'login' && (
          <p className="mt-4 text-center text-sm">
            <Link to="/forgot-password" className="font-medium underline">
              Forgot password?
            </Link>
          </p>
        )}
        {message && <p className="mt-3 text-center text-sm">{message}</p>}
      </div>
    </div>
  );
}

export default AuthPage;
