import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import PrivacyPolicy from './PrivacyPolicy';

describe('PrivacyPolicy', () => {
  it('renders the privacy policy heading and data-deletion explanation', () => {
    render(<PrivacyPolicy />);

    expect(screen.getByRole('heading', { name: 'Privacy policy' })).toBeInTheDocument();
    expect(screen.getByText(/anonymize your account/i)).toBeInTheDocument();
  });
});
