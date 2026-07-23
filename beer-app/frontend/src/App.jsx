import { useEffect, useState } from 'react';
import { Routes, Route, Link, useNavigate } from 'react-router-dom';
import { AUTH_CHANGED_EVENT, getRolesFromToken, logout } from './lib/api';
import Home from './pages/Home';
import AdminConfirmations from './pages/AdminConfirmations';
import BeerList from './pages/BeerList';
import BeerDetail from './pages/BeerDetail';
import BeerForm from './pages/BeerForm';
import AuthPage from './pages/AuthPage';
import AuthCallback from './pages/AuthCallback';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';
import MyProgress from './pages/MyProgress';
import MyPin from './pages/MyPin';
import LinkedAccounts from './pages/LinkedAccounts';
import PrivacyPolicy from './pages/PrivacyPolicy';

const navLinkClass =
  'rounded-lg px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-200 hover:text-gray-900';

function readAuthState() {
  return { signedIn: Boolean(localStorage.getItem('beer-token')), roles: getRolesFromToken() };
}

function App() {
  const navigate = useNavigate();
  const [auth, setAuth] = useState(readAuthState);

  // Same-tab login/register/logout don't fire the browser's 'storage' event (only other
  // tabs get that), so the nav would otherwise stay stale until a manual reload.
  useEffect(() => {
    const onAuthChanged = () => setAuth(readAuthState());
    window.addEventListener(AUTH_CHANGED_EVENT, onAuthChanged);
    window.addEventListener('storage', onAuthChanged);
    return () => {
      window.removeEventListener(AUTH_CHANGED_EVENT, onAuthChanged);
      window.removeEventListener('storage', onAuthChanged);
    };
  }, []);

  const handleSignOut = () => {
    logout();
    navigate('/');
  };

  return (
    <div className="mx-auto max-w-5xl p-4 md:p-8">
      <header className="mb-6 flex flex-wrap items-center justify-between gap-4">
        <div>
          <h1 className="m-0 text-2xl font-bold tracking-tight">Beer App</h1>
          <p className="m-0 mt-1 text-sm text-gray-600">
            The tavern&apos;s 200 club, on your phone
          </p>
        </div>
        <nav className="flex flex-wrap gap-1">
          <Link className={navLinkClass} to="/">
            Home
          </Link>
          <Link className={navLinkClass} to="/beers">
            Beers
          </Link>
          <Link className={navLinkClass} to="/progress">
            My Progress
          </Link>
          {auth.roles.includes('Admin') && (
            <Link className={navLinkClass} to="/beers/new">
              Add Beer
            </Link>
          )}
          <Link className={navLinkClass} to="/my-pin">
            My PIN
          </Link>
          {auth.signedIn && (
            <Link className={navLinkClass} to="/account/linked-providers">
              Linked accounts
            </Link>
          )}
          {auth.roles.includes('Admin') && (
            <Link className={navLinkClass} to="/admin/confirmations">
              Admin
            </Link>
          )}
          {auth.signedIn ? (
            <button type="button" onClick={handleSignOut} className={navLinkClass}>
              Sign out
            </button>
          ) : (
            <Link className={navLinkClass} to="/auth">
              Sign in
            </Link>
          )}
        </nav>
      </header>

      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/beers" element={<BeerList />} />
        <Route path="/beers/:id" element={<BeerDetail />} />
        <Route path="/beers/new" element={<BeerForm />} />
        <Route path="/beers/:id/edit" element={<BeerForm />} />
        <Route path="/progress" element={<MyProgress />} />
        <Route path="/my-pin" element={<MyPin />} />
        <Route path="/admin/confirmations" element={<AdminConfirmations />} />
        <Route path="/auth" element={<AuthPage />} />
        <Route path="/auth/callback" element={<AuthCallback />} />
        <Route path="/forgot-password" element={<ForgotPassword />} />
        <Route path="/reset-password" element={<ResetPassword />} />
        <Route path="/account/linked-providers" element={<LinkedAccounts />} />
        <Route path="/privacy" element={<PrivacyPolicy />} />
      </Routes>

      <footer className="mt-10 border-t border-gray-200 pt-4 text-sm text-gray-500">
        <Link className="hover:text-gray-700" to="/privacy">
          Privacy policy
        </Link>
      </footer>
    </div>
  );
}

export default App;
