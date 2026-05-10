import React, { useState, useEffect, useRef } from 'react';
import { Link, useSearchParams } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useForm, type Resolver } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Dialog, DialogPanel, DialogTitle } from '@headlessui/react';
import { apiService } from '../services/api';
import { useToast } from '../contexts/ToastContext';
import { ProjectStatusBadge } from '../components/ProjectStatusBadge';
import { formatDate } from '../utils/formatters';
import type { CreateProjectRequest } from '../types';
import {
  PlusIcon,
  FolderIcon,
  DocumentIcon,
  XMarkIcon,
  MagnifyingGlassIcon,
  Squares2X2Icon,
  ListBulletIcon,
  TrashIcon,
  ChevronUpIcon,
  ChevronDownIcon,
} from '@heroicons/react/24/outline';

const schema = z.object({
  name: z.string().min(1, 'Project name is required').max(200),
  description: z.string().max(2000).default(''),
  clientName: z.string().max(100).optional(),
  caseNumber: z.string().max(50).optional(),
  status: z.enum(['Intake', 'Active', 'Discovery', 'Negotiation', 'Hearing', 'OnHold', 'Settled', 'Closed', 'Archived']).default('Intake'),
  startDate: z.string().optional(),
  endDate: z.string().optional(),
}).refine(
  (data) => {
    if (data.startDate && data.endDate) {
      return new Date(data.endDate) >= new Date(data.startDate);
    }
    return true;
  },
  { message: 'End date must be on or after start date', path: ['endDate'] }
);

type FormValues = z.infer<typeof schema>;

export const ProjectsPage: React.FC = () => {
  const [searchParams, setSearchParams] = useSearchParams();
  // BUG-010: initialise from URL so the modal is open on first render when ?new=1
  const [modalOpen, setModalOpen] = useState(() => searchParams.get('new') === '1');
  const [search, setSearch] = useState('');
  const [viewMode, setViewMode] = useState<'card' | 'list'>(
    () => (localStorage.getItem('lawgate-projects-view') as 'card' | 'list') ?? 'card'
  );
  const [sortBy, setSortBy] = useState<'name' | 'status' | 'createdAt'>(
    () => (localStorage.getItem('lawgate-projects-sort') as 'name' | 'status' | 'createdAt') ?? 'createdAt'
  );
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>(
    () => (localStorage.getItem('lawgate-projects-sort-dir') as 'asc' | 'desc') ?? 'desc'
  );
  const [deleteTarget, setDeleteTarget] = useState<{ id: number; name: string } | null>(null);
  const { showToast } = useToast();
  const queryClient = useQueryClient();
  const clearedRef = useRef(false);

  // Strip ?new=1 from the URL without triggering a re-render loop.
  // Only setSearchParams is called here (not setState), which is safe in an effect.
  useEffect(() => {
    if (!clearedRef.current && searchParams.get('new') === '1') {
      clearedRef.current = true;
      setSearchParams({}, { replace: true });
    }
  }, [searchParams, setSearchParams]);

  const { data: projects = [], isLoading } = useQuery({
    queryKey: ['projects'],
    queryFn: () => apiService.getProjects(),
  });

  const createMutation = useMutation({
    mutationFn: (data: CreateProjectRequest) => apiService.createProject(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      showToast('Project created successfully', 'success');
      setModalOpen(false);
      reset();
    },
    onError: () => showToast('Failed to create project', 'error'),
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => apiService.deleteProject(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      showToast('Project deleted', 'success');
      setDeleteTarget(null);
    },
    onError: () => showToast('Failed to delete project', 'error'),
  });

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema) as Resolver<FormValues> });

  const onSubmit = (values: FormValues) => {
    const payload: CreateProjectRequest = {
      ...values,
      clientName: values.clientName || undefined,
      caseNumber: values.caseNumber || undefined,
      startDate: values.startDate || undefined,
      endDate: values.endDate || undefined,
    };
    createMutation.mutate(payload);
  };

  const filtered = projects.filter(
    (p) =>
      p.name.toLowerCase().includes(search.toLowerCase()) ||
      (p.clientName ?? '').toLowerCase().includes(search.toLowerCase()) ||
      (p.caseNumber ?? '').toLowerCase().includes(search.toLowerCase())
  );

  const sorted = [...filtered].sort((a, b) => {
    let cmp = 0;
    if (sortBy === 'name') cmp = a.name.localeCompare(b.name);
    else if (sortBy === 'status') cmp = a.status.localeCompare(b.status);
    else cmp = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
    return sortDir === 'asc' ? cmp : -cmp;
  });

  const handleViewMode = (mode: 'card' | 'list') => {
    setViewMode(mode);
    localStorage.setItem('lawgate-projects-view', mode);
  };

  const handleSort = (field: typeof sortBy) => {
    if (field === sortBy) {
      const newDir = sortDir === 'asc' ? 'desc' : 'asc';
      setSortDir(newDir);
      localStorage.setItem('lawgate-projects-sort-dir', newDir);
    } else {
      setSortBy(field);
      setSortDir('asc');
      localStorage.setItem('lawgate-projects-sort', field);
      localStorage.setItem('lawgate-projects-sort-dir', 'asc');
    }
  };

  return (
    <div className="p-8 max-w-7xl mx-auto">
      {/* Header */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Projects</h1>
          <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            {isLoading
              ? 'Loading projects…'
              : `${projects.length} project${projects.length !== 1 ? 's' : ''} in your firm`}
          </p>
        </div>
        <button
          onClick={() => setModalOpen(true)}
          className="flex items-center gap-2 px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700 transition-colors"
        >
          <PlusIcon className="w-4 h-4" />
          New Project
        </button>
      </div>

      {/* Search */}
      <div className="relative mb-6">
        <MagnifyingGlassIcon className="absolute left-3 top-1/2 -translate-y-1/2 w-4 h-4 text-gray-400" />
        <input
          type="text"
          placeholder="Search by name, client, or case number…"
          value={search}
          onChange={(e) => setSearch(e.target.value)}
          className="w-full pl-9 pr-4 py-2.5 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-800 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
        />
      </div>

      {/* Controls row: sort + view toggle */}
      <div className="flex items-center justify-between mb-4 gap-3 flex-wrap">
        <div className="flex items-center gap-2">
          <span className="text-xs text-gray-500 dark:text-gray-400 font-medium">Sort by</span>
          {(['name', 'status', 'createdAt'] as const).map((field) => {
            const labels: Record<string, string> = { name: 'Name', status: 'Status', createdAt: 'Date created' };
            const active = sortBy === field;
            return (
              <button
                key={field}
                onClick={() => handleSort(field)}
                className={`flex items-center gap-1 px-2.5 py-1 rounded-md text-xs font-medium transition-colors ${
                  active
                    ? 'bg-blue-100 dark:bg-blue-900/40 text-blue-700 dark:text-blue-300'
                    : 'text-gray-600 dark:text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-700'
                }`}
                aria-pressed={active}
              >
                {labels[field]}
                {active && (sortDir === 'asc' ? <ChevronUpIcon className="w-3 h-3" /> : <ChevronDownIcon className="w-3 h-3" />)}
              </button>
            );
          })}
        </div>
        <div className="flex items-center gap-1 bg-gray-100 dark:bg-gray-700 rounded-lg p-1">
          <button
            onClick={() => handleViewMode('card')}
            aria-label="Card view"
            aria-pressed={viewMode === 'card'}
            className={`p-1.5 rounded-md transition-colors ${
              viewMode === 'card'
                ? 'bg-white dark:bg-gray-600 text-blue-600 dark:text-blue-400 shadow-sm'
                : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'
            }`}
          >
            <Squares2X2Icon className="w-4 h-4" />
          </button>
          <button
            onClick={() => handleViewMode('list')}
            aria-label="List view"
            aria-pressed={viewMode === 'list'}
            className={`p-1.5 rounded-md transition-colors ${
              viewMode === 'list'
                ? 'bg-white dark:bg-gray-600 text-blue-600 dark:text-blue-400 shadow-sm'
                : 'text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200'
            }`}
          >
            <ListBulletIcon className="w-4 h-4" />
          </button>
        </div>
      </div>

      {/* Content */}
      {isLoading ? (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {[1, 2, 3, 4, 5, 6].map((i) => (
            <div key={i} className="h-48 bg-gray-100 dark:bg-gray-700 rounded-xl animate-pulse" />
          ))}
        </div>
      ) : sorted.length === 0 ? (
        <div className="text-center py-20 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 border-dashed">
          <FolderIcon className="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
          <h3 className="text-sm font-semibold text-gray-900 dark:text-white mb-1">
            {search ? 'No projects match your search' : 'No projects yet'}
          </h3>
          <p className="text-sm text-gray-500 dark:text-gray-400 mb-4">
            {search ? 'Try a different search term.' : 'Create your first project to get started.'}
          </p>
          {!search && (
            <button
              onClick={() => setModalOpen(true)}
              className="inline-flex items-center gap-2 px-4 py-2 bg-blue-600 text-white text-sm font-medium rounded-lg hover:bg-blue-700"
            >
              <PlusIcon className="w-4 h-4" />
              New Project
            </button>
          )}
        </div>
      ) : viewMode === 'card' ? (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {sorted.map((project) => (
            <div key={project.id} className="relative group">
              <Link
                to={`/projects/${project.id}`}
                className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5 hover:border-blue-300 dark:hover:border-blue-600 hover:shadow-sm transition-all group flex flex-col gap-3 block"
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="w-9 h-9 rounded-lg bg-blue-50 dark:bg-blue-900/30 flex items-center justify-center flex-shrink-0">
                      <FolderIcon className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                    </div>
                    <h3 className="font-semibold text-gray-900 dark:text-white truncate group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                      {project.name}
                    </h3>
                  </div>
                  <ProjectStatusBadge status={project.status} />
                </div>

                {project.description && (
                  <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-2">{project.description}</p>
                )}

                <div className="text-xs text-gray-500 dark:text-gray-400 space-y-1 mt-auto pt-2 border-t border-gray-100 dark:border-gray-700">
                  {project.clientName && (
                    <div className="flex justify-between">
                      <span>Client</span>
                      <span className="font-medium text-gray-700 dark:text-gray-300">{project.clientName}</span>
                    </div>
                  )}
                  {project.caseNumber && (
                    <div className="flex justify-between">
                      <span>Case #</span>
                      <span className="font-medium text-gray-700 dark:text-gray-300">{project.caseNumber}</span>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <span className="flex items-center gap-1">
                      <DocumentIcon className="w-3.5 h-3.5" /> Documents
                    </span>
                    <span className="font-medium text-gray-700 dark:text-gray-300">{project.documentCount}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Created</span>
                    <span className="font-medium text-gray-700 dark:text-gray-300">{formatDate(project.createdAt)}</span>
                  </div>
                </div>
              </Link>
              <button
                onClick={(e) => { e.preventDefault(); setDeleteTarget({ id: project.id, name: project.name }); }}
                aria-label={`Delete ${project.name}`}
                title="Delete project"
                className="absolute top-3 right-3 p-1.5 rounded-md text-gray-400 opacity-0 group-hover:opacity-100 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 transition-all"
              >
                <TrashIcon className="w-4 h-4" />
              </button>
            </div>
          ))}
        </div>
      ) : (
        /* List view */
        <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 dark:border-gray-700 text-left text-xs text-gray-500 dark:text-gray-400">
                <th className="px-4 py-3 font-medium">Project</th>
                <th className="px-4 py-3 font-medium hidden sm:table-cell">Client</th>
                <th className="px-4 py-3 font-medium hidden md:table-cell">Case #</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium hidden lg:table-cell">Docs</th>
                <th className="px-4 py-3 font-medium hidden xl:table-cell">Created</th>
                <th className="px-4 py-3 font-medium w-10"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50 dark:divide-gray-700/50">
              {sorted.map((project) => (
                <tr key={project.id} className="hover:bg-gray-50 dark:hover:bg-gray-700/30 group transition-colors">
                  <td className="px-4 py-3">
                    <Link to={`/projects/${project.id}`} className="flex items-center gap-3 min-w-0 hover:text-blue-600 dark:hover:text-blue-400 transition-colors">
                      <div className="w-7 h-7 rounded-md bg-blue-50 dark:bg-blue-900/30 flex items-center justify-center flex-shrink-0">
                        <FolderIcon className="w-4 h-4 text-blue-600 dark:text-blue-400" />
                      </div>
                      <span className="font-medium text-gray-900 dark:text-white truncate">{project.name}</span>
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-gray-500 dark:text-gray-400 hidden sm:table-cell">{project.clientName ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-500 dark:text-gray-400 hidden md:table-cell">{project.caseNumber ?? '—'}</td>
                  <td className="px-4 py-3"><ProjectStatusBadge status={project.status} /></td>
                  <td className="px-4 py-3 text-gray-500 dark:text-gray-400 hidden lg:table-cell">{project.documentCount}</td>
                  <td className="px-4 py-3 text-gray-500 dark:text-gray-400 hidden xl:table-cell">{formatDate(project.createdAt)}</td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => setDeleteTarget({ id: project.id, name: project.name })}
                      aria-label={`Delete ${project.name}`}
                      title="Delete project"
                      className="p-1.5 rounded-md text-gray-400 opacity-0 group-hover:opacity-100 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 transition-all"
                    >
                      <TrashIcon className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Delete project confirmation */}
      <Dialog open={deleteTarget !== null} onClose={() => setDeleteTarget(null)} className="relative z-50">
        <div className="fixed inset-0 bg-black/30" aria-hidden="true" />
        <div className="fixed inset-0 flex items-center justify-center p-4">
          <DialogPanel className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-md p-6">
            <DialogTitle className="text-base font-semibold text-gray-900 dark:text-white mb-2">
              Delete Project
            </DialogTitle>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">
              Are you sure you want to delete <strong className="text-gray-900 dark:text-white">{deleteTarget?.name}</strong>?
              This will also delete all associated documents. This cannot be undone.
            </p>
            <div className="flex justify-end gap-3">
              <button
                onClick={() => setDeleteTarget(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-600"
              >
                Cancel
              </button>
              <button
                onClick={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
                disabled={deleteMutation.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-lg hover:bg-red-700 disabled:opacity-50"
              >
                {deleteMutation.isPending ? 'Deleting…' : 'Delete Project'}
              </button>
            </div>
          </DialogPanel>
        </div>
      </Dialog></div>
      ) : viewMode === 'card' ? (
        <div className="grid grid-cols-1 md:grid-cols-2 xl:grid-cols-3 gap-4">
          {sorted.map((project) => (
            <div key={project.id} className="relative group">
              <Link
                to={`/projects/${project.id}`}
                className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5 hover:border-blue-300 dark:hover:border-blue-600 hover:shadow-sm transition-all group flex flex-col gap-3 block"
              >
                <div className="flex items-start justify-between gap-2">
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="w-9 h-9 rounded-lg bg-blue-50 dark:bg-blue-900/30 flex items-center justify-center flex-shrink-0">
                      <FolderIcon className="w-5 h-5 text-blue-600 dark:text-blue-400" />
                    </div>
                    <h3 className="font-semibold text-gray-900 dark:text-white truncate group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                      {project.name}
                    </h3>
                  </div>
                  <ProjectStatusBadge status={project.status} />
                </div>

                {project.description && (
                  <p className="text-sm text-gray-500 dark:text-gray-400 line-clamp-2">{project.description}</p>
                )}

                <div className="text-xs text-gray-500 dark:text-gray-400 space-y-1 mt-auto pt-2 border-t border-gray-100 dark:border-gray-700">
                  {project.clientName && (
                    <div className="flex justify-between">
                      <span>Client</span>
                      <span className="font-medium text-gray-700 dark:text-gray-300">{project.clientName}</span>
                    </div>
                  )}
                  {project.caseNumber && (
                    <div className="flex justify-between">
                      <span>Case #</span>
                      <span className="font-medium text-gray-700 dark:text-gray-300">{project.caseNumber}</span>
                    </div>
                  )}
                  <div className="flex justify-between">
                    <span className="flex items-center gap-1">
                      <DocumentIcon className="w-3.5 h-3.5" /> Documents
                    </span>
                    <span className="font-medium text-gray-700 dark:text-gray-300">{project.documentCount}</span>
                  </div>
                  <div className="flex justify-between">
                    <span>Created</span>
                    <span className="font-medium text-gray-700 dark:text-gray-300">{formatDate(project.createdAt)}</span>
                  </div>
                </div>
              </Link>
              <button
                onClick={(e) => { e.preventDefault(); setDeleteTarget({ id: project.id, name: project.name }); }}
                aria-label={`Delete ${project.name}`}
                title="Delete project"
                className="absolute top-3 right-3 p-1.5 rounded-md text-gray-400 opacity-0 group-hover:opacity-100 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 transition-all"
              >
                <TrashIcon className="w-4 h-4" />
              </button>
            </div>
          ))}
        </div>
      ) : (
        /* List view */
        <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 dark:border-gray-700 text-left text-xs text-gray-500 dark:text-gray-400">
                <th className="px-4 py-3 font-medium">Project</th>
                <th className="px-4 py-3 font-medium hidden sm:table-cell">Client</th>
                <th className="px-4 py-3 font-medium hidden md:table-cell">Case #</th>
                <th className="px-4 py-3 font-medium">Status</th>
                <th className="px-4 py-3 font-medium hidden lg:table-cell">Docs</th>
                <th className="px-4 py-3 font-medium hidden xl:table-cell">Created</th>
                <th className="px-4 py-3 font-medium w-10"></th>
              </tr>
            </thead>
            <tbody className="divide-y divide-gray-50 dark:divide-gray-700/50">
              {sorted.map((project) => (
                <tr key={project.id} className="hover:bg-gray-50 dark:hover:bg-gray-700/30 group transition-colors">
                  <td className="px-4 py-3">
                    <Link to={`/projects/${project.id}`} className="flex items-center gap-3 min-w-0 hover:text-blue-600 dark:hover:text-blue-400 transition-colors">
                      <div className="w-7 h-7 rounded-md bg-blue-50 dark:bg-blue-900/30 flex items-center justify-center flex-shrink-0">
                        <FolderIcon className="w-4 h-4 text-blue-600 dark:text-blue-400" />
                      </div>
                      <span className="font-medium text-gray-900 dark:text-white truncate">{project.name}</span>
                    </Link>
                  </td>
                  <td className="px-4 py-3 text-gray-500 dark:text-gray-400 hidden sm:table-cell">{project.clientName ?? '—'}</td>
                  <td className="px-4 py-3 text-gray-500 dark:text-gray-400 hidden md:table-cell">{project.caseNumber ?? '—'}</td>
                  <td className="px-4 py-3"><ProjectStatusBadge status={project.status} /></td>
                  <td className="px-4 py-3 text-gray-500 dark:text-gray-400 hidden lg:table-cell">{project.documentCount}</td>
                  <td className="px-4 py-3 text-gray-500 dark:text-gray-400 hidden xl:table-cell">{formatDate(project.createdAt)}</td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => setDeleteTarget({ id: project.id, name: project.name })}
                      aria-label={`Delete ${project.name}`}
                      title="Delete project"
                      className="p-1.5 rounded-md text-gray-400 opacity-0 group-hover:opacity-100 hover:text-red-600 hover:bg-red-50 dark:hover:bg-red-900/20 transition-all"
                    >
                      <TrashIcon className="w-4 h-4" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}

      {/* Delete project confirmation */}
      <Dialog open={deleteTarget !== null} onClose={() => setDeleteTarget(null)} className="relative z-50">
        <div className="fixed inset-0 bg-black/30" aria-hidden="true" />
        <div className="fixed inset-0 flex items-center justify-center p-4">
          <DialogPanel className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-md p-6">
            <DialogTitle className="text-base font-semibold text-gray-900 dark:text-white mb-2">
              Delete Project
            </DialogTitle>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-6">
              Are you sure you want to delete <strong className="text-gray-900 dark:text-white">{deleteTarget?.name}</strong>?
              This will also delete all associated documents. This cannot be undone.
            </p>
            <div className="flex justify-end gap-3">
              <button
                onClick={() => setDeleteTarget(null)}
                className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-600"
              >
                Cancel
              </button>
              <button
                onClick={() => deleteTarget && deleteMutation.mutate(deleteTarget.id)}
                disabled={deleteMutation.isPending}
                className="px-4 py-2 text-sm font-medium text-white bg-red-600 rounded-lg hover:bg-red-700 disabled:opacity-50"
              >
                {deleteMutation.isPending ? 'Deleting…' : 'Delete Project'}
              </button>
            </div>
          </DialogPanel>
        </div>
      </Dialog>

      {/* Create Project Modal */}
      <Dialog open={modalOpen} onClose={() => { setModalOpen(false); reset(); }} className="relative z-50">
        <div className="fixed inset-0 bg-black/30" aria-hidden="true" />
        <div className="fixed inset-0 flex items-center justify-center p-4">
          <DialogPanel className="bg-white dark:bg-gray-800 rounded-xl shadow-xl w-full max-w-lg">
            <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-700">
              <DialogTitle className="text-base font-semibold text-gray-900 dark:text-white">
                New Project
              </DialogTitle>
              <button onClick={() => { setModalOpen(false); reset(); }} className="text-gray-400 hover:text-gray-600 dark:hover:text-gray-200">
                <XMarkIcon className="w-5 h-5" />
              </button>
            </div>

            <div className="p-6 space-y-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  Project Name <span className="text-red-500">*</span>
                </label>
                <input
                  {...register('name')}
                  className="w-full px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
                  placeholder="e.g. Smith v. Jones"
                />
                {errors.name && <p className="mt-1 text-xs text-red-500">{errors.name.message}</p>}
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Client Name</label>
                  <input
                    {...register('clientName')}
                    className="w-full px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="Client name"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Case Number</label>
                  <input
                    {...register('caseNumber')}
                    className="w-full px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500"
                    placeholder="e.g. 2024-CV-0001"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Status</label>
                <select
                  {...register('status')}
                  className="w-full px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                >
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
                  <input
                    type="date"
                    {...register('startDate')}
                    className="w-full px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">End Date</label>
                  <input
                    type="date"
                    {...register('endDate')}
                    className="w-full px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-500"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">Description</label>
                <textarea
                  {...register('description')}
                  rows={3}
                  className="w-full px-3 py-2 text-sm border border-gray-300 dark:border-gray-600 rounded-lg bg-white dark:bg-gray-700 text-gray-900 dark:text-white placeholder-gray-500 dark:placeholder-gray-400 focus:outline-none focus:ring-2 focus:ring-blue-500 resize-none"
                  placeholder="Brief description of the matter…"
                />
              </div>

              <div className="flex justify-end gap-3 pt-2">
                <button
                  type="button"
                  onClick={() => { setModalOpen(false); reset(); }}
                  className="px-4 py-2 text-sm font-medium text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-700 border border-gray-300 dark:border-gray-600 rounded-lg hover:bg-gray-50 dark:hover:bg-gray-600"
                >
                  Cancel
                </button>
                <button
                  type="button"
                  onClick={handleSubmit(onSubmit)}
                  disabled={isSubmitting || createMutation.isPending}
                  className="px-4 py-2 text-sm font-medium text-white bg-blue-600 rounded-lg hover:bg-blue-700 disabled:opacity-50"
                >
                  {createMutation.isPending ? 'Creating…' : 'Create Project'}
                </button>
              </div>
            </div>
          </DialogPanel>
        </div>
      </Dialog>
    </div>
  );
};
