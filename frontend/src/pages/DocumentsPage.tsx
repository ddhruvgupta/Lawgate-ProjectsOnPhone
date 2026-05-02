import React from 'react';
import { DocumentIcon } from '@heroicons/react/24/outline';

export const DocumentsPage: React.FC = () => {
  return (
    <div className="p-8 max-w-7xl mx-auto">
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-gray-900 dark:text-white">Documents</h1>
        <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">Firm-wide document library</p>
      </div>

      <div className="flex flex-col items-center justify-center py-24 bg-white dark:bg-gray-800 rounded-xl border border-dashed border-gray-300 dark:border-gray-600 text-center">
        <DocumentIcon className="w-12 h-12 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
        <h3 className="text-sm font-semibold text-gray-900 dark:text-white mb-1">Documents library coming soon</h3>
        <p className="text-sm text-gray-500 dark:text-gray-400 max-w-xs">
          The firm-wide document library is under development. For now, access documents through
          individual projects.
        </p>
      </div>
    </div>
  );
};
