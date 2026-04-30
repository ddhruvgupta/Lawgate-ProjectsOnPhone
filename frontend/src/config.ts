/** Shape of the runtime config loaded from /config.js before React boots. */
interface AppConfig {
  apiUrl: string;
}

declare global {
  interface Window {
    __CONFIG__?: Partial<AppConfig>;
  }
}

/**
 * Application configuration.
 * Priority: window.__CONFIG__ (runtime) → VITE_API_URL (build-time env) → empty string.
 * An empty string will cause Axios to use a relative base URL,
 * making it obvious in dev tools if neither is configured.
 */
export const config: AppConfig = {
  // Use window.__CONFIG__ only when it carries a non-empty URL (i.e. the
  // deployment pipeline has overwritten public/config.js). Fall back to
  // VITE_API_URL for local dev / build-time env vars.
  apiUrl: window.__CONFIG__?.apiUrl || import.meta.env.VITE_API_URL || '',
};
