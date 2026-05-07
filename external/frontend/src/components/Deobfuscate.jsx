import { useState } from 'react';
import { urlAPI } from '../api/urls';

export default function Deobfuscate() {
  const [formData, setFormData] = useState({
    obfuscatedText: '',
    nickname: '',
    passkey: ''
  });
  const [result, setResult] = useState(null);
  const [error, setError] = useState('');
  const [loading, setLoading] = useState(false);

  const handleChange = (e) => {
    setFormData({ ...formData, [e.target.name]: e.target.value });
    setError('');
    setResult(null);
  };

  const handleSubmit = async (e) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    setResult(null);

    try {
      const response = await urlAPI.deobfuscate(formData);
      setResult(response.data);
      setFormData({ obfuscatedText: '', nickname: '', passkey: '' });
    } catch (err) {
      if (err.response?.status === 429) {
        setError('Rate limit exceeded. Please wait 5 minutes between attempts.');
      } else if (err.response?.status === 423) {
        setError('Entry locked due to too many failed attempts.');
      } else if (err.response?.status === 401) {
        setError('Invalid credentials.');
      } else {
        setError('An error occurred. Please try again.');
      }
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="container">
      <h2>Deobfuscate</h2>
      
      <form onSubmit={handleSubmit}>
        <textarea
          name="obfuscatedText"
          value={formData.obfuscatedText}
          onChange={handleChange}
          placeholder="Paste obfuscated text here"
          required
          rows="4"
        />

        <input
          type="text"
          name="nickname"
          value={formData.nickname}
          onChange={handleChange}
          placeholder="Nickname"
          required
          maxLength="50"
        />

        <input
          type="password"
          name="passkey"
          value={formData.passkey}
          onChange={handleChange}
          placeholder="Passkey"
          required
        />

        {error && <div className="error">{error}</div>}

        {result && (
          <div className="success">
            <h3>Success!</h3>
            <p><strong>Original URL:</strong> <a href={result.originalUrl} target="_blank" rel="noopener noreferrer">{result.originalUrl}</a></p>
            <p><strong>Nickname:</strong> {result.nickname}</p>
            <p><strong>Created:</strong> {new Date(result.createdAt).toLocaleString()}</p>
          </div>
        )}

        <button type="submit" disabled={loading}>
          {loading ? 'Processing...' : 'Deobfuscate'}
        </button>
      </form>
    </div>
  );
}
