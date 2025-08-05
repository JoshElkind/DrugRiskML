import React, { useState, useRef, useEffect } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import './Home.css';

const Home: React.FC = () => {
  const navigate = useNavigate();
  const [isDragOver, setIsDragOver] = useState(false);
  const [isDropdownOpen, setIsDropdownOpen] = useState(false);
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedDrug, setSelectedDrug] = useState<string>('');
  const [vcfFile, setVcfFile] = useState<File | null>(null);
  const [isProcessing, setIsProcessing] = useState(false);
  const [riskAssessment, setRiskAssessment] = useState<any>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);

  const handleFileSelect = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file && file.name.endsWith('.vcf')) {
      console.log('VCF file selected:', file.name);
      setVcfFile(file);
      // Clear the input to allow re-uploading the same file
      if (fileInputRef.current) {
        fileInputRef.current.value = '';
      }
    }
  };

  const handleDragOver = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(true);
  };

  const handleDragLeave = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
  };

  const handleDrop = (e: React.DragEvent) => {
    e.preventDefault();
    setIsDragOver(false);
    
    const files = Array.from(e.dataTransfer.files);
    const vcfFile = files.find(file => file.name.endsWith('.vcf'));
    
    if (vcfFile) {
      console.log('VCF file dropped:', vcfFile.name);
      setVcfFile(vcfFile);
    }
  };

  const handleClick = () => {
    fileInputRef.current?.click();
  };

  const handleRemoveFile = () => {
    setVcfFile(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
  };

  const handleReset = () => {
    // Clear selected drug
    setSelectedDrug('');
    // Clear uploaded file
    setVcfFile(null);
    // Clear risk assessment results
    setRiskAssessment(null);
    if (fileInputRef.current) {
      fileInputRef.current.value = '';
    }
    // Clear search term
    setSearchTerm('');
    // Close dropdown if open
    setIsDropdownOpen(false);
  };

  const handleDrugButtonClick = () => {
    setIsDropdownOpen(true);
  };

  const handleClosePopup = () => {
    setIsDropdownOpen(false);
    setSearchTerm(''); // Clear search term when popup closes
  };

  const handleOverlayClick = (e: React.MouseEvent) => {
    if (e.target === e.currentTarget) {
      setIsDropdownOpen(false);
      setSearchTerm(''); // Clear search term when clicking outside
    }
  };

  const handleDrugSelect = (drug: string) => {
    console.log('Selected drug:', drug);
    setSelectedDrug(drug);
    setIsDropdownOpen(false);
    setSearchTerm(''); // Clear search term when drug is selected
  };

  // Drug list with 4 basic drugs from README + 35 additional demo drugs (20 more added)
  const drugList = [
    'Warfarin',
    'Clopidogrel', 
    'Simvastatin',
    'Codeine',
    'Amitriptyline',
    'Fluoxetine',
    'Omeprazole',
    'Metoprolol',
    'Lisinopril',
    'Atorvastatin',
    'Metformin',
    'Losartan',
    'Amlodipine',
    'Hydrochlorothiazide',
    'Furosemide',
    'Digoxin',
    'Quinidine',
    'Phenytoin',
    'Carbamazepine',
    'Valproic Acid',
    'Sertraline',
    'Paroxetine',
    'Venlafaxine',
    'Bupropion',
    'Trazodone',
    'Mirtazapine',
    'Escitalopram',
    'Citalopram',
    'Duloxetine',
    'Milnacipran',
    'Levomilnacipran',
    'Vortioxetine',
    'Vilazodone',
    'Agomelatine',
    'Tianeptine',
    'Reboxetine',
    'Maprotiline',
    'Amoxapine',
    'Nortriptyline',
    'Desipramine'
  ];

  const filteredDrugs = drugList.filter(drug =>
    drug.toLowerCase().includes(searchTerm.toLowerCase())
  );

  // Check if brain button should be enabled
  const isBrainButtonEnabled = selectedDrug && vcfFile && !isProcessing;

  const handleBrainButtonClick = async () => {
    if (!selectedDrug || !vcfFile) return;
    
    setIsProcessing(true);
    
    try {
      // Read the VCF file content
      const vcfContent = await vcfFile.text();
      
      // Prepare the request payload
      const requestPayload = {
        drugName: selectedDrug,
        vcfFileContent: vcfContent
      };
      
      // Make API call to the backend
      const response = await fetch('http://localhost:8000/api/drugrisk/assess', {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json',
        },
        body: JSON.stringify(requestPayload)
      });
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      const result = await response.json();
      console.log('Risk assessment result:', result);
      
      // Store the risk assessment result
      setRiskAssessment(result);
      
    } catch (error) {
      console.error('Error processing risk assessment:', error);
      // TODO: Show error message to user
    } finally {
      setIsProcessing(false);
    }
  };

  return (
    <div className="home">
      {/* GeneRisk Branding - Left Side */}
      <div className="branding-section">
        <h1 className="brand-title">GeneRisk</h1>
        <p className="brand-description">Discover risks drugs pose to you, and safe alternatives.</p>
        <div className="brand-underline"></div>
        <div className="helix-wave-visual">
          <div className="genetic-code-grid">
            <div className="code-row">
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
            </div>
            <div className="code-row">
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
            </div>
            <div className="code-row">
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
            </div>
            <div className="code-row">
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
            </div>
            <div className="code-row">
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
            </div>
            <div className="code-row">
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
              <div className="code-square"></div>
              <div className="code-square active"></div>
            </div>
          </div>
        </div>
      </div>

      <div className="main-layout">
        <div className="left-section">
          {/* Left section is empty */}
        </div>

        <div className="right-container">
          {/* Right container is empty */}
          
          {!isProcessing && !riskAssessment ? (
            <>
              {/* Drug Selection Button */}
              <div className="drug-selection-section">
                <button className="drug-select-btn" onClick={handleDrugButtonClick}>
                  {selectedDrug || 'Select Drug'}
                  <svg className="dropdown-arrow" width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M6 9L12 15L18 9" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                </button>
              </div>

              {/* VCF File Upload Section */}
              <div className="vcf-upload-section">
                <div 
                  className={`vcf-drop-zone ${isDragOver ? 'drag-over' : ''}`} 
                  id="vcf-drop-zone"
                  onDragOver={handleDragOver}
                  onDragLeave={handleDragLeave}
                  onDrop={handleDrop}
                  onClick={!vcfFile ? handleClick : undefined}
                >
                  {!vcfFile ? (
                    <div className="vcf-upload-content">
                      <svg width="24" height="24" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                        <path d="M14 2H6C5.46957 2 4.96086 2.21071 4.58579 2.58579C4.21071 2.96086 4 3.46957 4 4V20C4 20.5304 4.21071 21.0391 4.58579 21.4142C4.96086 21.7893 5.46957 22 6 22H18C18.5304 22 19.0391 21.7893 19.4142 21.4142C19.7893 21.0391 20 20.5304 20 20V8L14 2Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                        <polyline points="14,2 14,8 20,8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                      </svg>
                      <p className="vcf-upload-text">Drag & drop VCF file here</p>
                      <p className="vcf-upload-subtext">or click to browse</p>
                    </div>
                  ) : (
                    <div className="vcf-file-display">
                      <div className="file-info">
                        <svg width="20" height="20" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                          <path d="M14 2H6C5.46957 2 4.96086 2.21071 4.58579 2.58579C4.21071 2.96086 4 3.46957 4 4V20C4 20.5304 4.21071 21.0391 4.58579 21.4142C4.96086 21.7893 5.46957 22 6 22H18C18.5304 22 19.0391 21.7893 19.4142 21.4142C19.7893 21.0391 20 20.5304 20 20V8L14 2Z" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                          <polyline points="14,2 14,8 20,8" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                        </svg>
                        <span className="file-name">{vcfFile.name}</span>
                      </div>
                      <button className="remove-file-btn" onClick={handleRemoveFile}>
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                          <path d="M18 6L6 18M6 6L18 18" stroke="#ff4444" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                        </svg>
                      </button>
                    </div>
                  )}
                  <input 
                    ref={fileInputRef}
                    type="file" 
                    id="vcf-file-input" 
                    accept=".vcf" 
                    style={{ display: 'none' }}
                    onChange={handleFileSelect}
                  />
                </div>
              </div>
              
              {/* Circular buttons at bottom right */}
              <div className="circular-buttons">
                <button className="circular-btn reset-btn" onClick={handleReset}>
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M1 4V10H7" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                    <path d="M3.51 15A9 9 0 1 0 6 5L1 10" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                </button>
                
                <button 
                  className={`circular-btn brain-btn ${!isBrainButtonEnabled ? 'brain-btn-disabled' : ''}`}
                  disabled={!isBrainButtonEnabled}
                  onClick={handleBrainButtonClick}
                >
                  <img src="/brain.png" alt="Brain" width="28" height="28" />
                </button>
              </div>
            </>
          ) : isProcessing ? (
            /* Loading state - replaces entire right container content */
            <div className="container-loading">
              <div className="loading-content">
                <div className="spinner"></div>
                <p className="loading-text">Predicting risks...</p>
              </div>
            </div>
          ) : (
            /* Risk Assessment Results */
            <div className="risk-assessment-results">
              <div className="results-header">
                <h2>{riskAssessment.drugName}</h2>
                <button className="back-btn" onClick={handleReset}>
                  <svg width="16" height="16" viewBox="0 0 24 24" fill="none" xmlns="http://www.w3.org/2000/svg">
                    <path d="M15 18L9 12L15 6" stroke="white" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round"/>
                  </svg>
                </button>
              </div>
              
              <div className="drug-info">
                <div className="risk-score-container">
                  <div className="risk-score">
                    <span className="score-label">Risk Score</span>
                    <span className="score-value">{(riskAssessment.riskScore * 100).toFixed(1)}%</span>
                  </div>
                  <div className="risk-level">
                    <span className="level-label">Risk Level</span>
                    <span className={`level-value level-${riskAssessment.riskLevel.toLowerCase()}`}>
                      {riskAssessment.riskLevel}
                    </span>
                  </div>
                </div>
              </div>
              
              <div className="explanation-section">
                <p className="explanation-text">{riskAssessment.explanation}</p>
              </div>
              
              <div className="alternatives-summary">
                <div className="alternatives-container">
                  <div className="alternative-summary">
                    <span className="alternative-label">ALTERNATIVES</span>
                    <span className="alternative-value">
                      {riskAssessment.drugAlternatives && riskAssessment.drugAlternatives.length > 0 
                        ? riskAssessment.drugAlternatives[0].alternativeDrug 
                        : 'N/A'}
                    </span>
                  </div>
                </div>
              </div>
              
              <button className="analytics-btn" onClick={() => navigate('/analytics', { 
                state: { 
                  riskScore: riskAssessment.riskScore,
                  riskLevel: riskAssessment.riskLevel,
                  drugName: riskAssessment.drugName
                }
              })}>
                Analytics
              </button>
              

            </div>
          )}
        </div>
      </div>
      
      {/* Drug Popup - Moved outside of right-container to avoid stacking context issues */}
      {isDropdownOpen && (
        <div className="drug-popup-overlay" onClick={handleOverlayClick}>
          <div className="drug-popup">
            <div className="popup-header">
              <input
                type="text"
                placeholder="Search drugs..."
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
                className="drug-search-input"
                style={{ flex: 1, marginLeft: '1rem', marginRight: '1rem' }}
              />
              <button className="popup-close-btn" onClick={handleClosePopup}>
                ✕
              </button>
            </div>
            <div className="drug-list">
              {filteredDrugs.map((drug, index) => (
                <div
                  key={index}
                  className="drug-item"
                  onClick={() => handleDrugSelect(drug)}
                >
                  {drug}
                </div>
              ))}
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default Home; 