# Claude.ai Long-Term Memory Context - Frontend

## Frontend Technology Stack

### Core: React 18 + Vite + TypeScript
- **React 18**: Modern React with concurrent features, automatic batching
- **Vite**: Sub-second HMR, optimized builds, native ESM
- **TypeScript**: Full type safety, better IDE support, catch errors early

### Why This Stack?
1. **Vite over Create React App**: 10-100x faster dev server, modern tooling
2. **TypeScript**: Prevents bugs, better refactoring, self-documenting code
3. **React 18**: Better performance, concurrent rendering, server components ready

## Project Structure Philosophy

```
src/
├── components/         # Reusable UI components (Button, Input, Card, etc.)
│   └── [Component]/
│       ├── Component.tsx
│       ├── Component.test.tsx
│       └── index.ts
├── pages/             # Route-level components (HomePage, LoginPage, etc.)
├── services/          # API calls, external services
├── hooks/             # Custom React hooks
├── contexts/          # React Context for global state
├── utils/             # Pure utility functions
├── types/             # TypeScript type definitions
└── assets/            # Images, fonts, static files
```

### Component Organization
- **Components**: Reusable, presentational, stateless when possible
- **Pages**: Route-level, can be stateful, compose components
- **Services**: API layer, isolate backend communication
- **Hooks**: Reusable logic, separate from UI

## Critical Concepts

### Tailwind CSS
Utility-first CSS framework - no separate CSS files needed!

```typescript
// Instead of writing CSS:
<div className="container">  {/* NO! */}

// Write utility classes:
<div className="max-w-7xl mx-auto px-4 py-8">  {/* YES! */}
```

**Common Patterns**:
```typescript
// Layout
className="flex items-center justify-between"
className="grid grid-cols-1 md:grid-cols-3 gap-4"
className="container mx-auto px-4"

// Spacing
className="p-4"  // padding all sides
className="px-4 py-2"  // padding horizontal/vertical
className="mt-4 mb-8"  // margin top/bottom

// Colors
className="bg-blue-600 text-white"
className="hover:bg-blue-700"
className="border border-gray-300"

// Typography
className="text-2xl font-bold"
className="text-sm text-gray-600"
```

### React Router v6
```typescript
// Define routes
<Routes>
  <Route path="/" element={<HomePage />} />
  <Route path="/login" element={<LoginPage />} />
  <Route path="/dashboard" element={
    <ProtectedRoute>  {/* Wrapper for auth */}
      <DashboardPage />
    </ProtectedRoute>
  } />
</Routes>

// Navigate programmatically
const navigate = useNavigate();
navigate('/dashboard');

// Access params
const { id } = useParams();  // From /users/:id
```

### Axios Interceptors
```typescript
// Add token to ALL requests automatically
api.interceptors.request.use((config) => {
  const token = localStorage.getItem('token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Handle errors globally
api.interceptors.response.use(
  response => response,
  error => {
    if (error.response?.status === 401) {
      // Auto-logout on unauthorized
      localStorage.removeItem('token');
      window.location.href = '/login';
    }
    return Promise.reject(error);
  }
);
```

### React Context for Auth
```typescript
// Create context
const AuthContext = createContext<AuthContextType>(null!);

// Provider wraps app
<AuthContext.Provider value={{ user, login, logout }}>
  {children}
</AuthContext.Provider>

// Use anywhere in app
const { user, logout } = useAuth();
```

## State Management Strategy

### Local State (useState)
- Component-specific state
- Form inputs, toggles, UI state
- No need to share across components

### Context (React Context)
- Global state: authentication, theme, language
- Data needed by many components
- Avoid prop drilling

### Server State (React Query)
- API data, caching, background updates
- Automatic refetching, optimistic updates
- Better than managing API state yourself

```typescript
// Example with React Query
const { data, isLoading, error } = useQuery({
  queryKey: ['users'],
  queryFn: () => api.get('/users').then(res => res.data)
});
```

## TypeScript Patterns

### Props Interface
```typescript
interface ButtonProps {
  children: React.ReactNode;
  onClick: () => void;
  variant?: 'primary' | 'secondary';
  disabled?: boolean;
}

export function Button({ children, onClick, variant = 'primary', disabled }: ButtonProps) {
  // ...
}
```

### API Response Types
```typescript
// Define what backend returns
interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  role: 'Admin' | 'User';
}

interface ApiResponse<T> {
  success: boolean;
  data: T;
  message?: string;
}

// Use in service
const response = await api.get<ApiResponse<User>>('/users/me');
const user: User = response.data.data;
```

### Custom Hooks
```typescript
// Extract reusable logic
function useLocalStorage<T>(key: string, initialValue: T) {
  const [value, setValue] = useState<T>(() => {
    const item = localStorage.getItem(key);
    return item ? JSON.parse(item) : initialValue;
  });

  useEffect(() => {
    localStorage.setItem(key, JSON.stringify(value));
  }, [key, value]);

  return [value, setValue] as const;
}

// Use it
const [theme, setTheme] = useLocalStorage('theme', 'light');
```

## Environment Variables

### Vite Environment Variables
- Must prefix with `VITE_`
- Available as `import.meta.env.VITE_*`
- Never commit `.env.local` with secrets!

```env
# .env.local (for development)
VITE_API_URL=http://localhost:5000/api
VITE_APP_NAME=Lawgate

# .env.production (for production)
VITE_API_URL=https://api.lawgate.com/api
```

```typescript
// Usage
const apiUrl = import.meta.env.VITE_API_URL;
```

## Development Workflow

### Start Development
```powershell
npm run dev
# Opens http://localhost:3000
# Hot reload on file changes
```

### Common Tasks
```powershell
# Add new dependency
npm install package-name

# Add dev dependency
npm install -D package-name

# Type check
npm run type-check

# Lint
npm run lint

# Build
npm run build

# Preview build
npm run preview
```

## API Integration Pattern

### 1. Define Types
```typescript
// src/types/user.types.ts
export interface User {
  id: string;
  email: string;
  name: string;
}
```

### 2. Create Service
```typescript
// src/services/user.service.ts
import api from './api';
import { User } from '../types/user.types';

export const userService = {
  getUsers: () => api.get<User[]>('/users').then(res => res.data),
  getUser: (id: string) => api.get<User>(`/users/${id}`).then(res => res.data),
  updateUser: (id: string, data: Partial<User>) => 
    api.put<User>(`/users/${id}`, data).then(res => res.data),
};
```

### 3. Use in Component
```typescript
// src/pages/UsersPage.tsx
import { useEffect, useState } from 'react';
import { userService } from '../services/user.service';
import { User } from '../types/user.types';

export function UsersPage() {
  const [users, setUsers] = useState<User[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    userService.getUsers()
      .then(setUsers)
      .finally(() => setLoading(false));
  }, []);

  if (loading) return <div>Loading...</div>;

  return (
    <div>
      {users.map(user => (
        <div key={user.id}>{user.name}</div>
      ))}
    </div>
  );
}
```

## Form Handling with React Hook Form

```typescript
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

// Define validation schema
const loginSchema = z.object({
  email: z.string().email('Invalid email'),
  password: z.string().min(6, 'Password must be 6+ characters'),
});

type LoginForm = z.infer<typeof loginSchema>;

export function LoginPage() {
  const { register, handleSubmit, formState: { errors } } = useForm<LoginForm>({
    resolver: zodResolver(loginSchema),
  });

  const onSubmit = (data: LoginForm) => {
    // data is typed and validated!
    authService.login(data.email, data.password);
  };

  return (
    <form onSubmit={handleSubmit(onSubmit)}>
      <input {...register('email')} />
      {errors.email && <span>{errors.email.message}</span>}
      
      <input type="password" {...register('password')} />
      {errors.password && <span>{errors.password.message}</span>}
      
      <button type="submit">Login</button>
    </form>
  );
}
```

## Performance Best Practices

### 1. Lazy Loading
```typescript
// Lazy load route components
const DashboardPage = lazy(() => import('./pages/DashboardPage'));

<Suspense fallback={<LoadingSpinner />}>
  <DashboardPage />
</Suspense>
```

### 2. Memoization
```typescript
// Memo expensive calculations
const expensiveValue = useMemo(() => {
  return complexCalculation(data);
}, [data]);

// Memo callback functions
const handleClick = useCallback(() => {
  doSomething(id);
}, [id]);

// Memo entire component
export const ExpensiveComponent = memo(function ExpensiveComponent({ data }) {
  // Only re-renders if data changes
  return <div>{data}</div>;
});
```

### 3. Virtual Scrolling
```typescript
// For long lists, use react-window
import { FixedSizeList } from 'react-window';

<FixedSizeList
  height={500}
  itemCount={1000}
  itemSize={35}
>
  {Row}
</FixedSizeList>
```

## Deployment to Azure

### Azure Static Web Apps (Recommended)
```powershell
# Build
npm run build

# Deploy via Azure Portal
# or GitHub Actions (automatic)
```

### Azure Blob Storage
```powershell
# Build
npm run build

# Upload to $web container
az storage blob upload-batch \
  --account-name lawgatestorage \
  --destination '$web' \
  --source ./dist
```

### Environment Variables in Azure
Set in Azure Portal > Configuration:
- `VITE_API_URL` → Your backend API URL
- Build command: `npm run build`
- Output directory: `dist`

## Common Pitfalls & Solutions

### ❌ Don't do this:
```typescript
// Using index as key
{items.map((item, index) => <div key={index}>{item}</div>)}

// Mutating state directly
user.name = 'New Name'; // NO!
setUser(user); // Won't trigger re-render

// Missing dependencies in useEffect
useEffect(() => {
  fetchData(id);
}, []); // Missing 'id' dependency!
```

### ✅ Do this instead:
```typescript
// Use stable ID as key
{items.map(item => <div key={item.id}>{item}</div>)}

// Create new object
setUser({ ...user, name: 'New Name' });

// Include all dependencies
useEffect(() => {
  fetchData(id);
}, [id]);
```

## Troubleshooting

### Vite not updating on changes
1. Check Windows Defender isn't scanning project folder
2. Try: `npm run dev -- --host`
3. Delete `node_modules/.vite` cache

### Build fails with TypeScript errors
```powershell
# Check errors
npm run type-check

# Clear cache
Remove-Item -Recurse .vite, node_modules/.vite
npm run build
```

### API calls failing
1. Check backend is running
2. Check CORS configuration in backend
3. Check `VITE_API_URL` in `.env.local`
4. Open browser DevTools > Network tab

## VS Code Setup

### Recommended Extensions
- ESLint
- Prettier
- Tailwind CSS IntelliSense
- TypeScript Vue Plugin (Volar)
- Auto Rename Tag

### Settings (`.vscode/settings.json`)
```json
{
  "editor.formatOnSave": true,
  "editor.defaultFormatter": "esbenp.prettier-vscode",
  "tailwindCSS.experimental.classRegex": [
    ["clsx\\(([^)]*)\\)", "(?:'|\"|`)([^']*)(?:'|\"|`)"]
  ]
}
```

## Remember for Future Claude

### When user returns:
1. Run `npm install` to install dependencies
2. Copy `.env.example` to `.env.local` and configure
3. Ensure backend is running
4. Run `npm run dev`
5. All state management is in code, no external config needed!

### Key Commands:
```powershell
# Fresh start
npm install
npm run dev

# Production build
npm run build
npm run preview  # Test production build locally

# Clean restart
Remove-Item -Recurse node_modules
npm install
```

### Project Conventions:
- Components in PascalCase: `Button.tsx`
- Hooks in camelCase with 'use' prefix: `useAuth.ts`
- Services in camelCase with 'service' suffix: `auth.service.ts`
- Types in camelCase with 'types' suffix: `user.types.ts`

## Next Steps (TODO)
- [ ] Set up React Query for server state
- [ ] Add error boundary component
- [ ] Implement toast notifications
- [ ] Add loading states for all async operations
- [ ] Set up E2E tests with Playwright
- [ ] Add analytics (Application Insights)
- [ ] Implement internationalization (i18n)
- [ ] Add PWA support (service worker, manifest)
