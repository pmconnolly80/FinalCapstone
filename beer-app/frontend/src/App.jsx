import { Routes, Route, Link } from 'react-router-dom';
import BeerList from './pages/BeerList';
import BeerDetail from './pages/BeerDetail';
import BeerForm from './pages/BeerForm';
import AuthPage from './pages/AuthPage';
import MyProgress from './pages/MyProgress';

function App() {
  return (
    <div className="app-shell" style={{ maxWidth: 980, margin: '0 auto', padding: 16 }}>
      <header style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center', marginBottom: 20 }}>
        <div>
          <h1 style={{ margin: 0 }}>Beer App</h1>
          <p style={{ margin: '4px 0 0', color: '#4b5563' }}>Mobile-first beer catalog</p>
        </div>
        <nav style={{ display: 'flex', gap: 12, flexWrap: 'wrap' }}>
          <Link to="/">Home</Link>
          <Link to="/beers">Beers</Link>
          <Link to="/progress">My Progress</Link>
          <Link to="/beers/new">Add Beer</Link>
          <Link to="/auth">Sign in</Link>
        </nav>
      </header>

      <Routes>
        <Route path="/" element={<div style={{ background: '#fff', borderRadius: 16, padding: 20, boxShadow: '0 10px 30px rgba(0,0,0,0.06)' }}><h2>Start exploring beers</h2><p>Browse your catalog on the go.</p></div>} />
        <Route path="/beers" element={<BeerList />} />
        <Route path="/beers/:id" element={<BeerDetail />} />
        <Route path="/beers/new" element={<BeerForm />} />
        <Route path="/beers/:id/edit" element={<BeerForm />} />
        <Route path="/progress" element={<MyProgress />} />
        <Route path="/auth" element={<AuthPage />} />
      </Routes>
    </div>
  );
}

export default App;
