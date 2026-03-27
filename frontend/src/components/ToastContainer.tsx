import { useToast } from '../contexts/ToastContext';
import { XMarkIcon, CheckCircleIcon, ExclamationCircleIcon, InformationCircleIcon, ExclamationTriangleIcon } from '@heroicons/react/24/solid';
import { cn } from '../utils/cn';
import type { ToastType } from '../types';

const borderClass: Record<ToastType, string> = {
  success: 'border-l-green-500',
  error: 'border-l-red-500',
  warning: 'border-l-yellow-500',
  info: 'border-l-blue-500',
};

function ToastIcon({ type }: { type: ToastType }) {
  if (type === 'success') return <CheckCircleIcon className="w-5 h-5 text-green-500" />;
  if (type === 'error') return <ExclamationCircleIcon className="w-5 h-5 text-red-500" />;
  if (type === 'warning') return <ExclamationTriangleIcon className="w-5 h-5 text-yellow-500" />;
  return <InformationCircleIcon className="w-5 h-5 text-blue-500" />;
}

export function ToastContainer() {
  const { toasts, dismissToast } = useToast();

  if (toasts.length === 0) return null;

  return (
    <div className="fixed bottom-4 right-4 z-50 flex flex-col gap-2 w-80">
      {toasts.map((toast) => (
        <div
          key={toast.id}
          className={cn(
            'flex items-start gap-3 bg-white rounded-lg shadow-lg border border-gray-200 border-l-4 p-4',
            borderClass[toast.type]
          )}
        >
          <div className="flex-shrink-0 mt-0.5"><ToastIcon type={toast.type} /></div>
          <p className="flex-1 text-sm text-gray-800">{toast.message}</p>
          <button
            onClick={() => dismissToast(toast.id)}
            className="flex-shrink-0 text-gray-400 hover:text-gray-600"
          >
            <XMarkIcon className="w-4 h-4" />
          </button>
        </div>
      ))}
    </div>
  );
}
