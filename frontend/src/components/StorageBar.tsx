import React from 'react';
import { useCompany } from '../hooks/useCompany';
import { formatBytes } from '../utils/formatters';

// ─── Tier display metadata ────────────────────────────────────────────────────

const TIER_META: Record<string, { label: string; className: string }> = {
  Trial: {
    label: 'Trial',
    className: 'bg-amber-50 text-amber-700 dark:bg-amber-950 dark:text-amber-300',
  },
  Basic: {
    label: 'Basic',
    className: 'bg-blue-50 text-blue-700 dark:bg-blue-950 dark:text-blue-300',
  },
  Professional: {
    label: 'Professional',
    className: 'bg-violet-50 text-violet-700 dark:bg-violet-950 dark:text-violet-300',
  },
  Enterprise: {
    label: 'Enterprise',
    className: 'bg-emerald-50 text-emerald-700 dark:bg-emerald-950 dark:text-emerald-300',
  },
};

function getBarColor(pct: number): string {
  if (pct >= 90) return 'bg-red-500';
  if (pct >= 75) return 'bg-amber-400';
  return 'bg-blue-500';
}

// ─── Component ────────────────────────────────────────────────────────────────

export const StorageBar: React.FC = () => {
  const { data: company, isLoading } = useCompany();

  if (isLoading) {
    return (
      <div className="px-4 py-3 space-y-2" aria-hidden="true">
        <div className="h-4 w-16 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
        <div className="h-1.5 bg-gray-200 dark:bg-gray-700 rounded-full animate-pulse" />
        <div className="h-3 w-28 bg-gray-200 dark:bg-gray-700 rounded animate-pulse" />
      </div>
    );
  }

  if (!company) return null;

  const pct =
    company.storageQuotaBytes > 0
      ? Math.min(100, (company.storageUsedBytes / company.storageQuotaBytes) * 100)
      : 0;

  const tierMeta = TIER_META[company.subscriptionTier] ?? TIER_META['Trial'];
  const barColor = getBarColor(pct);

  // Days left on trial
  let trialDaysLeft: number | null = null;
  if (company.subscriptionTier === 'Trial' && company.subscriptionEndDate) {
    const diff = new Date(company.subscriptionEndDate).getTime() - Date.now();
    trialDaysLeft = Math.max(0, Math.ceil(diff / (1000 * 60 * 60 * 24)));
  }

  return (
    <div className="px-4 py-3 space-y-2">
      {/* Tier badge + trial countdown */}
      <div className="flex items-center justify-between">
        <span
          className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-semibold ${tierMeta.className}`}
        >
          {tierMeta.label}
        </span>
        {trialDaysLeft !== null && (
          <span
            className={`text-xs font-medium ${trialDaysLeft <= 3 ? 'text-red-500 dark:text-red-400' : 'text-amber-600 dark:text-amber-400'}`}
            aria-label={`${trialDaysLeft} days left on trial`}
          >
            {trialDaysLeft}d left
          </span>
        )}
      </div>

      {/* Storage bar */}
      <div className="space-y-1">
        <div
          className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-1.5 overflow-hidden"
          role="presentation"
        >
          <div
            className={`h-1.5 rounded-full transition-all duration-500 ${barColor}`}
            style={{ width: `${pct.toFixed(1)}%` }}
            role="progressbar"
            aria-valuenow={Math.round(pct)}
            aria-valuemin={0}
            aria-valuemax={100}
            aria-label={`Storage ${Math.round(pct)}% used`}
          />
        </div>
        <p className="text-xs text-gray-500 dark:text-gray-400">
          {formatBytes(company.storageUsedBytes)}{' '}
          <span className="text-gray-400 dark:text-gray-600">of</span>{' '}
          {formatBytes(company.storageQuotaBytes)} used
        </p>
      </div>

      {/* Warning when nearly full */}
      {pct >= 90 && (
        <p className="text-xs text-red-600 dark:text-red-400 font-medium leading-tight">
          Storage almost full — upgrade your plan
        </p>
      )}
    </div>
  );
};
