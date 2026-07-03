import axios from 'axios';

const configuredBaseUrl = import.meta.env.VITE_API_BASE_URL;
const API_BASE_URL = configuredBaseUrl === undefined ? 'http://localhost:5000' : configuredBaseUrl;

export const api = axios.create({
  baseURL: `${API_BASE_URL}/api`,
});

api.interceptors.request.use((config) => {
  const token = localStorage.getItem('shortie.accessToken');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export const setTokens = ({ accessToken, refreshToken }) => {
  localStorage.setItem('shortie.accessToken', accessToken);
  localStorage.setItem('shortie.refreshToken', refreshToken);
};

export const clearTokens = () => {
  localStorage.removeItem('shortie.accessToken');
  localStorage.removeItem('shortie.refreshToken');
};
