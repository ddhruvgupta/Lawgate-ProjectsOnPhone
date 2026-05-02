import React from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { useAuth } from '../contexts/AuthContext';
import { useCompany } from '../hooks/useCompany';
import { apiService } from '../services/api';
import { formatDate } from '../utils/formatters';
import { ProjectStatusBadge } from '../components/ProjectStatusBadge';
import { CardSkeleton } from '../components/LoadingSkeleton';
import {
  FolderIcon,
  UsersIcon,
  DocumentIcon,
  PlusIcon,
  ArrowRightIcon,
  CheckCircleIcon,
} from '@heroicons/react/24/outline';

export const DashboardPage: React.FC = () => {
  const { user } = useAuth();
  const { data: company } = useCompany();
  const navigate = useNavigate();

  const { data: projects = [], isLoading: projectsLoading } = useQuery({
    queryKey: ['projects'],
    queryFn: () => apiService.getProjects(),
  });

  const { data: teamMembers = [], isLoading: teamLoading } = useQuery({
    queryKey: ['team'],
    queryFn: () => apiService.getTeamMembers(),
  });

  const activeProjects = projects.filter((p) => p.status === 'Active');
  const totalDocuments = projects.reduce((sum, p) => sum + (p.documentCount ?? 0), 0);
  const recentProjects = [...projects]
    .sort((a, b) => new Date(b.createdAt).getTime() - new Date(a.createdAt).getTime())
    .slice(0, 5);

  return (
    <div className="p-8 max-w-7xl mx-auto">
      {/* Page header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">
          Welcome back, {user?.firstName}
        </h1>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
          {company?.name ? <><span className="font-medium text-gray-700 dark:text-gray-300">{company.name}</span> &mdash; </> : null}Here's an overview of your firm's activity.
        </p>
      </div>

      {/* Email verification warning */}
      {user && !user.isEmailVerified && (
        <div className="mb-6 bg-yellow-50 dark:bg-yellow-900/20 border border-yellow-300 dark:border-yellow-700 text-yellow-800 dark:text-yellow-200 px-4 py-3 rounded flex items-center justify-between">
          <span>Your email address has not been verified. Please check your inbox.</span>
          <Link to="/resend-verification" className="ml-4 font-medium underline text-yellow-900 dark:text-yellow-100">
            Resend email
          </Link>
        </div>
      )}

      {/* Stat cards */}
      {projectsLoading || teamLoading ? (
        <CardSkeleton count={4} />
      ) : (
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 mb-8">
        <StatCard
          label="Total Projects"
          value={String(projects.length)}
          icon={<FolderIcon className="w-6 h-6 text-blue-600 dark:text-blue-400" />}
          bg="bg-blue-50 dark:bg-blue-900/30"
        />
        <StatCard
          label="Active Projects"
          value={String(activeProjects.length)}
          icon={<CheckCircleIcon className="w-6 h-6 text-green-600 dark:text-green-400" />}
          bg="bg-green-50 dark:bg-green-900/30"
        />
        <StatCard
          label="Total Documents"
          value={String(totalDocuments)}
          icon={<DocumentIcon className="w-6 h-6 text-purple-600 dark:text-purple-400" />}
          bg="bg-purple-50 dark:bg-purple-900/30"
        />
        <StatCard
          label="Team Members"
          value={String(teamMembers.length)}
          icon={<UsersIcon className="w-6 h-6 text-orange-600 dark:text-orange-400" />}
          bg="bg-orange-50 dark:bg-orange-900/30"
        />
      </div>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* Recent projects — 2/3 width */}
        <div className="lg:col-span-2 bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700">
          <div className="flex items-center justify-between px-6 py-4 border-b border-gray-100 dark:border-gray-700">
            <h2 className="font-semibold text-gray-900 dark:text-white">Recent Projects</h2>
            <Link
              to="/projects"
              className="text-sm text-blue-600 hover:text-blue-700 flex items-center gap-1"
            >
              View all <ArrowRightIcon className="w-3.5 h-3.5" />
            </Link>
          </div>

          {projectsLoading ? (
            <div className="p-6 space-y-3">
              {[1, 2, 3].map((i) => (
                <div key={i} className="h-14 bg-gray-100 dark:bg-gray-700 rounded-lg animate-pulse" />
              ))}
            </div>
          ) : recentProjects.length === 0 ? (
            <div className="py-16 text-center">
              <FolderIcon className="w-10 h-10 text-gray-300 mx-auto mb-3" />
              <p className="text-sm font-medium text-gray-500 dark:text-gray-400">No projects yet</p>
              <button
                onClick={() => navigate('/projects')}
                className="mt-3 text-sm text-blue-600 hover:text-blue-700 font-medium"
              >
                Create your first project →
              </button>
            </div>
          ) : (
            <ul className="divide-y divide-gray-100 dark:divide-gray-700">
              {recentProjects.map((project) => (
                <li key={project.id}>
                  <Link
                    to={`/projects/${project.id}`}
                    className="flex items-center gap-4 px-6 py-4 hover:bg-gray-50 dark:hover:bg-gray-700/50 transition-colors group"
                  >
                    <div className="w-9 h-9 rounded-lg bg-blue-50 dark:bg-blue-900/30 flex items-center justify-center flex-shrink-0">
                      <FolderIcon className="w-5 h-5 text-blue-600" />
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="font-medium text-gray-900 dark:text-white truncate group-hover:text-blue-600 dark:group-hover:text-blue-400 transition-colors">
                        {project.name}
                      </p>
                      <p className="text-xs text-gray-500 dark:text-gray-400 mt-0.5">
                        {project.clientName ?? 'No client'} &middot; {project.documentCount}{' '}
                        doc{project.documentCount !== 1 ? 's' : ''}
                      </p>
                    </div>
                    <div className="flex items-center gap-3 flex-shrink-0">
                      <ProjectStatusBadge status={project.status} />
                      <span className="text-xs text-gray-400 dark:text-gray-500 hidden sm:block">
                        {formatDate(project.createdAt)}
                      </span>
                    </div>
                  </Link>
                </li>
              ))}
            </ul>
          )}
        </div>

        {/* Quick actions — 1/3 width */}
        <div className="space-y-4">
          <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6">
            <h2 className="font-semibold text-gray-900 dark:text-white mb-4">Quick Actions</h2>
            <div className="space-y-3">
              <QuickAction
                label="New Project"
                description="Start a new matter or case"
                icon={<PlusIcon className="w-5 h-5 text-blue-600 dark:text-blue-400" />}
                bg="bg-blue-50 dark:bg-blue-900/30"
                onClick={() => navigate('/projects?new=1')}
              />
              <QuickAction
                label="Manage Team"
                description="Invite or manage firm members"
                icon={<UsersIcon className="w-5 h-5 text-purple-600 dark:text-purple-400" />}
                bg="bg-purple-50 dark:bg-purple-900/30"
                onClick={() => navigate('/team')}
              />
            </div>
          </div>

          {/* Account summary */}
          <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-6">
            <h2 className="font-semibold text-gray-900 dark:text-white mb-3">Your Account</h2>
            <dl className="space-y-2 text-sm">
              <div className="flex justify-between">
                <dt className="text-gray-500 dark:text-gray-400">Name</dt>
                <dd className="font-medium text-gray-900 dark:text-white">
                  {user?.firstName} {user?.lastName}
                </dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500 dark:text-gray-400">Role</dt>
                <dd className="font-medium text-gray-900 dark:text-white">{user?.role}</dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500 dark:text-gray-400">Email</dt>
                <dd className="font-medium text-gray-900 dark:text-white truncate max-w-[160px] text-right">
                  {user?.email}
                </dd>
              </div>
              <div className="flex justify-between">
                <dt className="text-gray-500 dark:text-gray-400">Firm</dt>
                <dd className="font-medium text-gray-900 dark:text-white truncate max-w-[160px] text-right">
                  {company?.name ?? '\u2014'}
                </dd>
              </div>
            </dl>
          </div>
        </div>
      </div>
    </div>
  );
};

const StatCard: React.FC<{
  label: string;
  value: string;
  icon: React.ReactNode;
  bg: string;
}> = ({ label, value, icon, bg }) => (
  <div className="bg-white dark:bg-gray-800 rounded-xl border border-gray-200 dark:border-gray-700 p-5 flex items-center gap-4">
    <div className={`w-12 h-12 rounded-lg ${bg} flex items-center justify-center flex-shrink-0`}>
      {icon}
    </div>
    <div>
      <p className="text-sm text-gray-500 dark:text-gray-400">{label}</p>
      <p className="text-2xl font-bold text-gray-900 dark:text-white">{value}</p>
    </div>
  </div>
);

const QuickAction: React.FC<{
  label: string;
  description: string;
  icon: React.ReactNode;
  bg: string;
  onClick: () => void;
}> = ({ label, description, icon, bg, onClick }) => (
  <button
    onClick={onClick}
    className="w-full flex items-center gap-3 p-3 rounded-lg border border-gray-200 dark:border-gray-700 hover:border-blue-300 dark:hover:border-blue-600 hover:bg-blue-50/50 dark:hover:bg-blue-900/20 transition-colors text-left"
  >
    <div className={`w-9 h-9 rounded-lg ${bg} flex items-center justify-center flex-shrink-0`}>
      {icon}
    </div>
    <div>
      <p className="text-sm font-medium text-gray-900 dark:text-white">{label}</p>
      <p className="text-xs text-gray-500 dark:text-gray-400">{description}</p>
    </div>
  </button>
);
