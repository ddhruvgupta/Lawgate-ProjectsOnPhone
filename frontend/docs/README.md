# Frontend Documentation

## Overview
React 18 application with Vite, Tailwind CSS, TypeScript, and modern development tools.

## Project Structure
```
frontend/
├── src/
│   ├── components/      # Reusable UI components
│   ├── pages/           # Page components
│   ├── services/        # API service layer
│   ├── hooks/           # Custom React hooks
│   ├── contexts/        # React Context providers
│   ├── utils/           # Utility functions
│   ├── types/           # TypeScript type definitions
│   ├── assets/          # Images, fonts, static files
│   ├── App.tsx          # Root component
│   ├── main.tsx         # Application entry point
│   └── index.css        # Global styles (Tailwind)
├── public/              # Static assets
├── docs/                # Documentation
├── package.json         # Dependencies
├── vite.config.ts       # Vite configuration
├── tailwind.config.js   # Tailwind configuration
└── tsconfig.json        # TypeScript configuration
```

## Getting Started

### Prerequisites
- Node.js 20+ and npm
- Backend API running

### Installation
```powershell
# Install dependencies
npm install

# Start development server
npm run dev
```

### Development
```powershell
# Development server with hot reload
npm run dev

# Build for production
npm run build

# Preview production build
npm run preview

# Run linter
npm run lint

# Run tests
npm test
```

## Technology Stack

### Core
- **React 18** - UI library with concurrent features
- **TypeScript** - Type safety and better DX
- **Vite** - Lightning-fast build tool and dev server

### Styling
- **Tailwind CSS** - Utility-first CSS framework
- **PostCSS** - CSS transformations
- **Autoprefixer** - Automatic vendor prefixes

### Routing
- **React Router v6** - Client-side routing
- Lazy loading for code splitting
- Protected routes for authentication

### State Management
- **React Context** - Global state (auth, theme)
- **React Query** - Server state management
- Local state with useState/useReducer

### HTTP Client
- **Axios** - Promise-based HTTP client
- Interceptors for auth tokens
- Automatic error handling

### Form Handling
- **React Hook Form** - Performant form library
- **Zod** - TypeScript-first schema validation
- Minimal re-renders

### UI Components
- Custom components with Tailwind
- Headless UI for accessible components
- Heroicons for icon library

## Configuration

### Environment Variables
Create `.env.local`:
```env
VITE_API_URL=http://localhost:5000/api
VITE_APP_NAME=Lawgate
```

### API Configuration
File: `src/services/api.ts`
```typescript
const API_URL = import.meta.env.VITE_API_URL;
```

### Tailwind Configuration
File: `tailwind.config.js`
```javascript
module.exports = {
  content: ['./index.html', './src/**/*.{js,ts,jsx,tsx}'],
  theme: {
    extend: {
      colors: {
        primary: { /* custom colors */ },
      },
    },
  },
  plugins: [],
}
```

## Project Setup Commands

### Create React + Vite + TypeScript Project
```powershell
npm create vite@latest frontend -- --template react-ts
cd frontend
npm install
```

### Install Tailwind CSS
```powershell
npm install -D tailwindcss postcss autoprefixer
npx tailwindcss init -p
```

### Install Dependencies
```powershell
# Routing
npm install react-router-dom

# HTTP Client
npm install axios

# Form Handling
npm install react-hook-form @hookform/resolvers zod

# UI Components
npm install @headlessui/react @heroicons/react

# State Management
npm install @tanstack/react-query

# Utilities
npm install clsx
```

## Component Structure

### Example Component
```typescript
// src/components/Button/Button.tsx
import { ButtonHTMLAttributes } from 'react';
import clsx from 'clsx';

interface ButtonProps extends ButtonHTMLAttributes<HTMLButtonElement> {
  variant?: 'primary' | 'secondary';
  size?: 'sm' | 'md' | 'lg';
}

export function Button({ 
  variant = 'primary', 
  size = 'md',
  className,
  children,
  ...props 
}: ButtonProps) {
  return (
    <button
      className={clsx(
        'rounded font-medium transition',
        {
          'bg-blue-600 text-white hover:bg-blue-700': variant === 'primary',
          'bg-gray-200 text-gray-900 hover:bg-gray-300': variant === 'secondary',
          'px-3 py-1.5 text-sm': size === 'sm',
          'px-4 py-2': size === 'md',
          'px-6 py-3 text-lg': size === 'lg',
        },
        className
      )}
      {...props}
    >
      {children}
    </button>
  );
}
```

## API Integration

### API Service Layer
```typescript
// src/services/api.ts
import axios from 'axios';

const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request interceptor - Add auth token
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response interceptor - Handle errors
api.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      // Redirect to login
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);

export default api;
```

### API Calls
```typescript
// src/services/auth.service.ts
import api from './api';

export const authService = {
  login: async (email: string, password: string) => {
    const response = await api.post('/auth/login', { email, password });
    return response.data;
  },
  
  register: async (data: RegisterData) => {
    const response = await api.post('/auth/register', data);
    return response.data;
  },
  
  logout: () => {
    localStorage.removeItem('token');
  },
};
```

## Authentication Flow

### Auth Context
```typescript
// src/contexts/AuthContext.tsx
import { createContext, useContext, useState, useEffect } from 'react';
import { authService } from '../services/auth.service';

interface AuthContextType {
  user: User | null;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  isAuthenticated: boolean;
}

const AuthContext = createContext<AuthContextType>(null!);

export function AuthProvider({ children }: { children: React.ReactNode }) {
  const [user, setUser] = useState<User | null>(null);

  const login = async (email: string, password: string) => {
    const data = await authService.login(email, password);
    localStorage.setItem('token', data.token);
    setUser(data.user);
  };

  const logout = () => {
    authService.logout();
    setUser(null);
  };

  return (
    <AuthContext.Provider value={{ user, login, logout, isAuthenticated: !!user }}>
      {children}
    </AuthContext.Provider>
  );
}

export const useAuth = () => useContext(AuthContext);
```

### Protected Routes
```typescript
// src/components/ProtectedRoute.tsx
import { Navigate } from 'react-router-dom';
import { useAuth } from '../contexts/AuthContext';

export function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const { isAuthenticated } = useAuth();
  
  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }
  
  return <>{children}</>;
}
```

## Routing Setup

```typescript
// src/App.tsx
import { BrowserRouter, Routes, Route } from 'react-router-dom';
import { AuthProvider } from './contexts/AuthContext';
import { ProtectedRoute } from './components/ProtectedRoute';
import { HomePage } from './pages/HomePage';
import { LoginPage } from './pages/LoginPage';
import { DashboardPage } from './pages/DashboardPage';

function App() {
  return (
    <BrowserRouter>
      <AuthProvider>
        <Routes>
          <Route path="/" element={<HomePage />} />
          <Route path="/login" element={<LoginPage />} />
          <Route
            path="/dashboard"
            element={
              <ProtectedRoute>
                <DashboardPage />
              </ProtectedRoute>
            }
          />
        </Routes>
      </AuthProvider>
    </BrowserRouter>
  );
}

export default App;
```

## Styling with Tailwind

### Setup
```css
/* src/index.css */
@tailwind base;
@tailwind components;
@tailwind utilities;

/* Custom styles */
@layer components {
  .btn-primary {
    @apply px-4 py-2 bg-blue-600 text-white rounded hover:bg-blue-700 transition;
  }
}
```

### Usage
```typescript
<div className="container mx-auto px-4">
  <h1 className="text-3xl font-bold text-gray-900">
    Welcome
  </h1>
  <button className="btn-primary">
    Click Me
  </button>
</div>
```

## Build & Deployment

### Build for Production
```powershell
npm run build
```
Output: `dist/` folder

### Deploy to Azure Static Web Apps
```powershell
# Install Azure CLI
# az login

# Deploy
az staticwebapp create \
  --name lawgate-frontend \
  --resource-group lawgate-rg \
  --source ./dist \
  --location westus2 \
  --branch main
```

### Deploy to Azure Blob Storage (Static Website)
```powershell
# Build
npm run build

# Upload
az storage blob upload-batch \
  --account-name lawgatestorage \
  --destination '$web' \
  --source ./dist
```

## Performance Optimization

### Code Splitting
```typescript
// Lazy load routes
const DashboardPage = lazy(() => import('./pages/DashboardPage'));

<Suspense fallback={<Loading />}>
  <DashboardPage />
</Suspense>
```

### Image Optimization
```typescript
// Use WebP format with fallback
<picture>
  <source srcSet="image.webp" type="image/webp" />
  <img src="image.jpg" alt="Description" />
</picture>
```

### Bundle Size Analysis
```powershell
npm run build
npx vite-bundle-visualizer
```

## Testing

### Unit Tests (Vitest)
```powershell
npm install -D vitest @testing-library/react @testing-library/jest-dom
npm test
```

### E2E Tests (Playwright)
```powershell
npm install -D @playwright/test
npx playwright test
```

## Accessibility

### Best Practices
- Use semantic HTML
- Add ARIA labels
- Ensure keyboard navigation
- Test with screen readers
- Maintain color contrast ratios

### Example
```typescript
<button
  aria-label="Close dialog"
  onClick={handleClose}
  className="..."
>
  <XMarkIcon className="h-5 w-5" aria-hidden="true" />
</button>
```

## Troubleshooting

### "Module not found" Error
```powershell
# Clear cache and reinstall
Remove-Item -Recurse -Force node_modules
Remove-Item package-lock.json
npm install
```

### "Vite HMR not working"
- Check Vite server is running
- Disable browser extensions
- Check firewall settings

### "Build fails"
```powershell
# Check TypeScript errors
npm run type-check

# Update dependencies
npm update
```

## VS Code Extensions (Recommended)
- ESLint
- Prettier
- Tailwind CSS IntelliSense
- TypeScript Vue Plugin (Volar)
- Path Intellisense

## Documentation Updates
When adding features:
1. Update this README
2. Update component documentation
3. Update `claude.md` for context
4. Add inline code comments
