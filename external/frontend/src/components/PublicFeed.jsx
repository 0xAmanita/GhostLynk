import { useState, useEffect } from 'react';
import { urlAPI } from '../api/urls';

export default function PublicFeed() {
  const [entries, setEntries] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState('');
  const [currentPage, setCurrentPage] = useState(1);
  const [totalPages, setTotalPages] = useState(1);
  const [totalCount, setTotalCount] = useState(0);
  const pageSize = 20;

  const fetchFeed = async (page = 1) => {
    setLoading(true);
    setError('');

    try {
      const response = await urlAPI.getFeed(page, pageSize);
      setEntries(response.data.entries);
      setCurrentPage(response.data.currentPage);
      setTotalPages(response.data.totalPages);
      setTotalCount(response.data.totalCount);
    } catch (err) {
      setError(err.response?.data?.error || 'Failed to load feed');
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchFeed(currentPage);
  }, [currentPage]);

  const handlePageChange = (newPage) => {
    if (newPage >= 1 && newPage <= totalPages) {
      setCurrentPage(newPage);
    }
  };

  const formatDate = (dateString) => {
    const date = new Date(dateString);
    return date.toLocaleString();
  };

  if (loading && entries.length === 0) {
    return <div className="loading">Loading feed...</div>;
  }

  return (
    <div className="public-feed">
      <h2>Public Feed</h2>
      <p className="feed-info">Total entries: {totalCount}</p>

      {error && <div className="error-message">{error}</div>}

      {entries.length === 0 ? (
        <p className="no-entries">No entries yet. Be the first to submit a URL!</p>
      ) : (
        <>
          <div className="feed-entries">
            {entries.map((entry, index) => (
              <div key={index} className="feed-entry">
                <div className="entry-nickname">
                  <strong>{entry.nickname}</strong>
                </div>
                <div className="entry-obfuscated">
                  <code>{entry.obfuscatedUrl}</code>
                </div>
                <div className="entry-timestamp">
                  {formatDate(entry.createdAt)}
                </div>
              </div>
            ))}
          </div>

          {totalPages > 1 && (
            <div className="pagination">
              <button
                onClick={() => handlePageChange(currentPage - 1)}
                disabled={currentPage === 1}
              >
                Previous
              </button>
              <span className="page-info">
                Page {currentPage} of {totalPages}
              </span>
              <button
                onClick={() => handlePageChange(currentPage + 1)}
                disabled={currentPage === totalPages}
              >
                Next
              </button>
            </div>
          )}
        </>
      )}
    </div>
  );
}
