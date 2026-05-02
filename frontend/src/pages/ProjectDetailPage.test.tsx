import { describe, it, expect, vi, beforeEach } from 'vitest'
import { render, screen, waitFor } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { MemoryRouter, Route, Routes } from 'react-router-dom'
import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import type { User, AuthContextType } from '../types/auth'
import type { Document, Project, UploadUrlResponse } from '../types'

// ── Module mocks (must be hoisted) ───────────────────────────────────────────

const mockUseAuth = vi.fn<() => AuthContextType>()

vi.mock('../contexts/AuthContext', () => ({
  useAuth: () => mockUseAuth(),
}))

const mockShowToast = vi.fn()
vi.mock('../contexts/ToastContext', () => ({
  useToast: () => ({ showToast: mockShowToast }),
}))

const mockApiService = {
  getProject: vi.fn(),
  getProjectDocuments: vi.fn(),
  updateProject: vi.fn(),
  deleteProject: vi.fn(),
  deleteDocument: vi.fn(),
  generateUploadUrl: vi.fn(),
  confirmUpload: vi.fn(),
  getDownloadUrl: vi.fn(),
}

vi.mock('../services/api', () => ({
  apiService: mockApiService,
}))

// ── Dynamic import (after mocks are in place) ─────────────────────────────────
const { ProjectDetailPage } = await import('./ProjectDetailPage')

// ── Helpers ──────────────────────────────────────────────────────────────────

const PROJECT_ID = 42

const sampleProject: Project = {
  id: PROJECT_ID,
  companyId: 1,
  name: 'Test Project',
  description: 'A test project',
  status: 'Active',
  createdAt: '2025-01-01T00:00:00Z',
  documentCount: 0,
}

const sampleDocument: Document = {
  id: 1,
  projectId: PROJECT_ID,
  fileName: 'brief.pdf',
  fileExtension: '.pdf',
  fileSizeBytes: 2048,
  documentType: 'Brief',
  version: 1,
  isLatestVersion: true,
  uploadedBy: 'Jane Owner',
  createdAt: '2025-01-15T10:00:00Z',
}

function makeUser(role: User['role']): User {
  return {
    id: 1,
    email: 'user@test.com',
    firstName: 'Jane',
    lastName: 'Owner',
    role,
    companyId: 1,
    isActive: true,
    isEmailVerified: true,
  }
}

function makeAuthContext(role: User['role']): AuthContextType {
  const user = makeUser(role)
  return {
    user,
    token: 'fake-token',
    login: vi.fn(),
    register: vi.fn(),
    logout: vi.fn(),
    forgotPassword: vi.fn(),
    resetPassword: vi.fn(),
    isAuthenticated: true,
    isLoading: false,
  }
}

function renderPage(role: User['role'] = 'CompanyOwner') {
  const qc = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  })
  mockUseAuth.mockReturnValue(makeAuthContext(role))

  return render(
    <QueryClientProvider client={qc}>
      <MemoryRouter initialEntries={[`/projects/${PROJECT_ID}`]}>
        <Routes>
          <Route path="/projects/:id" element={<ProjectDetailPage />} />
          <Route path="/projects" element={<div>Projects List</div>} />
        </Routes>
      </MemoryRouter>
    </QueryClientProvider>
  )
}

// ── Tests ─────────────────────────────────────────────────────────────────────

describe('ProjectDetailPage', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    mockApiService.getProject.mockResolvedValue(sampleProject)
    mockApiService.getProjectDocuments.mockResolvedValue([])
  })

  // ── Helper to wait for project heading ───────────────────────────────────
  // Both the breadcrumb <span> and the <h1> contain "Test Project", so we use
  // the heading role to uniquely identify the h1.
  const waitForHeading = () => screen.findByRole('heading', { name: 'Test Project' })

  // ── Loading & rendering ──────────────────────────────────────────────────

  it('shows loading skeleton while project is fetching', () => {
    // Make the promise never resolve to keep loading state
    mockApiService.getProject.mockReturnValue(new Promise(() => {}))
    renderPage()
    // Skeleton divs have animate-pulse class — check at least one is present
    const skeleton = document.querySelector('.animate-pulse')
    expect(skeleton).toBeInTheDocument()
  })

  it('renders project name after loading', async () => {
    renderPage()
    await waitForHeading()
  })

  it('shows "Project not found" when API returns null', async () => {
    mockApiService.getProject.mockResolvedValue(null)
    renderPage()
    await screen.findByText(/Project not found/i)
  })

  // ── Documents list ───────────────────────────────────────────────────────

  it('shows empty state when there are no documents', async () => {
    mockApiService.getProjectDocuments.mockResolvedValue([])
    renderPage()
    await waitForHeading()
    await screen.findByText(/No documents yet/i)
  })

  it('renders document rows when documents exist', async () => {
    mockApiService.getProjectDocuments.mockResolvedValue([sampleDocument])
    renderPage()
    await screen.findByText('brief.pdf')
    // documentType is rendered inside a paragraph with other text, use regex
    expect(screen.getByText(/Brief/)).toBeInTheDocument()
    expect(screen.getByText(/Jane Owner/)).toBeInTheDocument()
  })

  // ── Upload button visibility ──────────────────────────────────────────────

  it('shows Upload button for CompanyOwner', async () => {
    renderPage('CompanyOwner')
    await waitForHeading()
    expect(screen.getByRole('button', { name: 'Upload Document' })).toBeInTheDocument()
  })

  it('shows Upload button for Admin', async () => {
    renderPage('Admin')
    await waitForHeading()
    expect(screen.getByRole('button', { name: 'Upload Document' })).toBeInTheDocument()
  })

  it('shows Upload button for User role', async () => {
    renderPage('User')
    await waitForHeading()
    expect(screen.getByRole('button', { name: 'Upload Document' })).toBeInTheDocument()
  })

  it('hides Upload button for Viewer role', async () => {
    renderPage('Viewer')
    await waitForHeading()
    expect(screen.queryByRole('button', { name: 'Upload Document' })).not.toBeInTheDocument()
  })

  // ── Upload modal ─────────────────────────────────────────────────────────

  it('opens upload modal when Upload button is clicked', async () => {
    renderPage()
    await waitForHeading()

    await userEvent.click(screen.getByRole('button', { name: 'Upload Document' }))

    expect(screen.getByRole('dialog')).toBeInTheDocument()
    expect(screen.getByLabelText(/document type/i)).toBeInTheDocument()
  })

  it('closes upload modal when Cancel button is clicked', async () => {
    renderPage()
    await waitForHeading()
    await userEvent.click(screen.getByRole('button', { name: 'Upload Document' }))

    // Cancel button is inside the upload dialog footer
    await userEvent.click(screen.getByRole('button', { name: 'Cancel' }))

    await waitFor(() => {
      expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
    })
  })

  // ── File size validation ──────────────────────────────────────────────────

  it('shows error when selected file exceeds 50 MB', async () => {
    renderPage()
    await waitForHeading()
    await userEvent.click(screen.getByRole('button', { name: 'Upload Document' }))

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const bigFile = new File([new ArrayBuffer(51 * 1024 * 1024)], 'huge.pdf', { type: 'application/pdf' })
    Object.defineProperty(bigFile, 'size', { value: 51 * 1024 * 1024 })

    await userEvent.upload(fileInput, bigFile)

    // The error p has role="alert" with specific message text
    await screen.findByText('File must be 50 MB or smaller.')
  })

  // ── Successful upload flow ────────────────────────────────────────────────

  it('calls generateUploadUrl, fetch, confirmUpload on successful upload', async () => {
    const uploadUrlResponse: UploadUrlResponse = {
      documentId: 99,
      uploadUrl: 'https://fake.blob/upload?sas=token',
      blobName: '42/uuid_brief.pdf',
      expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
    }
    mockApiService.generateUploadUrl.mockResolvedValue(uploadUrlResponse)
    mockApiService.confirmUpload.mockResolvedValue(sampleDocument)

    // Mock global fetch for the blob PUT
    const mockFetch = vi.fn().mockResolvedValue({ ok: true, status: 201 })
    vi.stubGlobal('fetch', mockFetch)

    renderPage()
    await waitForHeading()
    await userEvent.click(screen.getByRole('button', { name: 'Upload Document' }))

    // Select document type (the select has id="upload-doc-type", label text "Document Type")
    const typeSelect = screen.getByLabelText(/document type/i)
    await userEvent.selectOptions(typeSelect, '2') // Brief

    // Upload a valid file
    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['pdf content'], 'brief.pdf', { type: 'application/pdf' })
    await userEvent.upload(fileInput, file)

    // Submit — button text is "Upload" (not "Upload Document")
    await userEvent.click(screen.getByRole('button', { name: 'Upload' }))

    await waitFor(() => {
      expect(mockApiService.generateUploadUrl).toHaveBeenCalledWith(
        expect.objectContaining({ projectId: PROJECT_ID, fileName: 'brief.pdf' })
      )
    })

    await waitFor(() => {
      expect(mockFetch).toHaveBeenCalledWith(
        uploadUrlResponse.uploadUrl,
        expect.objectContaining({ method: 'PUT' })
      )
    })

    await waitFor(() => {
      expect(mockApiService.confirmUpload).toHaveBeenCalledWith(99)
    })

    await waitFor(() => {
      expect(mockShowToast).toHaveBeenCalledWith('Document uploaded successfully', 'success')
    })

    vi.unstubAllGlobals()
  })

  it('shows error toast when upload fails', async () => {
    mockApiService.generateUploadUrl.mockRejectedValue(new Error('Server error'))

    renderPage()
    await waitForHeading()
    await userEvent.click(screen.getByRole('button', { name: 'Upload Document' }))

    const typeSelect = screen.getByLabelText(/document type/i)
    await userEvent.selectOptions(typeSelect, '2')

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['pdf'], 'brief.pdf', { type: 'application/pdf' })
    await userEvent.upload(fileInput, file)

    await userEvent.click(screen.getByRole('button', { name: 'Upload' }))

    await waitFor(() => {
      expect(mockShowToast).toHaveBeenCalledWith('Upload failed. Please try again.', 'error')
    })
  })

  it('shows error toast when blob PUT fails', async () => {
    const uploadUrlResponse: UploadUrlResponse = {
      documentId: 99,
      uploadUrl: 'https://fake.blob/upload?sas=token',
      blobName: '42/uuid_brief.pdf',
      expiresAt: new Date(Date.now() + 15 * 60 * 1000).toISOString(),
    }
    mockApiService.generateUploadUrl.mockResolvedValue(uploadUrlResponse)

    const mockFetch = vi.fn().mockResolvedValue({ ok: false, status: 403 })
    vi.stubGlobal('fetch', mockFetch)

    renderPage()
    await waitForHeading()
    await userEvent.click(screen.getByRole('button', { name: 'Upload Document' }))

    const typeSelect = screen.getByLabelText(/document type/i)
    await userEvent.selectOptions(typeSelect, '3')

    const fileInput = document.querySelector('input[type="file"]') as HTMLInputElement
    const file = new File(['content'], 'motion.pdf', { type: 'application/pdf' })
    await userEvent.upload(fileInput, file)

    await userEvent.click(screen.getByRole('button', { name: 'Upload' }))

    await waitFor(() => {
      expect(mockShowToast).toHaveBeenCalledWith('Upload failed. Please try again.', 'error')
    })

    vi.unstubAllGlobals()
  })

  // ── Download ──────────────────────────────────────────────────────────────

  it('calls getDownloadUrl when download button clicked', async () => {
    mockApiService.getProjectDocuments.mockResolvedValue([sampleDocument])
    mockApiService.getDownloadUrl.mockResolvedValue('https://fake.blob/brief.pdf?sas=read')

    renderPage()
    await screen.findByText('brief.pdf')

    // Download button has title="Download" (icon-only button)
    const downloadBtn = screen.getByTitle('Download')
    await userEvent.click(downloadBtn)

    await waitFor(() => {
      expect(mockApiService.getDownloadUrl).toHaveBeenCalledWith(sampleDocument.id)
    })
  })

  it('shows error toast when download fails', async () => {
    mockApiService.getProjectDocuments.mockResolvedValue([sampleDocument])
    mockApiService.getDownloadUrl.mockRejectedValue(new Error('Not found'))

    renderPage()
    await screen.findByText('brief.pdf')

    const downloadBtn = screen.getByTitle('Download')
    await userEvent.click(downloadBtn)

    await waitFor(() => {
      expect(mockShowToast).toHaveBeenCalledWith('Failed to get download link', 'error')
    })
  })

  // ── Delete document ───────────────────────────────────────────────────────

  it('calls deleteDocument when delete button clicked (owner)', async () => {
    mockApiService.getProjectDocuments.mockResolvedValue([sampleDocument])
    mockApiService.deleteDocument.mockResolvedValue(undefined)

    renderPage('CompanyOwner')
    await screen.findByText('brief.pdf')

    // Delete button has title="Delete" (icon-only button)
    const deleteBtn = screen.getByTitle('Delete')
    await userEvent.click(deleteBtn)

    await waitFor(() => {
      expect(mockApiService.deleteDocument).toHaveBeenCalledWith(sampleDocument.id)
    })
  })
})
