import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { drugRiskApi } from '../services/api';
import { DrugRiskResponse } from '../types';
import './Results.css';

const Results: React.FC = () => {
  const { assessmentId } = useParams<{ assessmentId: string }>();
  const [results, setResults] = useState<DrugRiskResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string>('');

  useEffect(() => {
    if (assessmentId) {
      loadResults();
    }
  }, [assessmentId]);

  const loadResults = async () => {
    try {
      setLoading(true);
      const response = await drugRiskApi.getUserAssessment(parseInt(assessmentId!));
      setResults(response);
    } catch (err: any) {
      console.error('Error loading results:', err);
      setError('Failed to load assessment results. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const getRiskLevelColor = (level: string) => {
    switch (level.toUpperCase()) {
      case 'HIGH':
        return '#dc3545';
      case 'MODERATE':
        return '#ffc107';
      case 'LOW':
        return '#28a745';
      default:
        return '#6c757d';
    }
  };

  const getRiskLevelIcon = (level: string) => {
    switch (level.toUpperCase()) {
      case 'HIGH':
        return '🔴';
      case 'MODERATE':
        return '🟡';
      case 'LOW':
        return '🟢';
      default:
        return '⚪';
    }
  };

  if (loading) {
    return (
      <div className="results">
        <div className="loading-container">
          <div className="loading">Loading assessment results...</div>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="results">
        <div className="error-container">
          <div className="error-message">{error}</div>
          <Link to="/assessment" className="btn btn-primary">
            🔬 Make New Assessment
          </Link>
        </div>
      </div>
    );
  }

  if (!results) {
    return (
      <div className="results">
        <div className="error-container">
          <div className="error-message">No results found for this assessment.</div>
          <Link to="/assessment" className="btn btn-primary">
            🔬 Make New Assessment
          </Link>
        </div>
      </div>
    );
  }

  return (
    <div className="results">
      <div className="results-header">
        <h1>📊 Assessment Results</h1>
        <p>Your personalized drug risk analysis for {results.drugName}</p>
      </div>

      <div className="results-container">
        {/* Main Results Card */}
        <div className="main-results">
          <div className="result-card primary">
            <div className="result-header">
              <h2>🧬 Risk Assessment Summary</h2>
              <div className="risk-badge" style={{ backgroundColor: getRiskLevelColor(results.riskLevel) }}>
                {getRiskLevelIcon(results.riskLevel)} {results.riskLevel}
              </div>
            </div>
            
            <div className="results-grid">
              <div className="result-item">
                <div className="result-label">Drug Name</div>
                <div className="result-value">{results.drugName}</div>
              </div>
              <div className="result-item">
                <div className="result-label">Risk Score</div>
                <div className="result-value">{results.riskScore.toFixed(2)}</div>
              </div>

              <div className="result-item">
                <div className="result-label">Assessment Date</div>
                <div className="result-value">{new Date(results.createdAt).toLocaleDateString()}</div>
              </div>
            </div>

            <div className="explanation">
              <h3>📋 Clinical Explanation</h3>
              <p>{results.explanation}</p>
            </div>
          </div>
        </div>



        {/* Tableau Dashboard Integration */}
        {(results.tableauDashboardUrl || results.tableauUserSpecificUrl) && (
          <div className="analytics-section">
            <h2>📊 Personalized Analytics</h2>
            <p>Compare your results with community data and view detailed analytics</p>
            
            <div className="dashboard-tabs">
              <div className="dashboard-tab">
                <h3>🌐 Community Comparison</h3>
                <div className="dashboard-container">
                  <iframe
                    src={results.tableauDashboardUrl || 'http://localhost:8080/test_dashboard.html'}
                    className="tableau-iframe"
                    title="Community Analytics Dashboard"
                    frameBorder="0"
                  />
                </div>
              </div>

              {results.tableauUserSpecificUrl && (
                <div className="dashboard-tab">
                  <h3>👤 Your Personalized View</h3>
                  <div className="dashboard-container">
                    <iframe
                      src={results.tableauUserSpecificUrl}
                      className="tableau-iframe"
                      title="User-Specific Analytics Dashboard"
                      frameBorder="0"
                    />
                  </div>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Action Buttons */}
        <div className="action-buttons">
          <Link to="/assessment" className="btn btn-primary">
            🔬 New Assessment
          </Link>
          <Link 
            to="/analytics" 
            state={{ 
              riskScore: results.riskScore,
              riskLevel: results.riskLevel,
              drugName: results.drugName
            }}
            className="btn btn-secondary"
          >
            📊 View Analytics
          </Link>
          <button onClick={() => window.print()} className="btn btn-outline">
            🖨️ Print Results
          </button>
        </div>
      </div>
    </div>
  );
};

export default Results; 