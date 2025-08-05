export interface DrugRiskRequest {
  userId?: string;
  drugName: string;
  vcfFileContent: string;
  vcfFileName: string;
}

export interface DrugRiskResponse {
  userRunId: number;
  userId: string;
  drugName: string;
  riskScore: number;
  riskLevel: string;
  confidence: number;
  explanation: string;
  drugAlternatives: DrugAlternative[];
  createdAt: string;
  tableauDashboardUrl?: string;
  tableauUserSpecificUrl?: string;
  tableauUserContext?: Record<string, any>;
}

export interface DrugAlternative {
  alternativeDrug: string;
  reason: string;
  confidenceScore: number;
  clinicalEvidence: string;
  dosageRecommendation: string;
  monitoringRequirements: string;
}

export interface TableauEmbedUrl {
  url: string;
  token: string;
  expiresAt: string;
  userContext?: Record<string, any>;
}

export interface CommunityAnalytics {
  TOTAL_ASSESSMENTS: number;
  UNIQUE_USERS: number;
  UNIQUE_DRUGS: number;
  OVERALL_AVG_RISK: number;
  TOTAL_HIGH_RISK: number;
  TOTAL_MODERATE_RISK: number;
  TOTAL_LOW_RISK: number;
}

export interface DrugOption {
  value: string;
  label: string;
  description: string;
}

export const DRUG_OPTIONS: DrugOption[] = [
  { value: 'Warfarin', label: 'Warfarin', description: 'Anticoagulant' },
  { value: 'Clopidogrel', label: 'Clopidogrel', description: 'Antiplatelet' },
  { value: 'Simvastatin', label: 'Simvastatin', description: 'Statin' },
  { value: 'Codeine', label: 'Codeine', description: 'Opioid' },
  { value: 'Carbamazepine', label: 'Carbamazepine', description: 'Anticonvulsant' },
  { value: 'Tramadol', label: 'Tramadol', description: 'Opioid' },
  { value: 'Tacrolimus', label: 'Tacrolimus', description: 'Immunosuppressant' },
  { value: 'Azathioprine', label: 'Azathioprine', description: 'Immunosuppressant' },
]; 