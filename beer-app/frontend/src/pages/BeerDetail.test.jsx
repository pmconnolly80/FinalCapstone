import { render, screen } from '@testing-library/react';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import BeerDetail from './BeerDetail';
import { fetchBeer } from '../lib/api';

vi.mock('../lib/api');

function renderBeerDetail(id = '1') {
  return render(
    <MemoryRouter initialEntries={[`/beers/${id}`]}>
      <Routes>
        <Route path="/beers/:id" element={<BeerDetail />} />
      </Routes>
    </MemoryRouter>
  );
}

describe('BeerDetail', () => {
  afterEach(() => {
    vi.resetAllMocks();
  });

  it('shows a loading state before the beer resolves', () => {
    fetchBeer.mockReturnValue(new Promise(() => {}));

    renderBeerDetail();

    expect(screen.getByText('Loading...')).toBeInTheDocument();
  });

  it('renders the beer once loaded', async () => {
    fetchBeer.mockResolvedValue({
      id: 1,
      name: 'Duvel',
      brewery: 'Duvel Moortgat',
      style: 'Belgian Strong Golden Ale',
      description: 'Deceptively light-bodied with a dry, spicy finish.',
    });

    renderBeerDetail('1');

    expect(await screen.findByText('Duvel')).toBeInTheDocument();
    expect(screen.getByText('Duvel Moortgat')).toBeInTheDocument();
    expect(screen.getByText('Belgian Strong Golden Ale')).toBeInTheDocument();
    expect(fetchBeer).toHaveBeenCalledWith('1');
  });
});
