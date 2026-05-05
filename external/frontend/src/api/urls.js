import apiClient from './client';

export const urlAPI = {
  submitUrl: (data) => apiClient.post('/urls/submit', data),
  getFeed: (page = 1, pageSize = 20) => apiClient.get('/urls/feed', { params: { page, pageSize } }),
};
