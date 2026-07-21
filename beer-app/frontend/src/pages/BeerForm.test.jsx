import { render, screen } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import BeerForm from './BeerForm';
import { fetchBeer, saveBeer } from '../lib/api';

vi.mock('../lib/api');

function renderAt(path) {
  return render(
    <MemoryRouter initialEntries={[path]}>
      <Routes>
        <Route path="/beers/new" element={<BeerForm />} />
        <Route path="/beers/:id/edit" element={<BeerForm />} />
        <Route path="/beers" element={<div>Beer List Page</div>} />
      </Routes>
    </MemoryRouter>
  );
}

describe('BeerForm', () => {
  afterEach(() => {
    vi.resetAllMocks();
  });

  it('submits the entered values and navigates back to the list on success', async () => {
    saveBeer.mockResolvedValue({ id: 9 });
    const user = userEvent.setup();

    renderAt('/beers/new');
    await user.type(screen.getByPlaceholderText('Name'), 'Duvel');
    await user.type(screen.getByPlaceholderText('Brewery'), 'Duvel Moortgat');
    await user.type(screen.getByPlaceholderText('Style'), 'Belgian Strong Golden Ale');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(saveBeer).toHaveBeenCalledWith(
      expect.objectContaining({
        name: 'Duvel',
        brewery: 'Duvel Moortgat',
        style: 'Belgian Strong Golden Ale',
      }),
      undefined
    );
    expect(await screen.findByText('Beer List Page')).toBeInTheDocument();
  });

  it('loads and pre-fills the existing beer in edit mode', async () => {
    fetchBeer.mockResolvedValue({
      name: 'Fat Tire',
      brewery: 'New Belgium',
      style: 'Amber Ale',
      description: 'Toasty malt sweetness.',
    });

    renderAt('/beers/42/edit');

    expect(await screen.findByDisplayValue('Fat Tire')).toBeInTheDocument();
    expect(screen.getByDisplayValue('New Belgium')).toBeInTheDocument();
    expect(fetchBeer).toHaveBeenCalledWith('42');
  });

  it('submits blank beer-nerd stat fields as null rather than empty strings', async () => {
    saveBeer.mockResolvedValue({ id: 9 });
    const user = userEvent.setup();

    renderAt('/beers/new');
    await user.type(screen.getByPlaceholderText('Name'), 'Duvel');
    await user.type(screen.getByPlaceholderText('Brewery'), 'Duvel Moortgat');
    await user.type(screen.getByPlaceholderText('Style'), 'Belgian Strong Golden Ale');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(saveBeer).toHaveBeenCalledWith(
      expect.objectContaining({ abv: null, ibu: null, styleFamily: null, class: null }),
      undefined
    );
  });

  it('submits entered beer-nerd stats as numbers', async () => {
    saveBeer.mockResolvedValue({ id: 9 });
    const user = userEvent.setup();

    renderAt('/beers/new');
    await user.type(screen.getByPlaceholderText('Name'), 'Duvel');
    await user.type(screen.getByPlaceholderText('Brewery'), 'Duvel Moortgat');
    await user.type(screen.getByPlaceholderText('Style'), 'Belgian Strong Golden Ale');
    await user.type(screen.getByPlaceholderText('ABV %'), '8.5');
    await user.type(screen.getByPlaceholderText('IBU'), '30');
    await user.type(screen.getByPlaceholderText('Style family'), 'Strong Golden Ale');
    await user.selectOptions(screen.getByLabelText('Class'), 'Ale');
    await user.click(screen.getByRole('button', { name: 'Save' }));

    expect(saveBeer).toHaveBeenCalledWith(
      expect.objectContaining({ abv: 8.5, ibu: 30, styleFamily: 'Strong Golden Ale', class: 'Ale' }),
      undefined
    );
  });

  it('pre-fills beer-nerd stats in edit mode when present', async () => {
    fetchBeer.mockResolvedValue({
      name: 'Fat Tire',
      brewery: 'New Belgium',
      style: 'Amber Ale',
      abv: 5.2,
      ibu: 18,
      styleFamily: 'Amber',
      class: 'Ale',
    });

    renderAt('/beers/42/edit');

    expect(await screen.findByDisplayValue('5.2')).toBeInTheDocument();
    expect(screen.getByDisplayValue('18')).toBeInTheDocument();
    expect(screen.getByDisplayValue('Amber')).toBeInTheDocument();
    expect(screen.getByLabelText('Class')).toHaveValue('Ale');
  });
});
