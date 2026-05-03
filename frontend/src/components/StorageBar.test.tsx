import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { CompanyInfo } from '../types'

// ── Module mocks ──────────────────────────────────────────────────────────────

const mockGetMyCompany = vi.fn()

vi.mock('../services/api', () => ({
  apiService: { getMyCompany: mockGetMyCompany },
}))

const { StorageBar } = await import('./StorageBar')

// ── Helpers ───────────────────────────────────────────────────────────────────

function makeCompany(overrides: Partial<CompanyInfo> = {}): CompanyInfo {
  return {
    id: 1,
    name: 'Test Firm',
    subscriptionTier: 'Trial',
    subscriptionEndDate: null,
    storageUsedBytes: 0,
    storageQuotaBytes: 1 * 1024 * 1024 * 1024, // 1 GB
    ...overrides,
  }
}

function renderStorageBar() {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: false } },
  })
  return render(
    <QueryClientProvider client={qc}>
      <StorageBar />
    </QueryClientProvider>
  )
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('StorageBar', () => {
  beforeEach(() => {
    vi.clearAllMocks()
  })

  afterEach(() => {
    vi.unstubAllGlobals()
  })

  // ── Loading state ───────────────────────────────────────────────────────

  it('renders skeleton while loading', () => {
    // Never resolves — keeps isLoading=true indefinitely
    mockGetMyCompany.mockReturnValue(new Promise(() => {}))

    const { container } = renderStorageBar()

    // Skeleton elements should be present and have the animate-pulse class
    expect(container.querySelector('.animate-pulse')).toBeTruthy()
    // No storage text while loading
    expect(screen.queryByText(/of/i)).toBeNull()
  })

  // ── Tier badge ──────────────────────────────────────────────────────────

  it.each([
    ['Trial',        'Trial'],
    ['Basic',        'Basic'],
    ['Professional', 'Professional'],
    ['Enterprise',   'Enterprise'],
  ] as const)('shows %s tier badge when tier is %s', async (tier, label) => {
    mockGetMyCompany.mockResolvedValue(makeCompany({ subscriptionTier: tier }))

    renderStorageBar()

    expect(await screen.findByText(label)).toBeTruthy()
  })

  // ── Storage text ────────────────────────────────────────────────────────

  it('displays used and total storage in human-readable format', async () => {
    mockGetMyCompany.mockResolvedValue(
      makeCompany({ storageUsedBytes: 500 * 1024 * 1024, storageQuotaBytes: 1024 * 1024 * 1024 })
    )

    renderStorageBar()

    // "500 MB of 1 GB used"
    expect(await screen.findByText(/500 MB/i)).toBeTruthy()
    expect(screen.getByText(/1 GB/i)).toBeTruthy()
  })

  it('shows 0 B when storage is empty', async () => {
    mockGetMyCompany.mockResolvedValue(
      makeCompany({ storageUsedBytes: 0, storageQuotaBytes: 1024 * 1024 * 1024 })
    )

    renderStorageBar()

    expect(await screen.findByText(/0 B/i)).toBeTruthy()
  })

  // ── Progress bar accessibility ──────────────────────────────────────────

  it('renders a progressbar with correct aria-valuenow', async () => {
    // 512 MB used out of 1 GB = 50%
    mockGetMyCompany.mockResolvedValue(
      makeCompany({
        storageUsedBytes: 512 * 1024 * 1024,
        storageQuotaBytes: 1024 * 1024 * 1024,
      })
    )

    renderStorageBar()

    const bar = await screen.findByRole('progressbar')
    expect(bar).toHaveAttribute('aria-valuenow', '50')
    expect(bar).toHaveAttribute('aria-valuemin', '0')
    expect(bar).toHaveAttribute('aria-valuemax', '100')
  })

  it('clamps aria-valuenow to 100 when used exceeds quota', async () => {
    mockGetMyCompany.mockResolvedValue(
      makeCompany({
        storageUsedBytes: 2 * 1024 * 1024 * 1024,
        storageQuotaBytes: 1024 * 1024 * 1024,
      })
    )

    renderStorageBar()

    const bar = await screen.findByRole('progressbar')
    expect(Number(bar.getAttribute('aria-valuenow'))).toBeLessThanOrEqual(100)
  })

  // ── Warning message ─────────────────────────────────────────────────────

  it('shows upgrade warning when storage is >= 90%', async () => {
    // 950 MB out of 1 GB = 92.8%
    mockGetMyCompany.mockResolvedValue(
      makeCompany({
        storageUsedBytes: 950 * 1024 * 1024,
        storageQuotaBytes: 1024 * 1024 * 1024,
      })
    )

    renderStorageBar()

    expect(await screen.findByText(/Storage almost full/i)).toBeTruthy()
  })

  it('does not show upgrade warning when storage is below 90%', async () => {
    // 500 MB out of 1 GB = 48.8%
    mockGetMyCompany.mockResolvedValue(
      makeCompany({
        storageUsedBytes: 500 * 1024 * 1024,
        storageQuotaBytes: 1024 * 1024 * 1024,
      })
    )

    renderStorageBar()

    await screen.findByRole('progressbar')
    expect(screen.queryByText(/Storage almost full/i)).toBeNull()
  })

  // ── Trial countdown ─────────────────────────────────────────────────────

  it('shows days-left countdown for Trial tier', async () => {
    const futureDate = new Date(Date.now() + 10 * 24 * 60 * 60 * 1000).toISOString()
    mockGetMyCompany.mockResolvedValue(
      makeCompany({ subscriptionTier: 'Trial', subscriptionEndDate: futureDate })
    )

    renderStorageBar()

    expect(await screen.findByText(/10d left/i)).toBeTruthy()
  })

  it('shows 0d left when trial has expired', async () => {
    const pastDate = new Date(Date.now() - 24 * 60 * 60 * 1000).toISOString()
    mockGetMyCompany.mockResolvedValue(
      makeCompany({ subscriptionTier: 'Trial', subscriptionEndDate: pastDate })
    )

    renderStorageBar()

    expect(await screen.findByText(/0d left/i)).toBeTruthy()
  })

  it('does not show days-left for non-Trial tiers', async () => {
    const futureDate = new Date(Date.now() + 30 * 24 * 60 * 60 * 1000).toISOString()
    mockGetMyCompany.mockResolvedValue(
      makeCompany({ subscriptionTier: 'Basic', subscriptionEndDate: futureDate })
    )

    renderStorageBar()

    await screen.findByText('Basic')
    expect(screen.queryByText(/d left/i)).toBeNull()
  })

  it('does not show days-left when subscriptionEndDate is null', async () => {
    mockGetMyCompany.mockResolvedValue(
      makeCompany({ subscriptionTier: 'Trial', subscriptionEndDate: null })
    )

    renderStorageBar()

    await screen.findByText('Trial')
    expect(screen.queryByText(/d left/i)).toBeNull()
  })

  // ── Renders nothing when API returns null/no data ───────────────────────

  it('renders nothing when query returns no data', async () => {
    mockGetMyCompany.mockResolvedValue(null)

    const { container } = renderStorageBar()

    // Wait for the loading state to clear, then nothing should be rendered
    await waitFor(() => expect(container.firstChild).toBeNull())
  })
})
