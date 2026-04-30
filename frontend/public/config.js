// Runtime configuration for Lawgate.
// This file is served as a static asset and is NOT bundled — it can be
// overwritten after build to retarget a different API without rebuilding.
// The deployment pipeline should overwrite this file with env-specific values.
window.__CONFIG__ = {
  // Overwritten by the deployment pipeline per environment.
  // Leave empty in-repo so local dev falls back to VITE_API_URL.
  apiUrl: '',
};
