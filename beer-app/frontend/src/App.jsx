import { Routes, Route, Link } from 'react-router-dom';
import Home from './pages/Home';
import BeerList from './pages/BeerList';
import BeerDetail from './pages/BeerDetail';
import BeerForm from './pages/BeerForm';
import AuthPage from './pages/AuthPage';
import MyProgress from './pages/MyProgress';
import MyPin from './pages/MyPin';

const navLinkClass =
  'rounded-lg px-3 py-2 text-sm font-medium text-gray-700 hover:bg-gray-200 hover:text-gray-900';

function App() {
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
          <Link className={navLinkClass} to="/beers/new">
            Add Beer
          </Link>
          <Link className={navLinkClass} to="/my-pin">
            My PIN
          </Link>
          <Link className={navLinkClass} to="/auth">
            Sign in
          </Link>
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
        <Route path="/auth" element={<AuthPage />} />
      </Routes>
    </div>
  );
}

export default App;
