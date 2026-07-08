import axios from 'axios';

// YARP Gateway is running on Port 5000
const apiClient = axios.create({
  baseURL: 'http://localhost:5000/api',
  headers: {
    'Content-Type': 'application/json',
  },
});

export default apiClient;
