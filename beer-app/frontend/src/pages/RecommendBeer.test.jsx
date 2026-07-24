import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import RecommendBeer from './RecommendBeer';
import { submitRecommendation } from '../lib/api';

vi.mock('../lib/api');

function renderPage(initialEntries = ['/recommend']) {
  return render(
    <MemoryRouter initialEntries={initialEntries}>
      <Routes>
        <Route path="/recommend" element={<RecommendBeer />} />
        <Route path="/beers" element={<p>Beer List Page</p>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('RecommendBeer', () => {
  afterEach(() => {
    vi.resetAllMocks();
  });

  it('submits a standalone plain-text recommendation', async () => {
    const user = userEvent.setup();
    submitRecommendation.mockResolvedValue({});

    renderPage();
    await user.type(screen.getByLabelText('Beer name'), 'Some Great IPA');
    await user.click(screen.getByRole('button', { name: 'Submit recommendation' }));

    expect(submitRecommendation).toHaveBeenCalledWith(
      expect.objectContaining({ beerName: 'Some Great IPA' })
    );
    expect(await screen.findByText('Thanks!')).toBeInTheDocument();
  });

  it('rejects submission without a beer name', async () => {
    const user = userEvent.setup();

    renderPage();
    await user.click(screen.getByRole('button', { name: 'Submit recommendation' }));

    expect(screen.getByText('A beer name is required.')).toBeInTheDocument();
    expect(submitRecommendation).not.toHaveBeenCalled();
  });

  it('prefills from location.state (a search-hit "Recommend this beer" click)', async () => {
    const user = userEvent.setup();
    submitRecommendation.mockResolvedValue({});

    render(
      <MemoryRouter
        initialEntries={[
          { pathname: '/recommend', state: { beerName: 'Duvel', breweryName: 'Duvel Moortgat', externalCatalogBeerId: 'cb-1' } },
        ]}
      >
        <Routes>
          <Route path="/recommend" element={<RecommendBeer />} />
        </Routes>
      </MemoryRouter>
    );

    expect(screen.getByLabelText('Beer name')).toHaveValue('Duvel');
    expect(screen.getByLabelText('Brewery (optional)')).toHaveValue('Duvel Moortgat');

    await user.click(screen.getByRole('button', { name: 'Submit recommendation' }));

    expect(submitRecommendation).toHaveBeenCalledWith(
      expect.objectContaining({ beerName: 'Duvel', breweryName: 'Duvel Moortgat', externalCatalogBeerId: 'cb-1' })
    );
  });

  it('shows the API error message on failure', async () => {
    const user = userEvent.setup();
    submitRecommendation.mockRejectedValue(new Error('Failed to submit recommendation'));

    renderPage();
    await user.type(screen.getByLabelText('Beer name'), 'Duvel');
    await user.click(screen.getByRole('button', { name: 'Submit recommendation' }));

    expect(await screen.findByText('Failed to submit recommendation')).toBeInTheDocument();
  });
});
