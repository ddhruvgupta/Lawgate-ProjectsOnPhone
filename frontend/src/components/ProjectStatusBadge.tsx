import { cn } from '../utils/cn';
import type { ProjectStatus } from '../types';

const config: Record<string, { label: string; classes: string }> = {
  Intake:      { label: 'Intake',           classes: 'bg-slate-100 dark:bg-slate-800 text-slate-700 dark:text-slate-300 ring-slate-500/20' },
  Active:      { label: 'Active',           classes: 'bg-green-50 dark:bg-green-900/30 text-green-700 dark:text-green-400 ring-green-600/20' },
  Discovery:   { label: 'Discovery',        classes: 'bg-blue-50 dark:bg-blue-900/30 text-blue-700 dark:text-blue-400 ring-blue-600/20' },
  Negotiation: { label: 'Negotiation',      classes: 'bg-yellow-50 dark:bg-yellow-900/30 text-yellow-700 dark:text-yellow-400 ring-yellow-600/20' },
  Hearing:     { label: 'Hearing',          classes: 'bg-orange-50 dark:bg-orange-900/30 text-orange-700 dark:text-orange-400 ring-orange-600/20' },
  OnHold:      { label: 'On Hold',          classes: 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 ring-gray-500/20' },
  Settled:     { label: 'Settled',          classes: 'bg-teal-50 dark:bg-teal-900/30 text-teal-700 dark:text-teal-400 ring-teal-600/20' },
  Closed:      { label: 'Closed',           classes: 'bg-gray-100 dark:bg-gray-700 text-gray-500 dark:text-gray-400 ring-gray-400/20' },
  Archived:    { label: 'Archived',         classes: 'bg-gray-100 dark:bg-gray-700 text-gray-400 dark:text-gray-500 ring-gray-300/20' },
};

export const ProjectStatusBadge: React.FC<{ status: ProjectStatus | string }> = ({ status }) => {
  const { label, classes } = config[status] ?? {
    label: status,
    classes: 'bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 ring-gray-500/20',
  };
  return (
    <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset', classes)}>
      {label}
    </span>
  );
};
