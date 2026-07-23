function PrivacyPolicy() {
  return (
    <div className="mx-auto max-w-2xl space-y-4 text-sm text-gray-700">
      <h2 className="text-2xl font-bold tracking-tight text-gray-900">Privacy policy</h2>
      <p>
        This app tracks your progress through the tavern&apos;s mug club: which beers on the
        menu you&apos;ve had, confirmed by a bartender at the time you drink them.
      </p>
      <h3 className="text-lg font-semibold text-gray-900">What we collect</h3>
      <ul className="list-disc space-y-1 pl-5">
        <li>Your email address and password (or, if you sign in with Google or Facebook, the
          verified email and name your provider shares with us).</li>
        <li>Which beers you&apos;ve had confirmed, and when.</li>
        <li>Whether you&apos;ve opted in to marketing emails.</li>
      </ul>
      <h3 className="text-lg font-semibold text-gray-900">How we use it</h3>
      <p>
        Your email identifies your account and lets a bartender confirm beers against your
        progress. We only send marketing email if you&apos;ve opted in. We don&apos;t sell or
        share your data with third parties beyond the sign-in providers you choose to use.
      </p>
      <h3 className="text-lg font-semibold text-gray-900">Deleting your data</h3>
      <p>
        If you signed in with Facebook and remove this app from your Facebook account, we
        anonymize your account&apos;s email and name and unlink all sign-in methods. Your
        confirmed-beer history stays on the tavern&apos;s records under the anonymized
        account, the same way a paper punch-card would stay behind the bar.
      </p>
    </div>
  );
}

export default PrivacyPolicy;
