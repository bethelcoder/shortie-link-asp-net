import { useEffect, useMemo, useState } from 'react';
import { api, clearTokens, setTokens } from './api';
import './App.css';

const INITIAL_FORM = {
  originalUrl: '',
  customAlias: '',
  expiresAtUtc: '',
};

const formatError = (err, defaultMsg) => {
  const responseData = err.response?.data;
  if (!responseData) return defaultMsg;
  if (responseData.errors) {
    if (Array.isArray(responseData.errors)) {
      return responseData.errors.join(' ');
    }
    if (typeof responseData.errors === 'object') {
      return Object.values(responseData.errors).flat().join(' ');
    }
  }
  return responseData.message || responseData.title || defaultMsg;
};

function App() {
  const [mode, setMode] = useState('login');
  const [authForm, setAuthForm] = useState({ fullName: '', email: '', password: '' });
  const [token, setToken] = useState(localStorage.getItem('shortie.accessToken') || '');
  const [urls, setUrls] = useState([]);
  const [urlForm, setUrlForm] = useState(INITIAL_FORM);
  const [analytics, setAnalytics] = useState(null);
  const [error, setError] = useState('');
  const [message, setMessage] = useState('');
  const [qrModalUrl, setQrModalUrl] = useState(null);
  const [qrCodeName, setQrCodeName] = useState('qrcode.png');

  const isAuthenticated = useMemo(() => Boolean(token), [token]);

  const loadUrls = async () => {
    try {
      const { data } = await api.get('/urls');
      setUrls(data);
    } catch (err) {
      setError(formatError(err, 'Failed to load URLs'));
    }
  };

  useEffect(() => {
    if (isAuthenticated) {
      api.get('/urls')
        .then(({ data }) => setUrls(data))
        .catch((err) => setError(formatError(err, 'Failed to load URLs')));
    }
  }, [isAuthenticated]);

  const onAuthSubmit = async (e) => {
    e.preventDefault();
    setError('');
    setMessage('');

    const endpoint = mode === 'login' ? '/auth/login' : '/auth/register';
    const payload =
      mode === 'login'
        ? { email: authForm.email, password: authForm.password }
        : { fullName: authForm.fullName, email: authForm.email, password: authForm.password };

    try {
      const { data } = await api.post(endpoint, payload);
      setTokens(data);
      setToken(data.accessToken);
      setMessage(mode === 'login' ? 'Logged in successfully.' : 'Registered and logged in successfully.');
      setAuthForm({ fullName: '', email: '', password: '' });
    } catch (err) {
      setError(formatError(err, 'Authentication failed'));
    }
  };

  const onCreateUrl = async (e) => {
    e.preventDefault();
    setError('');
    setMessage('');

    try {
      const payload = {
        originalUrl: urlForm.originalUrl,
        customAlias: urlForm.customAlias || null,
        expiresAtUtc: urlForm.expiresAtUtc ? new Date(urlForm.expiresAtUtc).toISOString() : null,
      };

      await api.post('/urls', payload);
      setUrlForm(INITIAL_FORM);
      setMessage('Short URL created.');
      await loadUrls();
    } catch (err) {
      setError(formatError(err, 'Failed to create short URL'));
    }
  };

  const onDelete = async (id) => {
    try {
      await api.delete(`/urls/${id}`);
      setUrls((previous) => previous.filter((url) => url.id !== id));
      setAnalytics(null);
    } catch (err) {
      setError(formatError(err, 'Failed to delete URL'));
    }
  };

  const onLoadAnalytics = async (id) => {
    try {
      const { data } = await api.get(`/urls/${id}/analytics`);
      setAnalytics(data);
    } catch (err) {
      setError(formatError(err, 'Failed to load analytics'));
    }
  };

  const onShare = async (shortLink) => {
    setError('');
    setMessage('');
    const shareLink = `${shortLink}?src=share`;
    try {
      if (navigator.share) {
        await navigator.share({
          title: 'Shortie Link',
          text: 'Check out this short link!',
          url: shareLink,
        });
        setMessage('Shared successfully.');
      } else {
        await navigator.clipboard.writeText(shareLink);
        setMessage('Share link copied to clipboard.');
      }
    } catch (err) {
      if (err.name !== 'AbortError') {
        setError('Failed to share link.');
      }
    }
  };

  const onShowQr = async (url) => {
    setError('');
    setMessage('');
    try {
      const response = await api.get(`/urls/${url.id}/qrcode`, { responseType: 'blob' });
      const blobUrl = URL.createObjectURL(response.data);
      setQrModalUrl(blobUrl);
      setQrCodeName(`${url.customAlias || url.shortCode}-qrcode.png`);
    } catch (err) {
      setError(formatError(err, 'Failed to generate QR Code'));
    }
  };

  const onCloseQrModal = () => {
    if (qrModalUrl) {
      URL.revokeObjectURL(qrModalUrl);
    }
    setQrModalUrl(null);
  };

  const onLogout = () => {
    clearTokens();
    setToken('');
    setUrls([]);
    setAnalytics(null);
    if (qrModalUrl) {
      URL.revokeObjectURL(qrModalUrl);
      setQrModalUrl(null);
    }
    setMessage('Logged out.');
  };

  return (
    <div className="page">
      <header className="hero">
        <p className="tag">Graduate .NET Portfolio Project</p>
        <h1>Shortie Fullstack</h1>
        <p className="subtitle">ASP.NET Core 9 + React URL shortener with analytics, JWT auth, and QR codes.</p>
      </header>

      {!isAuthenticated ? (
        <section className="panel">
          <div className="switcher">
            <button className={mode === 'login' ? 'active' : ''} onClick={() => setMode('login')}>Login</button>
            <button className={mode === 'register' ? 'active' : ''} onClick={() => setMode('register')}>Register</button>
          </div>

          <form onSubmit={onAuthSubmit} className="form-grid">
            {mode === 'register' && (
              <label>
                Full Name
                <input
                  value={authForm.fullName}
                  onChange={(e) => setAuthForm((p) => ({ ...p, fullName: e.target.value }))}
                  required
                />
              </label>
            )}

            <label>
              Email
              <input
                type="email"
                value={authForm.email}
                onChange={(e) => setAuthForm((p) => ({ ...p, email: e.target.value }))}
                required
              />
            </label>

            <label>
              Password
              <input
                type="password"
                value={authForm.password}
                onChange={(e) => setAuthForm((p) => ({ ...p, password: e.target.value }))}
                required
              />
            </label>

            <button type="submit" className="primary">{mode === 'login' ? 'Login' : 'Create Account'}</button>
          </form>
        </section>
      ) : (
        <>
          <section className="panel">
            <div className="panel-head">
              <h2>Create Short URL</h2>
              <button className="ghost" onClick={onLogout}>Logout</button>
            </div>

            <form onSubmit={onCreateUrl} className="form-grid">
              <label>
                Original URL
                <input
                  type="url"
                  value={urlForm.originalUrl}
                  onChange={(e) => setUrlForm((p) => ({ ...p, originalUrl: e.target.value }))}
                  placeholder="https://example.com"
                  required
                />
              </label>

              <label>
                Custom Alias (optional)
                <input
                  value={urlForm.customAlias}
                  onChange={(e) => setUrlForm((p) => ({ ...p, customAlias: e.target.value }))}
                  placeholder="my-job-portfolio"
                />
              </label>

              <label>
                Expiration (optional)
                <input
                  type="datetime-local"
                  value={urlForm.expiresAtUtc}
                  onChange={(e) => setUrlForm((p) => ({ ...p, expiresAtUtc: e.target.value }))}
                />
              </label>

              <button type="submit" className="primary">Create</button>
            </form>
          </section>

          <section className="panel">
            <h2>Your URLs</h2>
            {urls.length === 0 ? <p>No URLs created yet.</p> : null}
            <div className="cards">
              {urls.map((url) => (
                <article key={url.id} className="card">
                  <div className="link-row">
                    <a href={url.shortLink} target="_blank" rel="noopener noreferrer" className="mono short-link">
                      {url.shortLink}
                      <svg className="icon-external" viewBox="0 0 24 24" width="14" height="14" style={{ marginLeft: '4px', verticalAlign: 'middle', display: 'inline-block' }}>
                        <path fill="currentColor" d="M14,3V5H17.59L7.76,14.83L9.17,16.24L19,6.41V10H21V3M19,19H5V5H12V3H5C3.89,3 3,3.9 3,5V19A2,2 0 0,0 5,21H19A2,2 0 0,0 21,19V12H19V19Z"/>
                      </svg>
                    </a>
                  </div>
                  <p className="original-link">{url.originalUrl}</p>
                  <p className="clicks-info">Clicks: {url.clickCount}</p>
                  <div className="actions">
                    <button onClick={() => onLoadAnalytics(url.id)}>Analytics</button>
                    <button onClick={() => onShowQr(url)}>QR</button>
                    <button onClick={() => onShare(url.shortLink)}>Share</button>
                    <button className="danger" onClick={() => onDelete(url.id)}>Delete</button>
                  </div>
                </article>
              ))}
            </div>
          </section>

          {analytics ? (
            <section className="panel">
              <h2>Analytics Snapshot</h2>
              <p>Total Clicks: {analytics.clickCount}</p>
              <p>Last Accessed: {analytics.lastAccessedAtUtc || 'N/A'}</p>
              <pre>{JSON.stringify(analytics, null, 2)}</pre>
            </section>
          ) : null}
        </>
      )}

      {error ? <p className="status error">{error}</p> : null}
      {message ? <p className="status success">{message}</p> : null}

      {qrModalUrl && (
        <div className="modal-backdrop" onClick={onCloseQrModal}>
          <div className="modal-content" onClick={(e) => e.stopPropagation()}>
            <h2>QR Code</h2>
            <p className="subtitle">Scan to redirect or download the image below.</p>
            <div className="qr-image-wrapper">
              <img src={qrModalUrl} alt="QR Code" className="qr-image" />
            </div>
            <div className="modal-actions">
              <a href={qrModalUrl} download={qrCodeName} className="primary-btn">Download</a>
              <button className="ghost" onClick={onCloseQrModal}>Close</button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}

export default App;
