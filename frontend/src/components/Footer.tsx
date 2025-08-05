import React from 'react';
import { Github, Linkedin } from 'lucide-react';
import './Footer.css';

const Footer: React.FC = () => {
  return (
    <footer className="footer">
      <div className="footer-container">
        <div className="footer-content">
          
          <div className="footer-left">
            <div className="footer-brand">
              A JE Production
            </div>
          </div>

          <div className="footer-right">
            <div className="social-links">
              <div className="social-link">
                <Github className="social-icon" />
                <a
                  href="https://github.com/JoshElkind"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="social-text"
                >
                  GitHub
                </a>
              </div>

              <div className="social-link">
                <Linkedin className="social-icon" />
                <a
                  href="https://www.linkedin.com/in/joshua-elkind-565014345/"
                  target="_blank"
                  rel="noopener noreferrer"
                  className="social-text"
                >
                  LinkedIn
                </a>
              </div>
            </div>
          </div>
        </div>
      </div>
    </footer>
  );
};

export default Footer; 