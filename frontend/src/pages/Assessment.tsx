import React, { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { drugRiskApi } from '../services/api';
import { DrugRiskRequest, DRUG_OPTIONS } from '../types';
import './Assessment.css';

const Assessment: React.FC = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState({
    userId: '',
    drugName: 'Warfarin',
    vcfFileContent: '',
    vcfFileName: ''
  });
  const [file, setFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState<string>('');
  const [success, setSuccess] = useState<string>('');

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const selectedFile = e.target.files?.[0];
    if (selectedFile) {
      setFile(selectedFile);
      setFormData(prev => ({
        ...prev,
        vcfFileName: selectedFile.name
      }));
      
      // Read file content
      const reader = new FileReader();
      reader.onload = (event) => {
        const content = event.target?.result as string;
        setFormData(prev => ({
          ...prev,
          vcfFileContent: content
        }));
      };
      reader.readAsText(selectedFile);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement | HTMLTextAreaElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setLoading(true);
    setError('');
    setSuccess('');

    try {
      const request: DrugRiskRequest = {
        userId: formData.userId || `user-${Date.now()}`,
        drugName: formData.drugName,
        vcfFileContent: formData.vcfFileContent,
        vcfFileName: formData.vcfFileName
      };

      const response = await drugRiskApi.assessRisk(request);
      setSuccess('Assessment completed successfully!');
      
      // Navigate to results page after a short delay
      setTimeout(() => {
        navigate(`/results/${response.userRunId}`);
      }, 2000);

    } catch (err: any) {
      console.error('Assessment error:', err);
      setError(err.response?.data?.message || 'Failed to complete assessment. Please try again.');
    } finally {
      setLoading(false);
    }
  };

  const generateSampleVCF = () => {
    const sampleVCF = `#CHROM\tPOS\tID\tREF\tALT\tQUAL\tFILTER\tINFO
1\t1000\t.\tA\tT\t100\tPASS\tGENE=CYP2C9;IMPACT=HIGH
1\t2000\t.\tG\tC\t100\tPASS\tGENE=VKORC1;IMPACT=MODERATE
1\t3000\t.\tT\tA\t100\tPASS\tGENE=SLCO1B1;IMPACT=LOW`;
    
    setFormData(prev => ({
      ...prev,
      vcfFileContent: sampleVCF,
      vcfFileName: 'sample.vcf'
    }));
    setFile(null);
  };

  return (
    <div className="assessment">
      <div className="assessment-header">
        <h1>🔬 Drug Risk Assessment</h1>
        <p>Upload your genetic data and receive personalized drug risk analysis</p>
      </div>

      <div className="assessment-container">
        <div className="assessment-form">
          <form onSubmit={handleSubmit}>
            <div className="form-group">
              <label htmlFor="userId">User ID (Optional):</label>
              <input
                type="text"
                id="userId"
                name="userId"
                value={formData.userId}
                onChange={handleInputChange}
                placeholder="Enter your user ID or leave blank for anonymous"
                className="form-control"
              />
            </div>

            <div className="form-group">
              <label htmlFor="drugName">Drug to Assess:</label>
              <select
                id="drugName"
                name="drugName"
                value={formData.drugName}
                onChange={handleInputChange}
                className="form-control"
                required
              >
                {DRUG_OPTIONS.map(drug => (
                  <option key={drug.value} value={drug.value}>
                    {drug.label} ({drug.description})
                  </option>
                ))}
              </select>
            </div>

            <div className="form-group">
              <label htmlFor="vcfFile">VCF File Upload:</label>
              <input
                type="file"
                id="vcfFile"
                accept=".vcf,.txt"
                onChange={handleFileChange}
                className="form-control file-input"
              />
              <small className="form-help">
                Upload a VCF file containing your genetic variants, or use the sample data below
              </small>
            </div>

            <div className="form-group">
              <label>VCF Content:</label>
              <textarea
                name="vcfFileContent"
                value={formData.vcfFileContent}
                onChange={handleInputChange}
                placeholder="Paste VCF content here or upload a file above"
                rows={8}
                className="form-control"
                required
              />
              <div className="vcf-actions">
                <button
                  type="button"
                  onClick={generateSampleVCF}
                  className="btn btn-secondary"
                >
                  📋 Load Sample VCF
                </button>
                <button
                  type="button"
                  onClick={() => setFormData(prev => ({ ...prev, vcfFileContent: '' }))}
                  className="btn btn-outline"
                >
                  🗑️ Clear
                </button>
              </div>
            </div>

            {error && (
              <div className="error-message">
                ❌ {error}
              </div>
            )}

            {success && (
              <div className="success-message">
                ✅ {success}
              </div>
            )}

            <button
              type="submit"
              disabled={loading || !formData.vcfFileContent}
              className="btn btn-primary btn-large"
            >
              {loading ? '🔬 Processing...' : '🔬 Start Assessment'}
            </button>
          </form>
        </div>

        <div className="assessment-info">
          <div className="info-card">
            <h3>📋 What is a VCF File?</h3>
            <p>
              VCF (Variant Call Format) files contain information about genetic variants in your DNA. 
              They include details about chromosome position, reference and alternate alleles, and quality scores.
            </p>
          </div>

          <div className="info-card">
            <h3>🧬 How It Works</h3>
            <ul>
              <li>Upload your VCF file or paste VCF content</li>
              <li>Select the drug you want to assess</li>
              <li>Our AI analyzes your genetic variants</li>
              <li>Receive personalized risk scores and recommendations</li>
              <li>Compare with community data in analytics</li>
            </ul>
          </div>

          <div className="info-card">
            <h3>💊 Supported Drugs</h3>
            <div className="supported-drugs">
              {DRUG_OPTIONS.map(drug => (
                <div key={drug.value} className="drug-item">
                  <strong>{drug.label}</strong>
                  <span>{drug.description}</span>
                </div>
              ))}
            </div>
          </div>

          <div className="info-card">
            <h3>🔒 Privacy & Security</h3>
            <p>
              Your genetic data is processed securely and not stored permanently. 
              Assessment results are anonymized for community analytics.
            </p>
          </div>
        </div>
      </div>
    </div>
  );
};

export default Assessment; 