import axios from 'axios';

// YARP Gateway is running on Port 5000
const apiClient = axios.create({
  baseURL: `${process.env.NEXT_PUBLIC_API_URL ?? 'http://localhost:5000'}/api`,
  headers: {
    'Content-Type': 'application/json',
  },
});

export default apiClient;
