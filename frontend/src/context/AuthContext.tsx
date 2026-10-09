import React, { createContext, useContext, useState, useEffect } from 'react';
import { api } from '../api/client';

export interface User {
  id: string;
  email: string;
  firstName: string;
  lastName: string;
  displayName: string;
  role: number | string; // 0 = User, 1 = Admin or "User", "Admin"
  isActive: boolean;
  createdAt: string;
}

interface AuthContextType {
  user: User | null;
  token: string | null;
  isLoading: boolean;
  isAdmin: boolean;
  login: (email: string, password: string) => Promise<{ success: boolean; message: string }>;
  register: (data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    displayName?: string;
  }) => Promise<{ success: boolean; message: string }>;
  logout: () => Promise<void>;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [token, setToken] = useState<string | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  useEffect(() => {
    const savedToken = localStorage.getItem('vc_access_token');
    const savedUser = localStorage.getItem('vc_user');

    if (savedToken && savedUser) {
      try {
        setToken(savedToken);
        setUser(JSON.parse(savedUser));
      } catch {
        localStorage.clear();
      }
    }
    setIsLoading(false);
  }, []);

  const isAdmin = user?.role === 1 || user?.role === 'Admin';

  const login = async (email: string, password: string) => {
    const res = await api.post('/auth/login', { email, password });
    if (res.success && res.data) {
      const accessToken = res.data.accessToken;
      const refreshToken = res.data.refreshToken;
      const userData = res.data.user;

      localStorage.setItem('vc_access_token', accessToken);
      localStorage.setItem('vc_refresh_token', refreshToken);
      localStorage.setItem('vc_user', JSON.stringify(userData));

      setToken(accessToken);
      setUser(userData);
      return { success: true, message: res.message || 'Login successful' };
    }
    return { success: false, message: res.message || 'Login failed' };
  };

  const register = async (data: {
    email: string;
    password: string;
    firstName: string;
    lastName: string;
    displayName?: string;
  }) => {
    const res = await api.post('/auth/register', data);
    if (res.success) {
      return { success: true, message: res.message || 'Registration successful. You can now log in.' };
    }
    return { success: false, message: res.message || 'Registration failed' };
  };

  const logout = async () => {
    try {
      await api.post('/auth/logout');
    } catch {
      // Ignore network errors on logout
    }
    localStorage.removeItem('vc_access_token');
    localStorage.removeItem('vc_refresh_token');
    localStorage.removeItem('vc_user');
    setToken(null);
    setUser(null);
  };

  const refreshUser = async () => {
    if (!token) return;
    const res = await api.get('/auth/me');
    if (res.success && res.data) {
      setUser(res.data);
      localStorage.setItem('vc_user', JSON.stringify(res.data));
    }
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        token,
        isLoading,
        isAdmin,
        login,
        register,
        logout,
        refreshUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
