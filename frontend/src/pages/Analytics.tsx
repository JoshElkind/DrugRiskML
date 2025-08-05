import React, { useState, useEffect, useRef } from 'react';
import { useNavigate, useLocation } from 'react-router-dom';
import './Analytics.css';

const Analytics: React.FC = () => {
  const navigate = useNavigate();
  const location = useLocation();
  const userData = location.state || { riskScore: 0, riskLevel: 'N/A', drugName: 'N/A' };
  const vizRef = useRef<HTMLDivElement>(null);
  
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');
  const [scriptLoaded, setScriptLoaded] = useState(false);
  const [debugInfo, setDebugInfo] = useState<any>(null);

  // Load Tableau Embedding API script
  useEffect(() => {
    const loadTableauScript = () => {
      // Check if script is already loaded
      if (document.querySelector('script[src*="tableau.embedding"]')) {
        console.log('Tableau script already loaded');
        setScriptLoaded(true);
        return;
      }

      console.log('Loading Tableau Embedding API script...');
      const script = document.createElement('script');
      script.type = 'module';
      script.src = 'https://prod-ca-a.online.tableau.com/javascripts/api/tableau.embedding.3.latest.min.js';
      script.onload = () => {
        console.log('Tableau Embedding API script loaded successfully');
        setScriptLoaded(true);
      };
      script.onerror = () => {
        console.error('Failed to load Tableau Embedding API');
        setError('Failed to load Tableau Embedding API. Please check your internet connection.');
        setLoading(false);
      };
      
      document.head.appendChild(script);
    };

    loadTableauScript();

    // Add timeout to prevent infinite loading
    const timeout = setTimeout(() => {
      if (loading) {
        console.warn('Tableau dashboard loading timeout - forcing completion');
        setLoading(false);
        setError('Dashboard loading timed out. This may be due to authentication issues or network problems.');
      }
    }, 30000); // 30 second timeout

    return () => clearTimeout(timeout);
  }, [loading]);

  // Load dashboard URL from backend and create tableau-viz element
  useEffect(() => {
    if (!scriptLoaded) return;

    const loadDashboardAndCreateViz = async () => {
      try {
        setLoading(true);
        setError('');
        setDebugInfo(null);
        
        console.log('Loading dashboard URL from backend...');
        
        // Call backend API to get authenticated Tableau URL
        const response = await fetch(`http://localhost:8000/api/tableau/analytics/user-specific?drugName=${encodeURIComponent(userData.drugName)}&userRiskScore=${userData.riskScore}&userRiskLevel=${encodeURIComponent(userData.riskLevel)}`, {
          method: 'GET',
          headers: {
            'Content-Type': 'application/json',
          },
        });
        
        if (!response.ok) {
          const errorText = await response.text();
          console.error('Backend error:', response.status, errorText);
          throw new Error(`Backend error: ${response.status} - ${errorText}`);
        }
        
        const data = await response.json();
        console.log('Backend response:', data);
        setDebugInfo(data);
        
        if (!data.url) {
          throw new Error('No URL returned from backend');
        }

        const vizElement = vizRef.current;
        if (!vizElement) return;

        // Wait for the custom element to be defined
        const createTableauViz = () => {
          console.log('Creating tableau-viz element...');
          
          // Clear any existing content
          vizElement.innerHTML = '';

          // Create tableau-viz element with the authenticated URL from backend
          const tableauViz = document.createElement('tableau-viz');
          tableauViz.setAttribute('id', 'tableau-viz');
          
          console.log('Dashboard URL from backend:', data.url);
          tableauViz.setAttribute('src', data.url);
          tableauViz.setAttribute('width', '1366');
          tableauViz.setAttribute('height', '840');
          tableauViz.setAttribute('hide-tabs', '');
          tableauViz.setAttribute('toolbar', 'bottom');

          const handleVizLoad = (event: any) => {
            console.log('Tableau viz loaded successfully', event);
            setLoading(false);
            setError(''); // Clear any previous errors
          };

          const handleVizError = (event: any) => {
            console.error('Tableau viz error:', event);
            const errorMessage = event.detail?.message || 'Unknown error occurred';
            setError(`Failed to load Tableau dashboard: ${errorMessage}. This may be due to authentication issues or the dashboard requiring additional login.`);
            setLoading(false);
          };

          console.log('Adding event listeners to tableau-viz element');

          // Add event listeners
          tableauViz.addEventListener('firstinteractive', handleVizLoad);
          tableauViz.addEventListener('vizloadingerror', handleVizError);

          // Append the tableau-viz element
          console.log('Appending tableau-viz element to DOM');
          vizElement.appendChild(tableauViz);
          console.log('tableau-viz element appended to DOM');

          return tableauViz;
        };

        // Check if the custom element is already defined
        if (customElements.get('tableau-viz')) {
          console.log('tableau-viz custom element already defined, creating viz');
          createTableauViz();
        } else {
          console.log('Waiting for tableau-viz custom element to be defined...');
          // Wait for the custom element to be defined
          customElements.whenDefined('tableau-viz').then(() => {
            console.log('tableau-viz custom element defined, creating viz');
            createTableauViz();
          }).catch((error) => {
            console.error('Failed to define tableau-viz element:', error);
            setError('Failed to initialize Tableau dashboard component. Please check if the Tableau embedding script loaded correctly.');
            setLoading(false);
          });
        }

        // Cleanup function
        return () => {
          const existingViz = vizElement.querySelector('tableau-viz');
          if (existingViz) {
            existingViz.remove();
          }
        };
      } catch (error) {
        console.error('Error loading dashboard from backend:', error);
        setError(`Failed to load dashboard URL from backend: ${error instanceof Error ? error.message : 'Unknown error'}. Please check that the backend is running and properly configured.`);
        setLoading(false);
      }
    };

    loadDashboardAndCreateViz();
  }, [scriptLoaded, userData.drugName, userData.riskScore, userData.riskLevel]);

  const openInNewTab = async () => {
    try {
      const response = await fetch(`http://localhost:8000/api/tableau/analytics/user-specific?drugName=${encodeURIComponent(userData.drugName)}&userRiskScore=${userData.riskScore}&userRiskLevel=${encodeURIComponent(userData.riskLevel)}`, {
        method: 'GET',
        headers: {
          'Content-Type': 'application/json',
        },
      });
      
      if (response.ok) {
        const data = await response.json();
        if (data.url) {
          window.open(data.url, '_blank');
        } else {
          // Fallback to base URL
          const baseUrl = 'https://prod-ca-a.online.tableau.com/t/jdelkind-f1b10c20eb/views/DrugRiskAnalytics/Dashboard';
          const url = userData.drugName && userData.drugName !== 'N/A' 
            ? `${baseUrl}?vf_drugName=${encodeURIComponent(userData.drugName)}&vf_UserRiskScore=${(userData.riskScore * 100).toFixed(1)}&vf_UserRiskLevel=${encodeURIComponent(userData.riskLevel)}`
            : baseUrl;
          window.open(url, '_blank');
        }
      } else {
        // Fallback to base URL
        const baseUrl = 'https://prod-ca-a.online.tableau.com/t/jdelkind-f1b10c20eb/views/DrugRiskAnalytics/Dashboard';
        const url = userData.drugName && userData.drugName !== 'N/A' 
          ? `${baseUrl}?vf_drugName=${encodeURIComponent(userData.drugName)}&vf_UserRiskScore=${(userData.riskScore * 100).toFixed(1)}&vf_UserRiskLevel=${encodeURIComponent(userData.riskLevel)}`
          : baseUrl;
        window.open(url, '_blank');
      }
    } catch (error) {
      console.error('Error getting authenticated URL:', error);
      // Fallback to base URL
      const baseUrl = 'https://prod-ca-a.online.tableau.com/t/jdelkind-f1b10c20eb/views/DrugRiskAnalytics/Dashboard';
      const url = userData.drugName && userData.drugName !== 'N/A' 
        ? `${baseUrl}?vf_drugName=${encodeURIComponent(userData.drugName)}&vf_UserRiskScore=${(userData.riskScore * 100).toFixed(1)}&vf_UserRiskLevel=${encodeURIComponent(userData.riskLevel)}`
        : baseUrl;
      window.open(url, '_blank');
    }
  };

  const retryLoading = () => {
    setLoading(true);
    setError('');
    setDebugInfo(null);
    // Force reload by triggering the useEffect
    const vizElement = vizRef.current;
    if (vizElement) {
      vizElement.innerHTML = '';
    }
  };

  return (
    <div className="analytics">
      <div className="analytics-header">
        <button className="back-btn" onClick={() => navigate('/')}>
          <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
            <path d="M15 18L9 12L15 6" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
          </svg>
        </button>
        <span className="header-text">Analytics</span>
        <div className="header-separator"></div>
        <div className="user-stats">
          <div className="stat-item">
            <span className="stat-label">Drug</span>
            <span className="stat-value">{userData.drugName}</span>
          </div>
          <div className="stat-item">
            <span className="stat-label">Risk</span>
            <span className="stat-value">{(userData.riskScore * 100).toFixed(1)}%</span>
          </div>
          <div className="stat-item">
            <span className="stat-label">Level</span>
            <span className={`stat-value level-${userData.riskLevel.toLowerCase()}`}>
              {userData.riskLevel}
            </span>
          </div>
        </div>
      </div>

      {error && (
        <div className="error-message">
          <div className="error-icon">⚠️</div>
          <div className="error-content">
            <h4>Analytics Dashboard Error</h4>
            <p>{error}</p>
            <div className="error-actions">
              <button onClick={retryLoading} className="retry-btn">
                Retry Loading
              </button>
              <button onClick={openInNewTab} className="open-external-btn">
                Open Dashboard in New Tab
              </button>
            </div>
            {debugInfo && (
              <details className="debug-info">
                <summary>Debug Information</summary>
                <pre>{JSON.stringify(debugInfo, null, 2)}</pre>
              </details>
            )}
          </div>
        </div>
      )}

      <div className="dashboard-container">
        {loading ? (
          <div className="loading">
            <div className="loading-spinner"></div>
            <p>Loading analytics dashboard...</p>
            <p className="loading-subtitle">This may take a moment while we authenticate with Tableau and Snowflake</p>
          </div>
        ) : scriptLoaded ? (
          <div
            ref={vizRef}
            id="tableau-viz-container"
            style={{ width: '100%', height: '840px' }}
          />
        ) : (
          <div className="error">
            <div className="error-icon">❌</div>
            <div className="error-content">
              <h4>Dashboard Unavailable</h4>
              <p>Failed to load analytics dashboard. Please try again later.</p>
              <div className="error-actions">
                <button onClick={retryLoading} className="retry-btn">
                  Retry Loading
                </button>
                <button onClick={openInNewTab} className="open-external-btn">
                  Open Dashboard in New Tab
                </button>
              </div>
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default Analytics; 