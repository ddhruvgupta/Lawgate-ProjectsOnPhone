import React, { useState } from 'react';
import { useParams, Link, useNavigate } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Dialog, DialogPanel, DialogTitle } from '@headlessui/react';
import { apiService } from '../services/api';
import { useToast } from '../contexts/ToastContext';
import { usePermissions } from '../hooks/usePermissions';
import { ProjectStatusBadge } from '../components/ProjectStatusBadge';
import { formatDate, formatBytes } from '../utils/formatters';
import type { UpdateProjectRequest, UploadDocumentRequest } from '../types';
import {
  ArrowLeftIcon,
  PencilIcon,
  TrashIcon,
  DocumentIcon,
  XMarkIcon,
  ArrowDownTrayIcon,
  ArrowUpTrayIcon,
  CalendarIcon,
  UserIcon,
  HashtagIcon,
} from '@heroicons/react/24/outline';

const schema = z.object({
  name: z.string().min(1, 'Name is required').max(200),
  description: z.string().max(2000).default(''),
  clientName: z.string().max(100).optional(),
  caseNumber: z.string().max(50).optional(),
  status: z.enum(['Intake', 'Active', 'Discovery', 'Negotiation', 'Hearing', 'OnHold', 'Settled', 'Closed', 'Archived']),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
});

type FormValues = z.infer<typeof schema>;

const uploadSchema = z.object({
  documentType: z.coerce.number().int(),
  description: z.string().max(500).optional().default(''),
});

type UploadFormValues = z.infer<typeof uploadSchema>;

const DOCUMENT_TYPES = [
  { value: 1, label: 'Contract' },
  { value: 2, label: 'Brief' },
  { value: 3, label: 'Motion' },
  { value: 4, label: 'Pleading' },
  { value: 5, label: 'Agreement' },
  { value: 6, label: 'Evidence' },
  { value: 7, label: 'Correspondence' },
  { value: 8, label: 'Research' },
  { value: 99, label: 'Other' },
];

const MAX_FILE_SIZE_BYTES = 50 * 1024 * 1024; // 50 MB

export const ProjectDetailPage: React.FC = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const { showToast } = useToast();
  const { canEditProject, canDeleteProject, canUploadDocument } = usePermissions();
  const queryClient = useQueryClient();
  const [editOpen, setEditOpen] = useState(false);
  const [deleteOpen, setDeleteOpen] = useState(false);
  const [uploadOpen, setUploadOpen] = useState(false);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const [fileError, setFileError] = useState<string | null>(null);
  const [isUploading, setIsUploading] = useState(false);
  const projectId = Number(id);

  const { data: project, isLoading: projectLoading } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => apiService.getProject(projectId),
    enabled: !!projectId,
  });

  const { data: documents = [], isLoading: docsLoading } = useQuery({
    queryKey: ['documents', projectId],
    queryFn: () => apiService.getProjectDocuments(projectId),
    enabled: !!projectId,
  });

  const updateMutation = useMutation({
    mutationFn: (data: UpdateProjectRequest) => apiService.updateProject(projectId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['project', projectId] });
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      showToast('Project updated', 'success');
      setEditOpen(false);
    },
    onError: () => showToast('Failed to update project', 'error'),
  });

  const deleteMutation = useMutation({
    mutationFn: () => apiService.deleteProject(projectId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      showToast('Project deleted', 'success');
      navigate('/projects');
    },
    onError: () => showToast('Failed to delete project', 'error'),
  });

  const deleteDocMutation = useMutation({
    mutationFn: (docId: number) => apiService.deleteDocument(docId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['documents', projectId] });
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      showToast('Document deleted', 'success');
    },
    onError: () => showToast('Failed to delete document', 'error'),
  });

  const {
    register: registerUpload,
    handleSubmit: handleUploadSubmit,
    reset: resetUpload,
    formState: { errors: uploadErrors },
  } = useForm<UploadFormValues>({ resolver: zodResolver(uploadSchema) as Resolver<UploadFormValues> });

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0] ?? null;
    setFileError(null);
    if (file && file.size > MAX_FILE_SIZE_BYTES) {
      setFileError('File must be 50 MB or smaller.');
      setSelectedFile(null);
      return;
    }
    setSelectedFile(file);
  };

  const openUploadModal = () => {
    setSelectedFile(null);
    setFileError(null);
    resetUpload();
    setUploadOpen(true);
  };

  const handleUpload = async (values: UploadFormValues) => {
    if (!selectedFile) {
      setFileError('Please select a file.');
      return;
    }

    setIsUploading(true);
    try {
      // Step 1: Get SAS upload URL from backend
      const uploadRequest: UploadDocumentRequest = {
        projectId,
        fileName: selectedFile.name,
        fileSizeBytes: selectedFile.size,
        documentType: values.documentType,
        description: values.description || undefined,
        contentType: selectedFile.type || 'application/octet-stream',
      };
      const { documentId, uploadUrl } = await apiService.generateUploadUrl(uploadRequest);

      // Step 2: PUT the file directly to Azure Blob Storage via SAS URL (no auth header)
      const putResponse = await fetch(uploadUrl, {
        method: 'PUT',
        headers: {
          'x-ms-blob-type': 'BlockBlob',
          'Content-Type': selectedFile.type || 'application/octet-stream',
        },
        body: selectedFile,
      });

      if (!putResponse.ok) {
        throw new Error(`Upload to storage failed: ${putResponse.status}`);
      }

      // Step 3: Confirm upload with backend
      await apiService.confirmUpload(documentId);

      queryClient.invalidateQueries({ queryKey: ['documents', projectId] });
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      showToast('Document uploaded successfully', 'success');
      setUploadOpen(false);
    } catch {
      showToast('Upload failed. Please try again.', 'error');
    } finally {
      setIsUploading(false);
    }
  };

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors },
  } = useForm<FormValues>({ resolver: zodResolver(schema) as Resolver<FormValues> });

  const openEdit = () => {
    if (project) {
      reset({
        name: project.name,
        description: project.description,
        clientName: project.clientName ?? '',
        caseNumber: project.caseNumber ?? '',
        status: project.status as FormValues['status'],
        startDate: project.startDate?.slice(0, 10) ?? '',
        endDate: project.endDate?.slice(0, 10) ?? '',
      });
    }
    setEditOpen(true);
  };

  const handleDownload = async (docId: number, fileName: string) => {
    try {
      const url = await apiService.getDownloadUrl(docId);
      const a = document.createElement('a');
      a.href = url;
      a.download = fileName;
      a.click();
    } catch {
      showToast('Failed to get download link', 'error');
    }
  };

  if (projectLoading) {
    return (
      <div className="p-8 max-w-5xl mx-auto space-y-4">
        <div className="h-8 w-48 bg-gray-100 rounded animate-pulse" />
        <div className="h-40 bg-gray-100 rounded-xl animate-pulse" />
        <div className="h-64 bg-gray-100 rounded-xl animate-pulse" />
      </div>
    );
  }

  if (!project) {
    return (
      <div className="p-8 text-center">
        <p className="text-gray-500">Project not found.</p>
        <Link to="/projects" className="text-blue-600 hover:text-blue-700 text-sm mt-2 inline-block">
          ← Back to Projects
        </Link>
      </div>
    );
  }

  return (
    <div className="p-8 max-w-5xl mx-auto">
      {/* Breadcrumb */}
      <div className="flex items-center gap-2 text-sm text-gray-500 mb-6">
        <Link to="/projects" className="hover:text-gray-700 flex items-center gap-1">
          <ArrowLeftIcon className="w-3.5 h-3.5" /> Projects
        </Link>
        <span>/</span>
        <span className="text-gray-900 font-medium">{project.name}</span>
      </div>

      {/* Project header */}
      <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
        <div className="flex items-start justify-between gap-4">
          <div className="flex-1 min-w-0">
            <div className="flex items-center gap-3 mb-2">
              <h1 className="text-xl font-bold text-gray-900">{project.name}</h1>
              <ProjectStatusBadge status={project.status} />
            </div>
            {project.description && (
              <p className="text-sm text-gray-600 mb-4">{project.description}</p>
            )}
            <div className="grid grid-cols-2 sm:grid-cols-4 gap-4 text-sm">
              {project.clientName && (
                <div className="flex items-center gap-2 text-gray-600">
                  <UserIcon className="w-4 h-4 text-gray-400" />
                  <div>
                    <p className="text-xs text-gray-400">Client</p>
                    <p className="font-medium text-gray-900">{project.clientName}</p>
                  </div>
                </div>
              )}
              {project.caseNumber && (
                <div className="flex items-center gap-2 text-gray-600">
                  <HashtagIcon className="w-4 h-4 text-gray-400" />
                  <div>
                    <p className="text-xs text-gray-400">Case No.</p>
                    <p className="font-medium text-gray-900">{project.caseNumber}</p>
                  </div>
                </div>
              )}
              {project.startDate && (
                <div className="flex items-center gap-2 text-gray-600">
                  <CalendarIcon className="w-4 h-4 text-gray-400" />
                  <div>
                    <p className="text-xs text-gray-400">Start Date</p>
                    <p className="font-medium text-gray-900">{formatDate(project.startDate)}</p>
                  </div>
                </div>
              )}
              {project.endDate && (
                <div className="flex items-center gap-2 text-gray-600">
                  <CalendarIcon className="w-4 h-4 text-gray-400" />
                  <div>
                    <p className="text-xs text-gray-400">End Date</p>
                    <p className="font-medium text-gray-900">{formatDate(project.endDate)}</p>
                  </div>
                </div>
              )}
            </div>
          </div>
          <div className="flex items-center gap-2 flex-shrink-0">
            {canEditProject && (
              <button
                onClick={openEdit}
                className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50"
              >
                <PencilIcon className="w-3.5 h-3.5" /> Edit
              </button>
            )}
            {canDeleteProject && (
              <button
                onClick={() => setDeleteOpen(true)}
                className="flex items-center gap-1.5 px-3 py-1.5 text-sm font-medium text-red-600 bg-white border border-red-200 rounded-lg hover:bg-red-50"
              >
                <TrashIcon className="w-3.5 h-3.5" /> Delete
              </button>
            )}
          </div>
        </div>
      </div>

      {/* Documents section */}
      <div className="bg-white rounded-xl border border-gray-200">
        <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
          <div>
            <h2 className="font-semibold text-gray-900">Documents</h2>
            <p className="text-xs text-gray-500 mt-0.5">{documents.length} file{documents.length !== 1 ? 's' : ''}</p>
          </div>
          {/* Upload button */}
          {canUploadDocument && (
            <button
              onClick={openUploadModal}
              className="flex items-center gap-2 px-3 py-1.5 text-sm font-medium text-white bg-blue-600 border border-blue-600 rounded-lg hover:bg-blue-700 transition-colors"
            >
              <ArrowUpTrayIcon className="w-4 h-4" />
              Upload Document
            </button>
          )}
        </div>

        {docsLoading ? (
          <div className="p-6 space-y-3">
            {[1, 2, 3].map((i) => (
              <div key={i} className="h-12 bg-gray-100 rounded animate-pulse" />
            ))}
          </div>
        ) : documents.length === 0 ? (
          <div className="py-16 text-center">
            <DocumentIcon className="w-10 h-10 text-gray-300 mx-auto mb-3" />
            <p className="text-sm font-medium text-gray-500">No documents yet</p>
            <p className="text-xs text-gray-400 mt-1">Upload a document to get started</p>
          </div>
        ) : (
          <ul className="divide-y divide-gray-100">
            {documents.map((doc) => (
              <li key={doc.id} className="flex items-center gap-4 px-6 py-4 hover:bg-gray-50 transition-colors">
                <div className="w-9 h-9 rounded-lg bg-gray-100 flex items-center justify-center flex-shrink-0">
                  <DocumentIcon className="w-5 h-5 text-gray-500" />
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-medium text-gray-900 truncate">{doc.fileName}</p>
                  <p className="text-xs text-gray-500 mt-0.5">
                    {formatBytes(doc.fileSizeBytes)} &middot; v{doc.version} &middot; {doc.documentType} &middot; {doc.uploadedBy}
                  </p>
                </div>
                <div className="flex items-center gap-1 flex-shrink-0">
                  <span className="text-xs text-gray-400 mr-2">{formatDate(doc.createdAt)}</span>
                  <button
                    onClick={() => handleDownload(doc.id, doc.fileName)}
                    className="p-1.5 text-gray-400 hover:text-blue-600 hover:bg-blue-50 rounded-lg transition-colors"
                    title="Download"
                  >
                    <ArrowDownTrayIcon className="w-4 h-4" />
                  </button>
                  <button
                    onClick={() => deleteDocMutation.mutate(doc.id)}
                    className="p-1.5 text-gray-400 hover:text-red-600 hover:bg-red-50 rounded-lg transition-colors"
                    title="Delete"
                  >
                    <TrashIcon className="w-4 h-4" />
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      {/* Edit Modal */}
      <Dialog open={editOpen} onClose={() => setEditOpen(false)} className="relative z-50">
        <div className="fixed inset-0 bg-black/30" aria-hidden="true" />
        <div className="fixed inset-0 flex items-center justify-center p-4">
          <DialogPanel className="bg-white rounded-xl shadow-xl w-full max-w-lg">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
              <DialogTitle className="text-base font-semibold text-gray-900">Edit Project</DialogTitle>
              <button onClick={() => setEditOpen(false)} className="text-gray-400 hover:text-gray-600">
                <XMarkIcon className="w-5 h-5" />
              </button>
            </div>
            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Project Name <span className="text-red-500">*</span></label>
                <input {...register('name')} className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500" />
                {errors.name && <p className="mt-1 text-xs text-red-500">{errors.name.message}</p>}
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Client Name</label>
                  <input {...register('clientName')} className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Case Number</label>
                  <input {...register('caseNumber')} className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Status</label>
                <select {...register('status')} className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500">
                  <option value="Intake">Intake</option>
                  <option value="Active">Active</option>
                  <option value="Discovery">Discovery</option>
                  <option value="Negotiation">Negotiation</option>
                  <option value="Hearing">Hearing / Tribunal</option>
                  <option value="OnHold">On Hold</option>
                  <option value="Settled">Settled</option>
                  <option value="Closed">Closed</option>
                  <option value="Archived">Archived</option>
                </select>
              </div>
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">Start Date</label>
                  <input type="date" {...register('startDate')} className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">End Date</label>
                  <input type="date" {...register('endDate')} className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500" />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">Description</label>
                <textarea {...register('description')} rows={3} className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none" />
              </div>
              <div className="flex justify-end gap-3 pt-2">
                <button type="button" onClick={() => setEditOpen(false)} className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50">
                  Cancel
                </button>
                <button type="button" onClick={handleSubmit((v) => updateMutation.mutate({ name: v.name, description: v.description, status: v.status, clientName: v.clientName || undefined, caseNumber: v.caseNumber || undefined, startDate: v.startDate || undefined, endDate: v.endDate || undefined }))} disabled={updateMutation.isPending} className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50">
                  {updateMutation.isPending ? 'Saving…' : 'Save Changes'}
                </button>
              </div>
            </div>
          </DialogPanel>
        </div>
      </Dialog>

      {/* Delete Confirm Modal */}
      <Dialog open={deleteOpen} onClose={() => setDeleteOpen(false)} className="relative z-50">
        <div className="fixed inset-0 bg-black/30" aria-hidden="true" />
        <div className="fixed inset-0 flex items-center justify-center p-4">
          <DialogPanel className="bg-white rounded-xl shadow-xl w-full max-w-sm p-6">
            <DialogTitle className="text-base font-semibold text-gray-900 mb-2">Delete Project</DialogTitle>
            <p className="text-sm text-gray-600 mb-6">
              Are you sure you want to delete <strong>{project.name}</strong>? This will also delete all {project.documentCount} associated document{project.documentCount !== 1 ? 's' : ''}. This cannot be undone.
            </p>
            <div className="flex justify-end gap-3">
              <button onClick={() => setDeleteOpen(false)} className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50">
                Cancel
              </button>
              <button
                onClick={() => deleteMutation.mutate()}
                disabled={deleteMutation.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-lg hover:bg-red-700 disabled:opacity-50"
              >
                {deleteMutation.isPending ? 'Deleting…' : 'Delete Project'}
              </button>
            </div>
          </DialogPanel>
        </div>
      </Dialog>

      {/* Upload Document Modal */}
      <Dialog open={uploadOpen} onClose={() => !isUploading && setUploadOpen(false)} className="relative z-50">
        <div className="fixed inset-0 bg-black/30" aria-hidden="true" />
        <div className="fixed inset-0 flex items-center justify-center p-4">
          <DialogPanel className="bg-white rounded-xl shadow-xl w-full max-w-md">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100">
              <DialogTitle className="text-base font-semibold text-gray-900">Upload Document</DialogTitle>
              <button
                onClick={() => setUploadOpen(false)}
                disabled={isUploading}
                className="text-gray-400 hover:text-gray-600 disabled:opacity-40"
                aria-label="Close"
              >
                <XMarkIcon className="w-5 h-5" />
              </button>
            </div>
            <form onSubmit={handleUploadSubmit(handleUpload)} noValidate>
              <div className="p-6 space-y-4">
                {/* File input */}
                <div>
                  <label htmlFor="upload-file" className="block text-sm font-medium text-gray-700 mb-1">
                    File <span className="text-red-500" aria-hidden="true">*</span>
                  </label>
                  <input
                    id="upload-file"
                    type="file"
                    onChange={handleFileChange}
                    disabled={isUploading}
                    className="block w-full text-sm text-gray-500 file:mr-3 file:py-1.5 file:px-3 file:rounded-lg file:border file:border-gray-300 file:text-sm file:font-medium file:text-gray-700 file:bg-white hover:file:bg-gray-50 cursor-pointer"
                    aria-describedby={fileError ? 'file-error' : undefined}
                  />
                  {fileError && (
                    <p id="file-error" role="alert" className="mt-1 text-xs text-red-500">{fileError}</p>
                  )}
                  {selectedFile && (
                    <p className="mt-1 text-xs text-gray-500">
                      {selectedFile.name} &mdash; {formatBytes(selectedFile.size)}
                    </p>
                  )}
                  <p className="mt-1 text-xs text-gray-400">Maximum file size: 50 MB</p>
                </div>

                {/* Document type */}
                <div>
                  <label htmlFor="upload-doc-type" className="block text-sm font-medium text-gray-700 mb-1">
                    Document Type <span className="text-red-500" aria-hidden="true">*</span>
                  </label>
                  <select
                    id="upload-doc-type"
                    {...registerUpload('documentType')}
                    disabled={isUploading}
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 disabled:opacity-50"
                  >
                    {DOCUMENT_TYPES.map((t) => (
                      <option key={t.value} value={t.value}>{t.label}</option>
                    ))}
                  </select>
                  {uploadErrors.documentType && (
                    <p role="alert" className="mt-1 text-xs text-red-500">{uploadErrors.documentType.message}</p>
                  )}
                </div>

                {/* Description */}
                <div>
                  <label htmlFor="upload-description" className="block text-sm font-medium text-gray-700 mb-1">
                    Description
                  </label>
                  <textarea
                    id="upload-description"
                    {...registerUpload('description')}
                    rows={2}
                    disabled={isUploading}
                    placeholder="Optional description…"
                    className="w-full px-3 py-2 text-sm border border-gray-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none disabled:opacity-50"
                  />
                  {uploadErrors.description && (
                    <p role="alert" className="mt-1 text-xs text-red-500">{uploadErrors.description.message}</p>
                  )}
                </div>
              </div>

              <div className="flex justify-end gap-3 px-6 py-4 border-t border-gray-100">
                <button
                  type="button"
                  onClick={() => setUploadOpen(false)}
                  disabled={isUploading}
                  className="px-4 py-2 text-sm font-medium text-gray-700 bg-white border border-gray-300 rounded-lg hover:bg-gray-50 disabled:opacity-50"
                >
                  Cancel
                </button>
                <button
                  type="submit"
                  disabled={isUploading}
                  className="flex items-center gap-2 px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50"
                >
                  {isUploading ? (
                    <>
                      <span className="w-4 h-4 border-2 border-white/30 border-t-white rounded-full animate-spin" aria-hidden="true" />
                      Uploading…
                    </>
                  ) : (
                    <>
                      <ArrowUpTrayIcon className="w-4 h-4" aria-hidden="true" />
                      Upload
                    </>
                  )}
                </button>
              </div>
            </form>
          </DialogPanel>
        </div>
      </Dialog>
    </div>
  );
};
