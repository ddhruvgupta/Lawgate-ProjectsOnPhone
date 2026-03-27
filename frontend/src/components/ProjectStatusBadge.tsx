import { cn } from '../utils/cn';
import type { ProjectStatus } from '../types';

const config: Record<string, { label: string; classes: string }> = {
  Intake:      { label: 'Intake',           classes: 'bg-slate-100 text-slate-700 ring-slate-500/20' },
  Active:      { label: 'Active',           classes: 'bg-green-50 text-green-700 ring-green-600/20' },
  Discovery:   { label: 'Discovery',        classes: 'bg-blue-50 text-blue-700 ring-blue-600/20' },
  Negotiation: { label: 'Negotiation',      classes: 'bg-yellow-50 text-yellow-700 ring-yellow-600/20' },
  Hearing:     { label: 'Hearing',          classes: 'bg-orange-50 text-orange-700 ring-orange-600/20' },
  OnHold:      { label: 'On Hold',          classes: 'bg-gray-100 text-gray-600 ring-gray-500/20' },
  Settled:     { label: 'Settled',          classes: 'bg-teal-50 text-teal-700 ring-teal-600/20' },
  Closed:      { label: 'Closed',           classes: 'bg-gray-100 text-gray-500 ring-gray-400/20' },
  Archived:    { label: 'Archived',         classes: 'bg-gray-100 text-gray-400 ring-gray-300/20' },
};

export const ProjectStatusBadge: React.FC<{ status: ProjectStatus | string }> = ({ status }) => {
  const { label, classes } = config[status] ?? {
    label: status,
    classes: 'bg-gray-100 text-gray-600 ring-gray-500/20',
  };
  return (
    <span className={cn('inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium ring-1 ring-inset', classes)}>
      {label}
    </span>
  );
};
