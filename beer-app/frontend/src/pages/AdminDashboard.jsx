import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import { fetchAdminAnomalies, fetchBeerConfirmationCounts, fetchDashboardSummary, getRolesFromToken } from '../lib/api';

const ANOMALY_BADGES = {
  BulkBeerAdd: { label: 'Bulk Beer Add', className: 'bg-yellow-100 text-yellow-800' },
  ConfirmationVelocitySpike: { label: 'Velocity Spike', className: 'bg-orange-100 text-orange-800' },
  OffHoursActivity: { label: 'Off Hours', className: 'bg-red-100 text-red-800' },
  // #81: not really an "anomaly" (it's a crowd-sourced report, not a statistical
  // outlier), but it reuses this same panel rather than a new screen, per the issue's
  // "similar in spirit to the existing anomaly panel" framing.
  UnavailabilityReport: { label: 'Unavailable Report', className: 'bg-purple-100 text-purple-800' },
};

// Admin Dashboard (#59): closes Sprint 5, ties #53-#58 together. Summary cards and the
// anomaly panel are fetched independently (not a single Promise.all) so a broken
// endpoint only blanks its own section, not the whole page.
//
// #78: reframed as "operational health" — this dashboard was implicitly expected to
// also answer "what should I order more of," which it never did. The most/least-
// confirmed beers panel below is a cheap first slice of that real "beer intelligence"
// question (PERSONAS_AND_USAGE.md's "Weekly ritual"); the fuller version (want-list
// demand, anonymized ratings, lapsed-member list) stays deferred to a separate Owner
// Analytics screen once the Engagement/Retention epic is groomed.
function AdminDashboard() {
  const isAdmin = getRolesFromToken().includes('Admin');
  const [summary, setSummary] = useState(null);
  const [summaryError, setSummaryError] = useState('');
  const [anomalies, setAnomalies] = useState(null);
  const [anomaliesError, setAnomaliesError] = useState('');
  const [beerCounts, setBeerCounts] = useState(null);
  const [beerCountsError, setBeerCountsError] = useState('');

  useEffect(() => {
    if (!isAdmin) return;
    fetchDashboardSummary().then(setSummary).catch((error) => setSummaryError(error.message));
    fetchAdminAnomalies().then(setAnomalies).catch((error) => setAnomaliesError(error.message));
    fetchBeerConfirmationCounts().then(setBeerCounts).catch((error) => setBeerCountsError(error.message));
  }, [isAdmin]);

  if (!isAdmin) {
    return (
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">Admin dashboard</h2>
        <p className="mt-2 text-gray-600">Sign in with an admin account to view the dashboard.</p>
      </section>
    );
  }

  const cards = summary && [
    { label: 'Total Beers', value: summary.totalBeers },
    { label: 'Confirmations Today', value: summary.confirmationsToday },
    { label: 'Active Members', value: summary.activeMembers },
    { label: 'Mugs Awarded', value: summary.mugsAwarded },
  ];

  return (
    <div className="grid gap-4">
      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h2 className="m-0 text-xl font-bold">Admin dashboard</h2>
        <p className="mt-1 text-sm text-gray-500">
          Operational health — day-to-day counts and anomalies. Beer-purchasing
          intelligence (demand, ratings, lapsed members) lives in a separate Owner
          Analytics screen once that's built; the most/least-confirmed panel below is
          a first, cheap slice of that pulled forward.
        </p>

        <h3 className="m-0 mt-4 text-lg font-semibold">Operational health</h3>

        {summaryError && <p className="mt-3 text-red-700">{summaryError}</p>}

        {cards && (
          <div className="mt-4 grid grid-cols-2 gap-4 md:grid-cols-4">
            {cards.map((card) => (
              <div key={card.label} className="rounded-2xl bg-white p-4 shadow-[0_8px_24px_rgba(0,0,0,0.06)]">
                <p className="m-0 text-sm text-gray-500">{card.label}</p>
                <p className="m-0 mt-1 text-2xl font-bold">{card.value}</p>
              </div>
            ))}
          </div>
        )}
      </section>

      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h3 className="m-0 text-lg font-semibold">Anomalies</h3>

        {anomaliesError && <p className="mt-3 text-red-700">{anomaliesError}</p>}

        {anomalies && anomalies.length === 0 && (
          <p className="mt-2 text-sm text-gray-500">No anomalies detected.</p>
        )}

        {anomalies && anomalies.length > 0 && (
          <ul className="mt-3 grid gap-2">
            {anomalies.map((anomaly, index) => {
              const badge = ANOMALY_BADGES[anomaly.type] ?? { label: anomaly.type, className: 'bg-gray-100 text-gray-600' };
              return (
                <li key={index} className="flex flex-wrap items-center justify-between gap-2 border-b border-gray-100 pb-2">
                  <span className="flex flex-wrap items-center gap-2">
                    <span className={`rounded-full px-2 py-0.5 text-xs font-medium ${badge.className}`}>
                      {badge.label}
                    </span>
                    <span>{anomaly.summary}</span>
                    {anomaly.actorEmail && <span className="text-sm text-gray-500">({anomaly.actorEmail})</span>}
                  </span>
                  <Link to={anomaly.deepLink} className="text-sm font-medium underline">
                    View
                  </Link>
                </li>
              );
            })}
          </ul>
        )}
      </section>

      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h3 className="m-0 text-lg font-semibold">Most / least confirmed beers</h3>
        <p className="mt-1 text-sm text-gray-500">
          A cheap first slice of beer-purchasing intelligence — what's carrying the
          month, and what nobody's ordered in a while.
        </p>

        {beerCountsError && <p className="mt-3 text-red-700">{beerCountsError}</p>}

        {beerCounts && (
          <div className="mt-3 grid gap-4 sm:grid-cols-2">
            <div>
              <p className="m-0 text-sm font-semibold text-gray-500">Most confirmed</p>
              <ol className="mt-2 grid gap-1 pl-4 text-sm">
                {beerCounts.mostConfirmed.map((beer) => (
                  <li key={beer.beerId}>
                    <Link to={`/beers/${beer.beerId}`} className="underline">
                      {beer.name}
                    </Link>{' '}
                    ({beer.confirmedCount})
                  </li>
                ))}
              </ol>
            </div>
            <div>
              <p className="m-0 text-sm font-semibold text-gray-500">Least confirmed</p>
              <ol className="mt-2 grid gap-1 pl-4 text-sm">
                {beerCounts.leastConfirmed.map((beer) => (
                  <li key={beer.beerId}>
                    <Link to={`/beers/${beer.beerId}`} className="underline">
                      {beer.name}
                    </Link>{' '}
                    ({beer.confirmedCount})
                  </li>
                ))}
              </ol>
            </div>
          </div>
        )}
      </section>

      <section className="rounded-2xl bg-white p-6 shadow-[0_10px_30px_rgba(0,0,0,0.06)]">
        <h3 className="m-0 text-lg font-semibold">Quick links</h3>
        <div className="mt-3 flex flex-wrap gap-2">
          <Link
            to="/admin/beers"
            className="rounded-full border-0 bg-gray-900 px-4 py-2 text-sm font-medium text-white hover:bg-gray-700"
          >
            Manage Beers
          </Link>
          <Link
            to="/admin/users"
            className="rounded-full border-0 bg-gray-900 px-4 py-2 text-sm font-medium text-white hover:bg-gray-700"
          >
            Manage Users
          </Link>
          <Link
            to="/admin/confirmations"
            className="rounded-full border-0 bg-gray-900 px-4 py-2 text-sm font-medium text-white hover:bg-gray-700"
          >
            Confirmations
          </Link>
        </div>
      </section>
    </div>
  );
}

export default AdminDashboard;
