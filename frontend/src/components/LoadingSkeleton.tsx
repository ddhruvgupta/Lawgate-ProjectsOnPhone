import React from 'react';

interface Props {
  /** Number of skeleton rows to show */
  rows?: number;
  /** Show a full-page spinner instead of skeleton rows */
  spinner?: boolean;
}

export const LoadingSkeleton: React.FC<Props> = ({ rows = 5, spinner = false }) => {
  if (spinner) {
    return (
      <div className="flex items-center justify-center py-20">
        <div className="animate-spin rounded-full h-10 w-10 border-b-2 border-primary-600" />
      </div>
    );
  }

  return (
    <div className="animate-pulse space-y-4">
      {Array.from({ length: rows }).map((_, i) => (
        <div key={i} className="flex items-center space-x-4">
          <div className="rounded-full bg-gray-200 dark:bg-gray-700 h-10 w-10 flex-shrink-0" />
          <div className="flex-1 space-y-2">
            <div className="h-4 bg-gray-200 dark:bg-gray-700 rounded w-3/4" />
            <div className="h-3 bg-gray-200 dark:bg-gray-700 rounded w-1/2" />
          </div>
        </div>
      ))}
    </div>
  );
};

/** Simple card skeleton used on dashboard / stats panels */
export const CardSkeleton: React.FC<{ count?: number }> = ({ count = 4 }) => (
  <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-5 mb-8">
    {Array.from({ length: count }).map((_, i) => (
      <div key={i} className="animate-pulse bg-gray-100 dark:bg-gray-700 rounded-xl h-24" />
    ))}
  </div>
);
