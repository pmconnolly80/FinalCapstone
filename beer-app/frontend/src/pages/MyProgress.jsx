import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { fetchMyProgress } from '../lib/api';

const cardStyle = { background: '#fff', borderRadius: 16, padding: 20, boxShadow: '0 10px 30px rgba(0,0,0,0.06)' };

function MyProgress() {
  const [progress, setProgress] = useState(null);
  const [error, setError] = useState('');
  const hasToken = Boolean(localStorage.getItem('beer-token'));

  useEffect(() => {
    if (!hasToken) return;
    fetchMyProgress()
      .then((data) => setProgress(data))
      .catch(() => setError('Could not load your progress. Try signing in again.'));
  }, [hasToken]);

  if (!hasToken) {
    return (
      <div style={cardStyle}>
        <h2>My Progress</h2>
        <p><Link to="/auth">Sign in</Link> to see your mug club progress.</p>
      </div>
    );
  }

  if (error) return <div style={cardStyle}><p>{error}</p></div>;
  if (!progress) return <p>Loading...</p>;

  const percent = Math.min(100, Math.round((progress.confirmedCount / progress.goal) * 100));

  return (
    <div style={cardStyle}>
      <h2 style={{ marginTop: 0 }}>My Progress</h2>
      <p style={{ fontSize: 40, fontWeight: 700, margin: '4px 0' }}>
        {progress.confirmedCount} of {progress.goal}
      </p>
      <div style={{ background: '#e5e7eb', borderRadius: 8, height: 12, overflow: 'hidden' }}>
        <div
          role="progressbar"
          aria-valuenow={progress.confirmedCount}
          aria-valuemin={0}
          aria-valuemax={progress.goal}
          style={{ background: '#d97706', height: '100%', width: `${percent}%` }}
        />
      </div>
      {progress.mugEarned && (
        <p style={{ fontSize: 20, marginTop: 12 }}>🏆 Mug earned — congratulations!</p>
      )}

      <h3 style={{ marginBottom: 8 }}>Confirmed beers</h3>
      {progress.confirmations.length === 0 ? (
        <p>
          Nothing confirmed yet — <Link to="/beers">find the beer you&apos;re drinking</Link> and
          hand your phone to the bartender.
        </p>
      ) : (
        <ul style={{ listStyle: 'none', padding: 0, margin: 0 }}>
          {progress.confirmations.map((c) => (
            <li key={c.beerId} style={{ padding: '10px 0', borderBottom: '1px solid #e5e7eb' }}>
              <Link to={`/beers/${c.beerId}`} style={{ fontWeight: 600 }}>{c.name}</Link>
              <span style={{ color: '#4b5563' }}> — {c.brewery} · {c.style}</span>
              <div style={{ color: '#6b7280', fontSize: 14 }}>
                {new Date(c.confirmedAt).toLocaleDateString()}
              </div>
            </li>
          ))}
        </ul>
      )}
    </div>
  );
}

export default MyProgress;
