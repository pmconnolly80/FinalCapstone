import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { fetchMyProgress } from '../lib/api';

function SignedOutPitch() {
  return (
    <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)] sm:p-10">
      <p className="m-0 text-sm font-semibold uppercase tracking-wide text-amber-600">
        The 200 Club
      </p>
      <h2 className="m-0 mt-2 text-3xl font-bold tracking-tight text-gray-900 sm:text-4xl">
        Drink the list. Earn your mug.
      </h2>
      <p className="m-0 mt-3 max-w-prose text-gray-600">
        Work through the tavern&apos;s beer list at your own pace. When you order one, a
        bartender confirms it right on your phone — no paper sheet, no lost progress.
        Finish the list and the mug is yours.
      </p>
      <div className="mt-6 flex flex-col gap-3 sm:flex-row">
        <Link
          to="/progress"
          className="rounded-full bg-gray-900 px-6 py-3 text-center font-medium text-white hover:bg-gray-700"
        >
          My progress
        </Link>
        <Link
          to="/beers"
          className="rounded-full border border-gray-300 bg-white px-6 py-3 text-center font-medium text-gray-900 hover:bg-gray-100"
        >
          Browse the beer list
        </Link>
        <Link
          to="/auth"
          className="rounded-full px-6 py-3 text-center font-medium text-gray-600 hover:text-gray-900"
        >
          Sign in
        </Link>
      </div>
    </section>
  );
}

function Home() {
  const hasToken = Boolean(localStorage.getItem('beer-token'));
  const [progress, setProgress] = useState(null);
  const [error, setError] = useState('');

  useEffect(() => {
    if (!hasToken) return;
    fetchMyProgress()
      .then((data) => setProgress(data))
      .catch(() => setError('Could not load your progress. Try signing in again.'));
  }, [hasToken]);

  if (!hasToken) {
    return <SignedOutPitch />;
  }

  if (error) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <p className="m-0 text-red-700">{error}</p>
      </section>
    );
  }

  if (!progress) {
    return <p>Loading...</p>;
  }

  const percent = Math.min(100, Math.round((progress.confirmedCount / progress.goal) * 100));

  return (
    <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)] sm:p-10">
      <p className="m-0 text-sm font-semibold uppercase tracking-wide text-amber-600">
        Your progress
      </p>
      <p className="m-0 mt-2 text-4xl font-bold tracking-tight text-gray-900">
        {progress.confirmedCount} of {progress.goal}
      </p>
      <div className="mt-4 h-3 overflow-hidden rounded-full bg-gray-200">
        <div
          role="progressbar"
          aria-valuenow={progress.confirmedCount}
          aria-valuemin={0}
          aria-valuemax={progress.goal}
          className="h-full bg-amber-600"
          style={{ width: `${percent}%` }}
        />
      </div>

      {progress.mugEarned && (
        <p className="mt-4 text-lg font-medium text-gray-900">
          🏆 Mug earned
          {progress.mugEarnedAt ? ` on ${new Date(progress.mugEarnedAt).toLocaleDateString()}` : ''} —
          congratulations!
        </p>
      )}

      <div className="mt-6 flex flex-col gap-3 sm:flex-row">
        <Link
          to="/beers"
          className="rounded-full bg-gray-900 px-6 py-3 text-center font-medium text-white hover:bg-gray-700"
        >
          Browse the beer list
        </Link>
        <Link
          to="/progress"
          className="rounded-full border border-gray-300 bg-white px-6 py-3 text-center font-medium text-gray-900 hover:bg-gray-100"
        >
          Full progress detail
        </Link>
      </div>
    </section>
  );
}

export default Home;
