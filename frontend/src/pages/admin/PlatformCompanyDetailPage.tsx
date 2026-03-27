import { useQuery } from '@tanstack/react-query';
import { Link, useParams } from 'react-router-dom';
import { apiService } from '../../services/api';
import { usePermissions } from '../../hooks/usePermissions';
import type { CompanyDetail, CompanyDocument } from '../../types';

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

const ROLE_COLORS: Record<string, string> = {
  CompanyOwner: 'bg-purple-100 text-purple-800',
  Admin: 'bg-blue-100 text-blue-800',
  User: 'bg-gray-100 text-gray-700',
  Viewer: 'bg-yellow-100 text-yellow-800',
};

export function PlatformCompanyDetailPage() {
  const { id } = useParams<{ id: string }>();
  const companyId = Number(id);
  const { isPlatformSuperAdmin } = usePermissions();

  const { data: company, isLoading } = useQuery<CompanyDetail>({
    queryKey: ['platform-company', companyId],
    queryFn: () => apiService.getPlatformCompany(companyId),
    enabled: !!companyId,
  });

  const { data: documents = [] } = useQuery<CompanyDocument[]>({
    queryKey: ['platform-company-docs', companyId],
    queryFn: () => apiService.getPlatformCompanyDocuments(companyId),
    enabled: !!companyId && isPlatformSuperAdmin,
  });

  if (isLoading || !company) {
    return <div className="p-8 text-gray-500">Loading…</div>;
  }

  return (
    <div className="p-8 max-w-7xl mx-auto">
      {/* Breadcrumb */}
      <nav className="flex items-center gap-2 text-sm text-gray-500 mb-6">
        <Link to="/admin" className="hover:text-blue-600">Customers</Link>
        <span>/</span>
        <span className="text-gray-900 font-medium">{company.name}</span>
      </nav>

      {/* Header */}
      <div className="bg-white rounded-xl border border-gray-200 p-6 mb-6">
        <div className="flex items-start justify-between">
          <div>
            <div className="flex items-center gap-2 mb-1">
              <span className="text-xs font-semibold uppercase tracking-widest text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded">
                Lawgate Admin
              </span>
              <span className={`text-xs font-medium px-2 py-0.5 rounded ${company.isActive ? 'bg-green-100 text-green-800' : 'bg-gray-100 text-gray-600'}`}>
                {company.isActive ? 'Active' : 'Inactive'}
              </span>
            </div>
            <h1 className="text-2xl font-bold text-gray-900">{company.name}</h1>
            <p className="text-gray-500 text-sm mt-1">{company.email} · {company.subscriptionTier}</p>
            {(company.city || company.country) && (
              <p className="text-gray-400 text-xs mt-1">
                {[company.city, company.state, company.country].filter(Boolean).join(', ')}
              </p>
            )}
          </div>
          <div className="grid grid-cols-4 gap-4 text-center">
            {[
              { label: 'Users', value: company.userCount },
              { label: 'Projects', value: company.projectCount },
              { label: 'Documents', value: company.documentCount },
              { label: 'Storage', value: formatBytes(company.storageUsedBytes) },
            ].map((s) => (
              <div key={s.label} className="bg-gray-50 rounded-lg p-3 min-w-[80px]">
                <p className="text-lg font-bold text-gray-900">{s.value}</p>
                <p className="text-xs text-gray-500">{s.label}</p>
              </div>
            ))}
          </div>
        </div>
      </div>

      <div className="grid grid-cols-2 gap-6 mb-6">
        {/* Users */}
        <div className="bg-white rounded-xl border border-gray-200">
          <div className="px-4 py-3 border-b border-gray-100 font-medium text-gray-900 text-sm">
            Team Members ({company.users.length})
          </div>
          <div className="divide-y divide-gray-50">
            {company.users.map((u) => (
              <div key={u.id} className="px-4 py-3 flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-900">{u.firstName} {u.lastName}</p>
                  <p className="text-xs text-gray-400">{u.email}</p>
                </div>
                <div className="flex items-center gap-2">
                  <span className={`text-xs px-2 py-0.5 rounded font-medium ${ROLE_COLORS[u.role] ?? 'bg-gray-100 text-gray-700'}`}>
                    {u.role}
                  </span>
                  <span className={`w-1.5 h-1.5 rounded-full ${u.isActive ? 'bg-green-500' : 'bg-gray-300'}`} />
                </div>
              </div>
            ))}
            {company.users.length === 0 && (
              <p className="px-4 py-6 text-sm text-gray-400 text-center">No users</p>
            )}
          </div>
        </div>

        {/* Projects */}
        <div className="bg-white rounded-xl border border-gray-200">
          <div className="px-4 py-3 border-b border-gray-100 font-medium text-gray-900 text-sm">
            Projects ({company.projects.length})
          </div>
          <div className="divide-y divide-gray-50">
            {company.projects.map((p) => (
              <div key={p.id} className="px-4 py-3 flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-900">{p.name}</p>
                  {p.clientName && <p className="text-xs text-gray-400">{p.clientName}</p>}
                </div>
                <div className="flex items-center gap-3 text-xs text-gray-500">
                  <span>{p.documentCount} docs</span>
                  <span className="px-2 py-0.5 rounded bg-gray-100 text-gray-700">{p.status}</span>
                </div>
              </div>
            ))}
            {company.projects.length === 0 && (
              <p className="px-4 py-6 text-sm text-gray-400 text-center">No projects</p>
            )}
          </div>
        </div>
      </div>

      {/* Documents — super admin only */}
      {isPlatformSuperAdmin && (
        <div className="bg-white rounded-xl border border-gray-200">
          <div className="px-4 py-3 border-b border-gray-100 flex items-center justify-between">
            <span className="font-medium text-gray-900 text-sm">Documents ({documents.length})</span>
            <span className="text-xs text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded font-medium">
              Super Admin view
            </span>
          </div>
          <div className="divide-y divide-gray-50">
            {documents.map((d) => (
              <div key={d.id} className="px-4 py-3 flex items-center justify-between">
                <div>
                  <p className="text-sm font-medium text-gray-900">{d.fileName}</p>
                  <p className="text-xs text-gray-400">{d.projectName} · uploaded by {d.uploadedBy}</p>
                </div>
                <div className="flex items-center gap-3 text-xs text-gray-500">
                  <span>{formatBytes(d.fileSizeBytes)}</span>
                  <span className="px-2 py-0.5 rounded bg-gray-100 text-gray-700">{d.documentType}</span>
                  <span>{new Date(d.createdAt).toLocaleDateString()}</span>
                </div>
              </div>
            ))}
            {documents.length === 0 && (
              <p className="px-4 py-6 text-sm text-gray-400 text-center">No documents</p>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
