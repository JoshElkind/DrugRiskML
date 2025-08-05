import axios from 'axios';
import { DrugRiskRequest, DrugRiskResponse, TableauEmbedUrl, CommunityAnalytics } from '../types';

const API_BASE_URL = 'http://localhost:5000/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

export const drugRiskApi = {
  // Make a drug risk assessment
  async assessRisk(request: DrugRiskRequest): Promise<DrugRiskResponse> {
    const response = await api.post<DrugRiskResponse>('/drugrisk/assess', request);
    return response.data;
  },

  // Get user's assessment results
  async getUserAssessment(userRunId: number): Promise<DrugRiskResponse> {
    const response = await api.get<DrugRiskResponse>(`/drugrisk/user-run/${userRunId}`);
    return response.data;
  },

  // Get user's latest assessment
  async getUserLatestAssessment(userId: string): Promise<DrugRiskResponse> {
    const response = await api.get<DrugRiskResponse>(`/drugrisk/user/${userId}/latest`);
    return response.data;
  },

  // Get general Tableau dashboard URL
  async getGeneralDashboardUrl(drugName?: string): Promise<TableauEmbedUrl> {
    const params = drugName ? { drugName } : {};
    const response = await api.get<TableauEmbedUrl>('/tableau/analytics/drug-risk', { params });
    return response.data;
  },

  // Get user-specific Tableau dashboard URL
  async getUserSpecificDashboardUrl(
    drugName: string,
    userRiskScore: number,
    userRiskLevel: string,
    userId?: string
  ): Promise<TableauEmbedUrl> {
    const params: any = {
      drugName,
      userRiskScore,
      userRiskLevel,
    };
    if (userId) params.userId = userId;

    const response = await api.get<TableauEmbedUrl>('/tableau/analytics/user-specific', { params });
    return response.data;
  },

  // Get available dashboards
  async getAvailableDashboards(): Promise<string[]> {
    const response = await api.get<string[]>('/tableau/dashboards');
    return response.data;
  },

  // Get community analytics
  async getCommunityAnalytics(drugName?: string): Promise<CommunityAnalytics> {
    const params = drugName ? { drugName } : {};
    const response = await api.get<CommunityAnalytics>('/snowflake/analytics/summary', { params });
    return response.data;
  },

  // Get community analytics by drug
  async getCommunityAnalyticsByDrug(drugName: string): Promise<any[]> {
    const response = await api.get<any[]>('/snowflake/analytics/community', { 
      params: { drugName } 
    });
    return response.data;
  },
};

export default api; 