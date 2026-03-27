import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import { apiService } from '../../services/api';
import type { CompanyOverview } from '../../types';

function formatBytes(bytes: number): string {
  if (bytes === 0) return '0 B';
  const k = 1024;
  const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
  const i = Math.floor(Math.log(bytes) / Math.log(k));
  return `${parseFloat((bytes / Math.pow(k, i)).toFixed(1))} ${sizes[i]}`;
}

const TIER_COLORS: Record<string, string> = {
  Trial: 'bg-yellow-100 text-yellow-800',
  Basic: 'bg-blue-100 text-blue-800',
  Professional: 'bg-purple-100 text-purple-800',
  Enterprise: 'bg-green-100 text-green-800',
};

export function PlatformAdminPage() {
  const { data: companies = [], isLoading, error } = useQuery<CompanyOverview[]>({
    queryKey: ['platform-companies'],
    queryFn: () => apiService.getPlatformCompanies(),
  });

  const activeCount = companies.filter((c) => c.isActive).length;
  const totalUsers = companies.reduce((s, c) => s + c.userCount, 0);
  const totalProjects = companies.reduce((s, c) => s + c.projectCount, 0);

  if (isLoading) {
    return (
      <div className="p-8 flex items-center justify-center">
        <div className="text-gray-500">Loading customers…</div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="p-8">
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 text-red-700">
          Failed to load customers.
        </div>
      </div>
    );
  }

  return (
    <div className="p-8 max-w-7xl mx-auto">
      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center gap-2 mb-1">
          <span className="text-xs font-semibold uppercase tracking-widest text-indigo-600 bg-indigo-50 px-2 py-0.5 rounded">
            Lawgate Admin
          </span>
        </div>
        <h1 className="text-2xl font-bold text-gray-900">All Customers</h1>
        <p className="text-sm text-gray-500 mt-1">{companies.length} companies</p>
      </div>

      {/* Stats bar */}
      <div className="grid grid-cols-4 gap-4 mb-8">
        {[
          { label: 'Total Companies', value: companies.length },
          { label: 'Active', value: activeCount },
          { label: 'Total Users', value: totalUsers },
          { label: 'Total Projects', value: totalProjects },
        ].map((stat) => (
          <div key={stat.label} className="bg-white rounded-xl border border-gray-200 p-4">
            <p className="text-xs text-gray-500 mb-1">{stat.label}</p>
            <p className="text-2xl font-bold text-gray-900">{stat.value}</p>
          </div>
        ))}
      </div>

      {/* Table */}
      <div className="bg-white rounded-xl border border-gray-200 overflow-hidden">
        <table className="w-full text-sm">
          <thead>
            <tr className="bg-gray-50 border-b border-gray-200">
              <th className="text-left px-4 py-3 font-medium text-gray-500 uppercase text-xs tracking-wide">Company</th>
              <th className="text-left px-4 py-3 font-medium text-gray-500 uppercase text-xs tracking-wide">Plan</th>
              <th className="text-right px-4 py-3 font-medium text-gray-500 uppercase text-xs tracking-wide">Users</th>
              <th className="text-right px-4 py-3 font-medium text-gray-500 uppercase text-xs tracking-wide">Projects</th>
              <th className="text-right px-4 py-3 font-medium text-gray-500 uppercase text-xs tracking-wide">Docs</th>
              <th className="text-right px-4 py-3 font-medium text-gray-500 uppercase text-xs tracking-wide">Storage</th>
              <th className="text-left px-4 py-3 font-medium text-gray-500 uppercase text-xs tracking-wide">Status</th>
              <th className="text-left px-4 py-3 font-medium text-gray-500 uppercase text-xs tracking-wide">Joined</th>
            </tr>
          </thead>
          <tbody>
            {companies.map((c) => (
              <tr key={c.id} className="border-b border-gray-100 hover:bg-gray-50 transition-colors">
                <td className="px-4 py-3">
                  <Link
                    to={`/admin/companies/${c.id}`}
                    className="font-medium text-gray-900 hover:text-blue-600"
                  >
                    {c.name}
                  </Link>
                  <p className="text-xs text-gray-400">{c.email}</p>
                </td>
                <td className="px-4 py-3">
                  <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${TIER_COLORS[c.subscriptionTier] ?? 'bg-gray-100 text-gray-700'}`}>
                    {c.subscriptionTier}
                  </span>
                </td>
                <td className="px-4 py-3 text-right text-gray-700">{c.userCount}</td>
                <td className="px-4 py-3 text-right text-gray-700">{c.projectCount}</td>
                <td className="px-4 py-3 text-right text-gray-700">{c.documentCount}</td>
                <td className="px-4 py-3 text-right text-gray-500 text-xs">{formatBytes(c.storageUsedBytes)}</td>
                <td className="px-4 py-3">
                  <span className={`inline-flex items-center gap-1 text-xs font-medium ${c.isActive ? 'text-green-700' : 'text-gray-400'}`}>
                    <span className={`w-1.5 h-1.5 rounded-full ${c.isActive ? 'bg-green-500' : 'bg-gray-300'}`} />
                    {c.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-4 py-3 text-gray-500 text-xs">
                  {new Date(c.createdAt).toLocaleDateString()}
                </td>
              </tr>
            ))}
          </tbody>
        </table>

        {companies.length === 0 && (
          <div className="text-center py-12 text-gray-400">No customers yet.</div>
        )}
      </div>
    </div>
  );
}
