import { useState } from 'react';
import { urlAPI } from '../api/urls';

export default function UrlSubmissionForm({ onSubmitSuccess }) {
  const [formData, setFormData] = useState({
    url: '',
    nickname: '',
    passkey: '',
  });
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);
  const [success, setSuccess] = useState('');

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
  };

  const validateUrl = (url) => {
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setSuccess('');
    setLoading(true);

    // Client-side validation
    if (!validateUrl(formData.url)) {
      setError('Please enter a valid URL');
      setLoading(false);
      return;
    }

    if (formData.nickname.length < 1 || formData.nickname.length > 50) {
      setError('Nickname must be between 1 and 50 characters');
      setLoading(false);
      return;
    }

    if (!formData.passkey) {
      setError('Passkey is required');
      setLoading(false);
      return;
    }

    try {
      const response = await urlAPI.submitUrl(formData);
      setSuccess('URL submitted successfully!');
      setFormData({ url: '', nickname: '', passkey: '' });
      
      if (onSubmitSuccess) {
        onSubmitSuccess(response.data);
      }
    } catch (err) {
      if (err.response?.status === 429) {
        setError('Rate limit exceeded. Please wait 5 minutes between submissions.');
      } else {
        setError(err.response?.data?.error || 'Failed to submit URL');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container">
      <h2>Submit URL</h2>
      <form onSubmit={handleSubmit}>
        <input
          type="text"
          name="url"
          value={formData.url}
          onChange={handleChange}
          placeholder="https://example.com"
          required
        />

        <input
          type="text"
          name="nickname"
          value={formData.nickname}
          onChange={handleChange}
          placeholder="Enter a title"
          maxLength={50}
          required
        />

        <input
          type="password"
          name="passkey"
          value={formData.passkey}
          onChange={handleChange}
          placeholder="Enter a passkey"
          required
        />

        {error && <div className="error">{error}</div>}
        {success && <div className="success">{success}</div>}

        <button type="submit" disabled={loading}>
          {loading ? 'Submitting...' : 'Submit URL'}
        </button>
      </form>
    </div>
  );
}
