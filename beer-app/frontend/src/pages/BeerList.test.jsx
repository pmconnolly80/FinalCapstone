import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import BeerList from './BeerList';
import { fetchBeers } from '../lib/api';

vi.mock('../lib/api');

function renderBeerList() {
  return render(
    <MemoryRouter>
      <BeerList />
    </MemoryRouter>
  );
}

describe('BeerList', () => {
  afterEach(() => {
    vi.resetAllMocks();
  });

  it('shows a loading state before the beers resolve', () => {
    fetchBeers.mockReturnValue(new Promise(() => {}));

    renderBeerList();

    expect(screen.getByText('Loading beers…')).toBeInTheDocument();
  });

  it('renders one entry per beer once loaded', async () => {
    fetchBeers.mockResolvedValue([
      { id: 1, name: 'Pale Ale', brewery: 'Sierra Nevada', style: 'American Pale Ale' },
      { id: 2, name: 'Hefeweizen', brewery: 'Weihenstephaner', style: 'German Wheat Beer' },
    ]);

    renderBeerList();

    expect(await screen.findByText('Pale Ale')).toBeInTheDocument();
    expect(screen.getByText('Hefeweizen')).toBeInTheDocument();
    expect(screen.getByText(/Sierra Nevada/)).toBeInTheDocument();
  });

  it('shows an empty state when there are no beers', async () => {
    fetchBeers.mockResolvedValue([]);

    renderBeerList();

    expect(await screen.findByText('No beers yet.')).toBeInTheDocument();
  });
});
