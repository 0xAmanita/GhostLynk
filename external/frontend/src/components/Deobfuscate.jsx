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
    <div style={{ maxWidth: '600px', margin: '2rem auto', padding: '0 1rem' }}>
      <h2>Deobfuscate</h2>
      
      <form onSubmit={handleSubmit} style={{ display: 'flex', flexDirection: 'column', gap: '1rem' }}>
        <div>
          <label htmlFor="obfuscatedText">Obfuscated Text</label>
          <textarea
            id="obfuscatedText"
            name="obfuscatedText"
            value={formData.obfuscatedText}
            onChange={handleChange}
            required
            rows="4"
            style={{ width: '100%', padding: '0.5rem' }}
          />
        </div>

        <div>
          <label htmlFor="nickname">Nickname</label>
          <input
            type="text"
            id="nickname"
            name="nickname"
            value={formData.nickname}
            onChange={handleChange}
            required
            maxLength="50"
            style={{ width: '100%', padding: '0.5rem' }}
          />
        </div>

        <div>
          <label htmlFor="passkey">Passkey</label>
          <input
            type="password"
            id="passkey"
            name="passkey"
            value={formData.passkey}
            onChange={handleChange}
            required
            style={{ width: '100%', padding: '0.5rem' }}
          />
        </div>

        <button type="submit" disabled={loading} style={{ padding: '0.75rem', cursor: 'pointer' }}>
          {loading ? 'Processing...' : 'Deobfuscate'}
        </button>
      </form>

      {error && (
        <div style={{ marginTop: '1rem', padding: '1rem', backgroundColor: '#fee', border: '1px solid #fcc', borderRadius: '4px' }}>
          {error}
        </div>
      )}

      {result && (
        <div style={{ marginTop: '1rem', padding: '1rem', backgroundColor: '#efe', border: '1px solid #cfc', borderRadius: '4px' }}>
          <h3>Success!</h3>
          <p><strong>Original URL:</strong> <a href={result.originalUrl} target="_blank" rel="noopener noreferrer">{result.originalUrl}</a></p>
          <p><strong>Nickname:</strong> {result.nickname}</p>
          <p><strong>Created:</strong> {new Date(result.createdAt).toLocaleString()}</p>
        </div>
      )}
    </div>
  );
}
