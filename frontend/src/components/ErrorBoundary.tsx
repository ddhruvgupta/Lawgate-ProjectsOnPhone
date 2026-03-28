import { Component, type ErrorInfo, type ReactNode } from 'react';

interface Props {
  children: ReactNode;
  fallback?: ReactNode;
}

interface State {
  hasError: boolean;
  error: Error | null;
}

export class ErrorBoundary extends Component<Props, State> {
  constructor(props: Props) {
    super(props);
    this.state = { hasError: false, error: null };
  }

  static getDerivedStateFromError(error: Error): State {
    return { hasError: true, error };
  }

  componentDidCatch(error: Error, info: ErrorInfo) {
    console.error('Uncaught error:', error, info.componentStack);
  }

  private handleRetry = () => {
    this.setState({ hasError: false, error: null });
  };

  render() {
    if (this.state.hasError) {
      if (this.props.fallback) return this.props.fallback;

      return (
        <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
          <div className="max-w-md w-full bg-white p-10 rounded-xl shadow text-center space-y-4">
            <div className="text-5xl">⚠️</div>
            <h2 className="text-xl font-bold text-gray-900">Something went wrong</h2>
            <p className="text-sm text-gray-500">
              An unexpected error occurred. Please try again or refresh the page.
            </p>
            {this.state.error && (
              <pre className="text-xs text-left bg-gray-100 rounded p-3 overflow-auto max-h-32 text-red-600">
                {this.state.error.message}
              </pre>
            )}
            <div className="flex gap-3 justify-center">
              <button
                onClick={this.handleRetry}
                className="px-4 py-2 bg-primary-600 text-white text-sm rounded-lg hover:bg-primary-700 transition-colors"
              >
                Try again
              </button>
              <button
                onClick={() => window.location.reload()}
                className="px-4 py-2 bg-gray-200 text-gray-700 text-sm rounded-lg hover:bg-gray-300 transition-colors"
              >
                Refresh page
              </button>
            </div>
          </div>
        </div>
      );
    }

    return this.props.children;
  }
}
