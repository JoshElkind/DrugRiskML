#!/usr/bin/env python3

import snowflake.connector
import pandas as pd
import numpy as np
import xgboost as xgb
from sklearn.model_selection import train_test_split, cross_val_score, GridSearchCV
from sklearn.metrics import classification_report, confusion_matrix, roc_auc_score, precision_recall_curve
from sklearn.preprocessing import LabelEncoder, StandardScaler
from sklearn.ensemble import VotingClassifier, RandomForestClassifier, GradientBoostingClassifier
from sklearn.linear_model import LogisticRegression
from sklearn.svm import SVC
from sklearn.neural_network import MLPClassifier
import joblib
import json
from datetime import datetime
import warnings
warnings.filterwarnings('ignore')

class EnhancedDrugRiskTrainer:
    def __init__(self):
        self.ensemble_model = None
        self.xgb_model = None
        self.scaler = StandardScaler()
        self.label_encoder = LabelEncoder()
        self.feature_columns = []
        self.model_info = {}
        self.individual_models = {}
        
    def connect_to_snowflake(self):
        try:
            self.conn = snowflake.connector.connect(
                user="",
                password="",
                account="",
                warehouse="",
                database="",
                schema=""
            )
            return True
        except Exception as e:
            return False
    
    def load_training_data(self):
        try:
            query = """
            SELECT 
                UPLOAD_ID,
                DRUG_NAME,
                VARIANT_COUNT,
                HIGH_RISK_VARIANTS,
                RISK_SCORE,
                CLINICAL_OUTCOME,
                CREATED_AT
            FROM PATIENT_PRESCRIPTIONS
            WHERE CLINICAL_OUTCOME IS NOT NULL
            """
            
            df_prescriptions = pd.read_sql(query, self.conn)
            
            query_variants = """
            SELECT 
                UPLOAD_ID,
                GENE,
                IMPACT,
                VARIANT_TYPE,
                CLINICAL_SIGNIFICANCE,
                DRUG_INTERACTIONS
            FROM ANNOTATED_VARIANTS
            """
            
            df_variants = pd.read_sql(query_variants, self.conn)
            
            query_interactions = """
            SELECT 
                VARIANT_ID,
                GENE,
                DRUGS,
                SIGNIFICANCE,
                CLINICAL_EVIDENCE
            FROM PHARMGKB_VARIANT_DRUG
            """
            
            df_interactions = pd.read_sql(query_interactions, self.conn)
            
            return df_prescriptions, df_variants, df_interactions
            
        except Exception as e:
            return None, None, None
    
    def engineer_features(self, df_prescriptions, df_variants, df_interactions):
        try:
            df = df_prescriptions.copy()
            
            df['DRUG_RISK_RATIO'] = df['HIGH_RISK_VARIANTS'] / df['VARIANT_COUNT'].replace(0, 1)
            df['VARIANT_DENSITY'] = df['VARIANT_COUNT'] / 1000
            
            if df_variants is not None and len(df_variants) > 0:
                gene_features = df_variants.groupby('UPLOAD_ID').agg({
                    'GENE': 'nunique',
                    'IMPACT': lambda x: (x == 'HIGH').sum(),
                    'CLINICAL_SIGNIFICANCE': lambda x: (x == 'Pathogenic').sum()
                }).reset_index()
                gene_features.columns = ['UPLOAD_ID', 'UNIQUE_GENES', 'HIGH_IMPACT_VARIANTS', 'PATHOGENIC_VARIANTS']
                df = df.merge(gene_features, on='UPLOAD_ID', how='left')
                df = df.fillna(0)
            
            if df_interactions is not None and len(df_interactions) > 0:
                interaction_features = df_interactions.groupby('GENE').agg({
                    'DRUGS': 'nunique',
                    'SIGNIFICANCE': lambda x: (x == 'High').sum()
                }).reset_index()
                interaction_features.columns = ['GENE', 'DRUG_INTERACTIONS', 'HIGH_SIGNIFICANCE_INTERACTIONS']
                
                if df_variants is not None:
                    gene_interactions = df_variants.merge(interaction_features, on='GENE', how='left')
                    interaction_summary = gene_interactions.groupby('UPLOAD_ID').agg({
                        'DRUG_INTERACTIONS': 'sum',
                        'HIGH_SIGNIFICANCE_INTERACTIONS': 'sum'
                    }).reset_index()
                    df = df.merge(interaction_summary, on='UPLOAD_ID', how='left')
                    df = df.fillna(0)
            
            df['RISK_SCORE_SQUARED'] = df['RISK_SCORE'] ** 2
            df['HIGH_RISK_VARIANT_RATIO'] = df['HIGH_RISK_VARIANTS'] / df['VARIANT_COUNT'].replace(0, 1)
            df['INTERACTION_DENSITY'] = df.get('DRUG_INTERACTIONS', 0) / df['VARIANT_COUNT'].replace(0, 1)
            
            df['DRUG_NAME_ENCODED'] = self.label_encoder.fit_transform(df['DRUG_NAME'])
            
            return df
            
        except Exception as e:
            return None
    
    def prepare_training_data(self, df):
        try:
            feature_cols = [
                'VARIANT_COUNT', 'HIGH_RISK_VARIANTS', 'RISK_SCORE', 'DRUG_RISK_RATIO',
                'VARIANT_DENSITY', 'UNIQUE_GENES', 'HIGH_IMPACT_VARIANTS', 
                'PATHOGENIC_VARIANTS', 'DRUG_INTERACTIONS', 'HIGH_SIGNIFICANCE_INTERACTIONS',
                'RISK_SCORE_SQUARED', 'HIGH_RISK_VARIANT_RATIO', 'INTERACTION_DENSITY',
                'DRUG_NAME_ENCODED'
            ]
            
            available_cols = [col for col in feature_cols if col in df.columns]
            self.feature_columns = available_cols
            
            X = df[available_cols]
            y = df['CLINICAL_OUTCOME']
            
            X_train, X_test, y_train, y_test = train_test_split(
                X, y, test_size=0.2, random_state=42, stratify=y
            )
            
            X_train_scaled = self.scaler.fit_transform(X_train)
            X_test_scaled = self.scaler.transform(X_test)
            
            return X_train_scaled, X_test_scaled, y_train, y_test
            
        except Exception as e:
            return None, None, None, None
    
    def create_individual_models(self):
        self.individual_models['xgb'] = xgb.XGBClassifier(
            n_estimators=100,
            max_depth=6,
            learning_rate=0.1,
            random_state=42,
            eval_metric='logloss'
        )
        
        self.individual_models['rf'] = RandomForestClassifier(
            n_estimators=100,
            max_depth=10,
            random_state=42
        )
        
        self.individual_models['gb'] = GradientBoostingClassifier(
            n_estimators=100,
            max_depth=6,
            learning_rate=0.1,
            random_state=42
        )
        
        self.individual_models['lr'] = LogisticRegression(
            random_state=42,
            max_iter=1000
        )
        
        self.individual_models['svm'] = SVC(
            probability=True,
            random_state=42,
            kernel='rbf'
        )
        
        self.individual_models['mlp'] = MLPClassifier(
            hidden_layer_sizes=(100, 50),
            max_iter=500,
            random_state=42
        )
    
    def train_individual_models(self, X_train, y_train):
        model_scores = {}
        
        for name, model in self.individual_models.items():
            try:
                model.fit(X_train, y_train)
                cv_scores = cross_val_score(model, X_train, y_train, cv=5, scoring='roc_auc')
                model_scores[name] = cv_scores.mean()
            except Exception as e:
                model_scores[name] = 0
        
        return model_scores
    
    def create_ensemble_model(self, model_scores):
        top_models = sorted(model_scores.items(), key=lambda x: x[1], reverse=True)[:4]
        
        estimators = []
        for name, score in top_models:
            if name in self.individual_models:
                estimators.append((name, self.individual_models[name]))
        
        self.ensemble_model = VotingClassifier(
            estimators=estimators,
            voting='soft'
        )
    
    def train_ensemble_model(self, X_train, y_train):
        self.ensemble_model.fit(X_train, y_train)
        
        if 'xgb' in self.individual_models:
            self.xgb_model = self.individual_models['xgb']
    
    def evaluate_models(self, X_test, y_test):
        results = {}
        
        y_pred_ensemble = self.ensemble_model.predict(X_test)
        y_prob_ensemble = self.ensemble_model.predict_proba(X_test)[:, 1]
        
        results['ensemble'] = {
            'accuracy': (y_pred_ensemble == y_test).mean(),
            'roc_auc': roc_auc_score(y_test, y_prob_ensemble),
            'classification_report': classification_report(y_test, y_pred_ensemble)
        }
        
        for name, model in self.individual_models.items():
            try:
                y_pred = model.predict(X_test)
                y_prob = model.predict_proba(X_test)[:, 1]
                
                results[name] = {
                    'accuracy': (y_pred == y_test).mean(),
                    'roc_auc': roc_auc_score(y_test, y_prob),
                    'classification_report': classification_report(y_test, y_pred)
                }
                
            except Exception as e:
                pass
        
        return results
    
    def save_enhanced_model(self, results):
        joblib.dump(self.ensemble_model, 'models/enhanced_drug_risk_model.pkl')
        
        if self.xgb_model:
            joblib.dump(self.xgb_model, 'models/xgb_drug_risk_model.pkl')
        
        joblib.dump(self.scaler, 'models/enhanced_scaler.pkl')
        joblib.dump(self.label_encoder, 'models/enhanced_label_encoder.pkl')
        
        with open('models/enhanced_feature_columns.json', 'w') as f:
            json.dump(self.feature_columns, f)
        
        model_info = {
            'ensemble_model_path': 'models/enhanced_drug_risk_model.pkl',
            'xgb_model_path': 'models/xgb_drug_risk_model.pkl',
            'scaler_path': 'models/enhanced_scaler.pkl',
            'label_encoder_path': 'models/enhanced_label_encoder.pkl',
            'feature_columns': self.feature_columns,
            'training_date': datetime.now().isoformat(),
            'model_type': 'Ensemble (XGBoost + scikit-learn)',
            'individual_models': list(self.individual_models.keys()),
            'evaluation_results': results
        }
        
        with open('models/enhanced_model_metadata.json', 'w') as f:
            json.dump(model_info, f, indent=2)
        
        return model_info
    
    def run_enhanced_training_pipeline(self):
        if not self.connect_to_snowflake():
            return False
        
        df_prescriptions, df_variants, df_interactions = self.load_training_data()
        if df_prescriptions is None:
            return False
        
        df = self.engineer_features(df_prescriptions, df_variants, df_interactions)
        if df is None:
            return False
        
        X_train, X_test, y_train, y_test = self.prepare_training_data(df)
        if X_train is None:
            return False
        
        self.create_individual_models()
        model_scores = self.train_individual_models(X_train, y_train)
        self.create_ensemble_model(model_scores)
        self.train_ensemble_model(X_train, y_train)
        results = self.evaluate_models(X_test, y_test)
        model_info = self.save_enhanced_model(results)
        
        return True

def main():
    trainer = EnhancedDrugRiskTrainer()
    success = trainer.run_enhanced_training_pipeline()

if __name__ == "__main__":
    main()