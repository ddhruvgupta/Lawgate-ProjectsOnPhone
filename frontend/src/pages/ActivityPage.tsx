import { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { apiService } from '../services/api';
import type { AuditLog } from '../types';

// ─── Helpers ─────────────────────────────────────────────────────────────────

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const minutes = Math.floor(diff / 60_000);
  if (minutes < 1) return 'just now';
  if (minutes < 60) return `${minutes}m ago`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  if (days < 7) return `${days}d ago`;
  return new Date(dateStr).toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' });
}

function formatFullDate(dateStr: string): string {
  return new Date(dateStr).toLocaleString('en-US', {
    month: 'short', day: 'numeric', year: 'numeric',
    hour: 'numeric', minute: '2-digit', hour12: true,
  });
}

type FilterTab = 'All' | 'Project' | 'User' | 'Document';

const FILTER_TABS: { label: string; value: FilterTab; entityType?: string }[] = [
  { label: 'All activity', value: 'All' },
  { label: 'Projects', value: 'Project', entityType: 'Project' },
  { label: 'Team', value: 'User', entityType: 'User' },
  { label: 'Documents', value: 'Document', entityType: 'Document' },
];

// ─── Icon per entity type ─────────────────────────────────────────────────────

function EntityIcon({ entityType, action }: { entityType: string; action: string }) {
  const isDelete = action.includes('Deleted') || action.includes('Deactivated');

  if (entityType === 'Project') {
    return (
        <span className={`inline-flex items-center justify-center w-8 h-8 rounded-full ${isDelete ? 'bg-red-100 dark:bg-red-900/40' : 'bg-blue-100 dark:bg-blue-900/40'}`}>
        <svg className={`w-4 h-4 ${isDelete ? 'text-red-600 dark:text-red-400' : 'text-blue-600 dark:text-blue-400'}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M3 7a2 2 0 012-2h4l2 2h8a2 2 0 012 2v8a2 2 0 01-2 2H5a2 2 0 01-2-2V7z" />
        </svg>
      </span>
    );
  }
  if (entityType === 'User') {
    return (
      <span className={`inline-flex items-center justify-center w-8 h-8 rounded-full ${isDelete ? 'bg-red-100 dark:bg-red-900/40' : 'bg-purple-100 dark:bg-purple-900/40'}`}>
        <svg className={`w-4 h-4 ${isDelete ? 'text-red-600 dark:text-red-400' : 'text-purple-600 dark:text-purple-400'}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M16 7a4 4 0 11-8 0 4 4 0 018 0zM12 14a7 7 0 00-7 7h14a7 7 0 00-7-7z" />
        </svg>
      </span>
    );
  }
  if (entityType === 'Document') {
    return (
      <span className={`inline-flex items-center justify-center w-8 h-8 rounded-full ${isDelete ? 'bg-red-100 dark:bg-red-900/40' : 'bg-orange-100 dark:bg-orange-900/40'}`}>
        <svg className={`w-4 h-4 ${isDelete ? 'text-red-600 dark:text-red-400' : 'text-orange-600 dark:text-orange-400'}`} fill="none" viewBox="0 0 24 24" stroke="currentColor">
          <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M7 21h10a2 2 0 002-2V9.414a1 1 0 00-.293-.707l-5.414-5.414A1 1 0 0012.586 3H7a2 2 0 00-2 2v14a2 2 0 002 2z" />
        </svg>
      </span>
    );
  }
  return (
    <span className="inline-flex items-center justify-center w-8 h-8 rounded-full bg-gray-100 dark:bg-gray-700">
      <svg className="w-4 h-4 text-gray-500 dark:text-gray-400" fill="none" viewBox="0 0 24 24" stroke="currentColor">
        <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={2} d="M13 16h-1v-4h-1m1-4h.01M21 12a9 9 0 11-18 0 9 9 0 0118 0z" />
      </svg>
    </span>
  );
}

function ActionBadge({ action }: { action: string }) {
  const isCreate = action.includes('Created') || action.includes('Uploaded');
  const isDelete = action.includes('Deleted') || action.includes('Deactivated');
  const isUpdate = action.includes('Updated') || action.includes('Activated');

  const label = action.split('.')[1] ?? action;

  if (isCreate) return <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-green-100 dark:bg-green-900/30 text-green-700 dark:text-green-400">{label}</span>;
  if (isDelete) return <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-red-100 dark:bg-red-900/30 text-red-700 dark:text-red-400">{label}</span>;
  if (isUpdate) return <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-blue-100 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400">{label}</span>;
  return <span className="inline-flex items-center px-2 py-0.5 rounded text-xs font-medium bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300">{label}</span>;
}

function UserAvatar({ name }: { name: string }) {
  const parts = name.trim().split(' ');
  const initials = parts.length >= 2 ? parts[0][0] + parts[parts.length - 1][0] : name.slice(0, 2);
  return (
    <span className="inline-flex items-center justify-center w-6 h-6 rounded-full bg-indigo-100 dark:bg-indigo-900/40 text-indigo-700 dark:text-indigo-300 text-xs font-semibold flex-shrink-0">
      {initials.toUpperCase()}
    </span>
  );
}

// ─── Row ──────────────────────────────────────────────────────────────────────

function AuditRow({ log }: { log: AuditLog }) {
  const navigate = useNavigate();
  const [expanded, setExpanded] = useState(false);

  const handleNavigate = () => {
    if (log.entityType === 'Project' && log.entityId && !log.action.includes('Deleted')) {
      navigate(`/projects/${log.entityId}`);
    }
  };

  const canNavigate = log.entityType === 'Project' && log.entityId && !log.action.includes('Deleted');

  return (
    <li className="flex gap-4 py-4">
      {/* Entity icon */}
      <div className="flex-shrink-0 mt-0.5">
        <EntityIcon entityType={log.entityType} action={log.action} />
      </div>

      {/* Content */}
      <div className="flex-1 min-w-0">
        <div className="flex items-start justify-between gap-2">
          <div className="flex-1 min-w-0">
            {/* Action badge + description */}
            <div className="flex items-center gap-2 flex-wrap">
              <ActionBadge action={log.action} />
              <p
                className={`text-sm text-gray-800 dark:text-gray-200 ${canNavigate ? 'cursor-pointer hover:text-blue-600 dark:hover:text-blue-400 hover:underline' : ''}`}
                onClick={canNavigate ? handleNavigate : undefined}
              >
                {log.description}
              </p>
            </div>
            {/* Actor + time */}
            <div className="flex items-center gap-1.5 mt-1">
              <UserAvatar name={log.userName} />
              <span className="text-xs text-gray-500 dark:text-gray-400">{log.userName}</span>
              <span className="text-gray-300 dark:text-gray-600 text-xs">·</span>
              <span className="text-xs text-gray-400 dark:text-gray-500" title={formatFullDate(log.createdAt)}>
                {timeAgo(log.createdAt)}
              </span>
            </div>
          </div>

          {/* Expand toggle if there are old values */}
          {log.oldValues && (
            <button
              type="button"
              onClick={() => setExpanded(!expanded)}
              className="flex-shrink-0 text-xs text-gray-400 dark:text-gray-500 hover:text-gray-600 dark:hover:text-gray-300 mt-0.5"
            >
              {expanded ? 'hide' : 'diff'}
            </button>
          )}
        </div>

        {/* Old values diff */}
        {expanded && log.oldValues && (
          <pre className="mt-2 p-2 bg-gray-50 dark:bg-gray-900 rounded text-xs text-gray-600 dark:text-gray-300 overflow-x-auto border border-gray-200 dark:border-gray-700 whitespace-pre-wrap">
            {log.oldValues}
          </pre>
        )}
      </div>
    </li>
  );
}

// ─── Page ────────────────────────────────────────────────────────────────────

export function ActivityPage() {
  const [activeFilter, setActiveFilter] = useState<FilterTab>('All');
  const [page, setPage] = useState(1);
  const PAGE_SIZE = 30;

  const entityType = FILTER_TABS.find(t => t.value === activeFilter)?.entityType;

  const { data, isLoading, isError } = useQuery({
    queryKey: ['audit-logs', entityType, page],
    queryFn: () => apiService.getAuditLogs({ entityType, page, pageSize: PAGE_SIZE }),
  });

  const handleFilterChange = (filter: FilterTab) => {
    setActiveFilter(filter);
    setPage(1);
  };

  return (
    <div className="p-8">
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Activity</h1>
        <p className="mt-1 text-sm text-gray-500 dark:text-gray-400">
          Full audit trail of changes made within your firm.
        </p>
      </div>

      {/* Filter tabs */}
      <div className="flex gap-1 mb-6 border-b border-gray-200 dark:border-gray-700">
        {FILTER_TABS.map(tab => (
          <button
            key={tab.value}
            type="button"
            onClick={() => handleFilterChange(tab.value)}
            className={`px-4 py-2 text-sm font-medium border-b-2 -mb-px transition-colors ${
              activeFilter === tab.value
                ? 'border-blue-600 text-blue-600 dark:text-blue-400 dark:border-blue-400'
                : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:border-gray-300 dark:hover:border-gray-600'
            }`}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {/* Content */}
      {isLoading && (
        <div className="flex items-center justify-center py-16 text-gray-400 dark:text-gray-500">
          <svg className="animate-spin w-5 h-5 mr-2" fill="none" viewBox="0 0 24 24">
            <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4" />
            <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4z" />
          </svg>
          Loading activity…
        </div>
      )}

      {isError && (
        <div className="py-8 text-center text-red-500 text-sm">
          Failed to load activity log. Please try again.
        </div>
      )}

      {!isLoading && !isError && data && (
        <>
          {data.items.length === 0 ? (
            <div className="py-16 text-center">
              <svg className="mx-auto w-10 h-10 text-gray-300 dark:text-gray-600 mb-3" fill="none" viewBox="0 0 24 24" stroke="currentColor">
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth={1.5} d="M9 5H7a2 2 0 00-2 2v12a2 2 0 002 2h10a2 2 0 002-2V7a2 2 0 00-2-2h-2M9 5a2 2 0 002 2h2a2 2 0 002-2M9 5a2 2 0 012-2h2a2 2 0 012 2" />
              </svg>
              <p className="text-sm text-gray-500 dark:text-gray-400">No activity recorded yet.</p>
            </div>
          ) : (
            <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
              {/* Count */}
              <div className="px-5 py-3 border-b border-gray-100 dark:border-gray-700 flex items-center justify-between">
                <span className="text-xs text-gray-400 dark:text-gray-500 font-medium uppercase tracking-wide">
                  {data.totalCount} event{data.totalCount !== 1 ? 's' : ''}
                </span>
                {data.totalPages > 1 && (
                  <span className="text-xs text-gray-400 dark:text-gray-500">
                    Page {data.page} of {data.totalPages}
                  </span>
                )}
              </div>

              <ul className="divide-y divide-gray-100 dark:divide-gray-700 px-5">
                {data.items.map(log => (
                  <AuditRow key={log.id} log={log} />
                ))}
              </ul>

              {/* Pagination */}
              {data.totalPages > 1 && (
                <div className="px-5 py-3 border-t border-gray-100 dark:border-gray-700 flex items-center justify-between">
                  <button
                    type="button"
                    onClick={() => setPage(p => Math.max(1, p - 1))}
                    disabled={page === 1}
                    className="px-3 py-1.5 text-sm text-gray-600 dark:text-gray-300 border border-gray-200 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed"
                  >
                    ← Previous
                  </button>
                  <button
                    type="button"
                    onClick={() => setPage(p => Math.min(data.totalPages, p + 1))}
                    disabled={page === data.totalPages}
                    className="px-3 py-1.5 text-sm text-gray-600 dark:text-gray-300 border border-gray-200 dark:border-gray-600 rounded-md hover:bg-gray-50 dark:hover:bg-gray-700 disabled:opacity-40 disabled:cursor-not-allowed"
                  >
                    Next →
                  </button>
                </div>
              )}
            </div>
          )}
        </>
      )}
    </div>
  );
}
