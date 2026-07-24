import { useEffect, useState } from 'react';
import { Routes, Route, Link, useLocation } from 'react-router-dom';
import { AUTH_CHANGED_EVENT, getRolesFromToken } from './lib/api';
import Home from './pages/Home';
import AdminConfirmations from './pages/AdminConfirmations';
import AdminUsers from './pages/AdminUsers';
import AdminBeers from './pages/AdminBeers';
import AdminDashboard from './pages/AdminDashboard';
import AdminRecommendations from './pages/AdminRecommendations';
import AdminSearchDemand from './pages/AdminSearchDemand';
import BeerList from './pages/BeerList';
import BeerDetail from './pages/BeerDetail';
import BeerForm from './pages/BeerForm';
import AuthPage from './pages/AuthPage';
import AuthCallback from './pages/AuthCallback';
import ForgotPassword from './pages/ForgotPassword';
import ResetPassword from './pages/ResetPassword';
import MyProgress from './pages/MyProgress';
import MyPin from './pages/MyPin';
import RecommendBeer from './pages/RecommendBeer';
import LinkedAccounts from './pages/LinkedAccounts';
import PrivacyPolicy from './pages/PrivacyPolicy';
import Account from './pages/Account';

const TABS = [
  { to: '/', label: 'Home' },
  { to: '/beers', label: 'Beers' },
  { to: '/progress', label: 'My Progress' },
  { to: '/account', label: 'Account' },
];

function readAuthState() {
  return { signedIn: Boolean(localStorage.getItem('beer-token')), roles: getRolesFromToken() };
}

function BottomTabBar() {
  const location = useLocation();

  return (
    <nav className="fixed inset-x-0 bottom-0 z-40 flex border-t border-gray-200 bg-white">
      {TABS.map((tab) => {
        const isActive = tab.to === '/' ? location.pathname === '/' : location.pathname.startsWith(tab.to);
        return (
          <Link
            key={tab.to}
            to={tab.to}
            className={`flex-1 py-3 text-center text-sm font-medium ${
              isActive ? 'text-gray-900' : 'text-gray-500'
            }`}
          >
            {tab.label}
          </Link>
        );
      })}
    </nav>
  );
}

function App() {
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

  return (
    <div className="mx-auto max-w-5xl p-4 pb-20 md:p-8 md:pb-24">
      <header className="mb-6">
        <h1 className="m-0 text-2xl font-bold tracking-tight">Beer App</h1>
        <p className="m-0 mt-1 text-sm text-gray-600">
          The tavern&apos;s 200 club, on your phone
        </p>
      </header>

      <Routes>
        <Route path="/" element={<Home />} />
        <Route path="/beers" element={<BeerList />} />
        <Route path="/beers/:id" element={<BeerDetail />} />
        <Route path="/beers/new" element={<BeerForm />} />
        <Route path="/beers/:id/edit" element={<BeerForm />} />
        <Route path="/progress" element={<MyProgress />} />
        <Route path="/account" element={<Account />} />
        <Route path="/my-pin" element={<MyPin />} />
        <Route path="/recommend" element={<RecommendBeer />} />
        <Route path="/admin/dashboard" element={<AdminDashboard />} />
        <Route path="/admin/confirmations" element={<AdminConfirmations />} />
        <Route path="/admin/users" element={<AdminUsers />} />
        <Route path="/admin/beers" element={<AdminBeers />} />
        <Route path="/admin/recommendations" element={<AdminRecommendations />} />
        <Route path="/admin/search-demand" element={<AdminSearchDemand />} />
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

      {auth.signedIn && <BottomTabBar />}
    </div>
  );
}

export default App;
