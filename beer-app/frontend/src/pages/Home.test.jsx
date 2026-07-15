import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import Home from './Home';

function renderHome() {
  return render(
    <MemoryRouter>
      <Home />
    </MemoryRouter>
  );
}

describe('Home', () => {
  it('pitches the mug club, not a generic catalog', () => {
    renderHome();

    expect(
      screen.getByRole('heading', { name: /drink the list\. earn your mug\./i })
    ).toBeInTheDocument();
    expect(screen.getByText(/bartender confirms/i)).toBeInTheDocument();
  });

  it('links to progress, the beer list, and sign-in', () => {
    renderHome();

    expect(screen.getByRole('link', { name: /my progress/i })).toHaveAttribute(
      'href',
      '/progress'
    );
    expect(
      screen.getByRole('link', { name: /browse the beer list/i })
    ).toHaveAttribute('href', '/beers');
    expect(screen.getByRole('link', { name: /sign in/i })).toHaveAttribute(
      'href',
      '/auth'
    );
  });
});
