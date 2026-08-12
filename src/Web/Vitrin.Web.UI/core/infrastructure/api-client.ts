import axios from 'axios';
import { getApiUrl } from '@/lib/api-url';

// YARP Gateway is running on Port 5000
const apiClient = axios.create({
  baseURL: `${getApiUrl() || 'http://localhost:5000'}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
});

export default apiClient;
