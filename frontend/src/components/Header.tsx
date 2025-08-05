import React from 'react';
import { useNavigate } from 'react-router-dom';
import './Header.css';

const Header: React.FC = () => {
  const navigate = useNavigate();

  const handleLogoClick = () => {
    navigate('/');
  };

  return (
    <header className="header">
      <div className="header-container">
        <div className="logo" onClick={handleLogoClick} style={{ cursor: 'pointer' }}>
          <img src="/drlogo.png" alt="Drug Risk Logo" className="logo-image" />
        </div>
        
        <div className="info-section">
          <div className="info-button">
            <div className="info-icon">
              <svg width="12" height="12" viewBox="0 0 16 16" fill="none" xmlns="http://www.w3.org/2000/svg">
                <circle cx="8" cy="8" r="7" stroke="white" strokeWidth="1.5" fill="none"/>
                <text x="8" y="11" textAnchor="middle" fill="white" fontSize="8" fontWeight="normal">i</text>
              </svg>
            </div>
            <div className="info-text">info</div>
          </div>
        </div>
      </div>
    </header>
  );
};

export default Header; 