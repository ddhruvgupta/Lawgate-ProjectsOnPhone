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
  apiUrl: window.__CONFIG__?.apiUrl ?? import.meta.env.VITE_API_URL ?? '',
};
