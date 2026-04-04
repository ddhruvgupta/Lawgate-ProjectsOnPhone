import { useEffect, useState } from 'react';

const STORAGE_KEY = 'lawgate-dark-mode';

export function useDarkMode() {
  const [isDark, setIsDark] = useState<boolean>(() => {
    // Prefer persisted preference; fall back to OS preference
    const stored = localStorage.getItem(STORAGE_KEY);
    if (stored !== null) return stored === 'true';
    return window.matchMedia('(prefers-color-scheme: dark)').matches;
  });

  useEffect(() => {
    const root = document.documentElement;
    if (isDark) {
      root.classList.add('dark');
    } else {
      root.classList.remove('dark');
    }
    localStorage.setItem(STORAGE_KEY, String(isDark));
  }, [isDark]);

  const toggle = () => setIsDark((prev) => !prev);

  return { isDark, toggle };
}
