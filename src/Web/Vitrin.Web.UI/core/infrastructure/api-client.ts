import axios from 'axios';

// Production'da NEXT_PUBLIC_API_URL boş string olarak gelir, bu durumda relative path kullanılmalı
const getApiUrl = () => {
  const env = process.env.NEXT_PUBLIC_API_URL;
  return (!env || env === '') ? '' : env;
};

// YARP Gateway is running on Port 5000
const apiClient = axios.create({
  baseURL: `${getApiUrl() || 'http://localhost:5000'}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
});

export default apiClient;
